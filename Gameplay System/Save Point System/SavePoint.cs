using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OddyseusJourneysEnd.Save
{
    /// <summary>
    /// Popup Save Point / Settings: Save, Restart, Exit, tombol close (X), dan slider Music + SFX.
    /// Taruh di GameObject yang SELALU aktif (mis. root canvas); panel popup-nya di-toggle lewat panelRoot.
    /// </summary>
    public class SavePoint : MonoBehaviour
    {
        private const string MusicPref = "Settings.MusicVolume";
        private const string SfxPref = "Settings.SfxVolume";
        private const float DefaultVolume = 0.8f;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;   // tombol gear di top bar
        [SerializeField] private Button closeButton;  // tombol X merah

        [Header("Tombol aksi")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;

        [Header("Volume")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string musicParameter = "MusicVolume";
        [SerializeField] private string sfxParameter = "SFXVolume";

        [Header("Konfirmasi Restart/Exit (opsional)")]
        [SerializeField] private GameObject confirmRoot;
        [SerializeField] private TMP_Text confirmMessage;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        [Header("Feedback (opsional)")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private float statusDuration = 2f;

        [Header("Perilaku")]
        [SerializeField] private bool pauseWhileOpen = true;
        [SerializeField] private bool autoLoadOnStart = true;
        [SerializeField] private bool saveOnExit = false;
        [Tooltip("Kosong = reload scene yang sedang aktif.")]
        [SerializeField] private string restartSceneName = "";
        [Tooltip("Kosong = tutup aplikasi (di Editor: stop Play Mode).")]
        [SerializeField] private string mainMenuSceneName = "";

        private System.Action _pendingConfirm;
        private float _prevTimeScale = 1f;
        private bool _pausedByUs;
        private bool _warnedNoMixer;
        private Coroutine _statusRoutine;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        // ---------- Lifecycle ----------
        private void Start()
        {
            Bind(openButton, Toggle);
            Bind(closeButton, Close);
            Bind(saveButton, SaveGame);
            Bind(restartButton, () => Ask("Mulai ulang run? Progres yang tersimpan akan dihapus.", RestartRun));
            Bind(exitButton, () => Ask(saveOnExit ? "Simpan dan keluar?" : "Keluar tanpa menyimpan?", ExitGame));
            Bind(confirmYesButton, ConfirmYes);
            Bind(confirmNoButton, HideConfirm);

            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            LoadVolumes();

            HideConfirm();
            if (panelRoot != null) panelRoot.SetActive(false);
            if (statusText != null) statusText.text = "";

            if (autoLoadOnStart && SaveSystem.HasSave()) LoadGame(showMessage: false);
        }

        private void Update()
        {
            if (!EscapePressed()) return;

            if (confirmRoot != null && confirmRoot.activeSelf) HideConfirm();
            else Toggle();
        }

        private void OnDestroy() => ResumeTime();

        private static void Bind(Button button, System.Action action)
        {
            if (button != null) button.onClick.AddListener(() => action());
        }

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Keyboard.current != null &&
                   UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        // ---------- Buka / tutup ----------
        public void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }

        public void Open()
        {
            if (panelRoot == null || IsOpen) return;
            panelRoot.SetActive(true);
            HideConfirm();

            if (pauseWhileOpen && !_pausedByUs)
            {
                _prevTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                _pausedByUs = true;
            }
        }

        public void Close()
        {
            if (panelRoot == null) return;
            HideConfirm();
            panelRoot.SetActive(false);
            ResumeTime();
            PlayerPrefs.Save();
        }

        private void ResumeTime()
        {
            if (!_pausedByUs) return;
            Time.timeScale = _prevTimeScale;
            _pausedByUs = false;
        }

        // ---------- Save / Load ----------
        public void SaveGame()
        {
            bool ok = SaveSystem.Save(SceneManager.GetActiveScene().name);
            ShowStatus(ok ? "Game tersimpan" : "Gagal menyimpan");
        }

        public bool LoadGame(bool showMessage = true)
        {
            if (!SaveSystem.TryLoad(out var data))
            {
                if (showMessage) ShowStatus("Tidak ada data save");
                return false;
            }

            SaveSystem.Apply(data);
            if (showMessage) ShowStatus("Save dimuat");
            return true;
        }

        // ---------- Restart / Exit ----------
        public void RestartRun()
        {
            ResumeTime();
            SaveSystem.Delete();     // hapus save lama
            SaveSystem.ResetAll();   // kembalikan party, gold, dll ke kondisi awal

            string scene = string.IsNullOrEmpty(restartSceneName)
                ? SceneManager.GetActiveScene().name
                : restartSceneName;
            SceneManager.LoadScene(scene);
        }

        public void ExitGame()
        {
            if (saveOnExit) SaveSystem.Save(SceneManager.GetActiveScene().name);
            ResumeTime();

            if (!string.IsNullOrEmpty(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- Konfirmasi ----------
        private void Ask(string message, System.Action onYes)
        {
            if (confirmRoot == null) { onYes(); return; } // tanpa dialog, langsung jalankan

            _pendingConfirm = onYes;
            if (confirmMessage != null) confirmMessage.text = message;
            confirmRoot.SetActive(true);
        }

        private void ConfirmYes()
        {
            var action = _pendingConfirm;
            HideConfirm();
            action?.Invoke();
        }

        private void HideConfirm()
        {
            _pendingConfirm = null;
            if (confirmRoot != null) confirmRoot.SetActive(false);
        }

        // ---------- Volume ----------
        private void LoadVolumes()
        {
            float music = PlayerPrefs.GetFloat(MusicPref, DefaultVolume);
            float sfx = PlayerPrefs.GetFloat(SfxPref, DefaultVolume);

            if (musicSlider != null) musicSlider.SetValueWithoutNotify(music);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);

            ApplyVolume(musicParameter, music);
            ApplyVolume(sfxParameter, sfx);
        }

        private void OnMusicChanged(float value)
        {
            PlayerPrefs.SetFloat(MusicPref, value);
            ApplyVolume(musicParameter, value);
        }

        private void OnSfxChanged(float value)
        {
            PlayerPrefs.SetFloat(SfxPref, value);
            ApplyVolume(sfxParameter, value);
        }

        // Slider linear (0-1) -> desibel. Parameter harus di-"Expose" di AudioMixer.
        private void ApplyVolume(string parameter, float linear)
        {
            if (audioMixer == null)
            {
                if (!_warnedNoMixer)
                {
                    Debug.LogWarning("[SavePoint] AudioMixer belum di-assign, slider volume belum berpengaruh.");
                    _warnedNoMixer = true;
                }
                return;
            }

            float db = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
            audioMixer.SetFloat(parameter, db);
        }

        // ---------- Feedback ----------
        private void ShowStatus(string message)
        {
            if (statusText == null) return;
            if (_statusRoutine != null) StopCoroutine(_statusRoutine);
            _statusRoutine = StartCoroutine(StatusRoutine(message));
        }

        private IEnumerator StatusRoutine(string message)
        {
            statusText.text = message;
            yield return new WaitForSecondsRealtime(statusDuration); // realtime: tetap jalan saat game di-pause
            statusText.text = "";
            _statusRoutine = null;
        }
    }
}
