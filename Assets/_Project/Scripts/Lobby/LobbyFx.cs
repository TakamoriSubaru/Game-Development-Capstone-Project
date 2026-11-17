using UnityEngine;

/// <summary>
/// Small helpers for the lobby effects, all drawn from code (no art files needed):
/// soft glow disc, heart, "Z", a shared sprite material, flicker noise and the visible bounds of a sprite.
/// Used by FireAnimator and PropLife.
/// </summary>
public static class LobbyFx
{
    static Material mat;
    static Sprite soft, heart, zee;

    public static Material Mat
    {
        get
        {
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
            return mat;
        }
    }

    /// <summary>Round soft glow, 1 world unit wide.</summary>
    public static Sprite Soft
    {
        get
        {
            if (soft != null) return soft;
            const int n = 64;
            Texture2D tex = NewTex(n);
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
            soft = Finish(tex, px, n);
            return soft;
        }
    }

    /// <summary>Small heart, 1 world unit wide.</summary>
    public static Sprite Heart
    {
        get
        {
            if (heart != null) return heart;
            const int n = 48;
            Texture2D tex = NewTex(n);
            Color[] px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float hx = ((x + 0.5f) / n * 2f - 1f) * 1.25f;
                    float hy = ((y + 0.5f) / n * 2f - 1f) * 1.25f + 0.15f;
                    float t = hx * hx + hy * hy - 1f;
                    float f = t * t * t - hx * hx * hy * hy * hy;     // < 0 inside the heart
                    float a = Mathf.Clamp01(0.5f - f * 7f);
                    Color c = Color.Lerp(new Color(0.82f, 0.18f, 0.28f), new Color(1f, 0.55f, 0.60f), Mathf.Clamp01(hy * 0.5f + 0.5f));
                    px[y * n + x] = new Color(c.r, c.g, c.b, a);
                }
            heart = Finish(tex, px, n);
            return heart;
        }
    }

    /// <summary>The letter Z with a dark soft outline, 1 world unit wide.</summary>
    public static Sprite Zee
    {
        get
        {
            if (zee != null) return zee;
            const int n = 48;
            Texture2D tex = NewTex(n);
            Color[] px = new Color[n * n];
            Vector2 a0 = new Vector2(9f, 38f), a1 = new Vector2(38f, 38f);   // top bar
            Vector2 b0 = new Vector2(38f, 38f), b1 = new Vector2(9f, 9f);    // diagonal
            Vector2 c0 = new Vector2(9f, 9f), c1 = new Vector2(38f, 9f);     // bottom bar
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Mathf.Min(SegDist(p, a0, a1), Mathf.Min(SegDist(p, b0, b1), SegDist(p, c0, c1)));
                    float inner = Mathf.Clamp01(3.4f - d);
                    float outer = Mathf.Clamp01(5.6f - d);
                    Color c = Color.Lerp(new Color(0.30f, 0.15f, 0.07f), new Color(1f, 0.96f, 0.82f), inner);
                    px[y * n + x] = new Color(c.r, c.g, c.b, outer);
                }
            zee = Finish(tex, px, n);
            return zee;
        }
    }

    static float SegDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }

    static Texture2D NewTex(int n)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    static Sprite Finish(Texture2D tex, Color[] px, int n)
    {
        tex.SetPixels(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }

    /// <summary>Smooth flicker 0..1 (several sine waves plus noise), different per seed.</summary>
    public static float Flicker(float t, float seed)
    {
        float n = Mathf.PerlinNoise(t * 6.5f + seed * 13.1f, seed * 7.7f);
        float s = 0.5f + 0.5f * Mathf.Sin(t * 23f + seed * 5f);
        return Mathf.Clamp01(0.75f * n + 0.25f * s);
    }

    /// <summary>
    /// Bounds (world) of the part of a sprite that is not transparent. Falls back to the full sprite bounds
    /// when the texture is not readable. Call it while the object is not rotated.
    /// </summary>
    public static Bounds VisibleWorldBounds(SpriteRenderer sr)
    {
        Bounds full = sr.bounds;
        Sprite sp = sr.sprite;
        if (sp == null) return full;
        try
        {
            Texture2D tex = sp.texture;
            Rect r = sp.rect;
            Color32[] px = tex.GetPixels32();
            int tw = tex.width;
            int x0 = (int)r.x, y0 = (int)r.y, x1 = (int)(r.x + r.width), y1 = (int)(r.y + r.height);
            int minX = x1, minY = y1, maxX = -1, maxY = -1;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    if (px[y * tw + x].a > 20)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
            if (maxX < 0) return full;
            float ppu = sp.pixelsPerUnit;
            Vector2 pv = sp.pivot;   // pixels, relative to the sprite rect
            Vector3 lo = new Vector3((minX - x0 - pv.x) / ppu, (minY - y0 - pv.y) / ppu, 0f);
            Vector3 hi = new Vector3((maxX + 1 - x0 - pv.x) / ppu, (maxY + 1 - y0 - pv.y) / ppu, 0f);
            Vector3 a = sr.transform.TransformPoint(lo);
            Vector3 b = sr.transform.TransformPoint(hi);
            Bounds bb = new Bounds(a, Vector3.zero);
            bb.Encapsulate(b);
            return bb;
        }
        catch (System.Exception)
        {
            return full;   // texture not readable
        }
    }
}
