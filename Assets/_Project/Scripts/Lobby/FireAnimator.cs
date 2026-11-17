using UnityEngine;

/// <summary>
/// Real fire from the still prop_fire.png, no extra art needed. Put it on the object that has the
/// fire SpriteRenderer (Oddyseus > Add Lobby Interactables does this for you):
///  - the flame is cut into thin horizontal strips that sway, lean and stretch like licking tongues
///    (the logs stay still, the tip moves most); a second, slightly smaller warm copy dances out of
///    phase on top, so the flame looks soft and full instead of a pulsing picture
///  - embers float up, sway and fade out
///  - a warm glow flickers in the hearth and on the floor in front of it
///  - clicking the fireplace makes the fire flare up with a burst of embers
/// Every number below can be tuned in the Inspector while the game is playing.
/// </summary>
[DisallowMultipleComponent]
public class FireAnimator : MonoBehaviour
{
    [Header("Flame")]
    [Range(8, 48)] public int strips = 30;
    [Tooltip("How far the tip of the flame sways, as a fraction of the sprite width.")]
    public float sway = 0.09f;
    public float speed = 1f;
    [Tooltip("How much the flame grows and shrinks in height (0.1 = 10%).")]
    [Range(0f, 0.4f)] public float stretch = 0.12f;
    [Tooltip("Where the moving part starts: 0 = whole sprite moves, 0.4 = only the top 60% moves (logs stay still).")]
    [Range(0f, 0.8f)] public float logsHeight = 0.34f;
    [Range(0f, 1f)] public float ghostAlpha = 0.5f;
    public Color ghostTint = new Color(1f, 0.82f, 0.45f, 1f);

    [Header("Embers")]
    [Range(0, 40)] public int embers = 20;
    public Color emberHot = new Color(1f, 0.88f, 0.45f, 1f);
    public Color emberCool = new Color(1f, 0.30f, 0.08f, 1f);

    [Header("Light")]
    public bool glow = true;
    public Color glowColor = new Color(1f, 0.55f, 0.16f, 0.30f);
    public Color floorGlowColor = new Color(1f, 0.50f, 0.14f, 0.16f);

    class Strip
    {
        public Transform t;
        public SpriteRenderer r;
        public float yOff;      // distance of the strip bottom above the sprite bottom (local units)
        public float height;    // strip height (local units)
        public float v;         // 0 bottom .. 1 top (centre of the strip)
        public float weight;    // how much this strip moves
    }

    class Ember
    {
        public Transform t;
        public SpriteRenderer r;
        public float age, life, x0, speed, phase, size, drift;
    }

    SpriteRenderer src;
    PropHotspot hotspot;
    Strip[] baseStrips, ghostStrips;
    Ember[] pool;
    Transform fx;
    SpriteRenderer hearthGlow, floorGlow;
    Bounds wb;                   // visible bounds of the fire (world)
    float localW, localBottom;   // sprite width and bottom in local units
    float flare;                 // 1 after a click, fades to 0
    float seed;

    void Awake()
    {
        src = GetComponent<SpriteRenderer>();
        if (src == null || src.sprite == null)
        {
            Debug.LogWarning("FireAnimator needs a SpriteRenderer with the fire sprite on the same object.");
            enabled = false;
            return;
        }
        seed = Random.value * 10f;
        wb = LobbyFx.VisibleWorldBounds(src);
        BuildStrips();
        BuildEmbers();
        BuildGlow();
        src.enabled = false;   // the strips replace the single sprite
    }

    void OnEnable()
    {
        hotspot = GetComponentInParent<PropHotspot>();
        if (hotspot != null) hotspot.Clicked += OnClicked;
    }

    void OnDisable()
    {
        if (hotspot != null) hotspot.Clicked -= OnClicked;
    }

    void OnDestroy()
    {
        if (fx != null) Destroy(fx.gameObject);
    }

    void OnClicked()
    {
        flare = 1f;
        // burst: restart most embers with a faster launch
        if (pool == null) return;
        for (int i = 0; i < pool.Length; i += 2) Respawn(pool[i], true, 1.7f);
    }

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------
    void BuildStrips()
    {
        Sprite sp = src.sprite;
        Texture2D tex = sp.texture;
        Rect rect = sp.rect;
        float ppu = sp.pixelsPerUnit;
        int rows = Mathf.RoundToInt(rect.height);
        localW = rect.width / ppu;
        localBottom = -sp.pivot.y / ppu;
        float centreX = (rect.width * 0.5f - sp.pivot.x) / ppu;

        baseStrips = new Strip[strips];
        ghostStrips = new Strip[strips];
        for (int layer = 0; layer < 2; layer++)
        {
            Strip[] arr = layer == 0 ? baseStrips : ghostStrips;
            for (int i = 0; i < strips; i++)
            {
                int y0 = Mathf.RoundToInt((float)rows * i / strips);
                int y1 = Mathf.RoundToInt((float)rows * (i + 1) / strips);
                int overlap = (i < strips - 1) ? 1 : 0;   // one extra row so no hairline gaps show between strips
                Rect rr = new Rect(rect.x, rect.y + y0, rect.width, (y1 - y0) + overlap);
                Sprite s = Sprite.Create(tex, rr, new Vector2(0.5f, 0f), ppu, 0, SpriteMeshType.FullRect);
                s.name = "fire strip " + i;

                GameObject go = new GameObject((layer == 0 ? "Flame " : "Flame ghost ") + i);
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = s;
                sr.sharedMaterial = src.sharedMaterial;
                sr.sortingLayerID = src.sortingLayerID;
                sr.sortingOrder = src.sortingOrder + layer;
                if (layer == 1) sr.color = new Color(ghostTint.r, ghostTint.g, ghostTint.b, ghostAlpha);

                Strip st = new Strip();
                st.t = go.transform;
                st.r = sr;
                st.yOff = y0 / ppu;
                st.height = (y1 - y0) / ppu;
                st.v = (i + 0.5f) / strips;
                float w = Mathf.Clamp01(Mathf.InverseLerp(logsHeight, 1f, st.v));
                st.weight = Mathf.Pow(Mathf.SmoothStep(0f, 1f, w), 1.35f);
                arr[i] = st;
                go.transform.localPosition = new Vector3(centreX, localBottom + st.yOff, 0f);
            }
        }
    }

    void BuildEmbers()
    {
        fx = new GameObject("Fire FX").transform;
        Transform parent = transform.parent != null ? transform.parent : transform;
        fx.SetParent(parent, false);
        fx.position = Vector3.zero;

        pool = new Ember[embers];
        for (int i = 0; i < embers; i++)
        {
            GameObject go = new GameObject("Ember");
            go.transform.SetParent(fx, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LobbyFx.Soft;
            sr.sharedMaterial = LobbyFx.Mat;
            sr.sortingLayerID = src.sortingLayerID;
            sr.sortingOrder = src.sortingOrder + 3;
            Ember e = new Ember();
            e.t = go.transform;
            e.r = sr;
            pool[i] = e;
            Respawn(e, false, 1f);
            e.age = Random.value * e.life;   // spread them out at the start
        }
    }

    void BuildGlow()
    {
        if (!glow) return;
        hearthGlow = MakeGlow("Hearth Glow", src.sortingOrder - 1);
        floorGlow = MakeGlow("Floor Glow", src.sortingOrder - 2);
        float w = wb.size.x, h = wb.size.y;
        hearthGlow.transform.position = new Vector3(wb.center.x, wb.min.y + h * 0.42f, 0f);
        hearthGlow.transform.localScale = new Vector3(w * 2.1f, h * 1.7f, 1f);
        floorGlow.transform.position = new Vector3(wb.center.x, wb.min.y - h * 0.18f, 0f);
        floorGlow.transform.localScale = new Vector3(w * 4.2f, h * 0.9f, 1f);
    }

    SpriteRenderer MakeGlow(string n, int order)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(fx, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LobbyFx.Soft;
        sr.sharedMaterial = LobbyFx.Mat;
        sr.sortingLayerID = src.sortingLayerID;
        sr.sortingOrder = order;
        return sr;
    }

    void Respawn(Ember e, bool burst, float boost)
    {
        float w = wb.size.x, h = wb.size.y;
        e.age = 0f;
        e.life = Random.Range(1.2f, 2.6f) * (burst ? 1.15f : 1f);
        e.x0 = wb.center.x + Random.Range(-0.16f, 0.16f) * w;
        e.speed = h * Random.Range(0.32f, 0.62f) * boost;
        e.phase = Random.value * 6.28f;
        e.size = h * Random.Range(0.018f, 0.04f) * (burst ? 1.3f : 1f);
        e.drift = Random.Range(-0.05f, 0.08f) * w;
    }

    // ------------------------------------------------------------------
    // Animate
    // ------------------------------------------------------------------
    void Update()
    {
        float dt = Time.deltaTime;
        float t = Time.time * speed + seed;
        flare = Mathf.MoveTowards(flare, 0f, dt * 1.1f);

        float nSlow = Mathf.PerlinNoise(t * 0.9f, 3.7f) * 2f - 1f;     // lean of the whole flame
        float nFast = Mathf.PerlinNoise(t * 3.6f, 9.1f) * 2f - 1f;     // height flicker
        float heightF = 1f + stretch * (0.55f * nFast + 0.45f * nSlow) + 0.20f * flare;
        float swayNow = sway * (1f + 0.5f * flare);

        float bright = 0.90f + 0.10f * LobbyFx.Flicker(t, 1f);
        Color baseCol = new Color(bright, bright, bright, 1f);

        AnimateLayer(baseStrips, 0, t, nSlow, heightF, swayNow, 1f, baseCol);
        AnimateLayer(ghostStrips, 1, t, nSlow, heightF * 0.97f, swayNow * 1.25f, 0.90f,
                     new Color(ghostTint.r, ghostTint.g, ghostTint.b, ghostAlpha * (0.8f + 0.4f * LobbyFx.Flicker(t * 0.7f, 4f))));

        AnimateEmbers(dt);
        AnimateGlow(t);
    }

    void AnimateLayer(Strip[] arr, int layer, float t, float lean, float heightF, float swayNow, float scale, Color col)
    {
        if (arr == null) return;
        float phase = layer * 1.9f;
        float baseX = centreXOf();   // horizontal centre of the sprite; the sway is added on top
        for (int i = 0; i < arr.Length; i++)
        {
            Strip s = arr[i];
            float w = s.weight;
            float wave = Mathf.Sin(t * 5.2f + s.v * 6.5f + phase) * 0.55f
                       + Mathf.Sin(t * 9.7f - s.v * 13f + phase * 2.1f) * 0.30f
                       + (Mathf.PerlinNoise(t * 2.3f + phase, s.v * 3.1f) * 2f - 1f) * 0.55f;
            float dx = (lean * 0.55f + wave * 0.55f) * swayNow * localW * w;

            float grow = Mathf.Lerp(1f, heightF, w);
            float y = localBottom + s.yOff * scale * grow;
            s.t.localPosition = new Vector3(baseX * scale + dx, y, 0f);

            float sx = scale * (1f + 0.035f * Mathf.Sin(t * 6f + s.v * 9f + phase) * w);
            float sy = scale * grow * (1f + 1.2f / Mathf.Max(8f, s.height * src.sprite.pixelsPerUnit));
            s.t.localScale = new Vector3(sx, sy, 1f);

            Color c = col;
            if (layer == 1) c.a *= Mathf.Lerp(0.55f, 1f, w);   // the copy is mostly visible in the tongues
            s.r.color = c;
        }
    }

    float cachedCentre = float.NaN;
    float centreXOf()
    {
        if (float.IsNaN(cachedCentre))
        {
            Sprite sp = src.sprite;
            cachedCentre = (sp.rect.width * 0.5f - sp.pivot.x) / sp.pixelsPerUnit;
        }
        return cachedCentre;
    }

    void AnimateEmbers(float dt)
    {
        if (pool == null) return;
        float w = wb.size.x, h = wb.size.y;
        float yStart = wb.min.y + h * 0.28f;
        for (int i = 0; i < pool.Length; i++)
        {
            Ember e = pool[i];
            e.age += dt;
            if (e.age >= e.life) Respawn(e, false, 1f);
            float k = e.age / e.life;
            float x = e.x0 + Mathf.Sin(e.age * 2.3f + e.phase) * w * 0.06f + e.drift * e.age;
            float y = yStart + e.speed * e.age * (1f - 0.35f * k);
            e.t.position = new Vector3(x, y, 0f);
            float a = Mathf.Pow(Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI), 0.7f);
            Color c = Color.Lerp(emberHot, emberCool, k);
            c.a = a * 0.95f;
            e.r.color = c;
            float s = e.size * (1f - 0.55f * k);
            e.t.localScale = new Vector3(s, s, 1f);
        }
    }

    void AnimateGlow(float t)
    {
        if (!glow || hearthGlow == null) return;
        float f = 0.70f + 0.30f * LobbyFx.Flicker(t * 1.2f, 2f) + 0.35f * flare;
        Color a = glowColor; a.a = Mathf.Clamp01(glowColor.a * f);
        hearthGlow.color = a;
        Color b = floorGlowColor; b.a = Mathf.Clamp01(floorGlowColor.a * (0.8f + 0.5f * LobbyFx.Flicker(t * 0.8f, 6f)) * (1f + 0.6f * flare));
        floorGlow.color = b;
    }
}
