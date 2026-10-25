using UnityEngine;

/// <summary>
/// Tiny static helper for the collection screens (AI-4).
/// Other scenes open the Character Detail screen with:
///     CollectionNav.OpenCharacterDetail("odysseus", true, GameScenes.Voyage);
/// </summary>
public static class CollectionNav
{
    // Scene name of the Character Detail screen. GameScenes has no constant for it in the contract,
    // so it lives here. (See REQUESTS TO OTHER AI: AI-2 may add GameScenes.CharacterDetail = "CharacterDetail".)
    public const string CharacterDetailScene = "CharacterDetail";

    // Set before loading the Character Detail scene.
    public static string DetailCharId;
    public static bool DetailFromRun;
    public static string DetailReturnScene = GameScenes.Legends;

    public static void OpenCharacterDetail(string charId, bool fromRun, string returnScene = null)
    {
        DetailCharId = charId;
        DetailFromRun = fromRun;
        DetailReturnScene = string.IsNullOrEmpty(returnScene) ? GameScenes.Legends : returnScene;
        SceneFader.LoadScene(CharacterDetailScene);
    }

    // BACK from Legends, Armory, Pack Compendium and Achievements goes to the Dungeon hub.
    public static void Back()
    {
        SceneFader.LoadScene(GameScenes.DungeonHub);
    }

    // BACK from Character Detail goes to where it was opened from.
    public static void BackFromDetail()
    {
        string scene = string.IsNullOrEmpty(DetailReturnScene) ? GameScenes.Legends : DetailReturnScene;
        SceneFader.LoadScene(scene);
    }
}
