/// <summary>Tiny static state that survives scene loads.</summary>
public static class GameState
{
    /// <summary>Flip to true in the next update to open Story Mode.</summary>
    public static bool StoryUnlocked = false;

    /// <summary>Which lobby stop to return to (0 Window, 1 Lobby, 2 Table, 3 Door).</summary>
    public static int LastLobbyStop = 1;
}
