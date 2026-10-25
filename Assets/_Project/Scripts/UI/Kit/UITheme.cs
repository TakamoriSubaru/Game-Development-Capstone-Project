using UnityEngine;
using TMPro;

// Text styles used across the whole game UI.
public enum UITextStyle
{
    TitleCaps,   // Cinzel 48, ALL CAPS   ("PRESS ANY KEY")
    Subtitle,    // Noto Serif 32, Title Case ("Tap to begin")
    Body,        // Noto Serif 30
    Label,       // Cinzel 28, ALL CAPS (buttons, plates)
    Ribbon,      // Cinzel 30, ALL CAPS (hover ribbon, small plates)
    Small        // Noto Serif 24
}

/// <summary>
/// Night Sea and Bronze theme: colors, sizes, fonts. Static, no scene object needed.
/// Fonts are loaded from Resources/Fonts ("Cinzel SDF", "NotoSerif SDF").
/// If they are missing, the TMP default font is used (no crash).
/// </summary>
public static class UITheme
{
    // ---------- Palette ----------
    public static readonly Color Navy       = Hex("0B2A4A");
    public static readonly Color NavyDeep   = Hex("07203A");
    public static readonly Color Bronze     = Hex("B8863B");
    public static readonly Color Gold       = Hex("F2C66D");
    public static readonly Color Accent     = Hex("FFD166");
    public static readonly Color Cream      = Hex("E8E6D9");
    public static readonly Color Blue       = Hex("2E6B8F");
    public static readonly Color BlueLight  = Hex("8FB7D6");
    public static readonly Color SliderFill = Hex("F4D27A");
    public static readonly Color SliderTrack= Hex("2B3B56");
    public static readonly Color Dim        = new Color(0.02f, 0.07f, 0.13f, 0.75f);
    public static readonly Color TextDark   = Hex("2B1B0E");   // dark text on cream cards / gold buttons
    public static readonly Color Disabled   = Hex("7C8AA0");

    // Card and choice style (from 19_relic_choice and 20_event_quiet_cove)
    public static readonly Color CardCream   = Hex("F3E6C8");
    public static readonly Color TagBronze   = Hex("A8793A");
    public static readonly Color TagRust     = Hex("C8553A");
    public static readonly Color ChoiceBlue  = Hex("17406B");
    public static readonly Color ChoiceRed   = Hex("6B2323");
    public static readonly Color ChoiceGreen = Hex("2D5240");

    // ---------- Sizes (reference 1920x1080) ----------
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    public static readonly Vector2 DialogueBoxSize   = new Vector2(1400f, 220f);
    public static readonly Vector2 NamePlateSize     = new Vector2(320f, 56f);
    public const float ContinueChevronSize = 28f;
    public static readonly Vector2 HoverRibbonSize  = new Vector2(340f, 64f);
    public const float HoverPointerHeight = 20f;
    public static readonly Vector2 EdgeChevronSize  = new Vector2(280f, 84f);
    public static readonly Vector2 CompassSize      = new Vector2(640f, 96f);
    public const float MarkerSize = 48f;
    public static readonly Vector2 ConfirmPanelSize = new Vector2(760f, 360f);
    public static readonly Vector2 ConfirmButtonSize= new Vector2(280f, 72f);
    public static readonly Vector2 SliderTrackSize  = new Vector2(640f, 16f);
    public const float SliderHandleSize = 24f;
    public static readonly Vector2 SettingsPanelSize= new Vector2(900f, 640f);
    public const float SettingsSliderWidth = 400f;
    public const float SettingsKnobSize = 32f;

    public const int FontTitle = 48;
    public const int FontSubtitle = 32;
    public const int FontBody = 30;
    public const int FontLabel = 28;

    // ---------- Fonts ----------
    const string CinzelPath = "Fonts/Cinzel SDF";
    const string NotoPath   = "Fonts/NotoSerif SDF";
    static TMP_FontAsset _cinzel, _noto;
    static bool _fontsLoaded, _warned;

    public static TMP_FontAsset TitleFont { get { LoadFonts(); return _cinzel; } }
    public static TMP_FontAsset BodyFont  { get { LoadFonts(); return _noto; } }

    public static bool FontsInstalled
    {
        get { return Resources.Load<TMP_FontAsset>(CinzelPath) != null && Resources.Load<TMP_FontAsset>(NotoPath) != null; }
    }

    // Call after creating font assets in the editor.
    public static void ReloadFonts() { _fontsLoaded = false; _cinzel = null; _noto = null; _warned = false; }

    static void LoadFonts()
    {
        if (_fontsLoaded && (_cinzel != null || _noto != null)) return;
        _fontsLoaded = true;
        _cinzel = Resources.Load<TMP_FontAsset>(CinzelPath);
        _noto = Resources.Load<TMP_FontAsset>(NotoPath);
        TMP_FontAsset fallback = null;
        if (TMP_Settings.instance != null) fallback = TMP_Settings.defaultFontAsset;
        if ((_cinzel == null || _noto == null) && !_warned)
        {
            _warned = true;
            Debug.LogWarning("[UITheme] Cinzel or Noto Serif font asset not found in Resources/Fonts. " +
                             "Using the TMP default font. Run menu: Oddyseus/Create UI Fonts.");
        }
        if (_cinzel == null) _cinzel = fallback;
        if (_noto == null) _noto = fallback;
    }

    public static bool IsCinzel(UITextStyle s)
    {
        return s == UITextStyle.TitleCaps || s == UITextStyle.Label || s == UITextStyle.Ribbon;
    }

    public static int SizeOf(UITextStyle s)
    {
        switch (s)
        {
            case UITextStyle.TitleCaps: return FontTitle;
            case UITextStyle.Subtitle: return FontSubtitle;
            case UITextStyle.Label: return FontLabel;
            case UITextStyle.Ribbon: return 30;
            case UITextStyle.Small: return 24;
            default: return FontBody;
        }
    }

    // Only the font (safe to call again at runtime when fonts were added after a prefab was built).
    public static void ApplyFont(TMP_Text t, UITextStyle s)
    {
        if (t == null) return;
        TMP_FontAsset f = IsCinzel(s) ? TitleFont : BodyFont;
        if (f != null && t.font != f) t.font = f;
    }

    // Font, size, caps. Color only when given.
    public static void Apply(TMP_Text t, UITextStyle s, Color? color = null)
    {
        if (t == null) return;
        ApplyFont(t, s);
        t.fontSize = SizeOf(s);
        t.fontStyle = IsCinzel(s) ? FontStyles.UpperCase : FontStyles.Normal;
        if (color.HasValue) t.color = color.Value;
        else t.color = UITheme.Cream;
    }

    public static Color Hex(string hex)
    {
        Color c;
        if (ColorUtility.TryParseHtmlString("#" + hex, out c)) return c;
        return Color.magenta;
    }
}
