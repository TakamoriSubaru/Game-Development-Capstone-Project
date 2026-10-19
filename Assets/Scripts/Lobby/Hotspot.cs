using UnityEngine;

/// <summary>
/// Base class for clickable things in the Lobby. Put a subclass on a GameObject that has a Collider2D
/// (or whose children do). Hover/click is driven by LobbyHotspotInput.
/// </summary>
public abstract class Hotspot : MonoBehaviour
{
    public bool IsHovered { get; private set; }
    public virtual string Label => gameObject.name;

    public void SetHovered(bool value)
    {
        if (value == IsHovered) return;
        IsHovered = value;
        if (value) OnHoverEnter(); else OnHoverExit();
    }

    protected virtual void OnHoverEnter() { }
    protected virtual void OnHoverExit() { }
    public abstract void Click();
}
