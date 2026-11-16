using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Layar 2: Legends. Tab Legends/Costumes, grid kartu, penghitung "Legends 6/9".
    public class LegendsPage : Page
    {
        TabBar tabs;
        RectTransform content;
        GameObject ribbonHolder;
        int tab;

        protected override void Build()
        {
            Ui.TitleBar(C, "Legends", () => Host.Go(PageId.Hub));
            tabs = new TabBar(C, new[] { "Legends", "Costumes" }, 0.05f, 0.775f, 0.33f, 0.855f, i => { tab = i; Fill(); });

            RectTransform grid;
            Ui.VScroll(C, "Scroll", 0.05f, 0.05f, 0.95f, 0.75f, out grid);
            content = grid;
            var g = content.gameObject.AddComponent<GridLayoutGroup>();
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rg = content.gameObject.AddComponent<ResponsiveGrid>();
            rg.Cols = 5; rg.Spacing = 14f; rg.Aspect = 1.55f; rg.Pad = 6f;
        }

        public override void Refresh()
        {
            tabs.Select(tab, false);
            Fill();
        }

        void Fill()
        {
            UIFactory.ClearChildren(content);
            if (ribbonHolder != null) Object.Destroy(ribbonHolder);
            var holder = UIFactory.NewRect(C, "CounterHolder");
            UIFactory.Stretch(holder, 0f, 0f, 1f, 1f);
            ribbonHolder = holder.gameObject;

            if (tab == 0)
            {
                Ui.Ribbon(holder, "olive", "Legends " + LegendCatalog.UnlockedCount + "/" + LegendCatalog.All.Length, 20, 0.66f, 0.765f, 0.95f, 0.865f);
                foreach (var l in LegendCatalog.All) LegendCard(l);
            }
            else
            {
                int owned = 0, total = LegendCatalog.All.Length * LegendCatalog.CostumesPerLegend;
                foreach (var l in LegendCatalog.All) owned += LegendCatalog.CostumesOwned(l);
                Ui.Ribbon(holder, "gold", "Costumes " + owned + "/" + total, 20, 0.66f, 0.765f, 0.95f, 0.865f);
                foreach (var l in LegendCatalog.All) CostumeCard(l);
            }
        }

        RectTransform NewCard()
        {
            var img = Ui.Sliced(content, "Card", "frames/frame_card_tall", 0f, 0f, 1f, 1f, 0.55f);
            return img.rectTransform;
        }

        void LegendCard(LegendInfo l)
        {
            var card = NewCard();
            if (!l.Unlocked || l.Data == null)
            {
                var pic = Ui.FitPic(card, "Portrait", "portraits/portrait_locked_round", 0.16f, 0.58f, 0.84f, 0.95f);
                Ui.FitPic(card, "Lock", "icons/icon_ui_lock", 0.60f, 0.575f, 0.84f, 0.68f);
                Ui.Txt(card, "???", 20, Theme.Muted, TextAnchor.MiddleCenter, 0.08f, 0.49f, 0.92f, 0.58f, true);
                Ui.Txt(card, "Locked", 16, Theme.Muted, TextAnchor.MiddleCenter, 0.08f, 0.42f, 0.92f, 0.49f);
                Ui.Txt(card, l.UnlockHint, 16, Theme.DarkBrown, TextAnchor.UpperCenter, 0.10f, 0.12f, 0.90f, 0.40f);
                return;
            }

            var u = l.Data;
            Ui.FitPic(card, "Portrait", l.RoundPath, 0.16f, 0.60f, 0.84f, 0.96f);
            Ui.Txt(card, l.Name, 19, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.06f, 0.525f, 0.94f, 0.60f, true);
            Ui.RoleChip(card, u.Role, 0.06f, 0.385f, 0.94f, 0.52f);

            Stat(card, "icons/icon_stat_hp", "HP", u.MaxHp, 0.08f, 0.275f, 0.50f, 0.37f);
            Stat(card, "icons/icon_stat_atk", "ATK", u.Atk, 0.52f, 0.275f, 0.94f, 0.37f);
            Stat(card, "icons/icon_stat_def", "DEF", u.Def, 0.08f, 0.175f, 0.50f, 0.27f);
            Stat(card, "icons/icon_stat_spd", "SPD", u.Speed, 0.52f, 0.175f, 0.94f, 0.27f);

            int owned = LegendCatalog.CostumesOwned(l), total = LegendCatalog.CostumesPerLegend;
            Ui.Txt(card, "Costumes " + owned + "/" + total, 14, Theme.Muted, TextAnchor.LowerCenter, 0.08f, 0.095f, 0.92f, 0.15f);
            Ui.ProgressBar(card, total > 0 ? (float)owned / total : 0f, 0.10f, 0.035f, 0.90f, 0.10f, 0.35f);
        }

        void CostumeCard(LegendInfo l)
        {
            var card = NewCard();
            bool open = l.Unlocked;
            var pic = Ui.FitPic(card, "Icon", "icons/icon_costume", 0.22f, 0.58f, 0.78f, 0.93f);
            if (!open) pic.color = new Color(1f, 1f, 1f, 0.45f);
            Ui.Txt(card, open ? l.Name : "???", 19, open ? Theme.DarkBrown : Theme.Muted, TextAnchor.MiddleCenter, 0.06f, 0.46f, 0.94f, 0.56f, true);
            int owned = LegendCatalog.CostumesOwned(l), total = LegendCatalog.CostumesPerLegend;
            Ui.Txt(card, "Costumes " + owned + "/" + total, 16, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.08f, 0.34f, 0.92f, 0.44f);
            Ui.ProgressBar(card, total > 0 ? (float)owned / total : 0f, 0.10f, 0.24f, 0.90f, 0.31f, 0.35f);
            Ui.Txt(card, open ? "More costumes are earned through Achievements." : l.UnlockHint, 15, Theme.Muted, TextAnchor.UpperCenter, 0.10f, 0.06f, 0.90f, 0.22f);
        }

        static void Stat(Transform card, string icon, string label, int value, float x0, float y0, float x1, float y1)
        {
            float mid = x0 + (x1 - x0) * 0.30f;
            Ui.FitPic(card, "StatIcon", icon, x0, y0, mid, y1);
            Ui.Txt(card, "<size=11>" + label + "</size> " + value, 16, Theme.DarkBrown, TextAnchor.MiddleLeft, mid + 0.01f, y0, x1, y1);
        }
    }
}
