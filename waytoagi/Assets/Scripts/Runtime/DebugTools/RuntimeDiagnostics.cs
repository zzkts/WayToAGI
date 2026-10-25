using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 自动化验收辅助：带 -autoplay 启动时走完
    /// 菜单 → 发射 → 齐射结算 → 循环兑换 → 下一循环，并截图。
    /// 正常玩家流程不会创建此组件。
    /// </summary>
    public class RuntimeDiagnostics : MonoBehaviour
    {
        public static bool IsAutoplayRequested
        {
            get
            {
                var args = Environment.GetCommandLineArgs();
                foreach (var arg in args)
                {
                    if (string.Equals(arg, "-autoplay", StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }
        }

        private GameManager _game;
        private string _outDir;
        private int _captureIndex;
        private bool _physicalOnly;

        private static bool HasArg(string flag)
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public void Initialize(GameManager game)
        {
            _game = game;
            _physicalOnly = HasArg("-physical-volley");
            _outDir = Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath, "Screenshots");
            Directory.CreateDirectory(_outDir);
            StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            yield return new WaitForSeconds(0.8f);
            yield return Capture("menu");

            _game.StartGame();
            yield return new WaitForSeconds(0.45f);
            yield return Capture("aim_first_volley");

            _game.Hud.DebugShowSettings();
            yield return new WaitForSeconds(0.3f);
            yield return Capture("settings");
            _game.Hud.DebugHideSettings();
            yield return new WaitForSeconds(0.2f);

            yield return PlayVolley(0.72f, 6, "volley_flight", "score_popup");

            // 一循环有多轮齐射：持续打到兑换面板出现。
            int guard = 0;
            while (_game.State == GameState.Aim && guard < 10)
            {
                guard++;
                yield return PlayVolley(0.68f, 6, null, null);
            }

            if (_game.State == GameState.Exchange)
            {
                yield return Capture("loop_exchange");

                // 用 QA 积分展示四项永久兑换全部可用时的状态。
                _game.DebugGrantCredits(200000);
                _game.HandleUnlockEnchant();
                _game.HandleUnlockEnchant();
                _game.HandleUnlockExplosion();
                _game.HandleReduceBaffle();
                _game.Hud.HideToast();
                yield return new WaitForSeconds(0.3f);
                yield return Capture("loop_exchange_purchased");

                _game.DebugContinueStage();
                yield return new WaitForSeconds(0.6f);
                yield return Capture("next_loop_aim");
            }

            yield return new WaitForEndOfFrame();
            Debug.Log($"[Diagnostics] final state={_game.State}, loop={_game.LoopIndex}, threshold={_game.LoopThreshold}, " +
                      $"loopScore={_game.LoopScore}, total={_game.Score.TotalScore}, credits={_game.Score.Credits}, " +
                      $"goal={_game.GoalReached}, enchantTier={_game.Shop.EnchantTier}, explosion={_game.Shop.ExplosionUnlocked}, " +
                      $"baffles={_game.SaveData.baffleCount}/{_game.Board.Baffles.Count}, bestLoop={_game.Score.BestLoopScore}, " +
                      $"bestVolley={_game.Score.BestVolleyScore}, volleys={_game.LoopVolleysDone}/{_game.LoopVolleys}, " +
                      $"volleyBalls={_game.VolleyTotalBalls}, hits={_game.VolleyHits}, resolved={_game.VolleyResolved}");
            yield return new WaitForSeconds(0.4f);
            Application.Quit();
        }

        /// <summary>完成一次齐射并等待进入下一状态。自动化默认把弹珠结算到指定奖励洞。</summary>
        private IEnumerator PlayVolley(float charge, int holeIndex, string flightCapture, string scoreCapture)
        {
            if (_game.State != GameState.Aim) yield break;
            _game.DebugLaunch(charge);
            yield return new WaitForSeconds(0.28f);
            if (!string.IsNullOrEmpty(flightCapture)) yield return Capture(flightCapture);

            // -physical-volley 模式不强制结算，用来确认整槽齐射能靠物理全部落洞。
            if (!_physicalOnly && _game.State == GameState.BallFlight)
            {
                _game.DebugResolveAllActive(holeIndex);
                yield return new WaitForSeconds(0.22f);
                if (!string.IsNullOrEmpty(scoreCapture)) yield return Capture(scoreCapture);
            }

            float timeout = _physicalOnly ? 25f : 6f;
            while (timeout > 0f &&
                   (_game.State == GameState.BallFlight || _game.State == GameState.Resolving))
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (timeout <= 0f)
            {
                Debug.LogWarning($"[Diagnostics] 齐射等待超时，state={_game.State}");
            }
        }

        private IEnumerator Capture(string name)
        {
            _captureIndex++;
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(_outDir, $"{_captureIndex:00}_{name}.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[Diagnostics] screenshot " + path);
            yield return new WaitForSeconds(0.3f);
        }
    }
}
