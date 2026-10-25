using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Top bar for full screens (Legends, Armory, ...): BACK button on the left, title in the middle,
/// Greek-key border strip along the bottom edge.
/// Art slots: "Greek Key Strip (replace sprite)", plus the BACK button art.
/// </summary>
public class ScreenHeader : MonoBehaviour
{
    public ThemedButton backButton;
    public TMP_Text titleLabel;

    public void Setup(string title, UnityAction onBack)
    {
        if (titleLabel != null) titleLabel.text = title;
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            if (onBack != null) backButton.onClick.AddListener(onBack);
        }
    }
}
