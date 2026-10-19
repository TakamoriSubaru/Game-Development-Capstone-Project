using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oddyseus.UI
{
    // Tanpa setup di Unity: saat scene "Dungeon" (GameScenes.Dungeon) dimuat,
    // placeholder dimatikan dan DungeonController dibuat lewat kode.
    public static class DungeonBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (s.name != GameScenes.Dungeon) return;
            if (Object.FindAnyObjectByType<DungeonController>() != null) return;

            foreach (var ph in Object.FindObjectsByType<ScenePlaceholder>(FindObjectsSortMode.None))
                ph.gameObject.SetActive(false);
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                c.gameObject.SetActive(false);

            new GameObject("DungeonController").AddComponent<DungeonController>();
        }
    }
}
