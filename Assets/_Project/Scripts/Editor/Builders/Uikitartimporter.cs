#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Auto-configures every PNG named pa_*.png in a Resources/UIKit folder so the painted UI art
/// works with the UI Kit: Sprite (2D and UI), no compression, correct 9-slice borders and PPU.
/// Border values are in source pixels (left, bottom, right, top). With the kit's canvas
/// (reference 100 PPU) the on-screen border = border * 100 / PPU.
/// To retune: edit the table below, then right-click the UIKit folder > Reimport.
/// </summary>
public class UIKitArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string p = assetPath.Replace('\\', '/');
        if (!p.Contains("/Resources/UIKit/")) return;
        string n = System.IO.Path.GetFileNameWithoutExtension(p);
        if (!n.StartsWith("pa_")) return;

        TextureImporter ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.isReadable = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.maxTextureSize = 2048;

        float ppu = 100f;
        Vector4 border = Vector4.zero;

        if (n == "pa_panel")          { ppu = 250f; border = new Vector4(110, 110, 110, 110); }
        else if (n == "pa_dialogue")  { ppu = 250f; border = new Vector4(130, 130, 130, 130); }
        else if (n == "pa_plate")     { ppu = 200f; border = new Vector4(60, 40, 60, 40); }
        else if (n.StartsWith("pa_btn_")) { ppu = 200f; border = new Vector4(64, 44, 64, 44); }
        else if (n == "pa_slider")    { ppu = 200f; border = new Vector4(40, 0, 40, 0); }

        ti.spritePixelsPerUnit = ppu;
        ti.spriteBorder = border;
    }

    [MenuItem("Oddyseus/Reimport UI Kit Art")]
    static void ReimportAll()
    {
        string[] guids = AssetDatabase.FindAssets("pa_ t:Texture2D");
        int count = 0;
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            if (!path.Replace('\\', '/').Contains("/Resources/UIKit/")) continue;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            count++;
        }
        Debug.Log("[UIKitArtImporter] reimported " + count + " pa_ sprites");
    }
}
#endif