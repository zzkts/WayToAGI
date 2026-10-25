using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 自动化验收辅助：带 -autoplay 启动时走完
    /// 商店阶段 → 阶段过场 → 弹簧连弹 → 冲分结算 → 商店阶段，并截图。
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
            yield return Capture("shop_initial");

            _game.DebugGrantCredits(6000);
            _game.HandleUnlockBall("ball_02");
            _game.HandleBuyBall("ball_02");
            _game.HandleBuyBall("ball_02");
            _game.HandleBuyBall("ball_02");
            _game.HandleUpgradeHole("hole_07");
            yield return new WaitForSeconds(0.35f);
            yield return Capture("shop_loaded");

            _game.CloseShop();
            yield return new WaitForSeconds(0.4f);
            yield return Capture("stage_launch");
            _game.DebugContinueStage();
            yield return new WaitForSeconds(0.4f);
            yield return Capture("aim_volley_rack");

            _game.Hud.DebugShowSettings();
            yield return new WaitForSeconds(0.3f);
            yield return Capture("settings");
            _game.Hud.DebugHideSettings();
            yield return new WaitForSeconds(0.2f);

            // 一次拉杆，弹簧连续回弹，整槽弹珠快速出膛。
            _game.DebugLaunch(0.72f);
            yield return new WaitForSeconds(0.28f);
            yield return Capture("volley_flight");
            yield return new WaitForSeconds(0.25f);

            // 正常自动化为了稳定跑完阶段，把所有在飞弹珠结算到 7 号洞；
            // -physical-volley 模式不强制结算，用来确认整槽齐射能靠物理全部落洞。
            if (!_physicalOnly && _game.State == GameState.BallFlight)
            {
                _game.DebugResolveAllActive(6);
                yield return new WaitForSeconds(0.22f);
                yield return Capture("score_burst");
            }

            float timeout = _physicalOnly ? 25f : 5f;
            while (timeout > 0f &&
                   _game.State != GameState.Milestone &&
                   _game.State != GameState.StageTransition &&
                   _game.State != GameState.Shop)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (_game.State == GameState.Milestone)
            {
                yield return new WaitForSeconds(0.25f);
                yield return Capture("milestone");
                _game.ContinueFromMilestone();
                yield return new WaitForSeconds(0.4f);
            }

            if (_game.State == GameState.StageTransition)
            {
                yield return Capture("stage_shop");
                _game.DebugContinueStage();
                yield return new WaitForSeconds(0.4f);
            }

            if (_game.State == GameState.Shop)
            {
                yield return Capture("shop_returned");

                // 用返回商店后的 QA 积分展示完整商店升级页签，不改变已经打完的本轮数据。
                _game.DebugGrantCredits(20000);
                _game.HandleUpgradeEnchant(EnchantId.Ember);
                _game.HandleUpgradeEnchant(EnchantId.Frost);
                _game.HandleUpgradeEnchant(EnchantId.Gild);
                _game.HandleUpgradeBallSkill(BallSkillId.Split);
                _game.HandleUpgradeBallSkill(BallSkillId.Bounce);
                _game.HandleUpgradeBallSkill(BallSkillId.Magnet);
                _game.HandleUpgradeHoleSkill("hole_01");
                _game.HandleUpgradeHoleSkill("hole_03");
                _game.HandleUpgradeHoleSkill("hole_05");
                _game.HandleUpgradeHoleSkill("hole_07");
                _game.Hud.HideToast();
                yield return new WaitForSeconds(0.2f);

                _game.Hud.DebugShowShopTab(1);
                yield return new WaitForSeconds(0.2f);
                yield return Capture("shop_enchant_skills");
                _game.Hud.DebugShowShopTab(2);
                yield return new WaitForSeconds(0.2f);
                yield return Capture("shop_hole_skills");
                _game.Hud.DebugShowShopTab(0);
                yield return new WaitForSeconds(0.2f);
                yield return Capture("shop_split_preview");
            }

            yield return new WaitForEndOfFrame();
            Debug.Log($"[Diagnostics] final state={_game.State}, total={_game.Score.TotalScore}, credits={_game.Score.Credits}, bestVolley={_game.Score.BestVolleyScore}, bestSingle={_game.Score.BestSingleScore}, shots={_game.RoundsPlayed}, volley={_game.VolleyTotalBalls}, hits={_game.VolleyHits}, resolved={_game.VolleyResolved}, milestone={_game.NextMilestone}");
            yield return new WaitForSeconds(0.4f);
            Application.Quit();
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
