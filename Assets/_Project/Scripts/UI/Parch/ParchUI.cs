using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Palet tema "Warm Parchment Gouache" untuk Hub dan layar turunannya. Teks selalu DarkBrown.
    public static class Parch
    {
        public static readonly Color Parchment = Theme.Hex("#F7EEDC");
        public static readonly Color Terracotta = Theme.Hex("#C97A52");
        public static readonly Color Olive = Theme.Hex("#6B8F5B");
        public static readonly Color DarkBrown = Theme.Hex("#4B3B2E");
        public static readonly Color Gold = Theme.Hex("#EBB24E");
        public static readonly Color Muted = Theme.Hex("#6B5846");   // lebih gelap supaya teks kedua mudah dibaca
        public static readonly Color Danger = Theme.Hex("#B5483A");
        public static readonly Color Disabled = new Color(0.55f, 0.5f, 0.45f, 1f);
    }
}

namespace Oddyseus.UI
{
    // Tombol bergambar: simpan warna dasar dan label supaya SetEnabled bisa meredupkan keduanya.
    public class UiBtn : MonoBehaviour
    {
        public Image Img;
        public Text Label;
        public Color Tint = Color.white;
    }

    // Kartu Pack (gulungan): hover = ganti sprite, ditekan = sedikit mengecil.
    public class ScrollCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image Img;
        public Sprite Normal, Hover;
        public Transform Scaled;      // yang diskalakan saat ditekan (seluruh kartu); kosong = objek ini
        bool over, down;

        Transform Target { get { return Scaled != null ? Scaled : transform; } }

        void Apply()
        {
            if (Hover != null) Img.sprite = over ? Hover : Normal;
            Target.localScale = down ? new Vector3(0.97f, 0.97f, 1f) : Vector3.one;
        }
        public void OnPointerEnter(PointerEventData e) { over = true; Apply(); }
        public void OnPointerExit(PointerEventData e) { over = false; down = false; Apply(); }
        public void OnPointerDown(PointerEventData e) { down = true; Apply(); }
        public void OnPointerUp(PointerEventData e) { down = false; Apply(); }
        void OnDisable() { over = false; down = false; if (Img != null) { Img.sprite = Normal; Target.localScale = Vector3.one; } }
    }

    // Deretan tab (Legends/Costumes, Floor 1..4, ...) pakai sprite tab_selected / tab_unselected.
    // Hover, pressed, dan disabled lewat ColorTint karena aset tab hanya punya dua sprite.
    public class TabBar
    {
        public readonly RectTransform Root;
        readonly Image[] imgs;
        readonly Text[] labels;
        readonly Action<int> onChange;
        public int Selected { get; private set; }

        public TabBar(Transform parent, string[] names, float x0, float y0, float x1, float y1, Action<int> onChange, int fontSize = 22)
        {
            this.onChange = onChange;
            Root = UIFactory.NewRect(parent, "Tabs");
            UIFactory.Stretch(Root, x0, y0, x1, y1);
            int n = names.Length;
            imgs = new Image[n];
            labels = new Text[n];
            float w = 1f / n;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                var img = ParchUI.Sliced(Root, "Tab" + i, "buttons/tab_unselected", i * w + 0.004f, 0f, (i + 1) * w - 0.004f, 1f, 0.5f);
                img.raycastTarget = true;
                var b = img.gameObject.AddComponent<Button>();
                b.targetGraphic = img;
                b.navigation = new Navigation { mode = Navigation.Mode.None };
                var cb = b.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1f, 0.95f, 0.82f, 1f);
                cb.pressedColor = new Color(0.8f, 0.7f, 0.56f, 1f);
                cb.selectedColor = Color.white;
                cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
                cb.fadeDuration = 0.08f;
                b.colors = cb;
                b.onClick.AddListener(() => Select(k, true));
                imgs[i] = img;
                labels[i] = ParchUI.Txt(img.transform, names[i], fontSize, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.06f, 0.08f, 0.94f, 0.92f, true);
            }
            Selected = -1;
        }

        public void Select(int i, bool notify)
        {
            Selected = i;
            for (int k = 0; k < imgs.Length; k++)
            {
                imgs[k].sprite = UISprites.Get(k == i ? "buttons/tab_selected" : "buttons/tab_unselected");
                labels[k].color = k == i ? Parch.DarkBrown : Parch.Muted;
            }
            if (notify && onChange != null) onChange(i);
        }
    }

    // Kumpulan pembuat widget bergambar. Semua posisi = anchor 0..1 relatif terhadap induk (sama seperti UIFactory).
    public static class ParchUI
    {
        // ---------- dasar ----------

        // Kotak 16:9 terbesar yang muat di layar. Semua isi layar dibangun di dalamnya sehingga tata letak sama
        // di 16:9 dan tidak pecah di rasio lain (latar tetap memenuhi layar).
        public static RectTransform SafeArea(Transform canvas)
        {
            var rt = UIFactory.NewRect(canvas, "SafeArea");
            UIFactory.Stretch(rt, 0f, 0f, 1f, 1f);
            var f = rt.gameObject.AddComponent<AspectRatioFitter>();
            f.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            f.aspectRatio = 16f / 9f;
            return rt;
        }

        public static Image Pic(Transform parent, string name, string sprite, float x0, float y0, float x1, float y1, bool preserve = true)
        {
            var rt = UIFactory.NewRect(parent, name);
            UIFactory.Stretch(rt, x0, y0, x1, y1);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISprites.Get(sprite);
            img.preserveAspect = preserve;
            img.raycastTarget = false;
            return img;
        }

        // Gambar yang selalu pas di dalam kotak dengan rasio aslinya (rata tengah). Cocok untuk chip, ribbon, gulungan.
        // Anak-anaknya memakai anchor relatif terhadap gambar itu sendiri, jadi teks tidak bergeser.
        public static Image FitPic(Transform parent, string name, string sprite, float x0, float y0, float x1, float y1)
        {
            var box = UIFactory.NewRect(parent, name + "Box");
            UIFactory.Stretch(box, x0, y0, x1, y1);
            var rt = UIFactory.NewRect(box, name);
            UIFactory.Stretch(rt, 0f, 0f, 1f, 1f);
            var s = UISprites.Get(sprite);
            var fit = rt.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = s != null ? s.rect.width / s.rect.height : 1f;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s;
            img.raycastTarget = false;
            return img;
        }

        // Kotak kosong dengan rasio tetap (lebar/tinggi), rata tengah di dalam area. Dipakai kalau beberapa anak harus
        // berbagi satu bidang gambar (mis. lukisan Pack di belakang gulungan).
        public static RectTransform FitRect(Transform parent, string name, float x0, float y0, float x1, float y1, float aspect)
        {
            var box = UIFactory.NewRect(parent, name + "Box");
            UIFactory.Stretch(box, x0, y0, x1, y1);
            var rt = UIFactory.NewRect(box, name);
            UIFactory.Stretch(rt, 0f, 0f, 1f, 1f);
            var fit = rt.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = aspect;
            return rt;
        }

        // Baris "Label  nilai" (label redup, nilai gelap) dalam satu Text.
        public static Text Field(Transform parent, string label, string value, int size, float x0, float y0, float x1, float y1,
                                 TextAnchor align = TextAnchor.UpperLeft)
        {
            string hex = ColorUtility.ToHtmlStringRGB(Parch.Muted);
            return Txt(parent, "<color=#" + hex + ">" + label + "</color>  " + value, size, Parch.DarkBrown, align, x0, y0, x1, y1);
        }

        // Frame 9-slice. borderScale < 1 mengecilkan dekorasi sudut (border asli dihitung di UIAssetImporter).
        // Catatan Unity: pixelsPerUnitMultiplier BESAR = border KECIL, jadi nilainya harus 1 / borderScale.
        public static Image Sliced(Transform parent, string name, string sprite, float x0, float y0, float x1, float y1, float borderScale)
        {
            var rt = UIFactory.NewRect(parent, name);
            UIFactory.Stretch(rt, x0, y0, x1, y1);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISprites.Get(sprite);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.05f, borderScale);
            img.raycastTarget = false;
            return img;
        }

        public static Image Panel9(Transform parent, string name, float x0, float y0, float x1, float y1, float borderScale = 0.5f)
        {
            return Sliced(parent, name, "frames/frame_panel_large", x0, y0, x1, y1, borderScale);
        }

        public static Image Solid(Transform parent, string name, Color c, float x0, float y0, float x1, float y1)
        {
            var rt = UIFactory.NewRect(parent, name);
            UIFactory.Stretch(rt, x0, y0, x1, y1);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISprites.White;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        // Teks asli (tidak pernah dipanggang ke gambar). title = Cinzel bold, selain itu EB Garamond.
        // Satu tempat untuk memperbesar SEMUA teks. 1.0 = ukuran lama, 1.15 = 15% lebih besar.
        public static float TextScale = 1.15f;

        public static Text Txt(Transform parent, string text, int size, Color color, TextAnchor align,
                               float x0, float y0, float x1, float y1, bool title = false)
        {
            var t = UIFactory.Label(parent, text, Mathf.RoundToInt(size * TextScale), color, align, x0, y0, x1, y1);
            Font body = UISprites.BodyFont;
            if (body != null) t.font = body;
            if (title)
            {
                var f = UISprites.TitleFont;
                if (f != null) t.font = f;
            }
            t.fontStyle = FontStyle.Bold;   // semua teks tebal supaya jelas di atas parchment
            // Teks tidak boleh keluar kotak: mengecil otomatis kalau terlalu panjang.
            t.resizeTextForBestFit = true;
            t.resizeTextMaxSize = t.fontSize;
            t.resizeTextMinSize = Mathf.Max(9, Mathf.RoundToInt(size * 0.65f));
            return t;
        }

        // ---------- tombol ----------

        // Tombol pill: set = "parchment" | "terracotta" | "alt". Empat state (normal, hover, pressed, disabled) lewat SpriteSwap.
        // tint dipakai untuk tombol emas (set parchment diwarnai emas).
        public static Button Pill(Transform parent, string name, string label, string set, Color tint,
                                  float x0, float y0, float x1, float y1, Action onClick, int fontSize = 22, float borderScale = 0.55f)
        {
            string p = "buttons/btn_" + set + "_";
            var img = Sliced(parent, name, p + "normal", x0, y0, x1, y1, borderScale);
            img.color = tint;
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState
            {
                highlightedSprite = UISprites.Get(p + "hover"),
                pressedSprite = UISprites.Get(p + "pressed"),
                selectedSprite = UISprites.Get(p + "normal"),
                disabledSprite = UISprites.Get(p + "disabled"),
            };
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            if (onClick != null) b.onClick.AddListener(() => onClick());
            var t = Txt(img.transform, label, fontSize, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.08f, 0.12f, 0.92f, 0.88f, true);
            var meta = img.gameObject.AddComponent<UiBtn>();
            meta.Img = img; meta.Label = t; meta.Tint = tint;
            return b;
        }

        public static Button Pill(Transform parent, string name, string label, string set,
                                  float x0, float y0, float x1, float y1, Action onClick, int fontSize = 22, float borderScale = 0.55f)
        {
            return Pill(parent, name, label, set, Color.white, x0, y0, x1, y1, onClick, fontSize, borderScale);
        }

        public static readonly Color GoldTint = new Color(0.98f, 0.80f, 0.30f, 1f);

        public static void SetEnabled(Button b, bool on)
        {
            b.interactable = on;
            var m = b.GetComponent<UiBtn>();
            if (m == null) return;
            m.Img.color = on ? m.Tint : Color.Lerp(m.Tint, new Color(0.62f, 0.58f, 0.54f, 1f), 0.65f);
            m.Label.color = on ? Parch.DarkBrown : Parch.Disabled;
        }

        // Tombol dari satu sprite (menu kiri hub, kartu, slot). Hover/pressed/disabled lewat ColorTint.
        public static Button TintButton(Image img, Action onClick)
        {
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.96f, 0.84f, 1f);
            cb.pressedColor = new Color(0.8f, 0.7f, 0.55f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.75f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        // Tombol menu hub: frame_menu_button + ikon + judul + subjudul (semuanya teks asli).
        public static Button MenuButton(Transform parent, string name, string icon, string title, string subtitle,
                                        float x0, float y0, float x1, float y1, Action onClick)
        {
            var img = Sliced(parent, name, "frames/frame_menu_button", x0, y0, x1, y1, 0.6f);
            var b = TintButton(img, onClick);
            // Isi tombol berada di dalam dekorasi sudut (x 0.10 - 0.88); ikon tidak digepengkan.
            FitPic(img.transform, "Icon", icon, 0.10f, 0.14f, 0.25f, 0.86f);
            Txt(img.transform, title, 26, Parch.DarkBrown, TextAnchor.LowerLeft, 0.27f, 0.48f, 0.88f, 0.92f, true);
            Txt(img.transform, subtitle, 18, Parch.DarkBrown, TextAnchor.UpperLeft, 0.27f, 0.08f, 0.88f, 0.50f);
            return b;
        }

        // ---------- bar ----------

        // Bar progres: track bergambar + isi emas yang dihitung di kode (aset "filled" sudah mengandung isi tetap).
        public static Image ProgressBar(Transform parent, float fraction, float x0, float y0, float x1, float y1, float borderScale = 0.4f)
        {
            var track = Sliced(parent, "Bar", "bars/progress_track_empty", x0, y0, x1, y1, borderScale);
            var inner = UIFactory.NewRect(track.transform, "Inner");
            UIFactory.Stretch(inner, 0.12f, 0.30f, 0.88f, 0.70f);
            var fill = Solid(inner, "Fill", Parch.Gold, 0f, 0f, Mathf.Clamp01(fraction), 1f);
            return fill;
        }

        public static void SetBar(Image fill, float fraction)
        {
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }

        // ---------- daftar scroll ----------

        public static ScrollRect VScroll(Transform parent, string name, float x0, float y0, float x1, float y1, out RectTransform content)
        {
            var root = UIFactory.NewRect(parent, name);
            UIFactory.Stretch(root, x0, y0, x1, y1);
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var sr = root.gameObject.AddComponent<ScrollRect>();

            var vp = UIFactory.NewRect(root, "Viewport");
            UIFactory.Stretch(vp, 0f, 0f, 0.972f, 1f);
            vp.gameObject.AddComponent<RectMask2D>();

            content = UIFactory.NewRect(vp, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;

            var track = Sliced(root, "Scrollbar", "bars/scrollbar_track_thin", 0.978f, 0f, 1f, 1f, 0.6f);
            track.raycastTarget = true;
            var sb = track.gameObject.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            var area = UIFactory.NewRect(track.transform, "SlidingArea");
            UIFactory.Stretch(area, 0f, 0.01f, 1f, 0.99f);
            var handle = Sliced(area, "Handle", "bars/scrollbar_thumb", 0f, 0f, 1f, 1f, 0.6f);
            handle.raycastTarget = true;
            sb.handleRect = handle.rectTransform;
            sb.targetGraphic = handle;

            sr.viewport = vp;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;
            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return sr;
        }

        // ---------- latar dan judul ----------

        // parchment_base + vignette di atasnya (sesuai tata letak tiap layar). Memenuhi seluruh layar tanpa distorsi.
        public static RectTransform Background(Transform canvas, bool topRight, bool bottomLeft, bool bottomRight, bool leftEdge)
        {
            var bg = UIFactory.NewRect(canvas, "Background");
            UIFactory.Stretch(bg, 0f, 0f, 1f, 1f);
            var baseRt = UIFactory.NewRect(bg, "Base");
            UIFactory.Stretch(baseRt, 0f, 0f, 1f, 1f);
            var fit = baseRt.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 1672f / 940f;
            var bi = baseRt.gameObject.AddComponent<Image>();
            bi.sprite = UISprites.Get("backgrounds/parchment_base");
            bi.raycastTarget = false;

            if (topRight) Pic(bg, "VignetteTR", "backgrounds/vignette_top_right", 0.58f, 0.45f, 1f, 1f, false);
            if (bottomLeft) Pic(bg, "VignetteBL", "backgrounds/vignette_bottom_left_a", 0f, 0f, 0.42f, 0.55f, false);
            if (bottomRight) Pic(bg, "VignetteBR", "backgrounds/vignette_bottom_right", 0.58f, 0f, 1f, 0.55f, false);
            if (leftEdge) Pic(bg, "VignetteL", "backgrounds/vignette_left_edge", 0f, 0f, 0.26f, 1f, false);
            return bg;
        }

        // Bar judul sesuai mockup: judul besar (Cinzel) diapit daun, tombol BACK di kiri, lalu pita Greek-key di bawahnya.
        // redStrip: pita terracotta (Armory, Achievements) atau cokelat (Hub, Compendium, Legends).
        // Greek-key strip: repeated (tiled) at its real height so the pattern is not stretched.
        public static void Strip(Transform parent, string name, string sprite, float x0, float y0, float x1, float y1)
        {
            var sp = UISprites.Get(sprite);
            if (sp == null) return;
            var rt = UIFactory.NewRect(parent, name);
            UIFactory.Stretch(rt, x0, y0, x1, y1);
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            rt.gameObject.AddComponent<TileStrip>().Setup(sp);
        }

        public static void TitleBar(Transform parent, string title, Action onBack, bool redStrip = false)
        {
            Strip(parent, "KeyStrip", redStrip ? "ornaments/key_strip_terracotta" : "ornaments/key_strip_brown", 0f, 0.862f, 1f, 0.906f);
            // Daun mengikuti panjang judul supaya tidak menimpa huruf (judul panjang seperti PACK COMPENDIUM).
            float half = 0.011f * title.Length + 0.03f;
            Pic(parent, "SprigL", "ornaments/sprig_left", 0.5f - half - 0.085f, 0.915f, 0.5f - half, 0.99f);
            Pic(parent, "SprigR", "ornaments/sprig_right", 0.5f + half, 0.915f, 0.5f + half + 0.085f, 0.99f);
            Txt(parent, title, 48, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.5f - half, 0.91f, 0.5f + half, 0.995f, true);
            if (onBack != null)
            {
                var b = Pill(parent, "Back", "BACK", "parchment", 0.045f, 0.925f, 0.17f, 0.985f, onBack, 22, 0.5f);
                var m = b.GetComponent<UiBtn>();
                m.Label.rectTransform.anchorMin = new Vector2(0.32f, 0.12f);
                Pic(b.transform, "Icon", "icons/icon_ui_back", 0.12f, 0.18f, 0.32f, 0.82f);
            }
        }

        // ---------- chip dan lencana ----------

        public static void RoleChip(Transform parent, Role role, float x0, float y0, float x1, float y1)
        {
            string key = "chip_tank";
            switch (role)
            {
                case Role.DPS: key = "chip_dps"; break;
                case Role.Healer: key = "chip_healer"; break;
                case Role.DoTApplier: key = "chip_dot"; break;
                case Role.BufferDebuffer: key = "chip_buffer"; break;
            }
            var img = FitPic(parent, "RoleChip", "chips_badges/" + key, x0, y0, x1, y1);
            var lbl = Txt(img.transform, UnitData.RoleLabel(role), 14, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.42f, 0.18f, 0.88f, 0.82f, true);
            lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 9; lbl.resizeTextMaxSize = 14;
        }

        public static void RewardBadge(Transform parent, RewardKind type, float x0, float y0, float x1, float y1)
        {
            var img = FitPic(parent, "Badge", "chips_badges/badge_" + type.ToString().ToLowerInvariant(), x0, y0, x1, y1);
            var lbl = Txt(img.transform, type.ToString(), 20, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.40f, 0.10f, 0.95f, 0.90f, true);
            lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 10; lbl.resizeTextMaxSize = 24;
        }

        // Penghitung (mis. "Found 1/20") di atas pil parchment supaya terbaca di atas vignette.
        public static void Counter(Transform parent, string text, int size, float x0, float y0, float x1, float y1)
        {
            var pill = Sliced(parent, "Counter", "buttons/btn_parchment_normal", x0, y0, x1, y1, 0.55f);
            pill.color = new Color(1f, 0.97f, 0.9f, 0.92f);
            Txt(pill.transform, text, size, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.06f, 0.1f, 0.94f, 0.9f, true);
        }

        // Ribbon bernama (teks asli di tengah).
        public static Image Ribbon(Transform parent, string color, string text, int size,
                                   float x0, float y0, float x1, float y1)
        {
            var img = FitPic(parent, "Ribbon", "ribbons/ribbon_" + color, x0, y0, x1, y1);
            Txt(img.transform, text, size, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.17f, 0.30f, 0.83f, 0.78f, true);
            return img;
        }
    }
}

namespace Oddyseus.UI
{
    // Grid yang lebar selnya mengikuti lebar kontainer (jumlah kolom tetap, rasio tinggi/lebar tetap).
    // Pasang pada Content di dalam ScrollRect bersama ContentSizeFitter (vertical = PreferredSize).
    [RequireComponent(typeof(GridLayoutGroup))]
    public class ResponsiveGrid : MonoBehaviour
    {
        public int Cols = 5;
        public float Spacing = 12f;
        public float Aspect = 1.4f;     // tinggi / lebar
        public float Pad = 8f;
        GridLayoutGroup g;
        RectTransform rt;

        void OnEnable() { Apply(); }
        void Start() { Apply(); }
        void OnRectTransformDimensionsChange() { Apply(); }

        public void Apply()
        {
            if (g == null) g = GetComponent<GridLayoutGroup>();
            if (rt == null) rt = transform as RectTransform;
            if (g == null || rt == null) return;
            float w = rt.rect.width;
            if (w <= 1f) return;
            float cell = (w - 2f * Pad - Spacing * (Cols - 1)) / Cols;
            var size = new Vector2(cell, cell * Aspect);
            if ((g.cellSize - size).sqrMagnitude < 0.01f && g.constraintCount == Cols) return;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = Cols;
            g.spacing = new Vector2(Spacing, Spacing);
            g.padding = new RectOffset((int)Pad, (int)Pad, (int)Pad, (int)Pad);
            g.cellSize = size;
            LayoutRebuilder.MarkLayoutForRebuild(rt);
        }
    }
}