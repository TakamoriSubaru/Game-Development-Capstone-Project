using System.Collections.Generic;
using UnityEngine;

namespace Oddyseus.UI
{
    // Pemuat sprite dan font tema. Semua aset ada di Assets/_Project/Resources/UI, dipanggil dengan path relatif
    // tanpa ekstensi, contoh: UISprites.Get("frames/frame_panel_large"). Hasilnya di-cache.
    public static class UISprites
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Font title, body;
        static Sprite white;

        public static Sprite Get(string path)
        {
            Sprite s;
            if (cache.TryGetValue(path, out s)) return s;
            s = Resources.Load<Sprite>("UI/" + path);
            if (s == null) Debug.LogWarning("UISprites: sprite tidak ditemukan: UI/" + path);
            cache[path] = s;
            return s;
        }

        // Judul: Cinzel (bold lewat FontStyle.Bold). Isi: EB Garamond. Keduanya OFL; lihat Resources/UI/Fonts.
        public static Font TitleFont
        {
            get { if (title == null) title = Resources.Load<Font>("UI/Fonts/Cinzel-Variable"); return title; }
        }

        public static Font BodyFont
        {
            get { if (body == null) body = Resources.Load<Font>("UI/Fonts/EBGaramond-Variable"); return body; }
        }

        // Sprite putih 4x4 untuk isi bar dan warna rata (di-tint lewat Image.color).
        public static Sprite White
        {
            get
            {
                if (white == null)
                {
                    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    var px = new Color32[16];
                    for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                    tex.SetPixels32(px);
                    tex.Apply();
                    white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
                }
                return white;
            }
        }
    }
}
