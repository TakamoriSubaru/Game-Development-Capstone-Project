using UnityEngine;
using UnityEngine.UI;

/// <summary>Lobby HUD: left/right arrows, current stop label, hover tooltip, character speech box.</summary>
public class LobbyUI : MonoBehaviour
{
    public LobbyCameraRig rig;
    public Button leftButton, rightButton;
    public Text stopLabel, tooltip;
    public GameObject speechPanel;
    public Text speakerText, lineText;

    Canvas canvas;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        speechPanel.SetActive(false);
        tooltip.text = "";
    }

    void OnEnable() => rig.StopChanged += OnStop;
    void OnDisable() => rig.StopChanged -= OnStop;

    void Start()
    {
        GameInput.EnsureEventSystem();
        leftButton.onClick.AddListener(() => rig.Step(-1));
        rightButton.onClick.AddListener(() => rig.Step(1));
    }

    void Update()
    {
        bool free = !rig.InputLocked;
        leftButton.gameObject.SetActive(free && rig.Index > 0);
        rightButton.gameObject.SetActive(free && rig.Index < LobbyCameraRig.StopCount - 1);
        stopLabel.gameObject.SetActive(free);

        if (tooltip.text.Length > 0)
            tooltip.rectTransform.anchoredPosition =
                GameInput.PointerPosition / canvas.scaleFactor + new Vector2(24f, -10f);
    }

    void OnStop(int i) => stopLabel.text = LobbyCameraRig.StopNames[i];

    public void SetTooltip(string text) => tooltip.text = text;

    public void ShowSpeech(string speaker, string line)
    {
        speechPanel.SetActive(true);
        speakerText.text = speaker;
        lineText.text = line;
    }

    public void SetSpeechLine(string line) => lineText.text = line;
    public void HideSpeech() => speechPanel.SetActive(false);
}
