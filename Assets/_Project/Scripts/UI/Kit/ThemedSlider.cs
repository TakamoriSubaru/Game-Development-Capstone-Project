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
        RectTransform root = (RectTransform)transform;
        float height = Mathf.Max(knobSize + 8f, 32f);
        root.sizeDelta = new Vector2(width, height);
        if (track != null) track.sizeDelta = new Vector2(0f, trackHeight);
        if (knob != null) knob.sizeDelta = new Vector2(knobSize, knobSize);
        if (handleArea != null)
        {
            handleArea.offsetMin = new Vector2(knobSize * 0.5f, 0f);
            handleArea.offsetMax = new Vector2(-knobSize * 0.5f, 0f);
        }
    }
}
