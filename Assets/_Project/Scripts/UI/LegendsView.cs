using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Semua karakter: yang dimiliki (dari GameData) dan karakter bonus yang terkunci sampai achievement tercapai.
    // Tab Legends / Costumes, kartu dengan portrait bulat, chip peran, stat, dan bar kostum.
    public class LegendsView : HubScreen
    {
        int tab;

        protected override string Title { get { return "LEGENDS"; } }

        protected override void Populate(RectTransform body)
        {
            var roster = new List<UnitData>();
            foreach (var d in GameData.CreateRoster()) roster.Add(d);
            var bonus = AchievementCatalog.Bonus;

            int total = roster.Count + bonus.Length;
            int owned = roster.Count;
            for (int i = 0; i < bonus.Length; i++)
            {
                var a = AchievementCatalog.Find(bonus[i].AchievementId);
                if (a != null && a.Unlocked) owned++;
            }

            var tabs = new TabBar(body, new[] { "LEGENDS", "COSTUMES" }, 0.045f, 0.77f, 0.34f, 0.85f, i => { tab = i; Refresh(); });
            tabs.Select(tab, false);
            ParchUI.Txt(body, "Unlocked through Campaign chapters and Achievements.", 17, Parch.Muted, TextAnchor.MiddleLeft, 0.36f, 0.77f, 0.66f, 0.85f);
            ParchUI.Ribbon(body, "olive", (tab == 0 ? "Legends " : "Legends ") + owned + "/" + total, 20, 0.69f, 0.755f, 0.955f, 0.865f);

            RectTransform grid;
            ParchUI.VScroll(body, "Scroll", 0.045f, 0.05f, 0.955f, 0.745f, out grid);
            grid.gameObject.AddComponent<GridLayoutGroup>();
            var fit = grid.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rg = grid.gameObject.AddComponent<ResponsiveGrid>();
            rg.Cols = 5; rg.Spacing = 14f; rg.Aspect = 1.55f; rg.Pad = 6f;

            for (int i = 0; i < roster.Count; i++) { if (tab == 0) OwnedCard(grid, roster[i]); else CostumeCard(grid, roster[i].Id, roster[i].Name, true); }
            for (int i = 0; i < bonus.Length; i++)
            {
                var a = AchievementCatalog.Find(bonus[i].AchievementId);
                bool open = a != null && a.Unlocked;
                if (tab == 0) BonusCard(grid, bonus[i], a, open); else CostumeCard(grid, IdOf(bonus[i].Name), bonus[i].Name, open);
            }
        }

        static string IdOf(string name) { return name.Split(' ')[0].ToLowerInvariant(); }

        static RectTransform NewCard(RectTransform grid)
        {
            return ParchUI.Sliced(grid, "Card", "frames/frame_card_tall", 0f, 0f, 1f, 1f, 0.55f).rectTransform;
        }

        static Sprite Round(string id) { return UISprites.Get("portraits/portrait_" + id + "_round"); }

        static void OwnedCard(RectTransform grid, UnitData d)
        {
            var card = NewCard(grid);
            var pic = ParchUI.FitPic(card, "Portrait", "portraits/portrait_" + d.Id + "_round", 0.16f, 0.60f, 0.84f, 0.96f);
            ParchUI.Txt(card, d.Name, 19, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.06f, 0.525f, 0.94f, 0.60f, true);
            ParchUI.RoleChip(card, d.Role, 0.06f, 0.385f, 0.94f, 0.52f);
            Stat(card, "icons/icon_stat_hp", "HP", d.MaxHp.ToString(), 0.08f, 0.275f, 0.50f, 0.37f);
            Stat(card, "icons/icon_stat_atk", "ATK", d.Atk.ToString(), 0.52f, 0.275f, 0.94f, 0.37f);
            Stat(card, "icons/icon_stat_def", "DEF", d.Def.ToString(), 0.08f, 0.175f, 0.50f, 0.27f);
            Stat(card, "icons/icon_stat_spd", "SPD", d.SpeedLabel, 0.52f, 0.175f, 0.94f, 0.27f);   // rentang min-max dari data
            Costumes(card, d.Name);
        }

        static void BonusCard(RectTransform grid, BonusCharacter b, Achievement a, bool open)
        {
            var card = NewCard(grid);
            if (open)
            {
                ParchUI.FitPic(card, "Portrait", "portraits/portrait_" + IdOf(b.Name) + "_round", 0.16f, 0.60f, 0.84f, 0.96f);
                ParchUI.Txt(card, b.Name, 19, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.06f, 0.525f, 0.94f, 0.60f, true);
                ParchUI.RoleChip(card, b.Role, 0.06f, 0.385f, 0.94f, 0.52f);
                ParchUI.Txt(card, "UNLOCKED", 16, Parch.Olive, TextAnchor.MiddleCenter, 0.08f, 0.29f, 0.92f, 0.375f, true);
                ParchUI.Txt(card, "Battle data is not made yet.", 15, Parch.Muted, TextAnchor.UpperCenter, 0.10f, 0.08f, 0.90f, 0.29f);
                return;
            }
            ParchUI.FitPic(card, "Portrait", "portraits/portrait_locked_round", 0.16f, 0.60f, 0.84f, 0.96f);
            ParchUI.FitPic(card, "Lock", "icons/icon_ui_lock", 0.60f, 0.575f, 0.84f, 0.68f);
            ParchUI.Txt(card, "???", 20, Parch.Muted, TextAnchor.MiddleCenter, 0.08f, 0.49f, 0.92f, 0.58f, true);
            ParchUI.Txt(card, "LOCKED", 15, Parch.Danger, TextAnchor.MiddleCenter, 0.08f, 0.42f, 0.92f, 0.49f, true);
            ParchUI.Txt(card, a != null ? a.Desc : "", 16, Parch.DarkBrown, TextAnchor.UpperCenter, 0.10f, 0.08f, 0.90f, 0.41f);
        }

        static void CostumeCard(RectTransform grid, string id, string name, bool open)
        {
            var card = NewCard(grid);
            var pic = ParchUI.FitPic(card, "Icon", "icons/icon_costume", 0.22f, 0.58f, 0.78f, 0.94f);
            if (!open) pic.color = new Color(1f, 1f, 1f, 0.45f);
            ParchUI.Txt(card, open ? name : "???", 19, open ? Parch.DarkBrown : Parch.Muted, TextAnchor.MiddleCenter, 0.06f, 0.47f, 0.94f, 0.57f, true);
            var list = AchievementCatalog.CostumesFor(name);
            int got = 0;
            for (int i = 0; i < list.Count; i++) if (list[i].Unlocked) got++;
            ParchUI.Txt(card, "Costumes " + got + "/" + list.Count, 16, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.08f, 0.35f, 0.92f, 0.45f);
            ParchUI.ProgressBar(card, list.Count > 0 ? (float)got / list.Count : 0f, 0.10f, 0.26f, 0.90f, 0.33f, 0.35f);
            string foot = list.Count == 0 ? "No costumes yet." : "";
            for (int i = 0; i < list.Count; i++)
                foot += (i > 0 ? "\n" : "") + (list[i].Unlocked ? list[i].RewardName : "??? (" + list[i].Name + ")");
            ParchUI.Txt(card, foot, 14, Parch.Muted, TextAnchor.UpperCenter, 0.08f, 0.05f, 0.92f, 0.25f);
        }

        static void Costumes(RectTransform card, string who)
        {
            var list = AchievementCatalog.CostumesFor(who);
            int got = 0;
            for (int i = 0; i < list.Count; i++) if (list[i].Unlocked) got++;
            ParchUI.Txt(card, list.Count == 0 ? "Costumes: none yet" : "Costumes " + got + "/" + list.Count, 14, Parch.Muted,
                        TextAnchor.LowerCenter, 0.08f, 0.095f, 0.92f, 0.15f);
            ParchUI.ProgressBar(card, list.Count > 0 ? (float)got / list.Count : 0f, 0.10f, 0.035f, 0.90f, 0.10f, 0.35f);
        }

        static void Stat(Transform card, string icon, string label, string value, float x0, float y0, float x1, float y1)
        {
            float mid = x0 + (x1 - x0) * 0.30f;
            ParchUI.FitPic(card, "StatIcon", icon, x0, y0, mid, y1);
            ParchUI.Txt(card, "<size=11>" + label + "</size> " + value, 16, Parch.DarkBrown, TextAnchor.MiddleLeft, mid + 0.01f, y0, x1, y1);
        }
    }
}
