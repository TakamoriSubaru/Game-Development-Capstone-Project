using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Lobby HUD in the "Night Sea and Bronze" look, built from the UI Kit when the scene plays:
///  - compass indicator (STORY / LOBBY / DUNGEON / EXIT) at the top
///  - edge chevron buttons to move left / right
///  - hover ribbon above the hotspot under the mouse
///  - pulsing "!" markers on things you can click
///  - dialogue box with name plate (Companion)
///  - Settings panel and the "Leave the voyage?" confirm
/// The old placeholder UI that SceneBuilder put on this object is hidden. No scene rebuild is needed.
/// The public methods used by other scripts (SetTooltip, ShowSpeech, SetSpeechLine, HideSpeech) are unchanged.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    public LobbyCameraRig rig;

    // Legacy fields: only here because SceneBuilder still assigns them. Not used any more.
    [HideInInspector] public Button leftButton, rightButton;
    [HideInInspector] public Text stopLabel, tooltip;
    [HideInInspector] public GameObject speechPanel;
    [HideInInspector] public Text speakerText, lineText;

    public static LobbyUI Instance { get; private set; }

    static readonly string[] ShortNames = { "STORY", "LOBBY", "DUNGEON", "EXIT" };
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);

    class MarkerEntry
    {
        public Hotspot hotspot;
        public Collider2D col;
        public HotspotMarker marker;
        public RectTransform rt;
    }

    Canvas ui;
    RectTransform canvasRect;
    Camera cam;
    CompassIndicator compass;
    EdgeChevronButton leftBtn, rightBtn;
    HoverRibbon ribbon;
    DialogueBox dialogue;
    readonly List<MarkerEntry> markers = new List<MarkerEntry>();

    string hoverText = "";
    Vector3 hoverWorld;

    void Awake()
    {
        Instance = this;

        // Hide the old placeholder UI that lives on this object.
        Canvas old = GetComponent<Canvas>();
        if (old != null) old.enabled = false;
        for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);

        BuildUI();
    }

    void OnEnable()
    {
        if (rig != null) rig.StopChanged += OnStop;
    }

    void OnDisable()
    {
        if (rig != null) rig.StopChanged -= OnStop;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        GameInput.EnsureEventSystem();
        cam = rig.GetComponent<Camera>();
        BuildMarkers();
        OnStop(rig.Index);
    }

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------
    void BuildUI()
    {
        ui = UIKit.CreateOverlayCanvas("Lobby HUD Canvas", 10);
        canvasRect = (RectTransform)ui.transform;
        Transform root = ui.transform;

        compass = UIKit.CreateCompass(root);
        UIKit.Place((RectTransform)compass.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -14f), UITheme.CompassSize);

        leftBtn = UIKit.CreateEdgeChevron(root, "", false);
        UIKit.Place((RectTransform)leftBtn.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 60f), UITheme.EdgeChevronSize);
        leftBtn.Setup("", false, () => rig.Step(-1));
        FixEdgeLabel(leftBtn, false);

        rightBtn = UIKit.CreateEdgeChevron(root, "", true);
        UIKit.Place((RectTransform)rightBtn.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(0f, 60f), UITheme.EdgeChevronSize);
        rightBtn.Setup("", true, () => rig.Step(1));
        FixEdgeLabel(rightBtn, true);

        dialogue = UIKit.CreateDialogueBox(root);
        UIKit.Place((RectTransform)dialogue.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 40f), UITheme.DialogueBoxSize);
        dialogue.gameObject.SetActive(false);

        ribbon = UIKit.CreateHoverRibbon(root);
        ribbon.Hide();
    }

    // Wider text area and no wrapping, so "DUNGEON" stays on one line (the chevron side keeps its margin).
    static void FixEdgeLabel(EdgeChevronButton b, bool pointsRight)
    {
        TMP_Text l = b.label;
        if (l == null) return;
        RectTransform r = l.rectTransform;
        r.offsetMin = new Vector2(pointsRight ? 22f : 66f, 0f);
        r.offsetMax = new Vector2(pointsRight ? -66f : -22f, 0f);
        l.enableAutoSizing = true;
        l.fontSizeMin = 16f;
        l.fontSizeMax = 26f;
    }

    void BuildMarkers()
    {
        Hotspot[] all = FindObjectsByType<Hotspot>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Collider2D col = all[i].GetComponent<Collider2D>();
            if (col == null) col = all[i].GetComponentInChildren<Collider2D>();
            if (col == null) continue;

            HotspotMarker m = UIKit.CreateHotspotMarker(ui.transform);
            if (m.marker != null) m.marker.raycastTarget = false;   // markers must never block clicks
            m.SetPulsing(true);
            RectTransform rt = (RectTransform)m.transform;
            UIKit.Place(rt, C, C, Vector2.zero, new Vector2(UITheme.MarkerSize, UITheme.MarkerSize));
            m.gameObject.SetActive(false);

            MarkerEntry e = new MarkerEntry();
            e.hotspot = all[i];
            e.col = col;
            e.marker = m;
            e.rt = rt;
            markers.Add(e);
        }
        // Dialogue box and ribbon stay on top of the markers.
        if (dialogue != null) dialogue.transform.SetAsLastSibling();
        if (ribbon != null) ribbon.transform.SetAsLastSibling();
    }

    // ------------------------------------------------------------------
    // Update
    // ------------------------------------------------------------------
    void Update()
    {
        bool free = !rig.InputLocked;
        int idx = rig.Index;

        // The room is a circle: both buttons always work (Exit to Story and back).
        leftBtn.gameObject.SetActive(free);
        rightBtn.gameObject.SetActive(free);

        // Hover ribbon follows the hotspot (the camera may still be moving).
        if (free && hoverText.Length > 0 && cam != null) ribbon.Show(hoverText, hoverWorld);
        else ribbon.Hide();

        UpdateMarkers(free);
    }

    void UpdateMarkers(bool free)
    {
        bool speaking = dialogue != null && dialogue.gameObject.activeSelf;
        for (int i = 0; i < markers.Count; i++)
        {
            MarkerEntry e = markers[i];
            if (e.hotspot == null || e.col == null) { e.marker.gameObject.SetActive(false); continue; }

            bool show = free && !speaking && !e.hotspot.IsHovered && cam != null;
            if (show)
            {
                Vector3 w = e.col.bounds.center;
                Vector3 vp = cam.WorldToViewportPoint(w);
                show = vp.z > 0f && vp.x > 0.06f && vp.x < 0.94f && vp.y > 0.08f && vp.y < 0.92f;
                if (show)
                {
                    Vector2 local;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, cam.WorldToScreenPoint(w), null, out local);
                    e.rt.anchoredPosition = local;
                }
            }
            if (e.marker.gameObject.activeSelf != show) e.marker.gameObject.SetActive(show);
        }
    }

    void OnStop(int i)
    {
        if (compass == null) return;
        compass.SetActive(i);
        int n = LobbyCameraRig.StopCount;
        leftBtn.SetLabel(ShortNames[(i - 1 + n) % n]);
        rightBtn.SetLabel(ShortNames[(i + 1) % n]);
    }

    // ------------------------------------------------------------------
    // Public API (used by LobbyHotspotInput, CharacterTalker and the hotspots)
    // ------------------------------------------------------------------
    public void SetTooltip(string text) { hoverText = text ?? ""; }

    // Text plus the world point (top of the hotspot) where the ribbon's pointer should land.
    public void SetTooltip(string text, Vector3 worldPoint)
    {
        hoverText = text ?? "";
        hoverWorld = worldPoint;
    }

    public void ShowSpeech(string speaker, string line)
    {
        dialogue.Show(speaker, "");
        SetSpeechLine(line);
    }

    public void SetSpeechLine(string line)
    {
        dialogue.bodyLabel.text = line;
        dialogue.bodyLabel.maxVisibleCharacters = 99999;   // CharacterTalker types the line itself
    }

    public void HideSpeech()
    {
        dialogue.Hide();
    }

    public void OpenSettings()
    {
        if (rig.InputLocked) return;
        rig.InputLocked = true;
        SettingsPanel.GetOrCreate().Open(() => { rig.InputLocked = false; });
    }

    // "Leave the voyage?" Stay / Leave.
    public void AskLeave(Action onLeave, Action onStay)
    {
        ConfirmPanel.GetOrCreate().Show("Leave the voyage?", "Stay", "Leave", onLeave, onStay);
    }
}