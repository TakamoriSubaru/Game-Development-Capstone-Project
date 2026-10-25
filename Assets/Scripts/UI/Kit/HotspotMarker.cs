using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 48x48 "!" marker. SetPulsing(true) makes the glow breathe.
/// Art slots: "Marker (replace sprite)", "Pulse Glow (replace sprite)".
/// </summary>
public class HotspotMarker : MonoBehaviour
{
    public Image marker;
    public Image pulseGlow;
    public bool pulsing;

    public void SetPulsing(bool on)
    {
        pulsing = on;
        if (!on) ResetLook();
    }

    void OnEnable()
    {
        if (!pulsing) ResetLook();
    }

    void ResetLook()
    {
        if (marker != null) marker.rectTransform.localScale = Vector3.one;
        if (pulseGlow != null)
        {
            Color c = pulseGlow.color;
            c.a = 0f;
            pulseGlow.color = c;
            pulseGlow.rectTransform.localScale = Vector3.one;
        }
    }

    void Update()
    {
        if (!pulsing) return;
        float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
        if (marker != null) marker.rectTransform.localScale = Vector3.one * (1f + 0.08f * k);
        if (pulseGlow != null)
        {
            Color c = pulseGlow.color;
            c.a = 0.25f + 0.55f * k;
            pulseGlow.color = c;
            pulseGlow.rectTransform.localScale = Vector3.one * (0.9f + 0.35f * k);
        }
    }
}
