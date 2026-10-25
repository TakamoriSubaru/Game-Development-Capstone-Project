using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placeholder sprites drawn from code (no art files needed).
/// UIArt.Get(name) first looks for a real sprite at Resources/UIKit/<name>
/// (the builder writes PNG files there; your artist can overwrite them),
/// and if there is none it draws one in memory.
/// </summary>
public static class UIArt
{
    public const string ResourcesFolder = "UIKit/";

    public static readonly string[] Names =
    {
        "panel", "panel_cream", "plate",
        "btn_primary", "btn_primary_hover", "btn_primary_pressed",
        "btn_secondary", "btn_secondary_hover", "btn_secondary_pressed",
        "btn_edge", "btn_edge_hover", "btn_disabled",
        "choice_blue", "choice_blue_hover", "choice_red", "choice_red_hover", "choice_green", "choice_green_hover",
        "glow", "corner", "key_tile", "diamond", "chevron", "chevron_double",
        "node", "node_glow", "marker", "circle", "knob", "pill", "pointer"
    };

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static void ClearCache() { cache.Clear(); }

    public static Sprite Get(string name)
    {
        Sprite s;
        if (cache.TryGetValue(name, out s) && s != null) return s;
        s = Resources.Load<Sprite>(ResourcesFolder + name);
        if (s == null)
        {
            Texture2D tex = Create(name);
            if (tex != null) s = MakeSprite(name, tex);
        }
        cache[name] = s;
        return s;
    }

    static Sprite MakeSprite(string name, Texture2D tex)
    {
        tex.name = name;
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                                  100f, 0, SpriteMeshType.FullRect, BorderOf(name));
        sp.name = name;
        return sp;
    }

    // Sprite border for 9-slice: (left, bottom, right, top)
    public static Vector4 BorderOf(string name)
    {
        switch (name)
        {
            case "panel":
            case "panel_cream": return new Vector4(40, 40, 40, 40);
            case "plate": return new Vector4(30, 30, 30, 30);
            case "glow": return new Vector4(40, 40, 40, 40);
            case "pill": return new Vector4(15, 15, 15, 15);
        }
        if (name.StartsWith("btn_") || name.StartsWith("choice_")) return new Vector4(32, 32, 32, 32);
        return Vector4.zero;
    }

    // ------------------------------------------------------------------
    // Drawing
    // ------------------------------------------------------------------
    public static Texture2D Create(string name)
    {
        Color navy = UITheme.Navy, deep = UITheme.NavyDeep, gold = UITheme.Gold, bronze = UITheme.Bronze;
        switch (name)
        {
            case "panel":
                return Frame(96, 96, false, 22f, 1f, A(deep, 0.97f), A(navy, 0.97f), gold, 3f);
            case "panel_cream":
                return Frame(96, 96, false, 22f, 1f, UITheme.Hex("E9DAB8"), UITheme.CardCream, gold, 4f);
            case "plate":
                return Frame(96, 80, true, 14f, 1f, A(deep, 0.98f), A(navy, 0.98f), gold, 3f);

            case "btn_primary":
                return Frame(96, 96, false, 30f, 1f, deep, navy, gold, 3f);
            case "btn_primary_hover":
                return Frame(96, 96, false, 30f, 1f, bronze, gold, UITheme.Accent, 3f);
            case "btn_primary_pressed":
                return Frame(96, 96, false, 30f, 1f, UITheme.Hex("7A5520"), bronze, bronze, 3f);

            case "btn_secondary":
                return Frame(96, 96, false, 30f, 1f, deep, navy, UITheme.BlueLight, 2.5f);
            case "btn_secondary_hover":
                return Frame(96, 96, false, 30f, 1f, UITheme.Hex("22546F"), UITheme.Blue, UITheme.Hex("DFF1FF"), 3f);
            case "btn_secondary_pressed":
                return Frame(96, 96, false, 30f, 1f, deep, UITheme.Hex("0F3556"), UITheme.BlueLight, 2.5f);

            case "btn_edge":
                return Frame(96, 96, false, 30f, 1f, deep, navy, gold, 3f);
            case "btn_edge_hover":
                return Frame(96, 96, false, 30f, 1f, deep, navy, UITheme.Hex("DFF1FF"), 3.5f);
            case "btn_disabled":
                return Frame(96, 96, false, 30f, 1f, A(deep, 0.6f), A(navy, 0.6f), A(UITheme.Disabled, 0.6f), 2.5f);

            case "choice_blue": return Choice(UITheme.ChoiceBlue, gold, false);
            case "choice_blue_hover": return Choice(UITheme.ChoiceBlue, UITheme.Accent, true);
            case "choice_red": return Choice(UITheme.ChoiceRed, UITheme.Hex("E0907E"), false);
            case "choice_red_hover": return Choice(UITheme.ChoiceRed, UITheme.Accent, true);
            case "choice_green": return Choice(UITheme.ChoiceGreen, UITheme.Hex("A6D3B5"), false);
            case "choice_green_hover": return Choice(UITheme.ChoiceGreen, UITheme.Accent, true);

            case "glow": return Glow();
            case "corner":
                return Strokes(40, 40, gold, 2.4f, new float[][]
                {
                    new float[]{3,37,37,37}, new float[]{3,37,3,3},
                    new float[]{9,31,26,31}, new float[]{9,31,9,14},
                    new float[]{14,25,21,25}, new float[]{21,25,21,18},
                    new float[]{21,18,14,18}, new float[]{14,18,14,25}
                });
            case "key_tile":
                return Strokes(32, 32, gold, 2.2f, new float[][]
                {
                    new float[]{4,4,28,4}, new float[]{28,4,28,28}, new float[]{28,28,4,28}, new float[]{4,28,4,12},
                    new float[]{4,12,11,12},
                    new float[]{11,11,21,11}, new float[]{21,11,21,21}, new float[]{21,21,11,21}, new float[]{11,21,11,11}
                });
            case "diamond": return Diamond();
            case "chevron":
                return Strokes(28, 28, Color.white, 3.2f, new float[][]
                {
                    new float[]{9,5,19,14}, new float[]{19,14,9,23}
                });
            case "chevron_double":
                return Strokes(28, 28, gold, 3f, new float[][]
                {
                    new float[]{4,6,11,14}, new float[]{11,14,4,22},
                    new float[]{14,6,21,14}, new float[]{21,14,14,22}
                });
            case "node": return Node();
            case "node_glow": return Radial(128);
            case "marker": return Marker();
            case "circle": return Circle(64);
            case "knob": return Knob();
            case "pill": return Frame(32, 32, false, 16f, 0f, Color.white, Color.white, Color.white, 0f);
            case "pointer": return Pointer();
        }
        return null;
    }

    static Color A(Color c, float a) { c.a = a; return c; }

    static Texture2D ToTex(int w, int h, Color[] px)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        t.SetPixels(px);
        t.Apply(false, false);
        return t;
    }

    static float SdRound(float px, float py, float hx, float hy, float r)
    {
        float qx = Mathf.Abs(px) - hx + r;
        float qy = Mathf.Abs(py) - hy + r;
        float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
        return Mathf.Min(Mathf.Max(qx, qy), 0f) + Mathf.Sqrt(ox * ox + oy * oy) - r;
    }

    static float SdChamfer(float px, float py, float hx, float hy, float cut)
    {
        float box = Mathf.Max(Mathf.Abs(px) - hx, Mathf.Abs(py) - hy);
        float diag = (Mathf.Abs(px) + Mathf.Abs(py) - (hx + hy - cut)) * 0.70710678f;
        return Mathf.Max(box, diag);
    }

    static Texture2D Frame(int w, int h, bool chamfer, float param, float inset,
                           Color fillBottom, Color fillTop, Color border, float bw)
    {
        Color[] px = new Color[w * h];
        float hx = w * 0.5f - inset, hy = h * 0.5f - inset;
        for (int y = 0; y < h; y++)
        {
            Color fill = Color.Lerp(fillBottom, fillTop, (y + 0.5f) / h);
            for (int x = 0; x < w; x++)
            {
                float cx = x + 0.5f - w * 0.5f, cy = y + 0.5f - h * 0.5f;
                float d = chamfer ? SdChamfer(cx, cy, hx, hy, param) : SdRound(cx, cy, hx, hy, param);
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (d + bw));
                Color c = Color.Lerp(border, fill, inner);
                c.a = outer * Mathf.Lerp(border.a, fill.a, inner);
                px[y * w + x] = c;
            }
        }
        return ToTex(w, h, px);
    }

    static Texture2D Choice(Color fill, Color border, bool hover)
    {
        Color top = Color.Lerp(fill, Color.white, hover ? 0.22f : 0.08f);
        Color bottom = Color.Lerp(fill, Color.black, 0.15f);
        if (hover) bottom = Color.Lerp(fill, Color.white, 0.08f);
        return Frame(96, 96, false, 26f, 1f, bottom, top, border, hover ? 3.5f : 2.5f);
    }

    static Texture2D Glow()
    {
        int n = 96;
        Color[] px = new Color[n * n];
        float half = 26f, r = 20f, fall = 22f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = SdRound(x + 0.5f - n * 0.5f, y + 0.5f - n * 0.5f, half, half, r);
                float a = d <= 0f ? 1f : Mathf.Pow(Mathf.Clamp01(1f - d / fall), 2f);
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        return ToTex(n, n, px);
    }

    static float SegDist(float px, float py, float ax, float ay, float bx, float by)
    {
        float dx = bx - ax, dy = by - ay;
        float l2 = dx * dx + dy * dy;
        float t = l2 <= 0f ? 0f : Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / l2);
        float qx = ax + t * dx - px, qy = ay + t * dy - py;
        return Mathf.Sqrt(qx * qx + qy * qy);
    }

    static Texture2D Strokes(int w, int h, Color color, float thick, float[][] segs)
    {
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float best = 999f;
                for (int i = 0; i < segs.Length; i++)
                {
                    float d = SegDist(x + 0.5f, y + 0.5f, segs[i][0], segs[i][1], segs[i][2], segs[i][3]);
                    if (d < best) best = d;
                }
                px[y * w + x] = new Color(color.r, color.g, color.b, Mathf.Clamp01(thick * 0.5f + 0.5f - best));
            }
        return ToTex(w, h, px);
    }

    static Texture2D Diamond()
    {
        int n = 24;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float cx = x + 0.5f - n * 0.5f, cy = y + 0.5f - n * 0.5f;
                float d = (Mathf.Abs(cx) + Mathf.Abs(cy) - 9f) * 0.70710678f;
                Color c = UITheme.Gold;
                c.a = Mathf.Clamp01(0.5f - d);
                px[y * n + x] = c;
            }
        return ToTex(n, n, px);
    }

    static Texture2D Node()
    {
        int n = 64;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float cx = x + 0.5f - n * 0.5f, cy = y + 0.5f - n * 0.5f;
                float r = Mathf.Sqrt(cx * cx + cy * cy);
                float d = r - 27f;
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (d + 3f));
                Color c = Color.Lerp(UITheme.Gold, UITheme.Navy, inner);
                float dd = (Mathf.Abs(cx) + Mathf.Abs(cy) - 10f) * 0.70710678f;
                c = Color.Lerp(c, UITheme.Gold, Mathf.Clamp01(0.5f - dd));
                c.a = outer;
                px[y * n + x] = c;
            }
        return ToTex(n, n, px);
    }

    static Texture2D Marker()
    {
        int n = 48;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float cx = x + 0.5f - n * 0.5f, cy = y + 0.5f - n * 0.5f;
                float r = Mathf.Sqrt(cx * cx + cy * cy);
                float d = r - 22.5f;
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (d + 3f));
                Color c = Color.Lerp(UITheme.Gold, A(UITheme.Navy, 0.96f), inner);
                float bar = SegDist(x + 0.5f, y + 0.5f, 24f, 21f, 24f, 33f) - 3f;
                float dot = SegDist(x + 0.5f, y + 0.5f, 24f, 13f, 24f, 13f) - 3.4f;
                float mark = Mathf.Clamp01(0.5f - Mathf.Min(bar, dot));
                c = Color.Lerp(c, UITheme.Gold, mark);
                c.a = outer * c.a;
                px[y * n + x] = c;
            }
        return ToTex(n, n, px);
    }

    static Texture2D Radial(int n)
    {
        Color[] px = new Color[n * n];
        float half = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float cx = x + 0.5f - half, cy = y + 0.5f - half;
                float r = Mathf.Sqrt(cx * cx + cy * cy) / half;
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - r), 2f) * 0.95f);
            }
        return ToTex(n, n, px);
    }

    static Texture2D Circle(int n)
    {
        Color[] px = new Color[n * n];
        float half = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float cx = x + 0.5f - half, cy = y + 0.5f - half;
                float r = Mathf.Sqrt(cx * cx + cy * cy);
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - (r - (half - 1f))));
            }
        return ToTex(n, n, px);
    }

    static Texture2D Knob()
    {
        int n = 64;
        Color[] px = new Color[n * n];
        float half = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float cx = x + 0.5f - half, cy = y + 0.5f - half;
                float d = Mathf.Sqrt(cx * cx + cy * cy) - (half - 1f);
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (d + 6f));
                Color fill = Color.Lerp(UITheme.Gold, UITheme.Accent, (y + 0.5f) / n);
                Color c = Color.Lerp(UITheme.Bronze, fill, inner);
                c.a = outer;
                px[y * n + x] = c;
            }
        return ToTex(n, n, px);
    }

    static Texture2D Pointer()
    {
        int w = 28, h = 20;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float cx = x + 0.5f - w * 0.5f, ty = y + 0.5f;
                float d = (Mathf.Abs(cx) - 0.7f * ty) / 1.2207f;
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (d + 3f));
                Color c = Color.Lerp(UITheme.Gold, UITheme.Navy, inner);
                c.a = outer;
                px[y * w + x] = c;
            }
        return ToTex(w, h, px);
    }
}
