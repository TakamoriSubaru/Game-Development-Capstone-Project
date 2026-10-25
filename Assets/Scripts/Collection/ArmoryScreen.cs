using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Armory (concepts 15 and 16). Tabs EQUIPMENT and BLESSINGS.
/// Equipment: 5-column scrolling grid, unfound items show "?" / "???", detail panel, "Found x/N".
/// Blessings: the 8 gods with a detail panel (target, effect, price, "Single-use, drains the target's Stamina").
/// Scene: GameScenes.Armory. BACK goes to GameScenes.DungeonHub.
/// </summary>
public class ArmoryScreen : CollectionScreenBase
{
    public ThemedButton tabEquipment;
    public ThemedButton tabBlessings;
    public TMP_Text foundLabel;
    public TMP_Text blessingNote;
    public RectTransform equipGroup;
    public RectTransform blessGroup;

    // equipment
    public RectTransform equipGrid;
    public RectTransform equipCellTemplate;
    public TMP_Text equipName;
    public TMP_Text equipSource;
    public TMP_Text equipBody;

    // blessings
    public RectTransform blessGrid;
    public RectTransform blessCardTemplate;
    public Image blessEmblem;
    public TMP_Text blessEmblemLetter;
    public TMP_Text blessName;
    public TMP_Text blessTarget;
    public TMP_Text blessEffect;
    public TMP_Text blessPrice;
    public TMP_Text blessNote;

    List<CollectionData.EquipInfo> equipment = new List<CollectionData.EquipInfo>();
    List<CollectionData.BlessingInfo> blessings = new List<CollectionData.BlessingInfo>();
    int selectedEquip;
    int selectedBless;
    bool equipmentTab = true;

    protected override string ScreenTitle { get { return "Armory"; } }

    // =====================================================================
    // BUILD
    // =====================================================================
    protected override void BuildContent(Transform root)
    {
        tabEquipment = CollectionUI.NewButton(root, "Equipment", ThemedButtonStyle.Primary, 48f, 160f, 300f, 64f);
        tabBlessings = CollectionUI.NewButton(root, "Blessings", ThemedButtonStyle.Secondary, 364f, 160f, 300f, 64f);
        foundLabel = CollectionUI.TextRight(root, "Found Label", "Found 0/0", UITextStyle.Subtitle, UITheme.Accent,
            TextAlignmentOptions.MidlineRight, 48f, 166f, 500f, 50f);
        blessingNote = CollectionUI.TextRight(root, "Blessing Note", "Blessings: single-use, drain the target's Stamina.",
            UITextStyle.Small, UITheme.Accent, TextAlignmentOptions.MidlineRight, 48f, 170f, 1000f, 44f);

        equipGroup = UIKit.NewRect("Equipment Group", root);
        UIKit.Stretch(equipGroup);
        BuildEquipment(equipGroup);

        blessGroup = UIKit.NewRect("Blessings Group", root);
        UIKit.Stretch(blessGroup);
        BuildBlessings(blessGroup);
    }

    void BuildEquipment(Transform g)
    {
        CollectionUI.Panel(g, "Grid Frame (replace sprite)", 40f, 252f, 1270f, 800f);
        RectTransform content;
        CollectionUI.Scroll(g, "Equipment Scroll", 52f, 264f, 1246f, 776f, true, out content);
        equipGrid = content;
        GridLayoutGroup gl = content.gameObject.AddComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(224f, 168f);
        gl.spacing = new Vector2(16f, 16f);
        gl.padding = new RectOffset(8, 8, 8, 8);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = 5;
        gl.childAlignment = TextAnchor.UpperLeft;

        CollectionUI.Panel(g, "Detail Frame (replace sprite)", 1330f, 252f, 542f, 800f);
        equipName = CollectionUI.Text(g, "Detail Name", "???", UITextStyle.Subtitle, UITheme.Cream,
            TextAlignmentOptions.MidlineLeft, 1364f, 286f, 480f, 64f);
        CollectionUI.Divider(g, "Detail Divider A", 1364f, 360f, 478f, UITheme.Gold);
        equipSource = CollectionUI.Text(g, "Detail Source", "", UITextStyle.Body, UITheme.Accent,
            TextAlignmentOptions.MidlineLeft, 1364f, 374f, 480f, 48f);
        CollectionUI.Divider(g, "Detail Divider B", 1364f, 432f, 478f, UITheme.Gold);
        equipBody = CollectionUI.Text(g, "Detail Body", "", UITextStyle.Body, UITheme.Cream,
            TextAlignmentOptions.TopLeft, 1364f, 450f, 480f, 560f);

        // cell template
        RectTransform c = CollectionUI.Card(templates, "Equipment Cell Template", 224f, 168f);
        Image icon = UIKit.NewImage("Icon (replace sprite)", c, null, Image.Type.Simple, Color.white, false);
        CollectionUI.TL(icon.rectTransform, 62f, 14f, 100f, 76f);
        CollectionUI.Text(c, "Initials", "CS", UITextStyle.TitleCaps, UITheme.Cream, TextAlignmentOptions.Center, 0f, 20f, 224f, 66f);
        CollectionUI.Solid(c, "Initials Rule", 62f, 94f, 100f, 2f, UITheme.Gold);
        TMP_Text nm = CollectionUI.Text(c, "Name", "Name", UITextStyle.Small, UITheme.Cream, TextAlignmentOptions.Top, 8f, 102f, 208f, 56f);
        CollectionUI.AutoSize(nm, 14f, 22f);
        CollectionUI.Text(c, "Question", "?", UITextStyle.TitleCaps, UITheme.BlueLight, TextAlignmentOptions.Center, 0f, 14f, 224f, 86f);
        CollectionUI.Text(c, "Unknown Name", "???", UITextStyle.Small, UITheme.BlueLight, TextAlignmentOptions.Center, 0f, 110f, 224f, 40f);
        equipCellTemplate = c;
    }

    void BuildBlessings(Transform g)
    {
        RectTransform content;
        CollectionUI.Scroll(g, "Blessings Scroll", 40f, 252f, 1272f, 800f, true, out content);
        blessGrid = content;
        GridLayoutGroup gl = content.gameObject.AddComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(232f, 290f);
        gl.spacing = new Vector2(16f, 16f);
        gl.padding = new RectOffset(8, 8, 8, 8);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = 5;
        gl.childAlignment = TextAnchor.UpperLeft;

        CollectionUI.Panel(g, "Detail Frame (replace sprite)", 1330f, 252f, 542f, 800f);
        blessEmblem = UIKit.NewImage("Emblem (replace sprite)", g, "circle", Image.Type.Simple, UITheme.Gold, false);
        CollectionUI.TL(blessEmblem.rectTransform, 1364f, 286f, 96f, 96f);
        blessEmblemLetter = UIKit.CreateText(blessEmblem.transform, "Emblem Letter", "A", UITextStyle.TitleCaps, UITheme.TextDark, TextAlignmentOptions.Center);
        UIKit.Stretch(blessEmblemLetter.rectTransform);
        blessEmblemLetter.fontSize = 44f;
        blessName = CollectionUI.Text(g, "Detail Name", "Blessing of Apollo", UITextStyle.Subtitle, UITheme.Cream,
            TextAlignmentOptions.MidlineLeft, 1476f, 290f, 380f, 88f);
        CollectionUI.AutoSize(blessName, 22f, 32f);
        blessTarget = CollectionUI.Text(g, "Detail Target", "Target: 1 character", UITextStyle.Body, UITheme.Accent,
            TextAlignmentOptions.MidlineLeft, 1364f, 400f, 480f, 48f);
        CollectionUI.Divider(g, "Detail Divider", 1364f, 460f, 478f, UITheme.Gold);
        blessEffect = CollectionUI.Text(g, "Detail Effect", "", UITextStyle.Body, UITheme.Cream,
            TextAlignmentOptions.TopLeft, 1364f, 480f, 480f, 360f);
        blessPrice = CollectionUI.Text(g, "Detail Price", "Price: 0 Gold.", UITextStyle.Body, UITheme.Accent,
            TextAlignmentOptions.MidlineLeft, 1364f, 880f, 480f, 44f);
        blessNote = CollectionUI.Text(g, "Detail Note", "Single-use. Gives a massive buff, then drains the target's Stamina.",
            UITextStyle.Small, UITheme.Cream, TextAlignmentOptions.TopLeft, 1364f, 928f, 480f, 100f);

        // card template
        RectTransform c = CollectionUI.Card(templates, "Blessing Card Template", 232f, 290f);
        Image ring = UIKit.NewImage("Emblem Ring (replace sprite)", c, "circle", Image.Type.Simple, UITheme.Gold, false);
        CollectionUI.TL(ring.rectTransform, 46f, 28f, 140f, 140f);
        CollectionUI.Text(c, "Emblem Letter", "A", UITextStyle.TitleCaps, UITheme.TextDark, TextAlignmentOptions.Center, 46f, 28f, 140f, 140f);
        CollectionUI.Text(c, "Name", "God", UITextStyle.Subtitle, UITheme.Cream, TextAlignmentOptions.Center, 8f, 188f, 216f, 52f);
        CollectionUI.Solid(c, "Underline", 66f, 248f, 100f, 3f, UITheme.Gold);
        blessCardTemplate = c;
    }

    // =====================================================================
    // RUNTIME
    // =====================================================================
    protected override void Wire()
    {
        tabEquipment.onClick.RemoveAllListeners();
        tabEquipment.onClick.AddListener(() => SetTab(true));
        tabBlessings.onClick.RemoveAllListeners();
        tabBlessings.onClick.AddListener(() => SetTab(false));
    }

    protected override void Populate()
    {
        equipment = CollectionData.Equipment();
        blessings = CollectionData.Blessings();
        selectedEquip = 0;
        selectedBless = 0;
        for (int i = 0; i < equipment.Count; i++) if (equipment[i].found) { selectedEquip = i; break; }

        RefreshEquipment();
        RefreshBlessings();
        SetTab(true);
    }

    void SetTab(bool equipmentSide)
    {
        equipmentTab = equipmentSide;
        equipGroup.gameObject.SetActive(equipmentSide);
        blessGroup.gameObject.SetActive(!equipmentSide);
        foundLabel.gameObject.SetActive(equipmentSide);
        blessingNote.gameObject.SetActive(!equipmentSide);
        tabEquipment.SetStyle(equipmentSide ? ThemedButtonStyle.Primary : ThemedButtonStyle.Secondary);
        tabBlessings.SetStyle(equipmentSide ? ThemedButtonStyle.Secondary : ThemedButtonStyle.Primary);
    }

    static string Initials(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        string[] words = name.Split(' ');
        string r = "";
        foreach (string w in words)
        {
            if (w.Length == 0) continue;
            r += w.Substring(0, 1).ToUpperInvariant();
            if (r.Length >= 2) break;
        }
        return r.Length == 0 ? "?" : r;
    }

    static void MarkSelected(RectTransform card, bool selected)
    {
        HoverGlow hg = card.GetComponent<HoverGlow>();
        if (hg != null) hg.SetHold(selected ? 0.9f : 0f);
    }

    // ---------- Equipment ----------
    void RefreshEquipment()
    {
        CollectionUI.ClearChildren(equipGrid);
        int found = 0;
        for (int i = 0; i < equipment.Count; i++)
        {
            CollectionData.EquipInfo e = equipment[i];
            if (e.found) found++;
            RectTransform cell = Spawn(equipCellTemplate, equipGrid);

            Image icon = CollectionUI.Find<Image>(cell, "Icon (replace sprite)");
            bool hasArt = icon != null && icon.sprite != null;
            if (icon != null) icon.enabled = e.found && hasArt;

            CollectionUI.SetActive(cell, "Initials", e.found && !hasArt);
            CollectionUI.SetActive(cell, "Initials Rule", e.found);
            CollectionUI.SetActive(cell, "Name", e.found);
            CollectionUI.SetActive(cell, "Question", !e.found);
            CollectionUI.SetActive(cell, "Unknown Name", !e.found);
            if (e.found)
            {
                CollectionUI.SetText(cell, "Initials", Initials(e.name));
                CollectionUI.SetText(cell, "Name", e.name);
            }
            Image fr = CollectionUI.Find<Image>(cell, "Frame (replace sprite)");
            if (fr != null) fr.color = e.found ? Color.white : new Color(0.72f, 0.76f, 0.84f, 1f);

            int index = i;
            cell.GetComponent<Button>().onClick.AddListener(() => { selectedEquip = index; RefreshEquipmentSelection(); });
        }
        foundLabel.text = "Found " + found + "/" + equipment.Count;
        RefreshEquipmentSelection();
    }

    void RefreshEquipmentSelection()
    {
        for (int i = 0; i < equipGrid.childCount; i++)
        {
            Transform t = equipGrid.GetChild(i);
            if (!t.gameObject.activeSelf) continue;
            MarkSelected((RectTransform)t, t.GetSiblingIndex() == selectedEquip);
        }
        if (equipment.Count == 0)
        {
            equipName.text = "???";
            equipSource.text = "";
            equipBody.text = "";
            return;
        }
        if (selectedEquip >= equipment.Count) selectedEquip = 0;
        CollectionData.EquipInfo e = equipment[selectedEquip];
        equipName.text = e.found ? e.name : "???";
        equipSource.text = e.source;
        equipBody.text = e.found ? e.effect : "Not found yet. Defeat a Pack's boss to claim its equipment.";
    }

    // ---------- Blessings ----------
    static Color GodColor(string god)
    {
        string g = string.IsNullOrEmpty(god) ? "" : god.ToLowerInvariant();
        if (g.Contains("apollo")) return UITheme.Hex("F2C66D");
        if (g.Contains("athena")) return UITheme.Hex("6FB7E8");
        if (g.Contains("ares")) return UITheme.Hex("E0605A");
        if (g.Contains("hephaest")) return UITheme.Hex("E8884A");
        if (g.Contains("artemis")) return UITheme.Hex("9CCB7A");
        if (g.Contains("demeter")) return UITheme.Hex("D9A441");
        if (g.Contains("hermes")) return UITheme.Hex("B58AE6");
        if (g.Contains("zeus")) return UITheme.Hex("5BB8F0");
        return UITheme.Gold;
    }

    void RefreshBlessings()
    {
        CollectionUI.ClearChildren(blessGrid);
        for (int i = 0; i < blessings.Count; i++)
        {
            CollectionData.BlessingInfo b = blessings[i];
            Color gc = GodColor(b.god);
            RectTransform card = Spawn(blessCardTemplate, blessGrid);

            Image fr = CollectionUI.Find<Image>(card, "Frame (replace sprite)");
            if (fr != null) fr.color = Color.Lerp(Color.white, gc, 0.65f);
            Image ring = CollectionUI.Find<Image>(card, "Emblem Ring (replace sprite)");
            if (ring != null) ring.color = gc;
            CollectionUI.SetText(card, "Emblem Letter", b.god.Length > 0 ? b.god.Substring(0, 1).ToUpperInvariant() : "?");
            CollectionUI.SetText(card, "Name", b.god);
            Image ul = CollectionUI.Find<Image>(card, "Underline");
            if (ul != null) ul.color = gc;
            HoverGlow hg = card.GetComponent<HoverGlow>();
            if (hg != null) hg.SetColor(gc);

            int index = i;
            card.GetComponent<Button>().onClick.AddListener(() => { selectedBless = index; RefreshBlessingSelection(); });
        }
        RefreshBlessingSelection();
    }

    void RefreshBlessingSelection()
    {
        for (int i = 0; i < blessGrid.childCount; i++)
        {
            Transform t = blessGrid.GetChild(i);
            if (!t.gameObject.activeSelf) continue;
            MarkSelected((RectTransform)t, t.GetSiblingIndex() == selectedBless);
        }
        if (blessings.Count == 0) return;
        if (selectedBless >= blessings.Count) selectedBless = 0;
        CollectionData.BlessingInfo b = blessings[selectedBless];
        Color gc = GodColor(b.god);
        blessEmblem.color = gc;
        blessEmblemLetter.text = b.god.Length > 0 ? b.god.Substring(0, 1).ToUpperInvariant() : "?";
        blessName.text = "Blessing of " + b.god;
        blessTarget.text = "Target: " + b.target;
        blessEffect.text = b.effect;
        blessPrice.text = "Price: " + b.price + " Gold.";
    }
}
