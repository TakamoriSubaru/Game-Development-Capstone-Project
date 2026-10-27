using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OddyseusJourneysEnd.Battle
{
    /// <summary>
    /// Mengatur mode fullscreen untuk Party Battle UI dan memastikan HUD atas
    /// (PARTY, Men, Food, Gear, Floor) tetap terlihat di semua ukuran layar.
    /// Taruh di root Canvas (atau object apa pun di dalam Canvas).
    /// </summary>
    public class BattleScreenController : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private bool enforceCanvasScaler = true;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [Range(0f, 1f)] [SerializeField] private float matchWidthOrHeight = 0.5f;

        [Header("HUD atas (object yang berisi PARTY, Men, Food, Gear, Floor)")]
        [SerializeField] private RectTransform hudTopBar;
        [SerializeField] private int hudSortingOrder = 100;
        [Tooltip("Kalau HUD ternyata berada di luar layar, tempelkan lagi ke tepi atas (stretch horizontal).")]
        [SerializeField] private bool repinHudIfOffscreen = true;

        [Header("Fullscreen")]
        [SerializeField] private Button fullscreenButton;
        [SerializeField] private TMP_Text fullscreenLabel;
        [SerializeField] private FullScreenMode fullscreenMode = FullScreenMode.FullScreenWindow;
        [SerializeField] private Vector2Int defaultWindowSize = new Vector2Int(1280, 720);

        private int _lastWidth, _lastHeight;
        private int _windowedWidth, _windowedHeight;
        private readonly Vector3[] _corners = new Vector3[4];

        public bool IsFullscreen => Screen.fullScreenMode != FullScreenMode.Windowed;

        // ---------- Lifecycle ----------
        private void Awake()
        {
            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null && hudTopBar != null) rootCanvas = hudTopBar.GetComponentInParent<Canvas>();
            if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;

            _windowedWidth = IsFullscreen ? defaultWindowSize.x : Screen.width;
            _windowedHeight = IsFullscreen ? defaultWindowSize.y : Screen.height;

            SetupCanvas();
        }

        private void Start()
        {
            if (fullscreenButton != null) fullscreenButton.onClick.AddListener(ToggleFullscreen);
            RefreshAll();
        }

        private void Update()
        {
            if (FullscreenKeyPressed()) ToggleFullscreen();

            // Deteksi perubahan ukuran layar (fullscreen, resize window, ganti aspect ratio)
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
                RefreshAll();
        }

        private static bool FullscreenKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Keyboard.current != null &&
                   UnityEngine.InputSystem.Keyboard.current.f11Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F11);
#else
            return false;
#endif
        }

        // ---------- Fullscreen ----------
        public void ToggleFullscreen()
        {
#if UNITY_EDITOR
            Debug.Log("[BattleScreen] Di Editor, Screen.SetResolution tidak mengubah Game view. " +
                      "Uji lewat 'Maximize On Play' atau di Build.");
#endif
            if (IsFullscreen)
            {
                Screen.SetResolution(_windowedWidth, _windowedHeight, FullScreenMode.Windowed);
            }
            else
            {
                _windowedWidth = Screen.width;
                _windowedHeight = Screen.height;
                Resolution display = Screen.currentResolution;
                Screen.SetResolution(display.width, display.height, fullscreenMode);
            }
            // RefreshAll dipanggil otomatis oleh Update() begitu ukuran layar benar-benar berubah
        }

        // ---------- Canvas & HUD ----------
        private void SetupCanvas()
        {
            if (rootCanvas == null) return;

            // Screen Space - Camera tanpa kamera = UI tidak tampil
            if (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera && rootCanvas.worldCamera == null)
                rootCanvas.worldCamera = Camera.main;

            if (!enforceCanvasScaler) return;

            var scaler = rootCanvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = rootCanvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
        }

        private void RefreshAll()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            SetupCanvas();
            EnsureHudOnTop();

            Canvas.ForceUpdateCanvases();
            if (hudTopBar != null) LayoutRebuilder.ForceRebuildLayoutImmediate(hudTopBar);

            if (repinHudIfOffscreen && hudTopBar != null && IsHudOffscreen())
            {
                Debug.LogWarning("[BattleScreen] HUD atas berada di luar layar, ditempelkan ke tepi atas.");
                PinHudToTop();
            }

            if (fullscreenLabel != null) fullscreenLabel.text = IsFullscreen ? "Windowed" : "Fullscreen";
        }

        // HUD diberi Canvas sendiri dengan sorting order tinggi, jadi panel battle
        // (yang membesar saat fullscreen) tidak bisa lagi menutupinya.
        private void EnsureHudOnTop()
        {
            if (hudTopBar == null) return;

            hudTopBar.gameObject.SetActive(true);

            var canvas = hudTopBar.GetComponent<Canvas>();
            if (canvas == null) canvas = hudTopBar.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = hudSortingOrder;

            if (hudTopBar.GetComponent<GraphicRaycaster>() == null)
                hudTopBar.gameObject.AddComponent<GraphicRaycaster>();

            var group = hudTopBar.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
        }

        private bool IsHudOffscreen()
        {
            hudTopBar.GetWorldCorners(_corners);
            Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, _corners[i]);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            var hud = new Rect(min, max - min);
            return !screen.Overlaps(hud);
        }

        private void PinHudToTop()
        {
            float height = Mathf.Max(hudTopBar.rect.height, 1f);
            hudTopBar.anchorMin = new Vector2(0f, 1f);
            hudTopBar.anchorMax = new Vector2(1f, 1f);
            hudTopBar.pivot = new Vector2(0.5f, 1f);
            hudTopBar.sizeDelta = new Vector2(0f, height);
            hudTopBar.anchoredPosition = Vector2.zero;
        }

        // Klik kanan komponen di Inspector > "Log HUD Diagnostics", jalankan saat fullscreen
        [ContextMenu("Log HUD Diagnostics")]
        private void LogDiagnostics()
        {
            if (rootCanvas == null || hudTopBar == null)
            {
                Debug.Log("[BattleScreen] rootCanvas / hudTopBar belum di-assign.");
                return;
            }

            var scaler = rootCanvas.GetComponent<CanvasScaler>();
            Debug.Log($"[BattleScreen] Screen {Screen.width}x{Screen.height}, mode {Screen.fullScreenMode}\n" +
                      $"Canvas: {rootCanvas.renderMode}, scaler: {(scaler != null ? scaler.uiScaleMode.ToString() : "TIDAK ADA")}\n" +
                      $"HUD aktif: {hudTopBar.gameObject.activeInHierarchy}, anchor: {hudTopBar.anchorMin}-{hudTopBar.anchorMax}, " +
                      $"pos: {hudTopBar.anchoredPosition}, size: {hudTopBar.rect.size}\n" +
                      $"HUD di luar layar: {IsHudOffscreen()}");
        }
    }
}
