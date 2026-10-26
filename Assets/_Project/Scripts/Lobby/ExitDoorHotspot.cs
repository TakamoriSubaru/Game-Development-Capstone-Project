using System.Collections;
using UnityEngine;

/// <summary>
/// LAST stop. The door is the Exit button. Starts closed, cracks open a little on hover,
/// swings fully open on click, then the app quits (stops play mode in the Editor).
/// The door "opens" by squeezing the leaf's width toward the hinge (doorPivot scale X).
/// </summary>
public class ExitDoorHotspot : Hotspot
{
    public LobbyCameraRig rig;
    public Transform doorPivot;           // hinge; the door leaf is its child
    [Range(0f, 1f)] public float hoverOpen = 0.2f;
    public float fullOpenScaleX = 0.08f;

    public override string Label => "Exit";

    float current, target;
    bool opened;

    void Update()
    {
        current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-(opened ? 5f : 9f) * Time.deltaTime));
        var s = doorPivot.localScale;
        s.x = Mathf.Lerp(1f, fullOpenScaleX, current);
        doorPivot.localScale = s;
    }

    protected override void OnHoverEnter() { if (!opened) target = hoverOpen; }
    protected override void OnHoverExit()  { if (!opened) target = 0f; }

    public override void Click()
    {
        if (opened) return;
        LobbyUI ui = FindAnyObjectByType<LobbyUI>();
        if (ui == null) { Leave(); return; }
        rig.InputLocked = true;            // freeze the camera while the question is open
        ui.AskLeave(Leave, Stay);
    }

    void Leave()
    {
        opened = true;
        target = 1f;
        rig.InputLocked = true;
        StartCoroutine(QuitRoutine());
    }

    void Stay()
    {
        rig.InputLocked = false;
    }

    IEnumerator QuitRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        yield return SceneFader.Instance.Fade(1f, 0.6f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}