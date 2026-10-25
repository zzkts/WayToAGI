using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pachinko
{
    public partial class HUDController
    {
        private class ExchangeRowUI
        {
            public Image background;
            public Image accent;
            public Image icon;
            public Text title;
            public Text description;
            public Text effect;
            public UIButton button;
        }

        private GameObject _shopRoot;
        private bool _shopFirstTime;
        private bool _shopPassed;
        private Text _shopSubtitleText;
        private Text _shopCreditsText;
        private Text _shopStatusTitle;
        private Text _shopStatusBody;
        private Text _shopFooterText;
        private UIButton _shopDoneButton;
        private ExchangeRowUI _enchantRow;
        private ExchangeRowUI _marbleRow;
        private ExchangeRowUI _explosionRow;
        private ExchangeRowUI _baffleRow;

        /// <summary>循环结束后的自动兑换面板。</summary>
        private void BuildExchange()
        {
            var root = UIFactory.Stretch(_modalLayer, "ExchangeModal");
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = Theme.Overlay;
            dim.raycastTarget = true;
            _shopRoot = root.gameObject;

            var panel = UIFactory.Panel(root, "ExchangePanel", UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(940f, 1240f), Theme.Panel, 34, true, 12f);
            UIFactory.Image(panel, "TopAccent", Theme.Rounded(3), Theme.Amber,
                UIFactory.TopCenter, UIFactory.TopCenter, new Vector2(0f, -14f), new Vector2(120f, 6f)).raycastTarget = false;

            UIFactory.Label(panel, "Kicker", "EXCHANGE", 15, Theme.Amber, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(46f, -22f), new Vector2(260f, 22f), true);
            UIFactory.Label(panel, "Title", "循环结算与兑换", 38, Theme.Cream, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(44f, -42f), new Vector2(430f, 52f), true);
            _shopSubtitleText = UIFactory.Label(panel, "Subtitle", "", 17, Theme.Cream.Alpha(0.6f), TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(46f, -94f), new Vector2(600f, 24f));

            var creditsBadge = UIFactory.Panel(panel, "CreditsBadge", UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-26f, -82f), new Vector2(248f, 46f), Theme.Ink.Alpha(0.5f), 16, false);
            _shopCreditsText = UIFactory.Label(creditsBadge, "CreditsText", "可用积分 0", 20, Theme.AmberBright,
                TextAnchor.MiddleCenter, UIFactory.Center, Vector2.zero, new Vector2(230f, 40f), true);

            var close = UIFactory.TextButton(panel, "CloseX", "×", UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-26f, -22f), new Vector2(54f, 54f), Theme.PanelLight, Theme.Cream, 30,
                OnExchangeCloseClicked, 16, false);
            close.label.rectTransform.anchoredPosition = new Vector2(0f, 1f);

            var status = UIFactory.Panel(panel, "LoopStatus", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(44f, -146f), new Vector2(852f, 164f), Theme.Ink.Alpha(0.45f), 22, false);
            _shopStatusTitle = UIFactory.Label(status, "StatusTitle", "", 30, Theme.Cream, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(28f, -16f), new Vector2(700f, 40f), true);
            _shopStatusBody = UIFactory.Label(status, "StatusBody", "", 18, Theme.Cream.Alpha(0.72f), TextAnchor.UpperLeft,
                UIFactory.TopLeft, new Vector2(28f, -66f), new Vector2(790f, 82f));

            CreateSectionHeader(panel, "ExchangeHeader", "永久兑换 · 只消耗可用积分", 336f);
            _enchantRow = BuildExchangeRow(panel, "Enchant", 372f, () => _game.HandleUnlockEnchant());
            _marbleRow = BuildExchangeRow(panel, "ExtraMarble", 522f, () => _game.HandleBuyExtraMarble());
            _explosionRow = BuildExchangeRow(panel, "Explosion", 672f, () => _game.HandleUnlockExplosion());
            _baffleRow = BuildExchangeRow(panel, "Baffle", 822f, () => _game.HandleReduceBaffle());

            _shopFooterText = UIFactory.Label(panel, "FooterNote",
                "得分按 1:1 同时计入累计得分与可用积分；兑换只扣可用积分，累计得分与通关进度永不减少。",
                15, Theme.Cream.Alpha(0.46f), TextAnchor.MiddleCenter, UIFactory.TopCenter,
                new Vector2(0f, -990f), new Vector2(860f, 24f));

            _shopDoneButton = UIFactory.TextButton(panel, "Done", "完成兑换", UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -1090f), new Vector2(600f, 88f), Theme.Amber, Theme.Ink, 24,
                OnExchangeCloseClicked, 24);
            _shopRoot.SetActive(false);
        }

        private void CreateSectionHeader(Transform parent, string name, string text, float top)
        {
            UIFactory.Image(parent, name + "Bar", Theme.Rounded(2), Theme.Amber,
                UIFactory.TopLeft, UIFactory.TopLeft, new Vector2(44f, -top), new Vector2(5f, 26f)).raycastTarget = false;
            UIFactory.Label(parent, name, text, 22, Theme.Cream, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(60f, -top - 2f), new Vector2(700f, 30f), true);
        }

        private ExchangeRowUI BuildExchangeRow(Transform parent, string name, float top, Action onClick)
        {
            var row = new ExchangeRowUI();
            var bg = UIFactory.Panel(parent, name + "Row", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(44f, -top), new Vector2(852f, 148f), Theme.PanelLight.Alpha(0.58f), 18, false);
            row.background = bg.GetComponent<Image>();
            row.accent = UIFactory.Image(bg, "Accent", Theme.Rounded(3), Theme.Amber,
                UIFactory.TopLeft, UIFactory.TopLeft, new Vector2(10f, -14f), new Vector2(5f, 120f));
            row.accent.raycastTarget = false;
            row.icon = UIFactory.Circle(bg, "Icon", Theme.Amber, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(38f, -48f), 38f);
            row.icon.raycastTarget = false;

            row.title = UIFactory.Label(bg, "Name", "", 23, Theme.Cream, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(76f, -10f), new Vector2(440f, 30f), true);
            row.description = UIFactory.Label(bg, "Desc", "", 15, Theme.Cream.Alpha(0.58f), TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(76f, -42f), new Vector2(540f, 22f));
            row.effect = UIFactory.Label(bg, "Effect", "", 14, Theme.Amber, TextAnchor.UpperLeft,
                UIFactory.TopLeft, new Vector2(76f, -70f), new Vector2(540f, 62f));

            row.button = UIFactory.TextButton(bg, "Exchange", "兑换", UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-18f, -50f), new Vector2(230f, 48f), Theme.Amber, Theme.Ink, 17,
                onClick, 15);
            return row;
        }

        public void ShowExchange(bool firstTime, bool passed)
        {
            HideAllModals();
            _shopFirstTime = firstTime;
            _shopPassed = passed;
            if (_shopRoot != null) _shopRoot.SetActive(true);
            _shopRoot.transform.SetAsLastSibling();
            RefreshExchange();
            RefreshAll();
        }

        public void HideExchange()
        {
            if (_shopRoot != null) _shopRoot.SetActive(false);
        }

        private void OnExchangeCloseClicked()
        {
            if (_game != null && _game.State == GameState.Exchange)
            {
                _game.CloseExchange();
            }
        }

        private void RefreshExchange()
        {
            if (_shopRoot == null || !_shopRoot.activeSelf || _shopCreditsText == null) return;

            _shopCreditsText.text = $"可用积分  {_game.Score.Credits:N0}";
            _shopSubtitleText.text = "每循环结束自动开启 · 兑换不会扣除累计得分";

            bool goal = _game.GoalReached;
            if (_shopPassed)
            {
                _shopStatusTitle.text = goal ? $"累计 {_game.GoalScore:N0} 达成 · 无尽模式" : $"第 {_game.LoopIndex} 循环完成";
                _shopStatusTitle.color = Theme.TealBright;
                _shopStatusBody.text =
                    $"本循环得分 {_game.LoopScore:N0} / 目标 {_game.LoopThreshold:N0}，完成 {_game.LoopVolleysDone}/{_game.LoopVolleys} 次齐射。\n" +
                    (goal
                        ? "通关目标已达成，可继续冲击更高循环；完成兑换后进入下一循环。"
                        : $"完成兑换后进入第 {_game.LoopIndex + 1} 循环，目标将变为 {_game.Loop.ThresholdFor(_game.LoopIndex + 1):N0}。");
            }
            else
            {
                _shopStatusTitle.text = "本循环未达标";
                _shopStatusTitle.color = Theme.Red;
                int shortfall = Mathf.Max(0, _game.LoopThreshold - _game.LoopScore);
                _shopStatusBody.text =
                    $"本循环得分 {_game.LoopScore:N0} / 目标 {_game.LoopThreshold:N0}，还差 {shortfall:N0}。\n" +
                    $"完成兑换后重试第 {_game.LoopIndex} 循环；累计得分 {_game.Score.TotalScore:N0} 不会减少。";
            }

            _shopDoneButton.SetText(_shopPassed
                ? (goal ? "完成兑换 · 继续无尽模式" : "完成兑换 · 进入下一循环")
                : "完成兑换 · 重试本循环");
            _shopDoneButton.SetInteractable(true);
            _shopFooterText.text = goal
                ? "无尽模式：循环继续变长，阈值继续提高，累计得分与最高循环记录持续保留。"
                : "得分按 1:1 同时计入累计得分与可用积分；兑换只扣可用积分，累计得分与通关进度永不减少。";

            RefreshEnchantRow();
            RefreshMarbleRow();
            RefreshExplosionRow();
            RefreshBaffleRow();
        }

        private void RefreshEnchantRow()
        {
            var shop = _game.Shop;
            var active = shop.ActiveEnchant;
            var next = shop.NextEnchant;
            int price = shop.EnchantPrice;
            bool maxed = shop.IsEnchantMaxed;

            _enchantRow.title.text = $"弹珠附魔   {shop.EnchantTier}/{shop.MaxEnchantTier}";
            _enchantRow.description.text = next != null
                ? next.description
                : "附魔已全部解锁，弹珠保持最高倍率与对应颜色。";
            string current = active != null
                ? $"当前：{active.displayName} ×{active.multiplier}"
                : "当前：标准白球 ×1";
            string nextText = next != null
                ? $"下一档：{next.displayName} ×{next.multiplier}"
                : "已解锁全部附魔";
            _enchantRow.effect.text = current + "\n" + nextText;

            Color color = next != null ? Theme.Hex(next.color) : Theme.Cream;
            _enchantRow.icon.color = color;
            _enchantRow.accent.color = color;
            if (maxed)
            {
                _enchantRow.button.SetText("已全部解锁");
                _enchantRow.button.SetInteractable(false);
            }
            else
            {
                _enchantRow.button.SetText($"解锁 {price:N0}");
                _enchantRow.button.SetInteractable(_game.Score.Credits >= price);
            }
        }

        private void RefreshExplosionRow()
        {
            var shop = _game.Shop;
            bool unlocked = shop.ExplosionUnlocked;
            int price = shop.ExplosionPrice;
            var settings = _config.explosion;

            _explosionRow.title.text = unlocked ? "引爆技巧   已解锁" : "引爆技巧   未解锁";
            _explosionRow.description.text = $"每累计碰撞 {settings.triggerCollisions} 次，向半径 {settings.radius * 100f:0}px 内的其他弹珠施加一次冲击。";
            _explosionRow.effect.text = $"触发：{settings.triggerCollisions} 次碰撞\n半径：{settings.radius * 100f:0}px · 力度：{settings.force:0.#}";
            _explosionRow.icon.color = unlocked ? Theme.Red : Theme.InkSoft;
            _explosionRow.accent.color = unlocked ? Theme.Red : Theme.InkSoft;
            _explosionRow.button.SetText(unlocked ? "已解锁" : $"解锁 {price:N0}");
            _explosionRow.button.SetInteractable(!unlocked && _game.Score.Credits >= price);
        }

        private void RefreshMarbleRow()
        {
            var shop = _game.Shop;
            bool canBuy = shop.CanBuyExtraMarble;
            int price = shop.ExtraMarblePrice;

            _marbleRow.title.text = $"额外发射弹珠   {shop.CurrentVolleySize}/{GameConfig.MaxVolleySize} 颗";
            _marbleRow.description.text = "永久增加每次齐射发射的弹珠数量。";
            _marbleRow.effect.text = canBuy
                ? $"购买后：每次齐射 +1 颗\n下一次：{price:N0} 可用积分"
                : "已达到每次齐射弹珠上限。";
            _marbleRow.icon.color = canBuy ? Theme.AmberBright : Theme.InkSoft;
            _marbleRow.accent.color = canBuy ? Theme.AmberBright : Theme.InkSoft;
            _marbleRow.button.SetText(canBuy ? $"购买 +1 · {price:N0}" : "已达上限");
            _marbleRow.button.SetInteractable(canBuy && _game.Score.Credits >= price);
        }

        private void RefreshBaffleRow()
        {
            var shop = _game.Shop;
            int count = shop.BaffleCount;
            int max = shop.MaxBaffleCount;
            int price = shop.BaffleReducePrice;

            _baffleRow.title.text = $"挡板调整   {count}/{max} 块";
            _baffleRow.description.text = "移除一块左右摆动的挡板，降低台面干扰；最低可以减到 0 块。";
            _baffleRow.effect.text = $"摆动幅度：{_config.baffle.amplitude * 100f:0}px · 周期：{_config.baffle.period:0.0}s\n每次移除：{price:N0} 可用积分";
            bool canReduce = shop.CanReduceBaffle;
            _baffleRow.icon.color = canReduce ? Theme.TealBright : Theme.InkSoft;
            _baffleRow.accent.color = canReduce ? Theme.TealBright : Theme.InkSoft;
            _baffleRow.button.SetText(canReduce ? $"移除 {price:N0}" : "已全部移除");
            _baffleRow.button.SetInteractable(canReduce && _game.Score.Credits >= price);
        }
    }
}
