using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Title / splash screen ("Night Sea and Bronze"). ONLY a mouse click / screen tap goes to the Lobby (keys do nothing).
///
/// The scene made by SceneBuilder only has old placeholder UI on this object. At runtime this script
/// hides that and builds the new layout with the UI Kit, so NO scene rebuild is needed.
///
/// ART SLOT: put a PNG at Assets/.../Resources/UIKit/title_bg.png (import as Sprite, 2D and UI).
/// It is scaled to cover the screen. Until it exists, a night-sea gradient is drawn from code.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    // Kept only because SceneBuilder still assigns it (legacy placeholder text). Not used any more.
    [HideInInspector] public Text prompt;

    public string versionLabel = "v0.1";
    public bool showArtHint = false;        // shows the "ART: ..." debug label when no title_bg.png exists
    public float inputDelay = 0.5f;         // ignore clicks right after the scene loads

    public enum BorderMode { None, AroundLogo, FullWidth }
    [Tooltip("None = no border, AroundLogo = short strips above/below the logo, FullWidth = strips across the whole screen")]
    public BorderMode border = BorderMode.FullWidth;

    [Tooltip("Optional: drag the logo sprite here. If empty, Resources/UIKit/title_logo is used.")]
    public Sprite logoSprite;
    [Tooltip("Soft dark band behind the logo so it stays readable on any artwork")]
    public bool logoBackdrop = true;
    [Range(0f, 1f)] public float backdropAlpha = 0.6f;
    public float logoY = 150f;
    [Tooltip("Logo width in canvas units (reference 1920x1080)")]
    public float logoWidth = 1000f;
    [Tooltip("Space between the logo and the Greek key strips (bigger = more airy)")]
    public float borderPadding = 100f;

    const string BackgroundResource = "UIKit/title_bg";
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);

    Canvas ui;
    CanvasGroup logoGroup, promptGroup, borderGroup, backdropGroup;
    readonly List<RawImage> fullBars = new List<RawImage>();
    float startTime;
    bool going;
    static Texture2D _keyTex;
    static Sprite _diamondSprite, _backdropFull, _backdropSoft;

    void Awake()
    {
        startTime = Time.unscaledTime;

        // Hide the old placeholder UI that lives on this object.
        Canvas old = GetComponent<Canvas>();
        if (old != null) old.enabled = false;
        for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);

        BuildUI();
    }

    void Update()
    {
        float t = Time.unscaledTime - startTime;
        if (logoGroup != null) logoGroup.alpha = Mathf.Clamp01(t / 1.2f);
        if (borderGroup != null) borderGroup.alpha = Mathf.Clamp01(t / 1.2f);
        if (backdropGroup != null) backdropGroup.alpha = Mathf.Clamp01(t / 1.2f);
        for (int i = 0; i < fullBars.Count; i++)
        {
            // keep tiles square (32 units each) at any screen width
            float w = fullBars[i].rectTransform.rect.width;
            fullBars[i].uvRect = new Rect(0f, 0f, w / 32f, 1f);
        }
        if (promptGroup != null)
        {
            float pulse = 0.8f + 0.2f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f));
            promptGroup.alpha = Mathf.Clamp01(t - 0.8f) * pulse;
        }
    }

    // Called only when the player clicks / taps the screen.
    void OnScreenClicked()
    {
        if (going || Time.unscaledTime - startTime < inputDelay) return;
        going = true;
        SceneFader.LoadScene(GameScenes.Lobby, 0.8f);
    }

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------
    void BuildUI()
    {
        ui = UIKit.CreateOverlayCanvas("Title Canvas", 0);
        Transform root = ui.transform;

        BuildBackground(root);
        BuildLogo(root);
        BuildPrompt(root);

        TMP_Text ver = UIKit.CreateText(root, "Version", versionLabel, UITextStyle.Small,
                                        WithAlpha(UITheme.Cream, 0.7f), TextAlignmentOptions.MidlineLeft);
        UIKit.Place(ver.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 20f), new Vector2(240f, 40f));

        BuildClickCatcher(root);
        EnsureEventSystem();
    }

    void BuildBackground(Transform root)
    {
        Image bg = UIKit.NewImage("Background (replace sprite)", root, null, Image.Type.Simple, Color.white, false);
        Sprite art = Resources.Load<Sprite>(BackgroundResource);
        if (art != null)
        {
            bg.sprite = art;
            UIKit.Place(bg.rectTransform, C, C, Vector2.zero, UITheme.ReferenceResolution);
            AspectRatioFitter fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = art.rect.width / art.rect.height;
            return;
        }

        bg.sprite = NightSeaSprite();
        UIKit.Stretch(bg.rectTransform);

        if (showArtHint)
        {
            TMP_Text tag = UIKit.CreateText(root, "ART Label", "ART: title illustration (Resources/UIKit/title_bg.png)",
                                            UITextStyle.Small, WithAlpha(UITheme.Cream, 0.35f), TextAlignmentOptions.MidlineLeft);
            UIKit.Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -20f), new Vector2(1000f, 40f));
        }
    }

    void BuildLogo(Transform root)
    {
        Sprite logoArt = logoSprite != null ? logoSprite : LoadLogo();
        float halfH = logoArt != null ? logoWidth * logoArt.rect.height / logoArt.rect.width * 0.5f + borderPadding : 158f + (borderPadding - 34f);
        BuildBackdrop(root, logoY, halfH);   // created first so it sits behind everything else of the logo

        RectTransform logo = UIKit.NewRect("Logo", root);
        UIKit.Place(logo, C, C, new Vector2(0f, logoY), new Vector2(1100f, 360f));
        logoGroup = logo.gameObject.AddComponent<CanvasGroup>();
        logoGroup.alpha = 0f;

        // Logo picture (Resources/UIKit/title_logo.png). If it exists it replaces the text logo below.
        if (logoArt != null)
        {
            // NewImage takes a sprite NAME (string), so pass null and assign the Sprite afterwards.
            Image li = UIKit.NewImage("Logo Image (replace Resources/UIKit/title_logo.png)", logo, null, Image.Type.Simple, Color.white, false);
            li.sprite = logoArt;
            li.preserveAspect = true;
            float lw = logoWidth;
            float lh = lw * logoArt.rect.height / logoArt.rect.width;
            UIKit.Place(li.rectTransform, C, C, Vector2.zero, new Vector2(lw, lh));
            Shadow sh = li.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.6f);
            sh.effectDistance = new Vector2(4f, -4f);
            BuildBorders(root, logoY, halfH);
            return;
        }

        // Drop shadow first (behind), then the gold title.
        TMP_Text shadow = Txt(logo, "Title Shadow", "ODDYSEUS", UITextStyle.TitleCaps, WithAlpha(Color.black, 0.55f),
                              150f, 6f, new Vector2(0f, 56f), new Vector2(1100f, 170f));
        shadow.raycastTarget = false;

        TMP_Text title = Txt(logo, "Title", "ODDYSEUS", UITextStyle.TitleCaps, Color.white,
                             150f, 6f, new Vector2(0f, 62f), new Vector2(1100f, 170f));
        title.enableVertexGradient = true;
        Color top = UITheme.Accent;
        Color bottom = Color.Lerp(UITheme.Gold, UITheme.Bronze, 0.6f);
        title.colorGradient = new VertexGradient(top, top, bottom, bottom);

        Image rule = UIKit.NewImage("Divider", logo, null, Image.Type.Simple, WithAlpha(UITheme.Gold, 0.7f), false);
        UIKit.Place(rule.rectTransform, C, C, new Vector2(0f, -42f), new Vector2(480f, 2f));
        Image dia = UIKit.NewImage("Divider Diamond (replace sprite)", logo, null, Image.Type.Simple, Color.white, false);
        dia.sprite = DiamondSprite();
        UIKit.Place(dia.rectTransform, C, C, new Vector2(0f, -42f), new Vector2(22f, 22f));

        Txt(logo, "Subtitle", "JOURNEY'S END", UITextStyle.Label, UITheme.Gold,
            56f, 14f, new Vector2(0f, -94f), new Vector2(1100f, 80f));

        BuildBorders(root, logoY, halfH);
    }

    // Soft dark band behind the logo (full width when border = FullWidth, otherwise a fading plate).
    void BuildBackdrop(Transform root, float centerY, float halfH)
    {
        if (!logoBackdrop) return;

        RectTransform holder = UIKit.NewRect("Logo Backdrop", root);
        UIKit.Stretch(holder);
        backdropGroup = holder.gameObject.AddComponent<CanvasGroup>();
        backdropGroup.alpha = 0f;
        backdropGroup.blocksRaycasts = false;

        bool full = border == BorderMode.FullWidth;
        Image img = UIKit.NewImage("Backdrop", holder, null, Image.Type.Simple, WithAlpha(UITheme.NavyDeep, backdropAlpha), false);
        img.sprite = BackdropSprite(full);
        RectTransform r = img.rectTransform;
        float height = halfH * 2f + 150f;
        if (full)
        {
            r.anchorMin = new Vector2(0f, 0.5f);
            r.anchorMax = new Vector2(1f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0f, centerY);
            r.sizeDelta = new Vector2(0f, height);
        }
        else
        {
            UIKit.Place(r, C, C, new Vector2(0f, centerY), new Vector2(1500f, height));
        }
    }

    // White pixels, alpha = soft profile. The Image color gives the tint and strength.
    static Sprite BackdropSprite(bool fullWidth)
    {
        Sprite cached = fullWidth ? _backdropFull : _backdropSoft;
        if (cached != null) return cached;

        const int w = 64, h = 64;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            float fy = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - Mathf.Abs(v - 0.5f) * 2f) / 0.6f));
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w;
                float fx = fullWidth ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - Mathf.Abs(u - 0.5f) * 2f) / 0.4f));
                px[y * w + x] = new Color(1f, 1f, 1f, fy * fx);
            }
        }
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(px);
        tex.Apply(false, false);
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        if (fullWidth) _backdropFull = sp; else _backdropSoft = sp;
        return sp;
    }

    // Greek key strips above and below the logo. centerY = logo center, halfH = distance from center to each strip.
    void BuildBorders(Transform root, float centerY, float halfH)
    {
        if (border == BorderMode.None) return;

        RectTransform holder = UIKit.NewRect("Border", root);
        UIKit.Stretch(holder);
        borderGroup = holder.gameObject.AddComponent<CanvasGroup>();
        borderGroup.alpha = 0f;
        borderGroup.blocksRaycasts = false;

        Color keyColor = WithAlpha(Color.white, 0.9f);
        float[] ys = { centerY + halfH, centerY - halfH };
        for (int i = 0; i < ys.Length; i++)
        {
            RawImage bar = MakeKey(holder, i == 0 ? "Greek Key Top" : "Greek Key Bottom", new Vector2(0f, ys[i]), keyColor);
            if (border == BorderMode.FullWidth)
            {
                RectTransform r = bar.rectTransform;
                r.anchorMin = new Vector2(0f, 0.5f);
                r.anchorMax = new Vector2(1f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(0f, ys[i]);
                r.sizeDelta = new Vector2(0f, 32f);
                fullBars.Add(bar);
            }
        }
    }

    static readonly string[] LogoPaths = { "UIKit/title_logo", "UIKit/Title_logo", "title_logo", "Title_logo" };

    static Sprite LoadLogo()
    {
        for (int i = 0; i < LogoPaths.Length; i++)
        {
            string p = LogoPaths[i];
            Sprite sp = Resources.Load<Sprite>(p);
            if (sp != null) return sp;

            Sprite[] all = Resources.LoadAll<Sprite>(p);   // Sprite Mode = Multiple
            if (all != null && all.Length > 0) return all[0];

            Texture2D tex = Resources.Load<Texture2D>(p);
            if (tex != null) return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
#if UNITY_EDITOR
        // Editor-only fallback: find title_logo anywhere in the project, so it works even outside Resources/.
        string[] guids = UnityEditor.AssetDatabase.FindAssets("title_logo");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!path.ToLowerInvariant().EndsWith("title_logo.png")) continue;
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            for (int k = 0; k < assets.Length; k++)
            {
                Sprite sp = assets[k] as Sprite;
                if (sp != null)
                {
                    Debug.LogWarning("[TitleScreen] title_logo loaded from '" + path + "' (editor only). " +
                                     "For builds, move it to Assets/_Project/Resources/UIKit/title_logo.png.");
                    return sp;
                }
            }
            Texture2D t2 = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t2 != null) return Sprite.Create(t2, new Rect(0, 0, t2.width, t2.height), new Vector2(0.5f, 0.5f), 100f);
        }
#endif
        Debug.LogWarning("[TitleScreen] title_logo not found. Put it at Assets/_Project/Resources/UIKit/title_logo.png " +
                         "(the folder must be named exactly 'Resources'), or drag the sprite into 'Logo Sprite' on the TitleScreen component.");
        return null;
    }

    void BuildPrompt(Transform root)
    {
        RectTransform box = UIKit.NewRect("Prompt", root);
        UIKit.Place(box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1400f, 160f));
        promptGroup = box.gameObject.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0f;

        // Dark plate behind the prompt so the text never blends with the artwork.
        Image plate = UIKit.NewImage("Prompt Plate", box, null, Image.Type.Simple, new Color(0.03f, 0.1f, 0.18f, 0.78f), false);
        UIKit.Place(plate.rectTransform, C, C, Vector2.zero, new Vector2(760f, 150f));
        Image edge = UIKit.NewImage("Prompt Plate Line", box, null, Image.Type.Simple, WithAlpha(UITheme.Gold, 0.8f), false);
        UIKit.Place(edge.rectTransform, C, C, new Vector2(0f, -75f), new Vector2(760f, 3f));
        Image edgeTop = UIKit.NewImage("Prompt Plate Line Top", box, null, Image.Type.Simple, WithAlpha(UITheme.Gold, 0.8f), false);
        UIKit.Place(edgeTop.rectTransform, C, C, new Vector2(0f, 75f), new Vector2(760f, 3f));

        Txt(box, "Click To Start", "Click to start", UITextStyle.TitleCaps, UITheme.Accent,
            52f, 4f, new Vector2(0f, 28f), new Vector2(1400f, 70f));
        Txt(box, "Tap To Begin", "Click anywhere on the screen", UITextStyle.Subtitle, UITheme.Cream,
            30f, 0f, new Vector2(0f, -30f), new Vector2(1400f, 50f));
    }

    // Invisible full-screen button, created LAST so it sits on top and receives every click / tap.
    void BuildClickCatcher(Transform root)
    {
        if (ui.GetComponent<GraphicRaycaster>() == null) ui.gameObject.AddComponent<GraphicRaycaster>();

        RectTransform r = UIKit.NewRect("Click To Continue", root);
        UIKit.Stretch(r);
        Image img = r.gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);   // transparent but still receives raycasts
        img.raycastTarget = true;

        Button b = r.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.targetGraphic = img;
        b.onClick.AddListener(OnScreenClicked);
    }

    // UI clicks need an EventSystem in the scene.
    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
#if ENABLE_LEGACY_INPUT_MANAGER
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
#else
        Debug.LogWarning("[TitleScreen] No EventSystem in the scene. Add one: GameObject > UI > Event System.");
#endif
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    static TMP_Text Txt(Transform parent, string name, string text, UITextStyle style, Color color,
                        float size, float spacing, Vector2 pos, Vector2 box)
    {
        TMP_Text t = UIKit.CreateText(parent, name, text, style, color, TextAlignmentOptions.Center);
        t.fontSize = size;
        t.characterSpacing = spacing;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        UIKit.Place(t.rectTransform, C, C, pos, box);
        return t;
    }

    static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }

    // ------------------------------------------------------------------
    // Procedural art (used only when Resources/UIKit/key_tile.png or diamond.png do not exist)
    // ------------------------------------------------------------------
    // A repeating strip. RawImage + uvRect always repeats the tile (does not depend on Image.Type.Tiled).
    static RawImage MakeKey(Transform parent, string name, Vector2 pos, Color color)
    {
        const float tile = 32f, width = 576f, height = 32f;
        RectTransform r = UIKit.NewRect(name, parent);
        UIKit.Place(r, C, C, pos, new Vector2(width, height));
        RawImage ri = r.gameObject.AddComponent<RawImage>();
        ri.texture = KeyTexture();
        ri.color = color;
        ri.raycastTarget = false;
        ri.uvRect = new Rect(0f, 0f, width / tile, 1f);
        return ri;
    }

    static Texture2D KeyTexture()
    {
        if (_keyTex != null) return _keyTex;

        Texture2D art = Resources.Load<Texture2D>("UIKit/key_tile");
        if (art != null)
        {
            art.wrapMode = TextureWrapMode.Repeat;
            return _keyTex = art;
        }

        const int n = 32;
        Color[] px = new Color[n * n];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
        Color g = UITheme.Gold;
        Fill(px, n, 0, 1, n, 2, g);     // top and bottom thin rails
        Fill(px, n, 0, 29, n, 2, g);
        Fill(px, n, 0, 5, n, 3, g);     // base line that joins the tiles together
        Fill(px, n, 4, 5, 3, 21, g);    // left post
        Fill(px, n, 4, 23, 24, 3, g);   // top bar
        Fill(px, n, 25, 11, 3, 15, g);  // right post
        Fill(px, n, 12, 11, 16, 3, g);  // inner bottom bar
        Fill(px, n, 12, 11, 3, 9, g);   // inner post
        Fill(px, n, 12, 17, 9, 3, g);   // inner top bar (spiral end)

        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(px);
        tex.Apply(false, false);
        return _keyTex = tex;
    }

    static Sprite DiamondSprite()
    {
        if (_diamondSprite != null) return _diamondSprite;
        Sprite art = Resources.Load<Sprite>("UIKit/diamond");
        if (art != null) return _diamondSprite = art;

        const int n = 32;
        Color[] px = new Color[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = (Mathf.Abs(x - c) + Mathf.Abs(y - c)) / c;   // 0 center, 1 edge
                float a = Mathf.Clamp01((1f - d) * c * 0.6f);
                px[y * n + x] = d <= 1f ? WithAlpha(UITheme.Gold, a) : Color.clear;
            }
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(px);
        tex.Apply(false, false);
        return _diamondSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    static void Fill(Color[] px, int n, int x, int y, int w, int h, Color c)
    {
        for (int yy = y; yy < y + h && yy < n; yy++)
            for (int xx = x; xx < x + w && xx < n; xx++)
                px[yy * n + xx] = c;
    }

    // Placeholder background: night sky with stars, a bronze glow on the horizon and a dark sea.
    static Sprite NightSeaSprite()
    {
        const int w = 512, h = 288;
        const float horizon = 0.34f;   // fraction of the height, measured from the bottom
        Color skyTop = UITheme.Hex("04101F");
        Color skyLow = UITheme.Hex("123F66");
        Color seaTop = UITheme.Hex("0B2A4A");
        Color seaLow = UITheme.Hex("051A30");

        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            Color baseColor;
            if (v < horizon) baseColor = Color.Lerp(seaLow, seaTop, v / horizon);
            else baseColor = Color.Lerp(skyLow, skyTop, Mathf.Pow((v - horizon) / (1f - horizon), 0.7f));

            float band = Mathf.Exp(-Mathf.Pow((v - horizon) / 0.07f, 2f));
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w;
                float across = Mathf.Exp(-Mathf.Pow((u - 0.5f) / 0.35f, 2f));
                Color c = Color.Lerp(baseColor, UITheme.Bronze, band * across * 0.45f);
                c.a = 1f;
                px[y * w + x] = c;
            }
        }

        System.Random rng = new System.Random(7);
        int minY = (int)(h * (horizon + 0.08f));
        for (int i = 0; i < 110; i++)
        {
            int sx = rng.Next(0, w);
            int sy = rng.Next(minY, h);
            float b = 0.35f + 0.65f * (float)rng.NextDouble();
            int idx = sy * w + sx;
            px[idx] = Color.Lerp(px[idx], UITheme.Cream, b);
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(px);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }
}