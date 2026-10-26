using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Finds the 2D collider under the mouse/finger and drives Hotspot hover + click.</summary>
public class LobbyHotspotInput : MonoBehaviour
{
    public LobbyCameraRig rig;
    public LobbyUI ui;

    Camera cam;
    Hotspot hovered;

    void Start() => cam = rig.GetComponent<Camera>();

    void Update()
    {
        Hotspot h = null;
        Collider2D hitCol = null;
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (!rig.InputLocked && !overUI)
        {
            Vector2 sp = GameInput.PointerPosition;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 10f));
            var col = Physics2D.OverlapPoint(world);
            if (col != null)
            {
                h = col.GetComponentInParent<Hotspot>();
                hitCol = col;
            }
        }

        if (h != hovered)
        {
            if (hovered != null) hovered.SetHovered(false);
            hovered = h;
            if (hovered != null) hovered.SetHovered(true);

            if (hovered != null && hitCol != null)
            {
                Bounds b = hitCol.bounds;
                ui.SetTooltip(hovered.Label, new Vector3(b.center.x, b.max.y, 0f));   // ribbon pointer on top of the object
            }
            else ui.SetTooltip("");
        }

        if (hovered != null && GameInput.PointerDown)
            hovered.Click();
    }
}