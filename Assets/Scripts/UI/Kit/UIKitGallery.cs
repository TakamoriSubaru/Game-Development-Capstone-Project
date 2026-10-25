using UnityEngine;
using TMPro;

/// <summary>
/// Runtime wiring for the UI Kit Gallery scene (made by Oddyseus/Build UI Kit Gallery).
/// Frozen buttons show forced states (hover / pressed); live ones react to the mouse.
/// </summary>
public class UIKitGallery : MonoBehaviour
{
    [Header("Frozen state previews")]
    public ThemedButton[] previewButtons;
    public int[] previewStates;       // 0 normal, 1 hover, 2 pressed, 3 disabled

    [Header("Dialogue")]
    public DialogueBox dialogue;
    public ThemedButton replayButton;

    [Header("Compass and markers")]
    public CompassIndicator compass;
    public ThemedButton[] compassButtons;
    public HotspotMarker pulsingMarker;
    public HotspotMarker normalMarker;
    public HoverRibbon liveRibbon;
    public ThemedButton ribbonButton;

    [Header("Modals")]
    public ThemedButton openConfirmButton;
    public ThemedButton openSettingsButton;
    public ConfirmPanel confirmPanel;
    public SettingsPanel settingsPanel;
    public TMP_Text statusLabel;

    static readonly string[] Lines =
    {
        "The sea is quiet tonight, captain. Too quiet. Even the gulls have left us to our thoughts.",
        "Press the box while the words are still appearing and the whole line shows at once.",
        "When the line is complete the chevron blinks. Press again to continue."
    };

    int lineIndex;
    bool ribbonVisible;

    void Start()
    {
        if (previewButtons != null && previewStates != null)
        {
            for (int i = 0; i < previewButtons.Length && i < previewStates.Length; i++)
                if (previewButtons[i] != null) previewButtons[i].ShowState(previewStates[i]);
        }

        if (pulsingMarker != null) pulsingMarker.SetPulsing(true);
        if (normalMarker != null) normalMarker.SetPulsing(false);

        if (dialogue != null)
        {
            dialogue.OnContinue += NextLine;
            ShowLine(0);
        }
        if (replayButton != null) replayButton.onClick.AddListener(() => ShowLine(0));

        if (compass != null) compass.SetActive(1);
        if (compassButtons != null)
        {
            for (int i = 0; i < compassButtons.Length; i++)
            {
                int stop = i;
                if (compassButtons[i] != null)
                    compassButtons[i].onClick.AddListener(() => compass.SetActive(stop));
            }
        }

        if (ribbonButton != null) ribbonButton.onClick.AddListener(ToggleRibbon);

        if (openConfirmButton != null)
            openConfirmButton.onClick.AddListener(() =>
                confirmPanel.Show("Leave the voyage?", "Stay", "Leave",
                                  () => SetStatus("ConfirmPanel: Leave pressed"),
                                  () => SetStatus("ConfirmPanel: Stay pressed")));

        if (openSettingsButton != null)
            openSettingsButton.onClick.AddListener(() =>
                settingsPanel.Open(() => SetStatus("SettingsPanel closed. Music " +
                    Mathf.RoundToInt(GameSettings.MusicVolume * 100f) + "%, SFX " +
                    Mathf.RoundToInt(GameSettings.SfxVolume * 100f) + "%")));
    }

    void ShowLine(int index)
    {
        lineIndex = index;
        dialogue.Show("Odysseus", Lines[lineIndex]);
    }

    void NextLine()
    {
        ShowLine((lineIndex + 1) % Lines.Length);
    }

    void ToggleRibbon()
    {
        if (liveRibbon == null || normalMarker == null) return;
        ribbonVisible = !ribbonVisible;
        if (ribbonVisible)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, normalMarker.transform.position);
            liveRibbon.ShowAtScreen("EXIT", screen + new Vector2(0f, 30f));
        }
        else
        {
            liveRibbon.Hide();
        }
    }

    void SetStatus(string text)
    {
        if (statusLabel != null) statusLabel.text = text;
    }
}
