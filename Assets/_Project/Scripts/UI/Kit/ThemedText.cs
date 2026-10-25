using UnityEngine;
using TMPro;

/// <summary>
/// Put on any TMP text that uses a theme style. At runtime it re-applies the Cinzel / Noto Serif
/// font, so prefabs built before the fonts were installed still show the right font.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class ThemedText : MonoBehaviour
{
    public UITextStyle style = UITextStyle.Body;

    void Awake()
    {
        UITheme.ApplyFont(GetComponent<TMP_Text>(), style);
    }
}
