using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Achievements (concept 17): one row per achievement with reward tag (CHARACTER teal, COSTUME gold),
/// title, description, reward text and a progress bar x/y, or "NOT ACTIVE YET" when the target is 0.
/// Counter "Unlocked n/N". DEBUG buttons only show in the Editor and in development builds.
/// Scene: GameScenes.Achievements. BACK goes to GameScenes.DungeonHub.
/// </summary>
public class AchievementsScreen : CollectionScreenBase
{
    public TMP_Text counterLabel;
    public RectTransform listContent;
    public RectTransform rowTemplate;
    public RectTransform debugGroup;
    public ThemedButton clearPacksButton;
    public ThemedButton resetButton;

    protected override string ScreenTitle { get { return "Achievements"; } }

    protected override void BuildContent(Transform root)
    {
        CollectionUI.Text(root, "Subtitle", "Rewards: extra characters, costumes, emblems and titles.",
            UITextStyle.Small, UITheme.Cream, TextAlignmentOptions.MidlineLeft, 48f, 152f, 1200f, 40f);
        counterLabel = CollectionUI.TextRight(root, "Counter", "Unlocked 0/0", UITextStyle.Subtitle, UITheme.Accent,
            TextAlignmentOptions.MidlineRight, 48f, 148f, 500f, 44f);

        CollectionUI.Scroll(root, "Achievements Scroll", 24f, 205f, 1872f, 770f, true, out listContent);
        VerticalLayoutGroup v = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 14f;
        v.padding = new RectOffset(10, 10, 6, 16);
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = false;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        rowTemplate = BuildRowTemplate();

        // Debug buttons (hidden at runtime in release builds)
        debugGroup = UIKit.NewRect("Debug Group", root);
        CollectionUI.TL(debugGroup, 40f, 990f, 900f, 60f);
        clearPacksButton = CollectionUI.NewButton(debugGroup, "DEBUG: CLEAR ALL PACKS", ThemedButtonStyle.Secondary, 0f, 0f, 430f, 56f);
        resetButton = CollectionUI.NewButton(debugGroup, "DEBUG: RESET PROGRESS", ThemedButtonStyle.Secondary, 446f, 0f, 400f, 56f);
        clearPacksButton.label.fontSize = 20f;
        resetButton.label.fontSize = 20f;
    }

    RectTransform BuildRowTemplate()
    {
        RectTransform r = CollectionUI.Card(templates, "Achievement Row Template", 1822f, 112f);

        Image tag = UIKit.NewImage("Tag (replace sprite)", r, "plate", Image.Type.Sliced, CollectionUI.TealTag, false);
        CollectionUI.TL(tag.rectTransform, 24f, 16f, 220f, 80f);
        Image tagIcon = UIKit.NewImage("Tag Icon (replace sprite)", tag.transform, "circle", Image.Type.Simple, UITheme.Cream, false);
        CollectionUI.TL(tagIcon.rectTransform, 16f, 26f, 28f, 28f);
        TMP_Text tl = UIKit.CreateText(tag.transform, "Tag Label", "CHARACTER", UITextStyle.Label, UITheme.Cream, TextAlignmentOptions.Center);
        CollectionUI.TL(tl.rectTransform, 48f, 0f, 168f, 80f);
        CollectionUI.AutoSize(tl, 12f, 20f);

        TMP_Text title = CollectionUI.Text(r, "Title", "Title", UITextStyle.Subtitle, UITheme.Cream,
            TextAlignmentOptions.MidlineLeft, 270f, 8f, 770f, 50f);
        CollectionUI.AutoSize(title, 22f, 34f);
        TMP_Text desc = CollectionUI.Text(r, "Description", "Description", UITextStyle.Small, UITheme.Cream,
            TextAlignmentOptions.TopLeft, 270f, 60f, 770f, 46f);
        CollectionUI.AutoSize(desc, 16f, 24f);

        CollectionUI.Solid(r, "Column Divider", 1060f, 16f, 2f, 80f, new Color(UITheme.Bronze.r, UITheme.Bronze.g, UITheme.Bronze.b, 0.7f));

        TMP_Text reward = CollectionUI.Text(r, "Reward", "Reward", UITextStyle.Small, UITheme.Cream,
            TextAlignmentOptions.MidlineLeft, 1090f, 12f, 700f, 40f);
        CollectionUI.AutoSize(reward, 16f, 24f);
        CollectionUI.Bar(r, "Progress Bar", 1090f, 66f, 520f, 18f, UITheme.SliderFill);
        CollectionUI.Text(r, "Count", "0/1", UITextStyle.Small, UITheme.Cream, TextAlignmentOptions.MidlineRight, 1626f, 50f, 160f, 40f);
        CollectionUI.Text(r, "Not Active", "NOT ACTIVE YET", UITextStyle.Small, UITheme.Disabled,
            TextAlignmentOptions.Center, 1560f, 20f, 230f, 72f);
        return r;
    }

    protected override void Wire()
    {
        clearPacksButton.onClick.RemoveAllListeners();
        clearPacksButton.onClick.AddListener(() =>
            ConfirmPanel.GetOrCreate().Show("DEBUG: mark every pack boss equipment as found?", "Cancel", "Clear packs", () =>
            {
                CollectionData.DebugClearAllPacks();
                Populate();
            }));

        resetButton.onClick.RemoveAllListeners();
        resetButton.onClick.AddListener(() =>
            ConfirmPanel.GetOrCreate().Show("DEBUG: reset ALL saved progress? This cannot be undone.", "Cancel", "Reset", () =>
            {
                CollectionData.DebugResetProgress();
                Populate();
            }));
    }

    protected override void Populate()
    {
        debugGroup.gameObject.SetActive(Application.isEditor || Debug.isDebugBuild);

        CollectionUI.ClearChildren(listContent);
        List<CollectionData.AchInfo> all = CollectionData.Achievements();
        int unlocked = 0;
        foreach (CollectionData.AchInfo a in all)
        {
            if (a.Done) unlocked++;
            RectTransform row = Spawn(rowTemplate, listContent);
            FillRow(row, a);
        }
        counterLabel.text = "Unlocked " + unlocked + "/" + all.Count;
    }

    static void FillRow(RectTransform row, CollectionData.AchInfo a)
    {
        // Tag
        string tagText = "REWARD";
        Color tagColor = UITheme.Blue;
        if (a.rewardType.Contains("char")) { tagText = "CHARACTER"; tagColor = CollectionUI.TealTag; }
        else if (a.rewardType.Contains("costume")) { tagText = "COSTUME"; tagColor = CollectionUI.GoldTag; }
        else if (a.rewardType.Length > 0) { tagText = a.rewardType.ToUpperInvariant(); }

        Image tag = CollectionUI.Find<Image>(row, "Tag (replace sprite)");
        if (tag != null) tag.color = tagColor;
        CollectionUI.SetText(row, "Tag (replace sprite)/Tag Label", tagText);

        CollectionUI.SetText(row, "Title", a.title);
        CollectionUI.SetText(row, "Description", a.description);
        CollectionUI.SetText(row, "Reward", a.rewardText);

        bool active = a.Active;
        CollectionUI.SetActive(row, "Progress Bar", active);
        CollectionUI.SetActive(row, "Count", active);
        CollectionUI.SetActive(row, "Not Active", !active);

        Image frame = CollectionUI.Find<Image>(row, "Frame (replace sprite)");
        if (frame != null) frame.color = active ? Color.white : new Color(0.78f, 0.82f, 0.90f, 1f);

        if (active)
        {
            int shown = Mathf.Min(a.progress, a.target);
            CollectionUI.SetBar(row.Find("Progress Bar"), (float)shown / a.target);
            CollectionUI.SetBarColor(row.Find("Progress Bar"), a.Done ? UITheme.Accent : UITheme.SliderFill);
            TMP_Text count = CollectionUI.Find<TMP_Text>(row, "Count");
            if (count != null)
            {
                count.text = shown + "/" + a.target;
                count.color = a.Done ? UITheme.Accent : UITheme.Cream;
            }
            if (a.Done && frame != null) frame.color = Color.Lerp(Color.white, UITheme.Gold, 0.35f);
        }

        // Achievement rows are not clickable.
        Button b = row.GetComponent<Button>();
        if (b != null) b.interactable = false;
        HoverGlow hg = row.GetComponent<HoverGlow>();
        if (hg != null) hg.enabled = false;
    }
}
