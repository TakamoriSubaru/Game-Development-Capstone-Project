using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Art slots for the Dungeon Hub, Voyage and Collection screens.
/// Every slot is an object named "NAME (replace sprite)".
/// To replace a placeholder: put a PNG (Texture Type = Sprite) at
///   Assets/_Project/Resources/Art/NAME (replace sprite).png
/// No code change is needed. Placeholders are drawn from code below.
/// </summary>
public static class SlotArt
{
    public const string Folder = "Art/";
    static readonly Dictionary<string, Sprite> real = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, Sprite> made = new Dictionary<string, Sprite>();

    delegate float AlphaAt(float dx, float dy);

    public static string SlotName(string name) { return name + " (replace sprite)"; }

    public static Sprite Load(string name)
    {
        Sprite s;
        if (real.TryGetValue(name, out s)) return s;
        s = Resources.Load<Sprite>(Folder + SlotName(name));
        real[name] = s;
        return s;
    }

    public static bool Has(string name) { return Load(name) != null; }

    // Ring: thickness is 0..1 of the radius.
    public static Sprite Ring(float thickness)
    {
        string key = "ring" + thickness.ToString("0.000");
        Sprite s;
        if (made.TryGetValue(key, out s) && s != null) return s;
        const int n = 256;
        float inner = 1f - thickness;
        s = Make(key, n, new Vector2(0.5f, 0.5f), delegate (float dx, float dy)
        {
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01((1f - r) * n * 0.5f) * Mathf.Clamp01((r - inner) * n * 0.5f);
        });
        made[key] = s;
        return s;
    }

    public static Sprite Disc()
    {
        const string key = "disc";
        Sprite s;
        if (made.TryGetValue(key, out s) && s != null) return s;
        const int n = 256;
        s = Make(key, n, new Vector2(0.5f, 0.5f), delegate (float dx, float dy)
        {
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01((1f - r) * n * 0.5f);
        });
        made[key] = s;
        return s;
    }

    // Narrow triangle pointing up. Pivot is the bottom centre (compass needles).
    public static Sprite Needle()
    {
        const string key = "needle";
        Sprite s;
        if (made.TryGetValue(key, out s) && s != null) return s;
        const int n = 256;
        s = Make(key, n, new Vector2(0.5f, 0f), delegate (float dx, float dy)
        {
            float v = (dy + 1f) * 0.5f;
            float half = 1f - v;
            return Mathf.Clamp01((half - Mathf.Abs(dx)) * n * 0.5f);
        });
        made[key] = s;
        return s;
    }

    static Sprite Make(string key, int n, Vector2 pivot, AlphaAt f)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(f(dx, dy)) * 255f);
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        tex.name = key;
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, n, n), pivot, 100f);
        sp.name = key;
        return sp;
    }
}
