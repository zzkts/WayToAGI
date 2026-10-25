using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pachinko
{
    public partial class HUDController : MonoBehaviour
    {
        private GameManager _game;
        private GameConfig _config;
        private Camera _camera;
        private BoardRefs _board;
        private TiltController _tilt;
        private SpringLauncher _launcher;

        private RectTransform _safeRoot;
        private RectTransform _hudLayer;
        private RectTransform _popupLayer;
        private RectTransform _modalLayer;
        private RectTransform _toastLayer;

        private Text _shotScoreText;
        private Text _loopScoreText;
        private Text _totalScoreText;
        private Text _creditsText;
        private Text _targetText;
        private Image _progressFill;
        private GameObject _comboRoot;
        private Text _comboText;
        private Image _comboBadgeImage;

        private Image _ballDot;
        private Text _ballCaptionText;
        private Text _ballNameText;
        private Text _ballDetailText;
        private UIButton _settingsButton;
        private readonly List<Image> _magazinePips = new List<Image>();

        private RectTransform _phasePanel;
        private Text _phaseTitleText;
        private Text _phaseDetailText;

        private Text _promptText;
        private GameObject _chargeRoot;
        private Image _chargeFill;
        private Image _tiltMarker;
        private Image _tiltTrack;
        private Text _tiltSourceText;
        private float _filteredPromptCharge = -1f;

        private bool _settingsOpen;
        private bool _resetConfirm;
        private Coroutine _resetConfirmRoutine;
        private Coroutine _toastRoutine;
        private CanvasGroup _toastCanvas;
        private Text _toastText;

        public bool IsAnyModalOpen =>
            (_menuRoot != null && _menuRoot.activeSelf) ||
            (_shopRoot != null && _shopRoot.activeSelf) ||
            (_victoryRoot != null && _victoryRoot.activeSelf) ||
            (_settingsRoot != null && _settingsRoot.activeSelf);

        public static HUDController Create(GameManager game, GameConfig config, Camera camera, BoardRefs board,
            TiltController tilt, SpringLauncher launcher)
        {
            var canvas = UIFactory.CreateCanvas(out var safeRoot);
            var go = new GameObject("HUD");
            go.transform.SetParent(game.transform, false);
            var hud = go.AddComponent<HUDController>();
            hud._game = game;
            hud._config = config;
            hud._camera = camera;
            hud._board = board;
            hud._tilt = tilt;
            hud._launcher = launcher;
            hud._safeRoot = safeRoot;
            hud.Build();
            return hud;
        }

        private void Build()
        {
            _hudLayer = UIFactory.Stretch(_safeRoot, "HUDLayer");
            _popupLayer = UIFactory.Stretch(_safeRoot, "PopupLayer");
            _modalLayer = UIFactory.Stretch(_safeRoot, "ModalLayer");
            _toastLayer = UIFactory.Stretch(_safeRoot, "ToastLayer");

            BuildScreenTint();
            BuildScoreCard();
            BuildBallCard();
            BuildComboBadge();
            BuildBottomControls();
            BuildMenu();
            BuildExchange();
            BuildVictoryPanel();
            BuildSettingsPanel();
            BuildToast();
        }

        private void BuildScreenTint()
        {
            var top = CreateTopGradient(_hudLayer, "TopShade", 300f, Theme.Hex("#140C08").Alpha(0.62f));
            var bottom = CreateBottomGradient(_hudLayer, "BottomShade", 260f, Theme.Hex("#140C08").Alpha(0.58f));
            top.raycastTarget = false;
            bottom.raycastTarget = false;
        }

        private static Image CreateTopGradient(Transform parent, string name, float height, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.sprite = Theme.VerticalGradient(color, color.Alpha(0f));
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateBottomGradient(Transform parent, string name, float height, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.sprite = Theme.VerticalGradient(color.Alpha(0f), color);
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private void BuildScoreCard()
        {
            var card = UIFactory.Panel(_hudLayer, "ScoreCard", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(24f, -24f), new Vector2(372f, 312f), Theme.Panel.Alpha(0.94f), 26, true, 7f);

            UIFactory.RoundedRectOnUI(card, "Accent", new Vector2(20f, -20f), new Vector2(5f, 34f), Theme.Amber, 3);

            var content = UIFactory.Rect(card, "Content", UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(24f, -18f), new Vector2(324f, 280f));

            CreateScoreRow(content, "本次齐射", 0f, out _shotScoreText, Theme.Cream, 28);
            CreateScoreRow(content, "本循环得分", -54f, out _loopScoreText, Theme.Cream, 28);
            CreateScoreRow(content, "累计得分", -108f, out _totalScoreText, Theme.Cream, 28);
            CreateScoreRow(content, "可用积分", -162f, out _creditsText, Theme.AmberBright, 33);

            var progressBg = UIFactory.Image(content, "ProgressBg", Theme.Rounded(4), Theme.Ink.Alpha(0.18f),
                UIFactory.TopLeft, UIFactory.TopLeft, new Vector2(0f, -232f), new Vector2(324f, 8f));
            progressBg.raycastTarget = false;
            var fillImage = UIFactory.Image(content, "ProgressFill", Theme.Rounded(4), Theme.Amber,
                UIFactory.TopLeft, UIFactory.TopLeft, new Vector2(0f, -232f), new Vector2(324f, 8f));
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;
            fillImage.raycastTarget = false;
            _progressFill = fillImage;

            _targetText = UIFactory.Label(content, "Target", $"通关目标 {_game.GoalScore:N0}", 16, Theme.Cream.Alpha(0.52f),
                TextAnchor.MiddleLeft, UIFactory.TopLeft, new Vector2(0f, -246f), new Vector2(324f, 24f));
        }

        private void CreateScoreRow(RectTransform parent, string label, float y, out Text value, Color valueColor, int size)
        {
            UIFactory.Label(parent, label + "Label", label, 20, Theme.InkSoft, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(0f, y), new Vector2(140f, 32f));
            value = UIFactory.Label(parent, label + "Value", "0", size, valueColor, TextAnchor.MiddleRight,
                UIFactory.TopLeft, new Vector2(150f, y), new Vector2(174f, 38f), true);
        }

        private void BuildBallCard()
        {
            var card = UIFactory.Panel(_hudLayer, "BallCard", UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-24f, -24f), new Vector2(304f, 126f), Theme.Panel.Alpha(0.94f), 24, true, 7f);
            _ballCaptionText = UIFactory.Label(card, "Caption", "当前弹珠", 15, Theme.Cream.Alpha(0.5f), TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(78f, -10f), new Vector2(210f, 22f));
            _ballDot = UIFactory.Circle(card, "BallDot", Theme.Cream, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(20f, -22f), 44f);
            _ballDot.raycastTarget = false;
            UIFactory.Circle(card, "BallDotRim", Theme.Lighten(Theme.Cream, 0.2f).Alpha(0.3f), UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(20f, -22f), 50f).transform.SetAsFirstSibling();
            _ballNameText = UIFactory.Label(card, "BallName", "标准弹珠", 24, Theme.Cream, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(78f, -32f), new Vector2(190f, 30f), true);
            _ballDetailText = UIFactory.Label(card, "BallDetail", "倍率 ×1", 17, Theme.Amber, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(78f, -64f), new Vector2(205f, 24f));

            var magazinePanel = UIFactory.Panel(_hudLayer, "MagazinePanel", UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-24f, -160f), new Vector2(304f, 46f), Theme.Panel.Alpha(0.9f), 16, false);
            UIFactory.Label(magazinePanel, "MagazineLabel", "齐射", 13, Theme.Cream.Alpha(0.48f), TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(14f, -11f), new Vector2(40f, 24f));
            for (int i = 0; i < 14; i++)
            {
                var pip = UIFactory.Circle(magazinePanel, "Pip" + i, Theme.InkSoft, UIFactory.TopLeft, UIFactory.TopLeft,
                    new Vector2(60f + i * 16f, -17f), 12f);
                pip.raycastTarget = false;
                pip.gameObject.SetActive(false);
                _magazinePips.Add(pip);
            }

            _settingsButton = UIFactory.TextButton(_hudLayer, "SettingsButton", "设置", UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-24f, -214f), new Vector2(96f, 38f), Theme.PanelLight.Alpha(0.94f), Theme.Cream, 16,
                ShowSettings, 13, false);
        }

        private void BuildComboBadge()
        {
            var root = UIFactory.Panel(_hudLayer, "ComboBadge", UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -26f), new Vector2(344f, 64f), Theme.Amber, 22, true, 5f);
            _comboRoot = root.gameObject;
            _comboBadgeImage = root.GetComponent<Image>();
            UIFactory.Circle(root, "ComboDot", Theme.Red, UIFactory.Center, UIFactory.Center, new Vector2(-140f, 0f), 18f).raycastTarget = false;
            _comboText = UIFactory.Label(root, "ComboText", "", 21, Theme.Ink, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(8f, 0f), new Vector2(296f, 50f), true);
            _comboRoot.SetActive(false);
        }

        private void BuildBottomControls()
        {
            var promptPanel = UIFactory.Panel(_hudLayer, "PromptPanel", UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 166f), new Vector2(780f, 50f), Theme.Panel.Alpha(0.84f), 22, false);
            _promptText = UIFactory.Label(promptPanel, "Prompt", "", 21, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, Vector2.zero, new Vector2(750f, 44f));

            var chargeRoot = UIFactory.Rect(_hudLayer, "ChargeRoot", UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 134f), new Vector2(460f, 12f));
            var chargeBg = UIFactory.Image(chargeRoot, "ChargeBg", Theme.Rounded(5), Theme.Ink.Alpha(0.2f),
                UIFactory.Center, UIFactory.Center, Vector2.zero, new Vector2(460f, 12f));
            chargeBg.raycastTarget = false;
            var chargeFill = UIFactory.Image(chargeRoot, "ChargeFill", Theme.Rounded(5), Theme.Amber,
                UIFactory.Center, UIFactory.Center, Vector2.zero, new Vector2(456f, 10f));
            chargeFill.type = Image.Type.Filled;
            chargeFill.fillMethod = Image.FillMethod.Horizontal;
            chargeFill.fillOrigin = 0;
            chargeFill.fillAmount = 0f;
            chargeFill.raycastTarget = false;
            _chargeFill = chargeFill;
            _chargeRoot = chargeRoot.gameObject;
            _chargeRoot.SetActive(false);

            var tiltCard = UIFactory.Panel(_hudLayer, "TiltCard", UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 22f), new Vector2(420f, 100f), Theme.Panel.Alpha(0.9f), 24, true, 6f);
            UIFactory.Label(tiltCard, "TiltLabel", "倾斜台面", 19, Theme.Cream, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(22f, -10f), new Vector2(160f, 28f), true);
            _tiltSourceText = UIFactory.Label(tiltCard, "TiltSource", "", 14, Theme.InkSoft, TextAnchor.MiddleRight,
                UIFactory.TopRight, new Vector2(-22f, -12f), new Vector2(220f, 24f));

            var track = UIFactory.Image(tiltCard, "TiltTrack", Theme.Rounded(4), Theme.Ink.Alpha(0.16f),
                UIFactory.Center, UIFactory.Center, new Vector2(0f, -14f), new Vector2(330f, 7f));
            track.raycastTarget = false;
            _tiltTrack = track;
            UIFactory.Image(tiltCard, "TiltCenter", Theme.Rounded(2), Theme.Ink.Alpha(0.35f),
                UIFactory.Center, UIFactory.Center, new Vector2(0f, -14f), new Vector2(2f, 16f));
            _tiltMarker = UIFactory.Circle(tiltCard, "TiltMarker", Theme.Amber, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -14f), 22f);
            _tiltMarker.raycastTarget = false;
            UIFactory.Label(tiltCard, "TiltLeft", "左", 15, Theme.InkSoft, TextAnchor.MiddleLeft,
                UIFactory.Center, new Vector2(-178f, -38f), new Vector2(30f, 22f));
            UIFactory.Label(tiltCard, "TiltRight", "右", 15, Theme.InkSoft, TextAnchor.MiddleRight,
                UIFactory.Center, new Vector2(178f, -38f), new Vector2(30f, 22f));

            _phasePanel = UIFactory.Panel(_hudLayer, "PhasePanel", UIFactory.BottomRight, UIFactory.BottomRight,
                new Vector2(-28f, 28f), new Vector2(250f, 86f), Theme.Teal.Alpha(0.95f), 24, true, 6f);
            UIFactory.Circle(_phasePanel, "PhaseDot", Theme.AmberBright, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(20f, -24f), 16f).raycastTarget = false;
            _phaseTitleText = UIFactory.Label(_phasePanel, "PhaseTitle", "第 1 循环", 24, Theme.Cream, TextAnchor.MiddleLeft,
                UIFactory.TopLeft, new Vector2(44f, -13f), new Vector2(190f, 32f), true);
            _phaseDetailText = UIFactory.Label(_phasePanel, "PhaseDetail", "", 15, Theme.Cream.Alpha(0.7f),
                TextAnchor.MiddleLeft, UIFactory.TopLeft, new Vector2(22f, -48f), new Vector2(216f, 24f));
            _phasePanel.gameObject.SetActive(false);
        }

        public void ShowMenu()
        {
            HideAllModals();
            if (_menuRoot != null) _menuRoot.SetActive(true);
            RefreshAll();
            RefreshMenuSummary();
        }

        public void HideAllModals()
        {
            if (_menuRoot != null) _menuRoot.SetActive(false);
            if (_shopRoot != null) _shopRoot.SetActive(false);
            if (_victoryRoot != null) _victoryRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
            _settingsOpen = false;
        }

        public void RefreshAll()
        {
            if (_game == null) return;
            var score = _game.Score;

            if (_shotScoreText != null) _shotScoreText.text = _game.CurrentShotScore.ToString("N0");
            if (_loopScoreText != null) _loopScoreText.text = _game.LoopScore.ToString("N0");
            if (_totalScoreText != null) _totalScoreText.text = score.TotalScore.ToString("N0");
            if (_creditsText != null) _creditsText.text = score.Credits.ToString("N0");
            if (_progressFill != null) _progressFill.fillAmount = _game.GoalProgress;
            if (_targetText != null)
            {
                _targetText.text = _game.GoalReached
                    ? $"已通关 {_game.GoalScore:N0} · 无尽模式继续冲分"
                    : $"通关目标 {_game.GoalScore:N0} · 还差 {_game.GoalRemaining:N0}";
            }

            if (_comboRoot != null)
            {
                bool show = score.Streak >= 2 &&
                    (_game.State == GameState.Aim || _game.State == GameState.BallFlight || _game.State == GameState.Resolving);
                _comboRoot.SetActive(show);
                if (show && _comboText != null)
                {
                    int combo = score.ComboMultiplier;
                    _comboText.text = $"同洞连击 ×{combo} · 第 {score.Streak} 连";
                    if (_comboBadgeImage != null)
                    {
                        _comboBadgeImage.color = combo >= ScoreRules.ComboCap ? Theme.Red : Theme.Amber;
                    }
                    _comboText.color = combo >= ScoreRules.ComboCap ? Theme.Cream : Theme.Ink;
                }
            }

            var enchant = _game.ActiveEnchant;
            if (_ballDot != null) _ballDot.color = _game.CurrentBallColor;
            if (_ballNameText != null)
            {
                _ballNameText.text = enchant != null ? enchant.displayName : "标准弹珠";
                _ballNameText.color = Theme.Cream;
            }
            if (_ballCaptionText != null)
            {
                switch (_game.State)
                {
                    case GameState.Aim:
                        _ballCaptionText.text = $"发射阶段 · 第 {_game.LoopIndex} 循环";
                        break;
                    case GameState.BallFlight:
                        _ballCaptionText.text = "发射阶段 · 齐射中";
                        break;
                    case GameState.Resolving:
                        _ballCaptionText.text = "发射阶段 · 本轮结算";
                        break;
                    case GameState.Exchange:
                        _ballCaptionText.text = "循环结束 · 兑换中";
                        break;
                    case GameState.Victory:
                        _ballCaptionText.text = "通关目标达成";
                        break;
                    default:
                        _ballCaptionText.text = "等待出发";
                        break;
                }
            }
            if (_ballDetailText != null)
            {
                string explosion = _game.SaveData.explosionUnlocked ? " · 引爆已解锁" : "";
                _ballDetailText.text = $"弹珠倍率 ×{_game.BallMultiplier}{explosion}";
                _ballDetailText.color = Theme.Amber;
            }

            RefreshMagazinePips();
            if (_phasePanel != null)
            {
                bool showPhase = _game.State != GameState.Menu;
                _phasePanel.gameObject.SetActive(showPhase);
                if (showPhase)
                {
                    _phaseTitleText.text = $"第 {_game.LoopIndex} 循环";
                    switch (_game.State)
                    {
                        case GameState.Aim:
                            // 不显示本循环总齐射次数，保留「循环何时结束」的悬念。
                            _phaseDetailText.text = $"第 {_game.CurrentVolleyNumber} 次齐射 · 目标 {_game.LoopThreshold:N0}";
                            break;
                        case GameState.BallFlight:
                            _phaseDetailText.text = $"落洞 {_game.VolleyHits}/{_game.VolleyTotalBalls} · 本循环 {_game.LoopScore:N0}";
                            break;
                        case GameState.Resolving:
                            _phaseDetailText.text = "齐射结算中…";
                            break;
                        case GameState.Victory:
                            _phaseDetailText.text = $"累计积分突破 {_game.GoalScore:N0}";
                            break;
                        case GameState.Exchange:
                            _phaseDetailText.text = "本循环结束 · 兑换中";
                            break;
                        default:
                            _phaseDetailText.text = $"目标 {_game.LoopThreshold:N0}";
                            break;
                    }
                }
            }

            if (_settingsButton != null)
            {
                _settingsButton.SetInteractable(_game.State == GameState.Menu || _game.State == GameState.Aim);
            }
            if (_tiltSourceText != null)
            {
                _tiltSourceText.text = _tilt != null && _tilt.HasSensor ? "重力感应" : "A / D 或按住左右屏";
            }

            RefreshExchange();
            RefreshVictory();
        }

        private void RefreshMagazinePips()
        {
            if (_magazinePips == null) return;
            int count = _game.State == GameState.Aim || _game.State == GameState.BallFlight || _game.State == GameState.Resolving
                ? _game.VolleyTotalBalls
                : 0;
            count = Mathf.Min(_magazinePips.Count, count);
            for (int i = 0; i < _magazinePips.Count; i++)
            {
                var pip = _magazinePips[i];
                bool visible = i < count;
                pip.gameObject.SetActive(visible);
                if (!visible) continue;
                pip.color = _game.CurrentBallColor;
                float scale = i == 0 ? 1.15f : 1f;
                pip.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public void ShowToast(string message)
        {
            if (string.IsNullOrEmpty(message) || _toastText == null) return;
            var rect = (RectTransform)_toastCanvas.transform;
            if (_shopRoot != null && _shopRoot.activeSelf)
            {
                rect.anchorMin = UIFactory.BottomCenter;
                rect.anchorMax = UIFactory.BottomCenter;
                rect.pivot = UIFactory.BottomCenter;
                rect.anchoredPosition = new Vector2(0f, 214f);
            }
            else
            {
                rect.anchorMin = UIFactory.TopCenter;
                rect.anchorMax = UIFactory.TopCenter;
                rect.pivot = UIFactory.TopCenter;
                rect.anchoredPosition = new Vector2(0f, -286f);
            }
            _toastText.text = message;
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ToastRoutine());
        }

        public void HideToast()
        {
            if (_toastRoutine != null)
            {
                StopCoroutine(_toastRoutine);
                _toastRoutine = null;
            }
            if (_toastCanvas != null) _toastCanvas.gameObject.SetActive(false);
        }

        private IEnumerator ToastRoutine()
        {
            if (_toastCanvas == null) yield break;
            var root = _toastCanvas.transform;
            root.gameObject.SetActive(true);
            _toastCanvas.alpha = 0f;
            float t = 0f;
            while (t < 0.16f)
            {
                t += Time.unscaledDeltaTime;
                _toastCanvas.alpha = Mathf.Clamp01(t / 0.16f);
                yield return null;
            }
            _toastCanvas.alpha = 1f;
            yield return new WaitForSecondsRealtime(1.55f);
            t = 0f;
            while (t < 0.28f)
            {
                t += Time.unscaledDeltaTime;
                _toastCanvas.alpha = 1f - Mathf.Clamp01(t / 0.28f);
                yield return null;
            }
            root.gameObject.SetActive(false);
            _toastRoutine = null;
        }

        private void BuildToast()
        {
            var panel = UIFactory.Panel(_toastLayer, "Toast", UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -286f), new Vector2(680f, 52f), Theme.Ink.Alpha(0.94f), 22, true, 4f);
            _toastCanvas = panel.gameObject.AddComponent<CanvasGroup>();
            _toastCanvas.blocksRaycasts = false;
            _toastCanvas.interactable = false;
            _toastText = UIFactory.Label(panel, "ToastText", "", 21, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, Vector2.zero, new Vector2(640f, 44f));
            panel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_game == null) return;

            float tilt = _game.State == GameState.BallFlight ? _game.Tilt.CurrentTilt : 0f;
            if (_tiltMarker != null)
            {
                _tiltMarker.rectTransform.anchoredPosition = new Vector2(tilt * 165f, -14f);
                bool active = _game.State == GameState.BallFlight;
                _tiltMarker.color = active ? Theme.Amber : Theme.InkSoft.Alpha(0.4f);
            }
            if (_tiltTrack != null)
            {
                _tiltTrack.color = Theme.Ink.Alpha(_game.State == GameState.BallFlight ? 0.24f : 0.11f);
            }

            UpdatePrompt();
            UpdateCharge();

            if (_comboRoot != null && _comboRoot.activeSelf)
            {
                float s = 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.012f;
                _comboRoot.transform.localScale = new Vector3(s, s, 1f);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_settingsRoot != null && _settingsRoot.activeSelf)
                {
                    HideSettings();
                }
                else if (_shopRoot != null && _shopRoot.activeSelf && _game.State == GameState.Exchange)
                {
                    _game.CloseExchange();
                }
            }
        }

        private void UpdatePrompt()
        {
            if (_promptText == null) return;
            var state = _game.State;
            if (state == GameState.Aim)
            {
                if (_launcher != null && _launcher.IsCharging)
                {
                    int percent = Mathf.RoundToInt(_launcher.ChargeRatio * 100f);
                    _promptText.text = $"蓄力 {percent}% · 松手一次发射全部 {_game.VolleyTotalBalls} 颗";
                    _promptText.color = Theme.AmberBright;
                }
                else
                {
                    _promptText.text = $"拖拽弹簧蓄力，一次发射全部 {_game.VolleyTotalBalls} 颗";
                    _promptText.color = Theme.Cream;
                }
            }
            else if (state == GameState.BallFlight)
            {
                _promptText.text = $"齐射中 · 倾斜控球 · 已落洞 {_game.VolleyHits}/{_game.VolleyTotalBalls}";
                _promptText.color = Theme.Cream;
            }
            else if (state == GameState.Resolving)
            {
                _promptText.text = "本轮结算中…";
                _promptText.color = Theme.Cream;
            }
            else if (state == GameState.Exchange)
            {
                _promptText.text = "循环结束 · 兑换只消耗可用积分";
                _promptText.color = Theme.AmberBright;
            }
            else if (state == GameState.Menu)
            {
                _promptText.text = "准备开始";
                _promptText.color = Theme.Cream;
            }
            else
            {
                _promptText.text = "";
            }
        }

        private void UpdateCharge()
        {
            if (_chargeRoot == null) return;
            bool charging = _game.State == GameState.Aim && _launcher != null && _launcher.IsCharging;
            if (charging)
            {
                float target = Mathf.Clamp01(_launcher.ChargeRatio);
                _filteredPromptCharge = _filteredPromptCharge < 0f ? target : Mathf.Lerp(_filteredPromptCharge, target, Time.unscaledDeltaTime * 18f);
                if (!_chargeRoot.activeSelf) _chargeRoot.SetActive(true);
                _chargeFill.fillAmount = _filteredPromptCharge;
                _chargeFill.color = Color.Lerp(Theme.Amber, Theme.Red, _filteredPromptCharge);
            }
            else
            {
                _filteredPromptCharge = -1f;
                if (_chargeRoot.activeSelf) _chargeRoot.SetActive(false);
            }
        }

        private void ShowSettings()
        {
            if (_game == null) return;
            if (_game.State != GameState.Aim && _game.State != GameState.Menu) return;
            if (_settingsRoot == null) return;
            _settingsRoot.SetActive(true);
            _settingsRoot.transform.SetAsLastSibling();
            _settingsOpen = true;
            _resetConfirm = false;
            RefreshSettings();
        }

        private void HideSettings()
        {
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
            _settingsOpen = false;
            _resetConfirm = false;
            if (_settingsResetButton != null) _settingsResetButton.SetText("重置游戏");
            if (_resetConfirmRoutine != null)
            {
                StopCoroutine(_resetConfirmRoutine);
                _resetConfirmRoutine = null;
            }
        }

        public void DebugShowSettings()
        {
            ShowSettings();
        }

        public void DebugHideSettings()
        {
            HideSettings();
        }
    }
}
