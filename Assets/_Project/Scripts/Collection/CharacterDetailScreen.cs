using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Character Detail (concept 14, variant A "Night Sea and Bronze").
/// Left: Party list (only during a run) + Character List. Centre: portrait, role pill, stat bars,
/// "Remove from Party" (only when opened from a run). Right: tabs Moves / Passive.
/// Moves: 8 tiles, max 4 equipped (click toggles), details with Equip / Unequip. Saved by CollectionData (UnitData.Equipped in a run, MetaProgress otherwise).
/// Open it with CollectionNav.OpenCharacterDetail(charId, fromRun, returnScene).
/// Scene name: CollectionNav.CharacterDetailScene ("CharacterDetail").
/// </summary>
public class CharacterDetailScreen : CollectionScreenBase
{
    // If false, the first click on a move only selects it and the second click toggles it.
    public bool clickTogglesMove = true;

    // ----- left -----
    public RectTransform partyContent;
    public TMP_Text partyHint;
    public RectTransform charGridContent;
    public RectTransform partyRowTemplate;
    public RectTransform charThumbTemplate;

    // ----- centre -----
    public Image portraitImage;
    public TMP_Text nameLabel;
    public Image rolePill;
    public RectTransform[] statBars = new RectTransform[4];
    public TMP_Text[] statValues = new TMP_Text[4];
    public ThemedButton removeButton;

    // ----- right -----
    public ThemedButton tabMoves;
    public ThemedButton tabPassive;
    public TMP_Text equippedLabel;
    public RectTransform movesGroup;
    public RectTransform passiveGroup;
    public RectTransform moveGrid;
    public RectTransform moveTemplate;
    public Image detailIcon;
    public TMP_Text detailIconLetter;
    public TMP_Text detailName;
    public TMP_Text detailStatus;
    public TMP_Text detailBody;
    public TMP_Text detailTarget;
    public TMP_Text detailMessage;
    public ThemedButton equipButton;

    public TMP_Text traitName;
    public TMP_Text traitText;
    public TMP_Text talentName;
    public TMP_Text talentText;
    public Image talentPill;

    // ----- runtime state (not saved in the scene) -----
    CharacterData cur;
    List<string> equipped = new List<string>();
    int selectedMove;
    bool movesTab = true;
    float messageTimer;

    static readonly float[] StatCaps = { 160f, 40f, 30f, 130f };
    static readonly string[] StatNames = { "HP", "ATK", "DEF", "SPD" };
    static readonly Color BarGreen = new Color(0.48f, 0.79f, 0.44f, 1f);

    protected override string ScreenTitle { get { return "Character"; } }

    protected override void OnBack()
    {
        CollectionNav.BackFromDetail();
    }

    // =====================================================================
    // BUILD
    // =====================================================================
    protected override void BuildContent(Transform root)
    {
        BuildLeft(root);
        BuildCentre(root);
        BuildRight(root);
        partyRowTemplate = BuildPartyRowTemplate();
        charThumbTemplate = BuildThumbTemplate();
        moveTemplate = BuildMoveTemplate();
    }

    void BuildLeft(Transform root)
    {
        CollectionUI.Panel(root, "Party Frame (replace sprite)", 40f, 160f, 440f, 400f);
        CollectionUI.Text(root, "Party Title", "Party", UITextStyle.Label, UITheme.Cream,
            TextAlignmentOptions.MidlineLeft, 68f, 170f, 300f, 40f);
        partyHint = CollectionUI.Text(root, "Party Hint", "No active voyage.\nThe party shows up during a run.",
            UITextStyle.Small, UITheme.BlueLight, TextAlignmentOptions.Center, 64f, 260f, 384f, 120f);

        partyContent = UIKit.NewRect("Party List", root);
        CollectionUI.TL(partyContent, 56f, 218f, 408f, 330f);
        VerticalLayoutGroup v = partyContent.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8f;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = false;
        v.childControlHeight = false;
        v.childForceExpandWidth = false;
        v.childForceExpandHeight = false;

        CollectionUI.Panel(root, "Character List Frame (replace sprite)", 40f, 576f, 440f, 474f);
        CollectionUI.Text(root, "Character List Title", "Character List", UITextStyle.Label, UITheme.Cream,
            TextAlignmentOptions.MidlineLeft, 68f, 586f, 360f, 40f);
        CollectionUI.Scroll(root, "Character List Scroll", 52f, 634f, 416f, 404f, true, out charGridContent);
        GridLayoutGroup g = charGridContent.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(88f, 88f);
        g.spacing = new Vector2(8f, 8f);
        g.padding = new RectOffset(4, 4, 4, 4);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 4;
        g.childAlignment = TextAnchor.UpperLeft;
    }

    void BuildCentre(Transform root)
    {
        CollectionUI.Panel(root, "Portrait Frame (replace sprite)", 504f, 160f, 400f, 470f);
        portraitImage = UIKit.NewImage("Portrait (replace sprite)", root, null, Image.Type.Simple, UITheme.Gold, false);
        CollectionUI.TL(portraitImage.rectTransform, 524f, 180f, 360f, 430f);
        TMP_Text art = UIKit.CreateText(portraitImage.transform, "Art Label", "ART", UITextStyle.Subtitle, UITheme.Cream, TextAlignmentOptions.Center);
        UIKit.Stretch(art.rectTransform);

        nameLabel = CollectionUI.Text(root, "Character Name", "Name", UITextStyle.Subtitle, UITheme.Cream,
            TextAlignmentOptions.Center, 504f, 640f, 400f, 50f);
        CollectionUI.AutoSize(nameLabel, 22f, 36f);
        rolePill = CollectionUI.Pill(root, "Role Pill", 594f, 696f, 220f, 38f, "Role", UITheme.Gold, 22f);

        for (int i = 0; i < 4; i++)
        {
            float y = 756f + i * 48f;
            CollectionUI.Text(root, "Stat Label " + StatNames[i], StatNames[i], UITextStyle.Small, UITheme.Gold,
                TextAlignmentOptions.MidlineLeft, 520f, y, 70f, 36f);
            statBars[i] = CollectionUI.Bar(root, "Stat Bar " + StatNames[i], 596f, y + 11f, 190f, 14f, BarGreen);
            statValues[i] = CollectionUI.Text(root, "Stat Value " + StatNames[i], "0", UITextStyle.Small, UITheme.Cream,
                TextAlignmentOptions.MidlineRight, 794f, y, 100f, 36f);
        }

        removeButton = CollectionUI.NewButton(root, "Remove from Party", ThemedButtonStyle.Primary, 504f, 968f, 400f, 64f);
    }

    void BuildRight(Transform root)
    {
        tabMoves = CollectionUI.NewButton(root, "Moves", ThemedButtonStyle.Primary, 928f, 160f, 200f, 60f);
        tabPassive = CollectionUI.NewButton(root, "Passive", ThemedButtonStyle.Secondary, 1140f, 160f, 200f, 60f);
        equippedLabel = CollectionUI.TextRight(root, "Equipped Label", "Equipped 0/4", UITextStyle.Body, UITheme.Accent,
            TextAlignmentOptions.MidlineRight, 40f, 166f, 360f, 48f);

        // ----- Moves group -----
        movesGroup = UIKit.NewRect("Moves Group", root);
        CollectionUI.TL(movesGroup, 928f, 230f, 952f, 820f);

        moveGrid = UIKit.NewRect("Move Grid", movesGroup);
        CollectionUI.TL(moveGrid, 0f, 0f, 952f, 476f);
        GridLayoutGroup g = moveGrid.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(220f, 226f);
        g.spacing = new Vector2(24f, 24f);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 4;
        g.childAlignment = TextAnchor.UpperLeft;

        CollectionUI.Panel(movesGroup, "Details Frame (replace sprite)", 0f, 496f, 952f, 324f);
        detailIcon = UIKit.NewImage("Move Icon (replace sprite)", movesGroup, "circle", Image.Type.Simple, UITheme.Blue, false);
        CollectionUI.TL(detailIcon.rectTransform, 28f, 520f, 96f, 96f);
        detailIconLetter = UIKit.CreateText(detailIcon.transform, "Icon Letter", "?", UITextStyle.TitleCaps, UITheme.Cream, TextAlignmentOptions.Center);
        UIKit.Stretch(detailIconLetter.rectTransform);
        detailIconLetter.fontSize = 40f;
        detailName = CollectionUI.Text(movesGroup, "Move Name", "Move", UITextStyle.Subtitle, UITheme.Cream,
            TextAlignmentOptions.MidlineLeft, 144f, 520f, 780f, 48f);
        detailStatus = CollectionUI.Text(movesGroup, "Move Status", "", UITextStyle.Small, UITheme.Accent,
            TextAlignmentOptions.MidlineLeft, 144f, 570f, 400f, 36f);
        detailBody = CollectionUI.Text(movesGroup, "Move Description", "", UITextStyle.Body, UITheme.Cream,
            TextAlignmentOptions.TopLeft, 28f, 636f, 896f, 100f);
        detailBody.fontSize = 28f;
        detailTarget = CollectionUI.Text(movesGroup, "Move Target", "", UITextStyle.Small, UITheme.BlueLight,
            TextAlignmentOptions.MidlineLeft, 28f, 744f, 420f, 40f);
        detailMessage = CollectionUI.Text(movesGroup, "Move Message", "", UITextStyle.Small, CollectionUI.Warning,
            TextAlignmentOptions.MidlineLeft, 28f, 790f, 640f, 40f);
        equipButton = CollectionUI.NewButton(movesGroup, "Equip", ThemedButtonStyle.Primary, 704f, 736f, 224f, 64f);

        // ----- Passive group -----
        passiveGroup = UIKit.NewRect("Passive Group", root);
        CollectionUI.TL(passiveGroup, 928f, 230f, 952f, 820f);
        CollectionUI.Panel(passiveGroup, "Passive Frame (replace sprite)", 0f, 0f, 952f, 820f);

        CollectionUI.Text(passiveGroup, "Trait Heading", "Trait", UITextStyle.Label, UITheme.Accent,
            TextAlignmentOptions.MidlineLeft, 40f, 36f, 400f, 40f);
        Image ti = UIKit.NewImage("Trait Icon (replace sprite)", passiveGroup, "circle", Image.Type.Simple, UITheme.Bronze, false);
        CollectionUI.TL(ti.rectTransform, 40f, 92f, 84f, 84f);
        traitName = CollectionUI.Text(passiveGroup, "Trait Name", "Trait name", UITextStyle.Subtitle, UITheme.Cream,
            TextAlignmentOptions.MidlineLeft, 148f, 92f, 760f, 48f);
        traitText = CollectionUI.Text(passiveGroup, "Trait Text", "", UITextStyle.Body, UITheme.Cream,
            TextAlignmentOptions.TopLeft, 148f, 146f, 760f, 110f);

        CollectionUI.Divider(passiveGroup, "Passive Divider", 40f, 300f, 872f, UITheme.Bronze);

        CollectionUI.Text(passiveGroup, "Talent Heading", "Talent", UITextStyle.Label, UITheme.Accent,
            TextAlignmentOptions.MidlineLeft, 40f, 336f, 400f, 40f);
        Image ki = UIKit.NewImage("Talent Icon (replace sprite)", passiveGroup, "circle", Image.Type.Simple, UITheme.Bronze, false);
        CollectionUI.TL(ki.rectTransform, 40f, 392f, 84f, 84f);
        talentName = CollectionUI.Text(passiveGroup, "Talent Name", "Talent name", UITextStyle.Subtitle, UITheme.Cream,
            TextAlignmentOptions.MidlineLeft, 148f, 392f, 760f, 48f);
        talentText = CollectionUI.Text(passiveGroup, "Talent Text", "", UITextStyle.Body, UITheme.Cream,
            TextAlignmentOptions.TopLeft, 148f, 446f, 760f, 110f);
        talentPill = CollectionUI.Pill(passiveGroup, "Talent Lock Pill", 326f, 600f, 300f, 54f, "Unlocks at Floor 2", UITheme.BlueLight, 24f);
    }

    RectTransform BuildPartyRowTemplate()
    {
        RectTransform r = CollectionUI.Card(templates, "Party Row Template", 408f, 76f);
        UseSmallFrame(r);
        Image p = UIKit.NewImage("Portrait (replace sprite)", r, "circle", Image.Type.Simple, UITheme.Gold, false);
        CollectionUI.TL(p.rectTransform, 10f, 10f, 56f, 56f);
        CollectionUI.Text(r, "Name", "Name", UITextStyle.Small, UITheme.Cream, TextAlignmentOptions.MidlineLeft, 78f, 6f, 200f, 34f);
        TMP_Text role = CollectionUI.Text(r, "Role", "Role", UITextStyle.Small, UITheme.BlueLight, TextAlignmentOptions.MidlineLeft, 78f, 40f, 200f, 28f);
        role.fontSize = 18f;
        CollectionUI.Bar(r, "HP Bar", 282f, 33f, 112f, 10f, BarGreen);
        return r;
    }

    RectTransform BuildThumbTemplate()
    {
        RectTransform t = CollectionUI.Card(templates, "Character Thumb Template", 88f, 88f);
        UseSmallFrame(t);
        Image p = UIKit.NewImage("Portrait (replace sprite)", t, "circle", Image.Type.Simple, UITheme.Gold, false);
        CollectionUI.TL(p.rectTransform, 16f, 6f, 56f, 56f);
        TMP_Text initial = UIKit.CreateText(p.transform, "Initial", "?", UITextStyle.Label, UITheme.TextDark, TextAlignmentOptions.Center);
        UIKit.Stretch(initial.rectTransform);
        CollectionUI.Pill(t, "Party Tag", 9f, 64f, 70f, 20f, "PARTY", UITheme.Accent, 12f);
        return t;
    }

    RectTransform BuildMoveTemplate()
    {
        RectTransform t = CollectionUI.Card(templates, "Move Tile Template", 220f, 226f);
        Image icon = UIKit.NewImage("Icon (replace sprite)", t, "circle", Image.Type.Simple, UITheme.Blue, false);
        CollectionUI.TL(icon.rectTransform, 62f, 16f, 96f, 96f);
        TMP_Text letter = UIKit.CreateText(icon.transform, "Icon Letter", "?", UITextStyle.TitleCaps, UITheme.Cream, TextAlignmentOptions.Center);
        UIKit.Stretch(letter.rectTransform);
        letter.fontSize = 40f;
        TMP_Text name = CollectionUI.Text(t, "Name", "Move", UITextStyle.Small, UITheme.Cream, TextAlignmentOptions.Top, 10f, 120f, 200f, 62f);
        CollectionUI.AutoSize(name, 16f, 24f);
        CollectionUI.Pill(t, "Status Pill", 30f, 188f, 160f, 28f, "EQUIPPED", UITheme.Gold, 16f);
        return t;
    }

    static void UseSmallFrame(RectTransform card)
    {
        Image fr = CollectionUI.Find<Image>(card, "Frame (replace sprite)");
        if (fr == null) return;
        fr.sprite = UIArt.Get("plate");
        fr.type = Image.Type.Sliced;
    }

    // =====================================================================
    // RUNTIME
    // =====================================================================
    protected override void Wire()
    {
        tabMoves.onClick.RemoveAllListeners();
        tabMoves.onClick.AddListener(() => SetTab(true));
        tabPassive.onClick.RemoveAllListeners();
        tabPassive.onClick.AddListener(() => SetTab(false));
        equipButton.onClick.RemoveAllListeners();
        equipButton.onClick.AddListener(() => ToggleMove(selectedMove));
        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(OnRemoveClicked);
    }

    protected override void Populate()
    {
        cur = null;
        if (!string.IsNullOrEmpty(CollectionNav.DetailCharId)) cur = CollectionData.GetCharacter(CollectionNav.DetailCharId);
        if (cur == null)
        {
            foreach (CharacterData c in CollectionData.Characters())
                if (CollectionData.IsUnlocked(c)) { cur = c; break; }
        }
        if (cur == null) return;
        movesTab = true;
        SelectCharacter(cur);
    }

    protected override void Update()
    {
        base.Update();
        if (messageTimer > 0f)
        {
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f && detailMessage != null) detailMessage.text = "";
        }
    }

    void SelectCharacter(CharacterData c)
    {
        cur = c;
        equipped = CollectionData.GetEquippedMoves(c);
        selectedMove = 0;
        if (detailMessage != null) detailMessage.text = "";
        RefreshLists();
        RefreshCentre();
        RefreshMoves();
        RefreshMoveDetails();
        RefreshPassive();
        SetTab(movesTab);
    }

    bool OpenedFromRun
    {
        get { return CollectionNav.DetailFromRun && CollectionData.InRun; }
    }

    // ---------- Left lists ----------
    void RefreshLists()
    {
        CollectionUI.ClearChildren(partyContent);
        CollectionUI.ClearChildren(charGridContent);

        List<PartyMember> party = OpenedFromRun ? CollectionData.Party() : new List<PartyMember>();
        partyHint.gameObject.SetActive(party.Count == 0);
        foreach (PartyMember m in party)
        {
            CharacterData c = CollectionData.GetCharacter(m.charId);
            if (c == null) continue;
            RectTransform row = Spawn(partyRowTemplate, partyContent);
            string key = CollectionData.RoleKey(c);
            Color rc = CollectionData.RoleColor(key);
            CollectionUI.SetText(row, "Name", c.name);
            CollectionUI.SetText(row, "Role", CollectionData.RoleLabel(key));
            Image pi = CollectionUI.Find<Image>(row, "Portrait (replace sprite)");
            if (pi != null) pi.color = rc;
            float frac = m.maxHp > 0 ? (float)m.hp / m.maxHp : 0f;
            CollectionUI.SetBar(row.Find("HP Bar"), frac);
            MarkSelected(row, c.id == cur.id);
            string id = c.id;
            row.GetComponent<Button>().onClick.AddListener(() => SelectCharacter(CollectionData.GetCharacter(id)));
        }

        foreach (CharacterData c in CollectionData.Characters())
        {
            bool open = CollectionData.IsUnlocked(c);
            RectTransform th = Spawn(charThumbTemplate, charGridContent);
            string key = CollectionData.RoleKey(c);
            Image pi = CollectionUI.Find<Image>(th, "Portrait (replace sprite)");
            if (pi != null) pi.color = open ? CollectionData.RoleColor(key) : new Color(0.35f, 0.40f, 0.46f, 1f);
            string initial = string.IsNullOrEmpty(c.name) ? "?" : c.name.Substring(0, 1);
            CollectionUI.SetText(th, "Portrait (replace sprite)/Initial", initial);
            bool inParty = OpenedFromRun && CollectionData.InParty(c.id);
            CollectionUI.SetActive(th, "Party Tag (replace sprite)", inParty);
            Image fr = CollectionUI.Find<Image>(th, "Frame (replace sprite)");
            if (fr != null) fr.color = open ? Color.white : new Color(0.55f, 0.60f, 0.68f, 1f);
            MarkSelected(th, c.id == cur.id);

            Button b = th.GetComponent<Button>();
            HoverGlow hg = th.GetComponent<HoverGlow>();
            if (hg != null && !open) hg.enabled = false;
            b.interactable = open;
            string cid = c.id;
            b.onClick.AddListener(() => SelectCharacter(CollectionData.GetCharacter(cid)));
        }
    }

    static void MarkSelected(RectTransform card, bool selected)
    {
        HoverGlow hg = card.GetComponent<HoverGlow>();
        if (hg != null) hg.SetHold(selected ? 0.75f : 0f);
    }

    // ---------- Centre ----------
    void RefreshCentre()
    {
        string key = CollectionData.RoleKey(cur);
        Color rc = CollectionData.RoleColor(key);
        portraitImage.color = new Color(rc.r, rc.g, rc.b, 0.85f);
        nameLabel.text = cur.name;
        CollectionUI.SetPill(rolePill, CollectionData.RoleLabel(key), rc);

        float[] values = { cur.hp, cur.atk, cur.def, cur.spd };
        PartyMember pm;
        bool inParty = OpenedFromRun && CollectionData.TryGetPartyMember(cur.id, out pm);
        for (int i = 0; i < 4; i++)
        {
            float frac = Mathf.Clamp01(values[i] / StatCaps[i]);
            string text = Mathf.RoundToInt(values[i]).ToString();
            if (i == 0 && inParty)
            {
                PartyMember m;
                CollectionData.TryGetPartyMember(cur.id, out m);
                frac = m.maxHp > 0 ? (float)m.hp / m.maxHp : 0f;
                text = m.hp + "/" + m.maxHp;
            }
            CollectionUI.SetBar(statBars[i], frac);
            statValues[i].text = text;
        }

        bool canRemove = inParty;
        removeButton.gameObject.SetActive(canRemove);
        removeButton.interactable = CollectionData.PartyCount > 1;
    }

    void OnRemoveClicked()
    {
        if (cur == null || !OpenedFromRun || CollectionData.PartyCount <= 1) return;
        string id = cur.id;
        string who = cur.name;
        ConfirmPanel.GetOrCreate().Show("Remove " + who + " from the party?", "Stay", "Remove", () =>
        {
            CollectionData.RemoveFromParty(id);
            RefreshLists();
            RefreshCentre();
        });
    }

    // ---------- Tabs ----------
    void SetTab(bool moves)
    {
        movesTab = moves;
        movesGroup.gameObject.SetActive(moves);
        passiveGroup.gameObject.SetActive(!moves);
        equippedLabel.gameObject.SetActive(moves);
        tabMoves.SetStyle(moves ? ThemedButtonStyle.Primary : ThemedButtonStyle.Secondary);
        tabPassive.SetStyle(moves ? ThemedButtonStyle.Secondary : ThemedButtonStyle.Primary);
    }

    // ---------- Moves ----------
    static Color TagColor(string tag)
    {
        string t = string.IsNullOrEmpty(tag) ? "" : tag.ToLowerInvariant();
        if (t.Contains("base")) return UITheme.Hex("D9605A");
        if (t.Contains("def")) return UITheme.Hex("4CAF7A");
        if (t.Contains("role")) return UITheme.Hex("4F8FD6");
        return UITheme.Gold;
    }

    static string Initial(string s)
    {
        return string.IsNullOrEmpty(s) ? "?" : s.Substring(0, 1).ToUpperInvariant();
    }

    void RefreshMoves()
    {
        CollectionUI.ClearChildren(moveGrid);
        equippedLabel.text = "Equipped " + equipped.Count + "/" + CollectionData.MaxEquipped;
        if (cur.moves == null) return;

        for (int i = 0; i < cur.moves.Length; i++)
        {
            SkillData m = cur.moves[i];
            bool on = equipped.Contains(m.id);
            RectTransform tile = Spawn(moveTemplate, moveGrid);
            Color tc = TagColor(m.tag);

            Image icon = CollectionUI.Find<Image>(tile, "Icon (replace sprite)");
            if (icon != null) icon.color = on ? tc : new Color(tc.r * 0.6f, tc.g * 0.6f, tc.b * 0.6f, 1f);
            CollectionUI.SetText(tile, "Icon (replace sprite)/Icon Letter", Initial(m.name));
            CollectionUI.SetText(tile, "Name", m.name);

            Image pill = CollectionUI.Find<Image>(tile, "Status Pill (replace sprite)");
            CollectionUI.SetPill(pill, on ? "EQUIPPED" : "NOT EQUIPPED", on ? UITheme.Gold : UITheme.Disabled);

            Image fr = CollectionUI.Find<Image>(tile, "Frame (replace sprite)");
            if (fr != null) fr.color = on ? Color.white : new Color(0.72f, 0.76f, 0.84f, 1f);
            MarkSelected(tile, i == selectedMove);

            int index = i;
            tile.GetComponent<Button>().onClick.AddListener(() => OnMoveClicked(index));
        }
    }

    void OnMoveClicked(int index)
    {
        bool alreadySelected = index == selectedMove;
        selectedMove = index;
        if (clickTogglesMove || alreadySelected)
        {
            ToggleMove(index);
        }
        else
        {
            RefreshMoves();
            RefreshMoveDetails();
        }
    }

    void ToggleMove(int index)
    {
        if (cur == null || cur.moves == null || index < 0 || index >= cur.moves.Length) return;
        selectedMove = index;
        string id = cur.moves[index].id;
        List<string> eq = new List<string>(equipped);

        if (eq.Contains(id))
        {
            if (eq.Count <= 1)
            {
                ShowMessage("Keep at least one move equipped.");
                RefreshMoves();
                RefreshMoveDetails();
                return;
            }
            eq.Remove(id);
        }
        else
        {
            if (eq.Count >= CollectionData.MaxEquipped)
            {
                ShowMessage("Only " + CollectionData.MaxEquipped + " moves can be equipped. Unequip one first.");
                RefreshMoves();
                RefreshMoveDetails();
                return;
            }
            eq.Add(id);
        }

        equipped = eq;
        CollectionData.SaveEquippedMoves(cur, equipped);
        RefreshMoves();
        RefreshMoveDetails();
    }

    void ShowMessage(string text)
    {
        detailMessage.text = text;
        messageTimer = 2.5f;
    }

    void RefreshMoveDetails()
    {
        if (cur.moves == null || cur.moves.Length == 0)
        {
            detailName.text = "";
            detailBody.text = "";
            detailTarget.text = "";
            detailStatus.text = "";
            equipButton.gameObject.SetActive(false);
            return;
        }
        equipButton.gameObject.SetActive(true);
        if (selectedMove >= cur.moves.Length) selectedMove = 0;
        SkillData m = cur.moves[selectedMove];
        bool on = equipped.Contains(m.id);
        detailName.text = m.name;
        detailBody.text = m.description;
        detailTarget.text = "Target: " + m.target;
        detailStatus.text = on ? "Equipped" : "Not equipped";
        detailIcon.color = TagColor(m.tag);
        detailIconLetter.text = Initial(m.name);
        equipButton.SetLabel(on ? "Unequip" : "Equip");
    }

    // ---------- Passive ----------
    void RefreshPassive()
    {
        traitName.text = cur.traitName;
        traitText.text = cur.traitText;
        talentName.text = cur.talentName;
        talentText.text = cur.talentText;

        int floor = cur.talentUnlockFloor;
        if (floor <= 1)
        {
            talentPill.gameObject.SetActive(false);
            return;
        }
        talentPill.gameObject.SetActive(true);
        bool unlockedNow = OpenedFromRun && CollectionData.RunFloor >= floor;
        if (unlockedNow) CollectionUI.SetPill(talentPill, "Unlocked", UITheme.Hex("7BC96F"));
        else CollectionUI.SetPill(talentPill, "Unlocks at Floor " + floor, UITheme.BlueLight);
    }
}
