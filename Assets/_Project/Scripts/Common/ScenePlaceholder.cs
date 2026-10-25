using UnityEngine;
using UnityEngine.UI;

/// <summary>Used by the empty Story / Dungeon scenes: just a Back-to-Lobby button.</summary>
public class ScenePlaceholder : MonoBehaviour
{
    public Button backButton;

    void Start()
    {
        GameInput.EnsureEventSystem();
        backButton.onClick.AddListener(() => SceneFader.LoadScene(GameScenes.Lobby));
    }
}
