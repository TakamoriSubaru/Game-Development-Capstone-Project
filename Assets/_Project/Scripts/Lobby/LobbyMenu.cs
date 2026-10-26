using System;
using UnityEngine;

// MenuAction (Almanac, Settings) is defined in MenuObjectHotspot.cs.

/// <summary>
/// Opens the two lobby menus from the lobby objects (MenuObjectHotspot):
///   Settings (the ship's wheel)  - the Kit SettingsPanel.
///   Almanac  (the scroll barrel) - AlmanacPanel: every character with role, stamina, bio and moves.
/// While a menu is open the lobby camera input is locked. Created by LobbyUI.Start().
/// </summary>
public class LobbyMenu : MonoBehaviour
{
    public static LobbyMenu Instance { get; private set; }

    /// <summary>
    /// Set by AlmanacPanel.cs when that file is in the project. If you delete AlmanacPanel.cs
    /// (for example while the character data classes do not exist yet), the barrel simply does nothing.
    /// </summary>
    public static Action<Action> AlmanacOpener;

    LobbyCameraRig rig;
    bool open;

    /// <summary>Called by the wheel / barrel objects in the lobby.</summary>
    public static void Open(MenuAction action)
    {
        if (Instance != null) Instance.Show(action);
    }

    public static LobbyMenu Create(LobbyCameraRig rig)
    {
        if (Instance != null) { Instance.rig = rig; return Instance; }
        GameObject go = new GameObject("Lobby Menu");
        LobbyMenu m = go.AddComponent<LobbyMenu>();
        m.rig = rig;
        Instance = m;
        return m;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Show(MenuAction action)
    {
        if (open) return;
        SetLocked(true);
        if (action == MenuAction.Settings)
        {
            SettingsPanel.GetOrCreate().Open(() => SetLocked(false));
        }
        else if (AlmanacOpener != null)
        {
            AlmanacOpener(() => SetLocked(false));
        }
        else
        {
            Debug.Log("Almanac is not installed (AlmanacPanel.cs missing).");
            SetLocked(false);
        }
    }

    void SetLocked(bool on)
    {
        open = on;
        if (rig != null) rig.InputLocked = on;
    }
}
