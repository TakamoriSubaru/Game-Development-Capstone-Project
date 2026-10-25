using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Title / splash screen ("Night Sea and Bronze"). Press any key / tap goes to the Lobby.
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

    const string BackgroundResource = "UIKit/title_bg";
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);

    Canvas ui;
    CanvasGroup logoGroup, promptGroup;
    float startTime;
    bool going;

    void Awake()
    {
        startTime = Time.time;

        // Hide the old placeholder UI that lives on this object.
        Canvas old = GetComponent<Canvas>();
        if (old != null) old.enabled = false;
        for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);

        BuildUI();
    }

    void Update()
    {
        float t = Time.time - startTime;
        if (logoGroup != null) logoGroup.alpha = Mathf.Clamp01(t / 1.2f);
        if (promptGroup != null)
        {
            float pulse = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.time * 3f));
            promptGroup.alpha = Mathf.Clamp01(t - 0.8f) * pulse;
        }

        if (!going && GameInput.AnyKeyDown)
        {
            going = true;
            SceneFader.LoadScene(GameScenes.Lobby, 0.8f);
        }
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

        TMP_Text tag = UIKit.CreateText(root, "ART Label", "ART: title illustration (Resources/UIKit/title_bg.png)",
                                        UITextStyle.Small, WithAlpha(UITheme.Cream, 0.35f), TextAlignmentOptions.MidlineLeft);
        UIKit.Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -20f), new Vector2(1000f, 40f));
    }

    void BuildLogo(Transform root)
    {
        RectTransform logo = UIKit.NewRect("Logo", root);
        UIKit.Place(logo, C, C, new Vector2(0f, 150f), new Vector2(1100f, 360f));
        logoGroup = logo.gameObject.AddComponent<CanvasGroup>();
        logoGroup.alpha = 0f;

        Color keyColor = WithAlpha(Color.white, 0.9f);
        Image keyTop = UIKit.NewImage("Greek Key Top (replace sprite)", logo, "key_tile", Image.Type.Tiled, keyColor, false);
        UIKit.Place(keyTop.rectTransform, C, C, new Vector2(0f, 158f), new Vector2(576f, 32f));

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
        Image dia = UIKit.NewImage("Divider Diamond (replace sprite)", logo, "diamond", Image.Type.Simple, Color.white, false);
        UIKit.Place(dia.rectTransform, C, C, new Vector2(0f, -42f), new Vector2(22f, 22f));

        Txt(logo, "Subtitle", "JOURNEY'S END", UITextStyle.Label, UITheme.Gold,
            56f, 14f, new Vector2(0f, -94f), new Vector2(1100f, 80f));

        Image keyBottom = UIKit.NewImage("Greek Key Bottom (replace sprite)", logo, "key_tile", Image.Type.Tiled, keyColor, false);
        UIKit.Place(keyBottom.rectTransform, C, C, new Vector2(0f, -158f), new Vector2(576f, 32f));
    }

    void BuildPrompt(Transform root)
    {
        RectTransform box = UIKit.NewRect("Prompt", root);
        UIKit.Place(box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1400f, 160f));
        promptGroup = box.gameObject.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0f;

        Txt(box, "Press Any Key", "Press any key", UITextStyle.TitleCaps, UITheme.Cream,
            48f, 4f, new Vector2(0f, 38f), new Vector2(1400f, 70f));
        Txt(box, "Tap To Begin", "Tap to begin", UITextStyle.Subtitle, WithAlpha(UITheme.Cream, 0.85f),
            32f, 0f, new Vector2(0f, -32f), new Vector2(1400f, 50f));
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
        UIKit.Place(t.rectTransform, C, C, pos, box);
        return t;
    }

    static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
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