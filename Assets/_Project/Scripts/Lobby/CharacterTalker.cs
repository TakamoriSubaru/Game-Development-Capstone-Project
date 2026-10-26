using System.Collections;
using UnityEngine;

/// <summary>
/// The lobby character (Polites). Talks through the Kit DialogueBox (typewriter) when the camera
/// arrives at the Lobby stop (if Companion Chatter is on) and again when clicked.
/// Plug in your Animator: the script sets bool "Talking".
/// Without an Animator, a small body bob plays while the line types, and a slow breathing bob when idle.
/// Clicking the dialogue box once the line is finished closes it.
/// </summary>
public class CharacterTalker : Hotspot
{
    public LobbyCameraRig rig;
    public LobbyUI ui;
    public Transform model;      // whole character art (placeholder bob)
    public Transform mouth;      // optional, placeholder mouth that opens/closes
    public Animator animator;
    public string speakerName = "Polites";
    public float idleSeconds = 6f;   // how long a finished line stays before it closes by itself
    [TextArea] public string[] lines =
    {
        "A quiet day by the fire. I could get used to this.",
        "Pick a direction. The window, the map, or the door.",
        "Ithaca is still far. But every journey starts at a table like this one.",
        "The crew is restless. Shall we look at the charts?"
    };

    public override string Label => speakerName;

    bool talking;
    int lineIndex;
    Coroutine routine;
    Vector3 modelHome, mouthScale = Vector3.one;

    void Awake()
    {
        lineIndex = 0;
        if (model != null) modelHome = model.localPosition;
        if (mouth != null) mouthScale = mouth.localScale;
    }

    void OnEnable()
    {
        if (rig != null) rig.StopChanged += OnStop;
        if (ui != null) ui.SpeechContinued += OnContinue;
    }

    void OnDisable()
    {
        if (rig != null) rig.StopChanged -= OnStop;
        if (ui != null) ui.SpeechContinued -= OnContinue;
    }

    void OnStop(int index)
    {
        if (index == LobbyCameraRig.FrontStop)
        {
            if (GameSettings.CompanionChatter) Say();
        }
        else StopTalking();
    }

    void OnContinue()
    {
        if (routine != null) StopTalking();
    }

    public override void Click() => Say();

    public void Say()
    {
        if (lines == null || lines.Length == 0) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(SayRoutine(lines[lineIndex++ % lines.Length]));
    }

    IEnumerator SayRoutine(string line)
    {
        SetTalking(true);
        ui.ShowSpeech(speakerName, line);
        yield return null;
        while (ui.IsSpeechTyping) yield return null;
        SetTalking(false);
        float t = 0f;
        while (t < idleSeconds && ui.IsSpeechOpen)
        {
            t += Time.deltaTime;
            yield return null;
        }
        ui.HideSpeech();
        routine = null;
    }

    void StopTalking()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        SetTalking(false);
        ui.HideSpeech();
    }

    void SetTalking(bool value)
    {
        talking = value;
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetBool("Talking", value);
        if (!value && mouth != null) mouth.localScale = mouthScale;
    }

    void Update()
    {
        bool placeholder = animator == null || animator.runtimeAnimatorController == null;
        if (!placeholder || model == null) return;
        if (talking)
        {
            float s = Mathf.Abs(Mathf.Sin(Time.time * 14f));
            if (mouth != null) mouth.localScale = new Vector3(mouthScale.x, mouthScale.y * (0.4f + 2.2f * s), 1f);
            model.localPosition = modelHome + Vector3.up * 0.04f * s;
        }
        else
        {
            model.localPosition = modelHome + Vector3.up * 0.015f * (0.5f + 0.5f * Mathf.Sin(Time.time * 1.6f));
        }
    }
}
