using UnityEngine;
using TMPro;

/// <summary>
/// 340x64 ribbon with a 20 px pointer, shown above a world point (e.g. a hotspot).
/// The root pivot is the tip of the pointer, so the tip lands exactly on the point.
/// Art slots: "Plate (replace sprite)", "Pointer (replace sprite)".
/// </summary>
public class HoverRibbon : MonoBehaviour
{
    public TMP_Text label;
    public Camera worldCamera;      // empty = Camera.main
    public float lift = 6f;         // extra pixels above the point

    public void Show(string text, Vector3 worldPos)
    {
        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        Vector2 screen = cam != null ? (Vector2)cam.WorldToScreenPoint(worldPos) : new Vector2(worldPos.x, worldPos.y);
        ShowAtScreen(text, screen);
    }

    // Same as Show, but with a screen position in pixels.
    public void ShowAtScreen(string text, Vector2 screenPos)
    {
        if (label != null) label.text = text;
        gameObject.SetActive(true);

        RectTransform rt = (RectTransform)transform;
        RectTransform parent = rt.parent as RectTransform;
        if (parent == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera uiCam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) uiCam = canvas.worldCamera;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, uiCam, out local);

        Rect r = parent.rect;
        float halfW = rt.rect.width * 0.5f;
        local.x = Mathf.Clamp(local.x, r.xMin + halfW, r.xMax - halfW);
        local.y = Mathf.Clamp(local.y + lift, r.yMin, r.yMax - rt.rect.height);
        rt.localPosition = new Vector3(local.x, local.y, 0f);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
