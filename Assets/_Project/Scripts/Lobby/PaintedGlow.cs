using UnityEngine;

/// <summary>
/// Soft warm glow over a hotspot that is only PAINTED into the background (story window, exit door...).
/// Put it next to a Hotspot + BoxCollider2D. The glow fades in on hover and flashes a little on click.
/// Nothing to assign: it sizes itself from the collider.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class PaintedGlow : MonoBehaviour
{
    public Color color = new Color(1f, 0.82f, 0.45f, 0.30f);
    [Tooltip("1 = exactly the collider, 1.15 = a bit bigger.")]
    public float grow = 1.1f;
    public float fadeSpeed = 8f;
    public int sortingOrder = 20;

    Hotspot hotspot;
    SpriteRenderer sr;
    float k;
    static Sprite soft;
    static Material mat;

    void Awake()
    {
        hotspot = GetComponent<Hotspot>();
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));

        GameObject g = new GameObject("Hover Glow");
        g.transform.SetParent(transform, false);
        Vector3 ls = transform.lossyScale;
        Vector3 center = transform.TransformPoint(b.offset);
        g.transform.position = new Vector3(center.x, center.y, 0f);
        float sx = Mathf.Abs(ls.x) > 0.0001f ? b.size.x * grow : 1f;
        float sy = Mathf.Abs(ls.y) > 0.0001f ? b.size.y * grow : 1f;
        g.transform.localScale = new Vector3(sx, sy, 1f);
        sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = Soft();
        sr.sharedMaterial = mat;
        sr.sortingOrder = sortingOrder;
        sr.enabled = false;
    }

    void Update()
    {
        if (sr == null) return;
        bool on = hotspot != null && hotspot.IsHovered;
        k = Mathf.MoveTowards(k, on ? 1f : 0f, Time.deltaTime * fadeSpeed);
        float pulse = 0.88f + 0.12f * Mathf.Sin(Time.time * 4f);
        Color c = color;
        c.a = color.a * k * pulse;
        sr.color = c;
        sr.enabled = c.a > 0.002f;
    }

    static Sprite Soft()
    {
        if (soft != null) return soft;
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
                float a = Mathf.Clamp01(1.25f - Mathf.Sqrt(dx * dx + dy * dy) * 1.1f);
                a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply(false, true);
        soft = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return soft;
    }
}