using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Almanac: character list on the left, details on the right (role, stamina, bio, trait, talent, 8 moves).
/// Not a MonoBehaviour. LobbyMenu creates it. Locked characters show as "???".
/// </summary>
public class AlmanacPanel
{
    static AlmanacPanel instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        LobbyMenu.AlmanacOpener = onClose =>
        {
            if (instance == null) instance = new AlmanacPanel();
            instance.Open(onClose);
        };
    }

    static bool IsUnlocked(CharacterData c)
    {
        return c.unlockedByDefault || SaveSystem.Data.unlockedCharacters.Contains(c.id);
    }

    GameObject root;
    Action onClose;
    List<CharacterData> chars;
    RectTransform detail;
    int selected;

    public void Open(Action closed)
    {
        Close();
        onClose = closed;
        if (SaveSystem.Data == null) SaveSystem.Load();
        chars = new List<CharacterData>(CharacterDatabase.All);
        selected = 0;

        Canvas canvas = UIKit.CreateOverlayCanvas("Almanac Canvas", 350);
        root = canvas.gameObject;
        Transform t = canvas.transform;

        Image dim = UIKit.NewImage("Dim", t, null, Image.Type.Simple, UITheme.Dim, true);
        UIKit.Stretch(dim.rectTransform);

        RectTransform panel = UIKit.NewRect("Panel", t);
        ScreenKit.PutC(panel, 0f, 0f, 1560f, 900f);
        Image frame = UIKit.NewImage("Frame (replace sprite)", panel, "panel", Image.Type.Sliced, Color.white, true);
        UIKit.Stretch(frame.rectTransform);
        ScreenKit.Corners(panel, 26f);

        TMP_Text title = ScreenKit.Text(panel, "Title", "ALMANAC", UITextStyle.TitleCaps, UITheme.Cream, TextAlignmentOptions.Center, 48f);
        ScreenKit.Put(title, 300, 24, 960, 70);
        ThemedButton close = UIKit.CreateButton(panel, "CLOSE", ThemedButtonStyle.Primary, new Vector2(220f, 64f), UITextStyle.Small);
        ScreenKit.PutR(close, 40, 28, 220, 64);
        close.onClick.AddListener(Close);

        ScrollRect sr = ScreenKit.Scroll(panel, "Characters", 36, 120, 440, 740);
        VerticalLayoutGroup v = sr.content.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 10f;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        for (int i = 0; i < chars.Count; i++)
        {
            int idx = i;
            bool on = IsUnlocked(chars[i]);
            ThemedButton b = UIKit.CreateButton(sr.content, on ? chars[i].name : "???", ThemedButtonStyle.Secondary, new Vector2(380f, 64f), UITextStyle.Body);
            LayoutElement le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 64f;
            b.onClick.AddListener(() => { selected = idx; ShowDetail(); });
        }

        detail = UIKit.NewRect("Detail", panel);
        ScreenKit.Put(detail, 510, 120, 1010, 740);
        ShowDetail();
    }

    public void Close()
    {
        if (root != null) UnityEngine.Object.Destroy(root);
        root = null;
        Action cb = onClose;
        onClose = null;
        if (cb != null) cb();
    }

    static string Role(CharacterData c)
    {
        return c.role == null ? "" : c.role.ToString();
    }

    void ShowDetail()
    {
        ScreenKit.Clear(detail);
        if (chars == null || chars.Count == 0) return;
        CharacterData c = chars[Mathf.Clamp(selected, 0, chars.Count - 1)];
        if (!IsUnlocked(c))
        {
            TMP_Text lk = ScreenKit.Text(detail, "Locked", "???\n\n" + c.unlockConditionText, UITextStyle.Body, UITheme.BlueLight, TextAlignmentOptions.TopLeft, 30f);
            ScreenKit.Put(lk, 0, 0, 1010, 300);
            return;
        }

        TMP_Text name = ScreenKit.FitText(detail, "Name", c.name + "  -  " + c.title, UITextStyle.Subtitle, UITheme.Cream, TextAlignmentOptions.MidlineLeft, 40f);
        ScreenKit.Put(name, 0, 0, 1010, 56);
        TMP_Text meta = ScreenKit.Text(detail, "Meta", Role(c) + "     Stamina " + c.stamina, UITextStyle.Label, UITheme.Gold, TextAlignmentOptions.MidlineLeft, 26f);
        ScreenKit.Put(meta, 0, 58, 1010, 40);
        TMP_Text bio = ScreenKit.Text(detail, "Bio", c.bio, UITextStyle.Small, UITheme.Cream, TextAlignmentOptions.TopLeft, 24f);
        ScreenKit.Put(bio, 0, 104, 1010, 104);
        TMP_Text trait = ScreenKit.Text(detail, "Trait", c.traitName + ": " + c.traitText, UITextStyle.Small, UITheme.BlueLight, TextAlignmentOptions.TopLeft, 22f);
        ScreenKit.Put(trait, 0, 212, 1010, 62);
        TMP_Text talent = ScreenKit.Text(detail, "Talent", c.talentName + " (floor " + c.talentUnlockFloor + "): " + c.talentText, UITextStyle.Small, UITheme.BlueLight, TextAlignmentOptions.TopLeft, 22f);
        ScreenKit.Put(talent, 0, 278, 1010, 62);

        Image rule = ScreenKit.Box(detail, "Rule", UITheme.Bronze);
        ScreenKit.Put(rule, 0, 346, 1010, 2);
        TMP_Text mh = ScreenKit.Text(detail, "Moves Heading", "MOVES", UITextStyle.Label, UITheme.Gold, TextAlignmentOptions.MidlineLeft, 24f);
        ScreenKit.Put(mh, 0, 354, 400, 34);
        int n = c.moves == null ? 0 : c.moves.Length;
        for (int i = 0; i < n && i < 8; i++)
        {
            string line = c.moves[i].name + "  [" + c.moves[i].tag + "]  " + c.moves[i].description;
            TMP_Text m = ScreenKit.FitText(detail, "Move " + (i + 1), line, UITextStyle.Small, UITheme.Cream, TextAlignmentOptions.MidlineLeft, 22f);
            ScreenKit.Put(m, 0, 392 + i * 42, 1010, 40);
        }
    }
}
