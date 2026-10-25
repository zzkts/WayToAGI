using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pachinko
{
    public enum GameState
    {
        Menu,
        Shop,
        StageTransition,
        Aim,
        BallFlight,
        Resolving,
        Result,
        Milestone
    }

    public enum StageTransitionKind
    {
        None,
        ToLaunch,
        ToShop
    }

    /// <summary>
    /// 冲分循环：
    /// 商店阶段（购买/升级）→ 阶段过场 → 发射阶段（弹簧连续回弹，整槽弹珠快速出膛）
    /// → 得分与连锁结算 → 里程碑或下一轮商店。
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
        public HUDController Hud { get; private set; }
        public Camera Camera { get; private set; }
        public GameState State { get; private set; } = GameState.Menu;
        public int CurrentShotScore { get; private set; }
        public int RoundsPlayed { get; private set; }
        public int CurrentPhaseShots { get; private set; }
        public int CurrentPhaseScore { get; private set; }
        public int PhaseMagazineTotal { get; private set; }
        public int VolleyTotalBalls { get; private set; }
        public int VolleyResolved { get; private set; }
        public int VolleyHits { get; private set; }
        public int MagazineRemaining => _magazine.Count;
        public IReadOnlyList<BallDef> Magazine => _magazine;
        public StageTransitionKind PendingStage => _nextStage;
        public int BestVolleyScore => SaveData != null ? SaveData.bestVolleyScore : 0;
        public int NextMilestone
        {
            get
            {
                if (SaveData == null) return 0;
                foreach (int milestone in ScoreRules.Milestones)
                {
                    if (SaveData.totalScore < milestone) return milestone;
                }
                return 0;
            }
        }
        public int PreviousMilestone
        {
            get
            {
                if (SaveData == null) return 0;
                int previous = 0;
                foreach (int milestone in ScoreRules.Milestones)
                {
                    if (SaveData.totalScore >= milestone) previous = milestone;
                }
                return previous;
            }
        }
        public int LastMilestoneValue { get; private set; }
        public bool AllMilestonesReached => SaveData != null && SaveData.totalScore >= ScoreRules.MilestoneAt(ScoreRules.MilestoneCount - 1);
        public int MissShields => _missShields;
        public int PendingSplitBonus => _pendingSplitBonus;

        public bool IsLaunchPhase =>
            State == GameState.Aim || State == GameState.BallFlight ||
            State == GameState.Resolving || State == GameState.Result ||
            State == GameState.Milestone;

        /// <summary>弹仓组成文本，例如“强化弹珠 ×3 · 标准弹珠 ×3”。</summary>
        public string MagazineSummary
        {
            get
            {
                var counts = MagazineBuilder.CountByBall(_magazine);
                var sb = new StringBuilder();
                foreach (var ball in Config.balls)
                {
                    if (ball == null) continue;
                    counts.TryGetValue(ball.id, out int count);
                    if (count <= 0) continue;
                    if (sb.Length > 0) sb.Append(" · ");
                    sb.Append(ball.displayName).Append(" ×").Append(count);
                }
                return sb.Length > 0 ? sb.ToString() : "标准弹珠 ×1";
            }
        }

        public BallDef HighlightBall
        {
            get
            {
                BallDef best = null;
                foreach (var ball in _magazine)
                {
                    if (ball == null) continue;
                    if (best == null || ball.multiplier > best.multiplier) best = ball;
                }
                return best ?? (Config != null ? Config.GetBall("ball_01") : null);
            }
        }

        private readonly List<BallDef> _magazine = new List<BallDef>();
        private readonly List<MarbleController> _activeMarbles = new List<MarbleController>();
        private readonly HashSet<MarbleController> _resolvedMarbles = new HashSet<MarbleController>();
        private MarbleController[] _marbles;
        private StageTransitionKind _nextStage = StageTransitionKind.None;
        private bool _allHitBonusAwarded;
        private int _missShields;
        private int _pendingSplitBonus;
        private int _emptyHoleHits;

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

            State = GameState.Menu;
            CurrentShotScore = 0;
            CurrentPhaseShots = 0;
            CurrentPhaseScore = 0;
            PhaseMagazineTotal = 0;
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

            RefreshUpgradeEffects();
            RefreshHoleViews();
            Hud = HUDController.Create(this, config, camera, board, tilt, launcher);
            Hud.ShowMenu();
        }

        public void StartGame()
        {
            if (State != GameState.Menu) return;
            State = GameState.Shop;
            Shop.BeginShopVisit();
            _magazine.Clear();
            _activeMarbles.Clear();
            _resolvedMarbles.Clear();
            PhaseMagazineTotal = 0;
            VolleyTotalBalls = 0;
            VolleyResolved = 0;
            VolleyHits = 0;
            _emptyHoleHits = 0;
            _allHitBonusAwarded = false;
            LastMilestoneValue = 0;
            Hud.ShowShop(true);
            Hud.RefreshAll();
        }

        public void CloseShop()
        {
            if (State != GameState.Shop) return;
            Shop.EndShopVisit();

            _magazine.Clear();
            _magazine.AddRange(MagazineBuilder.Build(Config, SaveData));
            if (_magazine.Count == 0)
            {
                var standard = Config.GetBall("ball_01");
                if (standard != null) _magazine.Add(standard);
            }

            // 5 号洞「裂变回响」：上一轮命中后，下一轮额外生成分裂弹。
            if (_pendingSplitBonus > 0)
            {
                var splitStandard = Config.GetBall("ball_01");
                if (splitStandard != null)
                {
                    int add = Mathf.Min(_pendingSplitBonus, _marbles.Length - _magazine.Count);
                    for (int i = 0; i < add; i++) _magazine.Add(splitStandard);
                    Debug.Log($"[Split] 裂变回响追加 {add} 颗分裂弹，下一轮弹仓 {_magazine.Count} 颗");
                }
                _pendingSplitBonus = 0;
            }

            if (_magazine.Count > _marbles.Length)
            {
                Debug.LogWarning($"[Volley] 弹仓 {_magazine.Count} 颗超过对象池 {_marbles.Length}，只取前 {_marbles.Length} 颗。");
                _magazine.RemoveRange(_marbles.Length, _magazine.Count - _marbles.Length);
            }

            PhaseMagazineTotal = _magazine.Count;
            CurrentPhaseShots = 0;
            CurrentPhaseScore = 0;
            CurrentShotScore = 0;
            VolleyTotalBalls = _magazine.Count;
            VolleyResolved = 0;
            VolleyHits = 0;
            _emptyHoleHits = 0;
            Score.ResetCombo();

            State = GameState.StageTransition;
            _nextStage = StageTransitionKind.ToLaunch;
            Debug.Log($"[Phase] 商店 → 发射 | 齐射装填 {_magazine.Count} 颗：{MagazineSummary}");
            Hud.HideShop();
            Hud.ShowStageTransition(
                "SHOP PHASE COMPLETE",
                "发射阶段",
                $"下一里程碑 {MilestoneLabel()}\n已装填 {_magazine.Count} 颗：{MagazineSummary}\n一次拉杆，弹簧连续回弹，全部弹珠快速出膛。",
                "开始齐射",
                Theme.Teal,
                true);
            Hud.RefreshAll();
        }

        public void ContinueStageTransition()
        {
            if (State != GameState.StageTransition) return;
            var kind = _nextStage;
            _nextStage = StageTransitionKind.None;
            Hud.HideStageTransition();

            if (kind == StageTransitionKind.ToLaunch)
            {
                StartVolleyAim();
            }
            else if (kind == StageTransitionKind.ToShop)
            {
                State = GameState.Shop;
                Shop.BeginShopVisit();
                Hud.ShowShop(false);
                Hud.RefreshAll();
            }
        }

        private void StartVolleyAim()
        {
            if (_magazine.Count == 0)
            {
                BeginShopStage();
                return;
            }

            State = GameState.Aim;
            Tilt.SetControl(false, true);
            CurrentShotScore = 0;
            CurrentPhaseScore = 0;
            VolleyTotalBalls = _magazine.Count;
            VolleyResolved = 0;
            VolleyHits = 0;
            _emptyHoleHits = 0;
            _allHitBonusAwarded = false;
            _missShields = UpgradeCatalog.ShieldCount(SaveData.GetHoleSkillLevel("hole_03"));
            Score.ResetCombo();
            _activeMarbles.Clear();
            _resolvedMarbles.Clear();

            float bounceMultiplier = UpgradeCatalog.BounceMultiplier(SaveData.bounceLevel);
            float magnetAcceleration = UpgradeCatalog.MagnetAcceleration(SaveData.magnetLevel);
            float magnetTargetX = HighestValueHoleX();

            for (int i = 0; i < _marbles.Length; i++)
            {
                var marble = _marbles[i];
                if (marble == null) continue;
                if (i < _magazine.Count)
                {
                    marble.SetBall(_magazine[i]);
                    marble.PrepareAt(BoardGeometry.VolleyRackPosition(i, _magazine.Count));
                    marble.ConfigureFlightSkills(bounceMultiplier, magnetAcceleration, magnetTargetX);
                    _activeMarbles.Add(marble);
                }
                else
                {
                    marble.HideImmediate();
                }
            }

            // 弹珠对象刚被激活时再确认一次相互忽略碰撞，确保齐射不会互相卡住。
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
                hole.SetPoints(Shop.HolePoints(hole.Def));
            }
            Hud.HideAllModals();
            Hud.RefreshAll();
            Debug.Log($"[Volley] ready with {_activeMarbles.Count} balls");
        }

        private void OnLaunchRequested(float chargeRatio)
        {
            if (State != GameState.Aim || _activeMarbles.Count == 0) return;

            // 一次拉杆消耗本轮全部付费弹珠，并把整槽弹珠同时/连续弹出。
            var counts = MagazineBuilder.CountByBall(_magazine);
            foreach (var ball in Config.balls)
            {
                if (ball == null || ball.alwaysAvailable) continue;
                if (counts.TryGetValue(ball.id, out int count) && count > 0)
                {
                    SaveData.ConsumeStock(ball.id, count);
                }
            }
            SaveSystem.Save(SaveData);

            State = GameState.BallFlight;
            RoundsPlayed++;
            CurrentPhaseShots = _activeMarbles.Count;
            VolleyResolved = 0;
            VolleyHits = 0;
            _emptyHoleHits = 0;
            Tilt.SetControl(true);

            int n = _activeMarbles.Count;
            int pulses = Mathf.CeilToInt(n / 2f);
            Launcher.Fire(chargeRatio, pulses);
            StartCoroutine(LaunchVolleyBurst(chargeRatio, n));
            Hud.RefreshAll();
            StartCoroutine(MeasureVolley());
            StartCoroutine(VolleyTimeout());
        }

        /// <summary>
        /// 一次拉杆的弹簧连弹：出口常开、不做闸门，弹簧连续回弹，把整槽弹珠快速弹出。
        /// 每颗球仍独立落洞与结算。
        /// </summary>
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
            foreach (var marble in snapshot)
            {
                if (marble == null || _resolvedMarbles.Contains(marble)) continue;
                Debug.Log($"[Volley] unresolved {marble.name} pos={marble.transform.position} vel={marble.Body.linearVelocity}");
                _resolvedMarbles.Add(marble);
                VolleyResolved++;
                marble.HideImmediate();
            }
            Hud.ShowToast("齐射超时，未落洞的弹珠已收回");
            Hud.RefreshAll();
            StartCoroutine(FinishVolley());
        }

        private IEnumerator MeasureVolley()
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

            int points = Shop.HolePoints(hole.Def);
            var result = Score.Score(marble.Def, hole.Def, points);
            CurrentShotScore += result.total;
            CurrentPhaseScore += result.total;
            SaveSystem.Save(SaveData);
            Debug.Log($"[Hole] {hole.Def.id} ball={result.ballMultiplier} hole={result.holeMultiplier} points={result.holePoints} combo={result.comboMultiplier} chain={result.chainMultiplier} fever={result.feverMultiplier} jackpot={result.jackpotBonus} ember={result.emberBonus} bounty={result.bountyCredits} total={result.total} resolved={VolleyResolved}/{VolleyTotalBalls}");

            // 5 号洞「裂变回响」：为下一轮积累分裂弹。
            if (UpgradeCatalog.HoleSkillFor(hole.Def.id) == HoleSkillId.SplitEcho)
            {
                int add = UpgradeCatalog.SplitEchoCount(SaveData.GetHoleSkillLevel(hole.Def.id));
                if (add > _pendingSplitBonus)
                {
                    _pendingSplitBonus = add;
                    Hud.ShowToast($"裂变回响：下一轮 +{add} 颗分裂弹");
                }
            }

            marble.CaptureInto(new Vector2(hole.Def.positionX, BoardGeometry.HoleCenterY));
            Hud.PlayScorePopup(result, new Vector3(hole.Def.positionX, BoardGeometry.HoleCenterY + 0.9f, 0f));
            Hud.RefreshAll();

            if (VolleyResolved >= VolleyTotalBalls)
            {
                AwardAllHitBonusIfPerfect();
                StartCoroutine(FinishVolley());
            }
        }

        /// <summary>空穴：接住弹珠但不结算积分，并中断同洞连击与全局连锁。</summary>
        private void ResolveEmptyHole(RewardHole hole, MarbleController marble)
        {
            _resolvedMarbles.Add(marble);
            VolleyResolved++;
            VolleyHits++;
            _emptyHoleHits++;

            Score.RegisterMiss();
            marble.CaptureInto(new Vector2(hole.Def.positionX, BoardGeometry.HoleCenterY));
            Hud.PlayNoRewardPopup(hole.Def, new Vector3(hole.Def.positionX, BoardGeometry.HoleCenterY + 0.9f, 0f));
            Hud.RefreshAll();
            Debug.Log($"[Hole] {hole.Def.id} 空穴，无奖励，连锁中断 resolved={VolleyResolved}/{VolleyTotalBalls}");

            if (VolleyResolved >= VolleyTotalBalls) StartCoroutine(FinishVolley());
        }

        private void OnMarbleLost(MarbleController marble)
        {
            if (State != GameState.BallFlight || marble == null) return;
            if (!_activeMarbles.Contains(marble) || _resolvedMarbles.Contains(marble)) return;

            _resolvedMarbles.Add(marble);
            VolleyResolved++;
            if (_missShields > 0)
            {
                _missShields--;
                Hud.ShowToast("稳态护盾：抵挡一次断链");
                Debug.Log($"[Shield] 护盾抵挡漏球，剩余护盾 {_missShields}");
            }
            else
            {
                Score.RegisterMiss();
            }
            marble.HideImmediate();
            Hud.RefreshAll();
            Debug.Log($"[Volley] ball lost, chain preserved={_missShields > 0}, resolved={VolleyResolved}/{VolleyTotalBalls}");

            if (VolleyResolved >= VolleyTotalBalls) StartCoroutine(FinishVolley());
        }

        private void AwardAllHitBonusIfPerfect()
        {
            if (_allHitBonusAwarded) return;
            _allHitBonusAwarded = true;
            if (VolleyTotalBalls <= 0 || VolleyHits < VolleyTotalBalls || _emptyHoleHits > 0) return;

            int bonus = ScoreRules.AllHitBonusFor(CurrentPhaseScore);
            if (bonus <= 0) return;

            Score.AwardBonus(bonus);
            CurrentShotScore += bonus;
            CurrentPhaseScore += bonus;
            SaveSystem.Save(SaveData);
            Hud.ShowBonusBanner("全中奖励", bonus, Theme.AmberBright);
            Debug.Log($"[Bonus] 全中 {VolleyHits}/{VolleyTotalBalls}，奖励 +{bonus:N0}，本轮累计 {CurrentPhaseScore:N0}");
            Hud.RefreshAll();
        }

        private IEnumerator FinishVolley()
        {
            if (State != GameState.BallFlight) yield break;
            State = GameState.Resolving;
            Tilt.SetControl(false, true);
            yield return new WaitForSeconds(0.9f);

            Score.RecordVolleyScore(CurrentPhaseScore);
            int reached = AdvanceMilestones();
            SaveSystem.Save(SaveData);

            if (reached > 0)
            {
                LastMilestoneValue = reached;
                State = GameState.Milestone;
                Debug.Log($"[Milestone] 累计得分 {SaveData.totalScore:N0}，突破 {reached:N0}；下一目标 {MilestoneLabel()}");
                Hud.ShowMilestone();
                Hud.RefreshAll();
                yield break;
            }

            BeginShopStage();
        }

        private int AdvanceMilestones()
        {
            int reached = 0;
            while (SaveData.milestoneIndex < ScoreRules.MilestoneCount &&
                   SaveData.totalScore >= ScoreRules.Milestones[SaveData.milestoneIndex])
            {
                reached = ScoreRules.Milestones[SaveData.milestoneIndex];
                SaveData.milestoneIndex++;
            }
            return reached;
        }

        private string MilestoneLabel()
        {
            return NextMilestone > 0 ? $"{NextMilestone:N0}" : "传奇冲分";
        }

        private void BeginShopStage()
        {
            Debug.Log($"[Phase] 发射 → 商店 | 齐射 {VolleyTotalBalls} 颗，命中 {VolleyHits} 颗，本轮得分 {CurrentPhaseScore}，累计 {SaveData.totalScore}");
            Score.ResetCombo();
            State = GameState.StageTransition;
            _nextStage = StageTransitionKind.ToShop;
            Tilt.SetControl(false, true);
            Hud.HideAllModals();
            Hud.ShowStageTransition(
                "ROUND COMPLETE",
                "商店阶段",
                $"本轮 {VolleyTotalBalls} 颗命中 {VolleyHits} 颗，获得 {CurrentPhaseScore:N0} 分。\n最高单轮 {BestVolleyScore:N0} · 下一里程碑 {MilestoneLabel()}\n完成补给后，全部弹珠进入下一轮待发射槽。",
                "进入商店",
                Theme.Amber,
                false);
            Hud.RefreshAll();
        }

        public void ContinueFromMilestone()
        {
            if (State != GameState.Milestone) return;
            Hud.HideMilestone();
            BeginShopStage();
        }

        public void ContinueFromResult()
        {
            if (State != GameState.Result) return;
            BeginShopStage();
        }

        public void HandleUnlockBall(string ballId)
        {
            if (State != GameState.Shop) return;
            var result = Shop.TryUnlockBall(ballId);
            Hud.ShowToast(result.message);
            Hud.RefreshAll();
        }

        public void HandleBuyBall(string ballId)
        {
            if (State != GameState.Shop) return;
            var result = Shop.TryBuyBall(ballId);
            Hud.ShowToast(result.message);
            Hud.RefreshAll();
        }

        public void HandleUpgradeHole(string holeId)
        {
            if (State != GameState.Shop) return;
            var hole = Config.GetHole(holeId);
            var result = Shop.TryUpgradeHole(hole);
            Hud.ShowToast(result.message);
            RefreshHoleViews();
            Hud.RefreshAll();
        }

        public void HandleUpgradeEnchant(EnchantId id)
        {
            if (State != GameState.Shop) return;
            var result = Shop.TryUpgradeEnchant(id);
            Hud.ShowToast(result.message);
            Hud.RefreshAll();
        }

        public void HandleUpgradeBallSkill(BallSkillId id)
        {
            if (State != GameState.Shop) return;
            var result = Shop.TryUpgradeBallSkill(id);
            Hud.ShowToast(result.message);
            Hud.RefreshAll();
        }

        public void HandleUpgradeHoleSkill(string holeId)
        {
            if (State != GameState.Shop) return;
            var result = Shop.TryUpgradeHoleSkill(holeId);
            Hud.ShowToast(result.message);
            Hud.RefreshAll();
        }

        private void OnShopChanged()
        {
            RefreshUpgradeEffects();
            RefreshHoleViews();
        }

        private void RefreshUpgradeEffects()
        {
            if (Tilt != null)
            {
                Tilt.ControlMultiplier = UpgradeCatalog.FrostAuthorityMultiplier(SaveData.frostLevel);
            }
        }

        private void RefreshHoleViews()
        {
            if (Board == null || Board.Holes == null) return;
            foreach (var hole in Board.Holes)
            {
                if (hole == null || hole.Def == null) continue;
                hole.SetPoints(Shop.HolePoints(hole.Def));
            }
        }

        private float HighestValueHoleX()
        {
            HoleDef best = null;
            if (Config != null && Config.holes != null)
            {
                foreach (var hole in Config.holes)
                {
                    if (hole == null) continue;
                    if (best == null || hole.multiplier > best.multiplier) best = hole;
                }
            }
            return best != null ? best.positionX : 0f;
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
            if (State == GameState.Menu)
            {
                StartGame();
                CloseShop();
                ContinueStageTransition();
            }
        }

        public void DebugContinueStage()
        {
            ContinueStageTransition();
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
                SaveData.credits += 500;
                SaveSystem.Save(SaveData);
                Hud.ShowToast("调试：+500 可用积分");
                Hud.RefreshAll();
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                SaveData.totalScore += 5000;
                SaveSystem.Save(SaveData);
                Hud.ShowToast("调试：+5000 累计得分");
                Hud.RefreshAll();
            }
#endif
        }
    }
}
