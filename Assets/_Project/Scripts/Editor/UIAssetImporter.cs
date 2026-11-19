#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oddyseus.EditorTools
{
    // Semua PNG di Assets/_Project/Resources/UI otomatis jadi Sprite (Single), transparan, tanpa mipmap, Bilinear.
    // Frame yang dipakai sebagai 9-slice diberi border di sini (urutan Unity: kiri, bawah, kanan, atas; satuan piksel gambar asli).
    // Pusat frame itu rata (flat), dekorasi ada di sudut, jadi aman diregangkan.
    // Setelah mengubah angka di sini: klik kanan folder Resources/UI > Reimport.
    public class UIAssetImporter : AssetPostprocessor
    {
        const string Root = "Assets/_Project/Resources/UI/";

        // key = path relatif terhadap Root tanpa ekstensi. Nilai = (kiri, bawah, kanan, atas).
        static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "frames/frame_panel_large", new Vector4(96, 96, 96, 96) },
            { "frames/frame_card_tall",   new Vector4(36, 36, 36, 36) },
            { "frames/frame_row_wide",    new Vector4(70, 56, 70, 56) },
            { "frames/frame_menu_button", new Vector4(90, 34, 90, 34) },
            { "frames/slot_normal",       new Vector4(42, 36, 42, 42) },
            { "frames/slot_hover",        new Vector4(36, 36, 36, 36) },
            { "frames/slot_selected",     new Vector4(36, 36, 36, 36) },
            { "frames/slot_unknown",      new Vector4(36, 36, 36, 36) },
            { "relic_frames/frame_common",    new Vector4(112, 112, 112, 112) },
            { "relic_frames/frame_uncommon",  new Vector4(112, 112, 112, 112) },
            { "relic_frames/frame_rare",      new Vector4(112, 112, 112, 112) },
            { "relic_frames/frame_legendary", new Vector4(112, 112, 112, 112) },
            { "buttons/tab_selected",   new Vector4(28, 24, 28, 24) },
            { "buttons/tab_unselected", new Vector4(28, 24, 28, 24) },
            { "bars/progress_track_empty",    new Vector4(48, 14, 48, 14) },
            { "bars/scrollbar_thumb",         new Vector4(8, 14, 8, 14) },
            { "bars/scrollbar_track_thin",    new Vector4(8, 12, 8, 12) },
        };

        static Vector4 ButtonBorder = new Vector4(22, 20, 22, 20);

        void OnPreprocessTexture()
        {
            string p = assetPath;
            if (!p.StartsWith(Root) || !p.EndsWith(".png")) return;
            string key = p.Substring(Root.Length, p.Length - Root.Length - 4);

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100f;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.isReadable = false;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;   // wajib untuk sprite yang di-slice
            s.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(s);

            Vector4 b;
            if (Borders.TryGetValue(key, out b)) ti.spriteBorder = b;
            else if (key.StartsWith("buttons/btn_")) ti.spriteBorder = ButtonBorder;
            else ti.spriteBorder = Vector4.zero;
        }
    }
}
#endif