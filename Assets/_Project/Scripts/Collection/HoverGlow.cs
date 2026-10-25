using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Fades the child object named "Glow" in when the pointer is over this object.
/// Put on a card or tile. Art slot: the "Glow" child (replace its sprite).
/// SetHold(x) keeps a minimum glow level (used for the selected card).
/// </summary>
public class HoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image glow;
    public Color color = new Color(0.95f, 0.78f, 0.43f, 1f);
    public float maxAlpha = 0.85f;

    float hold;       // 0..1 minimum level
    float hover;      // 0 or 1
    float current;

    public void SetHold(float level)
    {
        hold = Mathf.Clamp01(level);
    }

    public void SetColor(Color c)
    {
        color = c;
    }

    void OnEnable()
    {
        hover = 0f;
        current = hold;
        Apply();
    }

    public void OnPointerEnter(PointerEventData e) { hover = 1f; }
    public void OnPointerExit(PointerEventData e) { hover = 0f; }

    void Update()
    {
        if (glow == null) return;
        float target = Mathf.Max(hover, hold);
        if (!Mathf.Approximately(current, target))
            current = Mathf.MoveTowards(current, target, Time.unscaledDeltaTime * 8f);
        Apply();
    }

    void Apply()
    {
        if (glow == null) return;
        Color c = color;
        c.a = current * maxAlpha;
        glow.color = c;
    }
}
