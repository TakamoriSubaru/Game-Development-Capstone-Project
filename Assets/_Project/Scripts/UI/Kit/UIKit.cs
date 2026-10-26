using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Builds every Kit component from code. The prefabs in Assets/_Project/Prefabs/UI/Kit are made
/// with these same functions, so code and prefabs always look the same.
/// Other scripts can call UIKit.CreateDialogueBox(parent) etc. when they do not want a prefab.
/// </summary>
public static class UIKit
{
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    // ------------------------------------------------------------------
    // Low-level helpers
    // ------------------------------------------------------------------
    public static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; // UI
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    // pad > 0 grows the rect outward, pad < 0 shrinks it.
    public static void Stretch(RectTransform rt, float pad = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-pad, -pad);
        rt.offsetMax = new Vector2(pad, pad);
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static Image NewImage(string name, Transform parent, string sprite, Image.Type type, Color color, bool raycast)
    {
        RectTransform rt = NewRect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        if (!string.IsNullOrEmpty(sprite))
        {
            img.sprite = UIArt.Get(sprite);
            img.type = type;
        }
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    public static TMP_Text CreateText(Transform parent, string name, string text, UITextStyle style,
                                      Color color, TextAlignmentOptions align)
    {
        RectTransform rt = NewRect(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        UITheme.Apply(t, style, color);
        t.text = text;
        t.alignment = align;
        t.raycastTarget = false;
        ThemedText th = rt.gameObject.AddComponent<ThemedText>();
        th.style = style;
        UITheme.ApplyFont(t, style);
        return t;
    }

    static RectTransform RectOf(Component c)
    {
        return (RectTransform)c.transform;
    }

    static Color A(Color c, float a)
    {
        c.a = a;
        return c;
    }

    // Greek-key corner brackets. One sprite, flipped with negative scale.
    // The corner ornaments are now part of the "panel_orn" sprite itself (baked into its corners),
    // so nothing is added here any more. Kept so the callers do not change.
    static void AddCorners(Transform parent, float inset)
    {
    }

    // ------------------------------------------------------------------
    // Canvas and event system
    // ------------------------------------------------------------------
    public static void ConfigureScaler(CanvasScaler s)
    {
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = UITheme.ReferenceResolution;
        s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        s.matchWidthOrHeight = 0.5f;
    }

    public static Canvas CreateOverlayCanvas(string name, int sortingOrder = 0)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = 5;
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortingOrder;
        ConfigureScaler(go.GetComponent<CanvasScaler>());
        EnsureEventSystem();
        return c;
    }

    // Adds an EventSystem with the right input module (new Input System or legacy) if the scene has none.
    public static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject go = new GameObject("EventSystem", typeof(EventSystem));
        Type t = null;
#if ENABLE_INPUT_SYSTEM
        t = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
#endif
        if (t != null)
        {
            Component module = go.AddComponent(t);
            MethodInfo assign = t.GetMethod("AssignDefaultActions");
            if (assign != null) assign.Invoke(module, null);
        }
        else
        {
            go.AddComponent<StandaloneInputModule>();
        }
    }

    // ------------------------------------------------------------------
    // ThemedButton (Primary / Secondary / Edge / Choice)
    // ------------------------------------------------------------------
    public static ThemedButton CreateButton(Transform parent, string text, ThemedButtonStyle style,
                                            Vector2 size, UITextStyle labelStyle = UITextStyle.Label)
    {
        RectTransform rt = NewRect(text + " Button", parent);
        rt.sizeDelta = size;

        Image glow = NewImage("Glow", rt, "glow", Image.Type.Sliced, Color.white, false);
        Stretch(glow.rectTransform, 20f);
        Image frame = NewImage("Frame (replace sprite)", rt, "btn_primary", Image.Type.Sliced, Color.white, true);
        Stretch(frame.rectTransform);

        bool choice = style == ThemedButtonStyle.ChoiceBlue || style == ThemedButtonStyle.ChoiceRed || style == ThemedButtonStyle.ChoiceGreen;
        TMP_Text label = CreateText(rt, "Label", text, labelStyle, UITheme.Cream,
                                    choice ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        if (choice)
        {
            label.rectTransform.offsetMin = new Vector2(100f, 4f);
            label.rectTransform.offsetMax = new Vector2(-70f, -4f);

            Color iconTint = style == ThemedButtonStyle.ChoiceBlue ? UITheme.Blue :
                             (style == ThemedButtonStyle.ChoiceRed ? UITheme.Hex("A83A2E") : UITheme.Hex("3E7A5C"));
            Image icon = NewImage("Icon (replace sprite)", rt, "circle", Image.Type.Simple, iconTint, false);
            Place(icon.rectTransform, new Vector2(0f, 0.5f), Center, new Vector2(52f, 0f), new Vector2(64f, 64f));
            Image chev = NewImage("Chevron Icon (replace sprite)", rt, "chevron", Image.Type.Simple, UITheme.Cream, false);
            Place(chev.rectTransform, new Vector2(1f, 0.5f), Center, new Vector2(-36f, 0f), new Vector2(28f, 28f));
        }
        else
        {
            label.rectTransform.offsetMin = new Vector2(20f, 0f);
            label.rectTransform.offsetMax = new Vector2(-20f, 0f);
        }

        ThemedButton b = rt.gameObject.AddComponent<ThemedButton>();
        b.style = style;
        b.frame = frame;
        b.glow = glow;
        b.label = label;
        b.ApplyStyle();
        return b;
    }

    public static ThemedButton CreateChoiceButton(Transform parent, string text, ThemedButtonStyle style)
    {
        return CreateButton(parent, text, style, new Vector2(620f, 108f), UITextStyle.Body);
    }

    // ------------------------------------------------------------------
    // EdgeChevronButton 280x84
    // ------------------------------------------------------------------
    public static EdgeChevronButton CreateEdgeChevron(Transform parent, string text, bool pointsRight)
    {
        RectTransform rt = NewRect("Edge Chevron Button " + (pointsRight ? "Right" : "Left"), parent);
        rt.sizeDelta = UITheme.EdgeChevronSize;

        Image glow = NewImage("Glow", rt, "glow", Image.Type.Sliced, Color.white, false);
        Stretch(glow.rectTransform, 20f);
        Image frame = NewImage("Frame (replace sprite)", rt, "btn_edge", Image.Type.Sliced, Color.white, true);
        Stretch(frame.rectTransform);
        TMP_Text label = CreateText(rt, "Label", text, UITextStyle.Label, UITheme.Cream, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(64f, 0f);
        label.rectTransform.offsetMax = new Vector2(-64f, 0f);
        Image chev = NewImage("Chevron Icon (replace sprite)", rt, "chevron", Image.Type.Simple, UITheme.Gold, false);
        chev.rectTransform.sizeDelta = new Vector2(28f, 28f);

        EdgeChevronButton b = rt.gameObject.AddComponent<EdgeChevronButton>();
        b.style = ThemedButtonStyle.Edge;
        b.frame = frame;
        b.glow = glow;
        b.label = label;
        b.chevron = chev;
        b.ApplyStyle();
        b.Setup(text, pointsRight, null);
        return b;
    }

    // ------------------------------------------------------------------
    // ThemedSlider (track 640x16, handle 24x24)
    // ------------------------------------------------------------------
    public static ThemedSlider CreateSlider(Transform parent)
    {
        RectTransform rt = NewRect("Themed Slider", parent);
        rt.sizeDelta = new Vector2(UITheme.SliderTrackSize.x, 40f);

        Image hit = NewImage("Hit Area", rt, null, Image.Type.Simple, new Color(0f, 0f, 0f, 0f), true);
        Stretch(hit.rectTransform);

        Image track = NewImage("Track (replace sprite)", rt, "pill", Image.Type.Sliced, UITheme.SliderTrack, false);
        RectTransform tr = track.rectTransform;
        tr.anchorMin = new Vector2(0f, 0.5f);
        tr.anchorMax = new Vector2(1f, 0.5f);
        tr.pivot = Center;
        tr.offsetMin = new Vector2(0f, -UITheme.SliderTrackSize.y * 0.5f);
        tr.offsetMax = new Vector2(0f, UITheme.SliderTrackSize.y * 0.5f);

        RectTransform fillArea = NewRect("Fill Area", rt);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.pivot = Center;
        fillArea.offsetMin = new Vector2(0f, -UITheme.SliderTrackSize.y * 0.5f);
        fillArea.offsetMax = new Vector2(0f, UITheme.SliderTrackSize.y * 0.5f);

        Image fill = NewImage("Fill (replace sprite)", fillArea, "pill", Image.Type.Sliced, UITheme.SliderFill, false);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        RectTransform handleArea = NewRect("Handle Slide Area", rt);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(UITheme.SliderHandleSize * 0.5f, 0f);
        handleArea.offsetMax = new Vector2(-UITheme.SliderHandleSize * 0.5f, 0f);

        Image knob = NewImage("Knob (replace sprite)", handleArea, "knob", Image.Type.Simple, Color.white, true);
        Place(knob.rectTransform, new Vector2(0f, 0.5f), Center, Vector2.zero,
              new Vector2(UITheme.SliderHandleSize, UITheme.SliderHandleSize));

        Slider s = rt.gameObject.AddComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = knob.rectTransform;
        s.targetGraphic = knob;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.wholeNumbers = false;
        s.transition = Selectable.Transition.None;
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        s.navigation = nav;
        s.value = GameSettings.DefaultVolume;

        ThemedSlider ts = rt.gameObject.AddComponent<ThemedSlider>();
        ts.slider = s;
        ts.track = tr;
        ts.handleArea = handleArea;
        ts.knob = knob.rectTransform;
        ts.Init();
        return ts;
    }

    // ------------------------------------------------------------------
    // DialogueBox 1400x220 + NamePlate 320x56 + chevron 28x28
    // ------------------------------------------------------------------
    public static DialogueBox CreateDialogueBox(Transform parent)
    {
        RectTransform rt = NewRect("Dialogue Box", parent);
        rt.sizeDelta = UITheme.DialogueBoxSize;

        Image frame = NewImage("Frame (replace sprite)", rt, "panel_orn", Image.Type.Sliced, Color.white, true);
        Stretch(frame.rectTransform);
        AddCorners(rt, 26f);

        RectTransform np = NewRect("NamePlate", rt);
        Place(np, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(56f, 0f), UITheme.NamePlateSize);
        Image npImg = NewImage("NamePlate (replace sprite)", np, "plate", Image.Type.Sliced, Color.white, false);
        Stretch(npImg.rectTransform);
        TMP_Text nameLabel = CreateText(np, "Name", "Name Plate", UITextStyle.Label, UITheme.Cream, TextAlignmentOptions.Center);
        Stretch(nameLabel.rectTransform);
        nameLabel.rectTransform.offsetMin = new Vector2(24f, 0f);
        nameLabel.rectTransform.offsetMax = new Vector2(-24f, 0f);
        nameLabel.enableAutoSizing = true;
        nameLabel.fontSizeMin = 18f;
        nameLabel.fontSizeMax = 28f;

        TMP_Text body = CreateText(rt, "Text", "Dialogue text goes here. Up to three lines at 30 px, Noto Serif.",
                                   UITextStyle.Body, UITheme.Cream, TextAlignmentOptions.TopLeft);
        Stretch(body.rectTransform);
        body.rectTransform.offsetMin = new Vector2(64f, 26f);
        body.rectTransform.offsetMax = new Vector2(-100f, -46f);

        Image chevron = NewImage("Continue Chevron (replace sprite)", rt, "chevron_double", Image.Type.Simple, Color.white, false);
        Place(chevron.rectTransform, new Vector2(1f, 0f), Center, new Vector2(-48f, 40f),
              new Vector2(UITheme.ContinueChevronSize, UITheme.ContinueChevronSize));

        DialogueBox d = rt.gameObject.AddComponent<DialogueBox>();
        d.nameLabel = nameLabel;
        d.bodyLabel = body;
        d.namePlate = np.gameObject;
        d.continueChevron = chevron;
        return d;
    }

    // ------------------------------------------------------------------
    // HoverRibbon 340x64 + 20 px pointer. Root pivot = tip of the pointer.
    // ------------------------------------------------------------------
    public static HoverRibbon CreateHoverRibbon(Transform parent)
    {
        RectTransform rt = NewRect("Hover Ribbon", parent);
        Place(rt, Center, new Vector2(0.5f, 0f), Vector2.zero,
              new Vector2(UITheme.HoverRibbonSize.x, UITheme.HoverRibbonSize.y + UITheme.HoverPointerHeight));

        Image pointer = NewImage("Pointer (replace sprite)", rt, "pointer", Image.Type.Simple, Color.white, false);
        Place(pointer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero,
              new Vector2(28f, UITheme.HoverPointerHeight));
        Image plate = NewImage("Plate (replace sprite)", rt, "plate", Image.Type.Sliced, Color.white, false);
        Place(plate.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, UITheme.HoverRibbonSize);

        TMP_Text label = CreateText(plate.rectTransform, "Label", "EXIT", UITextStyle.Ribbon, UITheme.Cream, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(30f, 0f);
        label.rectTransform.offsetMax = new Vector2(-30f, 0f);
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 30f;

        HoverRibbon r = rt.gameObject.AddComponent<HoverRibbon>();
        r.label = label;
        return r;
    }

    // ------------------------------------------------------------------
    // HotspotMarker 48x48
    // ------------------------------------------------------------------
    public static HotspotMarker CreateHotspotMarker(Transform parent)
    {
        RectTransform rt = NewRect("Hotspot Marker", parent);
        rt.sizeDelta = new Vector2(UITheme.MarkerSize, UITheme.MarkerSize);

        Image glow = NewImage("Pulse Glow (replace sprite)", rt, "node_glow", Image.Type.Simple, A(UITheme.Accent, 0f), false);
        Place(glow.rectTransform, Center, Center, Vector2.zero, new Vector2(120f, 120f));
        Image marker = NewImage("Marker (replace sprite)", rt, "marker", Image.Type.Simple, Color.white, true);
        Stretch(marker.rectTransform);

        HotspotMarker m = rt.gameObject.AddComponent<HotspotMarker>();
        m.marker = marker;
        m.pulseGlow = glow;
        return m;
    }

    // ------------------------------------------------------------------
    // CompassIndicator 640x96
    // ------------------------------------------------------------------
    public static CompassIndicator CreateCompass(Transform parent)
    {
        RectTransform rt = NewRect("Compass Indicator", parent);
        rt.sizeDelta = UITheme.CompassSize;

        Image line = NewImage("Line (replace sprite)", rt, null, Image.Type.Simple, A(UITheme.Bronze, 0.85f), false);
        Place(line.rectTransform, Center, Center, new Vector2(0f, 10f), new Vector2(540f, 2f));

        string[] names = { "STORY", "LOBBY", "DUNGEON", "EXIT" };
        float[] xs = { -255f, -85f, 85f, 255f };
        CompassIndicator c = rt.gameObject.AddComponent<CompassIndicator>();
        c.nodes = new Image[4];
        c.glows = new Image[4];
        c.labels = new TMP_Text[4];

        for (int i = 0; i < 4; i++)
        {
            Image glow = NewImage("Node Glow " + (i + 1) + " (replace sprite)", rt, "node_glow", Image.Type.Simple, A(UITheme.Accent, 0f), false);
            Place(glow.rectTransform, Center, Center, new Vector2(xs[i], 10f), new Vector2(130f, 130f));
            Image node = NewImage("Node " + (i + 1) + " (replace sprite)", rt, "node", Image.Type.Simple, Color.white, false);
            Place(node.rectTransform, Center, Center, new Vector2(xs[i], 10f), new Vector2(40f, 40f));
            TMP_Text label = CreateText(rt, "Label " + (i + 1), names[i], UITextStyle.Ribbon, UITheme.Cream, TextAlignmentOptions.Center);
            label.fontSize = 20f;
            Place(label.rectTransform, Center, Center, new Vector2(xs[i], -30f), new Vector2(160f, 28f));
            c.nodes[i] = node;
            c.glows[i] = glow;
            c.labels[i] = label;
        }
        return c;
    }

    // ------------------------------------------------------------------
    // ConfirmPanel 760x360 (starts hidden)
    // ------------------------------------------------------------------
    public static ConfirmPanel CreateConfirmPanel(Transform parent)
    {
        RectTransform root = NewRect("Confirm Panel", parent);
        Stretch(root);
        Canvas cv = root.gameObject.AddComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingOrder = 500;
        root.gameObject.AddComponent<GraphicRaycaster>();

        Image dim = NewImage("Dim", root, null, Image.Type.Simple, UITheme.Dim, true);
        Stretch(dim.rectTransform);

        RectTransform panel = NewRect("Panel", root);
        Place(panel, Center, Center, Vector2.zero, UITheme.ConfirmPanelSize);
        Image frame = NewImage("Frame (replace sprite)", panel, "panel_orn", Image.Type.Sliced, Color.white, true);
        Stretch(frame.rectTransform);
        AddCorners(panel, 26f);

        TMP_Text q = CreateText(panel, "Question", "Leave the voyage?", UITextStyle.Subtitle, UITheme.Cream, TextAlignmentOptions.Center);
        q.fontSize = 40f;
        q.rectTransform.anchorMin = new Vector2(0f, 0.42f);
        q.rectTransform.anchorMax = new Vector2(1f, 0.92f);
        q.rectTransform.offsetMin = new Vector2(50f, 0f);
        q.rectTransform.offsetMax = new Vector2(-50f, 0f);

        ThemedButton stay = CreateButton(panel, "Stay", ThemedButtonStyle.Secondary, UITheme.ConfirmButtonSize, UITextStyle.Body);
        Place(RectOf(stay), Center, Center, new Vector2(-160f, -100f), UITheme.ConfirmButtonSize);
        ThemedButton leave = CreateButton(panel, "Leave", ThemedButtonStyle.Primary, UITheme.ConfirmButtonSize, UITextStyle.Body);
        Place(RectOf(leave), Center, Center, new Vector2(160f, -100f), UITheme.ConfirmButtonSize);

        ConfirmPanel cp = root.gameObject.AddComponent<ConfirmPanel>();
        cp.questionLabel = q;
        cp.stayButton = stay;
        cp.confirmButton = leave;
        root.gameObject.SetActive(false);
        return cp;
    }

    // ------------------------------------------------------------------
    // ScreenHeader (top bar)
    // ------------------------------------------------------------------
    public static ScreenHeader CreateScreenHeader(Transform parent)
    {
        RectTransform rt = NewRect("Screen Header", parent);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 140f);

        Image bar = NewImage("Bar", rt, null, Image.Type.Simple, A(UITheme.NavyDeep, 0.93f), true);
        Stretch(bar.rectTransform);

        Image strip = NewImage("Greek Key Strip (replace sprite)", rt, "key_tile", Image.Type.Tiled, A(Color.white, 0.9f), false);
        strip.rectTransform.anchorMin = new Vector2(0f, 0f);
        strip.rectTransform.anchorMax = new Vector2(1f, 0f);
        strip.rectTransform.pivot = new Vector2(0.5f, 0f);
        strip.rectTransform.anchoredPosition = Vector2.zero;
        strip.rectTransform.sizeDelta = new Vector2(0f, 32f);

        Image rule = NewImage("Gold Line", rt, null, Image.Type.Simple, UITheme.Gold, false);
        rule.rectTransform.anchorMin = new Vector2(0f, 0f);
        rule.rectTransform.anchorMax = new Vector2(1f, 0f);
        rule.rectTransform.pivot = new Vector2(0.5f, 0f);
        rule.rectTransform.anchoredPosition = new Vector2(0f, 32f);
        rule.rectTransform.sizeDelta = new Vector2(0f, 2f);

        ThemedButton back = CreateButton(rt, "BACK", ThemedButtonStyle.Primary, new Vector2(240f, 64f));
        Place(RectOf(back), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 16f), new Vector2(240f, 64f));

        TMP_Text title = CreateText(rt, "Title", "TITLE", UITextStyle.TitleCaps, UITheme.Cream, TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = Vector2.zero;
        title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.offsetMin = new Vector2(340f, 36f);
        title.rectTransform.offsetMax = new Vector2(-340f, 0f);

        ScreenHeader h = rt.gameObject.AddComponent<ScreenHeader>();
        h.backButton = back;
        h.titleLabel = title;
        return h;
    }

    // ------------------------------------------------------------------
    // SettingsPanel 900x640 (starts hidden)
    // ------------------------------------------------------------------
    public static SettingsPanel CreateSettingsPanel(Transform parent)
    {
        RectTransform root = NewRect("Settings Panel", parent);
        Stretch(root);
        Canvas cv = root.gameObject.AddComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingOrder = 400;
        root.gameObject.AddComponent<GraphicRaycaster>();

        Image dim = NewImage("Dim", root, null, Image.Type.Simple, UITheme.Dim, true);
        Stretch(dim.rectTransform);

        RectTransform panel = NewRect("Panel", root);
        Place(panel, Center, Center, Vector2.zero, UITheme.SettingsPanelSize);
        Image frame = NewImage("Frame (replace sprite)", panel, "panel_orn", Image.Type.Sliced, Color.white, true);
        Stretch(frame.rectTransform);
        AddCorners(panel, 28f);

        // Header ribbon
        RectTransform hp = NewRect("Header Ribbon", panel);
        Place(hp, new Vector2(0.5f, 1f), Center, Vector2.zero, new Vector2(460f, 76f));
        Image plate = NewImage("Header Ribbon (replace sprite)", hp, "plate", Image.Type.Sliced, Color.white, false);
        Stretch(plate.rectTransform);
        Image keyL = NewImage("Key Ornament L (replace sprite)", hp, "key_tile", Image.Type.Tiled, Color.white, false);
        Place(keyL.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(64f, 32f));
        Image keyR = NewImage("Key Ornament R (replace sprite)", hp, "key_tile", Image.Type.Tiled, Color.white, false);
        Place(keyR.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(64f, 32f));
        TMP_Text title = CreateText(hp, "Title", "SETTINGS", UITextStyle.TitleCaps, UITheme.Cream, TextAlignmentOptions.Center);
        title.fontSize = 44f;
        Stretch(title.rectTransform);
        title.rectTransform.offsetMin = new Vector2(100f, 0f);
        title.rectTransform.offsetMax = new Vector2(-100f, 0f);

        ThemedSlider music = CreateSettingsRow(panel, "Music", 95f);
        ThemedSlider sfx = CreateSettingsRow(panel, "SFX", -15f);

        // Divider with diamond
        Image rule = NewImage("Divider", panel, null, Image.Type.Simple, A(UITheme.Gold, 0.7f), false);
        Place(rule.rectTransform, Center, Center, new Vector2(0f, -125f), new Vector2(800f, 2f));
        Image dia = NewImage("Divider Diamond (replace sprite)", panel, "diamond", Image.Type.Simple, Color.white, false);
        Place(dia.rectTransform, Center, Center, new Vector2(0f, -125f), new Vector2(20f, 20f));

        ThemedButton back = CreateButton(panel, "BACK", ThemedButtonStyle.Primary, UITheme.ConfirmButtonSize);
        Place(RectOf(back), Center, Center, new Vector2(0f, -215f), UITheme.ConfirmButtonSize);

        SettingsPanel sp = root.gameObject.AddComponent<SettingsPanel>();
        sp.musicSlider = music;
        sp.sfxSlider = sfx;
        sp.backButton = back;
        root.gameObject.SetActive(false);
        return sp;
    }

    static ThemedSlider CreateSettingsRow(Transform panel, string labelText, float y)
    {
        TMP_Text label = CreateText(panel, labelText + " Label", labelText, UITextStyle.Body, UITheme.Cream, TextAlignmentOptions.MidlineLeft);
        label.fontSize = 36f;
        Place(label.rectTransform, Center, new Vector2(0f, 0.5f), new Vector2(-390f, y), new Vector2(190f, 50f));

        ThemedSlider s = CreateSlider(panel);
        s.SetSizes(UITheme.SettingsSliderWidth, UITheme.SliderTrackSize.y, UITheme.SettingsKnobSize);
        s.name = labelText + " Slider";
        Place(RectOf(s), Center, Center, new Vector2(40f, y), RectOf(s).sizeDelta);

        TMP_Text value = CreateText(panel, labelText + " Value", "70%", UITextStyle.Body, UITheme.Cream, TextAlignmentOptions.MidlineRight);
        value.fontSize = 32f;
        Place(value.rectTransform, Center, new Vector2(1f, 0.5f), new Vector2(390f, y), new Vector2(120f, 50f));
        s.valueLabel = value;
        return s;
    }
}