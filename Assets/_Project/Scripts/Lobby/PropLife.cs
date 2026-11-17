using System.Collections.Generic;
using UnityEngine;

public enum PropLifeKind
{
    Breathe,   // sleeping dog: slow breathing, floating Zzz, wakes up happy (hearts) when clicked
    Rock,      // toy horse: rocks on its wheels with a springy wobble when clicked
    Swing      // hanging lantern: sways like a pendulum, light flickers, swings when clicked
}

/// <summary>
/// Gives a lobby prop real motion so it stops looking like a sticker on the painting.
/// All motion is code (springs, breathing, particles), no animation clips needed.
/// Oddyseus > Add Lobby Interactables puts it on the dog, the horse and the lantern.
///   Breathe / Rock: put it on the SPRITE object (it moves around the bottom of the visible sprite).
///   Swing:          put it on the pivot object at the top of the lantern (it rotates around its own origin).
/// Every number can be tuned in the Inspector while the game is playing.
/// </summary>
public class PropLife : MonoBehaviour
{
    public PropLifeKind kind = PropLifeKind.Breathe;

    [Header("Look")]
    [Tooltip("Soft dark ellipse under the object so it sits on the floor instead of floating.")]
    public bool contactShadow = true;
    public Color shadowColor = new Color(0.10f, 0.05f, 0.02f, 0.45f);

    [Header("Breathe (dog)")]
    public float breathSpeed = 1.5f;
    [Range(0f, 0.08f)] public float breathAmount = 0.024f;
    public bool sleepyZ = true;
    public float zInterval = 2.6f;

    [Header("Rock (horse)")]
    public float rockKick = 240f;        // degrees per second given by a click
    public float rockStiffness = 170f;
    public float rockDamping = 4.2f;
    public float rollDistance = 0.0035f; // sideways roll per degree (world units)

    [Header("Swing (lantern)")]
    public float swingStiffness = 26f;
    public float swingDamping = 1.5f;
    public float idleAngle = 2.4f;       // degrees of calm sway
    public float swingKick = 75f;
    public bool lightGlow = true;
    public Color lightColor = new Color(1f, 0.66f, 0.26f, 0.30f);

    class Particle
    {
        public Transform t;
        public SpriteRenderer r;
        public float age, life, rise, drift, phase, size;
        public bool heart;
        public Color color;
    }

    SpriteRenderer src;
    PropHotspot hotspot;
    Bounds wb;                       // visible bounds of the sprite (world)
    Vector3 basePos, baseScale;
    Vector3 pivotLocal;              // bottom centre of the sprite, in the parent's space
    float seed;

    // dog
    float happy;                     // 1 after a click, fades to 0
    float zTimer;
    readonly List<Particle> particles = new List<Particle>();

    // horse / lantern spring
    float angle, angVel;
    SpriteRenderer lightSr;

    void Awake()
    {
        src = GetComponent<SpriteRenderer>();
        if (src == null) src = GetComponentInChildren<SpriteRenderer>();
        if (src == null || src.sprite == null)
        {
            Debug.LogWarning("PropLife needs a SpriteRenderer (on this object or a child).");
            enabled = false;
            return;
        }
        seed = Random.value * 20f;
        basePos = transform.localPosition;
        baseScale = transform.localScale;
        wb = LobbyFx.VisibleWorldBounds(src);

        Transform parent = transform.parent;
        Vector3 bottomWorld = new Vector3(wb.center.x, wb.min.y, 0f);
        pivotLocal = parent != null ? parent.InverseTransformPoint(bottomWorld) : bottomWorld;
        pivotLocal.z = basePos.z;

        if (kind != PropLifeKind.Swing && contactShadow) BuildShadow();
        if (kind == PropLifeKind.Swing && lightGlow) BuildLight();
        zTimer = Random.Range(0.5f, 1.5f);
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
        for (int i = 0; i < particles.Count; i++)
            if (particles[i].t != null) Destroy(particles[i].t.gameObject);
        particles.Clear();
    }

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------
    void BuildShadow()
    {
        GameObject go = new GameObject("Contact Shadow");
        Transform parent = transform.parent != null ? transform.parent : transform;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(wb.center.x, wb.min.y + wb.size.y * 0.02f, 0f);
        go.transform.localScale = new Vector3(wb.size.x * 1.1f, Mathf.Max(0.12f, wb.size.y * 0.2f), 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LobbyFx.Soft;
        sr.sharedMaterial = LobbyFx.Mat;
        sr.color = shadowColor;
        sr.sortingLayerID = src.sortingLayerID;
        sr.sortingOrder = src.sortingOrder - 1;
    }

    void BuildLight()
    {
        GameObject go = new GameObject("Lantern Light");
        go.transform.SetParent(transform, false);
        Vector3 bulb = new Vector3(wb.center.x, wb.max.y - wb.size.y * 0.58f, 0f);
        go.transform.position = bulb;
        float s = wb.size.y * 1.35f;
        go.transform.localScale = new Vector3(s, s, 1f);
        lightSr = go.AddComponent<SpriteRenderer>();
        lightSr.sprite = LobbyFx.Soft;
        lightSr.sharedMaterial = LobbyFx.Mat;
        lightSr.sortingLayerID = src.sortingLayerID;
        lightSr.sortingOrder = src.sortingOrder + 1;
    }

    // ------------------------------------------------------------------
    // Click
    // ------------------------------------------------------------------
    void OnClicked()
    {
        switch (kind)
        {
            case PropLifeKind.Breathe:
                happy = 1f;
                for (int i = 0; i < 5; i++) SpawnHeart(i * 0.12f);
                break;
            case PropLifeKind.Rock:
                angVel += (Random.value > 0.5f ? 1f : -1f) * rockKick;
                break;
            case PropLifeKind.Swing:
                angVel += (Random.value > 0.5f ? 1f : -1f) * swingKick;
                break;
        }
    }

    // ------------------------------------------------------------------
    // Update
    // ------------------------------------------------------------------
    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        float t = Time.time;
        switch (kind)
        {
            case PropLifeKind.Breathe: UpdateBreathe(dt, t); break;
            case PropLifeKind.Rock: UpdateRock(dt); break;
            case PropLifeKind.Swing: UpdateSwing(dt, t); break;
        }
        UpdateParticles(dt);
    }

    // Scale / rotate the sprite about the bottom centre of its visible part.
    void ApplyAbout(Vector3 scale, float degrees, Vector2 extraOffset)
    {
        Vector3 rel = Vector3.Scale(basePos - pivotLocal, scale);
        rel = Quaternion.Euler(0f, 0f, degrees) * rel;
        transform.localPosition = pivotLocal + rel + new Vector3(extraOffset.x, extraOffset.y, 0f);
        transform.localRotation = Quaternion.Euler(0f, 0f, degrees);
        transform.localScale = Vector3.Scale(baseScale, scale);
    }

    void UpdateBreathe(float dt, float t)
    {
        happy = Mathf.MoveTowards(happy, 0f, dt * 0.55f);
        float fast = 1f + 1.8f * happy;                       // breathes faster while happy
        float breath = 0.5f + 0.5f * Mathf.Sin(t * breathSpeed * fast + seed);
        float sy = 1f + breathAmount * breath;
        float sx = 1f - breathAmount * 0.35f * breath;

        // happy wiggle: little hops and a wag
        float hop = happy > 0.02f ? Mathf.Abs(Mathf.Sin(t * 11f)) * 0.07f * Mathf.Min(1f, happy * 2f) : 0f;
        float wag = happy > 0.02f ? Mathf.Sin(t * 16f) * 2.2f * happy : 0f;
        ApplyAbout(new Vector3(sx, sy + 0.05f * happy, 1f), wag, new Vector2(0f, hop));

        if (sleepyZ && happy < 0.05f)
        {
            zTimer -= dt;
            if (zTimer <= 0f)
            {
                zTimer = zInterval * Random.Range(0.8f, 1.25f);
                SpawnZ();
            }
        }
    }

    void UpdateRock(float dt)
    {
        float acc = -rockStiffness * angle - rockDamping * angVel;
        angVel += acc * dt;
        angle += angVel * dt;
        float a = Mathf.Clamp(angle, -25f, 25f);
        ApplyAbout(Vector3.one, a, new Vector2(a * rollDistance, 0f));
    }

    void UpdateSwing(float dt, float t)
    {
        float target = idleAngle * (0.7f * Mathf.Sin(t * 0.9f + seed) + 0.3f * Mathf.Sin(t * 1.9f + seed * 2f));
        float acc = -swingStiffness * (angle - target) - swingDamping * angVel;
        angVel += acc * dt;
        angle += angVel * dt;
        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(angle, -40f, 40f));

        if (lightSr != null)
        {
            float f = 0.65f + 0.35f * LobbyFx.Flicker(t * 0.9f, 3f);
            Color c = lightColor;
            c.a = Mathf.Clamp01(lightColor.a * f);
            lightSr.color = c;
        }
    }

    // ------------------------------------------------------------------
    // Particles (Zzz and hearts)
    // ------------------------------------------------------------------
    void SpawnZ()
    {
        Particle p = NewParticle(LobbyFx.Zee, false);
        p.life = Random.Range(2.4f, 3.2f);
        p.rise = wb.size.y * Random.Range(0.8f, 1.2f);
        p.drift = wb.size.x * Random.Range(0.08f, 0.16f);
        p.size = wb.size.y * Random.Range(0.28f, 0.42f);
        p.color = Color.white;
        // the dog's head is on the left, near the top
        p.t.position = new Vector3(wb.min.x + wb.size.x * 0.22f, wb.max.y - wb.size.y * 0.18f, 0f);
    }

    void SpawnHeart(float delay)
    {
        Particle p = NewParticle(LobbyFx.Heart, true);
        p.age = -delay;
        p.life = Random.Range(1.3f, 1.9f);
        p.rise = wb.size.y * Random.Range(1.0f, 1.6f);
        p.drift = wb.size.x * Random.Range(-0.18f, 0.18f);
        p.size = wb.size.y * Random.Range(0.18f, 0.30f);
        p.color = Color.white;
        p.t.position = new Vector3(wb.center.x + wb.size.x * Random.Range(-0.2f, 0.2f), wb.max.y - wb.size.y * 0.2f, 0f);
    }

    Particle NewParticle(Sprite sprite, bool heart)
    {
        GameObject go = new GameObject(heart ? "Heart" : "Z");
        Transform parent = transform.parent != null ? transform.parent : transform;
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = LobbyFx.Mat;
        sr.sortingLayerID = src.sortingLayerID;
        sr.sortingOrder = src.sortingOrder + 4;
        Particle p = new Particle();
        p.t = go.transform;
        p.r = sr;
        p.heart = heart;
        p.phase = Random.value * 6.28f;
        sr.color = new Color(1f, 1f, 1f, 0f);
        particles.Add(p);
        return p;
    }

    readonly Dictionary<Particle, Vector3> starts = new Dictionary<Particle, Vector3>();

    void UpdateParticles(float dt)
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            Particle p = particles[i];
            if (!starts.ContainsKey(p)) starts[p] = p.t.position;
            p.age += dt;
            if (p.age < 0f) { p.r.color = new Color(1f, 1f, 1f, 0f); continue; }
            float k = p.age / p.life;
            if (k >= 1f)
            {
                Destroy(p.t.gameObject);
                starts.Remove(p);
                particles.RemoveAt(i);
                continue;
            }
            Vector3 s0 = starts[p];
            float x = s0.x + p.drift * k + Mathf.Sin(p.age * 2.2f + p.phase) * wb.size.x * 0.04f;
            float y = s0.y + p.rise * (1f - (1f - k) * (1f - k));   // fast at first, slows down
            p.t.position = new Vector3(x, y, 0f);

            float fade = Mathf.Clamp01(k * 6f) * Mathf.Clamp01((1f - k) * 2.2f);
            float scale = p.size * (p.heart ? Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(k * 4f)) : Mathf.Lerp(0.6f, 1.15f, k));
            p.t.localScale = new Vector3(scale, scale, 1f);
            p.t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p.age * 3f + p.phase) * 12f);
            p.r.color = new Color(1f, 1f, 1f, fade);
        }
    }
}
