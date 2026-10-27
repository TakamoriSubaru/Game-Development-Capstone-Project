using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 640x96 indicator with 4 nodes: 0 STORY, 1 LOBBY, 2 DUNGEON, 3 EXIT.
/// SetActive(stop) moves the gold knob (Resources/UIKit/pa_knob.png) onto that node.
/// The knob slides smoothly from stop to stop, so it is clear where you are.
/// The other nodes stay as small dim dots, and the current label turns bold terracotta.
/// Art slots: "Line (replace sprite)", "Node N (replace sprite)", "Node Glow N (replace sprite)",
/// and "Position Marker (replace sprite)" (the knob, created here).
/// </summary>
public class CompassIndicator : MonoBehaviour
{
    public Image[] nodes = new Image[4];
    public Image[] glows = new Image[4];
    public TMP_Text[] labels = new TMP_Text[4];
    public int startStop = 1;

    // ---- look (change these to taste) ----
    const string MarkerSpriteName = "pa_knob";
    const float MarkerSize = 60f;        // knob size in UI pixels
    const float SlideSpeed = 12f;        // higher = the knob arrives faster
    static readonly Color InactiveNode = new Color(0.55f, 0.58f, 0.50f, 1f);   // dimmed dots
    static readonly Color ActiveHalo = new Color32(255, 170, 40, 255);          // soft glow behind the knob
    const float HaloStrength = 0.55f;
    static readonly Color ActiveLabel = new Color32(190, 60, 25, 255);          // deep terracotta red
    const float EdgeInset = 22f;         // moves STORY and EXIT inward so the corner ornaments do not cover them

    int active = -1;
    float[] amount = new float[4];       // 0 inactive .. 1 active (animated)
    RectTransform marker;
    float markerX;

    void Awake()
    {
        NudgeEdges();
        SetActive(startStop);
        for (int i = 0; i < 4; i++) amount[i] = (i == active) ? 1f : 0f;
        BuildMarker();
        Apply();
    }

    void BuildMarker()
    {
        GameObject go = new GameObject("Position Marker (replace sprite)", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        marker = (RectTransform)go.transform;
        marker.anchorMin = marker.anchorMax = new Vector2(0.5f, 0.5f);
        marker.pivot = new Vector2(0.5f, 0.5f);
        marker.sizeDelta = new Vector2(MarkerSize, MarkerSize);
        Image img = go.GetComponent<Image>();
        img.sprite = UIArt.Get(MarkerSpriteName);
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;
        marker.SetAsLastSibling();

        Vector2 p = TargetPosition();
        markerX = p.x;
        marker.anchoredPosition = p;
    }

    Vector2 TargetPosition()
    {
        if (nodes != null && active >= 0 && active < nodes.Length && nodes[active] != null)
            return nodes[active].rectTransform.anchoredPosition;
        return Vector2.zero;
    }

    void NudgeEdges()
    {
        MoveX(0, EdgeInset);
        MoveX(3, -EdgeInset);
    }

    void MoveX(int i, float dx)
    {
        Shift(nodes, i, dx);
        Shift(glows, i, dx);
        if (labels != null && i < labels.Length && labels[i] != null)
        {
            RectTransform r = labels[i].rectTransform;
            r.anchoredPosition += new Vector2(dx, 0f);
        }
    }

    static void Shift(Image[] arr, int i, float dx)
    {
        if (arr == null || i >= arr.Length || arr[i] == null) return;
        RectTransform r = arr[i].rectTransform;
        r.anchoredPosition += new Vector2(dx, 0f);
    }

    // 0 Story, 1 Lobby, 2 Dungeon, 3 Exit
    public void SetActive(int stop)
    {
        active = Mathf.Clamp(stop, 0, 3);
        if (amount == null || amount.Length != 4) amount = new float[4];
    }

    void Update()
    {
        for (int i = 0; i < 4; i++)
        {
            float target = (i == active) ? 1f : 0f;
            if (!Mathf.Approximately(amount[i], target))
                amount[i] = Mathf.MoveTowards(amount[i], target, Time.unscaledDeltaTime * 6f);
        }
        Apply();
        SlideMarker();
    }

    void SlideMarker()
    {
        if (marker == null) return;
        Vector2 target = TargetPosition();
        markerX = Mathf.Lerp(markerX, target.x, 1f - Mathf.Exp(-SlideSpeed * Time.unscaledDeltaTime));
        marker.anchoredPosition = new Vector2(markerX, target.y);
        float breathe = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 3f);
        marker.localScale = Vector3.one * breathe;
    }

    void Apply()
    {
        for (int i = 0; i < 4; i++)
        {
            float a = amount[i];
            if (nodes != null && i < nodes.Length && nodes[i] != null)
            {
                // the knob covers the active node, so the node itself fades out
                Color c = InactiveNode;
                c.a = 1f - a;
                nodes[i].color = c;
                nodes[i].rectTransform.localScale = Vector3.one;
            }
            if (glows != null && i < glows.Length && glows[i] != null)
            {
                Color c = ActiveHalo;
                c.a = a * HaloStrength;
                glows[i].color = c;
            }
            if (labels != null && i < labels.Length && labels[i] != null)
            {
                labels[i].color = Color.Lerp(UITheme.Ink, ActiveLabel, a);
                labels[i].fontStyle = (a > 0.5f) ? FontStyles.Bold : FontStyles.Normal;
                labels[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, a);
            }
        }
    }
}