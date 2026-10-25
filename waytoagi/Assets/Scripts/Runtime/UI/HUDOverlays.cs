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

        private GameObject _victoryRoot;
        private Text _victoryStats;
        private UIButton _victoryContinueButton;

        private GameObject _settingsRoot;
        private Text _settingsSourceText;
        private Text _settingsBestText;
        private Text _settingsCalibrationNote;
        private UIButton _settingsInvertButton;
        private UIButton _settingsBgmButton;
        private UIButton _settingsSfxButton;
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
            UIFactory.Label(root, "Subtitle", "PACHINKO  ·  动态循环版", 27, Theme.Amber, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 380f), new Vector2(900f, 40f), true);
            UIFactory.Label(root, "Tagline", $"无限白球  /  同洞连击  /  {_game.GoalScore:N0} 目标", 20, Theme.Cream.Alpha(0.58f),
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 336f), new Vector2(760f, 32f));

            var firstEnchant = _config.GetEnchant(1);
            var secondEnchant = _config.GetEnchant(2);
            Color red = firstEnchant != null ? Theme.Hex(firstEnchant.color) : Theme.Hex("#E85C45");
            Color blue = secondEnchant != null ? Theme.Hex(secondEnchant.color) : Theme.Hex("#4C86D9");
            CreateMenuBall(root, new Vector2(-160f, 250f), 66f, Theme.Cream, "×1");
            CreateMenuBall(root, new Vector2(0f, 250f), 82f, red, $"×{(firstEnchant != null ? firstEnchant.multiplier : 2)}");
            CreateMenuBall(root, new Vector2(160f, 250f), 66f, blue, $"×{(secondEnchant != null ? secondEnchant.multiplier : 5)}");

            var instructions = UIFactory.Panel(root, "Instructions", UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -70f), new Vector2(880f, 366f), Theme.Hex("#21160F").Alpha(0.78f), 26, true, 8f);
            UIFactory.RoundedRectOnUI(instructions, "Accent", new Vector2(34f, -24f), new Vector2(6f, 312f), Theme.Amber, 3);
            UIFactory.Label(instructions, "Line1",
                $"<color=#E9A93E>① 无限白球</color>：标准弹珠不消耗库存，每次齐射装填 {_game.Shop.CurrentVolleySize} 颗，可在兑换中升级", 20, Theme.Cream,
                TextAnchor.UpperLeft, UIFactory.TopLeft, new Vector2(64f, -24f), new Vector2(790f, 56f));
            UIFactory.Label(instructions, "Line2",
                "<color=#57B4BC>② 动态循环</color>：每次循环的齐射次数不固定，循环越高次数越多", 20, Theme.Cream,
                TextAnchor.UpperLeft, UIFactory.TopLeft, new Vector2(64f, -88f), new Vector2(790f, 56f));
            UIFactory.Label(instructions, "Line3",
                "<color=#E9A93E>③ 倾斜控球</color>：拖拽弹簧一次发射全部弹珠，倾斜手机改变重力方向", 20, Theme.Cream,
                TextAnchor.UpperLeft, UIFactory.TopLeft, new Vector2(64f, -152f), new Vector2(790f, 56f));
            UIFactory.Label(instructions, "Line4",
                "<color=#C84B3D>④ 同洞连击</color>：连续命中同一奖励洞 ×2 / ×4 / ×8；换洞、空穴、漏球归零", 20, Theme.Cream,
                TextAnchor.UpperLeft, UIFactory.TopLeft, new Vector2(64f, -216f), new Vector2(790f, 56f));
            UIFactory.Label(instructions, "Line5",
                $"<color=#E9A93E>⑤ 兑换与通关</color>：每循环结束自动兑换附魔 / 额外弹珠 / 引爆 / 挡板；累计 {_game.GoalScore:N0} 通关", 20, Theme.Cream,
                TextAnchor.UpperLeft, UIFactory.TopLeft, new Vector2(64f, -280f), new Vector2(790f, 56f));

            UIFactory.Label(root, "DesktopHint", "电脑试玩：鼠标拖动推杆  ·  A / D 或 ← / → 倾斜（或按住屏幕左右侧）", 17,
                Theme.Cream.Alpha(0.45f), TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, -316f), new Vector2(900f, 28f));

            _menuSaveText = UIFactory.Label(root, "SaveSummary", "", 20, Theme.AmberBright, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, -372f), new Vector2(860f, 56f));

            _menuStartButton = UIFactory.TextButton(root, "StartButton", "开始游戏", UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -478f), new Vector2(460f, 100f), Theme.Amber, Theme.Ink, 36,
                () => _game.StartGame(), 30);
            UIFactory.AddShadow(_menuStartButton.label, Theme.Hex("#7B4A15").Alpha(0.28f), new Vector2(0f, -3f));

            _menuFooterText = UIFactory.Label(root, "Footer", $"V3.0 · 动态循环 · {_game.GoalScore:N0} 通关目标", 16,
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
            int bestLoop = _game.Score.BestLoopScore;
            int bestVolley = _game.Score.BestVolleyScore;
            int loop = _game.LoopIndex;

            if (total > 0 || credits > 0 || bestLoop > 0)
            {
                _menuSaveText.text =
                    $"第 {loop} 循环  ·  累计 {total:N0} / {_game.GoalScore:N0}  ·  可用 {credits:N0}\n" +
                    $"最高循环 {bestLoop:N0}  ·  最高齐射 {bestVolley:N0}";
            }
            else
            {
                _menuSaveText.text = $"新纪录从第 1 循环开始  ·  通关目标 {_game.GoalScore:N0}";
            }

            if (_menuFooterText != null)
            {
                _menuFooterText.text = _game.GoalReached
                    ? "V3.0 · 已通关 · 无尽模式"
                    : $"V3.0 · 动态循环 · {_game.GoalScore:N0} 通关目标";
            }

            if (_menuStartButton != null)
            {
                _menuStartButton.SetText(_game.GoalReached
                    ? "进入无尽模式"
                    : (total > 0 || credits > 0 || bestLoop > 0 ? "继续挑战" : "开始游戏"));
            }
        }

        private void BuildVictoryPanel()
        {
            var root = UIFactory.Stretch(_modalLayer, "VictoryModal");
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = Theme.Overlay;
            dim.raycastTarget = true;
            _victoryRoot = root.gameObject;

            var glow = UIFactory.Image(root, "VictoryGlow", Theme.Glow, Theme.Amber.Alpha(0.30f),
                UIFactory.Center, UIFactory.Center, new Vector2(0f, 40f), new Vector2(1300f, 1300f));
            glow.raycastTarget = false;

            var card = UIFactory.Panel(root, "VictoryCard", UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(860f, 760f), Theme.Panel, 36, true, 12f);
            UIFactory.Image(card, "TopBand", Theme.Rounded(3), Theme.Amber,
                UIFactory.TopCenter, UIFactory.TopCenter, new Vector2(0f, -14f), new Vector2(240f, 7f));
            UIFactory.Label(card, "Kicker", "GOAL REACHED", 20, Theme.Amber, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 302f), new Vector2(500f, 30f), true);
            UIFactory.Label(card, "Title", $"{_game.GoalScore:N0} 达成！", 66, Theme.AmberBright, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 222f), new Vector2(760f, 100f), true);
            UIFactory.Label(card, "Subtitle", "通关成功 · 无尽模式已解锁", 26, Theme.Cream,
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 150f), new Vector2(740f, 40f));
            UIFactory.Label(card, "Copy",
                $"累计得分达到 {_game.GoalScore:N0}。\n循环会继续变长、阈值继续提高，但累计得分与兑换进度都会保留。",
                19, Theme.Cream.Alpha(0.66f), TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 80f), new Vector2(720f, 70f));

            var statsCard = UIFactory.Panel(card, "StatsCard", UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -58f), new Vector2(680f, 190f), Theme.Ink.Alpha(0.42f), 22, false);
            _victoryStats = UIFactory.Label(statsCard, "Stats", "", 21, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, Vector2.zero, new Vector2(630f, 170f));

            _victoryContinueButton = UIFactory.TextButton(card, "Continue", "继续无尽模式", UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 48f), new Vector2(440f, 84f), Theme.Amber, Theme.Ink, 24,
                () => _game.ContinueFromVictory(), 24);
            _victoryRoot.SetActive(false);
        }

        public void ShowVictory()
        {
            HideAllModals();
            if (_victoryRoot != null)
            {
                _victoryRoot.SetActive(true);
                _victoryRoot.transform.SetAsLastSibling();
                RefreshVictory();
            }
        }

        public void HideVictory()
        {
            if (_victoryRoot != null) _victoryRoot.SetActive(false);
        }

        private void RefreshVictory()
        {
            if (_victoryRoot == null || !_victoryRoot.activeSelf || _victoryStats == null) return;
            _victoryStats.text =
                $"累计得分   {_game.Score.TotalScore:N0}\n" +
                $"当前循环   {_game.LoopIndex}   ·   最高循环   {_game.Score.BestLoopScore:N0}\n" +
                $"最高齐射   {_game.Score.BestVolleyScore:N0}   ·   可用积分   {_game.Score.Credits:N0}";
            if (_victoryContinueButton != null)
            {
                _victoryContinueButton.SetText(_game.Loop.AttemptComplete
                    ? "继续 · 兑换并进入下一循环"
                    : "继续 · 完成本循环剩余齐射");
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
                UIFactory.Center, new Vector2(0f, -20f), new Vector2(700f, 52f));

            _settingsResetButton = UIFactory.TextButton(card, "Reset", "重置游戏", UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -98f), new Vector2(360f, 60f), Theme.Red, Theme.Cream, 21, OnResetClicked, 18);

            UIFactory.Label(card, "DangerNote",
                "重置游戏会清空累计得分、可用积分、循环进度、最高记录、\n附魔 / 额外弹珠 / 引爆 / 挡板兑换和校准设置，等同重新开始游戏。",
                14, Theme.InkSoft, TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, -166f), new Vector2(700f, 50f));

            _settingsBgmButton = UIFactory.TextButton(card, "BgmToggle", "音乐 BGM：开", UIFactory.Center, UIFactory.Center,
                new Vector2(-168f, -242f), new Vector2(300f, 58f), Theme.Teal, Theme.Cream, 19, OnBgmToggleClicked, 18);
            _settingsSfxButton = UIFactory.TextButton(card, "SfxToggle", "音效 SFX：开", UIFactory.Center, UIFactory.Center,
                new Vector2(168f, -242f), new Vector2(300f, 58f), Theme.Teal, Theme.Cream, 19, OnSfxToggleClicked, 18);

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
            if (_settingsBgmButton != null)
            {
                _settingsBgmButton.SetText($"音乐 BGM：{(AudioManager.BgmEnabled ? "开" : "关")}");
            }
            if (_settingsSfxButton != null)
            {
                _settingsSfxButton.SetText($"音效 SFX：{(AudioManager.SfxEnabled ? "开" : "关")}");
            }
            _settingsBestText.text =
                $"第 {_game.LoopIndex} 循环 · 最高循环 {_game.Score.BestLoopScore:N0} · 最高齐射 {_game.Score.BestVolleyScore:N0}\n" +
                $"累计 {_game.Score.TotalScore:N0} / {_game.GoalScore:N0} · 可用积分 {_game.Score.Credits:N0}";
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

        private void OnBgmToggleClicked()
        {
            AudioManager.SetBgmEnabled(!AudioManager.BgmEnabled);
            RefreshSettings();
            ShowToast(AudioManager.BgmEnabled ? "背景音乐已开启" : "背景音乐已关闭");
        }

        private void OnSfxToggleClicked()
        {
            bool enable = !AudioManager.SfxEnabled;
            AudioManager.SetSfxEnabled(enable);
            if (enable) AudioManager.Play("ui_click");
            RefreshSettings();
            ShowToast(enable ? "音效已开启" : "音效已关闭");
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

            string header = result.comboCount > 1
                ? $"{result.hole.displayName}  ·  第 {result.comboCount} 连"
                : $"{result.hole.displayName}  ·  命中";
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

        /// <summary>弹珠落入空穴：只提示无奖励与连击中断，不弹出得分面板。</summary>
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
            UIFactory.Label(root, "Sub", "同洞连击中断", 15, Theme.AmberBright.Alpha(0.7f), TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, -34f), new Vector2(510f, 24f), true);

            StartCoroutine(PopupAnimation(root, group, local));
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
