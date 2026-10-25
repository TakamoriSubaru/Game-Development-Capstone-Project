using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public enum ThemedButtonStyle
{
    Primary,     // gold: navy with gold border, fills gold on hover
    Secondary,   // navy with blue border, fills blue on hover
    Edge,        // navy with gold border, white-blue border + blue glow on hover
    ChoiceBlue,  // event choice rows (20_event_quiet_cove)
    ChoiceRed,
    ChoiceGreen
}

/// <summary>
/// Themed button. Art slots:
///  - child "Frame (replace sprite)" = normal sprite
///  - Hover / Pressed / Disabled sprites = the Sprite Swap slots on this component
///  - child "Glow" fades in on hover
/// </summary>
public class ThemedButton : Button
{
    public ThemedButtonStyle style = ThemedButtonStyle.Primary;
    public Image frame;
    public Image glow;
    public TMP_Text label;

    public Color labelNormal;
    public Color labelHover;
    public Color labelPressed;
    public Color labelDisabled;
    public Color glowColor;
    public float glowMax = 0.8f;

    bool lockVisual;
    float glowTarget;
    float glowCurrent;

    public void SetLabel(string text)
    {
        if (label != null) label.text = text;
    }

    public void SetStyle(ThemedButtonStyle newStyle)
    {
        style = newStyle;
        ApplyStyle();
    }

    // Sets sprites, colors and sprite-swap for the current style.
    public void ApplyStyle()
    {
        string n, h, p;
        switch (style)
        {
            case ThemedButtonStyle.Secondary:
                n = "btn_secondary"; h = "btn_secondary_hover"; p = "btn_secondary_pressed";
                labelNormal = UITheme.Cream; labelHover = Color.white; labelPressed = UITheme.BlueLight;
                glowColor = UITheme.Hex("7FC4FF");
                break;
            case ThemedButtonStyle.Edge:
                n = "btn_edge"; h = "btn_edge_hover"; p = "btn_edge_hover";
                labelNormal = UITheme.Cream; labelHover = Color.white; labelPressed = Color.white;
                glowColor = UITheme.Hex("7FC4FF");
                break;
            case ThemedButtonStyle.ChoiceBlue:
                n = "choice_blue"; h = "choice_blue_hover"; p = "choice_blue_hover";
                labelNormal = UITheme.Cream; labelHover = Color.white; labelPressed = Color.white;
                glowColor = UITheme.Gold;
                break;
            case ThemedButtonStyle.ChoiceRed:
                n = "choice_red"; h = "choice_red_hover"; p = "choice_red_hover";
                labelNormal = UITheme.Cream; labelHover = Color.white; labelPressed = Color.white;
                glowColor = UITheme.Gold;
                break;
            case ThemedButtonStyle.ChoiceGreen:
                n = "choice_green"; h = "choice_green_hover"; p = "choice_green_hover";
                labelNormal = UITheme.Cream; labelHover = Color.white; labelPressed = Color.white;
                glowColor = UITheme.Gold;
                break;
            default:
                n = "btn_primary"; h = "btn_primary_hover"; p = "btn_primary_pressed";
                labelNormal = UITheme.Gold; labelHover = UITheme.TextDark; labelPressed = UITheme.TextDark;
                glowColor = UITheme.Gold;
                break;
        }
        labelDisabled = UITheme.Disabled;

        if (frame != null)
        {
            frame.sprite = UIArt.Get(n);
            frame.type = Image.Type.Sliced;
            frame.color = Color.white;
            targetGraphic = frame;
        }

        transition = Transition.SpriteSwap;
        SpriteState ss = new SpriteState();
        ss.highlightedSprite = UIArt.Get(h);
        ss.selectedSprite = UIArt.Get(h);
        ss.pressedSprite = UIArt.Get(p);
        ss.disabledSprite = UIArt.Get("btn_disabled");
        spriteState = ss;

        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        navigation = nav;

        if (glow != null)
        {
            Color g = glowColor;
            g.a = 0f;
            glow.color = g;
        }
        if (label != null) label.color = labelNormal;
    }

    // For the gallery: freeze the button in one look. 0 normal, 1 hover, 2 pressed, 3 disabled.
    public void ShowState(int state)
    {
        lockVisual = true;
        if (frame != null)
        {
            Sprite s = null;
            if (state == 1) s = spriteState.highlightedSprite;
            else if (state == 2) s = spriteState.pressedSprite;
            else if (state == 3) s = spriteState.disabledSprite;
            frame.overrideSprite = s;
        }
        if (label != null)
        {
            Color c = labelNormal;
            if (state == 1) c = labelHover;
            else if (state == 2) c = labelPressed;
            else if (state == 3) c = labelDisabled;
            label.color = c;
        }
        glowTarget = state == 1 ? 1f : (state == 2 ? 0.6f : 0f);
        glowCurrent = glowTarget;
        UpdateGlow();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        glowCurrent = glowTarget;
        UpdateGlow();
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        if (lockVisual) return;
        base.DoStateTransition(state, instant);

        Color lc = labelNormal;
        float g = 0f;
        switch (state)
        {
            case SelectionState.Highlighted:
            case SelectionState.Selected:
                lc = labelHover; g = 1f; break;
            case SelectionState.Pressed:
                lc = labelPressed; g = 0.6f; break;
            case SelectionState.Disabled:
                lc = labelDisabled; break;
        }
        if (label != null) label.color = lc;
        glowTarget = g;
        if (instant) glowCurrent = g;
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        // Do not stay "selected" after a mouse click, otherwise the hover look sticks.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void Update()
    {
        if (glow == null) return;
        if (!Mathf.Approximately(glowCurrent, glowTarget))
            glowCurrent = Mathf.MoveTowards(glowCurrent, glowTarget, Time.unscaledDeltaTime * 8f);
        UpdateGlow();
    }

    void UpdateGlow()
    {
        if (glow == null) return;
        Color c = glowColor;
        c.a = glowCurrent * glowMax;
        glow.color = c;
    }
}
