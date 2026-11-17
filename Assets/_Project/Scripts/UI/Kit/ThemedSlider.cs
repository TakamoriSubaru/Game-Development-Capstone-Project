using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gold slider. Default track 640x16, handle 24x24, fill #F4D27A, track #2B3B56.
/// Art slots: "Track (replace sprite)", "Fill (replace sprite)", "Knob (replace sprite)".
/// </summary>
public class ThemedSlider : MonoBehaviour
{
    public Slider slider;
    public RectTransform track;
    public RectTransform handleArea;
    public RectTransform knob;
    public TMP_Text valueLabel;     // optional "70%" label
    public RectTransform fillArea;  // set by UIKit; needed to fit the painted fill inside the painted track

    // True when the painted pa_slider_track / pa_slider_fill sprites are used (set by UIKit).
    // Painted track is 42 px tall (its natural size), painted fill 14 px tall, inset 11 px from both ends.
    public bool paintedArt;
    public const float PaintedTrackHeight = 42f;
    public const float PaintedFillHeight = 14f;
    public const float PaintedFillInset = 11f;

    float knobSize = 0f;            // > 0 = keep the knob exactly this square size

    public event Action<float> OnValueChanged;

    bool wired;

    void Awake()
    {
        Init();
    }

    void OnEnable()
    {
        Init();
    }

    // Safe to call many times. UIKit calls it after creating the slider from code.
    public void Init()
    {
        Wire();
        if (slider != null) Refresh(slider.value);
    }

    void Wire()
    {
        if (wired || slider == null) return;
        wired = true;
        slider.onValueChanged.AddListener(Handle);
    }

    void Handle(float v)
    {
        Refresh(v);
        if (OnValueChanged != null) OnValueChanged(v);
    }

    void Refresh(float v)
    {
        if (valueLabel != null) valueLabel.text = Mathf.RoundToInt(v * 100f) + "%";
    }

    // 0..1. Does not raise OnValueChanged.
    public float Value
    {
        get { return slider != null ? slider.value : 0f; }
        set
        {
            if (slider == null) return;
            Wire();
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            Refresh(slider.value);
        }
    }

    public void SetSizes(float width, float trackHeight, float knobSize)
    {
        this.knobSize = knobSize;
        RectTransform root = (RectTransform)transform;
        float height = Mathf.Max(knobSize + 8f, 32f);
        root.sizeDelta = new Vector2(width, height);
        if (paintedArt) trackHeight = PaintedTrackHeight;
        if (track != null) track.sizeDelta = new Vector2(0f, trackHeight);
        if (paintedArt && fillArea != null)
        {
            fillArea.offsetMin = new Vector2(PaintedFillInset, -PaintedFillHeight * 0.5f);
            fillArea.offsetMax = new Vector2(-PaintedFillInset, PaintedFillHeight * 0.5f);
        }
        if (handleArea != null)
        {
            handleArea.offsetMin = new Vector2(knobSize * 0.5f, 0f);
            handleArea.offsetMax = new Vector2(-knobSize * 0.5f, 0f);
        }
        LockKnob();
    }

    // The knob must always be a perfect square and vertically centred, or it looks squashed.
    void LateUpdate()
    {
        LockKnob();
    }

    void LockKnob()
    {
        if (knob == null) return;
        float size = knobSize > 0f ? knobSize : knob.sizeDelta.x;
        if (size <= 0f) return;
        if (knob.anchorMin.y != 0.5f || knob.anchorMax.y != 0.5f)
        {
            knob.anchorMin = new Vector2(knob.anchorMin.x, 0.5f);
            knob.anchorMax = new Vector2(knob.anchorMax.x, 0.5f);
        }
        if (knob.pivot.x != 0.5f || knob.pivot.y != 0.5f) knob.pivot = new Vector2(0.5f, 0.5f);
        if (knob.sizeDelta.x != size || knob.sizeDelta.y != size) knob.sizeDelta = new Vector2(size, size);
        if (knob.anchoredPosition.y != 0f) knob.anchoredPosition = new Vector2(knob.anchoredPosition.x, 0f);
        Image img = knob.GetComponent<Image>();
        if (img != null && !img.preserveAspect) img.preserveAspect = true;
    }
}
