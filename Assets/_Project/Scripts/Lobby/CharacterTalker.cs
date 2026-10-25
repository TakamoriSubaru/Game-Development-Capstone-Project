using System.Collections;
using UnityEngine;

/// <summary>
/// The lobby character. Talks (typewriter text + talking animation) when the camera arrives at the
/// Lobby stop, and again when clicked. Plug in your Animator: the script sets bool "Talking".
/// Without an Animator, a placeholder mouth/body bob plays so you can see it "speak".
/// </summary>
public class CharacterTalker : Hotspot
{
    public LobbyCameraRig rig;
    public LobbyUI ui;
    public Transform model;      // whole character (placeholder bob)
    public Transform mouth;      // optional, placeholder mouth that opens/closes
    public Animator animator;
    public string speakerName = "Companion";
    [TextArea] public string[] lines =
    {
        "Welcome back, Odysseus. The sea is calling again.",
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
        lineIndex = Random.Range(0, lines.Length);
        if (model != null) modelHome = model.localPosition;
        if (mouth != null) mouthScale = mouth.localScale;
    }

    void OnEnable() => rig.StopChanged += OnStop;
    void OnDisable() => rig.StopChanged -= OnStop;

    void OnStop(int index)
    {
        if (index == LobbyCameraRig.FrontStop) Say();
        else StopTalking();
    }

    public override void Click() => Say();

    public void Say()
    {
        if (lines.Length == 0) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(SayRoutine(lines[lineIndex++ % lines.Length]));
    }

    IEnumerator SayRoutine(string line)
    {
        SetTalking(true);
        ui.ShowSpeech(speakerName, "");
        for (int i = 1; i <= line.Length; i++)
        {
            ui.SetSpeechLine(line.Substring(0, i));
            yield return new WaitForSeconds(0.03f);
        }
        SetTalking(false);
        yield return new WaitForSeconds(4f);
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
        if (!value)
        {
            if (model != null) model.localPosition = modelHome;
            if (mouth != null) mouth.localScale = mouthScale;
        }
    }

    void Update()
    {
        bool placeholder = animator == null || animator.runtimeAnimatorController == null;
        if (!talking || !placeholder) return;
        float s = Mathf.Abs(Mathf.Sin(Time.time * 14f));
        if (mouth != null) mouth.localScale = new Vector3(mouthScale.x, mouthScale.y * (0.4f + 2.2f * s), 1f);
        if (model != null) model.localPosition = modelHome + Vector3.up * 0.05f * s;
    }
}
