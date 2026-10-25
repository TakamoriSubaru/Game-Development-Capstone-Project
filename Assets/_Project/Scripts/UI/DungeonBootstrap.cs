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

            // PENTING: hanya sentuh objek di scene Dungeon itu sendiri (s).
            // Jangan pakai FindObjectsByType<Canvas>() global: itu ikut menemukan Canvas milik
            // SceneFader (DontDestroyOnLoad). Mematikannya menghentikan coroutine fade dan membuat
            // SceneFader.loading macet true, sehingga SceneFader.LoadScene() berikutnya (Exit / akhir run)
            // diam-diam tidak melakukan apa pun.
            foreach (var root in s.GetRootGameObjects())
            {
                foreach (var ph in root.GetComponentsInChildren<ScenePlaceholder>(true))
                    ph.enabled = false;
                foreach (var c in root.GetComponentsInChildren<Canvas>(true))
                    c.gameObject.SetActive(false);
            }

            new GameObject("DungeonController").AddComponent<DungeonController>();
        }
    }
}
