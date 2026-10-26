using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Remastered Lobby HUD, built from the UI Kit at runtime (no scene wiring except 'rig'):
///   top compass (Story / Lobby / Dungeon / Exit), left and right edge chevron buttons,
///   dialogue box with name plate, hover ribbon, "!" markers over hotspots, version label.
/// Markers: every Hotspot that has a child object named "Marker Anchor" gets a "!" marker there.
/// Old public members still work: SetTooltip, ShowSpeech, SetSpeechLine, HideSpeech.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    public LobbyCameraRig rig;
    public string versionText = "v0.1";

    public event Action SpeechContinued;
    public bool IsSpeechTyping { get { return dialogue != null && dialogue.IsTyping; } }
    public bool IsSpeechOpen { get { return dialogue != null && dialogue.gameObject.activeSelf; } }

    static readonly string[] ShortNames = { "STORY", "LOBBY", "DUNGEON", "EXIT" };

    class Binding
    {
        public Hotspot hotspot;
        public Transform anchor;
        public HotspotMarker marker;
    }

    Canvas canvas;
    RectTransform canvasRect;
    Camera cam;
    DialogueBox dialogue;
    HoverRibbon ribbon;
    CompassIndicator compass;
    EdgeChevronButton leftBtn, rightBtn;
    readonly List<Binding> bindings = new List<Binding>();
    Hotspot hovered;

    void Awake()
    {
        canvas = UIKit.CreateOverlayCanvas("Lobby Canvas", 10);
        canvasRect = (RectTransform)canvas.transform;
        Transform root = canvas.transform;

        compass = UIKit.CreateCompass(root);
        UIKit.Place((RectTransform)compass.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -8f), UITheme.CompassSize);

        leftBtn = UIKit.CreateEdgeChevron(root, "STORY", false);
        UIKit.Place((RectTransform)leftBtn.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 30f), UITheme.EdgeChevronSize);
        rightBtn = UIKit.CreateEdgeChevron(root, "DUNGEON", true);
        UIKit.Place((RectTransform)rightBtn.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(0f, 30f), UITheme.EdgeChevronSize);

        ribbon = UIKit.CreateHoverRibbon(root);
        ribbon.Hide();

        dialogue = UIKit.CreateDialogueBox(root);
        UIKit.Place((RectTransform)dialogue.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 36f), UITheme.DialogueBoxSize);
        dialogue.OnContinue += HandleContinue;
        dialogue.Hide();

        TMP_Text ver = UIKit.CreateText(root, "Version", versionText, UITextStyle.Small,
                                        new Color(UITheme.Cream.r, UITheme.Cream.g, UITheme.Cream.b, 0.7f),
                                        TextAlignmentOptions.BottomLeft);
        UIKit.Place(ver.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 14f), new Vector2(200f, 40f));
    }

    void OnEnable() { if (rig != null) rig.StopChanged += OnStop; }
    void OnDisable() { if (rig != null) rig.StopChanged -= OnStop; }

    void Start()
    {
        cam = rig.GetComponent<Camera>();
        BuildMarkers();
        LobbyMenu.Create(rig);
        OnStop(rig.Index);
    }

    void BuildMarkers()
    {
        Hotspot[] all = FindObjectsByType<Hotspot>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Transform a = all[i].transform.Find("Marker Anchor");
            if (a == null) continue;
            HotspotMarker m = UIKit.CreateHotspotMarker(canvas.transform);
            m.marker.raycastTarget = false;     // must not block the hotspot under it
            m.gameObject.SetActive(false);
            Binding b = new Binding();
            b.hotspot = all[i];
            b.anchor = a;
            b.marker = m;
            bindings.Add(b);
        }
    }

    void OnStop(int i)
    {
        compass.SetActive(i);
        if (i > 0) leftBtn.Setup(ShortNames[i - 1], false, () => rig.Step(-1));
        if (i < LobbyCameraRig.StopCount - 1) rightBtn.Setup(ShortNames[i + 1], true, () => rig.Step(1));
    }

    void LateUpdate()
    {
        if (rig == null || cam == null) return;
        bool free = !rig.InputLocked;
        compass.gameObject.SetActive(free);
        leftBtn.gameObject.SetActive(free && rig.Index > 0);
        rightBtn.gameObject.SetActive(free && rig.Index < LobbyCameraRig.StopCount - 1);

        for (int i = 0; i < bindings.Count; i++)
        {
            Binding b = bindings[i];
            Vector3 sp = cam.WorldToScreenPoint(b.anchor.position);
            bool on = free && sp.z > 0f && sp.x > -60f && sp.x < Screen.width + 60f;
            if (b.marker.gameObject.activeSelf != on) b.marker.gameObject.SetActive(on);
            if (!on) continue;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(sp.x, sp.y), null, out local);
            ((RectTransform)b.marker.transform).anchoredPosition = local;
        }

        if (!free) ribbon.Hide();
        else if (hovered != null) PlaceRibbon();
    }

    // ------------------------------------------------------------ hover (called by LobbyHotspotInput)
    public void SetHover(Hotspot h)
    {
        hovered = h;
        for (int i = 0; i < bindings.Count; i++) bindings[i].marker.SetPulsing(h != null && bindings[i].hotspot == h);
        if (h == null) ribbon.Hide(); else PlaceRibbon();
    }

    void PlaceRibbon()
    {
        string text = hovered.Label.ToUpperInvariant();
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].hotspot != hovered) continue;
            Vector3 sp = cam.WorldToScreenPoint(bindings[i].anchor.position);
            ribbon.ShowAtScreen(text, new Vector2(sp.x, sp.y + 30f * canvas.scaleFactor));
            return;
        }
        ribbon.ShowAtScreen(text, GameInput.PointerPosition);
    }

    // Old API: plain text at the pointer.
    public void SetTooltip(string text)
    {
        if (string.IsNullOrEmpty(text)) { ribbon.Hide(); return; }
        ribbon.ShowAtScreen(text.ToUpperInvariant(), GameInput.PointerPosition);
    }

    // ------------------------------------------------------------ speech
    public void ShowSpeech(string speaker, string line) { dialogue.Show(speaker, line); }
    public void SetSpeechLine(string line) { if (dialogue != null && dialogue.bodyLabel != null) dialogue.bodyLabel.text = line; }
    public void HideSpeech() { if (dialogue != null) dialogue.Hide(); }

    void HandleContinue()
    {
        if (SpeechContinued != null) SpeechContinued();
        else HideSpeech();
    }
}
