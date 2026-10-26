using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu: Oddyseus/Build Splash Screen (Title scene)
/// Open the Title scene first. The menu backs the scene up, removes the old title canvases
/// ("TitleCanvas", "Title Canvas"), and builds one clean "Splash Canvas".
/// </summary>
public static class SplashBuilder
{
    const string ArtDir = "Assets/_Project/Art/UI/Splash";
    const string BgPath = ArtDir + "/splash_background.png";
    const string PillPath = ArtDir + "/splash_prompt_plate.png";
    const string LogoPath = ArtDir + "/splash_logo.png";
    const string ArtVersionKey = "Oddyseus.SplashArtVersion";
    const int ArtVersion = 2;   // raise this when the placeholder look changes

    static readonly Color NavyDeep = Hex("07203A");
    static readonly Color Navy = Hex("0B2A4A");
    static readonly Color Horizon = Hex("164670");
    static readonly Color Gold = Hex("F2C66D");
    static readonly Color Accent = Hex("FFD166");
    static readonly Color Cream = Hex("E8E6D9");
    static readonly Color BlueLight = Hex("8FB7D6");

    static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + hex, out c);
        return c;
    }

    [MenuItem("Oddyseus/Build Splash Screen (Title scene)")]
    static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Splash Screen",
                "The game is in Play mode (the blue Play button at the top). Changes made in Play mode are lost.\n\n" +
                "Click the Play button once to stop it, then run this menu again.", "OK");
            return;
        }
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Title")
        {
            EditorUtility.DisplayDialog("Splash Screen",
                "Open the Title scene first (Assets/_Project/Scenes/Title.unity), then run this menu again.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1. Backup
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string backupDir = Path.GetDirectoryName(scene.path).Replace('\\', '/') + "/_Backups";
        Directory.CreateDirectory(backupDir);
        AssetDatabase.Refresh();
        AssetDatabase.CopyAsset(scene.path, backupDir + "/Title_" + stamp + ".unity");

        // 2. Read what the old title used (fonts, art, version text), then remove it
        TMP_FontAsset titleFont = null, bodyFont = null;
        Sprite oldArt = null;
        string versionText = "v0.1";
        float bestArea = 0f;
        string removed = "";
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name != "TitleCanvas" && go.name != "Title Canvas" && go.name != "Splash Canvas") continue;
            foreach (TMP_Text t in go.GetComponentsInChildren<TMP_Text>(true))
            {
                string s = (t.text ?? "").ToLowerInvariant();
                if (titleFont == null && s.Contains("oddyseus")) titleFont = t.font;
                if (bodyFont == null && (s.Contains("tap") || s.Contains("begin"))) bodyFont = t.font;
                if (Regex.IsMatch(t.text ?? "", @"^v\d")) versionText = t.text;
            }
            foreach (Image im in go.GetComponentsInChildren<Image>(true))
            {
                if (im.sprite == null) continue;
                float a = im.sprite.rect.width * im.sprite.rect.height;
                if (a > bestArea) { bestArea = a; oldArt = im.sprite; }
            }
            removed += go.name + ", ";
            UnityEngine.Object.DestroyImmediate(go);
        }
        if (titleFont == null) titleFont = FindFont("Cinzel");
        if (bodyFont == null) bodyFont = FindFont("Noto");
        if (bodyFont == null) bodyFont = titleFont;

        // 3. Placeholder sprites
        Directory.CreateDirectory(ArtDir);
        bool refreshArt = EditorPrefs.GetInt(ArtVersionKey, 0) < ArtVersion;
        if (refreshArt || !File.Exists(BgPath)) File.WriteAllBytes(BgPath, MakeBackground().EncodeToPNG());
        if (refreshArt || !File.Exists(PillPath)) File.WriteAllBytes(PillPath, MakePlate().EncodeToPNG());
        EditorPrefs.SetInt(ArtVersionKey, ArtVersion);
        AssetDatabase.ImportAsset(BgPath, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(PillPath, ImportAssetOptions.ForceSynchronousImport);
        SetupSprite(BgPath, Vector4.zero);
        SetupSprite(PillPath, new Vector4(28, 28, 28, 28));
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
        Sprite plateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PillPath);

        // 4. Canvas
        GameObject canvasGo = new GameObject("Splash Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.layer = 5;
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        SplashTapToStart splash = canvasGo.AddComponent<SplashTapToStart>();
        Transform root = canvasGo.transform;

        // Background (the whole screen is the tap target)
        Image bg = NewImage("Background (replace sprite)", root, bgSprite, Image.Type.Simple, Color.white, true);
        Stretch(bg.rectTransform);
        Image art = NewImage("Ship Art (optional, replace sprite)", root, oldArt, Image.Type.Simple, new Color(1f, 1f, 1f, 0.5f), false);
        Stretch(art.rectTransform);

        // Logo: the supplied logo image (Art/UI/Splash/splash_logo.png). Falls back to text if the file is missing.
        RectTransform logo = NewRect("Logo", root);
        Sprite logoSprite = null;
        if (File.Exists(LogoPath))
        {
            AssetDatabase.ImportAsset(LogoPath, ImportAssetOptions.ForceSynchronousImport);
            SetupSprite(LogoPath, Vector4.zero);
            logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        }
        if (logoSprite != null)
        {
            float lw = 1000f;
            float lh = lw * logoSprite.rect.height / logoSprite.rect.width;
            Place(logo, new Vector2(0f, 150f), new Vector2(lw, lh));
            Image logoImg = NewImage("Logo (replace sprite)", logo, logoSprite, Image.Type.Simple, Color.white, false);
            logoImg.preserveAspect = true;
            Stretch(logoImg.rectTransform);
        }
        else
        {
            Place(logo, new Vector2(0f, 130f), new Vector2(1100f, 440f));
            Ornament(logo, "Ornament Top", 205f);
            TextMeshProUGUI title = NewText("Title", logo, "ODDYSEUS", titleFont, 150f, Gold, 10f);
            Place(title.rectTransform, new Vector2(0f, 60f), new Vector2(1100f, 190f));
            title.enableAutoSizing = true; title.fontSizeMin = 80f; title.fontSizeMax = 150f;
            TextMeshProUGUI sub = NewText("Subtitle", logo, "JOURNEY'S END", titleFont, 54f, Cream, 16f);
            Place(sub.rectTransform, new Vector2(0f, -110f), new Vector2(900f, 70f));
            Ornament(logo, "Ornament Bottom", -190f);
        }

        // Prompt (plate behind the text so it never blends with the background)
        RectTransform prompt = NewRect("Prompt", root);
        Place(prompt, new Vector2(0f, -300f), new Vector2(840f, 170f));
        CanvasGroup group = prompt.gameObject.AddComponent<CanvasGroup>();
        Image plate = NewImage("Prompt Plate (replace sprite)", prompt, plateSprite, Image.Type.Sliced, Color.white, true);
        Stretch(plate.rectTransform);
        TextMeshProUGUI p1 = NewText("Prompt Line 1", prompt, "TAP TO BEGIN", titleFont, 56f, Accent, 6f);
        Place(p1.rectTransform, new Vector2(0f, 32f), new Vector2(780f, 76f));
        TextMeshProUGUI p2 = NewText("Prompt Line 2", prompt, "Click anywhere on the screen", bodyFont, 32f, Cream, 1f);
        p2.fontStyle = FontStyles.Normal;
        Place(p2.rectTransform, new Vector2(0f, -34f), new Vector2(780f, 50f));

        // Version, bottom left
        TextMeshProUGUI ver = NewText("Version", root, versionText, bodyFont, 24f, BlueLight, 0f);
        ver.fontStyle = FontStyles.Normal;
        ver.alignment = TextAlignmentOptions.BottomLeft;
        RectTransform vr = ver.rectTransform;
        vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.zero; vr.pivot = Vector2.zero;
        vr.anchoredPosition = new Vector2(40f, 30f); vr.sizeDelta = new Vector2(240f, 40f);

        // Fade to black (last, on top)
        Image fade = NewImage("Fade", root, null, Image.Type.Simple, new Color(0f, 0f, 0f, 0f), false);
        Stretch(fade.rectTransform);

        splash.promptGroup = group;
        splash.fade = fade;
        splash.artOverlay = art;
        splash.showArt = oldArt != null;
        splash.artOpacity = 0.5f;
        art.enabled = oldArt != null;
        art.color = new Color(1f, 1f, 1f, 0.5f);

        EnsureEventSystem();

        Selection.activeGameObject = canvasGo;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SplashBuilder] Done. Removed: " + (removed.Length > 0 ? removed : "nothing") +
                  " | title font: " + (titleFont != null ? titleFont.name : "TMP default") +
                  " | backup: " + backupDir + "/Title_" + stamp + ".unity");
        EditorUtility.DisplayDialog("Splash Screen",
            "Done. Press Play and click anywhere on the splash to start (keys do nothing).\n\n" +
            "The old canvases (" + (removed.Length > 0 ? removed.TrimEnd(',', ' ') : "none") + ") were removed. " +
            "The old scene is saved in " + backupDir + ".\n\n" +
            "Ship art behind the logo: select Splash Canvas, tick or untick Show Art (and Art Opacity) in the Inspector.", "OK");
    }

    // ------------------------------------------------------------------
    static TMP_FontAsset FindFont(string search)
    {
        string[] guids = AssetDatabase.FindAssets(search + " t:TMP_FontAsset");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Image NewImage(string name, Transform parent, Sprite sprite, Image.Type type, Color color, bool raycast)
    {
        RectTransform rt = NewRect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        if (sprite != null) img.type = type;
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    static TextMeshProUGUI NewText(string name, Transform parent, string text, TMP_FontAsset font,
                                   float size, Color color, float spacing)
    {
        RectTransform rt = NewRect(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.UpperCase;
        t.characterSpacing = spacing;
        t.raycastTarget = false;
        return t;
    }

    // Thin gold line, a small diamond in the middle, thin gold line.
    static void Ornament(Transform parent, string name, float y)
    {
        RectTransform root = NewRect(name, parent);
        Place(root, new Vector2(0f, y), new Vector2(700f, 20f));
        Color line = new Color(Gold.r, Gold.g, Gold.b, 0.85f);
        Image l = NewImage("Line L", root, null, Image.Type.Simple, line, false);
        Place(l.rectTransform, new Vector2(-190f, 0f), new Vector2(300f, 2f));
        Image r = NewImage("Line R", root, null, Image.Type.Simple, line, false);
        Place(r.rectTransform, new Vector2(190f, 0f), new Vector2(300f, 2f));
        Image d = NewImage("Diamond", root, null, Image.Type.Simple, Gold, false);
        Place(d.rectTransform, Vector2.zero, new Vector2(14f, 14f));
        d.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
    }

    static void SetupSprite(string path, Vector4 border)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100f;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.filterMode = FilterMode.Bilinear;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.spriteBorder = border;
        imp.SaveAndReimport();
    }

    // Simple night-sea gradient with a soft glow behind the logo. No details, so text stays readable.
    static Texture2D MakeBackground()
    {
        int w = 256, h = 144;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h; // 0 bottom, 1 top
            Color c = v > 0.55f
                ? Color.Lerp(Navy, NavyDeep, (v - 0.55f) / 0.45f)
                : Color.Lerp(Horizon, Navy, v / 0.55f);
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w;
                float dx = (u - 0.5f) * 2f * 0.7f;
                float dy = (v - 0.6f) * 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Color p = c;
                float glow = Mathf.Clamp01(1f - d) * 0.07f;
                p.r += glow * 0.4f; p.g += glow * 0.8f; p.b += glow * 1.0f;
                p = Color.Lerp(p, NavyDeep, Mathf.Clamp01(d - 0.75f) * 0.6f);
                float noise = (((x * 7 + y * 13) % 5) - 2) * 0.0005f;
                p.r += noise; p.g += noise; p.b += noise;
                p.a = 1f;
                px[y * w + x] = p;
            }
        }
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels(px);
        t.Apply(false, false);
        return t;
    }

    // Rounded plate: dark navy, gold border. 9-slice border 28.
    static Texture2D MakePlate()
    {
        int n = 64;
        float half = n * 0.5f, r = 26f, bw = 4f;
        Color fill = new Color(NavyDeep.r, NavyDeep.g, NavyDeep.b, 0.94f);
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float cx = Mathf.Abs(x + 0.5f - half) - (half - 1f) + r;
                float cy = Mathf.Abs(y + 0.5f - half) - (half - 1f) + r;
                float ox = Mathf.Max(cx, 0f), oy = Mathf.Max(cy, 0f);
                float d = Mathf.Min(Mathf.Max(cx, cy), 0f) + Mathf.Sqrt(ox * ox + oy * oy) - r;
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (d + bw));
                Color c = Color.Lerp(Gold, fill, inner);
                c.a = outer * Mathf.Lerp(1f, fill.a, inner);
                px[y * n + x] = c;
            }
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.SetPixels(px);
        t.Apply(false, false);
        return t;
    }

    static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
        GameObject go = new GameObject("EventSystem", typeof(EventSystem));
        Type t = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
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
}