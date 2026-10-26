using System;
using UnityEngine;

/// <summary>
/// 900x640 settings panel: Music and SFX sliders (default 70%) and a BACK button.
/// Call on an instance: settingsPanel.Open(() => ...);
/// Get an instance with SettingsPanel.GetOrCreate() if your scene has none.
/// Values are saved through GameSettings (PlayerPrefs) and applied by AudioManager.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    public ThemedSlider musicSlider;
    public ThemedSlider sfxSlider;
    public ThemedButton backButton;

    Action closeCallback;
    bool wired;

    public bool IsOpen { get { return gameObject.activeSelf; } }

    void Awake()
    {
        Wire();
    }

    void Wire()
    {
        if (wired) return;
        if (musicSlider == null || sfxSlider == null || backButton == null) return; // fields not assigned yet
        wired = true;
        musicSlider.OnValueChanged += OnMusicChanged;
        sfxSlider.OnValueChanged += OnSfxChanged;
        backButton.onClick.AddListener(Close);
    }

    void OnMusicChanged(float v) { GameSettings.MusicVolume = v; }
    void OnSfxChanged(float v) { GameSettings.SfxVolume = v; }

    public void Open(Action onClose = null)
    {
        closeCallback = onClose;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Wire();
        if (musicSlider != null) musicSlider.Value = GameSettings.MusicVolume;
        if (sfxSlider != null) sfxSlider.Value = GameSettings.SfxVolume;
    }

    public void Close()
    {
        GameSettings.Save();
        gameObject.SetActive(false);
        Action cb = closeCallback;
        closeCallback = null;
        if (cb != null) cb();
    }

    public static SettingsPanel GetOrCreate()
    {
        SettingsPanel p = FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        if (p != null) return p;
        Canvas c = UIKit.CreateOverlayCanvas("Settings Canvas", 400);
        return UIKit.CreateSettingsPanel(c.transform);
    }
}
