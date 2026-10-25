using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tools for the UI Kit (AI-1 area).
///   Oddyseus/Create UI Fonts              make TMP font assets from the Cinzel + Noto Serif .ttf files
///   Oddyseus/Build UI Kit Gallery         create missing sprites and prefabs, rebuild the gallery scene
///   Oddyseus/Rebuild ALL UI Kit Assets    overwrite sprites and prefabs too (old ones are backed up)
/// </summary>
public static class UIKitBuilder
{
    const string Root = "Assets/_Project";
    const string KitDir = Root + "/Prefabs/UI/Kit";
    const string ResDir = KitDir + "/Resources";
    const string SpriteDir = ResDir + "/UIKit";
    const string FontDir = ResDir + "/Fonts";
    const string BackupRoot = KitDir + "/_Backups";
    const string SceneDir = Root + "/Scenes";
    const string ScenePath = SceneDir + "/UIKitGallery.unity";

    static string stamp;

    // ------------------------------------------------------------------
    // Menu
    // ------------------------------------------------------------------
    [MenuItem("Oddyseus/Build UI Kit Gallery")]
    static void MenuBuild()
    {
        Build(false);
    }

    [MenuItem("Oddyseus/Rebuild ALL UI Kit Assets (overwrites)")]
    static void MenuRebuildAll()
    {
        if (!EditorUtility.DisplayDialog("Rebuild ALL UI Kit Assets",
            "This overwrites the placeholder sprites and all Kit prefabs. " +
            "If your artist replaced a sprite or edited a prefab, the old file is copied to " +
            BackupRoot + " first. Continue?", "Rebuild", "Cancel")) return;
        Build(true);
    }

    [MenuItem("Oddyseus/Create UI Fonts (Cinzel + Noto Serif)")]
    static void MenuCreateFonts()
    {
        Directory.CreateDirectory(FontDir);
        string a = MakeFont("Cinzel", new[] { "Cinzel-SemiBold", "Cinzel-Regular", "Cinzel-Medium" }, "Cinzel SDF");
        string b = MakeFont("NotoSerif", new[] { "NotoSerif-Regular" }, "NotoSerif SDF");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UITheme.ReloadFonts();
        EditorUtility.DisplayDialog("UI Fonts", "Cinzel: " + a + "\nNoto Serif: " + b +
            "\n\nIf a font was not found, copy its .ttf into Assets/_Project/Fonts and run this menu again.", "OK");
    }

    // ------------------------------------------------------------------
    // Fonts
    // ------------------------------------------------------------------
    static string MakeFont(string search, string[] preferredNames, string assetName)
    {
        string outPath = FontDir + "/" + assetName + ".asset";
        if (File.Exists(outPath)) return "already exists (" + assetName + ")";

        string ttf = PickFont(search, preferredNames);
        if (ttf == null) return "NOT FOUND (no " + search + " .ttf in the project)";
        Font font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
        if (font == null) return "could not load " + ttf;

        TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                                                         AtlasPopulationMode.Dynamic, true);
        if (fa == null) return "CreateFontAsset failed for " + ttf;
        fa.name = assetName;
        AssetDatabase.CreateAsset(fa, outPath);
        if (fa.atlasTexture != null)
        {
            fa.atlasTexture.name = assetName + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
        }
        if (fa.material != null)
        {
            fa.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
        }
        EditorUtility.SetDirty(fa);
        return "created from " + Path.GetFileName(ttf);
    }

    static string PickFont(string search, string[] preferredNames)
    {
        string[] guids = AssetDatabase.FindAssets(search + " t:Font");
        List<string> paths = new List<string>();
        foreach (string g in guids) paths.Add(AssetDatabase.GUIDToAssetPath(g));

        foreach (string pref in preferredNames)
            foreach (string p in paths)
                if (Path.GetFileNameWithoutExtension(p) == pref) return p;

        foreach (string p in paths)
        {
            string n = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
            if (n.Contains("italic") || n.Contains("decorative") || n.Contains("variable") || n.Contains("[")) continue;
            return p;
        }
        return paths.Count > 0 ? paths[0] : null;
    }

    // ------------------------------------------------------------------
    // Main build
    // ------------------------------------------------------------------
    static void Build(bool overwrite)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        backupDirReady = false;

        Directory.CreateDirectory(SpriteDir);
        Directory.CreateDirectory(FontDir);
        Directory.CreateDirectory(SceneDir);
        Directory.CreateDirectory(BackupRoot);

        WriteSprites(overwrite);
        UIArt.ClearCache();
        UITheme.ReloadFonts();
        if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(UIArt.Get("panel"))))
        {
            EditorUtility.DisplayDialog("UI Kit", "The placeholder sprites were not imported (Resources/UIKit/panel.png is missing). " +
                "Look at the Console for errors, then run this menu again.", "OK");
            return;
        }
        BuildPrefabs(overwrite);
        BuildScene();

        string fontNote = UITheme.FontsInstalled ? "" :
            "\n\nNOTE: Cinzel / Noto Serif font assets were not found, so the TMP default font was used. " +
            "Run Oddyseus/Create UI Fonts, then run this menu again.";
        EditorUtility.DisplayDialog("UI Kit Gallery",
            "Done. The scene is " + ScenePath + ". Press Play to test it." + fontNote, "OK");
    }

    // ------------------------------------------------------------------
    // Sprites
    // ------------------------------------------------------------------
    static void WriteSprites(bool overwrite)
    {
        List<string> written = new List<string>();
        foreach (string name in UIArt.Names)
        {
            string path = SpriteDir + "/" + name + ".png";
            if (File.Exists(path))
            {
                if (!overwrite) continue;
                BackupFile(path);
            }
            Texture2D tex = UIArt.Create(name);
            if (tex == null) continue;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            written.Add(path);
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        foreach (string name in UIArt.Names)
        {
            string path = SpriteDir + "/" + name + ".png";
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            if (!written.Contains(path) && imp.textureType == TextureImporterType.Sprite) continue;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.spriteBorder = UIArt.BorderOf(name);
            imp.SaveAndReimport();
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    static bool backupDirReady;

    static string EnsureBackupDir()
    {
        string dir = BackupRoot + "/" + stamp;
        if (!backupDirReady)
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            backupDirReady = true;
        }
        return dir;
    }

    static void BackupFile(string assetPath)
    {
        string dir = EnsureBackupDir();
        AssetDatabase.CopyAsset(assetPath, dir + "/" + Path.GetFileName(assetPath));
    }

    // ------------------------------------------------------------------
    // Prefabs
    // ------------------------------------------------------------------
    static void BuildPrefabs(bool overwrite)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Canvas canvas = UIKit.CreateOverlayCanvas("Temp Canvas");
        Transform p = canvas.transform;

        SavePrefab("DialogueBox", overwrite, () => UIKit.CreateDialogueBox(p).gameObject);
        SavePrefab("HoverRibbon", overwrite, () => UIKit.CreateHoverRibbon(p).gameObject);
        SavePrefab("EdgeChevronButtonLeft", overwrite, () => UIKit.CreateEdgeChevron(p, "DUNGEON", false).gameObject);
        SavePrefab("EdgeChevronButtonRight", overwrite, () => UIKit.CreateEdgeChevron(p, "STORY", true).gameObject);
        SavePrefab("CompassIndicator", overwrite, () => UIKit.CreateCompass(p).gameObject);
        SavePrefab("HotspotMarker", overwrite, () => UIKit.CreateHotspotMarker(p).gameObject);
        SavePrefab("ConfirmPanel", overwrite, () => UIKit.CreateConfirmPanel(p).gameObject);
        SavePrefab("ThemedButtonPrimary", overwrite,
            () => UIKit.CreateButton(p, "Primary", ThemedButtonStyle.Primary, new Vector2(280f, 72f)).gameObject);
        SavePrefab("ThemedButtonSecondary", overwrite,
            () => UIKit.CreateButton(p, "Secondary", ThemedButtonStyle.Secondary, new Vector2(280f, 72f)).gameObject);
        SavePrefab("ThemedSlider", overwrite, () => UIKit.CreateSlider(p).gameObject);
        SavePrefab("ScreenHeader", overwrite, () => UIKit.CreateScreenHeader(p).gameObject);
        SavePrefab("SettingsPanel", overwrite, () => UIKit.CreateSettingsPanel(p).gameObject);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void SavePrefab(string name, bool overwrite, Func<GameObject> make)
    {
        string path = KitDir + "/" + name + ".prefab";
        bool exists = File.Exists(path);
        if (exists && !overwrite) return;
        if (exists) BackupFile(path);
        GameObject go = make();
        go.name = name;
        PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
    }

    static T Inst<T>(string prefabName, Transform parent) where T : Component
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KitDir + "/" + prefabName + ".prefab");
        if (prefab == null) throw new Exception("Missing prefab: " + prefabName);
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        return go.GetComponent<T>();
    }

    // ------------------------------------------------------------------
    // Gallery scene
    // ------------------------------------------------------------------
    const float ContentHeight = 3300f;

    static RectTransform Rt(Component c) { return (RectTransform)c.transform; }

    // Place at x (from center) and y (down from the top of the content), pivot centered.
    static void At(Component c, float x, float y, Vector2 size)
    {
        UIKit.Place(Rt(c), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, -y), size);
    }

    static TMP_Text AddText(Transform parent, string text, UITextStyle style, float x, float y, float w, Color color)
    {
        TMP_Text t = UIKit.CreateText(parent, "Text " + text, text, style, color, TextAlignmentOptions.Center);
        UIKit.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, -y), new Vector2(w, 60f));
        return t;
    }

    static void Caption(Transform parent, string text, float x, float y)
    {
        TMP_Text t = AddText(parent, text, UITextStyle.Small, x, y, 440f, UITheme.BlueLight);
        t.fontSize = 22f;
    }

    static void SectionTitle(Transform parent, string text, float y)
    {
        AddText(parent, text, UITextStyle.Label, 0f, y, 900f, UITheme.Accent);
        Image rule = UIKit.NewImage("Rule", parent, null, Image.Type.Simple, new Color(UITheme.Bronze.r, UITheme.Bronze.g, UITheme.Bronze.b, 0.6f), false);
        UIKit.Place(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -(y + 34f)), new Vector2(1500f, 2f));
    }

    static void BuildScene()
    {
        if (File.Exists(ScenePath))
        {
            string dir = EnsureBackupDir();
            AssetDatabase.CopyAsset(ScenePath, dir + "/UIKitGallery.unity");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Canvas canvas = UIKit.CreateOverlayCanvas("Gallery Canvas");
        UIKitGallery gallery = canvas.gameObject.AddComponent<UIKitGallery>();

        // Scroll view
        RectTransform scrollRt = UIKit.NewRect("Scroll View", canvas.transform);
        UIKit.Stretch(scrollRt);
        Image bg = scrollRt.gameObject.AddComponent<Image>();
        bg.color = UITheme.Hex("08182B");
        ScrollRect scroll = scrollRt.gameObject.AddComponent<ScrollRect>();

        RectTransform viewport = UIKit.NewRect("Viewport", scrollRt);
        UIKit.Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = UIKit.NewRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, ContentHeight);

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 60f;

        Transform c = content;
        List<ThemedButton> previews = new List<ThemedButton>();
        List<int> states = new List<int>();
        Vector2 btn = new Vector2(280f, 72f);
        float[] cols = { -690f, -230f, 230f, 690f };

        AddText(c, "UI KIT GALLERY", UITextStyle.TitleCaps, 0f, 60f, 1200f, UITheme.Cream);
        AddText(c, "Scroll with the mouse wheel. Live items react to the mouse. Frozen items show a forced state.",
             UITextStyle.Small, 0f, 118f, 1500f, UITheme.BlueLight);

        // --- Buttons
        SectionTitle(c, "THEMED BUTTON", 200f);
        string[] stateNames = { "normal (live)", "hover (frozen)", "pressed (frozen)", "disabled" };
        for (int row = 0; row < 2; row++)
        {
            string prefab = row == 0 ? "ThemedButtonPrimary" : "ThemedButtonSecondary";
            string rowName = row == 0 ? "Primary" : "Secondary";
            float y = row == 0 ? 300f : 440f;
            for (int i = 0; i < 4; i++)
            {
                ThemedButton b = Inst<ThemedButton>(prefab, c);
                At(b, cols[i], y, btn);
                b.name = rowName + " " + stateNames[i];
                b.SetLabel(rowName);
                if (i == 1) { previews.Add(b); states.Add(1); }
                if (i == 2) { previews.Add(b); states.Add(2); }
                if (i == 3) { b.interactable = false; }
                Caption(c, rowName + " - " + stateNames[i], cols[i], y + 62f);
            }
        }

        // Edge chevrons
        SectionTitle(c, "EDGE CHEVRON BUTTON", 570f);
        string[] edgeCaps = { "left - normal (live)", "left - hover (frozen)", "right - normal (live)", "right - hover (frozen)" };
        for (int i = 0; i < 4; i++)
        {
            bool right = i >= 2;
            EdgeChevronButton e = Inst<EdgeChevronButton>(right ? "EdgeChevronButtonRight" : "EdgeChevronButtonLeft", c);
            At(e, cols[i], 670f, new Vector2(280f, 84f));
            e.name = "Edge " + edgeCaps[i];
            e.Setup(right ? "STORY" : "DUNGEON", right, null);
            if (i % 2 == 1) { previews.Add(e); states.Add(1); }
            Caption(c, edgeCaps[i], cols[i], 735f);
        }

        // Choice buttons
        SectionTitle(c, "CHOICE BUTTONS (event style)", 800f);
        string[] choiceText = { "Offer to trade what you have", "Take their catch by force", "Sail on without stopping" };
        ThemedButtonStyle[] choiceStyle = { ThemedButtonStyle.ChoiceBlue, ThemedButtonStyle.ChoiceRed, ThemedButtonStyle.ChoiceGreen };
        float[] cx = { -640f, 0f, 640f };
        for (int i = 0; i < 3; i++)
        {
            ThemedButton cb = UIKit.CreateChoiceButton(c, choiceText[i], choiceStyle[i]);
            cb.name = "Choice " + (i + 1);
            At(cb, cx[i], 900f, new Vector2(600f, 100f));
        }

        // --- Slider
        SectionTitle(c, "THEMED SLIDER", 1010f);
        ThemedSlider slider = Inst<ThemedSlider>("ThemedSlider", c);
        At(slider, -60f, 1090f, new Vector2(640f, 40f));
        TMP_Text sliderValue = AddText(c, "70%", UITextStyle.Body, 330f, 1090f, 140f, UITheme.Cream);
        slider.valueLabel = sliderValue;
        Caption(c, "track 640x16, fill #F4D27A, handle 24x24", -60f, 1140f);

        // --- Text styles
        SectionTitle(c, "TEXT STYLES", 1200f);
        AddText(c, "Press any key", UITextStyle.TitleCaps, 0f, 1280f, 1200f, UITheme.Cream);
        Caption(c, "Cinzel 48, ALL CAPS", 0f, 1330f);
        AddText(c, "Tap to begin", UITextStyle.Subtitle, 0f, 1390f, 1200f, UITheme.Cream);
        Caption(c, "Noto Serif 32, Title Case", 0f, 1435f);
        AddText(c, "Body text is Noto Serif at 30 px.", UITextStyle.Body, 0f, 1495f, 1200f, UITheme.Cream);
        Caption(c, "Noto Serif 30 (body, dialogue)", 0f, 1540f);

        // --- Compass, markers, ribbon
        SectionTitle(c, "COMPASS, HOTSPOT MARKER, HOVER RIBBON", 1620f);
        CompassIndicator compass = Inst<CompassIndicator>("CompassIndicator", c);
        At(compass, 0f, 1730f, new Vector2(640f, 96f));
        string[] stopNames = { "Story", "Lobby", "Dungeon", "Exit" };
        float[] sx = { -405f, -135f, 135f, 405f };
        ThemedButton[] stopButtons = new ThemedButton[4];
        for (int i = 0; i < 4; i++)
        {
            ThemedButton sb = UIKit.CreateButton(c, "Stop " + stopNames[i], ThemedButtonStyle.Secondary, new Vector2(240f, 60f));
            sb.SetLabel(stopNames[i]);
            sb.name = "Compass Stop " + stopNames[i];
            At(sb, sx[i], 1845f, new Vector2(240f, 60f));
            stopButtons[i] = sb;
        }

        HotspotMarker markerNormal = Inst<HotspotMarker>("HotspotMarker", c);
        At(markerNormal, -380f, 2000f, new Vector2(48f, 48f));
        Caption(c, "marker - normal", -380f, 2050f);
        HotspotMarker markerPulse = Inst<HotspotMarker>("HotspotMarker", c);
        At(markerPulse, -60f, 2000f, new Vector2(48f, 48f));
        Caption(c, "marker - pulsing", -60f, 2050f);

        HoverRibbon staticRibbon = Inst<HoverRibbon>("HoverRibbon", c);
        UIKit.Place(Rt(staticRibbon), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(420f, -2050f), new Vector2(340f, 84f));
        Caption(c, "hover ribbon 340x64 + 20 px pointer", 420f, 2090f);

        HoverRibbon liveRibbon = Inst<HoverRibbon>("HoverRibbon", c);
        UIKit.Place(Rt(liveRibbon), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, -1900f), new Vector2(340f, 84f));
        liveRibbon.gameObject.SetActive(false);
        ThemedButton ribbonBtn = UIKit.CreateButton(c, "Toggle ribbon", ThemedButtonStyle.Secondary, new Vector2(300f, 60f));
        ribbonBtn.SetLabel("Ribbon on marker");
        At(ribbonBtn, 700f, 2000f, new Vector2(300f, 60f));

        // --- Dialogue
        SectionTitle(c, "DIALOGUE BOX", 2160f);
        DialogueBox dialogue = Inst<DialogueBox>("DialogueBox", c);
        At(dialogue, 0f, 2360f, new Vector2(1400f, 220f));
        ThemedButton replay = UIKit.CreateButton(c, "Replay", ThemedButtonStyle.Primary, new Vector2(280f, 72f));
        replay.SetLabel("Replay");
        At(replay, 0f, 2520f, new Vector2(280f, 72f));

        // --- Panels (card style)
        SectionTitle(c, "PANEL, CREAM CARD, PLATE", 2600f);
        Image navyCard = UIKit.NewImage("Navy Card", c, "panel", Image.Type.Sliced, Color.white, false);
        At(navyCard, -480f, 2790f, new Vector2(420f, 260f));
        TMP_Text t1 = UIKit.CreateText(navyCard.transform, "Text", "Navy panel", UITextStyle.Subtitle, UITheme.Cream, TextAlignmentOptions.Center);
        UIKit.Stretch(t1.rectTransform);
        Image creamCard = UIKit.NewImage("Cream Card", c, "panel_cream", Image.Type.Sliced, Color.white, false);
        At(creamCard, 0f, 2790f, new Vector2(420f, 260f));
        TMP_Text t2 = UIKit.CreateText(creamCard.transform, "Text", "Cream card", UITextStyle.Subtitle, UITheme.TextDark, TextAlignmentOptions.Center);
        UIKit.Stretch(t2.rectTransform);
        Image plate = UIKit.NewImage("Plate", c, "plate", Image.Type.Sliced, Color.white, false);
        At(plate, 480f, 2790f, new Vector2(340f, 64f));
        TMP_Text t3 = UIKit.CreateText(plate.transform, "Text", "Name plate", UITextStyle.Label, UITheme.Cream, TextAlignmentOptions.Center);
        UIKit.Stretch(t3.rectTransform);

        // --- Modals and header
        SectionTitle(c, "CONFIRM PANEL AND SETTINGS PANEL", 2960f);
        ThemedButton openConfirm = UIKit.CreateButton(c, "Open confirm", ThemedButtonStyle.Primary, new Vector2(360f, 72f));
        openConfirm.SetLabel("Open ConfirmPanel");
        At(openConfirm, -250f, 3050f, new Vector2(360f, 72f));
        ThemedButton openSettings = UIKit.CreateButton(c, "Open settings", ThemedButtonStyle.Primary, new Vector2(360f, 72f));
        openSettings.SetLabel("Open SettingsPanel");
        At(openSettings, 250f, 3050f, new Vector2(360f, 72f));
        TMP_Text status = AddText(c, "Status: nothing pressed yet", UITextStyle.Small, 0f, 3115f, 1400f, UITheme.Accent);

        ScreenHeader header = Inst<ScreenHeader>("ScreenHeader", c);
        Rt(header).anchoredPosition = new Vector2(0f, -3150f);
        header.titleLabel.text = "LEGENDS";

        // Modal prefabs live on the canvas root so they cover the whole screen.
        ConfirmPanel confirm = Inst<ConfirmPanel>("ConfirmPanel", canvas.transform);
        SettingsPanel settings = Inst<SettingsPanel>("SettingsPanel", canvas.transform);

        // Wire the gallery component
        gallery.previewButtons = previews.ToArray();
        gallery.previewStates = states.ToArray();
        gallery.dialogue = dialogue;
        gallery.replayButton = replay;
        gallery.compass = compass;
        gallery.compassButtons = stopButtons;
        gallery.pulsingMarker = markerPulse;
        gallery.normalMarker = markerNormal;
        gallery.liveRibbon = liveRibbon;
        gallery.ribbonButton = ribbonBtn;
        gallery.openConfirmButton = openConfirm;
        gallery.openSettingsButton = openSettings;
        gallery.confirmPanel = confirm;
        gallery.settingsPanel = settings;
        gallery.statusLabel = status;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log("[UIKitBuilder] Gallery scene saved: " + ScenePath);
    }
}
