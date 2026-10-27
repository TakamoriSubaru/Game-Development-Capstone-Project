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
        "panel", "panel_cream", "panel_orn", "plate",
        "btn_primary", "btn_primary_hover", "btn_primary_pressed",
        "btn_secondary", "btn_secondary_hover", "btn_secondary_pressed",
        "btn_edge", "btn_edge_hover", "btn_disabled",
        "choice_blue", "choice_blue_hover", "choice_red", "choice_red_hover", "choice_green", "choice_green_hover",
        "glow", "corner", "key_tile", "diamond", "chevron", "chevron_double",
        "node", "node_glow", "marker", "circle", "knob", "pill", "pointer",
        // Warm Parchment Gouache skin
        "pa_panel", "pa_plate", "pa_corner",
        "pa_btn_terra", "pa_btn_terra_hover", "pa_btn_terra_pressed",
        "pa_btn_olive", "pa_btn_olive_hover", "pa_btn_olive_pressed", "pa_btn_disabled",
        "pa_node", "pa_knob", "pa_marker", "pa_pointer", "pa_chevron_double", "pa_dialogue"
    };

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static void ClearCache() { cache.Clear(); }

    /// <summary>True when a real painted PNG exists at Resources/UIKit/<name> (not the code-drawn fallback).</summary>
    public static bool IsAuthored(string name)
    {
        return Resources.Load<Sprite>(ResourcesFolder + name) != null;
    }

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
            case "panel_cream":
            case "panel_orn":
            case "pa_panel":
            case "pa_dialogue": return new Vector4(40, 40, 40, 40);
            case "plate":
            case "pa_plate": return new Vector4(30, 30, 30, 30);
            case "glow": return new Vector4(40, 40, 40, 40);
            case "pill": return new Vector4(15, 15, 15, 15);
        }
        if (name.StartsWith("btn_") || name.StartsWith("pa_btn_") || name.StartsWith("choice_")) return new Vector4(32, 32, 32, 32);
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
            case "panel_orn": return PanelOrnate();
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
            case "corner": return Corner();
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

            // ---- Warm Parchment Gouache ----
            case "pa_panel":
                return PFrame(96, 96, 14f, UITheme.ParchmentDeep, UITheme.Parchment, UITheme.Ink, 3f, 0.05f, A(UITheme.Bronze, 0.6f), 7f);
            case "pa_plate":
                return PFrame(96, 80, 22f, UITheme.Hex("B5643F"), UITheme.Hex("D88A5E"), UITheme.GoldWarm, 2.5f, 0.04f, A(UITheme.Ink, 0f), 0f);
            case "pa_corner": return LeafCorner();
            case "pa_btn_terra":
                return PFrame(96, 96, 30f, UITheme.Hex("B5643F"), UITheme.Hex("D88A5E"), UITheme.GoldWarm, 3f, 0.04f, A(UITheme.Ink, 0f), 0f);
            case "pa_btn_terra_hover":
                return PFrame(96, 96, 30f, UITheme.Hex("E2A743"), UITheme.Hex("F4CB6E"), UITheme.Hex("FFE9B0"), 3f, 0.04f, A(UITheme.Ink, 0f), 0f);
            case "pa_btn_terra_pressed":
                return PFrame(96, 96, 30f, UITheme.Hex("7A3D24"), UITheme.TerracottaDark, UITheme.Hex("C9923A"), 3f, 0.04f, A(UITheme.Ink, 0f), 0f);
            case "pa_btn_olive":
                return PFrame(96, 96, 30f, UITheme.Hex("587A49"), UITheme.Hex("7BA069"), UITheme.GoldWarm, 3f, 0.04f, A(UITheme.Ink, 0f), 0f);
            case "pa_btn_olive_hover":
                return PFrame(96, 96, 30f, UITheme.Hex("6F9660"), UITheme.Hex("93BC82"), UITheme.Hex("FFE9B0"), 3f, 0.04f, A(UITheme.Ink, 0f), 0f);
            case "pa_btn_olive_pressed":
                return PFrame(96, 96, 30f, UITheme.Hex("3F5C35"), UITheme.Hex("4F6E43"), UITheme.Hex("C9923A"), 3f, 0.04f, A(UITheme.Ink, 0f), 0f);
            case "pa_btn_disabled":
                return PFrame(96, 96, 30f, A(UITheme.Hex("D9CDB4"), 0.85f), A(UITheme.Hex("E6DBC3"), 0.85f), A(UITheme.Hex("A89C84"), 0.9f), 2.5f, 0.03f, A(UITheme.Ink, 0f), 0f);
            case "pa_node": return PaNode();
            case "pa_knob": return PaKnob();
            case "pa_marker": return PaMarker();
            case "pa_pointer": return PaPointer();
            case "pa_chevron_double":
                return Strokes(28, 28, UITheme.Ink, 3f, new float[][]
                {
                    new float[]{4,6,11,14}, new float[]{11,14,4,22},
                    new float[]{14,6,21,14}, new float[]{21,14,14,22}
                });
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


    // Panel frame with the corner ornaments baked into its four corners (inner bracket + small square).
    // Square-ish corners (radius 6) so the ornament sits exactly on the frame. 9-slice border is 40,
    // so the 40x40 corner area is never stretched.
    static readonly float[][] OrnSegs =
    {
        new float[]{8.5f, 8.5f, 26f, 8.5f}, new float[]{8.5f, 8.5f, 8.5f, 26f},
        new float[]{12f, 12f, 19f, 12f}, new float[]{19f, 12f, 19f, 19f},
        new float[]{19f, 19f, 12f, 19f}, new float[]{12f, 19f, 12f, 12f}
    };
    static readonly Vector2[] OrnDots = { new Vector2(28.5f, 8.5f), new Vector2(8.5f, 28.5f) };

    static Texture2D PanelOrnate()
    {
        const int w = 96, h = 96;
        Texture2D t = Frame(w, h, false, 6f, 1f, A(UITheme.NavyDeep, 0.97f), A(UITheme.Navy, 0.97f), UITheme.Gold, 3f);
        Color[] px = t.GetPixels();
        Color gold = UITheme.Gold;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float best = 999f;
                for (int c = 0; c < 4; c++)
                {
                    // corner-local coordinates: origin at the frame corner, both axes grow inward
                    float lx = (c == 0 || c == 2) ? fx : w - fx;
                    float ly = (c == 0 || c == 1) ? h - fy : fy;
                    if (lx >= 40f || ly >= 40f) continue;
                    for (int i = 0; i < OrnSegs.Length; i++)
                    {
                        float d = SegDist(lx, ly, OrnSegs[i][0], OrnSegs[i][1], OrnSegs[i][2], OrnSegs[i][3]) - 1.1f;
                        if (d < best) best = d;
                    }
                    for (int i = 0; i < OrnDots.Length; i++)
                    {
                        float d = Vector2.Distance(new Vector2(lx, ly), OrnDots[i]) - 2.3f;
                        if (d < best) best = d;
                    }
                }
                float a = Mathf.Clamp01(0.5f - best);
                if (a <= 0f) continue;
                Color o = px[y * w + x];
                o.r = Mathf.Lerp(o.r, gold.r, a);
                o.g = Mathf.Lerp(o.g, gold.g, a);
                o.b = Mathf.Lerp(o.b, gold.b, a);
                o.a = Mathf.Max(o.a, a);
                px[y * w + x] = o;
            }
        t.SetPixels(px);
        t.Apply(false, false);
        return t;
    }

    // Corner ornament (40x40, top-left version; AddCorners mirrors it). It follows the rounded corner of the
    // "panel" frame (radius 22, border 3, 1 px inset => arc centre (23,17)): an inner bracket that runs
    // parallel to the frame, with a small dot at each end.
    static Texture2D Corner()
    {
        const int n = 40;
        const float cx = 23f, cy = 17f, r = 14.5f, thick = 2.4f, dot = 2.4f;
        const int steps = 12;
        List<float[]> segs = new List<float[]>();
        for (int i = 0; i < steps; i++)
        {
            float a0 = Mathf.Deg2Rad * (180f - 90f * i / steps);
            float a1 = Mathf.Deg2Rad * (180f - 90f * (i + 1) / steps);
            segs.Add(new float[] { cx + r * Mathf.Cos(a0), cy + r * Mathf.Sin(a0), cx + r * Mathf.Cos(a1), cy + r * Mathf.Sin(a1) });
        }
        segs.Add(new float[] { cx - r, cy, cx - r, 5f });       // tail along the left edge
        segs.Add(new float[] { cx, cy + r, 36f, cy + r });      // tail along the top edge
        Vector2 dotA = new Vector2(cx - r, 4.5f), dotB = new Vector2(37f, cy + r);

        Color[] px = new Color[n * n];
        Color gold = UITheme.Gold;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float best = 999f;
                for (int i = 0; i < segs.Count; i++)
                {
                    float d = SegDist(x + 0.5f, y + 0.5f, segs[i][0], segs[i][1], segs[i][2], segs[i][3]);
                    if (d < best) best = d;
                }
                float a = Mathf.Clamp01(thick * 0.5f + 0.5f - best);
                float da = Mathf.Clamp01(0.5f - (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), dotA) - dot));
                float db = Mathf.Clamp01(0.5f - (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), dotB) - dot));
                a = Mathf.Max(a, Mathf.Max(da, db));
                px[y * n + x] = new Color(gold.r, gold.g, gold.b, a);
            }
        return ToTex(n, n, px);
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

    // ------------------------------------------------------------------
    // Warm Parchment Gouache drawing
    // ------------------------------------------------------------------
    // Deterministic paper grain, 0..1
    static float Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }
    }

    // Rounded frame with a vertical gradient fill, paper grain and an optional thin inner line.
    static Texture2D PFrame(int w, int h, float radius, Color fillBottom, Color fillTop, Color border, float bw,
                            float grain, Color innerLine, float innerInset)
    {
        Color[] px = new Color[w * h];
        float hx = w * 0.5f - 1f, hy = h * 0.5f - 1f;
        for (int y = 0; y < h; y++)
        {
            Color fill = Color.Lerp(fillBottom, fillTop, (y + 0.5f) / h);
            for (int x = 0; x < w; x++)
            {
                float cx = x + 0.5f - w * 0.5f, cy = y + 0.5f - h * 0.5f;
                float d = SdRound(cx, cy, hx, hy, radius);
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (d + bw));

                float g = (Hash(x, y) - 0.5f) * grain;
                Color f = new Color(fill.r + g, fill.g + g, fill.b + g, fill.a);
                if (innerLine.a > 0f)
                {
                    float dl = Mathf.Abs(d + bw + innerInset) - 0.7f;
                    float la = Mathf.Clamp01(0.5f - dl) * innerLine.a;
                    f = Color.Lerp(f, new Color(innerLine.r, innerLine.g, innerLine.b, f.a), la);
                }

                Color c = Color.Lerp(border, f, inner);
                c.a = outer * Mathf.Lerp(border.a, f.a, inner);
                px[y * w + x] = c;
            }
        }
        return ToTex(w, h, px);
    }

    // Distance (in pixels, approx.) to a rotated ellipse; negative inside.
    static float SdLeaf(float px, float py, float cx, float cy, float angle, float ra, float rb)
    {
        float dx = px - cx, dy = py - cy;
        float ca = Mathf.Cos(angle), sa = Mathf.Sin(angle);
        float lx = dx * ca + dy * sa;
        float ly = -dx * sa + dy * ca;
        float k = Mathf.Sqrt((lx * lx) / (ra * ra) + (ly * ly) / (rb * rb));
        return (k - 1f) * Mathf.Min(ra, rb);
    }

    // Olive-leaf sprig for the panel corners (40x40, top-left version; AddCorners mirrors it).
    static Texture2D LeafCorner()
    {
        const int n = 40;
        Vector2 p0 = new Vector2(5f, 34f), p1 = new Vector2(10f, 12f), p2 = new Vector2(33f, 6f);
        const int steps = 14;
        Vector2[] pts = new Vector2[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            pts[i] = (1f - t) * (1f - t) * p0 + 2f * (1f - t) * t * p1 + t * t * p2;
        }

        // leaves: index on the stem, side (+1 / -1)
        int[] leafAt = { 3, 5, 8, 10, 13 };
        float[] leafSide = { 1f, -1f, 1f, -1f, 1f };
        int ln = leafAt.Length;
        Vector2[] lc = new Vector2[ln];
        float[] la = new float[ln];
        for (int i = 0; i < ln; i++)
        {
            int k = leafAt[i];
            Vector2 tan = (pts[Mathf.Min(k + 1, steps)] - pts[Mathf.Max(k - 1, 0)]).normalized;
            float ang = Mathf.Atan2(tan.y, tan.x) + leafSide[i] * 0.85f;
            la[i] = ang;
            lc[i] = pts[k] + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 4.2f;
        }

        Color olive = UITheme.Olive, ink = UITheme.Ink;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float stem = 999f;
                for (int i = 0; i < steps; i++)
                {
                    float d = SegDist(fx, fy, pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y);
                    if (d < stem) stem = d;
                }
                float leaf = 999f;
                for (int i = 0; i < ln; i++)
                {
                    float d = SdLeaf(fx, fy, lc[i].x, lc[i].y, la[i], 5.4f, 2.5f);
                    if (d < leaf) leaf = d;
                }
                float leafFill = Mathf.Clamp01(0.5f - leaf);
                float leafEdge = Mathf.Clamp01(0.5f - (leaf + 0.9f));
                float stemA = Mathf.Clamp01(0.8f + 0.5f - stem);   // 1.6 px wide stem

                Color c = Color.Lerp(ink, olive, leafEdge);        // ink outline, olive inside
                float a = leafFill;
                c = Color.Lerp(c, ink, stemA);
                a = Mathf.Max(a, stemA);
                px[y * n + x] = new Color(c.r, c.g, c.b, a);
            }
        }
        return ToTex(n, n, px);
    }

    // Round node, drawn in grey so it can be tinted (olive normally, gold when active).
    static Texture2D PaNode()
    {
        int n = 64;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float cx = x + 0.5f - n * 0.5f, cy = y + 0.5f - n * 0.5f;
                float d = Mathf.Sqrt(cx * cx + cy * cy) - 27f;
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (d + 3.5f));
                float shade = Mathf.Lerp(0.82f, 1f, (y + 0.5f) / n);
                float g = (Hash(x, y) - 0.5f) * 0.05f;
                Color fill = new Color(shade + g, shade + g, shade + g, 1f);
                Color c = Color.Lerp(new Color(0.32f, 0.32f, 0.32f, 1f), fill, inner);
                c.a = outer;
                px[y * n + x] = c;
            }
        return ToTex(n, n, px);
    }

    static Texture2D PaKnob()
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
                float inner = Mathf.Clamp01(0.5f - (d + 7f));
                Color fill = Color.Lerp(UITheme.GoldWarm, UITheme.GoldLight, (y + 0.5f) / n);
                Color c = Color.Lerp(UITheme.Ink, fill, inner);
                c.a = outer;
                px[y * n + x] = c;
            }
        return ToTex(n, n, px);
    }

    static Texture2D PaMarker()
    {
        int n = 48;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float cx = x + 0.5f - n * 0.5f, cy = y + 0.5f - n * 0.5f;
                float d = Mathf.Sqrt(cx * cx + cy * cy) - 22.5f;
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (d + 3f));
                Color fill = Color.Lerp(UITheme.Hex("B5643F"), UITheme.Hex("D88A5E"), (y + 0.5f) / n);
                Color c = Color.Lerp(UITheme.GoldWarm, fill, inner);
                float bar = SegDist(x + 0.5f, y + 0.5f, 24f, 21f, 24f, 33f) - 3f;
                float dot = SegDist(x + 0.5f, y + 0.5f, 24f, 13f, 24f, 13f) - 3.4f;
                float mark = Mathf.Clamp01(0.5f - Mathf.Min(bar, dot));
                c = Color.Lerp(c, UITheme.Paper, mark);
                c.a = outer;
                px[y * n + x] = c;
            }
        return ToTex(n, n, px);
    }

    static Texture2D PaPointer()
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
                Color c = Color.Lerp(UITheme.GoldWarm, UITheme.Hex("D88A5E"), inner);
                c.a = outer;
                px[y * w + x] = c;
            }
        return ToTex(w, h, px);
    }
}