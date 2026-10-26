using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Splash / title screen behaviour. Sits on the "Splash Canvas" object made by the menu
/// Oddyseus/Build Splash Screen (Title scene).
///  - ONLY a mouse click or a screen tap starts the game (keyboard keys do nothing)
///  - the prompt pulses so it is easy to see
///  - fades to black, then opens the Lobby scene
/// Self-contained: it does not need the UI Kit.
/// </summary>
public class SplashTapToStart : MonoBehaviour, IPointerClickHandler
{
    [Header("Scene to open (empty = GameScenes.Lobby)")]
    public string nextScene = "";

    [Header("Parts (set by the builder)")]
    public CanvasGroup promptGroup;
    public Image fade;
    public Image artOverlay;

    [Header("Optional ship art behind everything")]
    public bool showArt = false;
    [Range(0f, 1f)] public float artOpacity = 0.18f;

    [Header("Timing")]
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.45f;
    public float pulseSpeed = 2.2f;
    public float inputGrace = 0.4f;   // ignore clicks right after the scene opens

    bool starting;
    float startTime;

    void OnValidate()
    {
        ApplyArt();
    }

    void Start()
    {
        startTime = Time.unscaledTime;
        ApplyArt();
        if (fade != null) StartCoroutine(FadeIn());
    }

    void ApplyArt()
    {
        if (artOverlay == null) return;
        artOverlay.enabled = showArt && artOverlay.sprite != null;
        Color c = artOverlay.color;
        c.a = artOpacity;
        artOverlay.color = c;
    }

    void Update()
    {
        if (starting || promptGroup == null) return;
        promptGroup.alpha = 0.82f + 0.18f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed));
    }

    // Mouse click and touch, anywhere on the splash art.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.unscaledTime - startTime < inputGrace) return;
        Begin();
    }

    public void Begin()
    {
        if (starting) return;
        starting = true;
        if (promptGroup != null) promptGroup.alpha = 1f;
        StartCoroutine(FadeOutAndLoad());
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        SetFade(1f);
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            SetFade(1f - Mathf.Clamp01(t / fadeInTime));
            yield return null;
        }
        SetFade(0f);
    }

    IEnumerator FadeOutAndLoad()
    {
        float t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            SetFade(Mathf.Clamp01(t / fadeOutTime));
            yield return null;
        }
        SetFade(1f);
        if (string.IsNullOrEmpty(nextScene)) SceneManager.LoadScene(GameScenes.Lobby);
        else SceneManager.LoadScene(nextScene);
    }

    void SetFade(float a)
    {
        if (fade == null) return;
        Color c = fade.color;
        c.a = a;
        fade.color = c;
    }
}