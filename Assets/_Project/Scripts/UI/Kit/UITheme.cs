using UnityEngine;
using UnityEngine.TextCore.LowLevel;
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
    // Lookup order for each family:
    //  1. TMP Font Asset (made by menu Oddyseus/Create UI Fonts)
    //  2. A raw .ttf/.otf in Resources/Fonts -> a dynamic TMP asset is built at runtime
    //  3. TMP default font (warning is logged once)
    static readonly string[] CinzelAssets = { "Fonts/Cinzel SDF", "Fonts/Cinzel-Regular SDF", "Fonts/Cinzel-VariableFont_wght SDF" };
    static readonly string[] NotoAssets   = { "Fonts/NotoSerif SDF", "Fonts/NotoSerif-Regular SDF", "Fonts/NotoSerif-VariableFont_wdth,wght SDF" };
    static readonly string[] CinzelFiles  = { "Fonts/Cinzel", "Fonts/Cinzel-Regular", "Fonts/Cinzel-VariableFont_wght" };
    static readonly string[] NotoFiles    = { "Fonts/NotoSerif", "Fonts/NotoSerif-Regular", "Fonts/NotoSerif-VariableFont_wdth,wght" };

    static TMP_FontAsset _cinzel, _noto;
    static bool _fontsLoaded, _warned;

    public static TMP_FontAsset TitleFont { get { LoadFonts(); return _cinzel; } }
    public static TMP_FontAsset BodyFont  { get { LoadFonts(); return _noto; } }

    public static bool FontsInstalled
    {
        get { LoadFonts(); return _cinzelOk && _notoOk; }
    }

    static bool _cinzelOk, _notoOk;

    // Call after creating font assets in the editor.
    public static void ReloadFonts()
    {
        _fontsLoaded = false; _cinzel = null; _noto = null; _warned = false;
        _cinzelOk = false; _notoOk = false;
    }

    static TMP_FontAsset FindFont(string[] assetPaths, string[] filePaths)
    {
        for (int i = 0; i < assetPaths.Length; i++)
        {
            TMP_FontAsset a = Resources.Load<TMP_FontAsset>(assetPaths[i]);
            if (a != null) return a;
        }
        for (int i = 0; i < filePaths.Length; i++)
        {
            Font f = Resources.Load<Font>(filePaths[i]);
            if (f == null) continue;
            TMP_FontAsset made = TMP_FontAsset.CreateFontAsset(f, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                                                               AtlasPopulationMode.Dynamic, true);
            if (made != null)
            {
                made.name = f.name + " SDF (runtime)";
                return made;
            }
        }
        return null;
    }

    static void LoadFonts()
    {
        if (_fontsLoaded) return;   // load once, even when the result is the fallback
        _fontsLoaded = true;

        _cinzel = FindFont(CinzelAssets, CinzelFiles);
        _noto   = FindFont(NotoAssets, NotoFiles);
        _cinzelOk = _cinzel != null;
        _notoOk   = _noto != null;

        if (!_cinzelOk || !_notoOk)
        {
            TMP_FontAsset fallback = TMP_Settings.instance != null ? TMP_Settings.defaultFontAsset : null;
            if (!_cinzelOk) _cinzel = fallback;
            if (!_notoOk) _noto = fallback;

            if (!_warned)
            {
                _warned = true;
                Debug.LogWarning("[UITheme] " + (!_cinzelOk ? "Cinzel " : "") + (!_notoOk ? "Noto Serif " : "") +
                                 "font not found in Resources/Fonts. Using the TMP default font. " +
                                 "Fix: put Cinzel.ttf / NotoSerif.ttf in Assets/_Project/Resources/Fonts " +
                                 "or run menu Oddyseus/Create UI Fonts.");
            }
        }
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