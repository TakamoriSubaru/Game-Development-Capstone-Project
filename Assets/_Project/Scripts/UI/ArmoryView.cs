using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Equipment dari boss tiap Pack (slot "?" sampai ditemukan) dan Blessing para dewa (GDD 6.2). Klik item untuk detail.
    // Ikon equipment: taruh sprite di Resources/UI/equipment/<nama_snake_case>.png (mis. club_splinter.png);
    // selama belum ada, medali berisi kode item (mis. "CS") dipakai sebagai gantinya.
    public class ArmoryView : HubScreen
    {
        bool blessings;
        int sel;

        protected override string Title { get { return "ARMORY"; } }
        protected override bool RedStrip { get { return true; } }

        protected override void Populate(RectTransform body)
        {
            var tabs = new TabBar(body, new[] { "EQUIPMENT", "BLESSINGS" }, 0.04f, 0.765f, 0.40f, 0.85f,
                                  i => { blessings = i == 1; sel = 0; Refresh(); });
            tabs.Select(blessings ? 1 : 0, false);

            int count = blessings ? BlessingCatalog.All.Length : EquipmentCatalog.All.Length;
            int found = 0;
            if (!blessings)
                for (int i = 0; i < count; i++) if (MetaProgress.IsPackCleared(EquipmentCatalog.All[i].PackId)) found++;
            if (blessings)
                ParchUI.Txt(body, "Blessings: single-use, drain the target's Stamina.", 18, Parch.DarkBrown, TextAnchor.MiddleLeft, 0.43f, 0.755f, 0.72f, 0.875f);
            else
                ParchUI.Counter(body, "Found " + found + "/" + count, 20, 0.74f, 0.79f, 0.965f, 0.87f);

            if (sel >= count) sel = 0;
            if (blessings) BlessingGrid(body); else EquipmentGrid(body);
            Detail(body);
        }

        // ---------------- Equipment ----------------

        void EquipmentGrid(RectTransform body)
        {
            ParchUI.Panel9(body, "GridFrame", 0.03f, 0.055f, 0.685f, 0.745f, 0.3f);
            RectTransform grid;
            ParchUI.VScroll(body, "Scroll", 0.04f, 0.07f, 0.675f, 0.73f, out grid);
            grid.gameObject.AddComponent<GridLayoutGroup>();
            var fit = grid.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rg = grid.gameObject.AddComponent<ResponsiveGrid>();
            rg.Cols = 5; rg.Spacing = 8f; rg.Aspect = 0.72f; rg.Pad = 6f;

            for (int i = 0; i < EquipmentCatalog.All.Length; i++)
            {
                int idx = i;
                var e = EquipmentCatalog.All[i];
                bool known = MetaProgress.IsPackCleared(e.PackId);
                string frame = !known ? "frames/slot_unknown" : (i == sel ? "frames/slot_selected" : "frames/slot_normal");
                var img = ParchUI.Sliced(grid, "Slot" + (i + 1), frame, 0f, 0f, 1f, 1f, 0.35f);
                ParchUI.TintButton(img, () => { sel = idx; Refresh(); });
                if (known)
                {
                    Medallion(img.transform, e, 0.2f, 0.34f, 0.8f, 0.9f, 26);
                    ParchUI.Txt(img.transform, e.Name, 14, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.05f, 0.08f, 0.95f, 0.33f);
                }
                else
                {
                    var disc = ParchUI.FitPic(img.transform, "Disc", "icons/icon_ui_question", 0.25f, 0.2f, 0.75f, 0.8f);
                    disc.color = new Color(1f, 1f, 1f, 0.55f);
                }
            }
        }

        // Stand-in icons (relic art, in pack order). Put a PNG named after the item slug in Resources/UI/equipment/ to override.
        static readonly string[] StandIn = {
            "relicicons/relic_11_ca", "relicicons/relic_19_lf", "relicicons/relic_18_ce", "relicicons/relic_08_sc", "relicicons/relic_16_aw",
            "relicicons/relic_04_bs", "relicicons/relic_17_ch", "relicicons/relic_20_se", "relicicons/relic_24_hc", "relicicons/relic_03_wo",
            "relicicons/relic_28_sy", "relicicons/relic_50_hr", "relicicons/relic_41_pp", "relicicons/relic_21_st", "relicicons/relic_22_cw",
            "relicicons/relic_25_cv", "relicicons/relic_48_pt", "relicicons/relic_51_ae", "relicicons/relic_32_ps", "relicicons/relic_31_bo" };

        static string Slug(string name)
        {
            return Regex.Replace(name.ToLowerInvariant().Replace("'", ""), "[^a-z0-9]+", "_").Trim('_');
        }

        // Ikon item kalau ada, kalau tidak medali bulat berisi kode (dua huruf pertama dari dua kata awal).
        static void Medallion(Transform parent, EquipmentInfo e, float x0, float y0, float x1, float y1, int codeSize)
        {
            var sp = Resources.Load<Sprite>("UI/equipment/" + Slug(e.Name));
            if (sp == null) { int k = System.Array.IndexOf(EquipmentCatalog.All, e); if (k >= 0 && k < StandIn.Length) sp = UISprites.Get(StandIn[k]); }
            if (sp != null)
            {
                var irt = UIFactory.NewRect(parent, "Icon");
                UIFactory.Stretch(irt, x0, y0, x1, y1);
                var ic = irt.gameObject.AddComponent<Image>();
                ic.sprite = sp;
                ic.preserveAspect = true;
                ic.raycastTarget = false;
                return;
            }
            var box = ParchUI.FitRect(parent, "Medal", x0, y0, x1, y1, 1f);
            var disc = box.gameObject.AddComponent<Image>();
            disc.sprite = UIFactory.CircleSprite;
            disc.color = new Color(0.93f, 0.80f, 0.52f, 1f);
            disc.raycastTarget = false;
            ParchUI.Txt(box, HubUI.Initials(e.Name), codeSize, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.1f, 0.1f, 0.9f, 0.9f, true);
        }

        // ---------------- Blessings ----------------

        void BlessingGrid(RectTransform body)
        {
            const int cols = 5;
            for (int i = 0; i < BlessingCatalog.All.Length; i++)
            {
                int idx = i;
                int cx = i % cols, cy = i / cols;
                float x0 = 0.03f + cx * 0.1325f, x1 = x0 + 0.125f;
                float y1 = 0.745f - cy * 0.37f, y0 = y1 - 0.35f;
                var god = BlessingCatalog.All[i];
                var img = ParchUI.Sliced(body, "Bless" + i, i == sel ? "frames/slot_selected" : "frames/slot_normal", x0, y0, x1, y1, 0.4f);
                ParchUI.TintButton(img, () => { sel = idx; Refresh(); });
                ParchUI.FitPic(img.transform, "Emblem", "blessings/blessing_" + god.God.ToLowerInvariant() + "_round", 0.12f, 0.27f, 0.88f, 0.92f);
                ParchUI.Txt(img.transform, god.God, 18, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.04f, 0.06f, 0.96f, 0.27f, true);
            }
        }

        // ---------------- Detail ----------------

        void Detail(RectTransform body)
        {
            var t = ParchUI.Panel9(body, "Detail", 0.70f, 0.055f, 0.975f, 0.745f, 0.45f).rectTransform;
            string title, sub, text, foot;

            if (blessings)
            {
                var bl = BlessingCatalog.All[sel];
                ParchUI.FitPic(t, "Emblem", "blessings/blessing_" + bl.God.ToLowerInvariant() + "_round", 0.07f, 0.74f, 0.30f, 0.93f);
                title = "Blessing of " + bl.God;
                sub = "Target: " + bl.Target;
                text = bl.Effect;
                foot = "Price: " + bl.Gold + " Gold.\nSingle-use. Gives a massive buff, then drains the target's Stamina.";
                Header(t, title, 0.33f);
            }
            else
            {
                var e = EquipmentCatalog.All[sel];
                var p = EquipmentCatalog.PackOf(e);
                bool known = MetaProgress.IsPackCleared(e.PackId);
                if (known)
                {
                    Medallion(t, e, 0.07f, 0.74f, 0.30f, 0.93f, 24);
                    ParchUI.Txt(t, HubUI.Initials(e.Name), 30, Parch.DarkBrown, TextAnchor.LowerLeft, 0.33f, 0.83f, 0.93f, 0.95f, true);
                    ParchUI.Txt(t, e.Name, 22, Parch.DarkBrown, TextAnchor.UpperLeft, 0.33f, 0.72f, 0.93f, 0.83f, true);
                    sub = p != null ? "From " + p.Name + " (Floor " + p.Floor + ")" : "";
                    text = e.Effect;                       // baris stat, mis. "+Atk, -Speed."
                    foot = p != null ? "Dropped by " + p.Boss + "." : "";
                }
                else
                {
                    ParchUI.FitPic(t, "Unknown", "icons/icon_ui_question", 0.07f, 0.74f, 0.30f, 0.93f);
                    ParchUI.Txt(t, "???", 30, Parch.Muted, TextAnchor.MiddleLeft, 0.33f, 0.76f, 0.93f, 0.93f, true);
                    sub = p != null ? "Floor " + p.Floor + " Pack" : "";
                    text = "Not found yet. Defeat a Pack's boss to claim its equipment.";
                    foot = "";
                }
            }

            ParchUI.Txt(t, sub, 20, Parch.DarkBrown, TextAnchor.MiddleLeft, 0.08f, 0.60f, 0.92f, 0.70f);
            ParchUI.Pic(t, "Rule", "ornaments/divider_diamond", 0.08f, 0.57f, 0.92f, 0.60f, false);
            ParchUI.Txt(t, text, 24, Parch.DarkBrown, TextAnchor.UpperLeft, 0.08f, 0.30f, 0.92f, 0.56f);
            ParchUI.Txt(t, foot, 17, Parch.DarkBrown, TextAnchor.LowerLeft, 0.08f, 0.07f, 0.92f, 0.29f);
        }

        static void Header(Transform t, string title, float x0)
        {
            ParchUI.Txt(t, title, 26, Parch.DarkBrown, TextAnchor.MiddleLeft, x0, 0.76f, 0.95f, 0.92f, true);
        }
    }
}
