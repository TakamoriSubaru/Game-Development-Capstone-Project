using UnityEngine;
using UnityEngine.SceneManagement;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Pasang di satu GameObject kosong di scene MainMenu. Seluruh UI dibuat lewat kode.
    public class MainMenuController : MonoBehaviour
    {
        void Awake()
        {
            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.CreateCanvas("MenuCanvas");
            UIFactory.Panel(canvas, "Bg", Theme.Bg, 0f, 0f, 1f, 1f, false);
            UIFactory.Label(canvas, "ODDYSEUS", 72, Theme.Ink, TextAnchor.MiddleCenter, 0.1f, 0.62f, 0.9f, 0.88f);
            UIFactory.Label(canvas, "Journey's End", 32, Theme.Line, TextAnchor.MiddleCenter, 0.1f, 0.54f, 0.9f, 0.64f);

            string label = DungeonState.Active ? "Continue Dungeon Mode" : "Dungeon Mode";
            UIFactory.Btn(canvas, "DungeonBtn", label, Theme.Card, Theme.Ink, 0.36f, 0.36f, 0.64f, 0.46f,
                () => SceneManager.LoadScene(SceneNames.Dungeon), 28);
            UIFactory.Btn(canvas, "QuitBtn", "Quit", Theme.Card, Theme.Ink, 0.36f, 0.22f, 0.64f, 0.32f, Quit, 28);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
