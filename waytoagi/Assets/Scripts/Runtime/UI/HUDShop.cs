using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pachinko
{
    public partial class HUDController
    {
        private enum ShopTab
        {
            Balls,
            Enhance,
            Holes
        }

        private class BallRowUI
        {
            public BallDef ball;
            public Image background;
            public Image accent;
            public Image dot;
            public Text title;
            public Text description;
            public Text stock;
            public UIButton primary;
        }

        private class HoleRowUI
        {
            public HoleDef hole;
            public Image background;
            public Image accent;
            public Text title;
            public Text subtitle;
            public Text points;
            public UIButton upgrade;
            public Text skillTitle;
            public Text skillDesc;
            public UIButton skillButton;
        }

        private class UpgradeRowUI
        {
            public Image background;
            public Image accent;
            public Text title;
            public Text description;
            public Text effect;
            public UIButton button;
        }

        private class PreviewSlotUI
        {
            public BallDef ball;
            public Image dot;
            public Text count;
        }

        private GameObject _shopRoot;
        private bool _shopFirstTime;
        private ShopTab _activeTab = ShopTab.Balls;
        private GameObject _ballTabRoot;
        private GameObject _enhanceTabRoot;
        private GameObject _holeTabRoot;
        private UIButton _tabBallButton;
        private UIButton _tabEnhanceButton;
        private UIButton _tabHoleButton;
        private Text _shopSubtitleText;
        private Text _shopCreditsText;
        private Text _shopLimitText;
        private Text _shopPreviewTotalText;
        private Text _shopPreviewHint;
        private readonly List<BallRowUI> _ballRows = new List<BallRowUI>();
        private readonly List<HoleRowUI> _holeRows = new List<HoleRowUI>();
        private readonly List<PreviewSlotUI> _previewSlots = new List<PreviewSlotUI>();
        private readonly Dictionary<EnchantId, UpgradeRowUI> _enchantRows = new Dictionary<EnchantId, UpgradeRowUI>();
        private readonly Dictionary<BallSkillId, UpgradeRowUI> _ballSkillRows = new Dictionary<BallSkillId, UpgradeRowUI>();

        private void BuildShop()
        {
            var root = UIFactory.Stretch(_modalLayer, "ShopModal");
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = Theme.Overlay;
            dim.raycastTarget = true;
            _shopRoot = root.gameObject;

            var panel = UIFactory.Panel(root, "ShopPanel", UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(940f, 1320f), Theme.Panel, 34, true, 12f);
            UIFactory.Image(panel, "TopAccent", Theme.Rounded(3), Theme.Amber,
                UIFactory.TopCenter, UIFactory.TopCenter, new Vector2(0f, -14f), new Vector2(120f, 6f)).raycastTarget = false;

            UIFactory.Label(panel, "Kicker", "SHOP PHASE", 15, Theme.Amber, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(46f, -22f), new Vector2(260f, 22f), true);
            UIFactory.Label(panel, "Title", "商店阶段", 38, Theme.Cream, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(44f, -42f), new Vector2(320f, 52f), true);
            UIFactory.Label(panel, "SubTitleInline", "补给 · 附魔 · 技能", 19, Theme.Cream.Alpha(0.55f), TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(294f, -56f), new Vector2(240f, 28f));
            _shopSubtitleText = UIFactory.Label(panel, "Subtitle", "", 17, Theme.Cream.Alpha(0.6f), TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(46f, -94f), new Vector2(600f, 24f));

            var creditsBadge = UIFactory.Panel(panel, "CreditsBadge", UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-26f, -82f), new Vector2(248f, 46f), Theme.Ink.Alpha(0.5f), 16, false);
            _shopCreditsText = UIFactory.Label(creditsBadge, "CreditsText", "可用积分 0", 20, Theme.AmberBright,
                TextAnchor.MiddleCenter, UIFactory.Center, Vector2.zero, new Vector2(230f, 40f), true);

            var close = UIFactory.TextButton(panel, "CloseX", "×", UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-26f, -22f), new Vector2(54f, 54f), Theme.PanelLight, Theme.Cream, 30,
                OnShopCloseClicked, 16, false);
            close.label.rectTransform.anchoredPosition = new Vector2(0f, 1f);

            BuildShopTabs(panel);

            _ballTabRoot = UIFactory.Rect(panel, "BallTab", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(44f, -194f), new Vector2(852f, 820f)).gameObject;
            _enhanceTabRoot = UIFactory.Rect(panel, "EnhanceTab", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(44f, -194f), new Vector2(852f, 820f)).gameObject;
            _holeTabRoot = UIFactory.Rect(panel, "HoleTab", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(44f, -194f), new Vector2(852f, 820f)).gameObject;

            BuildBallTab();
            BuildEnhanceTab();
            BuildHoleTab();

            UIFactory.Label(panel, "PhaseNote",
                "积分 = 得分 ×20% · 升级永久生效 · 附魔 / 技能 / 洞技能三条升级线独立叠加", 14,
                Theme.Cream.Alpha(0.45f), TextAnchor.MiddleCenter, UIFactory.TopCenter,
                new Vector2(0f, -1032f), new Vector2(860f, 24f));

            UIFactory.TextButton(panel, "ShopDone", "完成补给 · 全部装填并进入发射阶段", UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -1090f), new Vector2(580f, 88f), Theme.Amber, Theme.Ink, 24,
                OnShopCloseClicked, 24);

            SetTab(ShopTab.Balls);
            _shopRoot.SetActive(false);
        }

        private void BuildShopTabs(RectTransform panel)
        {
            _tabBallButton = UIFactory.TextButton(panel, "TabBalls", "弹珠补给", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(44f, -126f), new Vector2(272f, 48f), Theme.Amber, Theme.Ink, 20,
                () => SetTab(ShopTab.Balls), 14, false);
            _tabEnhanceButton = UIFactory.TextButton(panel, "TabEnhance", "附魔 · 技能", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(334f, -126f), new Vector2(272f, 48f), Theme.PanelLight, Theme.Cream, 20,
                () => SetTab(ShopTab.Enhance), 14, false);
            _tabHoleButton = UIFactory.TextButton(panel, "TabHoles", "奖励洞", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(624f, -126f), new Vector2(272f, 48f), Theme.PanelLight, Theme.Cream, 20,
                () => SetTab(ShopTab.Holes), 14, false);
        }

        private void SetTab(ShopTab tab)
        {
            _activeTab = tab;
            if (_ballTabRoot != null) _ballTabRoot.SetActive(tab == ShopTab.Balls);
            if (_enhanceTabRoot != null) _enhanceTabRoot.SetActive(tab == ShopTab.Enhance);
            if (_holeTabRoot != null) _holeTabRoot.SetActive(tab == ShopTab.Holes);
            SetTabVisual(_tabBallButton, tab == ShopTab.Balls);
            SetTabVisual(_tabEnhanceButton, tab == ShopTab.Enhance);
            SetTabVisual(_tabHoleButton, tab == ShopTab.Holes);
            RefreshShop();
        }

        private static void SetTabVisual(UIButton button, bool active)
        {
            if (button == null) return;
            button.background.color = active ? Theme.Amber : Theme.PanelLight;
            button.label.color = active ? Theme.Ink : Theme.Cream;
        }

        /// <summary>自动化截图使用，正常 UI 不调用。</summary>
        public void DebugShowShopTab(int index)
        {
            if (_shopRoot == null || !_shopRoot.activeSelf) return;
            SetTab((ShopTab)Mathf.Clamp(index, 0, 2));
        }

        private void CreateShopSectionHeader(Transform parent, string name, string text, float top)
        {
            UIFactory.Image(parent, name + "Bar", Theme.Rounded(2), Theme.Amber,
                UIFactory.TopLeft, UIFactory.TopLeft, new Vector2(0f, -top - 2f), new Vector2(5f, 24f)).raycastTarget = false;
            UIFactory.Label(parent, name, text, 23, Theme.Cream, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(14f, -top), new Vector2(600f, 30f), true);
        }

        private void BuildBallTab()
        {
            CreateShopSectionHeader(_ballTabRoot.transform, "BallHeader", "①  弹珠补给 · 全部装填", 0f);
            _shopLimitText = UIFactory.Label(_ballTabRoot.transform, "Limit", "", 16, Theme.InkSoft, TextAnchor.MiddleRight,
                UIFactory.TopRight, new Vector2(0f, 2f), new Vector2(340f, 28f));

            float rowTop = 44f;
            foreach (var ball in _config.balls)
            {
                _ballRows.Add(BuildBallRow(_ballTabRoot.transform, ball, rowTop));
                rowTop += 102f;
            }

            BuildMagazinePreview(_ballTabRoot.transform, 370f);

            UIFactory.Label(_ballTabRoot.transform, "BallNote",
                "弹珠倍率是基础乘区；分裂技能会在免费弹之后追加分裂标准弹。", 15,
                Theme.Cream.Alpha(0.45f), TextAnchor.MiddleLeft, UIFactory.TopLeft,
                new Vector2(0f, -510f), new Vector2(820f, 24f));

            BuildDesignNote(_ballTabRoot.transform, 548f, "设计目标 · 平滑成长 / 波动体验",
                "平滑成长：洞积分与升级成本渐进，每级提升幅度可控。\n波动体验：连锁 / 狂热 / 全中形成波峰，商店阶段自然回落。\n正交升级：弹珠倍率、附魔、弹珠技能、洞技能各自负责不同层。");
            BuildExperienceCurve(_ballTabRoot.transform, 680f);
        }

        private void BuildExperienceCurve(Transform parent, float top)
        {
            var card = UIFactory.Panel(parent, "ExperienceCurve", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(0f, -top), new Vector2(852f, 140f), Theme.Hex("#21160F").Alpha(0.78f), 18, false);
            UIFactory.Label(card, "Title", "体验曲线 · 商店低谷 → 连锁上升 → 狂热 / 全中峰值", 16, Theme.Amber,
                TextAnchor.MiddleLeft, UIFactory.TopLeft, new Vector2(20f, -8f), new Vector2(800f, 24f), true);
            for (int i = 0; i <= 28; i++)
            {
                float t = i / 28f;
                float x = Mathf.Lerp(-390f, 390f, t);
                float wave = Mathf.Sin(t * Mathf.PI * 2.4f - 0.7f);
                float y = -12f + wave * 22f;
                float peak = Mathf.Clamp01((wave + 1f) * 0.5f);
                Color color = Color.Lerp(Theme.TealBright, Theme.Red, peak);
                var dot = UIFactory.Circle(card, "WaveDot" + i, color, UIFactory.Center, UIFactory.Center,
                    new Vector2(x, y), 8f + peak * 6f);
                dot.raycastTarget = false;
            }
            UIFactory.Label(card, "Caption", "商店阶段回落到波谷；发射后连锁抬升，狂热与全中叠加形成峰值。", 13,
                Theme.Cream.Alpha(0.5f), TextAnchor.MiddleCenter, UIFactory.BottomCenter,
                new Vector2(0f, 10f), new Vector2(820f, 20f));
        }

        private void BuildDesignNote(Transform parent, float top, string title, string body)
        {
            var card = UIFactory.Panel(parent, "DesignNote_" + title, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(0f, -top), new Vector2(852f, 132f), Theme.Hex("#21160F").Alpha(0.78f), 18, false);
            UIFactory.Label(card, "Title", title, 18, Theme.Amber, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(20f, -10f), new Vector2(800f, 26f), true);
            UIFactory.Label(card, "Body", body, 14, Theme.Cream.Alpha(0.55f), TextAnchor.UpperLeft,
                UIFactory.TopLeft, new Vector2(20f, -42f), new Vector2(812f, 80f));
        }

        private void BuildEnhanceTab()
        {
            CreateShopSectionHeader(_enhanceTabRoot.transform, "EnchantHeader", "①  弹珠附魔 · 命中后的元素效果", 0f);
            var ember = BuildUpgradeRow(_enhanceTabRoot.transform, "Ember", "炽焰", Theme.Red, 44f,
                () => _game.HandleUpgradeEnchant(EnchantId.Ember));
            var frost = BuildUpgradeRow(_enhanceTabRoot.transform, "Frost", "霜缚", Theme.TealBright, 156f,
                () => _game.HandleUpgradeEnchant(EnchantId.Frost));
            var gild = BuildUpgradeRow(_enhanceTabRoot.transform, "Gild", "金辉", Theme.Amber, 268f,
                () => _game.HandleUpgradeEnchant(EnchantId.Gild));
            _enchantRows[EnchantId.Ember] = ember;
            _enchantRows[EnchantId.Frost] = frost;
            _enchantRows[EnchantId.Gild] = gild;

            CreateShopSectionHeader(_enhanceTabRoot.transform, "SkillHeader", "②  弹珠技能 · 飞行行为", 390f);
            var split = BuildUpgradeRow(_enhanceTabRoot.transform, "Split", "分裂", Theme.Orange, 432f,
                () => _game.HandleUpgradeBallSkill(BallSkillId.Split));
            var bounce = BuildUpgradeRow(_enhanceTabRoot.transform, "Bounce", "弹跳", Theme.TealBright, 544f,
                () => _game.HandleUpgradeBallSkill(BallSkillId.Bounce));
            var magnet = BuildUpgradeRow(_enhanceTabRoot.transform, "Magnet", "磁引", Theme.AmberBright, 656f,
                () => _game.HandleUpgradeBallSkill(BallSkillId.Magnet));
            _ballSkillRows[BallSkillId.Split] = split;
            _ballSkillRows[BallSkillId.Bounce] = bounce;
            _ballSkillRows[BallSkillId.Magnet] = magnet;

            UIFactory.Label(_enhanceTabRoot.transform, "OrthogonalNote",
                "正交分层：附魔改变命中资源，弹珠技能改变飞行行为，奖励洞技能改变落洞转化。", 14,
                Theme.Cream.Alpha(0.48f), TextAnchor.MiddleCenter, UIFactory.TopCenter,
                new Vector2(0f, -782f), new Vector2(840f, 24f));
        }

        private UpgradeRowUI BuildUpgradeRow(Transform parent, string name, string displayName, Color accentColor,
            float top, Action onClick)
        {
            var row = new UpgradeRowUI();
            var bg = UIFactory.Panel(parent, name + "Row", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(0f, -top), new Vector2(852f, 102f), Theme.PanelLight.Alpha(0.58f), 18, false);
            row.background = bg.GetComponent<Image>();
            row.accent = UIFactory.Image(bg, "Accent", Theme.Rounded(3), accentColor,
                UIFactory.TopLeft, UIFactory.TopLeft, new Vector2(10f, -14f), new Vector2(5f, 74f));
            row.accent.raycastTarget = false;
            UIFactory.Circle(bg, "Dot", accentColor, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(28f, -50f), 28f).raycastTarget = false;

            row.title = UIFactory.Label(bg, "Name", displayName, 20, Theme.Cream, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(62f, -8f), new Vector2(430f, 26f), true);
            row.description = UIFactory.Label(bg, "Desc", "", 14, Theme.Cream.Alpha(0.58f), TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(62f, -36f), new Vector2(560f, 22f));
            row.effect = UIFactory.Label(bg, "Effect", "", 13, Theme.Amber, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(62f, -60f), new Vector2(560f, 22f));
            row.button = UIFactory.TextButton(bg, "Upgrade", "升级", UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-16f, -30f), new Vector2(190f, 42f), Theme.Amber, Theme.Ink, 16,
                onClick, 14);
            return row;
        }

        private void BuildHoleTab()
        {
            var rewardHoles = _config.GetRewardHoles();
            CreateShopSectionHeader(_holeTabRoot.transform, "HoleHeader",
                $"奖励洞强化与技能 · {rewardHoles.Length} 洞（另有 {_config.holes.Length - rewardHoles.Length} 个空穴）", 0f);
            for (int i = 0; i < rewardHoles.Length; i++)
            {
                float top = 44f + (i / 2) * 196f;
                float left = i % 2 == 0 ? 0f : 432f;
                _holeRows.Add(BuildHoleCard(_holeTabRoot.transform, rewardHoles[i], top, left));
            }

            UIFactory.Label(_holeTabRoot.transform, "HoleNote",
                "积分升级提高基础分；洞技能提供积分、护盾、分裂与头奖等转化，不重复基础乘区。空穴不参与升级。", 14,
                Theme.Cream.Alpha(0.45f), TextAnchor.MiddleCenter, UIFactory.TopCenter,
                new Vector2(0f, -650f), new Vector2(840f, 24f));

            BuildDesignNote(_holeTabRoot.transform, 682f, "成长曲线 · 缓慢抬升",
                "洞积分：每级 +25~+150 基础分，形成稳定的基础增长。\n洞技能：把命中转化为积分、护盾、分裂与头奖，制造波峰。\n空穴与奖励洞 1:1 交替排列，落空穴无分并中断连锁。");
        }

        private BallRowUI BuildBallRow(Transform parent, BallDef ball, float top)
        {
            var row = new BallRowUI { ball = ball };
            var bg = UIFactory.Panel(parent, "BallRow_" + ball.id, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(0f, -top), new Vector2(852f, 92f), Theme.PanelLight.Alpha(0.58f), 18, false);
            row.background = bg.GetComponent<Image>();
            row.accent = UIFactory.Image(bg, "Accent", Theme.Rounded(3), Theme.BallColor(ball),
                UIFactory.TopLeft, UIFactory.TopLeft, new Vector2(9f, -14f), new Vector2(5f, 64f));
            row.accent.raycastTarget = false;

            row.dot = UIFactory.Circle(bg, "Dot", Theme.BallColor(ball), UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(22f, -25f), 40f);
            row.dot.raycastTarget = false;
            UIFactory.Circle(bg, "DotRim", Theme.Ink.Alpha(0.28f), UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(19f, -22f), 46f).transform.SetAsFirstSibling();

            row.title = UIFactory.Label(bg, "Name", $"{ball.displayName}   ×{ball.multiplier}", 22, Theme.Cream,
                TextAnchor.MiddleLeft, UIFactory.TopLeft, new Vector2(78f, -8f), new Vector2(300f, 28f), true);
            row.description = UIFactory.Label(bg, "Desc", ball.description, 15, Theme.Cream.Alpha(0.5f),
                TextAnchor.MiddleLeft, UIFactory.TopLeft, new Vector2(78f, -36f), new Vector2(390f, 22f));
            row.stock = UIFactory.Label(bg, "Stock", "", 15, Theme.Amber, TextAnchor.MiddleRight,
                UIFactory.TopRight, new Vector2(-22f, -10f), new Vector2(330f, 22f));

            string id = ball.id;
            row.primary = UIFactory.TextButton(bg, "Primary", "购买", UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-22f, -46f), new Vector2(170f, 38f), Theme.Amber, Theme.Ink, 18,
                () => OnBallPrimaryClicked(id), 14);
            return row;
        }

        private void BuildMagazinePreview(Transform parent, float top)
        {
            var preview = UIFactory.Panel(parent, "MagazinePreview", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(0f, -top), new Vector2(852f, 116f), Theme.Hex("#2C1D14").Alpha(0.92f), 20, false);
            UIFactory.Label(preview, "PreviewTitle", "下一发射阶段 · 全部装填", 19, Theme.Amber, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(22f, -10f), new Vector2(420f, 26f), true);
            _shopPreviewHint = UIFactory.Label(preview, "PreviewHint", "完成补给后，下面这些弹珠会一次性进入弹仓", 14,
                Theme.Cream.Alpha(0.48f), TextAnchor.MiddleLeft, UIFactory.TopLeft, new Vector2(22f, -34f), new Vector2(560f, 20f));
            _shopPreviewTotalText = UIFactory.Label(preview, "PreviewTotal", "共 0 颗", 17, Theme.Cream,
                TextAnchor.MiddleRight, UIFactory.TopRight, new Vector2(-22f, -12f), new Vector2(220f, 26f), true);

            for (int i = 0; i < _config.balls.Length; i++)
            {
                var ball = _config.balls[i];
                float x = 22f + i * 272f;
                var dot = UIFactory.Circle(preview, "PreviewDot" + i, Theme.BallColor(ball), UIFactory.TopLeft, UIFactory.TopLeft,
                    new Vector2(x, -75f), 18f);
                dot.raycastTarget = false;
                UIFactory.Label(preview, "PreviewName" + i, ball.displayName, 15, Theme.Cream.Alpha(0.65f),
                    TextAnchor.MiddleLeft, UIFactory.TopLeft, new Vector2(x + 26f, -67f), new Vector2(120f, 22f));
                var count = UIFactory.Label(preview, "PreviewCount" + i, "×0", 18, Theme.Cream, TextAnchor.MiddleLeft,
                    UIFactory.TopLeft, new Vector2(x + 26f, -87f), new Vector2(150f, 22f), true);
                _previewSlots.Add(new PreviewSlotUI { ball = ball, dot = dot, count = count });
            }
        }

        private void OnBallPrimaryClicked(string ballId)
        {
            if (_game.Shop.IsUnlocked(ballId))
            {
                _game.HandleBuyBall(ballId);
            }
            else
            {
                _game.HandleUnlockBall(ballId);
            }
        }

        private HoleRowUI BuildHoleCard(Transform parent, HoleDef hole, float top, float left)
        {
            var row = new HoleRowUI { hole = hole };
            var bg = UIFactory.Panel(parent, "Hole_" + hole.id, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(left, -top), new Vector2(420f, 184f), Theme.PanelLight.Alpha(0.52f), 18, false);
            row.background = bg.GetComponent<Image>();
            row.accent = UIFactory.Image(bg, "Accent", Theme.Rounded(3), Theme.Hex(hole.color),
                UIFactory.TopLeft, UIFactory.TopLeft, new Vector2(9f, -14f), new Vector2(5f, 154f));
            row.accent.raycastTarget = false;

            row.title = UIFactory.Label(bg, "Name", $"{hole.displayName}   ×{hole.multiplier}", 19, Theme.Cream,
                TextAnchor.MiddleLeft, UIFactory.TopLeft, new Vector2(22f, -8f), new Vector2(260f, 26f), true);
            row.subtitle = UIFactory.Label(bg, "Level", "", 13, Theme.Cream.Alpha(0.5f),
                TextAnchor.MiddleLeft, UIFactory.TopLeft, new Vector2(22f, -30f), new Vector2(250f, 20f));
            row.points = UIFactory.Label(bg, "Points", "", 14, Theme.Amber, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(22f, -50f), new Vector2(260f, 20f));

            string id = hole.id;
            row.upgrade = UIFactory.TextButton(bg, "Upgrade", "升级", UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-14f, -24f), new Vector2(138f, 38f), Theme.Amber, Theme.Ink, 15,
                () => _game.HandleUpgradeHole(id), 14);

            UIFactory.Image(bg, "Divider", Theme.Rounded(1), Theme.Ink.Alpha(0.22f),
                UIFactory.TopLeft, UIFactory.TopLeft, new Vector2(22f, -92f), new Vector2(376f, 1f)).raycastTarget = false;

            row.skillTitle = UIFactory.Label(bg, "SkillName", "", 15, Theme.AmberBright, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(22f, -102f), new Vector2(250f, 22f), true);
            row.skillDesc = UIFactory.Label(bg, "SkillDesc", "", 12, Theme.Cream.Alpha(0.5f), TextAnchor.UpperLeft,
                UIFactory.TopLeft, new Vector2(22f, -126f), new Vector2(250f, 44f));
            row.skillButton = UIFactory.TextButton(bg, "SkillUpgrade", "技能", UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-14f, -126f), new Vector2(138f, 38f), Theme.Teal, Theme.Cream, 14,
                () => _game.HandleUpgradeHoleSkill(id), 14);
            return row;
        }

        private void OnShopCloseClicked()
        {
            if (_game != null && _game.State == GameState.Shop)
            {
                _game.CloseShop();
            }
        }

        private void RefreshShop()
        {
            if (_shopRoot == null || !_shopRoot.activeSelf || _shopCreditsText == null) return;
            _shopCreditsText.text = $"可用积分  {_game.Score.Credits:N0}";
            int max = _game.Shop.MaxPurchasePerVisit;
            int purchased = _game.Shop.ShopVisitPurchaseCount;
            _shopLimitText.text = $"本次购买 {purchased} / {max} 颗";
            string target = _game.NextMilestone > 0 ? $"下一里程碑 {_game.NextMilestone:N0}" : "传奇冲分";
            string pending = _game.PendingSplitBonus > 0 ? $" · 裂变 +{_game.PendingSplitBonus} 待装填" : "";
            _shopSubtitleText.text = $"{target} · {(_shopFirstTime ? "补给后进入发射阶段" : "完成补给后再次发射")}{pending}";

            SetTabVisual(_tabBallButton, _activeTab == ShopTab.Balls);
            SetTabVisual(_tabEnhanceButton, _activeTab == ShopTab.Enhance);
            SetTabVisual(_tabHoleButton, _activeTab == ShopTab.Holes);

            RefreshBallRows(max, purchased);
            RefreshMagazinePreview();
            RefreshEnchantRows();
            RefreshBallSkillRows();
            RefreshHoleRows();
        }

        private void RefreshBallRows(int max, int purchased)
        {
            int credits = _game.Score.Credits;
            foreach (var row in _ballRows)
            {
                var ball = row.ball;
                bool unlocked = ball.alwaysAvailable || _game.Shop.IsUnlocked(ball.id);
                int stock = ball.alwaysAvailable ? 0 : _game.SaveData.GetStock(ball.id);
                int remaining = max - purchased;

                row.dot.color = Theme.BallColor(ball);
                row.accent.color = Theme.BallColor(ball);
                bool included = ball.alwaysAvailable || stock > 0;
                row.background.color = included
                    ? Color.Lerp(Theme.PanelLight, Theme.BallColor(ball), 0.10f).Alpha(0.64f)
                    : Theme.PanelLight.Alpha(0.5f);

                if (ball.alwaysAvailable)
                {
                    row.stock.text = $"每阶段免费 +{_config.economy.freeStandardBallsPerPhase} 颗";
                    row.primary.SetActive(false);
                }
                else if (!unlocked)
                {
                    row.stock.text = "未解锁";
                    row.primary.SetActive(true);
                    row.primary.SetText($"解锁 {ball.unlockPrice:N0}");
                    row.primary.SetInteractable(credits >= ball.unlockPrice);
                }
                else
                {
                    row.stock.text = stock > 0 ? $"库存 {stock} 颗 · 全部装填" : "库存 0 颗";
                    row.primary.SetActive(true);
                    if (remaining <= 0)
                    {
                        row.primary.SetText("本次已满");
                        row.primary.SetInteractable(false);
                    }
                    else
                    {
                        row.primary.SetText($"购买 {ball.unitPrice:N0}");
                        row.primary.SetInteractable(credits >= ball.unitPrice);
                    }
                }
            }
        }

        private void RefreshMagazinePreview()
        {
            var planned = MagazineBuilder.Build(_config, _game.SaveData);
            var counts = MagazineBuilder.CountByBall(planned);
            foreach (var slot in _previewSlots)
            {
                counts.TryGetValue(slot.ball.id, out int count);
                slot.count.text = $"×{count}";
                slot.count.color = count > 0 ? Theme.Cream : Theme.Cream.Alpha(0.3f);
                slot.dot.color = count > 0 ? Theme.BallColor(slot.ball) : Theme.InkSoft.Alpha(0.35f);
            }
            _shopPreviewTotalText.text = $"共 {planned.Count} 颗";
            int split = _game.SaveData.splitLevel;
            _shopPreviewHint.text = split > 0
                ? $"含分裂技能 +{split} 颗标准弹；购买弹珠全部装填"
                : "完成补给后，下面这些弹珠会一次性进入弹仓";
        }

        private void RefreshEnchantRows()
        {
            int credits = _game.Score.Credits;
            foreach (var pair in _enchantRows)
            {
                var id = pair.Key;
                var row = pair.Value;
                int level = _game.Shop.EnchantLevel(id);
                bool maxed = _game.Shop.IsEnchantMaxed(id);
                int price = _game.Shop.EnchantUpgradePrice(id);
                row.title.text = $"{UpgradeCatalog.EnchantName(id)}   Lv.{level}/{UpgradeCatalog.MaxEnchantLevel}";
                row.description.text = UpgradeCatalog.EnchantDescription(id, Mathf.Max(1, level));
                row.effect.text = maxed
                    ? "已满级"
                    : $"下一级：{UpgradeCatalog.EnchantDescription(id, level + 1)}";
                row.button.SetText(maxed ? "已满级" : $"升级 {price:N0}");
                row.button.SetInteractable(!maxed && credits >= price);
            }
        }

        private void RefreshBallSkillRows()
        {
            int credits = _game.Score.Credits;
            foreach (var pair in _ballSkillRows)
            {
                var id = pair.Key;
                var row = pair.Value;
                int level = _game.Shop.BallSkillLevel(id);
                bool maxed = _game.Shop.IsBallSkillMaxed(id);
                int price = _game.Shop.BallSkillUpgradePrice(id);
                row.title.text = $"{UpgradeCatalog.BallSkillName(id)}   Lv.{level}/{UpgradeCatalog.MaxBallSkillLevel}";
                row.description.text = UpgradeCatalog.BallSkillDescription(id, Mathf.Max(1, level));
                row.effect.text = maxed
                    ? "已满级"
                    : $"下一级：{UpgradeCatalog.BallSkillDescription(id, level + 1)}";
                row.button.SetText(maxed ? "已满级" : $"升级 {price:N0}");
                row.button.SetInteractable(!maxed && credits >= price);
            }
        }

        private void RefreshHoleRows()
        {
            int credits = _game.Score.Credits;
            foreach (var row in _holeRows)
            {
                var hole = row.hole;
                int level = _game.Shop.HoleLevel(hole.id);
                int points = _game.Shop.HolePoints(hole);
                bool maxed = _game.Shop.IsHoleMaxed(hole);
                int price = _game.Shop.UpgradePrice(hole);

                row.accent.color = Theme.Hex(hole.color);
                row.subtitle.text = $"Lv.{level} / {hole.maxUpgradeLevel} · 每级 +{hole.upgradePointsPerLevel}";
                if (maxed)
                {
                    row.points.text = $"{points} 积分 · 已满级";
                    row.upgrade.SetText("已满级");
                    row.upgrade.SetInteractable(false);
                }
                else
                {
                    row.points.text = $"{points} → {hole.PointsAtLevel(level + 1)} 积分";
                    row.upgrade.SetText($"升级 {price:N0}");
                    row.upgrade.SetInteractable(credits >= price);
                }

                var skill = UpgradeCatalog.HoleSkillFor(hole.id);
                if (skill == HoleSkillId.None)
                {
                    row.skillTitle.text = "无技能";
                    row.skillDesc.text = "";
                    row.skillButton.SetActive(false);
                    continue;
                }

                int skillLevel = _game.Shop.HoleSkillLevel(hole.id);
                bool skillMaxed = _game.Shop.IsHoleSkillMaxed(hole.id);
                int skillPrice = _game.Shop.HoleSkillUpgradePrice(hole.id);
                row.skillTitle.text = $"{UpgradeCatalog.HoleSkillName(skill)}  Lv.{skillLevel}/{UpgradeCatalog.MaxHoleSkillLevel}";
                row.skillDesc.text = UpgradeCatalog.HoleSkillDescription(skill, Mathf.Max(1, skillLevel));
                row.skillButton.SetActive(true);
                row.skillButton.SetText(skillMaxed ? "已满级" : $"技能 {skillPrice:N0}");
                row.skillButton.SetInteractable(!skillMaxed && credits >= skillPrice);
            }
        }
    }
}
