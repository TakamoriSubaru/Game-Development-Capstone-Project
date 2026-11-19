using UnityEngine;
using UnityEngine.UI;

namespace Oddyseus.UI
{
    // Repeats a horizontal strip (Greek key) at its real height so the pattern is not stretched.
    [RequireComponent(typeof(RawImage))]
    public class TileStrip : MonoBehaviour
    {
        RawImage raw;
        float aspect = 16f;

        public void Setup(Sprite sprite)
        {
            raw = GetComponent<RawImage>();
            var tex = sprite.texture;
            tex.wrapMode = TextureWrapMode.Repeat;
            raw.texture = tex;
            aspect = (float)tex.width / Mathf.Max(1, tex.height);
            Apply();
        }

        void OnRectTransformDimensionsChange() { Apply(); }

        void Apply()
        {
            if (raw == null) return;
            Rect r = ((RectTransform)transform).rect;
            if (r.height < 1f) return;
            raw.uvRect = new Rect(0f, 0f, r.width / (r.height * aspect), 1f);
        }
    }
}
