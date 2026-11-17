using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// The Almanac: the book of the crew, opened from the scroll barrel in the lobby.
/// Left: one button per character. Right: portrait medallion, name, title, role, stamina pips,
/// stats, story, the four moves, and the trait / talent. Everything is read from CharacterDatabase,
/// so new characters show up by themselves.
/// Built from the UI Kit when first opened (no scene setup). BACK or Esc closes it.
/// LobbyMenu calls it through LobbyMenu.AlmanacOpener, which this file registers on startup.
/// </summary>
public class AlmanacPanel : MonoBehaviour
{
    // ---- layout (canvas pixels, reference 1920x1080, origin = panel centre) ----
    static readonly Vector2 PanelSize = new Vector2(1560f, 880f);
    const float ListX = -700f;           // centre of the character buttons
    const float ListTop = 255f;
    const float ListStep = 92f;
    static readonly Vector2 ListButton = new Vector2(310f, 76f);
    const float DividerX = -520f;
    const float RightLeft = -480f;       // left edge of the detail area
    const float RightWidth = 1200f;

    static readonly Vector2 C = new Vector2(0.5f, 0.5f);
    static readonly Vector2 MidLeft = new Vector2(0f, 0.5f);

    static AlmanacPanel instance;

    // Registers the opener so the barrel in the lobby works with no scene setup.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        LobbyMenu.AlmanacOpener = Open;
    }

    public static void Open(Action onClose)
    {
        if (instance == null) instance = Build();
        instance.Show(onClose);
    }

    // ------------------------------------------------------------------
    CharacterData[] chars;
    ThemedButton[] tabs;
    Image medallionTint;
    TMP_Text medallionLetter, nameText, titleText, roleText, statsText, bioText, extraText, staminaLabel;
    Image[] pips = new Image[4];
    TMP_Text[] moveTag = new TMP_Text[4], moveName = new TMP_Text[4], moveDesc = new TMP_Text[4];
    GameObject[] moveRow = new GameObject[4];
    CanvasGroup group;
    RectTransform panelRect;
    Action closeCallback;
    int selected = -1;
    Coroutine anim;

    static AlmanacPanel Build()
    {
        Canvas canvas = UIKit.CreateOverlayCanvas("Almanac Canvas", 400);
        AlmanacPanel a = canvas.gameObject.AddComponent<AlmanacPanel>();
        a.BuildUI(canvas.transform);
        canvas.gameObject.SetActive(false);
        return a;
    }

    void BuildUI(Transform root)
    {
        chars = CharacterDatabase.All;
        group = gameObject.AddComponent<CanvasGroup>();

        Image dim = UIKit.NewImage("Dim", root, null, Image.Type.Simple, UITheme.Dim, true);
        UIKit.Stretch(dim.rectTransform);

        RectTransform panel = UIKit.NewRect("Panel", root);
        UIKit.Place(panel, C, C, Vector2.zero, PanelSize);
        panelRect = panel;
        Image frame = UIKit.NewImage("Frame (replace sprite)", panel, "pa_panel", Image.Type.Sliced, Color.white, true);
        UIKit.Stretch(frame.rectTransform);

        // header ribbon
        RectTransform hp = UIKit.NewRect("Header Ribbon", panel);
        UIKit.Place(hp, new Vector2(0.5f, 1f), C, Vector2.zero, new Vector2(460f, 76f));
        Image plate = UIKit.NewImage("Header Ribbon (replace sprite)", hp, "pa_plate", Image.Type.Sliced, Color.white, false);
        UIKit.Stretch(plate.rectTransform);
        TMP_Text title = UIKit.CreateText(hp, "Title", "ALMANAC", UITextStyle.TitleCaps, UITheme.Paper, TextAlignmentOptions.Center);
        title.fontSize = 44f;
        UIKit.Stretch(title.rectTransform);

        // character buttons
        tabs = new ThemedButton[chars.Length];
        for (int i = 0; i < chars.Length; i++)
        {
            int idx = i;
            ThemedButton b = UIKit.CreateButton(panel, chars[i].name, ThemedButtonStyle.Secondary, ListButton);
            UIKit.Place((RectTransform)b.transform, C, C, new Vector2(ListX, ListTop - i * ListStep), ListButton);
            b.onClick.AddListener(() => Select(idx));
            tabs[i] = b;
        }

        // vertical divider
        Image rule = UIKit.NewImage("Divider", panel, null, Image.Type.Simple, new Color(UITheme.Ink.r, UITheme.Ink.g, UITheme.Ink.b, 0.35f), false);
        UIKit.Place(rule.rectTransform, C, C, new Vector2(DividerX, -10f), new Vector2(2f, 700f));

        // medallion: gold ring + coloured disc + initial
        float my = 262f;
        Image ring = UIKit.NewImage("Medallion Ring (replace sprite)", panel, "pa_knob", Image.Type.Simple, Color.white, false);
        ring.preserveAspect = true;
        UIKit.Place(ring.rectTransform, C, C, new Vector2(RightLeft + 70f, my), new Vector2(132f, 132f));
        medallionTint = UIKit.NewImage("Medallion Disc", panel, "circle", Image.Type.Simple, Color.gray, false);
        medallionTint.preserveAspect = true;
        UIKit.Place(medallionTint.rectTransform, C, C, new Vector2(RightLeft + 70f, my), new Vector2(92f, 92f));
        medallionLetter = UIKit.CreateText(panel, "Medallion Letter", "O", UITextStyle.TitleCaps, UITheme.Paper, TextAlignmentOptions.Center);
        medallionLetter.fontSize = 52f;
        UIKit.Place(medallionLetter.rectTransform, C, C, new Vector2(RightLeft + 70f, my + 2f), new Vector2(92f, 92f));

        float tx = RightLeft + 160f;
        nameText = MakeText(panel, "Name", UITextStyle.TitleCaps, 50f, UITheme.Ink, TextAlignmentOptions.MidlineLeft,
                        new Vector2(tx, my + 26f), new Vector2(760f, 64f));
        titleText = MakeText(panel, "Title", UITextStyle.Subtitle, 28f, UITheme.TerracottaDark, TextAlignmentOptions.MidlineLeft,
                         new Vector2(tx, my - 24f), new Vector2(760f, 40f));
        titleText.fontStyle = FontStyles.Italic;

        // role + stamina (right side of the header)
        roleText = MakeText(panel, "Role", UITextStyle.Label, 26f, UITheme.Olive, TextAlignmentOptions.MidlineRight,
                        new Vector2(RightLeft + RightWidth - 330f, my + 26f), new Vector2(330f, 44f));
        staminaLabel = MakeText(panel, "Stamina Label", UITextStyle.Label, 20f, UITheme.Ink, TextAlignmentOptions.MidlineRight,
                            new Vector2(RightLeft + RightWidth - 330f, my - 22f), new Vector2(150f, 36f));
        staminaLabel.text = "STAMINA";
        for (int i = 0; i < pips.Length; i++)
        {
            Image pip = UIKit.NewImage("Stamina Pip " + (i + 1), panel, "pa_knob", Image.Type.Simple, Color.white, false);
            pip.preserveAspect = true;
            UIKit.Place(pip.rectTransform, C, C, new Vector2(RightLeft + RightWidth - 168f + i * 42f, my - 22f), new Vector2(34f, 34f));
            pips[i] = pip;
        }

        statsText = MakeText(panel, "Stats", UITextStyle.Label, 28f, UITheme.Ink, TextAlignmentOptions.MidlineLeft,
                         new Vector2(RightLeft, 150f), new Vector2(RightWidth, 44f));
        bioText = MakeText(panel, "Bio", UITextStyle.Body, 26f, UITheme.Ink, TextAlignmentOptions.TopLeft,
                       new Vector2(RightLeft, 85f), new Vector2(RightWidth, 110f));
        bioText.rectTransform.pivot = new Vector2(0f, 1f);
        bioText.rectTransform.anchoredPosition = new Vector2(RightLeft, 118f);

        TMP_Text movesHead = MakeText(panel, "Moves Header", UITextStyle.Label, 28f, UITheme.TerracottaDark, TextAlignmentOptions.MidlineLeft,
                                  new Vector2(RightLeft, -22f), new Vector2(400f, 40f));
        movesHead.text = "MOVES";
        Image headRule = UIKit.NewImage("Moves Rule", panel, null, Image.Type.Simple, new Color(UITheme.Ink.r, UITheme.Ink.g, UITheme.Ink.b, 0.30f), false);
        UIKit.Place(headRule.rectTransform, MidLeft, MidLeft, new Vector2(RightLeft + 150f, -22f), new Vector2(RightWidth - 150f, 2f));

        for (int i = 0; i < 4; i++)
        {
            float y = -78f - i * 64f;
            RectTransform row = UIKit.NewRect("Move Row " + (i + 1), panel);
            UIKit.Place(row, C, C, Vector2.zero, Vector2.zero);
            moveRow[i] = row.gameObject;

            Image tagBg = UIKit.NewImage("Tag", row, "pa_plate", Image.Type.Sliced, Color.white, false);
            UIKit.Place(tagBg.rectTransform, C, MidLeft, new Vector2(RightLeft, y), new Vector2(120f, 40f));
            moveTag[i] = UIKit.CreateText(tagBg.rectTransform, "Tag Label", "ROLE", UITextStyle.Label, UITheme.Paper, TextAlignmentOptions.Center);
            moveTag[i].fontSize = 18f;
            UIKit.Stretch(moveTag[i].rectTransform);

            moveName[i] = MakeText(row, "Move Name", UITextStyle.Label, 24f, UITheme.Ink, TextAlignmentOptions.MidlineLeft,
                               new Vector2(RightLeft + 140f, y), new Vector2(290f, 48f));
            moveName[i].enableAutoSizing = true;
            moveName[i].fontSizeMin = 16f;
            moveName[i].fontSizeMax = 24f;

            moveDesc[i] = MakeText(row, "Move Description", UITextStyle.Small, 23f, UITheme.Ink, TextAlignmentOptions.MidlineLeft,
                               new Vector2(RightLeft + 450f, y), new Vector2(RightWidth - 450f, 56f));
            moveDesc[i].enableAutoSizing = true;
            moveDesc[i].fontSizeMin = 16f;
            moveDesc[i].fontSizeMax = 23f;
        }

        extraText = MakeText(panel, "Trait And Talent", UITextStyle.Small, 22f, UITheme.Ink, TextAlignmentOptions.TopLeft,
                         new Vector2(RightLeft, -330f), new Vector2(RightWidth, 90f));
        extraText.rectTransform.pivot = new Vector2(0f, 1f);
        extraText.rectTransform.anchoredPosition = new Vector2(RightLeft, -318f);

        Vector2 backSize = new Vector2(252f, 84f);
        ThemedButton back = UIKit.CreateButton(panel, "BACK", ThemedButtonStyle.Primary, backSize);
        UIKit.Place((RectTransform)back.transform, C, C, new Vector2(ListX, -330f), backSize);
        back.onClick.AddListener(Close);
    }

    // MakeText: text with a left-middle pivot so 'pos' is its left edge
    static TMP_Text MakeText(Transform parent, string name, UITextStyle style, float size, Color color,
                         TextAlignmentOptions align, Vector2 pos, Vector2 box)
    {
        TMP_Text t = UIKit.CreateText(parent, name, "", style, color, align);
        t.fontSize = size;
        UIKit.Place(t.rectTransform, C, MidLeft, pos, box);
        return t;
    }

    // ------------------------------------------------------------------
    // Show / select / close
    // ------------------------------------------------------------------
    void Show(Action onClose)
    {
        closeCallback = onClose;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Select(selected >= 0 ? selected : 0);
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        const float d = 0.18f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / d));
            group.alpha = k;
            panelRect.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, k);
            yield return null;
        }
        group.alpha = 1f;
        panelRect.localScale = Vector3.one;
        anim = null;
    }

    void Update()
    {
        if (EscapePressed()) Close();
    }

    static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    public void Close()
    {
        if (!gameObject.activeSelf) return;
        gameObject.SetActive(false);
        Action cb = closeCallback;
        closeCallback = null;
        if (cb != null) cb();
    }

    void Select(int i)
    {
        if (chars == null || chars.Length == 0) return;
        selected = Mathf.Clamp(i, 0, chars.Length - 1);
        for (int k = 0; k < tabs.Length; k++)
            tabs[k].SetStyle(k == selected ? ThemedButtonStyle.Primary : ThemedButtonStyle.Secondary);

        CharacterData c = chars[selected];
        nameText.text = c.name;
        titleText.text = c.title;
        roleText.text = CharacterDatabase.RoleLabel(c.role);
        medallionTint.color = c.color;
        medallionLetter.text = string.IsNullOrEmpty(c.name) ? "?" : c.name.Substring(0, 1).ToUpperInvariant();
        bioText.text = c.bio;

        for (int p = 0; p < pips.Length; p++)
        {
            bool shown = p < Mathf.Max(c.stamina, 3);
            pips[p].gameObject.SetActive(shown);
            pips[p].color = p < c.stamina ? Color.white : new Color(1f, 1f, 1f, 0.22f);
        }

        statsText.text = StatLine(c);

        SkillData[] moves = null;
        try { moves = c.moves; } catch (Exception) { moves = null; }
        if (moves == null || moves.Length == 0) moves = c.skills;
        for (int m = 0; m < 4; m++)
        {
            bool has = moves != null && m < moves.Length && moves[m] != null;
            moveRow[m].SetActive(has);
            if (!has) continue;
            moveTag[m].text = string.IsNullOrEmpty(moves[m].tag) ? "MOVE" : moves[m].tag;
            moveName[m].text = moves[m].name;
            string d = moves[m].description;
            if (!string.IsNullOrEmpty(moves[m].target)) d += "  (" + moves[m].target + ")";
            moveDesc[m].text = d;
        }

        extraText.text = ExtraLines(c);
    }

    static string StatLine(CharacterData c)
    {
        try
        {
            return "HP " + c.hp + "      ATK " + c.atk + "      DEF " + c.def + "      SPD " + c.spd;
        }
        catch (Exception)
        {
            return "";
        }
    }

    static string ExtraLines(CharacterData c)
    {
        StringBuilder sb = new StringBuilder();
        try
        {
            if (!string.IsNullOrEmpty(c.traitName))
                sb.Append("<b>Trait: ").Append(c.traitName).Append("</b>  ").Append(c.traitText);
            if (!string.IsNullOrEmpty(c.talentName))
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("<b>Talent: ").Append(c.talentName).Append("</b>  ").Append(c.talentText);
            }
        }
        catch (Exception) { }
        return sb.ToString();
    }
}
