using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Legends (concept 13): header with BACK, "Legends n/N" counter, grid of character cards.
/// Unlocked card: portrait slot, name, role pill, HP/ATK/DEF/SPD, "Costumes x/y" bar. Click opens Character Detail.
/// Locked card: greyed, lock + unlock condition text.
/// Scene: GameScenes.Legends. BACK goes to GameScenes.DungeonHub.
/// </summary>
public class LegendsScreen : CollectionScreenBase
{
    public TMP_Text counterLabel;
    public RectTransform gridContent;
    public RectTransform cardTemplate;

    protected override string ScreenTitle { get { return "Legends"; } }

    // ---------- Build (editor or first run) ----------
    protected override void BuildContent(Transform root)
    {
        CollectionUI.Text(root, "Subtitle", "Unlocked through Campaign chapters and Achievements.",
            UITextStyle.Small, UITheme.BlueLight, TextAlignmentOptions.MidlineLeft, 48f, 152f, 1100f, 40f);
        counterLabel = CollectionUI.TextRight(root, "Counter", "Legends 0/0", UITextStyle.Subtitle,
            UITheme.Accent, TextAlignmentOptions.MidlineRight, 48f, 148f, 500f, 44f);

        CollectionUI.Scroll(root, "Legends Scroll", 24f, 205f, 1872f, 855f, true, out gridContent);
        GridLayoutGroup g = gridContent.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(440f, 240f);
        g.spacing = new Vector2(20f, 20f);
        g.padding = new RectOffset(8, 8, 8, 24);
        g.startCorner = GridLayoutGroup.Corner.UpperLeft;
        g.childAlignment = TextAnchor.UpperLeft;
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 4;

        cardTemplate = BuildCardTemplate();
    }

    RectTransform BuildCardTemplate()
    {
        RectTransform card = CollectionUI.Card(templates, "Legend Card Template", 440f, 240f);

        Image ring = UIKit.NewImage("Portrait Ring (replace sprite)", card, "circle", Image.Type.Simple, UITheme.Gold, false);
        CollectionUI.TL(ring.rectTransform, 18f, 18f, 124f, 124f);
        Image portrait = UIKit.NewImage("Portrait (replace sprite)", card, null, Image.Type.Simple, UITheme.Gold, false);
        CollectionUI.TL(portrait.rectTransform, 40f, 38f, 80f, 84f);
        TMP_Text art = UIKit.CreateText(portrait.transform, "Art Label", "ART", UITextStyle.Small, UITheme.Cream, TextAlignmentOptions.Center);
        UIKit.Stretch(art.rectTransform);

        TMP_Text name = CollectionUI.Text(card, "Name", "Name", UITextStyle.Subtitle, UITheme.Cream,
            TextAlignmentOptions.MidlineLeft, 156f, 14f, 272f, 52f);
        CollectionUI.AutoSize(name, 20f, 32f);
        CollectionUI.Pill(card, "Role Pill", 156f, 70f, 190f, 36f, "Role", UITheme.Gold, 22f);

        // Unlocked group
        RectTransform ug = UIKit.NewRect("Unlocked Group", card);
        UIKit.Stretch(ug);
        TMP_Text stats = CollectionUI.Text(ug, "Stats", "HP 0   ATK 0   DEF 0   SPD 0", UITextStyle.Small,
            UITheme.Cream, TextAlignmentOptions.MidlineLeft, 156f, 116f, 272f, 32f);
        CollectionUI.AutoSize(stats, 14f, 20f);
        Image mask = UIKit.NewImage("Costume Icon (replace sprite)", ug, "diamond", Image.Type.Simple, UITheme.Cream, false);
        CollectionUI.TL(mask.rectTransform, 22f, 172f, 26f, 26f);
        TMP_Text cl = CollectionUI.Text(ug, "Costume Label", "Costumes 0/0", UITextStyle.Small, UITheme.Cream,
            TextAlignmentOptions.MidlineLeft, 58f, 168f, 300f, 34f);
        cl.fontSize = 20f;
        CollectionUI.Bar(ug, "Costume Bar", 20f, 208f, 400f, 10f, UITheme.SliderFill);

        // Locked group
        RectTransform lg = UIKit.NewRect("Locked Group", card);
        UIKit.Stretch(lg);
        Image lockIcon = UIKit.NewImage("Lock Icon (replace sprite)", lg, "circle", Image.Type.Simple, CollectionUI.Warning, false);
        CollectionUI.TL(lockIcon.rectTransform, 156f, 114f, 28f, 28f);
        TMP_Text lockLabel = CollectionUI.Text(lg, "Lock Label", "LOCKED", UITextStyle.Small, CollectionUI.Warning,
            TextAlignmentOptions.MidlineLeft, 194f, 110f, 200f, 36f);
        lockLabel.fontStyle = FontStyles.Bold;
        TMP_Text cond = CollectionUI.Text(lg, "Unlock Text", "Unlock condition", UITextStyle.Small, UITheme.Cream,
            TextAlignmentOptions.TopLeft, 22f, 156f, 400f, 76f);
        cond.fontSize = 20f;
        return card;
    }

    protected override void Wire() { }

    // ---------- Runtime ----------
    protected override void Populate()
    {
        CollectionUI.ClearChildren(gridContent);

        List<CharacterData> all = CollectionData.Characters();
        List<CharacterData> ordered = new List<CharacterData>();
        foreach (CharacterData c in all) if (CollectionData.IsUnlocked(c)) ordered.Add(c);
        foreach (CharacterData c in all) if (!CollectionData.IsUnlocked(c)) ordered.Add(c);

        int unlocked = 0;
        foreach (CharacterData c in ordered)
        {
            bool open = CollectionData.IsUnlocked(c);
            if (open) unlocked++;
            RectTransform card = Spawn(cardTemplate, gridContent);
            FillCard(card, c, open);
        }
        counterLabel.text = "Legends " + unlocked + "/" + ordered.Count;
    }

    void FillCard(RectTransform card, CharacterData c, bool open)
    {
        string key = CollectionData.RoleKey(c);
        Color roleColor = CollectionData.RoleColor(key);
        Color grey = new Color(0.55f, 0.60f, 0.68f, 1f);

        Image frame = CollectionUI.Find<Image>(card, "Frame (replace sprite)");
        if (frame != null) frame.color = open ? Color.white : grey;

        Image ring = CollectionUI.Find<Image>(card, "Portrait Ring (replace sprite)");
        if (ring != null) ring.color = open ? new Color(roleColor.r, roleColor.g, roleColor.b, 0.55f) : new Color(0.4f, 0.45f, 0.52f, 0.7f);
        Image portrait = CollectionUI.Find<Image>(card, "Portrait (replace sprite)");
        if (portrait != null) portrait.color = open ? roleColor : new Color(0.35f, 0.40f, 0.46f, 1f);

        TMP_Text nm = CollectionUI.Find<TMP_Text>(card, "Name");
        if (nm != null) { nm.text = c.name; nm.color = open ? UITheme.Cream : UITheme.Disabled; }

        Image pill = CollectionUI.Find<Image>(card, "Role Pill (replace sprite)");
        CollectionUI.SetPill(pill, CollectionData.RoleLabel(key), open ? roleColor : UITheme.Disabled);

        CollectionUI.SetActive(card, "Unlocked Group", open);
        CollectionUI.SetActive(card, "Locked Group", !open);

        if (open)
        {
            CollectionUI.SetText(card, "Unlocked Group/Stats",
                "HP " + c.hp + "   ATK " + c.atk + "   DEF " + c.def + "   SPD " + c.spd);
            int owned = CollectionData.CostumesOwned(c);
            int total = CollectionData.CostumesTotal(c);
            CollectionUI.SetText(card, "Unlocked Group/Costume Label", "Costumes " + owned + "/" + total);
            CollectionUI.SetBar(card.Find("Unlocked Group/Costume Bar"), total > 0 ? (float)owned / total : 0f);
        }
        else
        {
            CollectionUI.SetText(card, "Locked Group/Unlock Text", c.unlockConditionText);
        }

        Button b = card.GetComponent<Button>();
        HoverGlow hg = card.GetComponent<HoverGlow>();
        if (hg != null) hg.enabled = open;
        if (b != null)
        {
            b.interactable = open;
            string id = c.id;
            b.onClick.AddListener(() => CollectionNav.OpenCharacterDetail(id, false, GameScenes.Legends));
        }
    }
}
