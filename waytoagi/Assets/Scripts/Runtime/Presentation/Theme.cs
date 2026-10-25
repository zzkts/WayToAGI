using System.Collections.Generic;
using UnityEngine;

namespace Pachinko
{
    public static class Theme
    {
        // 暖色街机风：暖黄 / 琥珀橙 / 米白 / 青蓝 / 橙红
        public static readonly Color Background = Hex("#1B120D");
        public static readonly Color BackgroundSoft = Hex("#2A1C14");
        public static readonly Color Panel = Hex("#35251B");
        public static readonly Color PanelLight = Hex("#4A3526");
        public static readonly Color Cream = Hex("#F6E8CF");
        public static readonly Color CreamDeep = Hex("#EAD7B7");
        public static readonly Color Ink = Hex("#33241A");
        public static readonly Color InkSoft = Hex("#6C5542");
        public static readonly Color Amber = Hex("#E9A93E");
        public static readonly Color AmberBright = Hex("#FFC95C");
        public static readonly Color Orange = Hex("#DF7137");
        public static readonly Color Red = Hex("#C84B3D");
        public static readonly Color Teal = Hex("#357E8A");
        public static readonly Color TealBright = Hex("#57B4BC");
        public static readonly Color Green = Hex("#71A85B");
        public static readonly Color Shadow = new Color(0.08f, 0.04f, 0.02f, 0.55f);
        public static readonly Color Overlay = new Color(0.05f, 0.03f, 0.02f, 0.72f);

        public const float BallRadius = 0.10f;

        private static Font _font;
        private static Sprite _white;
        private static Sprite _circle;
        private static Sprite _glow;
        private static readonly Dictionary<int, Sprite> RoundedCache = new Dictionary<int, Sprite>();
        private static readonly Dictionary<(int, int, int, int), Sprite> GradientCache = new Dictionary<(int, int, int, int), Sprite>();

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.Load<Font>("Fonts/NotoSansSC-Medium");
                if (_font == null)
                {
                    _font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Microsoft YaHei", "Noto Sans CJK SC", "PingFang SC", "WenQuanYi Micro Hei", "Arial" },
                        32);
                }
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _font;
            }
        }

        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
                    {
                        name = "px_white",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    var pixels = new Color[16];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                    tex.SetPixels(pixels);
                    tex.Apply();
                    _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    _white.name = "px_white";
                }
                return _white;
            }
        }

        public static Sprite Circle => _circle != null ? _circle : (_circle = BuildCircle(128));

        public static Sprite Glow => _glow != null ? _glow : (_glow = BuildGlow(128));

        public static Sprite Rounded(int radius)
        {
            radius = Mathf.Clamp(radius, 2, 60);
            if (RoundedCache.TryGetValue(radius, out var cached)) return cached;
            var sprite = BuildRounded(128, radius);
            RoundedCache[radius] = sprite;
            return sprite;
        }

        public static Sprite VerticalGradient(Color top, Color bottom)
        {
            var key = (ToKey(top), ToKey(bottom), 0, 0);
            if (GradientCache.TryGetValue(key, out var cached)) return cached;

            const int w = 4;
            const int h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "vertical_gradient",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                var color = Color.Lerp(bottom, top, t);
                for (int x = 0; x < w; x++) pixels[y * w + x] = color;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = "vertical_gradient";
            GradientCache[key] = sprite;
            return sprite;
        }

        private static int ToKey(Color c)
        {
            return (Mathf.RoundToInt(c.r * 255) << 24) |
                   (Mathf.RoundToInt(c.g * 255) << 16) |
                   (Mathf.RoundToInt(c.b * 255) << 8) |
                   Mathf.RoundToInt(c.a * 255);
        }

        public static Color Hex(string hex)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.magenta;
        }

        public static Color Alpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color Lighten(Color c, float amount)
        {
            return Color.Lerp(c, Color.white, Mathf.Clamp01(amount));
        }

        public static Color Darken(Color c, float amount)
        {
            return Color.Lerp(c, Color.black, Mathf.Clamp01(amount));
        }

        private static Sprite BuildCircle(int size)
        {
            var tex = NewTexture(size, "circle");
            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f - 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < 4; sy++)
                    {
                        for (int sx = 0; sx < 4; sx++)
                        {
                            float px = x + (sx + 0.5f) / 4f - center;
                            float py = y + (sy + 0.5f) / 4f - center;
                            if (px * px + py * py <= radius * radius) inside++;
                        }
                    }
                    float a = inside / 16f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = "circle";
            return sprite;
        }

        private static Sprite BuildGlow(int size)
        {
            var tex = NewTexture(size, "glow");
            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / radius;
                    float dy = (y - center) / radius;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    a = a * a;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = "glow";
            return sprite;
        }

        private static Sprite BuildRounded(int size, int radius)
        {
            var tex = NewTexture(size, "rounded");
            var pixels = new Color[size * size];
            float half = size * 0.5f - 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < 4; sy++)
                    {
                        for (int sx = 0; sx < 4; sx++)
                        {
                            float px = x + (sx + 0.5f) / 4f - size * 0.5f;
                            float py = y + (sy + 0.5f) / 4f - size * 0.5f;
                            if (InsideRounded(px, py, half, radius)) inside++;
                        }
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, inside / 16f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var border = new Vector4(radius, radius, radius, radius);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = "rounded_" + radius;
            return sprite;
        }

        private static bool InsideRounded(float x, float y, float half, float radius)
        {
            float inner = half - radius;
            float cx = Mathf.Clamp(x, -inner, inner);
            float cy = Mathf.Clamp(y, -inner, inner);
            float dx = x - cx;
            float dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static Texture2D NewTexture(int size, string name)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        public static SpriteRenderer SpriteObject(Transform parent, string name, Sprite sprite, Color color, int order, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(position.x, position.y, order * 0.001f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        public static SpriteRenderer RoundedRect(Transform parent, string name, Vector2 position, Vector2 size, Color color, int radius, int order)
        {
            var renderer = SpriteObject(parent, name, Rounded(radius), color, order, position);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            return renderer;
        }

        public static SpriteRenderer CircleObject(Transform parent, string name, Vector2 position, float diameter, Color color, int order)
        {
            var renderer = SpriteObject(parent, name, Circle, color, order, position);
            float spriteSize = Circle.bounds.size.x;
            float scale = diameter / Mathf.Max(0.0001f, spriteSize);
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            return renderer;
        }

        public static void SetCircleSize(SpriteRenderer renderer, float diameter)
        {
            float spriteSize = renderer.sprite != null ? renderer.sprite.bounds.size.x : 1.28f;
            float scale = diameter / Mathf.Max(0.0001f, spriteSize);
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        public static Color BallColor(BallDef ball)
        {
            if (ball == null) return Cream;
            return Hex(ball.color);
        }

        public static Color BallOutline(BallDef ball)
        {
            if (ball == null) return Ink;
            return Hex(ball.outlineColor);
        }
    }
}
