using UnityEngine;

public enum MenuAction { Almanac, Settings }

/// <summary>
/// A clickable object in the lobby that opens a menu screen (e.g. the BOOK opens the Almanac,
/// the COMPASS opens Settings).
///
/// FOR THE ARTIST - how to make one look good:
///   1. Swap the sprite(s) of the object for your art (keep the Box Collider 2D around it).
///   2. Put your glow art on the child object named "Glow" (any sprite, or several, or a particle
///      effect). The script hides it, then fades it in when the mouse is over the object and fades it
///      out again when the mouse leaves. Nothing else to set up.
/// </summary>
public class MenuObjectHotspot : Hotspot
{
    public MenuAction action;
    public string label = "";               // hover tooltip; empty = uses the action name
    public GameObject glow;                  // the hover glow (child object)
    public float fadeSpeed = 8f;             // higher = glow appears faster
    [Range(0f, 0.2f)] public float hoverPop = 0.04f;   // object grows this much while hovered (0 = off)

    public override string Label => string.IsNullOrEmpty(label) ? action.ToString() : label;

    SpriteRenderer[] glowRenderers = new SpriteRenderer[0];
    float[] glowAlpha = new float[0];
    Vector3 baseScale;
    float t;

    void Awake()
    {
        baseScale = transform.localScale;
        if (glow != null)
        {
            glowRenderers = glow.GetComponentsInChildren<SpriteRenderer>(true);
            glowAlpha = new float[glowRenderers.Length];
            for (int i = 0; i < glowRenderers.Length; i++) glowAlpha[i] = glowRenderers[i].color.a;
        }
        SetGlow(0f);
    }

    void Update()
    {
        t = Mathf.MoveTowards(t, IsHovered ? 1f : 0f, Time.deltaTime * fadeSpeed);
        SetGlow(t);
        transform.localScale = baseScale * (1f + hoverPop * t);
    }

    void SetGlow(float k)
    {
        if (glow == null) return;
        for (int i = 0; i < glowRenderers.Length; i++)
        {
            var c = glowRenderers[i].color;
            c.a = glowAlpha[i] * k;
            glowRenderers[i].color = c;
        }
        bool want = k > 0.001f;
        if (glow.activeSelf != want) glow.SetActive(want);
    }

    public override void Click()
    {
        LobbyUI ui = FindFirstObjectByType<LobbyUI>();
        if (action == MenuAction.Settings && ui != null) { ui.OpenSettings(); return; }
        LobbyMenu.Open(action);
    }
}