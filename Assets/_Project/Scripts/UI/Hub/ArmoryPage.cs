using UnityEngine;
using UnityEngine.UI;

namespace Oddyseus.UI
{
    // Layar 4: Armory. Tab Equipment (60 slot relic + detail) dan Blessings (12 lambang dewa + detail).
    public class ArmoryPage : Page
    {
        TabBar tabs;
        int tab;
        RectTransform equipRoot, blessRoot;

        // --- equipment ---
        Image[] slotImgs;
        int selRelic;
        Text foundText;
        Image dFrame, dIcon;
        Text dCode, dName, dTier, dStat, dEffect, dSource;

        // --- blessings ---
        Image[] blessHl;
        int selBless;
        Image bEmblem;
        Text bName, bTarget, bEffect, bPrice, bStamina;

        protected override void Build()
        {
            Ui.TitleBar(C, "Armory", () => Host.Go(PageId.Hub));
            tabs = new TabBar(C, new[] { "Equipment", "Blessings" }, 0.05f, 0.775f, 0.38f, 0.855f, i => { tab = i; ShowTab(); });

            equipRoot = UIFactory.NewRect(C, "Equipment");
            UIFactory.Stretch(equipRoot, 0f, 0f, 1f, 1f);
            BuildEquipment();

            blessRoot = UIFactory.NewRect(C, "Blessings");
            UIFactory.Stretch(blessRoot, 0f, 0f, 1f, 1f);
            BuildBlessings();
        }

        public override void Refresh()
        {
            tabs.Select(tab, false);
            RefreshEquipment();
            SelectBlessing(selBless);
            ShowTab();
        }

        void ShowTab()
        {
            equipRoot.gameObject.SetActive(tab == 0);
            blessRoot.gameObject.SetActive(tab == 1);
        }

        // =============================== Equipment ===============================

        void BuildEquipment()
        {
            RectTransform content;
            Ui.VScroll(equipRoot, "Scroll", 0.05f, 0.11f, 0.625f, 0.76f, out content);
            content.gameObject.AddComponent<GridLayoutGroup>();
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rg = content.gameObject.AddComponent<ResponsiveGrid>();
            rg.Cols = 7; rg.Spacing = 8f; rg.Aspect = 1.28f; rg.Pad = 6f;

            slotImgs = new Image[RelicCatalog.All.Length];
            for (int i = 0; i < RelicCatalog.All.Length; i++)
            {
                int k = i;
                var img = Ui.Sliced(content, "Slot" + (i + 1), "frames/slot_unknown", 0f, 0f, 1f, 1f, 0.3f);
                Ui.TintButton(img, () => SelectRelic(k));
                var o = img.gameObject.AddComponent<Outline>();
                o.effectColor = Theme.Gold;
                o.effectDistance = new Vector2(3f, -3f);
                o.enabled = false;
                slotImgs[i] = img;
            }

            Ui.Sliced(equipRoot, "FoundPlaque", "frames/frame_row_wide", 0.05f, 0.025f, 0.34f, 0.095f, 0.45f);
            foundText = Ui.Txt(equipRoot, "", 22, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.07f, 0.03f, 0.32f, 0.09f, true);

            // panel detail
            var p = Ui.Panel9(equipRoot, "Detail", 0.65f, 0.09f, 0.975f, 0.78f, 0.45f).rectTransform;
            dFrame = Ui.Sliced(p, "Frame", "relic_frames/frame_common", 0.08f, 0.63f, 0.36f, 0.94f, 0.3f);
            dIcon = Ui.Pic(p, "Icon", "relics/relic_01_is", 0.13f, 0.695f, 0.31f, 0.875f);
            dCode = Ui.Txt(p, "", 34, Theme.DarkBrown, TextAnchor.LowerLeft, 0.40f, 0.79f, 0.94f, 0.94f, true);
            dName = Ui.Txt(p, "", 20, Theme.DarkBrown, TextAnchor.UpperLeft, 0.40f, 0.64f, 0.94f, 0.79f, true);
            dTier = Ui.Txt(p, "", 19, Theme.DarkBrown, TextAnchor.MiddleLeft, 0.08f, 0.55f, 0.92f, 0.62f);
            dStat = Ui.Txt(p, "", 19, Theme.DarkBrown, TextAnchor.MiddleLeft, 0.08f, 0.47f, 0.92f, 0.54f);
            dEffect = Ui.Txt(p, "", 19, Theme.DarkBrown, TextAnchor.UpperLeft, 0.08f, 0.27f, 0.92f, 0.46f);
            dSource = Ui.Txt(p, "", 19, Theme.DarkBrown, TextAnchor.MiddleLeft, 0.08f, 0.16f, 0.92f, 0.26f);
        }

        void RefreshEquipment()
        {
            for (int i = 0; i < RelicCatalog.All.Length; i++) PaintSlot(i);
            foundText.text = "Found " + MetaProgress.RelicFoundCount + "/" + RelicCatalog.Total;
            SelectRelic(selRelic);
        }

        void PaintSlot(int i)
        {
            var r = RelicCatalog.All[i];
            var img = slotImgs[i];
            // bersihkan isi lama (ikon / tanda tanya)
            for (int c = img.transform.childCount - 1; c >= 0; c--) Object.Destroy(img.transform.GetChild(c).gameObject);
            if (MetaProgress.RelicFound(r.N))
            {
                img.sprite = UISprites.Get(r.FramePath);
                Ui.Pic(img.transform, "Icon", r.IconPath, 0.17f, 0.21f, 0.83f, 0.77f);
            }
            else
            {
                img.sprite = UISprites.Get("frames/slot_unknown");
                Ui.Txt(img.transform, "?", 34, Theme.Muted, TextAnchor.MiddleCenter, 0.1f, 0.15f, 0.9f, 0.85f, true);
            }
        }

        void SelectRelic(int i)
        {
            selRelic = i;
            for (int k = 0; k < slotImgs.Length; k++) slotImgs[k].GetComponent<Outline>().enabled = k == i;
            var r = RelicCatalog.All[i];
            bool found = MetaProgress.RelicFound(r.N);
            if (found)
            {
                dFrame.sprite = UISprites.Get(r.FramePath);
                dIcon.sprite = UISprites.Get(r.IconPath);
                dIcon.enabled = true;
                dCode.text = r.Code;
                dName.text = r.Name;
                dTier.text = Row("Tier", r.TierLabel);
                dStat.text = Row("Stat", string.IsNullOrEmpty(r.Stat) ? "-" : r.Stat);       // mis. "+Atk, -Speed"
                dEffect.text = Row("Effect", string.IsNullOrEmpty(r.Effect) ? "-" : r.Effect);
                dSource.text = Row("Source", string.IsNullOrEmpty(r.SourcePack) ? "-" : r.SourcePack);
            }
            else
            {
                dFrame.sprite = UISprites.Get("frames/slot_unknown");
                dIcon.enabled = false;
                dCode.text = "?";
                dName.text = "Unknown relic";
                dTier.text = Row("Tier", "?");
                dStat.text = Row("Stat", "?");
                dEffect.text = Row("Effect", "Not discovered yet.");
                dSource.text = Row("Source", "?");
            }
        }

        static string Row(string label, string value)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(Theme.Muted) + ">" + label + "</color>  " + value;
        }

        // =============================== Blessings ===============================

        void BuildBlessings()
        {
            var area = UIFactory.NewRect(blessRoot, "Grid");
            UIFactory.Stretch(area, 0.05f, 0.11f, 0.62f, 0.76f);
            int n = BlessingCatalog.All.Length;
            blessHl = new Image[n];
            const int cols = 4, rows = 3;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                int cx = i % cols, cy = i / cols;
                float x0 = cx / (float)cols, x1 = (cx + 1) / (float)cols;
                float y1 = 1f - cy / (float)rows, y0 = 1f - (cy + 1) / (float)rows;
                var cell = UIFactory.NewRect(area, "Bless" + i);
                UIFactory.Stretch(cell, x0, y0, x1, y1);
                var hl = UIFactory.NewRect(cell, "Highlight");
                UIFactory.Stretch(hl, 0.04f, 0.18f, 0.96f, 0.98f);
                var hi = hl.gameObject.AddComponent<Image>();
                hi.sprite = UIFactory.CircleSprite;
                hi.color = Theme.Gold;
                hi.raycastTarget = false;
                hi.enabled = false;
                blessHl[i] = hi;
                var em = Ui.FitPic(cell, "Emblem", BlessingCatalog.All[i].RoundPath, 0.10f, 0.22f, 0.90f, 0.94f);
                Ui.TintButton(em, () => SelectBlessing(k));
                Ui.Txt(cell, BlessingCatalog.All[i].God, 17, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.02f, 0.0f, 0.98f, 0.19f, true);
            }

            var p = Ui.Panel9(blessRoot, "Detail", 0.65f, 0.09f, 0.975f, 0.78f, 0.45f).rectTransform;
            bEmblem = Ui.FitPic(p, "Emblem", BlessingCatalog.All[0].RoundPath, 0.07f, 0.62f, 0.42f, 0.94f);
            bName = Ui.Txt(p, "", 30, Theme.DarkBrown, TextAnchor.MiddleLeft, 0.45f, 0.70f, 0.94f, 0.86f, true);
            bTarget = Ui.Txt(p, "", 20, Theme.DarkBrown, TextAnchor.MiddleLeft, 0.08f, 0.52f, 0.92f, 0.60f);
            bEffect = Ui.Txt(p, "", 20, Theme.DarkBrown, TextAnchor.UpperLeft, 0.08f, 0.30f, 0.92f, 0.51f);
            Ui.FitPic(p, "GoldIcon", "icons/icon_res_gold", 0.08f, 0.18f, 0.18f, 0.27f);
            bPrice = Ui.Txt(p, "", 20, Theme.DarkBrown, TextAnchor.MiddleLeft, 0.20f, 0.18f, 0.92f, 0.27f);
            bStamina = Ui.Txt(p, "", 20, Theme.DarkBrown, TextAnchor.MiddleLeft, 0.08f, 0.08f, 0.92f, 0.16f);
        }

        void SelectBlessing(int i)
        {
            selBless = i;
            for (int k = 0; k < blessHl.Length; k++) blessHl[k].enabled = k == i;
            var b = BlessingCatalog.All[i];
            bEmblem.sprite = UISprites.Get(b.RoundPath);
            bName.text = b.God;
            bTarget.text = Row("Target", b.Target);
            bEffect.text = Row("Effect", b.Effect);
            bPrice.text = Row("Price", b.Price >= 0 ? b.Price + " Gold" : "TBD");
            bStamina.text = Row("Stamina cost", b.StaminaCost >= 0 ? b.StaminaCost.ToString() : "TBD");
        }
    }
}
