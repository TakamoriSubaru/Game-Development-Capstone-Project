#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace OddyseusJourneysEnd.Save.EditorTools
{
    /// <summary>
    /// Menu: OddyseusJourney's End > Create Save Point UI
    /// Membuat hierarchy UI Save Point (mirip mockup) dan mengisi semua referensi SavePoint otomatis.
    /// File ini HARUS berada di folder bernama "Editor".
    /// </summary>
    public static class SavePointUIBuilder
    {
        private static readonly Color Dark = new Color(0.33f, 0.33f, 0.33f, 1f);
        private static readonly Color Yellow = new Color(1f, 0.85f, 0.25f, 1f);
        private static readonly Color Red = new Color(0.8f, 0.1f, 0.1f, 1f);

        [MenuItem("OddyseusJourney's End/Create Save Point UI")]
        public static void Create()
        {
            Canvas canvas = FindOrCreateCanvas();

            // Root (selalu aktif, membawa komponen SavePoint)
            GameObject root = NewUI("SavePoint", canvas.transform);
            Stretch(Rect(root));
            var savePoint = root.AddComponent<SavePoint>();

            // Tombol gear untuk membuka popup
            Button gear = MakeButton(root.transform, "OpenButton", "Settings", new Vector2(-110f, -40f), new Vector2(180f, 60f), Dark);
            RectTransform gearRt = Rect(gear.gameObject);
            gearRt.anchorMin = gearRt.anchorMax = new Vector2(1f, 1f);
            gearRt.pivot = new Vector2(1f, 1f);
            gearRt.anchoredPosition = new Vector2(-20f, -20f);

            // Panel popup (dim background, memblokir klik ke belakang)
            GameObject panel = NewUI("Panel", root.transform);
            Stretch(Rect(panel));
            AddImage(panel, new Color(0f, 0f, 0f, 0.55f), false);

            // Panel tombol (abu-abu gelap, 3 tombol kuning)
            GameObject buttonsPanel = NewUI("ButtonsPanel", panel.transform);
            Place(Rect(buttonsPanel), new Vector2(0f, 70f), new Vector2(700f, 200f));
            AddImage(buttonsPanel, Dark, true);

            Button save = MakeButton(buttonsPanel.transform, "SaveButton", "SAVE", new Vector2(-220f, 0f), new Vector2(170f, 140f), Yellow);
            Button restart = MakeButton(buttonsPanel.transform, "RestartButton", "RESET", new Vector2(0f, 0f), new Vector2(170f, 140f), Yellow);
            Button exit = MakeButton(buttonsPanel.transform, "ExitButton", "EXIT", new Vector2(220f, 0f), new Vector2(170f, 140f), Yellow);

            // Tombol close (X merah) di pojok kanan atas panel tombol
            Button close = MakeButton(buttonsPanel.transform, "CloseButton", "X", Vector2.zero, new Vector2(56f, 56f), Red);
            RectTransform closeRt = Rect(close.gameObject);
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(0.5f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-10f, -10f);

            // Panel volume
            GameObject volumePanel = NewUI("VolumePanel", panel.transform);
            Place(Rect(volumePanel), new Vector2(-80f, -100f), new Vector2(540f, 150f));
            AddImage(volumePanel, Dark, true);

            Slider music = MakeVolumeRow(volumePanel.transform, "MusicSlider", "Music", 32f);
            Slider sfx = MakeVolumeRow(volumePanel.transform, "SfxSlider", "SFX", -32f);

            // Teks status ("Game tersimpan")
            TextMeshProUGUI status = MakeText(panel.transform, "StatusText", "", 30f, Color.white);
            Place(Rect(status.gameObject), new Vector2(0f, -230f), new Vector2(700f, 50f));

            // Dialog konfirmasi
            GameObject confirm = NewUI("ConfirmPanel", panel.transform);
            Stretch(Rect(confirm));
            AddImage(confirm, new Color(0f, 0f, 0f, 0.6f), false);

            GameObject box = NewUI("Box", confirm.transform);
            Place(Rect(box), Vector2.zero, new Vector2(620f, 280f));
            AddImage(box, Dark, true);

            TextMeshProUGUI confirmText = MakeText(box.transform, "Message", "Yakin?", 34f, Color.white);
            Place(Rect(confirmText.gameObject), new Vector2(0f, 50f), new Vector2(560f, 120f));
            Button yes = MakeButton(box.transform, "YesButton", "Ya", new Vector2(-130f, -80f), new Vector2(200f, 70f), Yellow);
            Button no = MakeButton(box.transform, "NoButton", "Batal", new Vector2(130f, -80f), new Vector2(200f, 70f), Yellow);
            confirm.SetActive(false);

            // Isi referensi SavePoint
            var so = new SerializedObject(savePoint);
            SetRef(so, "panelRoot", panel);
            SetRef(so, "openButton", gear);
            SetRef(so, "closeButton", close);
            SetRef(so, "saveButton", save);
            SetRef(so, "restartButton", restart);
            SetRef(so, "exitButton", exit);
            SetRef(so, "musicSlider", music);
            SetRef(so, "sfxSlider", sfx);
            SetRef(so, "confirmRoot", confirm);
            SetRef(so, "confirmMessage", confirmText);
            SetRef(so, "confirmYesButton", yes);
            SetRef(so, "confirmNoButton", no);
            SetRef(so, "statusText", status);
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(root, "Create Save Point UI");
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        // ---------- Helpers ----------
        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = null;
            if (Selection.activeGameObject != null)
                canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) return canvas;

            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            return canvas;
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        // Anchor & pivot di tengah layar/induk
        private static void Place(RectTransform r, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        private static Sprite Builtin(string path) => AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

        private static Image AddImage(GameObject go, Color color, bool rounded)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            if (rounded)
            {
                img.sprite = Builtin("UI/Skin/UISprite.psd");
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        private static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color)
        {
            GameObject go = NewUI(name, parent);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        private static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = NewUI(name, parent);
            Place(Rect(go), pos, size);
            Image img = AddImage(go, color, true);
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            TextMeshProUGUI text = MakeText(go.transform, "Label", label, 34f, Color.black);
            Stretch(Rect(text.gameObject));
            return button;
        }

        private static Slider MakeVolumeRow(Transform parent, string name, string label, float y)
        {
            TextMeshProUGUI text = MakeText(parent, name + "_Label", label, 28f, Color.white);
            Place(Rect(text.gameObject), new Vector2(-200f, y), new Vector2(110f, 40f));

            var res = new DefaultControls.Resources
            {
                standard = Builtin("UI/Skin/UISprite.psd"),
                background = Builtin("UI/Skin/Background.psd"),
                inputField = Builtin("UI/Skin/InputFieldBackground.psd"),
                knob = Builtin("UI/Skin/Knob.psd"),
                checkmark = Builtin("UI/Skin/Checkmark.psd"),
                dropdown = Builtin("UI/Skin/DropdownArrow.psd"),
                mask = Builtin("UI/Skin/UIMask.psd"),
            };

            GameObject go = DefaultControls.CreateSlider(res);
            go.name = name;
            go.transform.SetParent(parent, false);
            Place(Rect(go), new Vector2(60f, y), new Vector2(340f, 24f));

            var slider = go.GetComponent<Slider>();
            slider.minValue = 0.0001f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            if (slider.fillRect != null) slider.fillRect.GetComponent<Image>().color = Yellow;
            if (slider.handleRect != null) slider.handleRect.GetComponent<Image>().color = Color.white;
            return slider;
        }

        private static void SetRef(SerializedObject so, string field, Object value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning($"[SavePointUIBuilder] Field '{field}' tidak ditemukan di SavePoint.");
        }
    }
}
#endif
