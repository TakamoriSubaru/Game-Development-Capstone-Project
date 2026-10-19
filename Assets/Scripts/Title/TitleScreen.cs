using UnityEngine;
using UnityEngine.UI;

/// <summary>"Press any key / tap" screen. Goes to the Lobby.</summary>
public class TitleScreen : MonoBehaviour
{
    public Text prompt;
    bool going;

    void Update()
    {
        if (prompt != null)
        {
            var c = prompt.color;
            c.a = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.time * 3f));
            prompt.color = c;
        }

        if (!going && GameInput.AnyKeyDown)
        {
            going = true;
            SceneFader.LoadScene(GameScenes.Lobby, 0.8f);
        }
    }
}
