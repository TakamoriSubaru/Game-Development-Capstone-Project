using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Oddyseus.UI
{
    /// Top bar permanen 2 baris, tema gelap dan ringkas.
    /// Baris 1: [Party] | Men / Food | [Gear]
    /// Baris 2: equipment (scroll, tanpa batas) | blessing (maks 3, dijaga DungeonState) | Floor x/y
    public class TopBarView : MonoBehaviour
    {
        public const int MinEquipSlots = 0;          // 0 = tanpa placeholder, hanya item yang dimiliki
        public const int MinBuffSlots = 0;           // 0 = tanpa placeholder B1..B3
        public const int BuffSlots = 3;              // maks blessing; hanya dipakai untuk lebar kotak blessing
        public const float BarHeight = 124f;
        const float SlotW = 56f;
        const float BuffViewW = BuffSlots * SlotW + (BuffSlots - 1) * 8f + 20f;

        public event Action PartyClicked;
        public event Action SettingsClicked;

        static readonly Color Filled = HubTheme.Gold;
        static readonly Color Empty = HubTheme.Panel;

        Text _resText, _mapText;
        RectTransform _equipContent, _buffContent;

        public static TopBarView Create(Transform parent)
        {
            var rt = NewRect("TopBar", parent);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, BarHeight);
            rt.anchoredPosition = Vector2.zero;
            rt.gameObject.AddComponent<Image>().color = HubTheme.Deep;

            var view = rt.gameObject.AddComponent<TopBarView>();
            view.Build(rt);
            return view;
        }

        // ---------- API ----------
        public void SetResources(int men, int food) { _resText.text = "Men " + men + "     Food " + food; }
        public void SetMap(int current, int total) { _mapText.text = "FLOOR " + current + "/" + total; }

        /// Tanpa batas jumlah. Hanya item yang dimiliki yang tampil; kalau melebihi lebar kotak, bisa di-scroll.
        public void SetEquipment(IReadOnlyList<string> items)
        {
            Fill(_equipContent, items, MinEquipSlots, "E");
        }

        /// Hanya blessing yang dimiliki yang tampil. Batas maksimal 3 dijaga di DungeonState.AddRelic.
        public void SetBuffs(IReadOnlyList<string> buffs)
        {
            Fill(_buffContent, buffs, MinBuffSlots, "B");
        }

        void Fill(RectTransform content, IReadOnlyList<string> items, int min, string prefix)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var c = content.GetChild(i);
                c.SetParent(null);
                Destroy(c.gameObject);
            }
            int count = items != null ? items.Count : 0;
            int total = Mathf.Max(min, count);
            for (int i = 0; i < total; i++)
            {
                bool has = i < count && !string.IsNullOrEmpty(items[i]);
                Text tx;
                var img = NewSlot(content, has ? items[i] : prefix + (i + 1), SlotW, out tx);
                Style(img, tx, has);
            }
        }

        static void Style(Image img, Text tx, bool has)
        {
            img.color = has ? Filled : Empty;
            tx.color = has ? HubTheme.Night : HubTheme.Muted;
            var o = img.GetComponent<Outline>();
            if (o != null) o.effectColor = has ? HubTheme.Gold : new Color(HubTheme.Bronze.r, HubTheme.Bronze.g, HubTheme.Bronze.b, 0.5f);
        }

        // ---------- Build ----------
        void Build(RectTransform root)
        {
            var v = root.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(14, 14, 8, 8);
            v.spacing = 8;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            // Garis perunggu di tepi bawah.
            var line = NewRect("BottomLine", root);
            line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            line.anchorMin = new Vector2(0, 0);
            line.anchorMax = new Vector2(1, 0);
            line.pivot = new Vector2(0.5f, 0);
            line.sizeDelta = new Vector2(0, 3);
            line.anchoredPosition = Vector2.zero;
            line.gameObject.AddComponent<Image>().color = HubTheme.Bronze;

            // Baris 1
            var row1 = NewRow(root, 44f);
            NewButton(row1, "PARTY", 130f, () => { if (PartyClicked != null) PartyClicked(); });
            _resText = NewLabel(row1, 360f, 22, TextAnchor.MiddleLeft);
            Spacer(row1);
            NewButton(row1, "GEAR", 100f, () => { if (SettingsClicked != null) SettingsClicked(); });

            // Baris 2
            var row2 = NewRow(root, 56f);
            _equipContent = BuildScroll(row2, "EquipScroll", true, 0f);
            Separator(row2);
            _buffContent = BuildScroll(row2, "BuffScroll", false, BuffViewW);
            Separator(row2);
            _mapText = NewLabel(row2, 150f, 22, TextAnchor.MiddleCenter);

            SetResources(0, 0);
            SetMap(1, 3);
            SetEquipment(null);
            SetBuffs(null);
        }

        // Kotak scroll horizontal. flexible = true: mengisi sisa lebar. false: lebar tetap.
        RectTransform BuildScroll(Transform row, string name, bool flexible, float width)
        {
            var scrollRt = NewRect(name, row);
            var le = scrollRt.gameObject.AddComponent<LayoutElement>();
            if (flexible) le.flexibleWidth = 1; else le.preferredWidth = width;
            var bg = scrollRt.gameObject.AddComponent<Image>();
            bg.color = HubTheme.Night;
            HubTheme.Frame(bg, HubTheme.Bronze);

            var viewport = NewRect("Viewport", scrollRt);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var h = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 8, 6, 6);
            h.spacing = 8;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var sr = scrollRt.gameObject.AddComponent<ScrollRect>();
            sr.viewport = viewport;
            sr.content = content;
            sr.horizontal = true;
            sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;
            return content;
        }

        // ---------- Helpers ----------
        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static RectTransform NewRow(Transform parent, float height)
        {
            var rt = NewRect("Row", parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            return rt;
        }

        static void Spacer(Transform parent)
        {
            var rt = NewRect("Spacer", parent);
            rt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        static void Separator(Transform parent)
        {
            var rt = NewRect("Separator", parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredWidth = 3;
            rt.gameObject.AddComponent<Image>().color = HubTheme.Bronze;
        }

        static Text NewText(Transform parent, string s, int size, TextAnchor anchor, Color color, bool bestFit = false)
        {
            var rt = NewRect("Text", parent);
            Stretch(rt);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UIFactory.DefaultFont;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.text = s;
            t.raycastTarget = false;
            if (bestFit)
            {
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 8;
                t.resizeTextMaxSize = size;
            }
            return t;
        }

        static Text NewLabel(Transform parent, float width, int size, TextAnchor anchor)
        {
            var rt = NewRect("Label", parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
            return NewText(rt, "", size, anchor, HubTheme.Marble);
        }

        static Button NewButton(Transform parent, string label, float width, Action onClick)
        {
            var rt = NewRect(label + "Button", parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            img.sprite = UIFactory.RoundedSprite;
            img.type = Image.Type.Sliced;
            HubTheme.Frame(img, HubTheme.Bronze);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            HubUI.Tint(b, HubTheme.Panel);
            rt.gameObject.AddComponent<ButtonFx>();
            b.onClick.AddListener(() => { if (onClick != null) onClick(); });
            NewText(rt, label, 18, TextAnchor.MiddleCenter, HubTheme.Marble);
            return b;
        }

        static Image NewSlot(Transform parent, string label, float width, out Text text)
        {
            var rt = NewRect("Slot", parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UIFactory.RoundedSprite;
            img.type = Image.Type.Sliced;
            HubTheme.Frame(img, HubTheme.Bronze);
            text = NewText(rt, label, 15, TextAnchor.MiddleCenter, HubTheme.Muted, true);
            return img;
        }
    }
}