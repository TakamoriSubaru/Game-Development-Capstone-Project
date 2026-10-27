using System.Collections;
using UnityEngine;

/// <summary>
/// A small clickable thing in the Lobby (fireplace, dog, bookshelf...). Put it on a GameObject with a
/// Collider2D. It works even when the object is only painted into the background:
///   - hover: a soft gold glow appears over the collider and the ribbon shows the label
///   - click: the glow flashes, an optional sound plays, and Polites (or another speaker) says a line
///   - optional: wiggle / bounce / pulse a real sprite (set React Target) when it is its own object
/// Lines are shown through the Lobby dialogue box. Clicking the box closes the line early.
/// </summary>
public class PropHotspot : Hotspot
{
    public enum Reaction { None, Wiggle, Bounce, Pulse }

    [Tooltip("Text on the hover ribbon. Empty = the GameObject name.")]
    public string label = "";
    public string speaker = "Polites";
    [TextArea] public string[] lines = new string[0];
    [Tooltip("Seconds a finished line stays before it closes by itself.")]
    public float idleSeconds = 4f;
    public AudioClip sfx;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    [Tooltip("Show the pulsing \"!\" marker too. Off = only the hover ribbon (less clutter).")]
    public bool showMarker = false;

    [Header("Hover / click feedback")]
    public bool hoverGlow = true;
    public Color glowColor = new Color(0.92f, 0.70f, 0.30f, 0.35f);
    [Tooltip("Your own glow art (child object). Used instead of the code glow when set.")]
    public GameObject glow;
    public Reaction reaction = Reaction.None;
    [Tooltip("The sprite that moves for the reaction / hover pop. Leave empty for painted-in objects.")]
    public Transform reactTarget;
    [Range(0f, 0.2f)] public float hoverPop = 0f;
    [Header("Idle motion (needs React Target)")]
    [Tooltip("Slow breathing / flicker of the sprite. Dog about 0.02, fire about 0.06. 0 = off.")]
    [Range(0f, 0.15f)] public float idleAmount = 0f;
    public float idleSpeed = 1.6f;

    public override string Label => string.IsNullOrEmpty(label) ? gameObject.name : label;
    public override bool ShowMarker => showMarker;

    static PropHotspot speakingOwner;
    static Sprite softSprite;
    static Material glowMat;

    SpriteRenderer glowSr;
    SpriteRenderer[] ownGlow = new SpriteRenderer[0];
    float[] ownGlowAlpha = new float[0];
    Vector3 basePos, baseScale;
    Quaternion baseRot;
    float hov, flash, react;
    int lineIndex;
    Coroutine routine;

    void Awake()
    {
        if (reactTarget == null && (reaction != Reaction.None || hoverPop > 0f || idleAmount > 0f)) reactTarget = transform;
        if (reactTarget != null)
        {
            basePos = reactTarget.localPosition;
            baseScale = reactTarget.localScale;
            baseRot = reactTarget.localRotation;
        }
        if (lines != null && lines.Length > 0) lineIndex = Random.Range(0, lines.Length);

        if (glow != null)
        {
            ownGlow = glow.GetComponentsInChildren<SpriteRenderer>(true);
            ownGlowAlpha = new float[ownGlow.Length];
            for (int i = 0; i < ownGlow.Length; i++) ownGlowAlpha[i] = ownGlow[i].color.a;
            SetOwnGlow(0f);
        }
        else if (hoverGlow) BuildCodeGlow();
    }

    void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (speakingOwner == this) speakingOwner = null;
    }

    // ------------------------------------------------------------------
    // Feedback
    // ------------------------------------------------------------------
    void Update()
    {
        hov = Mathf.MoveTowards(hov, IsHovered ? 1f : 0f, Time.deltaTime * 8f);
        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 2.5f);
        react = Mathf.MoveTowards(react, 0f, Time.deltaTime * 2f);

        if (glowSr != null)
        {
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 4f);
            Color c = glowColor;
            c.a = Mathf.Clamp01(glowColor.a * hov * pulse + 0.5f * flash);
            glowSr.color = c;
            glowSr.enabled = c.a > 0.001f;
        }
        if (glow != null) SetOwnGlow(Mathf.Clamp01(hov + flash));

        if (reactTarget != null)
        {
            float k = Mathf.Sin((1f - react) * Mathf.PI);   // 0 -> 1 -> 0 over the reaction
            if (react <= 0f) k = 0f;
            Vector3 pos = basePos;
            Quaternion rot = baseRot;
            float scale = 1f + hoverPop * hov;
            if (reaction == Reaction.Wiggle)
                rot = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 30f) * 8f * react);
            else if (reaction == Reaction.Bounce) pos += Vector3.up * 0.2f * k;
            else if (reaction == Reaction.Pulse) scale *= 1f + 0.12f * k;
            float idle = 0f;
            if (idleAmount > 0f)
                idle = idleAmount * (0.6f * Mathf.Sin(Time.time * idleSpeed) + 0.4f * Mathf.Sin(Time.time * idleSpeed * 2.7f + 1.3f));
            reactTarget.localPosition = pos;
            reactTarget.localRotation = rot;
            reactTarget.localScale = Vector3.Scale(baseScale * scale, new Vector3(1f + idle * 0.4f, 1f + idle, 1f));
        }
    }

    void SetOwnGlow(float k)
    {
        for (int i = 0; i < ownGlow.Length; i++)
        {
            Color c = ownGlow[i].color;
            c.a = ownGlowAlpha[i] * k;
            ownGlow[i].color = c;
        }
        bool want = k > 0.001f;
        if (glow.activeSelf != want) glow.SetActive(want);
    }

    void BuildCodeGlow()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) col = GetComponentInChildren<Collider2D>();
        if (col == null) return;

        Vector3 center;
        Vector2 size;
        BoxCollider2D box = col as BoxCollider2D;
        if (box != null)
        {
            Vector3 ls = box.transform.lossyScale;
            center = box.transform.TransformPoint(box.offset);
            size = new Vector2(box.size.x * Mathf.Abs(ls.x), box.size.y * Mathf.Abs(ls.y));
        }
        else
        {
            center = col.bounds.center;
            size = col.bounds.size;
        }

        if (glowMat == null) glowMat = new Material(Shader.Find("Sprites/Default"));
        GameObject g = new GameObject("Hover Glow");
        g.transform.SetParent(transform, false);
        g.transform.position = new Vector3(center.x, center.y, 0f);
        Vector3 ps = transform.lossyScale;
        float sx = Mathf.Abs(ps.x) > 0.0001f ? size.x * 1.2f / Mathf.Abs(ps.x) : 1f;
        float sy = Mathf.Abs(ps.y) > 0.0001f ? size.y * 1.2f / Mathf.Abs(ps.y) : 1f;
        g.transform.localScale = new Vector3(sx, sy, 1f);
        glowSr = g.AddComponent<SpriteRenderer>();
        glowSr.sprite = SoftSprite();
        glowSr.sharedMaterial = glowMat;
        glowSr.sortingOrder = 20;
        glowSr.enabled = false;
    }

    static Sprite SoftSprite()
    {
        if (softSprite != null) return softSprite;
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply(false, true);
        softSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return softSprite;
    }

    // ------------------------------------------------------------------
    // Click
    // ------------------------------------------------------------------
    public override void Click()
    {
        flash = 1f;
        react = 1f;
        if (sfx != null && AudioManager.Instance != null) AudioManager.Instance.PlaySfx(sfx, sfxVolume);

        LobbyUI ui = LobbyUI.Instance;
        if (ui == null || lines == null || lines.Length == 0) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(SayRoutine(ui, lines[lineIndex++ % lines.Length]));
    }

    IEnumerator SayRoutine(LobbyUI ui, string line)
    {
        speakingOwner = this;
        ui.ShowSpeech(speaker, line);
        yield return null;
        while (speakingOwner == this && ui.IsSpeechTyping) yield return null;
        float t = 0f;
        while (speakingOwner == this && t < idleSeconds && ui.IsSpeechOpen)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (speakingOwner == this)
        {
            ui.HideSpeech();
            speakingOwner = null;
        }
        routine = null;
    }

    // Yellow outline in the Scene view so invisible (painted-in) props are easy to find and drag.
    void OnDrawGizmos()
    {
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        if (b == null) return;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(b.offset, b.size);
    }
}