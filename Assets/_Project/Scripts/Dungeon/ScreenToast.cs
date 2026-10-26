using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Small message at the bottom of the screen. Created by ScreenKit.NewScreen.</summary>
public class ScreenToast : MonoBehaviour
{
    GameObject box;
    TMP_Text label;
    float until;

    public void Build(Transform parent)
    {
        Image bg = UIKit.NewImage("Toast", parent, "plate", Image.Type.Sliced, Color.white, false);
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 110f);
        rt.sizeDelta = new Vector2(980f, 84f);
        label = ScreenKit.Text(bg.transform, "Label", "", UITextStyle.Small, UITheme.Cream, TextAlignmentOptions.Center);
        UIKit.Stretch(label.rectTransform);
        Canvas cv = bg.gameObject.AddComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingOrder = 300;
        box = bg.gameObject;
        box.SetActive(false);
    }

    public void Show(string msg)
    {
        if (box == null) return;
        label.text = msg;
        box.SetActive(true);
        box.transform.SetAsLastSibling();
        until = Time.unscaledTime + 2.4f;
    }

    void Update()
    {
        if (box != null && box.activeSelf && Time.unscaledTime > until) box.SetActive(false);
    }
}
