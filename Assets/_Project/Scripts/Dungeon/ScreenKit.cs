using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small helpers shared by the Dungeon Hub, Voyage and Collection screens.
/// Everything is built from the UI Kit (UIKit, UITheme, ThemedButton, ScreenHeader).
/// Positions use pixels in the 1920x1080 reference, measured from a corner of the parent.
/// </summary>
public static class ScreenKit
{
    public static Color A(Color c, float a) { c.a = a; return c; }

    // From the TOP-LEFT corner of the parent. y grows downward.
    public static void Put(Component c, float x, float y, float w, float h)
    {
        RectTransform rt = (RectTransform)c.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    // From the TOP-RIGHT corner of the parent.
    public static void PutR(Component c, float x, float y, float w, float h)
    {
        RectTransform rt = (RectTransform)c.transform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    // From the CENTRE of the parent. y grows upward.
    public static void PutC(Component c, float x, float y, float w, float h)
    {
        RectTransform rt = (RectTransform)c.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    // Stretch with insets (left, top, right, bottom).
    public static void Fill(Component c, float l, float t, float r, float b)
    {
        RectTransform rt = (RectTransform)c.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    // Full-screen canvas with the navy background and a toast.
    public static Canvas NewScreen(string name)
    {
        Canvas c = UIKit.CreateOverlayCanvas(name, 0);
        Image bg = UIKit.NewImage("Background", c.transform, null, Image.Type.Simple, UITheme.NavyDeep, true);
        UIKit.Stretch(bg.rectTransform);
        Image band = UIKit.NewImage("Sea Band", c.transform, null, Image.Type.Simple, A(UITheme.Navy, 0.55f), false);
        band.rectTransform.anchorMin = new Vector2(0f, 0.55f);
        band.rectTransform.anchorMax = new Vector2(1f, 1f);
        band.rectTransform.offsetMin = Vector2.zero;
        band.rectTransform.offsetMax = Vector2.zero;
        c.gameObject.AddComponent<ScreenToast>().Build(c.transform);
        return c;
    }

    public static void Toast(Canvas c, string msg)
    {
        if (c == null) return;
        ScreenToast t = c.GetComponent<ScreenToast>();
        if (t != null) t.Show(msg);
    }

    public static ScreenHeader Header(Transform parent, string title, UnityAction onBack)
    {
        ScreenHeader h = UIKit.CreateScreenHeader(parent);
        h.Setup(title, onBack);
        return h;
    }

    public static TMP_Text Text(Transform parent, string name, string text, UITextStyle style,
                                Color color, TextAlignmentOptions align, float size = 0f)
    {
        TMP_Text t = UIKit.CreateText(parent, name, text, style, color, align);
        if (size > 0f) t.fontSize = size;
        return t;
    }

    // Text that shrinks to fit its box (names) and never overflows.
    public static TMP_Text FitText(Transform parent, string name, string text, UITextStyle style,
                                   Color color, TextAlignmentOptions align, float size)
    {
        TMP_Text t = Text(parent, name, text, style, color, align, size);
        t.enableAutoSizing = true;
        t.fontSizeMax = size;
        t.fontSizeMin = Mathf.Max(12f, size * 0.5f);
        t.overflowMode = TextOverflowModes.Ellipsis;
        return t;
    }

    // 9-slice panel from the Kit sprites: "panel", "plate", "panel_cream", "pill".
    public static Image Panel(Transform parent, string name, string sprite, Color color, bool raycast)
    {
        return UIKit.NewImage(name, parent, sprite, Image.Type.Sliced, color, raycast);
    }

    public static Image Box(Transform parent, string name, Color color)
    {
        return UIKit.NewImage(name, parent, null, Image.Type.Simple, color, false);
    }

    // Art slot: object named "<slot> (replace sprite)". Shows real art when the PNG exists.
    // Without art it shows the placeholder sprite in the tint colour.
    public static Image Slot(Transform parent, string slot, string placeholderSprite, Color tint)
    {
        Sprite art = SlotArt.Load(slot);
        Image img = UIKit.NewImage(SlotArt.SlotName(slot), parent, placeholderSprite, Image.Type.Sliced, tint, false);
        if (art != null)
        {
            img.sprite = art;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
        }
        return img;
    }

    public static Image Ring(Transform parent, string name, float thickness, Color color)
    {
        Image img = UIKit.NewImage(name, parent, null, Image.Type.Simple, color, false);
        img.sprite = SlotArt.Ring(thickness);
        return img;
    }

    public static Image Disc(Transform parent, string name, Color color)
    {
        Image img = UIKit.NewImage(name, parent, null, Image.Type.Simple, color, false);
        img.sprite = SlotArt.Disc();
        return img;
    }

    // Adds a child "Glow" (fades in on hover) to a card.
    public static CardGlow AddGlow(Component card, Color color, float pad)
    {
        Image g = UIKit.NewImage("Glow", card.transform, "glow", Image.Type.Sliced, A(color, 0f), false);
        UIKit.Stretch(g.rectTransform, pad);
        g.transform.SetAsFirstSibling();
        CardGlow hg = card.gameObject.AddComponent<CardGlow>();
        hg.glow = g;
        hg.baseColor = color;
        return hg;
    }

    public static Button Click(Image img, UnityAction act)
    {
        img.raycastTarget = true;
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.None;
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        b.navigation = nav;
        b.onClick.AddListener(act);
        return b;
    }

    public static void Bar(Transform parent, string name, float x, float y, float w, float h, float frac, Color fill)
    {
        Image track = UIKit.NewImage(name, parent, "pill", Image.Type.Sliced, UITheme.SliderTrack, false);
        Put(track, x, y, w, h);
        frac = Mathf.Clamp01(frac);
        if (frac <= 0.001f) return;
        Image f = UIKit.NewImage("Fill", track.transform, "pill", Image.Type.Sliced, fill, false);
        f.rectTransform.anchorMin = Vector2.zero;
        f.rectTransform.anchorMax = new Vector2(frac, 1f);
        f.rectTransform.offsetMin = Vector2.zero;
        f.rectTransform.offsetMax = Vector2.zero;
    }

    public static void Corners(Transform parent, float inset)
    {
        string[] names = { "TL", "TR", "BL", "BR" };
        float[] px = { 1f, -1f, 1f, -1f };
        float[] py = { -1f, -1f, 1f, 1f };
        Vector2[] anchors = { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
        for (int i = 0; i < 4; i++)
        {
            Image img = UIKit.NewImage("Corner " + names[i] + " (replace sprite)", parent, "corner", Image.Type.Simple, Color.white, false);
            UIKit.Place(img.rectTransform, anchors[i], new Vector2(0.5f, 0.5f), new Vector2(px[i] * inset, py[i] * inset), new Vector2(40f, 40f));
            img.rectTransform.localScale = new Vector3(px[i], py[i], 1f);
        }
    }

    // Vertical scroll list. Add a layout group to sr.content yourself.
    public static ScrollRect Scroll(Transform parent, string name, float x, float y, float w, float h)
    {
        RectTransform root = UIKit.NewRect(name, parent);
        Put(root, x, y, w, h);
        Image hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0f);
        hit.raycastTarget = true;
        ScrollRect sr = root.gameObject.AddComponent<ScrollRect>();

        RectTransform vp = UIKit.NewRect("Viewport", root);
        UIKit.Stretch(vp);
        vp.offsetMax = new Vector2(-30f, 0f);
        vp.gameObject.AddComponent<RectMask2D>();

        RectTransform content = UIKit.NewRect("Content", vp);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        RectTransform barRt = UIKit.NewRect("Scrollbar", root);
        barRt.anchorMin = new Vector2(1f, 0f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(1f, 0.5f);
        barRt.anchoredPosition = Vector2.zero;
        barRt.sizeDelta = new Vector2(16f, 0f);
        Image track = barRt.gameObject.AddComponent<Image>();
        track.sprite = UIArt.Get("pill");
        track.type = Image.Type.Sliced;
        track.color = UITheme.SliderTrack;
        Scrollbar sb = barRt.gameObject.AddComponent<Scrollbar>();
        RectTransform area = UIKit.NewRect("Sliding Area", barRt);
        UIKit.Stretch(area);
        Image handle = UIKit.NewImage("Handle", area, "pill", Image.Type.Sliced, UITheme.Gold, true);
        UIKit.Stretch(handle.rectTransform);
        sb.handleRect = handle.rectTransform;
        sb.targetGraphic = handle;
        sb.direction = Scrollbar.Direction.BottomToTop;

        sr.viewport = vp;
        sr.content = content;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40f;
        sr.verticalScrollbar = sb;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        return sr;
    }

    // Removes all children (inactive at once so layout groups ignore them).
    public static void Clear(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            GameObject go = t.GetChild(i).gameObject;
            go.SetActive(false);
            UnityEngine.Object.Destroy(go);
        }
    }

    public static string Initials(string s)
    {
        if (string.IsNullOrEmpty(s)) return "?";
        string[] parts = s.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        string r = "";
        for (int i = 0; i < parts.Length && r.Length < 2; i++)
        {
            string w = parts[i];
            if (w == "of" || w == "the" || w == "a" || w == "and") continue;
            r += char.ToUpperInvariant(w[0]);
        }
        return r.Length == 0 ? "?" : r;
    }

    public static void BottomLine(Transform parent, float yFromBottom)
    {
        Image line = UIKit.NewImage("Footer Line", parent, null, Image.Type.Simple, UITheme.Bronze, false);
        RectTransform rt = line.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, yFromBottom);
        rt.sizeDelta = new Vector2(0f, 2f);
    }
}
