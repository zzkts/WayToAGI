using UnityEngine;
using UnityEngine.UI;

namespace Pachinko
{
    public partial class HUDController
    {
        private GameObject _stageRoot;
        private Text _stageKicker;
        private Text _stageTitle;
        private Text _stageBody;
        private Text _stageFooter;
        private UIButton _stageButton;
        private Image _stageGlow;
        private Image _stageAccentLine;
        private Image _stageShopChip;
        private Image _stageLaunchChip;
        private Text _stageShopChipText;
        private Text _stageLaunchChipText;

        private void BuildStageTransition()
        {
            var root = UIFactory.Stretch(_modalLayer, "StageModal");
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = Theme.VerticalGradient(Theme.Hex("#3A2418"), Theme.Hex("#100A07"));
            bg.type = Image.Type.Simple;
            bg.raycastTarget = true;
            _stageRoot = root.gameObject;

            _stageGlow = UIFactory.Image(root, "StageGlow", Theme.Glow, Theme.Teal.Alpha(0.18f),
                UIFactory.Center, UIFactory.Center, new Vector2(0f, 60f), new Vector2(1200f, 1200f));
            _stageGlow.raycastTarget = false;

            var card = UIFactory.Panel(root, "StageCard", UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(860f, 820f), Theme.Panel, 36, true, 12f);
            _stageKicker = UIFactory.Label(card, "Kicker", "", 19, Theme.Amber, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 320f), new Vector2(700f, 30f), true);
            _stageTitle = UIFactory.Label(card, "Title", "", 76, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 250f), new Vector2(760f, 100f), true);
            _stageAccentLine = UIFactory.Image(card, "Line", Theme.Rounded(2), Theme.Teal,
                UIFactory.Center, UIFactory.Center, new Vector2(0f, 196f), new Vector2(180f, 5f));
            _stageBody = UIFactory.Label(card, "Body", "", 24, Theme.Cream.Alpha(0.82f), TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 92f), new Vector2(740f, 170f));
            _stageFooter = UIFactory.Label(card, "Footer", "", 15, Theme.InkSoft, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, -332f), new Vector2(740f, 24f));

            // 阶段循环提示：商店阶段 ⇄ 发射阶段。
            _stageShopChip = UIFactory.Image(card, "ShopChip", Theme.Rounded(16), Theme.Hex("#3A281C"),
                UIFactory.Center, UIFactory.Center, new Vector2(-190f, -60f), new Vector2(250f, 66f));
            _stageShopChipText = UIFactory.Label(_stageShopChip.transform, "Label", "商店阶段", 23, Theme.Cream,
                TextAnchor.MiddleCenter, UIFactory.Center, Vector2.zero, new Vector2(230f, 50f), true);
            _stageLaunchChip = UIFactory.Image(card, "LaunchChip", Theme.Rounded(16), Theme.Hex("#3A281C"),
                UIFactory.Center, UIFactory.Center, new Vector2(190f, -60f), new Vector2(250f, 66f));
            _stageLaunchChipText = UIFactory.Label(_stageLaunchChip.transform, "Label", "发射阶段", 23, Theme.Cream,
                TextAnchor.MiddleCenter, UIFactory.Center, Vector2.zero, new Vector2(230f, 50f), true);
            UIFactory.Label(card, "Arrow", "⇄", 28, Theme.Amber, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, -60f), new Vector2(60f, 50f));

            _stageButton = UIFactory.TextButton(card, "Continue", "继续", UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -238f), new Vector2(440f, 90f), Theme.Teal, Theme.Ink, 29,
                OnStageButtonClicked, 26);

            _stageRoot.SetActive(false);
        }

        private void OnStageButtonClicked()
        {
            if (_game != null) _game.ContinueStageTransition();
        }

        public void ShowStageTransition(string kicker, string title, string body, string buttonLabel, Color accent,
            bool launchStage = false)
        {
            if (_stageRoot == null) return;
            HideToast();
            _stageRoot.SetActive(false);
            _stageRoot.transform.SetAsLastSibling();
            _stageKicker.text = kicker;
            _stageKicker.color = accent;
            _stageTitle.text = title;
            _stageTitle.color = Theme.Lighten(accent, 0.25f);
            _stageBody.text = body;
            _stageAccentLine.color = accent;
            _stageGlow.color = accent.Alpha(0.18f);
            _stageButton.SetText(buttonLabel);
            _stageButton.baseColor = accent;
            _stageButton.SetInteractable(true);
            _stageButton.background.color = accent;
            _stageButton.label.color = Theme.Ink;
            _stageFooter.text = launchStage ? "一次拉杆发射全部装填弹珠 · 打空弹仓后进入商店阶段" : "补给完成后全部装填进下一发射阶段";

            SetStageChip(_stageShopChip, _stageShopChipText, !launchStage, accent);
            SetStageChip(_stageLaunchChip, _stageLaunchChipText, launchStage, accent);
            _stageRoot.SetActive(true);
            _stageRoot.transform.SetAsLastSibling();
            RefreshAll();
        }

        private static void SetStageChip(Image chip, Text label, bool active, Color accent)
        {
            if (chip == null) return;
            chip.color = active ? accent : Theme.Hex("#3A281C");
            if (label != null) label.color = active ? Theme.Ink : Theme.Cream.Alpha(0.45f);
        }

        public void HideStageTransition()
        {
            if (_stageRoot != null) _stageRoot.SetActive(false);
        }
    }
}
