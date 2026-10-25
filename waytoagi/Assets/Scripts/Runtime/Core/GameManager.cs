using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pachinko
{
    public enum GameState
    {
        Menu,
        Exchange,
        Aim,
        BallFlight,
        Resolving,
        Victory
    }

    /// <summary>
    /// V3 动态循环：
    /// 发射（一次拉杆齐射）→ 倾斜控球 → 落洞结算 → 本循环齐射次数用完后判定阈值
    /// → 自动进入兑换 → 下一循环 / 重试当前循环。
    /// 标准弹珠无限供应，不再有商店补给与奖励洞升级。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public GameConfig Config { get; private set; }
        public SaveData SaveData { get; private set; }
        public BoardRefs Board { get; private set; }
        public SpringLauncher Launcher { get; private set; }
        public TiltController Tilt { get; private set; }
        public ScoreManager Score { get; private set; }
        public ShopManager Shop { get; private set; }
        public LoopManager Loop { get; private set; }
        public HUDController Hud { get; private set; }
        public Camera Camera { get; private set; }
        public GameState State { get; private set; } = GameState.Menu;

        public int CurrentShotScore { get; private set; }
        public int RoundsPlayed { get; private set; }
        public int VolleyTotalBalls { get; private set; }
        public int VolleyResolved { get; private set; }
        public int VolleyHits { get; private set; }
        public int LoopScore => Loop != null ? Loop.AttemptScore : 0;
        public int LoopIndex => Loop != null ? Loop.LoopIndex : 1;
        public int LoopThreshold => Loop != null ? Loop.Threshold : 0;
        public int LoopVolleys => Loop != null ? Loop.VolleysThisAttempt : 0;
        public int LoopVolleysDone => Loop != null ? Loop.VolleysCompleted : 0;
        public int LoopVolleysRemaining => Loop != null ? Loop.VolleysRemaining : 0;
        public int CurrentVolleyNumber => Mathf.Clamp(LoopVolleysDone + 1, 1, Mathf.Max(1, LoopVolleys));
        public int GoalScore => Loop != null ? Loop.GoalScore : 1000000;
        public int GoalRemaining => Loop != null ? Loop.GoalRemaining : 0;
        public float GoalProgress => Loop != null ? Loop.GoalProgress : 0f;
        public bool GoalReached => SaveData != null && SaveData.goalReached;
        public int BestVolleyScore => SaveData != null ? SaveData.bestVolleyScore : 0;
        public int BestLoopScore => SaveData != null ? SaveData.bestLoopScore : 0;
        public int BallMultiplier => Shop != null ? Shop.BallMultiplier : 1;
        public EnchantDef ActiveEnchant => Shop != null ? Shop.ActiveEnchant : null;
        public BallDef StandardBall => Config != null ? Config.GetStandardBall() : null;

        public Color CurrentBallColor
        {
            get
            {
                var enchant = ActiveEnchant;
                return enchant != null ? Theme.Hex(enchant.color) : Theme.BallColor(StandardBall);
            }
        }

        public bool IsLaunchPhase =>
            State == GameState.Aim || State == GameState.BallFlight || State == GameState.Resolving;

        private readonly List<BallDef> _magazine = new List<BallDef>();
        private readonly List<MarbleController> _activeMarbles = new List<MarbleController>();
        private readonly HashSet<MarbleController> _resolvedMarbles = new HashSet<MarbleController>();
        private MarbleController[] _marbles;
        private bool _pendingLoopAdvance;
        private bool _pendingLoopRetry;
        private bool _goalReachedThisVolley;

        public void Initialize(GameConfig config, SaveData save, BoardRefs board, MarbleController[] marbles,
            SpringLauncher launcher, TiltController tilt, ScoreManager score, ShopManager shop, Camera camera)
        {
            Config = config;
            SaveData = save;
            Board = board;
            _marbles = marbles;
            Launcher = launcher;
            Tilt = tilt;
            Score = score;
            Shop = shop;
            Camera = camera;
            Loop = new LoopManager(config, save);

            State = GameState.Menu;
            CurrentShotScore = 0;
            VolleyTotalBalls = 0;
            VolleyResolved = 0;
            VolleyHits = 0;
            _magazine.Clear();
            _activeMarbles.Clear();
            _resolvedMarbles.Clear();

            // 同一轮齐射的弹珠互相不碰撞，避免在待发射槽和出口挤成一团。
            for (int i = 0; i < _marbles.Length; i++)
            {
                if (_marbles[i] == null) continue;
                var captured = _marbles[i];
                captured.Lost += () => OnMarbleLost(captured);
                for (int j = i + 1; j < _marbles.Length; j++)
                {
                    if (_marbles[j] == null) continue;
                    Physics2D.IgnoreCollision(captured.Collider, _marbles[j].Collider, true);
                }
            }

            Launcher.CanLaunch = () => State == GameState.Aim;
            Launcher.LaunchRequested += OnLaunchRequested;
            foreach (var hole in Board.Holes) hole.Hit += OnHoleHit;
            Shop.Changed += OnShopChanged;

            ApplyBaffleCount();
            RefreshHoleViews();
            Hud = HUDController.Create(this, config, camera, board, tilt, launcher);
            Hud.ShowMenu();
            AudioManager.PlayBgm("menu_loop");
        }

        public void StartGame()
        {
            if (State != GameState.Menu) return;
            Hud.HideAllModals();
            BeginLoopAttempt();
        }

        /// <summary>开始一次循环尝试：重试失败循环时 loopIndex 不变。</summary>
        private void BeginLoopAttempt()
        {
            Loop.BeginAttempt();
            Score.ResetCombo();
            _goalReachedThisVolley = false;
            _pendingLoopAdvance = false;
            _pendingLoopRetry = false;
            Debug.Log($"[Loop] 第 {Loop.LoopIndex} 循环开始：{Loop.VolleysThisAttempt} 次齐射，目标 {Loop.Threshold:N0}");
            BeginVolley();
        }

        /// <summary>准备一次齐射：无限标准弹珠按固定数量装填，外观跟随当前附魔。</summary>
        private void BeginVolley()
        {
            _magazine.Clear();
            _magazine.AddRange(MagazineBuilder.Build(Config));
            if (_magazine.Count == 0 && StandardBall != null) _magazine.Add(StandardBall);
            if (_magazine.Count > _marbles.Length)
            {
                Debug.LogWarning($"[Volley] 齐射 {_magazine.Count} 颗超过对象池 {_marbles.Length}，只取前 {_marbles.Length} 颗。");
                _magazine.RemoveRange(_marbles.Length, _magazine.Count - _marbles.Length);
            }

            State = GameState.Aim;
            AudioManager.PlayBgm("main_loop");
            Tilt.SetControl(false, true);
            CurrentShotScore = 0;
            _goalReachedThisVolley = false;
            VolleyTotalBalls = _magazine.Count;
            VolleyResolved = 0;
            VolleyHits = 0;
            _activeMarbles.Clear();
            _resolvedMarbles.Clear();

            var enchant = ActiveEnchant;
            for (int i = 0; i < _marbles.Length; i++)
            {
                var marble = _marbles[i];
                if (marble == null) continue;
                if (i < _magazine.Count)
                {
                    marble.SetAppearance(StandardBall, enchant);
                    marble.SetExplosion(Config.explosion, SaveData.explosionUnlocked);
                    marble.PrepareAt(BoardGeometry.VolleyRackPosition(i, _magazine.Count));
                    _activeMarbles.Add(marble);
                }
                else
                {
                    marble.HideImmediate();
                }
            }

            for (int i = 0; i < _marbles.Length; i++)
            {
                if (_marbles[i] == null) continue;
                for (int j = i + 1; j < _marbles.Length; j++)
                {
                    if (_marbles[j] == null) continue;
                    Physics2D.IgnoreCollision(_marbles[i].Collider, _marbles[j].Collider, true);
                }
            }

            Launcher.Prepare();
            foreach (var hole in Board.Holes)
            {
                hole.Arm();
                hole.SetPoints(hole.Def.basePoints);
            }
            Hud.HideAllModals();
            Hud.RefreshAll();
            Debug.Log($"[Volley] 第 {Loop.LoopIndex} 循环 · 第 {CurrentVolleyNumber}/{Loop.VolleysThisAttempt} 次齐射，装填 {_activeMarbles.Count} 颗，附魔倍率 ×{BallMultiplier}");
        }

        private void OnLaunchRequested(float chargeRatio)
        {
            if (State != GameState.Aim || _activeMarbles.Count == 0) return;

            State = GameState.BallFlight;
            RoundsPlayed++;
            VolleyResolved = 0;
            VolleyHits = 0;
            CurrentShotScore = 0;
            AudioManager.Play("launch");
            Tilt.SetControl(true);

            int n = _activeMarbles.Count;
            int pulses = Mathf.CeilToInt(n / 2f);
            Launcher.Fire(chargeRatio, pulses);
            StartCoroutine(LaunchVolleyBurst(chargeRatio, n));
            Hud.RefreshAll();
            StartCoroutine(VolleyMeasure());
            StartCoroutine(VolleyTimeout());
        }

        /// <summary>一次拉杆的弹簧连弹：出口常开，弹簧连续回弹，把整槽弹珠快速弹出。</summary>
        private IEnumerator LaunchVolleyBurst(float chargeRatio, int total)
        {
            float baseSpeed = Mathf.Lerp(Config.launch.minBallSpeed, Config.launch.maxBallSpeed, Mathf.Clamp01(chargeRatio));
            int pulses = Mathf.CeilToInt(total / 2f);
            int launched = 0;

            for (int pulse = 0; pulse < pulses; pulse++)
            {
                if (State != GameState.BallFlight) yield break;
                int start = pulse * 2;
                int end = Mathf.Min(start + 2, total);
                for (int i = start; i < end; i++)
                {
                    if (i >= _activeMarbles.Count) continue;
                    var marble = _activeMarbles[i];
                    if (marble == null) continue;

                    float vy = baseSpeed * UnityEngine.Random.Range(0.82f, 1.0f);
                    float fan = total > 1 ? Mathf.Lerp(-0.5f, 0.5f, i / (float)(total - 1)) : 0f;
                    float vx = -2.2f + fan + UnityEngine.Random.Range(-0.12f, 0.12f);
                    marble.Launch(new Vector2(vx, vy));
                    launched++;
                }

                if (pulse < pulses - 1) yield return new WaitForSeconds(0.10f);
            }

            Debug.Log($"[Volley] one pull launched {launched} balls in {pulses} spring pulses at charge={chargeRatio:F2}, baseSpeed={baseSpeed:F2}");
        }

        private IEnumerator VolleyTimeout()
        {
            yield return new WaitForSeconds(15f);
            if (State != GameState.BallFlight) yield break;
            Debug.LogWarning("[Volley] timeout: resolving all remaining balls as lost");
            var snapshot = new List<MarbleController>(_activeMarbles);
            bool missed = false;
            foreach (var marble in snapshot)
            {
                if (marble == null || _resolvedMarbles.Contains(marble)) continue;
                Debug.Log($"[Volley] unresolved {marble.name} pos={marble.transform.position} vel={marble.Body.linearVelocity}");
                _resolvedMarbles.Add(marble);
                VolleyResolved++;
                missed = true;
                marble.HideImmediate();
            }
            if (missed) Score.RegisterMiss();
            Hud.ShowToast("齐射超时，未落洞的弹珠已收回");
            Hud.RefreshAll();
            StartCoroutine(FinishVolley());
        }

        private IEnumerator VolleyMeasure()
        {
            yield return new WaitForSeconds(0.35f);
            int active = 0;
            foreach (var marble in _activeMarbles)
            {
                if (!_resolvedMarbles.Contains(marble) && marble.gameObject.activeInHierarchy) active++;
            }
            Debug.Log($"[Volley] inFlight={active}, resolved={VolleyResolved}/{VolleyTotalBalls}");
        }

        private void OnHoleHit(RewardHole hole, MarbleController marble)
        {
            if (State != GameState.BallFlight || marble == null) return;
            if (!_activeMarbles.Contains(marble) || _resolvedMarbles.Contains(marble)) return;

            if (!hole.HasReward)
            {
                ResolveEmptyHole(hole, marble);
                return;
            }

            _resolvedMarbles.Add(marble);
            VolleyResolved++;
            VolleyHits++;

            int points = hole.Def.basePoints;
            var result = Score.Score(StandardBall, hole.Def, BallMultiplier, points);
            CurrentShotScore += result.total;
            Loop.AddScore(result.total);
            SaveSystem.Save(SaveData);
            Debug.Log($"[Hole] {hole.Def.id} ball=×{result.ballMultiplier} hole=×{result.holeMultiplier} points={result.holePoints} combo=×{result.comboMultiplier} total={result.total} loop={Loop.AttemptScore:N0} resolved={VolleyResolved}/{VolleyTotalBalls}");

            // 落洞基础音与当前连击等级音同时播放；连击 5 级及以上持续使用 5 级音。
            AudioManager.Play("score_base");
            AudioManager.Play($"combo_{Mathf.Clamp(result.comboCount, 1, 5):00}");

            EvaluateGoal();
            marble.CaptureInto(new Vector2(hole.Def.positionX, BoardGeometry.HoleCenterY));
            Hud.PlayScorePopup(result, new Vector3(hole.Def.positionX, BoardGeometry.HoleCenterY + 0.9f, 0f));
            Hud.RefreshAll();

            if (VolleyResolved >= VolleyTotalBalls) StartCoroutine(FinishVolley());
        }

        /// <summary>空穴：接住弹珠但不结算积分，并中断同洞连击。</summary>
        private void ResolveEmptyHole(RewardHole hole, MarbleController marble)
        {
            _resolvedMarbles.Add(marble);
            VolleyResolved++;
            VolleyHits++;
            Score.RegisterMiss();
            marble.CaptureInto(new Vector2(hole.Def.positionX, BoardGeometry.HoleCenterY));
            Hud.PlayNoRewardPopup(hole.Def, new Vector3(hole.Def.positionX, BoardGeometry.HoleCenterY + 0.9f, 0f));
            Hud.RefreshAll();
            Debug.Log($"[Hole] {hole.Def.id} 空穴，无奖励，连击中断 resolved={VolleyResolved}/{VolleyTotalBalls}");

            if (VolleyResolved >= VolleyTotalBalls) StartCoroutine(FinishVolley());
        }

        private void OnMarbleLost(MarbleController marble)
        {
            if (State != GameState.BallFlight || marble == null) return;
            if (!_activeMarbles.Contains(marble) || _resolvedMarbles.Contains(marble)) return;

            _resolvedMarbles.Add(marble);
            VolleyResolved++;
            Score.RegisterMiss();
            marble.HideImmediate();
            Hud.RefreshAll();
            Debug.Log($"[Volley] ball lost, combo reset, resolved={VolleyResolved}/{VolleyTotalBalls}");

            if (VolleyResolved >= VolleyTotalBalls) StartCoroutine(FinishVolley());
        }

        /// <summary>首次达到 100 万累计积分时记录通关标记；循环结束/齐射结束后再弹通关面板。</summary>
        private void EvaluateGoal()
        {
            if (SaveData.goalReached) return;
            if (SaveData.totalScore < Loop.GoalScore) return;
            SaveData.goalReached = true;
            _goalReachedThisVolley = true;
            SaveSystem.Save(SaveData);
            Debug.Log($"[Goal] 累计积分达到 {SaveData.totalScore:N0}，通关目标达成，进入无尽模式");
        }

        private IEnumerator FinishVolley()
        {
            if (State != GameState.BallFlight) yield break;
            State = GameState.Resolving;
            Tilt.SetControl(false, true);
            yield return new WaitForSeconds(0.9f);

            Score.RecordVolleyScore(CurrentShotScore);
            Loop.RegisterVolley();
            EvaluateGoal();
            SaveSystem.Save(SaveData);

            if (_goalReachedThisVolley)
            {
                _goalReachedThisVolley = false;
                // 通关可能恰好发生在循环最后一次齐射：先把本循环成绩计入最高纪录，
                // 再显示通关面板，避免跳过 EndLoopAttempt 后丢失最高循环统计。
                if (Loop.AttemptComplete) Score.RecordLoopScore(Loop.AttemptScore);
                State = GameState.Victory;
                AudioManager.Play("win");
                Hud.HideAllModals();
                Hud.ShowVictory();
                Hud.RefreshAll();
                yield break;
            }

            if (Loop.AttemptComplete)
            {
                EndLoopAttempt();
            }
            else
            {
                BeginVolley();
            }
        }

        /// <summary>本循环的齐射次数用完：判定阈值，并自动打开兑换面板。</summary>
        private void EndLoopAttempt()
        {
            Score.RecordLoopScore(Loop.AttemptScore);
            bool passed = Loop.MeetsThreshold;
            _pendingLoopAdvance = passed;
            _pendingLoopRetry = !passed;
            SaveSystem.Save(SaveData);

            Debug.Log(passed
                ? $"[Loop] 第 {Loop.LoopIndex} 循环达标：{Loop.AttemptScore:N0} / {Loop.Threshold:N0}，进入兑换"
                : $"[Loop] 第 {Loop.LoopIndex} 循环未达标：{Loop.AttemptScore:N0} / {Loop.Threshold:N0}，可重试");

            if (!passed) AudioManager.Play("fail");
            AudioManager.PlayBgm("shop_loop");

            State = GameState.Exchange;
            Hud.HideAllModals();
            Hud.ShowExchange(true, passed);
            Hud.RefreshAll();
        }

        /// <summary>兑换面板关闭：达标则推进到下一循环，未达标则用同一循环号重试。</summary>
        public void CloseExchange()
        {
            if (State != GameState.Exchange) return;
            Hud.HideExchange();

            if (_pendingLoopRetry)
            {
                _pendingLoopRetry = false;
                _pendingLoopAdvance = false;
                BeginLoopAttempt();
                return;
            }

            if (_pendingLoopAdvance)
            {
                _pendingLoopAdvance = false;
                Loop.AdvanceLoop();
                SaveSystem.Save(SaveData);
            }
            BeginLoopAttempt();
        }

        public void HandleUnlockEnchant()
        {
            if (State != GameState.Exchange) return;
            var result = Shop.TryUnlockNextEnchant();
            if (result.success) AudioManager.Play("ui_unlock");
            Hud.ShowToast(result.message);
            Hud.RefreshAll();
        }

        public void HandleUnlockExplosion()
        {
            if (State != GameState.Exchange) return;
            var result = Shop.TryUnlockExplosion();
            if (result.success) AudioManager.Play("ui_unlock");
            Hud.ShowToast(result.message);
            Hud.RefreshAll();
        }

        public void HandleReduceBaffle()
        {
            if (State != GameState.Exchange) return;
            var result = Shop.TryReduceBaffle();
            if (result.success) AudioManager.Play("ui_unlock");
            Hud.ShowToast(result.message);
            Hud.RefreshAll();
        }

        public void ContinueFromVictory()
        {
            if (State != GameState.Victory) return;
            Hud.HideVictory();

            if (Loop.AttemptComplete)
            {
                // 通关后本循环视为达标，兑换结束后进入下一循环；无尽模式继续累计。
                _pendingLoopAdvance = true;
                _pendingLoopRetry = false;
                State = GameState.Exchange;
                AudioManager.PlayBgm("shop_loop");
                Hud.HideAllModals();
                Hud.ShowExchange(true, true);
                Hud.RefreshAll();
            }
            else
            {
                BeginVolley();
            }
        }

        private void OnShopChanged()
        {
            ApplyBaffleCount();
            RefreshHoleViews();
            SaveSystem.Save(SaveData);
            Hud.RefreshAll();
        }

        private void ApplyBaffleCount()
        {
            if (Board == null || Board.Baffles == null) return;
            int active = Mathf.Clamp(SaveData.baffleCount, 0, Board.Baffles.Count);
            for (int i = 0; i < Board.Baffles.Count; i++)
            {
                if (Board.Baffles[i] != null) Board.Baffles[i].gameObject.SetActive(i < active);
            }
        }

        private void RefreshHoleViews()
        {
            if (Board == null || Board.Holes == null) return;
            foreach (var hole in Board.Holes)
            {
                if (hole == null || hole.Def == null) continue;
                hole.SetPoints(hole.Def.basePoints);
            }
        }

        public void ResetSave()
        {
            SaveSystem.Delete();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>调试/自动化截图使用，正常 UI 不调用。</summary>
        public void DebugLaunch(float chargeRatio)
        {
            if (State != GameState.Aim) return;
            OnLaunchRequested(chargeRatio);
        }

        public void DebugAutoStartAim()
        {
            if (State == GameState.Menu) StartGame();
        }

        /// <summary>兼容自动化脚本：结束当前兑换并开始下一循环。</summary>
        public void DebugContinueStage()
        {
            if (State == GameState.Exchange) CloseExchange();
        }

        public void DebugGrantCredits(int amount)
        {
            SaveData.credits = Mathf.Max(0, SaveData.credits + amount);
            SaveSystem.Save(SaveData);
            Hud.RefreshAll();
        }

        /// <summary>自动化用：把当前齐射中所有尚未结算的弹珠强制结算到指定洞。</summary>
        public void DebugResolveAllActive(int holeIndex)
        {
            if (State != GameState.BallFlight) return;
            if (Board == null || Board.Holes == null || holeIndex < 0 || holeIndex >= Board.Holes.Length) return;
            var hole = Board.Holes[holeIndex];
            var snapshot = new List<MarbleController>(_activeMarbles);
            foreach (var marble in snapshot)
            {
                if (marble == null || _resolvedMarbles.Contains(marble)) continue;
                OnHoleHit(hole, marble);
            }
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SaveData.credits += 10000;
                SaveSystem.Save(SaveData);
                Hud.ShowToast("调试：+10,000 可用积分");
                Hud.RefreshAll();
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                SaveData.totalScore += 100000;
                SaveData.credits += 100000;
                EvaluateGoal();
                SaveSystem.Save(SaveData);
                Hud.ShowToast("调试：+100,000 累计得分与可用积分");
                Hud.RefreshAll();
            }
#endif
        }
    }
}
