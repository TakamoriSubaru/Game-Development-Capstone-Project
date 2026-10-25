using System.Collections;
using UnityEngine;

/// <summary>
/// LEFT stop. The window is the Story Mode button.
/// Locked (GameState.StoryUnlocked = false): padlock shown, window shakes on click.
/// Unlocked: bird on the sill hops on hover; on click it flies out into the window and shrinks into the
/// distance, the camera zooms in and follows it, then the Story scene loads.
/// </summary>
public class StoryWindowHotspot : Hotspot
{
    public LobbyCameraRig rig;
    public Transform bird, flyTarget, wingL, wingR;
    public GameObject lockIcon;

    public override string Label => GameState.StoryUnlocked ? "Story Mode" : "Story Mode  [Locked - next update]";

    Vector3 birdHome, windowHome, birdScale;
    bool busy;

    void Awake()
    {
        birdHome = bird.localPosition;
        birdScale = bird.localScale;
        windowHome = transform.localPosition;
        if (lockIcon != null) lockIcon.SetActive(!GameState.StoryUnlocked);
    }

    void Update()
    {
        if (busy || !GameState.StoryUnlocked) return;
        float speed = IsHovered ? 12f : 1.5f;
        float amp = IsHovered ? 0.25f : 0.02f;
        bird.localPosition = birdHome + Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * speed)) * amp;
        float flutter = IsHovered ? Mathf.Sin(Time.time * 25f) * 15f : 0f;
        if (wingL != null) wingL.localRotation = Quaternion.Euler(0, 0, flutter);
        if (wingR != null) wingR.localRotation = Quaternion.Euler(0, 0, flutter * 0.7f);
    }

    public override void Click()
    {
        if (busy) return;
        busy = true;
        if (!GameState.StoryUnlocked) StartCoroutine(Shake());
        else StartCoroutine(FlyAway());
    }

    IEnumerator Shake()
    {
        for (float t = 0; t < 0.35f; t += Time.deltaTime)
        {
            transform.localPosition = windowHome + Vector3.right * Mathf.Sin(t * 70f) * 0.12f;
            yield return null;
        }
        transform.localPosition = windowHome;
        busy = false;
    }

    IEnumerator FlyAway()
    {
        GameState.LastLobbyStop = rig.Index;
        rig.Focus(bird, 2.4f);

        Vector3 p0 = bird.position, p2 = flyTarget.position;
        Vector3 p1 = new Vector3((p0.x + p2.x) * 0.5f + 1.5f, (p0.y + p2.y) * 0.5f + 1.8f, p0.z);
        float facing = p2.x >= p0.x ? 1f : -1f;
        const float duration = 2.2f;
        float t = 0f;
        bool loading = false;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            bird.position = (1 - e) * (1 - e) * p0 + 2 * (1 - e) * e * p1 + e * e * p2;
            float s = Mathf.Lerp(1f, 0.12f, e);                         // shrinks = flying away from us
            bird.localScale = new Vector3(birdScale.x * s * facing, birdScale.y * s, birdScale.z);

            float flap = Mathf.Sin(Time.time * 30f) * 40f;
            if (wingL != null) wingL.localRotation = Quaternion.Euler(0, 0, flap);
            if (wingR != null) wingR.localRotation = Quaternion.Euler(0, 0, flap * 0.7f);

            if (!loading && t > 0.7f)
            {
                loading = true;
                SceneFader.LoadScene(GameScenes.Story, 0.6f);
            }
            yield return null;
        }
    }
}
