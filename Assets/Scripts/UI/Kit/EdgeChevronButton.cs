using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 280x84 button with a chevron on one side. Left version: "<  DUNGEON". Right version: "STORY  >".
/// Art slot: child "Chevron Icon (replace sprite)".
/// </summary>
public class EdgeChevronButton : ThemedButton
{
    public Image chevron;
    public bool pointsRight = true;

    public void Setup(string label, bool pointsRight, UnityAction onClick)
    {
        this.pointsRight = pointsRight;
        SetLabel(label);

        if (chevron != null)
        {
            RectTransform rt = chevron.rectTransform;
            rt.anchorMin = new Vector2(pointsRight ? 1f : 0f, 0.5f);
            rt.anchorMax = new Vector2(pointsRight ? 1f : 0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(pointsRight ? -40f : 40f, 0f);
            rt.localScale = new Vector3(pointsRight ? 1f : -1f, 1f, 1f);
        }

        this.onClick.RemoveAllListeners();
        if (onClick != null) this.onClick.AddListener(onClick);
    }
}
