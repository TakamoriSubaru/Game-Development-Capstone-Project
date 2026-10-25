using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pack Compendium. PLACEHOLDER SCREEN: there is no concept art for it, so it reuses the Armory equipment layout.
/// Packs are listed by floor. Enemies are shown as "???" (the contract has no enemy data or "enemy met" flag yet).
/// The boss drop shows the equipment name once that equipment has been found, otherwise "???".
/// Scene: GameScenes.PackCompendium. BACK goes to GameScenes.DungeonHub.
/// </summary>
public class PackCompendiumScreen : CollectionScreenBase
{
    public TMP_Text counterLabel;
    public RectTransform packGrid;
    public RectTransform packCellTemplate;
    public TMP_Text detailName;
    public TMP_Text detailFloor;
    public TMP_Text detailEnemies;
    public TMP_Text detailBoss;

    List<CollectionData.PackInfo> packs = new List<CollectionData.PackInfo>();
    int selected;

    // How many "???" enemy lines the detail panel shows until real enemy data exists.
    const int UnknownEnemyLines = 3;

    protected override string ScreenTitle { get { return "Pack Compendium"; } }

    protected override void BuildContent(Transform root)
    {
        // Placeholder banner
        Image banner = UIKit.NewImage("Placeholder Banner (replace sprite)", root, "plate", Image.Type.Sliced,
            UITheme.TagRust, false);
        CollectionUI.TL(banner.rectTransform, 48f, 160f, 1100f, 64f);
        TMP_Text bt = UIKit.CreateText(banner.transform, "Banner Text",
            "PLACEHOLDER SCREEN - no concept art yet. Layout reused from the Armory.",
            UITextStyle.Small, UITheme.Cream, TextAlignmentOptions.Center);
        UIKit.Stretch(bt.rectTransform, -8f);
        CollectionUI.AutoSize(bt, 14f, 22f);

        counterLabel = CollectionUI.TextRight(root, "Counter", "Boss drops found 0/0", UITextStyle.Subtitle, UITheme.Accent,
            TextAlignmentOptions.MidlineRight, 48f, 166f, 640f, 50f);

        // Grid
        CollectionUI.Panel(root, "Grid Frame (replace sprite)", 40f, 252f, 1270f, 800f);
        RectTransform content;
        CollectionUI.Scroll(root, "Pack Scroll", 52f, 264f, 1246f, 776f, true, out content);
        packGrid = content;
        GridLayoutGroup gl = content.gameObject.AddComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(224f, 168f);
        gl.spacing = new Vector2(16f, 16f);
        gl.padding = new RectOffset(8, 8, 8, 8);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = 5;
        gl.childAlignment = TextAnchor.UpperLeft;

        // Detail
        CollectionUI.Panel(root, "Detail Frame (replace sprite)", 1330f, 252f, 542f, 800f);
        detailName = CollectionUI.Text(root, "Detail Name", "Pack", UITextStyle.Subtitle, UITheme.Cream,
            TextAlignmentOptions.MidlineLeft, 1364f, 286f, 480f, 64f);
        CollectionUI.AutoSize(detailName, 22f, 32f);
        CollectionUI.Divider(root, "Detail Divider A", 1364f, 360f, 478f, UITheme.Gold);
        detailFloor = CollectionUI.Text(root, "Detail Floor", "Floor 1", UITextStyle.Body, UITheme.Accent,
            TextAlignmentOptions.MidlineLeft, 1364f, 374f, 480f, 48f);
        CollectionUI.Divider(root, "Detail Divider B", 1364f, 432f, 478f, UITheme.Gold);
        CollectionUI.Text(root, "Enemies Heading", "Enemies", UITextStyle.Label, UITheme.Gold,
            TextAlignmentOptions.MidlineLeft, 1364f, 450f, 480f, 40f);
        detailEnemies = CollectionUI.Text(root, "Detail Enemies", "???", UITextStyle.Body, UITheme.Cream,
            TextAlignmentOptions.TopLeft, 1364f, 498f, 480f, 200f);
        CollectionUI.Text(root, "Boss Heading", "Boss drop", UITextStyle.Label, UITheme.Gold,
            TextAlignmentOptions.MidlineLeft, 1364f, 720f, 480f, 40f);
        detailBoss = CollectionUI.Text(root, "Detail Boss", "???", UITextStyle.Body, UITheme.Cream,
            TextAlignmentOptions.TopLeft, 1364f, 768f, 480f, 120f);
        CollectionUI.Text(root, "Placeholder Tag", "PLACEHOLDER", UITextStyle.Small, UITheme.TagRust,
            TextAlignmentOptions.MidlineLeft, 1364f, 990f, 480f, 40f);

        // Cell template
        RectTransform c = CollectionUI.Card(templates, "Pack Cell Template", 224f, 168f);
        CollectionUI.Text(c, "Floor", "FLOOR 1", UITextStyle.Small, UITheme.Accent, TextAlignmentOptions.Center, 0f, 22f, 224f, 36f);
        CollectionUI.Solid(c, "Floor Rule", 62f, 62f, 100f, 2f, UITheme.Gold);
        TMP_Text nm = CollectionUI.Text(c, "Name", "Pack", UITextStyle.Small, UITheme.Cream, TextAlignmentOptions.Top, 8f, 74f, 208f, 80f);
        CollectionUI.AutoSize(nm, 14f, 24f);
        packCellTemplate = c;
    }

    protected override void Wire() { }

    protected override void Populate()
    {
        packs = CollectionData.Packs();
        selected = 0;
        CollectionUI.ClearChildren(packGrid);

        int found = 0;
        for (int i = 0; i < packs.Count; i++)
        {
            CollectionData.PackInfo p = packs[i];
            if (p.bossFound) found++;
            RectTransform cell = Spawn(packCellTemplate, packGrid);
            CollectionUI.SetText(cell, "Floor", "FLOOR " + p.floor);
            CollectionUI.SetText(cell, "Name", p.name);
            int index = i;
            cell.GetComponent<Button>().onClick.AddListener(() => { selected = index; RefreshSelection(); });
        }
        counterLabel.text = "Boss drops found " + found + "/" + packs.Count;
        RefreshSelection();
    }

    void RefreshSelection()
    {
        for (int i = 0; i < packGrid.childCount; i++)
        {
            Transform t = packGrid.GetChild(i);
            if (!t.gameObject.activeSelf) continue;
            HoverGlow hg = t.GetComponent<HoverGlow>();
            if (hg != null) hg.SetHold(t.GetSiblingIndex() == selected ? 0.9f : 0f);
        }

        if (packs.Count == 0)
        {
            detailName.text = "No packs";
            detailFloor.text = "";
            detailEnemies.text = "";
            detailBoss.text = "";
            return;
        }
        if (selected >= packs.Count) selected = 0;
        CollectionData.PackInfo p = packs[selected];
        detailName.text = p.name;
        detailFloor.text = "Floor " + p.floor;

        string enemies = "";
        for (int i = 0; i < UnknownEnemyLines; i++) enemies += (i > 0 ? "\n" : "") + "???";
        detailEnemies.text = enemies;

        detailBoss.text = p.bossFound ? p.bossEquipName : "???";
    }
}
