using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pachinko
{
    public class UIButton
    {
        public RectTransform root;
        public Button button;
        public Image background;
        public Text label;
        public Color baseColor;
        public Color baseTextColor;

        public void SetText(string value)
        {
            if (label != null) label.text = value;
        }

        public void SetInteractable(bool value)
        {
            if (button == null) return;
            button.interactable = value;
            if (background != null)
            {
                background.color = value ? baseColor : Theme.Darken(baseColor, 0.22f).Alpha(0.9f);
            }
            if (label != null)
            {
                label.color = value ? baseTextColor : Theme.Cream.Alpha(0.72f);
            }
        }

        public void SetActive(bool value)
        {
            if (root != null) root.gameObject.SetActive(value);
        }
    }

    public static class UIFactory
    {
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        public static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        public static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

        public static Canvas CreateCanvas(out RectTransform safeRoot)
        {
            var canvasGo = new GameObject("HUDCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var safeGo = new GameObject("SafeArea", typeof(RectTransform));
            safeGo.transform.SetParent(canvasGo.transform, false);
            var safe = (RectTransform)safeGo.transform;
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            safe.offsetMin = Vector2.zero;
            safe.offsetMax = Vector2.zero;
            safeGo.AddComponent<SafeAreaFitter>();
            safeRoot = safe;
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Stretch(Transform parent, string name, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, bool raycast = false)
        {
            var rect = Rect(parent, name, anchor, pivot, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null && sprite.border.sqrMagnitude > 0f)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
            }
            return image;
        }

        public static Image Circle(Transform parent, string name, Color color, Vector2 anchor, Vector2 pivot, Vector2 position, float diameter)
        {
            return Image(parent, name, Theme.Circle, color, anchor, pivot, position, new Vector2(diameter, diameter));
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color color, int radius, bool shadow = false, float shadowOffset = 6f)
        {
            var panel = Rect(parent, name, anchor, pivot, position, size);
            if (shadow)
            {
                var shadowImage = Image(panel, "Shadow", Theme.Rounded(radius), Theme.Shadow, Center, Center, new Vector2(0, -shadowOffset), size);
                shadowImage.raycastTarget = false;
            }
            var image = panel.gameObject.AddComponent<Image>();
            image.sprite = Theme.Rounded(radius);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = color;
            return panel;
        }

        public static Text Label(Transform parent, string name, string value, int fontSize, Color color,
            TextAnchor alignment, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, bool bold = false)
        {
            var rect = Rect(parent, name, anchor, pivot, position, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Theme.Font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.05f;
            return text;
        }

        public static Text Label(Transform parent, string name, string value, int fontSize, Color color,
            TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 size, bool bold = false)
        {
            return Label(parent, name, value, fontSize, color, alignment, anchor, anchor, position, size, bold);
        }

        public static UIButton TextButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pivot,
            Vector2 position, Vector2 size, Color background, Color textColor, int fontSize, Action onClick,
            int radius = 18, bool shadow = true)
        {
            var root = Rect(parent, name, anchor, pivot, position, size);
            if (shadow)
            {
                Image(root, "Shadow", Theme.Rounded(radius), Theme.Shadow, Center, Center, new Vector2(0, -5f), size).raycastTarget = false;
            }

            var bgImage = Image(root, "Background", Theme.Rounded(radius), background, Center, Center, Vector2.zero, size, true);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = bgImage;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.84f, 1f);
            colors.pressedColor = new Color(0.84f, 0.78f, 0.7f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.82f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var text = Label(root, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter, Center, Vector2.zero, size - new Vector2(10, 6), true);
            return new UIButton
            {
                root = root,
                button = button,
                background = bgImage,
                label = text,
                baseColor = background,
                baseTextColor = textColor
            };
        }

        public static UIButton IconButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pivot,
            Vector2 position, float diameter, Color background, Color textColor, int fontSize, Action onClick)
        {
            var button = TextButton(parent, name, label, anchor, pivot, position, new Vector2(diameter, diameter), background, textColor, fontSize, onClick, Mathf.RoundToInt(diameter * 0.5f), false);
            return button;
        }

        public static Image RoundedRectOnUI(Transform parent, string name, Vector2 position, Vector2 size,
            Color color, int radius)
        {
            return Image(parent, name, Theme.Rounded(radius), color, TopLeft, TopLeft, position, size);
        }

        public static void SetFullScreenImage(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var rect = Stretch(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : Theme.White;
            image.color = color;
            image.raycastTarget = false;
        }

        public static void AddShadow(Text text, Color color, Vector2 distance)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }
    }

    public class SafeAreaFitter : MonoBehaviour
    {
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea || _lastScreen.x != Screen.width || _lastScreen.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            var rect = (RectTransform)transform;
            Rect safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0 || Screen.width <= 0 || Screen.height <= 0)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return;
            }

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _lastSafeArea = safe;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
