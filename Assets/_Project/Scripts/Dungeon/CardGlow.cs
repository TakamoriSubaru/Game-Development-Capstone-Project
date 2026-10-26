using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Put on a card that has a child object named "Glow".
/// The Glow fades in on hover, or stays on when pinned (selected card).
/// </summary>
public class CardGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image glow;
    public Color baseColor = new Color(0.95f, 0.78f, 0.43f, 1f);
    public float max = 0.85f;
    public bool pinned;

    bool hover;
    float cur;

    public void SetPinned(bool value) { pinned = value; }
    public void OnPointerEnter(PointerEventData e) { hover = true; }
    public void OnPointerExit(PointerEventData e) { hover = false; }

    void OnDisable() { hover = false; }

    void Update()
    {
        if (glow == null) return;
        float target = (hover || pinned) ? 1f : 0f;
        cur = Mathf.MoveTowards(cur, target, Time.unscaledDeltaTime * 8f);
        Color c = baseColor;
        c.a = cur * max;
        glow.color = c;
    }
}
