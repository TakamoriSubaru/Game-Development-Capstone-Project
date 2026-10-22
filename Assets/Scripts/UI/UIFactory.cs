using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Oddyseus.UI
{
    // Warna mengikuti wireframe (biru muda). Ganti di sini untuk re-skin seluruh UI.
    public static class Theme
    {
        public static readonly Color Bg = Hex("#C6D9FF");
        public static readonly Color Panel = Hex("#D6E3FF");
        public static readonly Color Card = Hex("#EEF3FF");
        public static readonly Color Slate = Hex("#8FA0BC");
        public static readonly Color Ink = Hex("#2B3652");
        public static readonly Color Line = Hex("#5A6A8C");
        public static readonly Color TabActive = Hex("#6B7690");
        public static readonly Color Gold = Hex("#E8DC9A");
        public static readonly Color Highlight = Hex("#F2C94C");
        public static readonly Color Good = Hex("#3DBE5A");
        public static readonly Color Bad = Hex("#D64545");
        public static readonly Color Dark = Hex("#555555");
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.55f);

        public static Color Hex(string h)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(h, out c)) c = Color.magenta;
            return c;
        }
    }

    // Semua UI dibangun lewat kode (tanpa prefab). Posisi memakai anchor 0..1 relatif terhadap induk.
    public static class UIFactory
    {
        static Font font;
        static Sprite circle;
        static Sprite rounded;

        public static Font DefaultFont
        {
            get
            {
                if (font == null)
                {
#if UNITY_2022_2_OR_NEWER
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
                }
                return font;
            }
        }

        // Sprite lingkaran dibuat lewat kode supaya tidak bergantung pada aset bawaan editor.
        public static Sprite CircleSprite
        {
            get
            {
                if (circle == null)
                {
                    const int n = 64;
                    var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    var px = new Color32[n * n];
                    float r = n / 2f;
                    for (int y = 0; y < n; y++)
                    {
                        for (int x = 0; x < n; x++)
                        {
                            float dx = x + 0.5f - r, dy = y + 0.5f - r;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            float a = Mathf.Clamp01(r - d);
                            px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                        }
                    }
                    tex.SetPixels32(px);
                    tex.Apply();
                    circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
                }
                return circle;
            }
        }

        // Sprite persegi bersudut membulat (9-slice), dibuat lewat kode.
        public static Sprite RoundedSprite
        {
            get
            {
                if (rounded == null)
                {
                    const int n = 32;
                    const float rad = 10f;
                    var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    var px = new Color32[n * n];
                    for (int y = 0; y < n; y++)
                    {
                        for (int x = 0; x < n; x++)
                        {
                            var p = new Vector2(x + 0.5f, y + 0.5f);
                            var c = new Vector2(Mathf.Clamp(p.x, rad, n - rad), Mathf.Clamp(p.y, rad, n - rad));
                            float a = Mathf.Clamp01(rad - Vector2.Distance(p, c) + 0.5f);
                            px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                        }
                    }
                    tex.SetPixels32(px);
                    tex.Apply();
                    rounded = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f,
                                            0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
                }
                return rounded;
            }
        }

        // Pakai GameInput milik tim: ia memasang InputSystemUIInputModule DAN memanggil AssignDefaultActions().
        // Tanpa AssignDefaultActions, tombol tidak merespons di build (project ini memakai Input System baru).
        public static void EnsureEventSystem()
        {
            GameInput.EnsureEventSystem();
        }

        public static RectTransform CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Anchor di tepi atas dengan tinggi piksel tetap (untuk isi daftar yang bisa di-scroll).
        public static void TopAnchored(RectTransform rt, float x0, float x1, float yFromTop, float height)
        {
            rt.anchorMin = new Vector2(x0, 1f);
            rt.anchorMax = new Vector2(x1, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -yFromTop);
            rt.sizeDelta = new Vector2(0f, height);
        }

        public static Image Panel(Transform parent, string name, Color color, float x0, float y0, float x1, float y1, bool border)
        {
            var rt = NewRect(parent, name);
            Stretch(rt, x0, y0, x1, y1);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            // Latar layar penuh serta bar/isi bar tetap persegi.
            bool full = x0 <= 0f && y0 <= 0f && x1 >= 1f && y1 >= 1f;
            if (!full && name != "Bar" && name != "Fill")
            {
                img.sprite = RoundedSprite;
                img.type = Image.Type.Sliced;
            }
            if (border) AddBorder(img);
            return img;
        }

        public static void AddBorder(Image img)
        {
            var o = img.gameObject.AddComponent<Outline>();
            o.effectColor = Theme.Line;
            o.effectDistance = new Vector2(2f, -2f);
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align,
                                 float x0, float y0, float x1, float y1)
        {
            var rt = NewRect(parent, "Label");
            Stretch(rt, x0, y0, x1, y1);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 9;
            t.resizeTextMaxSize = size;
            t.raycastTarget = false;
            return t;
        }

        public static Button Btn(Transform parent, string name, string text, Color fill, Color textColor,
                                 float x0, float y0, float x1, float y1, Action onClick, int fontSize)
        {
            var img = Panel(parent, name, fill, x0, y0, x1, y1, true);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            img.gameObject.AddComponent<ButtonFx>();
            var cb = b.colors;
            cb.highlightedColor = new Color(0.92f, 0.96f, 1f, 1f);
            cb.pressedColor = new Color(0.78f, 0.82f, 0.9f, 1f);
            b.colors = cb;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            Label(img.transform, text, fontSize, textColor, TextAnchor.MiddleCenter, 0.04f, 0.04f, 0.96f, 0.96f);
            return b;
        }

        public static void SetBtnText(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        public static void SetBtnFill(Button b, Color c)
        {
            var img = b.GetComponent<Image>();
            if (img != null) img.color = c;
        }

        // Bar proporsi (mis. Stamina). fraction 0..1.
        public static void Bar(Transform parent, float fraction, Color fill, float x0, float y0, float x1, float y1)
        {
            var bg = Panel(parent, "Bar", new Color(0.12f, 0.12f, 0.14f, 1f), x0, y0, x1, y1, false);
            var f = Panel(bg.transform, "Fill", fill, 0f, 0f, Mathf.Clamp01(fraction), 1f, false);
            f.raycastTarget = false;
            bg.raycastTarget = false;
        }

        public static Slider MakeSlider(Transform parent, string name, float x0, float y0, float x1, float y1,
                                        float value, Action<float> onChange)
        {
            var root = NewRect(parent, name);
            Stretch(root, x0, y0, x1, y1);

            var bg = NewRect(root, "Background");
            Stretch(bg, 0f, 0.38f, 1f, 0.62f);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            var fillArea = NewRect(root, "Fill Area");
            Stretch(fillArea, 0f, 0.38f, 1f, 0.62f);
            var fill = NewRect(fillArea, "Fill");
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = Theme.Highlight;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;

            var handleArea = NewRect(root, "Handle Slide Area");
            Stretch(handleArea, 0f, 0f, 1f, 1f);
            handleArea.offsetMin = new Vector2(12f, 0f);
            handleArea.offsetMax = new Vector2(-12f, 0f);
            var handle = NewRect(handleArea, "Handle");
            var hImg = handle.gameObject.AddComponent<Image>();
            hImg.sprite = CircleSprite;
            hImg.color = Theme.Highlight;
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = new Vector2(0f, 1f);
            handle.sizeDelta = new Vector2(24f, 0f);

            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill;
            s.handleRect = handle;
            s.targetGraphic = hImg;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = value;
            if (onChange != null) s.onValueChanged.AddListener(v => onChange(v));
            return s;
        }

        // Area scroll vertikal. content melebar penuh; atur tinggi lewat content.sizeDelta.
        public static ScrollRect Scroll(Transform parent, string name, float x0, float y0, float x1, float y1,
                                        out RectTransform content)
        {
            var root = NewRect(parent, name);
            Stretch(root, x0, y0, x1, y1);
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);
            var sr = root.gameObject.AddComponent<ScrollRect>();

            var vp = NewRect(root, "Viewport");
            Stretch(vp, 0f, 0f, 1f, 1f);
            vp.gameObject.AddComponent<RectMask2D>();

            content = NewRect(vp, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;

            sr.viewport = vp;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;
            return sr;
        }

        // Garis lurus antara dua titik (koordinat piksel dari pojok kiri-bawah induk).
        public static Image Segment(RectTransform parent, Vector2 a, Vector2 b, float thickness, Color color)
        {
            var rt = NewRect(parent, "Seg");
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            Vector2 d = b - a;
            rt.sizeDelta = new Vector2(d.magnitude + 1f, thickness);
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        // Kurva S horizontal (seperti jalur di Slay the Spire).
        public static void Curve(RectTransform parent, Vector2 a, Vector2 b, float thickness, Color color)
        {
            float dx = (b.x - a.x) * 0.5f;
            Vector2 c1 = a + new Vector2(dx, 0f), c2 = b - new Vector2(dx, 0f);
            Vector2 prev = a;
            const int steps = 14;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps, u = 1f - t;
                Vector2 p = u * u * u * a + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * b;
                Segment(parent, prev, p, thickness, color);
                prev = p;
            }
        }

        public static Image Circle(RectTransform parent, string name, Vector2 center, float diameter, Color color)
        {
            var rt = NewRect(parent, name);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(diameter, diameter);
            rt.anchoredPosition = center;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = CircleSprite;
            img.color = color;
            return img;
        }

        public static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}