using System;
using UnityEngine;
using TMPro;

/// <summary>
/// 760x360 confirm dialog: Secondary (Stay) and Primary (Leave) buttons 280x72.
/// Call on an instance: confirmPanel.Show("Leave the voyage?", "Stay", "Leave", () => ...);
/// Get an instance with ConfirmPanel.GetOrCreate() if your scene has none.
/// Art slots: "Frame (replace sprite)", corner ornaments, and the two buttons.
/// </summary>
public class ConfirmPanel : MonoBehaviour
{
    public TMP_Text questionLabel;
    public ThemedButton stayButton;      // Secondary
    public ThemedButton confirmButton;   // Primary

    Action confirmCallback;
    Action stayCallback;
    bool wired;

    public bool IsOpen { get { return gameObject.activeSelf; } }

    void Awake()
    {
        Wire();
    }

    void Wire()
    {
        if (wired) return;
        if (stayButton == null || confirmButton == null) return; // fields not assigned yet
        wired = true;
        stayButton.onClick.AddListener(OnStayClicked);
        confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    public void Show(string question, string stayLabel, string confirmLabel, Action onConfirm, Action onStay = null)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Wire();
        if (questionLabel != null) questionLabel.text = question;
        if (stayButton != null) stayButton.SetLabel(stayLabel);
        if (confirmButton != null) confirmButton.SetLabel(confirmLabel);
        confirmCallback = onConfirm;
        stayCallback = onStay;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void OnStayClicked()
    {
        Action cb = stayCallback;
        confirmCallback = null;
        stayCallback = null;
        Hide();
        if (cb != null) cb();
    }

    void OnConfirmClicked()
    {
        Action cb = confirmCallback;
        confirmCallback = null;
        stayCallback = null;
        Hide();
        if (cb != null) cb();
    }

    // Finds a ConfirmPanel in the open scene (even inactive) or builds one on a new overlay canvas.
    public static ConfirmPanel GetOrCreate()
    {
        ConfirmPanel p = FindAnyObjectByType<ConfirmPanel>(FindObjectsInactive.Include);
        if (p != null) return p;
        Canvas c = UIKit.CreateOverlayCanvas("Confirm Canvas", 500);
        return UIKit.CreateConfirmPanel(c.transform);
    }
}