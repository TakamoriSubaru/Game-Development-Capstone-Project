using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 640x96 indicator with 4 nodes: 0 STORY, 1 LOBBY, 2 DUNGEON, 3 EXIT.
/// SetActive(stop) makes that node glow and grow.
/// Art slots: "Line (replace sprite)", "Node N (replace sprite)", "Node Glow N (replace sprite)".
/// </summary>
public class CompassIndicator : MonoBehaviour
{
    public Image[] nodes = new Image[4];
    public Image[] glows = new Image[4];
    public TMP_Text[] labels = new TMP_Text[4];
    public int startStop = 1;

    int active = -1;
    float[] amount = new float[4];   // 0 inactive .. 1 active (animated)

    void Awake()
    {
        SetActive(startStop);
        for (int i = 0; i < 4; i++) amount[i] = (i == active) ? 1f : 0f;
        Apply();
    }

    // 0 Story, 1 Lobby, 2 Dungeon, 3 Exit
    public void SetActive(int stop)
    {
        active = Mathf.Clamp(stop, 0, 3);
        if (amount == null || amount.Length != 4) amount = new float[4];
    }

    void Update()
    {
        bool changed = false;
        for (int i = 0; i < 4; i++)
        {
            float target = (i == active) ? 1f : 0f;
            if (!Mathf.Approximately(amount[i], target))
            {
                amount[i] = Mathf.MoveTowards(amount[i], target, Time.unscaledDeltaTime * 6f);
                changed = true;
            }
        }
        if (changed || active >= 0) Apply();
    }

    void Apply()
    {
        for (int i = 0; i < 4; i++)
        {
            float a = amount[i];
            float pulse = (i == active) ? 0.9f + 0.1f * Mathf.Sin(Time.unscaledTime * 3f) : 1f;
            if (nodes != null && i < nodes.Length && nodes[i] != null)
                nodes[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.35f, a);
            if (glows != null && i < glows.Length && glows[i] != null)
            {
                Color c = UITheme.Accent;
                c.a = a * 0.9f * pulse;
                glows[i].color = c;
            }
            if (labels != null && i < labels.Length && labels[i] != null)
                labels[i].color = Color.Lerp(UITheme.Cream, UITheme.Accent, a);
        }
    }
}
