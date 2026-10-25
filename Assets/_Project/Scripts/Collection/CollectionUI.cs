using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Layout helpers shared by the collection screens. All positions are in the 1920x1080 reference
/// canvas, measured from the TOP-LEFT corner (x to the right, y downwards), pivot at the top-left.
/// Built on the UI Kit (UIKit, UIArt, UITheme, ThemedButton).
/// </summary>
public static class CollectionUI
{
    public static readonly Color TealTag = UITheme.Hex("2F7F7A");
    public static readonly Color GoldTag = UITheme.Hex("A8793A");
    public static readonly Color Locked = UITheme.Hex("7C8AA0");
    public static readonly Color Warning = UITheme.Hex("E0605A");

    // ---------- Rect placement ----------
    public static RectTransform TL(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    // Top-right anchored: 'right' is the distance from the right edge.
    public static RectTransform TR(RectTransform rt, float right, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-right, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    public static RectTransform FillWithMargins(RectTransform rt, float l, float t, float r, float b)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
        return rt;
    }

    // ---------- Text ----------
    public static TMP_Text Text(Transform parent, string name, string text, UITextStyle style, Color color,
                                TextAlignmentOptions align, float x, float y, float w, float h)
    {
        TMP_Text t = UIKit.CreateText(parent, name, text, style, color, align);
        TL(t.rectTransform, x, y, w, h);
        return t;
    }

    public static TMP_Text TextRight(Transform parent, string name, string text, UITextStyle style, Color color,
                                     TextAlignmentOptions align, float right, float y, float w, float h)
    {
        TMP_Text t = UIKit.CreateText(parent, name, text, style, color, align);
        TR(t.rectTransform, right, y, w, h);
        return t;
    }

    public static void AutoSize(TMP_Text t, float min, float max)
    {
        t.enableAutoSizing = true;
        t.fontSizeMin = min;
        t.fontSizeMax = max;
    }

    // ---------- Images ----------
    public static Image Panel(Transform parent, string name, float x, float y, float w, float h, string sprite = "panel")
    {
        Image i = UIKit.NewImage(name, parent, sprite, Image.Type.Sliced, Color.white, false);
        TL(i.rectTransform, x, y, w, h);
        return i;
    }

    public static Image Solid(Transform parent, string name, float x, float y, float w, float h, Color c)
    {
        Image i = UIKit.NewImage(name, parent, null, Image.Type.Simple, c, false);
        TL(i.rectTransform, x, y, w, h);
        return i;
    }

    public static Image Divider(Transform parent, string name, float x, float y, float w, Color c)
    {
        return Solid(parent, name, x, y, w, 2f, c);
    }

    // Rounded pill with a coloured label. Art slot: the pill sprite.
    public static Image Pill(Transform parent, string name, float x, float y, float w, float h,
                             string text, Color color, float fontSize)
    {
        Color bg = color;
        bg.a = 0.30f;
        Image p = UIKit.NewImage(name + " (replace sprite)", parent, "pill", Image.Type.Sliced, bg, false);
        TL(p.rectTransform, x, y, w, h);
        TMP_Text l = UIKit.CreateText(p.transform, "Label", text, UITextStyle.Small, color, TextAlignmentOptions.Center);
        UIKit.Stretch(l.rectTransform);
        l.fontSize = fontSize;
        return p;
    }

    public static void SetPill(Image pill, string text, Color color)
    {
        if (pill == null) return;
        Color bg = color;
        bg.a = 0.30f;
        pill.color = bg;
        TMP_Text l = pill.GetComponentInChildren<TMP_Text>(true);
        if (l != null) { l.text = text; l.color = color; }
    }

    // ---------- Buttons ----------
    public static ThemedButton NewButton(Transform parent, string text, ThemedButtonStyle style,
                                      float x, float y, float w, float h)
    {
        ThemedButton b = UIKit.CreateButton(parent, text, style, new Vector2(w, h));
        TL((RectTransform)b.transform, x, y, w, h);
        return b;
    }

    // ---------- Bars ----------
    // Bar root has two children: "Track (replace sprite)" and "Fill (replace sprite)".
    public static RectTransform Bar(Transform parent, string name, float x, float y, float w, float h, Color fill)
    {
        RectTransform root = UIKit.NewRect(name, parent);
        TL(root, x, y, w, h);
        Image track = UIKit.NewImage("Track (replace sprite)", root, null, Image.Type.Simple, UITheme.SliderTrack, false);
        UIKit.Stretch(track.rectTransform);
        Image f = UIKit.NewImage("Fill (replace sprite)", root, null, Image.Type.Simple, fill, false);
        RectTransform fr = f.rectTransform;
        fr.anchorMin = new Vector2(0f, 0f);
        fr.anchorMax = new Vector2(0f, 1f);
        fr.pivot = new Vector2(0f, 0.5f);
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;
        return root;
    }

    public static void SetBar(Transform barRoot, float fraction)
    {
        if (barRoot == null) return;
        Transform f = barRoot.Find("Fill (replace sprite)");
        if (f == null) return;
        RectTransform fr = (RectTransform)f;
        fr.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;
    }

    public static void SetBarColor(Transform barRoot, Color c)
    {
        if (barRoot == null) return;
        Transform f = barRoot.Find("Fill (replace sprite)");
        if (f == null) return;
        Image i = f.GetComponent<Image>();
        if (i != null) i.color = c;
    }

    // ---------- Glow + card ----------
    // Adds the child "Glow" (fades in on hover) and the HoverGlow component to 'owner'.
    public static Image AddGlow(Transform owner, float pad, Color color)
    {
        Color clear = color;
        clear.a = 0f;
        Image g = UIKit.NewImage("Glow", owner, "glow", Image.Type.Sliced, clear, false);
        UIKit.Stretch(g.rectTransform, pad);
        g.transform.SetAsFirstSibling();
        HoverGlow hg = owner.GetComponent<HoverGlow>();
        if (hg == null) hg = owner.gameObject.AddComponent<HoverGlow>();
        hg.glow = g;
        hg.color = color;
        return g;
    }

    // A clickable card: Glow (behind), Frame (9-slice panel), and a Button component.
    public static RectTransform Card(Transform parent, string name, float w, float h)
    {
        RectTransform rt = UIKit.NewRect(name, parent);
        rt.sizeDelta = new Vector2(w, h);
        AddGlow(rt, 18f, UITheme.Gold);
        Image frame = UIKit.NewImage("Frame (replace sprite)", rt, "panel", Image.Type.Sliced, Color.white, true);
        UIKit.Stretch(frame.rectTransform);
        Button b = rt.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.targetGraphic = frame;
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        b.navigation = nav;
        return rt;
    }

    // ---------- Scroll view ----------
    // Creates a vertical ScrollRect. 'content' stretches across the viewport width and grows downwards
    // (caller adds a layout group to it). Scrollbar art slots: "Scrollbar Track (replace sprite)",
    // "Scrollbar Handle (replace sprite)".
    public static ScrollRect Scroll(Transform parent, string name, float x, float y, float w, float h,
                                    bool withBar, out RectTransform content)
    {
        RectTransform root = UIKit.NewRect(name, parent);
        TL(root, x, y, w, h);
        ScrollRect sr = root.gameObject.AddComponent<ScrollRect>();

        RectTransform vp = UIKit.NewRect("Viewport", root);
        vp.anchorMin = Vector2.zero;
        vp.anchorMax = Vector2.one;
        vp.offsetMin = Vector2.zero;
        vp.offsetMax = new Vector2(withBar ? -30f : 0f, 0f);
        Image vpImg = vp.gameObject.AddComponent<Image>();
        vpImg.color = new Color(0f, 0f, 0f, 0f);
        vpImg.raycastTarget = true;
        vp.gameObject.AddComponent<RectMask2D>();

        content = UIKit.NewRect("Content", vp);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = vp;
        sr.content = content;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 35f;

        if (withBar)
        {
            RectTransform sbRt = UIKit.NewRect("Scrollbar", root);
            sbRt.anchorMin = new Vector2(1f, 0f);
            sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 0.5f);
            sbRt.anchoredPosition = Vector2.zero;
            sbRt.sizeDelta = new Vector2(22f, 0f);
            Image trackImg = sbRt.gameObject.AddComponent<Image>();
            trackImg.color = new Color(UITheme.SliderTrack.r, UITheme.SliderTrack.g, UITheme.SliderTrack.b, 0.8f);
            trackImg.name = "Scrollbar Track (replace sprite)";

            RectTransform area = UIKit.NewRect("Sliding Area", sbRt);
            UIKit.Stretch(area);
            Image handleImg = UIKit.NewImage("Scrollbar Handle (replace sprite)", area, null, Image.Type.Simple, UITheme.Gold, true);
            UIKit.Stretch(handleImg.rectTransform);

            Scrollbar sb = sbRt.gameObject.AddComponent<Scrollbar>();
            sb.handleRect = handleImg.rectTransform;
            sb.targetGraphic = handleImg;
            sb.direction = Scrollbar.Direction.BottomToTop;
            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }
        return sr;
    }

    // ---------- Lookups on built templates ----------
    public static T Find<T>(Transform root, string path) where T : Component
    {
        Transform t = root.Find(path);
        return t == null ? null : t.GetComponent<T>();
    }

    public static void SetActive(Transform root, string path, bool on)
    {
        Transform t = root.Find(path);
        if (t != null) t.gameObject.SetActive(on);
    }

    public static void SetText(Transform root, string path, string text)
    {
        TMP_Text t = Find<TMP_Text>(root, path);
        if (t != null) t.text = text;
    }

    // Removes every child (they are hidden at once, destroyed at the end of the frame).
    public static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject go = parent.GetChild(i).gameObject;
            go.SetActive(false);
            Object.Destroy(go);
        }
    }
}
