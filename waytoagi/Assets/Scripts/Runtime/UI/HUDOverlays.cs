using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Pachinko
{
    public partial class HUDController
    {
        private GameObject _menuRoot;
        private Text _menuSaveText;
        private Text _menuFooterText;
        private UIButton _menuStartButton;

        private GameObject _resultRoot;
        private Text _resultTitle;
        private GameObject _resultFormulaGroup;
        private Text _resultFormula;
        private Text _resultCombo;
        private Text _resultTotal;
        private Text _resultMiss;
        private Text _resultStats;
        private Text _resultPhaseInfo;
        private UIButton _resultContinueButton;
        private ScoreResult _lastResult;
        private bool _lastResultHit;
        private string _lastMissMessage;

        private GameObject _milestoneRoot;
        private Text _milestoneKicker;
        private Text _milestoneTitle;
        private Text _milestoneSubtitle;
        private Text _milestoneCopy;
        private Text _milestoneStats;
        private UIButton _milestoneContinueButton;

        private GameObject _settingsRoot;
        private Text _settingsSourceText;
        private Text _settingsBestText;
        private Text _settingsCalibrationNote;
        private UIButton _settingsInvertButton;
        private UIButton _settingsResetButton;

        private void BuildMenu()
        {
            var root = UIFactory.Stretch(_modalLayer, "MenuModal");
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = Theme.VerticalGradient(Theme.Hex("#3A2418"), Theme.Hex("#120C08"));
            bg.type = Image.Type.Simple;
            bg.raycastTarget = true;
            _menuRoot = root.gameObject;

            var glow = UIFactory.Image(root, "TitleGlow", Theme.Glow, Theme.Amber.Alpha(0.16f),
                UIFactory.Center, UIFactory.Center, new Vector2(0f, 400f), new Vector2(1100f, 1100f));
            glow.raycastTarget = false;
            var tealGlow = UIFactory.Image(root, "TealGlow", Theme.Glow, Theme.Teal.Alpha(0.12f),
                UIFactory.Center, UIFactory.Center, new Vector2(-380f, -500f), new Vector2(800f, 800f));
            tealGlow.raycastTarget = false;

            for (int i = 0; i < 9; i++)
            {
                float angle = i / 9f * Mathf.PI * 2f + 0.4f;
                var pos = new Vector2(Mathf.Cos(angle) * 440f, Mathf.Sin(angle) * 760f);
                var dot = UIFactory.Circle(root, "MenuDot" + i,
                    i % 2 == 0 ? Theme.Amber.Alpha(0.10f) : Theme.TealBright.Alpha(0.08f),
                    UIFactory.Center, UIFactory.Center, pos, 14f + (i % 3) * 8f);
                dot.raycastTarget = false;
            }

            var title = UIFactory.Label(root, "Title", "弹珠机", 118, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 480f), new Vector2(900f, 150f), true);
            UIFactory.AddShadow(title, Theme.Hex("#000000").Alpha(0.45f), new Vector2(0f, -6f));
            UIFactory.Label(root, "Subtitle", "PACHINKO  ·  重力感应倾斜版", 27, Theme.Amber, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 380f), new Vector2(900f, 40f), true);
            UIFactory.Label(root, "Tagline", "一拉齐射  /  连锁狂热  /  全中冲分", 20, Theme.Cream.Alpha(0.58f),
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 336f), new Vector2(760f, 32f));

            CreateMenuBall(root, new Vector2(-160f, 250f), 66f, Theme.Cream, "×1");
            CreateMenuBall(root, new Vector2(0f, 250f), 82f, Theme.Hex("#E85C45"), "×2");
            CreateMenuBall(root, new Vector2(160f, 250f), 66f, Theme.Hex("#4C86D9"), "×3");

            var instructions = UIFactory.Panel(root, "Instructions", UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -78f), new Vector2(880f, 386f), Theme.Hex("#21160F").Alpha(0.78f), 26, true, 8f);
            UIFactory.RoundedRectOnUI(instructions, "Accent", new Vector2(34f, -26f), new Vector2(6f, 330f), Theme.Amber, 3);
            UIFactory.Label(instructions, "Line1",
                "<color=#E9A93E>① 商店阶段</color>：弹珠 / 附魔技能 / 奖励洞三条升级线", 21, Theme.Cream,
                TextAnchor.UpperLeft, UIFactory.TopLeft, new Vector2(64f, -28f), new Vector2(780f, 64f));
            UIFactory.Label(instructions, "Line2",
                "<color=#57B4BC>② 发射阶段</color>：商店买的弹珠全部装填进弹仓", 21, Theme.Cream,
                TextAnchor.UpperLeft, UIFactory.TopLeft, new Vector2(64f, -110f), new Vector2(780f, 64f));
            UIFactory.Label(instructions, "Line3",
                "<color=#E9A93E>③</color>  拖拽弹簧蓄力，拉一次发射待发射槽里的全部弹珠", 21, Theme.Cream,
                TextAnchor.UpperLeft, UIFactory.TopLeft, new Vector2(64f, -192f), new Vector2(780f, 64f));
            UIFactory.Label(instructions, "Line4",
                "<color=#C84B3D>④</color>  倾斜控球；同洞连击 ×2/×4/×8，连续命中触发连锁与狂热", 21, Theme.Cream,
                TextAnchor.UpperLeft, UIFactory.TopLeft, new Vector2(64f, -274f), new Vector2(780f, 64f));

            UIFactory.Label(root, "DesktopHint", "电脑试玩：鼠标拖动推杆  ·  A / D 或 ← / → 倾斜（或按住屏幕左右侧）", 17,
                Theme.Cream.Alpha(0.45f), TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, -326f), new Vector2(900f, 28f));

            _menuSaveText = UIFactory.Label(root, "SaveSummary", "", 20, Theme.AmberBright, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, -380f), new Vector2(820f, 32f));

            _menuStartButton = UIFactory.TextButton(root, "StartButton", "开始游戏", UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -485f), new Vector2(460f, 100f), Theme.Amber, Theme.Ink, 36,
                () => _game.StartGame(), 30);
            UIFactory.AddShadow(_menuStartButton.label, Theme.Hex("#7B4A15").Alpha(0.28f), new Vector2(0f, -3f));

            _menuFooterText = UIFactory.Label(root, "Footer", "MVP V2.0 · 无止境冲分 · 连锁 / 狂热 / 全中", 16,
                Theme.Cream.Alpha(0.34f), TextAnchor.MiddleCenter, UIFactory.BottomCenter, new Vector2(0f, 34f), new Vector2(900f, 26f));

            _menuRoot.SetActive(false);
            RefreshMenuSummary();
        }

        private void CreateMenuBall(Transform parent, Vector2 pos, float size, Color color, string subtitle)
        {
            var shadow = UIFactory.Circle(parent, "MenuBallShadow", Theme.Shadow, UIFactory.Center, UIFactory.Center,
                pos + new Vector2(5f, -8f), size + 12f);
            shadow.raycastTarget = false;
            var rim = UIFactory.Circle(parent, "MenuBallRim", Theme.Ink, UIFactory.Center, UIFactory.Center,
                pos, size + 8f);
            rim.raycastTarget = false;
            var ball = UIFactory.Circle(parent, "MenuBall", color, UIFactory.Center, UIFactory.Center, pos, size);
            ball.raycastTarget = false;
            var highlight = UIFactory.Circle(parent, "MenuBallHighlight", Color.white.Alpha(0.52f),
                UIFactory.Center, UIFactory.Center, pos + new Vector2(-size * 0.16f, size * 0.17f), size * 0.16f);
            highlight.raycastTarget = false;
            UIFactory.Label(parent, "MenuBallMult", subtitle, 24, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, pos + new Vector2(0f, -size * 0.84f), new Vector2(90f, 30f), true);
        }

        private void RefreshMenuSummary()
        {
            if (_menuSaveText == null || _game == null) return;
            int total = _game.Score.TotalScore;
            int credits = _game.Score.Credits;
            int bestVolley = _game.Score.BestVolleyScore;
            int next = _game.NextMilestone;

            if (total > 0 || credits > 0 || bestVolley > 0)
            {
                string target = next > 0 ? $"下一目标 {next:N0}" : "传奇冲分";
                _menuSaveText.text =
                    $"累计 {total:N0}  ·  单轮 {bestVolley:N0}  ·  {target}  ·  积分 {credits:N0}";
            }
            else
            {
                _menuSaveText.text = next > 0
                    ? $"新纪录从第一轮开始  ·  下一目标 {next:N0}"
                    : "新纪录从第一轮开始";
            }

            if (_menuFooterText != null)
            {
                _menuFooterText.text = "MVP V2.0 · 无止境冲分 · 连锁 / 狂热 / 全中";
            }

            if (_menuStartButton != null)
            {
                _menuStartButton.SetText(total > 0 || credits > 0 || bestVolley > 0 ? "继续冲分" : "开始游戏");
            }
        }

        private void BuildResultPanel()
        {
            var root = UIFactory.Stretch(_modalLayer, "ResultModal");
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = Theme.Overlay;
            dim.raycastTarget = true;
            _resultRoot = root.gameObject;

            var card = UIFactory.Panel(root, "ResultCard", UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(820f, 700f), Theme.Panel, 34, true, 12f);
            UIFactory.Image(card, "Accent", Theme.Rounded(3), Theme.Amber,
                UIFactory.TopCenter, UIFactory.TopCenter, new Vector2(0f, -14f), new Vector2(120f, 6f));
            _resultTitle = UIFactory.Label(card, "Title", "命中", 40, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 282f), new Vector2(740f, 60f), true);

            var formulaGroup = UIFactory.Panel(card, "FormulaGroup", UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 170f), new Vector2(690f, 96f), Theme.Ink.Alpha(0.55f), 20, false);
            _resultFormulaGroup = formulaGroup.gameObject;
            _resultFormula = UIFactory.Label(formulaGroup, "Formula", "", 32, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, Vector2.zero, new Vector2(650f, 80f), true);
            _resultCombo = UIFactory.Label(card, "Combo", "", 23, Theme.Amber, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 96f), new Vector2(700f, 34f));

            UIFactory.Label(card, "TotalCaption", "本次得分", 18, Theme.Cream.Alpha(0.48f), TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 32f), new Vector2(300f, 24f));
            _resultTotal = UIFactory.Label(card, "Total", "+0", 58, Theme.AmberBright, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, -20f), new Vector2(700f, 74f), true);

            _resultMiss = UIFactory.Label(card, "Miss", "弹珠没有进入奖励洞", 23, Theme.Cream.Alpha(0.7f),
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 120f), new Vector2(700f, 36f));

            UIFactory.Image(card, "Divider", Theme.Rounded(1), Theme.Ink.Alpha(0.25f),
                UIFactory.Center, UIFactory.Center, new Vector2(0f, -80f), new Vector2(640f, 2f)).raycastTarget = false;
            _resultStats = UIFactory.Label(card, "Stats", "", 20, Theme.Cream.Alpha(0.72f), TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, -126f), new Vector2(700f, 60f));
            _resultPhaseInfo = UIFactory.Label(card, "PhaseInfo", "", 16, Theme.Amber.Alpha(0.8f), TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, -180f), new Vector2(700f, 24f));
            _resultContinueButton = UIFactory.TextButton(card, "Continue", "继续发射", UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 36f), new Vector2(430f, 84f), Theme.Amber, Theme.Ink, 24,
                () => _game.ContinueFromResult(), 24);

            _resultRoot.SetActive(false);
        }

        private void RefreshResultPanel(ScoreResult result, bool hit, string missMessage)
        {
            if (_resultTitle == null) return;
            _resultTitle.text = hit && result != null ? $"命中 · {result.hole.displayName}" : "差一点！";
            _resultFormulaGroup.SetActive(hit && result != null);
            _resultMiss.gameObject.SetActive(!hit);
            if (hit && result != null)
            {
                _resultFormula.text = result.Formula;
                _resultCombo.text = result.MultiplierText;
                _resultTotal.text = $"+{result.total:N0}";
                _resultMiss.text = "";
            }
            else
            {
                _resultCombo.text = "";
                _resultTotal.text = "+0";
                _resultMiss.text = string.IsNullOrEmpty(missMessage) ? "弹珠没有进入奖励洞" : missMessage;
            }

            _resultStats.text =
                $"累计得分   {_game.Score.TotalScore:N0}   ·   可用积分   {_game.Score.Credits:N0}\n" +
                $"最高单轮   {_game.Score.BestVolleyScore:N0}   ·   最高单次   {_game.Score.BestSingleScore:N0}";

            int remaining = _game.MagazineRemaining;
            if (_resultPhaseInfo != null)
            {
                _resultPhaseInfo.text = remaining > 0
                    ? $"发射阶段继续 · 弹仓剩余 {remaining} 颗"
                    : "最后一颗已发射 · 下一步返回商店阶段";
            }
            if (_resultContinueButton != null)
            {
                _resultContinueButton.SetText(remaining > 0 ? $"继续发射 · 剩余 {remaining} 颗" : "结束发射 · 进入商店");
            }
        }

        private void BuildMilestonePanel()
        {
            var root = UIFactory.Stretch(_modalLayer, "MilestoneModal");
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = Theme.Overlay;
            dim.raycastTarget = true;
            _milestoneRoot = root.gameObject;

            var glow = UIFactory.Image(root, "MilestoneGlow", Theme.Glow, Theme.Amber.Alpha(0.28f),
                UIFactory.Center, UIFactory.Center, new Vector2(0f, 40f), new Vector2(1200f, 1200f));
            glow.raycastTarget = false;

            var card = UIFactory.Panel(root, "MilestoneCard", UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(840f, 720f), Theme.Panel, 36, true, 12f);
            UIFactory.Image(card, "TopBand", Theme.Rounded(3), Theme.Amber,
                UIFactory.TopCenter, UIFactory.TopCenter, new Vector2(0f, -14f), new Vector2(220f, 7f));
            _milestoneKicker = UIFactory.Label(card, "Kicker", "SCORE MILESTONE", 20, Theme.Amber, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 282f), new Vector2(500f, 30f), true);
            _milestoneTitle = UIFactory.Label(card, "Title", "里程碑达成！", 68, Theme.AmberBright, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 202f), new Vector2(740f, 110f), true);
            _milestoneSubtitle = UIFactory.Label(card, "Subtitle", "", 24, Theme.Cream,
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 130f), new Vector2(740f, 40f));
            _milestoneCopy = UIFactory.Label(card, "Copy", "", 18,
                Theme.Cream.Alpha(0.6f), TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 62f), new Vector2(720f, 70f));

            var statsCard = UIFactory.Panel(card, "StatsCard", UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -60f), new Vector2(660f, 190f), Theme.Ink.Alpha(0.42f), 22, false);
            _milestoneStats = UIFactory.Label(statsCard, "Stats", "", 21, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, Vector2.zero, new Vector2(610f, 170f));

            _milestoneContinueButton = UIFactory.TextButton(card, "Continue", "继续冲分", UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 48f), new Vector2(430f, 84f), Theme.Amber, Theme.Ink, 24,
                () => _game.ContinueFromMilestone(), 24);
            _milestoneRoot.SetActive(false);
        }

        private void RefreshMilestonePanel()
        {
            if (_milestoneStats == null) return;
            bool legendary = _game.AllMilestonesReached;
            if (_milestoneKicker != null)
            {
                _milestoneKicker.text = legendary ? "LEGENDARY SCORE" : "SCORE MILESTONE";
            }
            if (_milestoneTitle != null)
            {
                _milestoneTitle.text = "里程碑达成！";
            }
            if (_milestoneSubtitle != null)
            {
                _milestoneSubtitle.text = $"累计得分突破 {_game.LastMilestoneValue:N0}";
            }
            if (_milestoneCopy != null)
            {
                _milestoneCopy.text = legendary
                    ? "全部里程碑已突破。\n商店、连锁、狂热与全中奖励仍会继续叠加。"
                    : $"下一里程碑 {_game.NextMilestone:N0}。\n回到商店继续强化，连锁与狂热会把分数推得更高。";
            }

            _milestoneStats.text =
                $"本轮得分   {_game.CurrentPhaseScore:N0}   ·   命中   {_game.VolleyHits}/{_game.VolleyTotalBalls}\n" +
                $"累计得分   {_game.Score.TotalScore:N0}   ·   可用积分   {_game.Score.Credits:N0}\n" +
                $"最高单轮   {_game.Score.BestVolleyScore:N0}   ·   最高单次   {_game.Score.BestSingleScore:N0}";
            if (_milestoneContinueButton != null)
            {
                _milestoneContinueButton.SetText(legendary ? "继续刷新纪录" : "继续冲分");
            }
        }

        private void BuildSettingsPanel()
        {
            var root = UIFactory.Stretch(_modalLayer, "SettingsModal");
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = Theme.Overlay;
            dim.raycastTarget = true;
            _settingsRoot = root.gameObject;

            var card = UIFactory.Panel(root, "SettingsCard", UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(780f, 860f), Theme.Panel, 34, true, 12f);
            UIFactory.Label(card, "Title", "设置与校准", 40, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 362f), new Vector2(620f, 56f), true);
            _settingsSourceText = UIFactory.Label(card, "Source", "", 21, Theme.Cream.Alpha(0.7f), TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 312f), new Vector2(660f, 32f));

            var info = UIFactory.Panel(card, "CalibrationInfo", UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 198f), new Vector2(660f, 148f), Theme.Ink.Alpha(0.35f), 20, false);
            UIFactory.Label(info, "InfoTitle", "倾斜校准", 22, Theme.Amber, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(24f, -16f), new Vector2(240f, 30f), true);
            _settingsCalibrationNote = UIFactory.Label(info, "InfoBody",
                "把手机放在你习惯的握持角度，点击“校准水平”，该姿态就会成为新的零点。设备没有加速度计时会自动使用键盘/触屏模拟。",
                17, Theme.Cream.Alpha(0.65f), TextAnchor.UpperLeft, UIFactory.TopLeft, new Vector2(24f, -54f), new Vector2(610f, 82f));

            UIFactory.TextButton(card, "Calibrate", "校准水平", UIFactory.Center, UIFactory.Center,
                new Vector2(-168f, 82f), new Vector2(300f, 68f), Theme.Amber, Theme.Ink, 23, OnCalibrateClicked, 20);
            _settingsInvertButton = UIFactory.TextButton(card, "Invert", "左右反转：关", UIFactory.Center, UIFactory.Center,
                new Vector2(168f, 82f), new Vector2(300f, 68f), Theme.Teal, Theme.Cream, 22, OnInvertClicked, 20);

            UIFactory.Label(card, "DevNote", "左右反转只影响重力感应输入；校准会保存到本地存档。", 16,
                Theme.InkSoft, TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 28f), new Vector2(680f, 26f));

            _settingsBestText = UIFactory.Label(card, "Best", "", 19, Theme.Cream.Alpha(0.65f), TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, -20f), new Vector2(660f, 32f));

            _settingsResetButton = UIFactory.TextButton(card, "Reset", "重置游戏", UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -98f), new Vector2(360f, 60f), Theme.Red, Theme.Cream, 21, OnResetClicked, 18);

            UIFactory.Label(card, "DangerNote",
                "重置游戏会清空积分、累计得分、最高单轮、里程碑进度、弹珠解锁与库存、\n附魔/技能/奖励洞升级和校准设置，等同重新开始游戏。",
                14, Theme.InkSoft, TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, -166f), new Vector2(700f, 50f));

            UIFactory.TextButton(card, "Close", "关闭", UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 46f), new Vector2(300f, 72f), Theme.PanelLight, Theme.Cream, 24, HideSettings, 22);

            _settingsRoot.SetActive(false);
        }

        private void RefreshSettings()
        {
            if (_settingsSourceText == null) return;
            bool sensor = _tilt != null && _tilt.HasSensor;
            _settingsSourceText.text = sensor ? "当前输入：设备重力感应" : "当前输入：键盘 / 触屏模拟（未检测到加速度计）";
            _settingsCalibrationNote.text = sensor
                ? "把手机放在你习惯的握持角度，点击“校准水平”，该姿态就会成为新的零点。建议每次更换握持方式后重新校准。"
                : "电脑或没有加速度计的设备会自动使用键盘 A/D、方向键，或按住屏幕左右半屏来控制倾斜。";
            _settingsInvertButton.SetText($"左右反转：{(_tilt != null && _tilt.Inverted ? "开" : "关")}");
            _settingsBestText.text = $"最高单轮 {_game.Score.BestVolleyScore:N0} · 最高单次 {_game.Score.BestSingleScore:N0} · 累计 {_game.Score.TotalScore:N0}";
            if (!_resetConfirm && _settingsResetButton != null)
            {
                _settingsResetButton.SetText("重置游戏");
            }
        }

        private void OnCalibrateClicked()
        {
            bool ok = _tilt != null && _tilt.CalibrateNow();
            ShowToast(ok ? "已把当前握持姿态设为水平基准" : "设备无加速度计，已切换到键盘 / 触屏模拟");
            RefreshSettings();
        }

        private void OnInvertClicked()
        {
            if (_tilt == null) return;
            bool inverted = _tilt.ToggleInvert();
            ShowToast(inverted ? "倾斜方向已反转" : "倾斜方向已恢复默认");
            RefreshSettings();
        }

        private void OnResetClicked()
        {
            if (!_resetConfirm)
            {
                _resetConfirm = true;
                _settingsResetButton.SetText("再次点击确认清空");
                if (_resetConfirmRoutine != null) StopCoroutine(_resetConfirmRoutine);
                _resetConfirmRoutine = StartCoroutine(ResetConfirmTimeout());
                return;
            }
            if (_resetConfirmRoutine != null) StopCoroutine(_resetConfirmRoutine);
            _resetConfirmRoutine = null;
            _resetConfirm = false;
            _game.ResetSave();
        }

        private IEnumerator ResetConfirmTimeout()
        {
            yield return new WaitForSecondsRealtime(3f);
            _resetConfirm = false;
            _resetConfirmRoutine = null;
            if (_settingsResetButton != null) _settingsResetButton.SetText("重置游戏");
        }

        public void PlayScorePopup(ScoreResult result, Vector3 worldPosition)
        {
            if (result == null || _camera == null || _popupLayer == null) return;
            Vector3 screen = _camera.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_safeRoot, screen, null, out local)) return;
            float halfWidth = _safeRoot.rect.width * 0.5f;
            local.x = Mathf.Clamp(local.x, -halfWidth + 280f, halfWidth - 280f);
            local.y += 24f;

            var root = UIFactory.Rect(_popupLayer, "ScorePopup", UIFactory.Center, UIFactory.Center,
                local, new Vector2(540f, 204f));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            UIFactory.Image(root, "Bg", Theme.Rounded(22), Theme.Ink.Alpha(0.94f), UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -4f), new Vector2(540f, 204f)).raycastTarget = false;
            UIFactory.Image(root, "Edge", Theme.Rounded(2), Theme.Hex(result.hole.color), UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -102f), new Vector2(380f, 4f)).raycastTarget = false;

            string header = result.jackpotBonus > 0
                ? $"{result.hole.displayName}  ·  JACKPOT"
                : $"{result.hole.displayName}  ·  连锁 ×{result.chainMultiplier}";
            UIFactory.Label(root, "Header", header, 19, Theme.Amber,
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 70f), new Vector2(510f, 28f), true);
            UIFactory.Label(root, "Formula", result.Formula, 27, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 34f), new Vector2(510f, 34f), true);
            UIFactory.Label(root, "Multipliers", result.MultiplierText, 14, Theme.AmberBright, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 1f), new Vector2(510f, 38f), true);
            UIFactory.Label(root, "Total", $"+{result.total:N0}", 34, Theme.AmberBright, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, -48f), new Vector2(510f, 42f), true);

            StartCoroutine(PopupAnimation(root, group, local));
        }

        /// <summary>弹珠落入空穴：只提示无奖励与连锁中断，不弹出得分面板。</summary>
        public void PlayNoRewardPopup(HoleDef hole, Vector3 worldPosition)
        {
            if (hole == null || _camera == null || _popupLayer == null) return;
            Vector3 screen = _camera.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_safeRoot, screen, null, out local)) return;
            float halfWidth = _safeRoot.rect.width * 0.5f;
            local.x = Mathf.Clamp(local.x, -halfWidth + 280f, halfWidth - 280f);
            local.y += 24f;

            var root = UIFactory.Rect(_popupLayer, "EmptyHolePopup", UIFactory.Center, UIFactory.Center,
                local, new Vector2(540f, 160f));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            UIFactory.Image(root, "Bg", Theme.Rounded(22), Theme.Ink.Alpha(0.94f), UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -4f), new Vector2(540f, 160f)).raycastTarget = false;
            UIFactory.Image(root, "Edge", Theme.Rounded(2), Theme.Hex(hole.color), UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -80f), new Vector2(380f, 4f)).raycastTarget = false;

            UIFactory.Label(root, "Header", $"{hole.displayName}  ·  EMPTY", 19, Theme.Cream.Alpha(0.65f),
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 44f), new Vector2(510f, 28f), true);
            UIFactory.Label(root, "Message", "空穴 · 无奖励", 28, Theme.Cream.Alpha(0.85f), TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 2f), new Vector2(510f, 36f), true);
            UIFactory.Label(root, "Sub", "连锁中断", 15, Theme.AmberBright.Alpha(0.7f), TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, -34f), new Vector2(510f, 24f), true);

            StartCoroutine(PopupAnimation(root, group, local));
        }

        public void ShowBonusBanner(string title, int amount, Color color)
        {
            if (_popupLayer == null) return;
            var root = UIFactory.Rect(_popupLayer, "BonusBanner", UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 70f), new Vector2(540f, 112f));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            UIFactory.Image(root, "Bg", Theme.Rounded(24), Theme.Ink.Alpha(0.94f), UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -4f), new Vector2(540f, 112f)).raycastTarget = false;
            UIFactory.Image(root, "Edge", Theme.Rounded(3), color, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -56f), new Vector2(320f, 5f)).raycastTarget = false;
            UIFactory.Label(root, "Title", title, 24, color, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 26f), new Vector2(500f, 32f), true);
            UIFactory.Label(root, "Amount", $"+{amount:N0}", 40, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, -20f), new Vector2(500f, 48f), true);

            StartCoroutine(PopupAnimation(root, group, new Vector2(0f, 70f)));
        }

        private IEnumerator PopupAnimation(RectTransform root, CanvasGroup group, Vector2 startPosition)
        {
            float t = 0f;
            float inDuration = 0.16f;
            while (t < inDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / inDuration);
                group.alpha = k;
                root.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, k * k * (3f - 2f * k));
                yield return null;
            }
            group.alpha = 1f;
            root.localScale = Vector3.one;
            float hold = 0.22f;
            t = 0f;
            while (t < hold)
            {
                t += Time.unscaledDeltaTime;
                root.anchoredPosition = startPosition + new Vector2(0f, 24f * (t / hold));
                yield return null;
            }
            float fade = 0.48f;
            t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fade);
                group.alpha = 1f - k;
                root.anchoredPosition = startPosition + new Vector2(0f, 24f + 60f * k);
                yield return null;
            }
            Destroy(root.gameObject);
        }
    }
}
