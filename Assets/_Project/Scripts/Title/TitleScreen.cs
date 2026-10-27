using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Title screen: sunset harbor background, a parchment scroll with the logo, "Click to start".
/// Only pressing the "Click to start" button starts the game (keys and clicks elsewhere do nothing).
///
/// ART SLOTS (put PNGs in Assets/_Project/Resources/UIKit/, import as Sprite 2D and UI):
///   title_bg.png      harbor / sunset background, 16:9, NO scroll and NO text
///   title_scroll.png  open parchment scroll with both rollers, TRIMMED (no empty margin), transparent
///   title_logo.png    "ODDYSEUS / JOURNEY'S END" logo, TRIMMED, transparent
///   title_button.png  "Click to start" plate, TRIMMED, transparent, no text
/// IMPORTANT: use the trimmed PNGs (transparent margins removed). Extra empty margin makes the plate tiny
/// and shifts the roller cut, which is what broke the layout before.
/// LIVING BACKGROUND: slow zoom/drift, warm clouds + mist, sparkles on the sea (mostly in the sun's
/// reflection), breathing sun glow, flickering lantern and fire bowl. All from code. "Animate Background" turns it off.
/// SOUND: Resources/Audio/title_ship.(ogg/wav/mp3), plays on boot. If missing, a placeholder is made from code.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    // Kept only because SceneBuilder still assigns it (legacy placeholder text). Not used any more.
    [HideInInspector] public Text prompt;

    [Tooltip("Tick = draw the logo in code even if title_logo.png exists.")]
    public bool forceCodeLogo = false;
    [Tooltip("The scroll fades in, opens from the middle, rollers slide apart, then the logo fades in.")]
    public bool playScrollAnimation = true;
    [Tooltip("Share of the TRIMMED scroll PNG width taken by ONE roller (caps included). 0.08 fits the supplied art. Raise to 0.09-0.10 if a cap is cut.")]
    [Range(0.03f, 0.2f)] public float rollerFraction = 0.08f;

    [Header("Living background (all from code, no extra art)")]
    public bool animateBackground = true;
    [Tooltip("Sea horizon relative to the screen center, in 1920x1080 pixels. title_bg: about -152.")]
    public float horizonOffsetY = -152f;
    [Tooltip("Sun position relative to the screen center. title_bg: about (392, -98).")]
    public Vector2 sunOffset = new Vector2(392f, -98f);
    [Tooltip("Lantern (top-left) and fire bowl (bottom-right) positions relative to the screen center.")]
    public Vector2 lanternOffset = new Vector2(-733f, 365f);
    public Vector2 fireOffset = new Vector2(906f, -150f);
    [Range(0f, 1.5f)] public float cloudStrength = 0.5f;
    [Range(0f, 1.5f)] public float glintStrength = 0.8f;
    [Range(0f, 1.5f)] public float sunGlowStrength = 0.6f;
    [Range(0f, 1.5f)] public float flickerStrength = 0.8f;

    [Header("Art slots (optional). Drag the PNG sprites here if Resources loading fails; they win over Resources.")]
    public Sprite bgSprite;
    public Sprite scrollSprite;
    public Sprite logoSprite;
    public Sprite buttonSprite;
    [Tooltip("Shows what was loaded for each art slot in the top-left corner for the first 10 seconds (stays if something is missing).")]
    public bool showDiagnostics = false;

    public string versionLabel = "v0.1";
    public string promptMain = "Click to start";
    public string promptSub = "Press the button to begin";

    const string BackgroundResource = "UIKit/title_bg";
    const string ScrollResource = "UIKit/title_scroll";
    const string LogoResource = "UIKit/title_logo";
    const string ButtonResource = "UIKit/title_button";
    const string ShipSoundResource = "Audio/title_ship";

    static readonly Vector2 C = new Vector2(0.5f, 0.5f);
    static readonly Color SubColor = new Color32(0x6B, 0x57, 0x42, 255);          // brown ink on the parchment
    static readonly Color PlateColor = new Color32(0xC9, 0x7A, 0x52, 255);        // terracotta (fallback when title_button.png is missing)
    static readonly Color PaperColor = new Color32(0xFB, 0xF3, 0xE0, 255);        // text on the plate
    static readonly Color Bronze2 = new Color32(0xB8, 0x86, 0x3B, 255);
    static readonly Vector2 ScrollMax = new Vector2(1400f, 870f);
    static bool quitting;

    // timeline (seconds)
    const float OpenStart = 0.4f, OpenTime = 1.5f;
    const float LogoStart = OpenStart + OpenTime, LogoTime = 0.7f;
    const float PromptStart = LogoStart + LogoTime, PromptTime = 0.4f;

    Canvas ui;
    CanvasGroup scrollGroup, logoGroup, mainGroup, subGroup;
    RectTransform scrollRt, logoRt, maskRt, leftRt, rightRt;
    float midW, rollerW, scrollH;
    float startTime;
    bool going, soundStarted;

    // living background
    RectTransform bgRt;
    RawImage[] cloudLayers;
    float[] cloudSpeed, cloudAlpha;
    Image[] glints;
    float[] glintPhase, glintRate, glintDrift;
    Vector2[] glintBase;
    Image sunGlow, sunCore, lanternGlow, fireGlow;
    readonly System.Collections.Generic.List<UnityEngine.Object> madeAssets = new System.Collections.Generic.List<Object>();
    readonly System.Collections.Generic.List<string> missingArt = new System.Collections.Generic.List<string>();
    readonly System.Collections.Generic.List<string> diag = new System.Collections.Generic.List<string>();
    CanvasGroup diagGroup;

    void Awake()
    {
        startTime = Time.time;

        // Hide the old placeholder UI that lives on this object.
        Canvas old = GetComponent<Canvas>();
        if (old != null) old.enabled = false;
        for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);

        BuildUI();
        if (!playScrollAnimation) startTime -= LogoStart;   // scroll already open, only the logo fades in
    }

    void Start()
    {
        PlayShipSound();
    }

    void Update()
    {
        float t = Time.time - startTime;

        // Scroll fades in + settles, then opens
        float appear = Mathf.Clamp01(t / 0.45f);
        if (scrollGroup != null) scrollGroup.alpha = appear;
        if (scrollRt != null)
        {
            float s = Mathf.Lerp(0.96f, 1f, Ease(appear));
            scrollRt.localScale = new Vector3(s, s, 1f);
        }
        ApplyOpen(Ease(Mathf.Clamp01((t - OpenStart) / OpenTime)));

        float logoA = Mathf.Clamp01((t - LogoStart) / LogoTime);
        float promptA = Mathf.Clamp01((t - PromptStart) / PromptTime);
        if (logoGroup != null) logoGroup.alpha = logoA;
        if (logoRt != null)
        {
            float s = Mathf.Lerp(0.94f, 1f, Ease(logoA));
            logoRt.localScale = new Vector3(s, s, 1f);
        }
        if (subGroup != null)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * Time.time / 2.4f);
            subGroup.alpha = promptA * Mathf.Lerp(0.55f, 1f, wave);   // the sub line breathes
        }
        if (mainGroup != null)
        {
            // The plate stays fully visible; it only breathes slightly (92%-100% alpha, tiny scale).
            float wave = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * Time.time / 2.4f);
            mainGroup.alpha = promptA * Mathf.Lerp(0.92f, 1f, wave);
            float sc = 1f + 0.012f * wave;
            mainGroup.transform.localScale = new Vector3(sc, sc, 1f);
        }

        if (diagGroup != null)
            diagGroup.alpha = missingArt.Count > 0 ? 1f : Mathf.Clamp01(1f - (Time.time - startTime - 10f) / 2f);

        if (animateBackground) AnimateBackground();

        // Only pressing the "Click to start" button starts the game (clicking elsewhere does nothing).
        // While the scroll is still opening, any click skips the intro to the end.
        bool overButton = false;
        if (mainGroup != null && t >= PromptStart)
        {
            RectTransform br = mainGroup.transform as RectTransform;
            overButton = br != null && RectTransformUtility.RectangleContainsScreenPoint(br, GameInput.PointerPosition, null);
        }
        if (mainGroup != null && overButton && !going)
            mainGroup.transform.localScale *= 1.05f;   // small hover pop

        if (!going && GameInput.PointerDown)
        {
            if (t < PromptStart) startTime = Time.time - PromptStart;
            else if (overButton)
            {
                going = true;
                SceneFader.LoadScene(GameScenes.Lobby, 0.8f);
            }
        }
    }

    static float Ease(float x)
    {
        return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;
    }

    // open = 0: closed scroll (rollers together). open = 1: fully open.
    void ApplyOpen(float open)
    {
        if (maskRt == null) return;
        float mw = Mathf.Max(6f, midW * open);
        maskRt.sizeDelta = new Vector2(mw, scrollH);
        float x = mw * 0.5f + rollerW * 0.5f;
        if (leftRt != null) leftRt.anchoredPosition = new Vector2(-x, 0f);
        if (rightRt != null) rightRt.anchoredPosition = new Vector2(x, 0f);
    }

    void OnApplicationQuit() { quitting = true; }

    void OnDestroy()
    {
        if (soundStarted && !quitting) AudioManager.Instance.StopMusic();
        for (int i = 0; i < madeAssets.Count; i++) if (madeAssets[i] != null) Destroy(madeAssets[i]);
    }

    // ------------------------------------------------------------------
    // Sound
    // ------------------------------------------------------------------
    void PlayShipSound()
    {
        AudioClip clip = Resources.Load<AudioClip>(ShipSoundResource);
        if (clip == null) clip = MakeSailingClip();
        AudioManager.Instance.PlayMusic(clip, true);
        soundStarted = true;
    }

    // Placeholder: sea swell + water hiss + a few wooden creaks.
    static AudioClip MakeSailingClip()
    {
        const int rate = 22050;
        const float seconds = 16f;
        int n = (int)(rate * seconds);
        float[] d = new float[n];
        System.Random rng = new System.Random(11);
        float lp1 = 0f, lp2 = 0f, peak = 0.0001f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp1 += (white - lp1) * 0.035f;
            lp2 += (lp1 - lp2) * 0.10f;
            float swellA = Mathf.Sin(2f * Mathf.PI * t * 2f / seconds);
            float swellB = Mathf.Sin(2f * Mathf.PI * t * 5f / seconds + 1.3f);
            float swell = 0.55f + 0.30f * swellA + 0.15f * swellB;
            float wave = lp2 * 6f * swell;
            float hiss = (white - lp1) * 0.05f * Mathf.Max(0f, swellB);
            float creak = Creak(t, 3.2f, 1.0f) + Creak(t, 9.5f, 0.8f) + Creak(t, 13.1f, 1.1f);
            float s = wave + hiss + creak * 0.12f;
            d[i] = s;
            peak = Mathf.Max(peak, Mathf.Abs(s));
        }

        float gain = 0.6f / peak;
        int fade = rate / 4;
        for (int i = 0; i < n; i++)
        {
            float v = d[i] * gain;
            if (i < fade) v *= i / (float)fade;
            if (i > n - fade) v *= (n - i) / (float)fade;
            d[i] = v;
        }

        AudioClip clip = AudioClip.Create("title_ship (placeholder)", n, 1, rate, false);
        clip.SetData(d, 0);
        return clip;
    }

    static float Creak(float t, float start, float length)
    {
        float u = t - start;
        if (u < 0f || u > length) return 0f;
        float env = Mathf.Sin(Mathf.PI * u / length);
        float tone = Mathf.Sin(2f * Mathf.PI * (170f * u - 25f * u * u));
        float rough = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 23f * u);
        return tone * env * rough;
    }

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------
    void BuildUI()
    {
        ui = UIKit.CreateOverlayCanvas("Title Canvas", 0);
        Transform root = ui.transform;

        BuildBackground(root);
        RectTransform scroll = BuildScroll(root);
        BuildLogo(scroll);
        BuildPrompts(scroll);

        // Version: bottom-left.
        TMP_Text ver = UIKit.CreateText(root, "Version", versionLabel, UITextStyle.Small,
                                        UITheme.Cream, TextAlignmentOptions.MidlineLeft);
        ver.fontSize = 16f;
        UIKit.Place(ver.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 30f), new Vector2(240f, 30f));

        if (showDiagnostics || missingArt.Count > 0)
        {
            TMP_Text tag = UIKit.CreateText(root, "Art Diagnostics",
                "ART  (Resources/UIKit)\n" + string.Join("\n", diag.ToArray()),
                UITextStyle.Small, new Color(1f, 0.95f, 0.8f, 0.95f), TextAlignmentOptions.TopLeft);
            tag.fontSize = 18f;
            UIKit.Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(1100f, 140f));
            tag.rectTransform.pivot = new Vector2(0f, 1f);
            tag.rectTransform.anchoredPosition = new Vector2(28f, -18f);
            diagGroup = tag.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void BuildBackground(Transform root)
    {
        Image bg = UIKit.NewImage("Background (replace sprite)", root, null, Image.Type.Simple, Color.white, false);
        Sprite art = LoadArt("title_bg", bgSprite, false);
        if (art != null)
        {
            bg.sprite = art;
            UIKit.Place(bg.rectTransform, C, C, Vector2.zero, UITheme.ReferenceResolution);
            AspectRatioFitter fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = art.rect.width / art.rect.height;
        }
        else
        {
            bg.sprite = NightSeaSprite();
            UIKit.Stretch(bg.rectTransform);
        }

        if (animateBackground) BuildLivingBackground(root, bg.rectTransform);
    }

    // ------------------------------------------------------------------
    // Living background. Sun glow, sea sparkles and the lantern / fire glows are CHILDREN of the background,
    // so they zoom and drift together with the picture and always stay on the right spot.
    // Order (back to front): background (+ its children), clouds, mist. The scroll is built after, so it stays on top.
    // ------------------------------------------------------------------
    void BuildLivingBackground(Transform root, RectTransform bg)
    {
        bgRt = bg;
        bgRt.pivot = C;

        Sprite dot = SoftDotSprite();
        Color warm = new Color(1f, 0.82f, 0.5f);

        // Sun glow: wide soft halo + a bright core.
        sunGlow = MakeGlow("Sun Glow", bg, dot, sunOffset, new Vector2(1000f, 700f), warm);
        sunCore = MakeGlow("Sun Core", bg, dot, sunOffset, new Vector2(260f, 260f), new Color(1f, 0.95f, 0.78f));

        // Lantern (top-left) and fire bowl (bottom-right)
        lanternGlow = MakeGlow("Lantern Glow", bg, dot, lanternOffset, new Vector2(340f, 340f), new Color(1f, 0.72f, 0.35f));
        fireGlow = MakeGlow("Fire Glow", bg, dot, fireOffset, new Vector2(380f, 380f), new Color(1f, 0.6f, 0.25f));

        // Clouds + mist: tileable soft-noise texture scrolled with uvRect (loops forever without seams). Warm tint.
        //                    y above center, height, speed (screen widths / second), alpha, texture seed
        float[,] cfg = {
            {  330f, 280f, 0.0040f, 0.20f, 3f },   // high thin clouds
            {  150f, 240f, 0.0075f, 0.17f, 7f },   // lower clouds
            { horizonOffsetY + 45f, 170f, -0.0055f, 0.16f, 11f }   // mist on the horizon, drifts the other way
        };
        int n = cfg.GetLength(0);
        cloudLayers = new RawImage[n];
        cloudSpeed = new float[n];
        cloudAlpha = new float[n];
        for (int i = 0; i < n; i++)
        {
            Texture2D tex = MakeCloudTexture((int)cfg[i, 4]);
            madeAssets.Add(tex);
            RectTransform rt = UIKit.NewRect("Clouds " + i, root);
            UIKit.Place(rt, C, C, new Vector2(0f, cfg[i, 0]), new Vector2(2800f, cfg[i, 1]));
            RawImage raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.raycastTarget = false;
            raw.uvRect = new Rect(0f, 0f, 2f, 1f);
            raw.color = new Color(1f, 0.88f, 0.72f, 0f);
            cloudLayers[i] = raw;
            cloudSpeed[i] = cfg[i, 2];
            cloudAlpha[i] = cfg[i, 3];
        }

        // Sparkles on the sea. 60% in the sun's reflection column (any depth), the rest on open water near the horizon.
        // (The stone ledges at the bottom corners are avoided.)
        const int count = 44;
        glints = new Image[count];
        glintPhase = new float[count];
        glintRate = new float[count];
        glintDrift = new float[count];
        glintBase = new Vector2[count];
        System.Random rng = new System.Random(5);
        for (int i = 0; i < count; i++)
        {
            bool column = i < count * 6 / 10;
            float depth, x;
            if (column)
            {
                depth = (float)rng.NextDouble();
                float spread = Mathf.Lerp(30f, 105f, depth);
                x = sunOffset.x + ((float)rng.NextDouble() * 2f - 1f) * spread;
            }
            else
            {
                depth = (float)rng.NextDouble() * 0.3f;
                x = Mathf.Lerp(-600f, 700f, (float)rng.NextDouble());
            }
            float y = horizonOffsetY - 14f - depth * 360f;
            float w = Mathf.Lerp(18f, 80f, depth) * (0.7f + 0.6f * (float)rng.NextDouble());
            Image g = UIKit.NewImage("Glint " + i, bg, null, Image.Type.Simple, WithAlpha(new Color(1f, 0.9f, 0.65f), 0f), false);
            g.sprite = dot;
            g.raycastTarget = false;
            UIKit.Place(g.rectTransform, C, C, new Vector2(x, y), new Vector2(w, Mathf.Lerp(3f, 8f, depth)));
            glints[i] = g;
            glintBase[i] = new Vector2(x, y);
            glintPhase[i] = (float)rng.NextDouble() * 6.2832f;
            glintRate[i] = 0.5f + 1.1f * (float)rng.NextDouble();
            glintDrift[i] = (5f + 14f * depth) * ((float)rng.NextDouble() > 0.5f ? 1f : -1f);
        }
    }

    Image MakeGlow(string name, Transform parent, Sprite dot, Vector2 pos, Vector2 size, Color tint)
    {
        Image g = UIKit.NewImage(name, parent, null, Image.Type.Simple, WithAlpha(tint, 0f), false);
        g.sprite = dot;
        g.raycastTarget = false;
        UIKit.Place(g.rectTransform, C, C, pos, size);
        return g;
    }

    void AnimateBackground()
    {
        float t = Time.time;
        float fade = Mathf.Clamp01(t / 2f);   // everything fades in over the first 2 s

        // Zoomed in ~3.5% at its smallest so the drift never shows an edge.
        if (bgRt != null)
        {
            float s = 1.06f + 0.025f * Mathf.Sin(t * 0.12f);
            bgRt.localScale = new Vector3(s, s, 1f);
            bgRt.anchoredPosition = new Vector2(Mathf.Sin(t * 0.07f) * 18f, Mathf.Sin(t * 0.05f + 1f) * 8f);
        }

        if (sunGlow != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 0.5f);
            sunGlow.color = WithAlpha(sunGlow.color, (0.22f + 0.10f * pulse) * sunGlowStrength * fade);
            sunCore.color = WithAlpha(sunCore.color, (0.35f + 0.15f * pulse) * sunGlowStrength * fade);
        }

        // Candle-like flicker: layered noise, so it is irregular but never harsh.
        if (lanternGlow != null)
        {
            float f = Mathf.PerlinNoise(t * 3.1f, 0.3f) * 0.6f + Mathf.PerlinNoise(t * 7.3f, 4.2f) * 0.4f;
            lanternGlow.color = WithAlpha(lanternGlow.color, (0.18f + 0.30f * f) * flickerStrength * fade);
            float g = Mathf.PerlinNoise(t * 4.1f, 9.1f) * 0.6f + Mathf.PerlinNoise(t * 9.7f, 1.7f) * 0.4f;
            fireGlow.color = WithAlpha(fireGlow.color, (0.20f + 0.34f * g) * flickerStrength * fade);
            float fs = 0.94f + 0.12f * g;
            fireGlow.rectTransform.localScale = new Vector3(fs, fs, 1f);
        }

        if (cloudLayers != null)
        {
            for (int i = 0; i < cloudLayers.Length; i++)
            {
                Rect r = cloudLayers[i].uvRect;
                r.x = Mathf.Repeat(t * cloudSpeed[i], 1f);
                cloudLayers[i].uvRect = r;
                Color c = cloudLayers[i].color;
                c.a = cloudAlpha[i] * cloudStrength * fade;
                cloudLayers[i].color = c;
            }
        }

        if (glints != null)
        {
            for (int i = 0; i < glints.Length; i++)
            {
                float tw = Mathf.Sin(t * glintRate[i] + glintPhase[i]);
                float a = Mathf.Pow(Mathf.Max(0f, tw), 3f) * 0.75f * glintStrength * fade;
                Color c = glints[i].color;
                c.a = a;
                glints[i].color = c;
                glints[i].rectTransform.anchoredPosition = glintBase[i] + new Vector2(Mathf.Sin(t * 0.2f + glintPhase[i]) * glintDrift[i], 0f);
            }
        }
    }

    // Soft round blob (alpha falls off smoothly to the edge). Tinted/stretched by the Image that uses it.
    Sprite SoftDotSprite()
    {
        const int n = 64;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        }
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(px);
        tex.Apply(false, false);
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        madeAssets.Add(tex);
        madeAssets.Add(sp);
        return sp;
    }

    // Soft cloud texture that tiles left-right (periodic value noise, 4 octaves), faded at the top and bottom.
    static Texture2D MakeCloudTexture(int seed)
    {
        const int w = 256, h = 128;
        float[] acc = new float[w * h];
        System.Random rng = new System.Random(seed);
        float amp = 1f, total = 0f;
        for (int oct = 0; oct < 4; oct++)
        {
            int gx = 4 << oct, gy = 2 << oct;
            float[] g = new float[gx * gy];
            for (int i = 0; i < g.Length; i++) g[i] = (float)rng.NextDouble();
            for (int y = 0; y < h; y++)
            {
                float fy = (float)y / h * gy;
                int y0 = (int)fy;
                float ty = fy - y0;
                ty = ty * ty * (3f - 2f * ty);
                int y1 = (y0 + 1) % gy;
                for (int x = 0; x < w; x++)
                {
                    float fx = (float)x / w * gx;
                    int x0 = (int)fx;
                    float tx = fx - x0;
                    tx = tx * tx * (3f - 2f * tx);
                    int x1 = (x0 + 1) % gx;
                    float top = Mathf.Lerp(g[y0 * gx + x0], g[y0 * gx + x1], tx);
                    float bot = Mathf.Lerp(g[y1 * gx + x0], g[y1 * gx + x1], tx);
                    acc[y * w + x] += Mathf.Lerp(top, bot, ty) * amp;
                }
            }
            total += amp;
            amp *= 0.5f;
        }

        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float edge = Mathf.Sin(Mathf.PI * (y + 0.5f) / h);
            for (int x = 0; x < w; x++)
            {
                float v = acc[y * w + x] / total;
                float a = Mathf.SmoothStep(0.42f, 0.78f, v) * edge;
                px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
        }
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(px);
        tex.Apply(false, false);
        return tex;
    }

    // ---- art loading: Inspector slot > biggest matching Sprite in Resources/UIKit > Texture2D (made into a sprite) ----
    // Several assets can share one name (for example a tiny code-made placeholder next to the real PNG).
    // We collect all of them and take the biggest, and we refuse anything smaller than 64 px wide.
    Sprite LoadArt(string file, Sprite inspectorSprite, bool trim = true)
    {
        Sprite sp = null;
        string info;
        int found = 0;
        if (inspectorSprite != null) { sp = inspectorSprite; info = "Inspector slot"; }
        else
        {
            info = "Sprite";
            Sprite[] all = Resources.LoadAll<Sprite>("UIKit");
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null || all[i].name != file) continue;
                    found++;
                    if (sp == null || all[i].rect.width * all[i].rect.height > sp.rect.width * sp.rect.height) sp = all[i];
                }
            }
            if (sp == null) sp = Resources.Load<Sprite>("UIKit/" + file);
            if (sp != null && sp.rect.width < 64f)
            {
                Debug.LogWarning("[TitleScreen] '" + file + "' in Resources/UIKit is only " + sp.rect.width + "x" + sp.rect.height +
                                 " px (a placeholder?). Search the Project for '" + file + "' and delete the tiny one, or drag the real PNG into the Inspector slot.");
                sp = null;
                info = "tiny placeholder ignored";
            }
            if (sp == null)
            {
                Texture2D tex = Resources.Load<Texture2D>("UIKit/" + file);
                if (tex != null && tex.width >= 64)
                {
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    madeAssets.Add(sp);
                    info = "Texture only (set Texture Type = Sprite (2D and UI))";
                }
            }
            if (found > 1) info += " [" + found + " assets share this name]";
        }
        if (sp == null) { info += " -> NOT USABLE"; missingArt.Add(file); }
        else
        {
            if (trim) sp = TrimTransparent(sp, ref info);
            info += "  " + Mathf.RoundToInt(sp.rect.width) + "x" + Mathf.RoundToInt(sp.rect.height);
        }
        diag.Add(file + ": " + info);
        Debug.Log("[TitleScreen] " + file + ": " + info);
        return sp;
    }

    // Cuts away empty transparent margins, so extra padding in the PNG can never shrink the art.
    // Needs a readable texture (the importer script TitleArtImporter sets that). If it is not readable the sprite is used as it is.
    Sprite TrimTransparent(Sprite sp, ref string info)
    {
        try
        {
            Texture2D tex = sp.texture;
            if (tex == null || !tex.isReadable) { info += " (not readable: no auto-trim)"; return sp; }
            Rect r = sp.rect;
            int rx = Mathf.RoundToInt(r.x), ry = Mathf.RoundToInt(r.y), rw = Mathf.RoundToInt(r.width), rh = Mathf.RoundToInt(r.height);
            Color32[] px = tex.GetPixels32();
            int tw = tex.width;
            int minX = rw, minY = rh, maxX = -1, maxY = -1;
            for (int y = 0; y < rh; y++)
            {
                int row = (ry + y) * tw + rx;
                for (int x = 0; x < rw; x++)
                {
                    if (px[row + x].a > 20)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            if (maxX < 0) return sp;
            int bw = maxX - minX + 1, bh = maxY - minY + 1;
            if (bw >= rw - 2 && bh >= rh - 2) return sp;   // nothing to trim
            Sprite t = Sprite.Create(tex, new Rect(rx + minX, ry + minY, bw, bh), new Vector2(0.5f, 0.5f), sp.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            madeAssets.Add(t);
            info += " (auto-trimmed from " + rw + "x" + rh + ")";
            return t;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[TitleScreen] auto-trim skipped: " + e.Message);
            return sp;
        }
    }

    // Plain Image (no UIKit involved, so nothing can hide it).
    static Image Pic(string name, Transform parent, Sprite sp, Vector2 size, Vector2 pos, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = C;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        Image im = go.GetComponent<Image>();
        im.sprite = sp;
        im.type = Image.Type.Simple;
        im.color = color;
        im.raycastTarget = false;
        return im;
    }

    static void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // The scroll: up to 1300x820, centered. Three pieces: left roller, parchment (inside a mask that grows), right roller.
    RectTransform BuildScroll(Transform root)
    {
        RectTransform group = UIKit.NewRect("Scroll", root);
        UIKit.Place(group, C, C, Vector2.zero, ScrollMax);
        scrollRt = group;
        scrollGroup = group.gameObject.AddComponent<CanvasGroup>();
        scrollGroup.alpha = 0f;

        Sprite art = LoadArt("title_scroll", scrollSprite);
        if (art != null) BuildScrollFromArt(group, art);
        else BuildScrollPlaceholder(group);
        ApplyOpen(0f);
        return group;
    }

    // Shows the one scroll PNG as three clipped windows: left roller / middle / right roller.
    // Each window is a RectMask2D showing a part of the SAME full sprite. The middle window grows from the center = the scroll opens.
    void BuildScrollFromArt(RectTransform group, Sprite art)
    {
        float aspect = art.rect.width / art.rect.height;
        float w = ScrollMax.x, h = w / aspect;
        if (h > ScrollMax.y) { h = ScrollMax.y; w = h * aspect; }
        scrollH = h;
        rollerW = w * rollerFraction;
        midW = w - 2f * rollerW;

        maskRt = ScrollWindow(group, "Parchment Mask", art, midW, w, h, 0f);
        leftRt = ScrollWindow(group, "Roller Left", art, rollerW, w, h, (w - rollerW) * 0.5f);
        rightRt = ScrollWindow(group, "Roller Right", art, rollerW, w, h, -(w - rollerW) * 0.5f);
    }

    // A clipped window of width winW. imageShiftX moves the full image inside it (0 = centered on the window).
    static RectTransform ScrollWindow(RectTransform parent, string name, Sprite art, float winW, float fullW, float fullH, float imageShiftX)
    {
        RectTransform win = UIKit.NewRect(name, parent);
        UIKit.Place(win, C, C, Vector2.zero, new Vector2(winW, fullH));
        win.gameObject.AddComponent<RectMask2D>();
        Pic("Art", win, art, new Vector2(fullW, fullH), new Vector2(imageShiftX, 0f), Color.white);
        return win;
    }

    // Placeholder scroll, plain colours only (always visible, even when no art loads).
    void BuildScrollPlaceholder(RectTransform group)
    {
        scrollH = 700f;
        rollerW = 70f;
        midW = 1250f;

        maskRt = UIKit.NewRect("Parchment Mask", group);
        UIKit.Place(maskRt, C, C, Vector2.zero, new Vector2(midW, scrollH));
        maskRt.gameObject.AddComponent<RectMask2D>();
        Pic("Paper", maskRt, null, new Vector2(midW, scrollH * 0.92f), Vector2.zero, new Color32(0xE9, 0xD6, 0xAE, 255));
        Color band = new Color32(0x6B, 0x7A, 0x4A, 255);
        Pic("Band Top", maskRt, null, new Vector2(midW, 16f), new Vector2(0f, scrollH * 0.36f), band);
        Pic("Band Bottom", maskRt, null, new Vector2(midW, 16f), new Vector2(0f, -scrollH * 0.36f), band);

        Color rod = new Color32(0xA8, 0x7A, 0x38, 255);
        leftRt = Pic("Roller Left", group, null, new Vector2(rollerW, scrollH), Vector2.zero, rod).rectTransform;
        rightRt = Pic("Roller Right", group, null, new Vector2(rollerW, scrollH), Vector2.zero, rod).rectTransform;
    }

    // Layout is proportional to the scroll height h (matches the taller mockup).
    // Logo: height 0.285h, center +0.15h.  Plate: width 0.46h, center -0.235h.  Sub line: -0.30h.
    // The clear parchment area (between the two Greek key bands) is about +-0.35h.
    void BuildLogo(RectTransform scroll)
    {
        float lh = scrollH * 0.285f;
        float ly = scrollH * 0.15f;

        logoRt = UIKit.NewRect("Logo", scroll);
        UIKit.Place(logoRt, C, C, new Vector2(0f, ly), new Vector2(lh * 3.45f, lh));
        RectTransform logo = logoRt;
        logoGroup = logo.gameObject.AddComponent<CanvasGroup>();
        logoGroup.alpha = 0f;

        Sprite art = forceCodeLogo ? null : LoadArt("title_logo", logoSprite);
        if (art != null)
        {
            Image img = Pic("Logo", logo, art, logo.sizeDelta, Vector2.zero, Color.white);
            img.preserveAspect = true;
            Fill(img.rectTransform);
            return;
        }

        // Code-drawn fallback (design size 940x270)
        UIKit.Place(logo, C, C, new Vector2(0f, ly), new Vector2(940f, 270f));
        Txt(logo, "Title Shadow", "ODDYSEUS", UITextStyle.TitleCaps, WithAlpha(Color.black, 0.25f),
            140f, 6f, new Vector2(0f, 38f), new Vector2(1072f, 170f));
        TMP_Text title = Txt(logo, "Title", "ODDYSEUS", UITextStyle.TitleCaps, Color.white,
                             140f, 6f, new Vector2(0f, 44f), new Vector2(1072f, 170f));
        title.fontStyle = FontStyles.UpperCase | FontStyles.Bold;
        title.enableVertexGradient = true;
        Color top = UITheme.Hex("E0AC4A");
        Color bottom = UITheme.Hex("9A6A1C");
        title.colorGradient = new VertexGradient(top, top, bottom, bottom);
        title.outlineWidth = 0.14f;
        title.outlineColor = new Color32(0x5A, 0x3A, 0x12, 255);

        Image rule = Pic("Divider", logo, null, new Vector2(520f, 2f), new Vector2(0f, -42f), WithAlpha(Bronze2, 0.8f));
        rule.raycastTarget = false;

        Txt(logo, "Subtitle", "JOURNEY'S END", UITextStyle.Label, UITheme.Hex("7A5520"),
            46f, 14f, new Vector2(0f, -92f), new Vector2(1072f, 70f));
    }

    // "Click to start" plate under the logo, the sub line under it.
    void BuildPrompts(RectTransform scroll)
    {
        RectTransform btn = UIKit.NewRect("Click To Start", scroll);
        UIKit.Place(btn, C, C, new Vector2(0f, -scrollH * 0.235f), new Vector2(scrollH * 0.46f, scrollH * 0.1f));
        mainGroup = btn.gameObject.AddComponent<CanvasGroup>();
        mainGroup.alpha = 0f;

        Sprite plateArt = LoadArt("title_button", buttonSprite);
        Image plate = Pic("Plate", btn, plateArt, btn.sizeDelta, Vector2.zero, plateArt != null ? Color.white : PlateColor);
        plate.preserveAspect = plateArt != null;
        Fill(plate.rectTransform);

        Txt(btn, "Label", promptMain, UITextStyle.Subtitle, PaperColor,
            scrollH * 0.04f, 1f, Vector2.zero, new Vector2(scrollH * 0.4f, scrollH * 0.08f));

        TMP_Text sub = Txt(scroll, "Click Anywhere", promptSub, UITextStyle.Small, SubColor,
                           scrollH * 0.028f, 0f, new Vector2(0f, -scrollH * 0.30f), new Vector2(900f, 40f));
        subGroup = sub.gameObject.AddComponent<CanvasGroup>();
        subGroup.alpha = 0f;
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

    // Placeholder background (only used when title_bg.png is missing): dusk sky, warm glow, dark sea.
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
                float across = Mathf.Exp(-Mathf.Pow((u - 0.85f) / 0.3f, 2f));
                Color c = Color.Lerp(baseColor, Bronze2, band * across * 0.6f);
                c.a = 1f;
                px[y * w + x] = c;
            }
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(px);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }
}