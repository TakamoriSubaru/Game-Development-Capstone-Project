using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The two lobby menu screens, opened by clicking objects in the lobby (MenuObjectHotspot):
///   Almanac  (the book)    - every character with their role, stamina, bio and moves.
///   Settings (the compass) - volume, fullscreen (PC only), companion chatter on/off.
/// It builds ALL of its own screens in code (like SceneFader). If the Lobby scene has no menu objects yet,
/// it shows two plain buttons top-right, at the Lobby stop only, so you can still get in.
/// Created by LobbyUI.Start().
/// </summary>
public class LobbyMenu : MonoBehaviour
{
    enum Page { None, Settings, Almanac }

    static readonly Color Gold = new Color(1f, .85f, .4f);
    static readonly Color BoxColor = new Color(.11f, .08f, .06f, .97f);
    static readonly Color ListNormal = new Color(0f, 0f, 0f, .45f);
    static readonly Color ListSelected = new Color(.6f, .42f, .08f, .95f);
    static readonly Vector2 Mid = new Vector2(.5f, .5f);
    static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    static readonly Vector2 TopMid = new Vector2(.5f, 1f);
    static readonly Vector2 TopRight = new Vector2(1f, 1f);
    static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
    static readonly Vector2 BottomMid = new Vector2(.5f, 0f);

    const float CardHeight = 100f, CardGap = 10f;

    LobbyCameraRig rig;
    Page page = Page.None;
    bool lockedByMe;

    GameObject[] fallbackButtons = new GameObject[0];
    RectTransform settingsPage, almanacPage;

    // almanac
    Button[] listButtons;
    Text nameText, subText, bioText;
    RectTransform skillContent;
    ScrollRect skillScroll;

    public static LobbyMenu Instance { get; private set; }

    /// <summary>Called by the book / compass objects in the lobby.</summary>
    public static void Open(MenuAction action)
    {
        if (Instance != null)
            Instance.Show(action == MenuAction.Almanac ? Page.Almanac : Page.Settings);
    }

    public static LobbyMenu Create(LobbyCameraRig rig)
    {
        var canvas = UIFactory.CreateCanvas("MenuCanvas");
        canvas.sortingOrder = 50;                         // above the lobby HUD, below the scene fader (999)
        var menu = canvas.gameObject.AddComponent<LobbyMenu>();
        menu.rig = rig;
        Instance = menu;
        menu.Build(canvas.transform);
        return menu;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        bool show = page == Page.None && !rig.InputLocked && rig.Index == LobbyCameraRig.FrontStop;   // Lobby stop only
        for (int i = 0; i < fallbackButtons.Length; i++)
            fallbackButtons[i].SetActive(show);
    }

    // ------------------------------------------------------------ open / close

    void Show(Page p)
    {
        page = p;
        settingsPage.gameObject.SetActive(p == Page.Settings);
        almanacPage.gameObject.SetActive(p == Page.Almanac);

        bool open = p != Page.None;
        if (open && !lockedByMe && !rig.InputLocked)
        {
            rig.InputLocked = true;           // freezes camera arrows / hotspots while a menu is open
            lockedByMe = true;
        }
        if (!open && lockedByMe)
        {
            rig.InputLocked = false;
            lockedByMe = false;
            GameSettings.Save();
        }
    }

    // ------------------------------------------------------------ build

    void Build(Transform root)
    {
        // Normal case: the book + compass objects exist in the lobby, nothing to show here.
        // Fallback: scene has no menu objects yet, so show plain buttons.
        if (FindAnyObjectByType<TableSilhouettes>() == null)
            Debug.LogWarning("Oddyseus: this Lobby scene is the OLD version (no top-down table / Book / Cog). " +
                             "Use the top menu Oddyseus > Update Lobby (top-down table + menu objects).");

        if (FindAnyObjectByType<MenuObjectHotspot>() == null)
        {
            var a = MakeButton(root, "Almanac", 40, TopRight, new Vector2(-30f, -30f), new Vector2(240f, 90f));
            a.onClick.AddListener(() => Show(Page.Almanac));
            var st = MakeButton(root, "Settings", 40, TopRight, new Vector2(-30f, -135f), new Vector2(240f, 90f));
            st.onClick.AddListener(() => Show(Page.Settings));
            fallbackButtons = new[] { a.gameObject, st.gameObject };
        }

        BuildSettingsPage(root);
        BuildAlmanacPage(root);
    }

    void BuildSettingsPage(Transform root)
    {
        settingsPage = MakePage(root, "SettingsPage");
        var box = MakeBox(settingsPage, new Vector2(1000f, 640f), "SETTINGS");
        float y = -160f;

        AddLabel(box, "Volume", y);
        var slider = MakeSlider(box, GameSettings.Volume, v => GameSettings.Volume = v);
        UIFactory.Anchor((RectTransform)slider.transform, TopRight, TopRight, TopRight, new Vector2(-60f, y), new Vector2(440f, 60f));
        y -= 110f;

        if (!Application.isMobilePlatform)       // phones are always fullscreen
        {
            AddLabel(box, "Fullscreen", y);
            var fs = MakeSwitch(box, Screen.fullScreen, v => GameSettings.Fullscreen = v);
            UIFactory.Anchor((RectTransform)fs.transform, TopRight, TopRight, TopRight, new Vector2(-60f, y), new Vector2(200f, 70f));
            y -= 110f;
        }

        AddLabel(box, "Companion chatter", y);
        var chat = MakeSwitch(box, GameSettings.CompanionChatter, v => GameSettings.CompanionChatter = v);
        UIFactory.Anchor((RectTransform)chat.transform, TopRight, TopRight, TopRight, new Vector2(-60f, y), new Vector2(200f, 70f));

        MakeButton(box, "Close", 44, BottomMid, new Vector2(0f, 40f), new Vector2(360f, 90f)).onClick.AddListener(() => Show(Page.None));
    }

    void BuildAlmanacPage(Transform root)
    {
        almanacPage = MakePage(root, "AlmanacPage");
        var box = MakeBox(almanacPage, new Vector2(1600f, 920f), "ALMANAC");

        // ---- left: character list
        var chars = CharacterDatabase.All;
        listButtons = new Button[chars.Length];
        for (int i = 0; i < chars.Length; i++)
        {
            int index = i;
            var b = MakeButton(box, chars[i].name, 36, TopLeft, new Vector2(40f, -120f - i * 96f), new Vector2(380f, 84f));
            b.onClick.AddListener(() => Select(index));
            listButtons[i] = b;
        }
        MakeButton(box, "Close", 40, BottomLeft, new Vector2(40f, 40f), new Vector2(380f, 84f)).onClick.AddListener(() => Show(Page.None));

        // ---- right: details
        var details = new GameObject("Details", typeof(RectTransform)).GetComponent<RectTransform>();
        details.SetParent(box, false);
        UIFactory.Anchor(details, TopLeft, TopLeft, TopLeft, new Vector2(460f, -120f), new Vector2(1100f, 780f));

        nameText = UIFactory.CreateText(details, "Name", "", 64, TextAnchor.UpperLeft, Gold);
        UIFactory.Anchor(nameText.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(0f, 0f), new Vector2(1100f, 80f));

        subText = UIFactory.CreateText(details, "Sub", "", 34, TextAnchor.UpperLeft, new Color(.75f, .85f, 1f));
        UIFactory.Anchor(subText.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(0f, -80f), new Vector2(1100f, 50f));

        bioText = UIFactory.CreateText(details, "Bio", "", 30, TextAnchor.UpperLeft, Color.white);
        UIFactory.Anchor(bioText.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(0f, -135f), new Vector2(1100f, 120f));

        var header = UIFactory.CreateText(details, "MovesHeader", "MOVES", 40, TextAnchor.UpperLeft, Gold);
        UIFactory.Anchor(header.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(0f, -265f), new Vector2(1100f, 55f));

        // scrollable list of moves (scrolls only if a character has more moves than fit)
        var sgo = new GameObject("Moves", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
        sgo.transform.SetParent(details, false);
        UIFactory.Anchor((RectTransform)sgo.transform, TopLeft, TopLeft, TopLeft, new Vector2(0f, -325f), new Vector2(1100f, 455f));
        sgo.GetComponent<Image>().color = Color.clear;          // invisible, but catches drags / mouse wheel

        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(sgo.transform, false);
        UIFactory.Anchor(content, TopLeft, TopRight, TopMid, Vector2.zero, Vector2.zero);
        skillContent = content;

        skillScroll = sgo.GetComponent<ScrollRect>();
        skillScroll.content = content;
        skillScroll.horizontal = false;
        skillScroll.vertical = true;
        skillScroll.movementType = ScrollRect.MovementType.Clamped;
        skillScroll.scrollSensitivity = 40f;

        Select(0);   // starts on Odysseus
    }

    void Select(int i)
    {
        var c = CharacterDatabase.All[i];
        nameText.text = c.name.ToUpper();
        subText.text = c.title + "   |   " + CharacterDatabase.RoleLabel(c.role) + "   |   Stamina: " + c.stamina;
        bioText.text = c.bio;

        for (int k = 0; k < listButtons.Length; k++)
            listButtons[k].GetComponent<Image>().color = k == i ? ListSelected : ListNormal;

        for (int k = skillContent.childCount - 1; k >= 0; k--)
            Destroy(skillContent.GetChild(k).gameObject);

        int count = c.skills != null ? c.skills.Length : 0;
        for (int k = 0; k < count; k++)
        {
            var s = c.skills[k];
            var card = UIFactory.CreatePanel(skillContent, "Skill_" + s.name, new Color(1f, 1f, 1f, .08f));
            UIFactory.Anchor(card.rectTransform, TopLeft, TopRight, TopMid, new Vector2(0f, -k * (CardHeight + CardGap)), new Vector2(0f, CardHeight));

            var n = UIFactory.CreateText(card.transform, "Name", s.name, 34, TextAnchor.UpperLeft, Gold);
            UIFactory.Anchor(n.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(20f, -8f), new Vector2(700f, 44f));

            var t = UIFactory.CreateText(card.transform, "Tag", s.tag, 28, TextAnchor.UpperRight, new Color(.7f, .85f, 1f));
            UIFactory.Anchor(t.rectTransform, TopRight, TopRight, TopRight, new Vector2(-20f, -10f), new Vector2(300f, 40f));

            var d = UIFactory.CreateText(card.transform, "Description", s.description, 26, TextAnchor.UpperLeft, Color.white);
            UIFactory.Anchor(d.rectTransform, Vector2.zero, Vector2.one, Mid, Vector2.zero, Vector2.zero);
            d.rectTransform.offsetMin = new Vector2(20f, 6f);
            d.rectTransform.offsetMax = new Vector2(-20f, -52f);
        }

        skillContent.sizeDelta = new Vector2(0f, count * (CardHeight + CardGap));
        skillContent.anchoredPosition = Vector2.zero;
    }

    // ------------------------------------------------------------ small UI helpers

    RectTransform MakePage(Transform root, string name)
    {
        var img = UIFactory.CreatePanel(root, name, new Color(0f, 0f, 0f, .72f));
        img.raycastTarget = true;     // blocks clicks from reaching the lobby behind it
        UIFactory.Anchor(img.rectTransform, Vector2.zero, Vector2.one, Mid, Vector2.zero, Vector2.zero);
        img.gameObject.SetActive(false);
        return img.rectTransform;
    }

    RectTransform MakeBox(Transform page, Vector2 size, string title)
    {
        var box = UIFactory.CreatePanel(page, "Box", BoxColor);
        UIFactory.Anchor(box.rectTransform, Mid, Mid, Mid, Vector2.zero, size);
        var t = UIFactory.CreateText(box.transform, "Title", title, 60, TextAnchor.MiddleCenter, Gold);
        UIFactory.Anchor(t.rectTransform, TopMid, TopMid, TopMid, new Vector2(0f, -20f), new Vector2(size.x - 80f, 80f));
        return box.rectTransform;
    }

    Button MakeButton(Transform parent, string label, int fontSize, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var b = UIFactory.CreateButton(parent, label + "Button", label, fontSize);
        b.targetGraphic = b.GetComponent<Image>();      // lets the button tint on hover / press
        UIFactory.Anchor((RectTransform)b.transform, anchor, anchor, anchor, pos, size);
        return b;
    }

    void AddLabel(Transform parent, string text, float y)
    {
        var t = UIFactory.CreateText(parent, text + "Label", text, 42, TextAnchor.MiddleLeft, Color.white);
        UIFactory.Anchor(t.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(60f, y), new Vector2(480f, 70f));
    }

    /// <summary>A button that flips between ON and OFF.</summary>
    Button MakeSwitch(Transform parent, bool initial, Action<bool> onChange)
    {
        var b = UIFactory.CreateButton(parent, "Switch", "", 38);
        b.targetGraphic = b.GetComponent<Image>();
        var img = b.GetComponent<Image>();
        var label = b.GetComponentInChildren<Text>();
        bool on = initial;

        Action refresh = () =>
        {
            label.text = on ? "ON" : "OFF";
            img.color = on ? new Color(.2f, .5f, .2f, .9f) : new Color(.5f, .15f, .15f, .9f);
        };
        refresh();

        b.onClick.AddListener(() =>
        {
            on = !on;
            refresh();
            onChange(on);
        });
        return b;
    }

    /// <summary>A 0..1 slider built from plain Images (track, fill, handle).</summary>
    Slider MakeSlider(Transform parent, float value, Action<float> onChange)
    {
        var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);

        var track = UIFactory.CreatePanel(go.transform, "Track", new Color(1f, 1f, 1f, .25f));
        track.raycastTarget = true;
        UIFactory.Anchor(track.rectTransform, new Vector2(0f, .5f), new Vector2(1f, .5f), Mid, Vector2.zero, new Vector2(0f, 14f));

        var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(go.transform, false);
        UIFactory.Anchor(fillArea, new Vector2(0f, .5f), new Vector2(1f, .5f), Mid, Vector2.zero, new Vector2(-40f, 14f));
        var fill = UIFactory.CreatePanel(fillArea, "Fill", Gold);
        UIFactory.Anchor(fill.rectTransform, Vector2.zero, new Vector2(0f, 1f), Mid, Vector2.zero, Vector2.zero);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(go.transform, false);
        UIFactory.Anchor(handleArea, Vector2.zero, Vector2.one, Mid, Vector2.zero, new Vector2(-40f, 0f));
        var handle = UIFactory.CreatePanel(handleArea, "Handle", Color.white);
        handle.raycastTarget = true;
        UIFactory.Anchor(handle.rectTransform, Vector2.zero, new Vector2(0f, 1f), Mid, Vector2.zero, new Vector2(40f, 0f));

        var s = go.GetComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = value;                                    // set BEFORE adding the listener
        s.onValueChanged.AddListener(v => onChange(v));
        return s;
    }
}
