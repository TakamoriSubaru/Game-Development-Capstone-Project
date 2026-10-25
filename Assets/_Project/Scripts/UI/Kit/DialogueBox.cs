using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 1400x220 dialogue box with a 320x56 name plate, typewriter text and a blinking 28x28 chevron.
/// Click the box (or call Advance() from your keyboard code):
///  - while typing: shows the whole line at once
///  - when the line is complete: raises OnContinue
/// Art slots: "Frame (replace sprite)", "NamePlate (replace sprite)", "Continue Chevron (replace sprite)".
/// </summary>
public class DialogueBox : MonoBehaviour, IPointerClickHandler
{
    public TMP_Text nameLabel;
    public TMP_Text bodyLabel;
    public GameObject namePlate;
    public Image continueChevron;
    public float charsPerSecond = 45f;

    public event Action OnContinue;
    public bool IsTyping { get; private set; }

    float revealed;
    int total;

    public void Show(string speaker, string line)
    {
        gameObject.SetActive(true);

        bool hasName = !string.IsNullOrEmpty(speaker);
        if (namePlate != null) namePlate.SetActive(hasName);
        if (nameLabel != null) nameLabel.text = speaker ?? "";

        bodyLabel.text = line ?? "";
        bodyLabel.maxVisibleCharacters = 0;
        bodyLabel.ForceMeshUpdate();
        total = bodyLabel.textInfo.characterCount;

        revealed = 0f;
        IsTyping = total > 0 && charsPerSecond > 0f;
        if (!IsTyping) bodyLabel.maxVisibleCharacters = 99999;
        SetChevronAlpha(0f);
    }

    public void Hide()
    {
        IsTyping = false;
        gameObject.SetActive(false);
    }

    // Same as clicking the box. Hook your Space / Enter key to this.
    public void Advance()
    {
        if (IsTyping)
        {
            Finish();
            return;
        }
        if (OnContinue != null) OnContinue();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Advance();
    }

    void Finish()
    {
        IsTyping = false;
        if (bodyLabel != null) bodyLabel.maxVisibleCharacters = 99999;
    }

    void Update()
    {
        if (IsTyping)
        {
            revealed += Time.unscaledDeltaTime * charsPerSecond;
            int n = Mathf.FloorToInt(revealed);
            bodyLabel.maxVisibleCharacters = n;
            if (n >= total) Finish();
            SetChevronAlpha(0f);
        }
        else if (continueChevron != null)
        {
            float a = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f));
            SetChevronAlpha(a);
        }
    }

    void SetChevronAlpha(float a)
    {
        if (continueChevron == null) return;
        Color c = continueChevron.color;
        c.a = a;
        continueChevron.color = c;
    }
}
