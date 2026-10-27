#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Put this file in any folder named "Editor" (for example Assets/_Project/Scripts/Editor/).
/// Every PNG in a Resources/UIKit folder whose name starts with "title_" is imported as
/// Sprite (2D and UI), uncompressed, Alpha Is Transparency, and readable (so TitleScreen can auto-trim the margins).
/// After adding the script: right-click the UIKit folder -> Reimport.
/// </summary>
public class TitleArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string p = assetPath.Replace('\\', '/');
        if (!p.Contains("/Resources/UIKit/")) return;
        string file = System.IO.Path.GetFileNameWithoutExtension(p);
        if (!file.StartsWith("title_")) return;

        TextureImporter ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.maxTextureSize = 4096;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.isReadable = file != "title_bg";   // title_bg is large and never trimmed
        ti.filterMode = FilterMode.Bilinear;
    }
}
#endif