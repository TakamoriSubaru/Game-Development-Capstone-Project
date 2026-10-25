using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Semua karakter: yang dimiliki (dari GameData) dan karakter bonus yang terkunci sampai achievement tercapai.
    public class LegendsView : HubScreen
    {
        const int Cols = 4;

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

            UIFactory.Label(body, "Unlocked through Campaign chapters and Achievements.", 16, HubTheme.Muted,
                            TextAnchor.MiddleLeft, 0.03f, 0.90f, 0.60f, 0.98f);
            UIFactory.Label(body, "Legends " + owned + "/" + total, 20, HubTheme.Gold,
                            TextAnchor.MiddleRight, 0.60f, 0.90f, 0.97f, 0.98f);

            int rows = (total + Cols - 1) / Cols;
            int idx = 0;
            for (int i = 0; i < roster.Count; i++, idx++) DrawOwned(body, idx, rows, roster[i]);
            for (int i = 0; i < bonus.Length; i++, idx++) DrawBonus(body, idx, rows, bonus[i]);
        }

        static void CellRect(int index, int rows, out float x0, out float y0, out float x1, out float y1)
        {
            const float w = 0.2275f, gx = 0.0133f, gy = 0.02f, top = 0.86f, avail = 0.84f;
            float h = Mathf.Min(0.27f, (avail - (rows - 1) * gy) / rows);
            int col = index % Cols, row = index / Cols;
            x0 = 0.03f + col * (w + gx);
            x1 = x0 + w;
            y1 = top - row * (h + gy);
            y0 = y1 - h;
        }

        void DrawOwned(RectTransform body, int idx, int rows, UnitData d)
        {
            float x0, y0, x1, y1;
            CellRect(idx, rows, out x0, out y0, out x1, out y1);
            var card = HubUI.Card(body, "Legend" + idx, HubTheme.Panel, x0, y0, x1, y1, HubTheme.Bronze);

            var por = UIFactory.Panel(card.transform, "Portrait", Theme.Hex(d.PortraitHex), 0.04f, 0.30f, 0.30f, 0.92f, false);
            HubTheme.Frame(por, HubTheme.Gold);
            var ini = UIFactory.Label(por.transform, HubUI.Initials(d.Name).Substring(0, 1), 40, Color.white,
                                      TextAnchor.MiddleCenter, 0.05f, 0.05f, 0.95f, 0.95f);
            ini.fontStyle = FontStyle.Bold;

            UIFactory.Label(card.transform, d.Name, 22, HubTheme.Marble, TextAnchor.MiddleLeft, 0.33f, 0.68f, 0.97f, 0.92f);
            UIFactory.Label(card.transform, UnitData.RoleLabel(d.Role), 16, HubTheme.Gold, TextAnchor.MiddleLeft, 0.33f, 0.50f, 0.97f, 0.68f);
            UIFactory.Label(card.transform, "HP " + d.MaxHp + "  ATK " + d.Atk + "  DEF " + d.Def + "  SPD " + d.SpeedLabel,
                            13, HubTheme.Muted, TextAnchor.MiddleLeft, 0.33f, 0.30f, 0.97f, 0.50f);
            UIFactory.Label(card.transform, CostumeLine(d.Name), 13, HubTheme.Marble, TextAnchor.UpperLeft, 0.04f, 0.03f, 0.96f, 0.28f);
        }

        void DrawBonus(RectTransform body, int idx, int rows, BonusCharacter b)
        {
            float x0, y0, x1, y1;
            CellRect(idx, rows, out x0, out y0, out x1, out y1);
            var a = AchievementCatalog.Find(b.AchievementId);
            bool unlocked = a != null && a.Unlocked;

            var card = HubUI.Card(body, "Bonus" + idx, unlocked ? HubTheme.Panel : HubTheme.Deep, x0, y0, x1, y1,
                                  unlocked ? HubTheme.Gold : HubTheme.Bronze);

            var por = UIFactory.Panel(card.transform, "Portrait", unlocked ? HubTheme.Teal : Theme.Hex("#26323F"), 0.04f, 0.30f, 0.30f, 0.92f, false);
            HubTheme.Frame(por, HubTheme.Bronze);
            var ini = UIFactory.Label(por.transform, unlocked ? HubUI.Initials(b.Name).Substring(0, 1) : "?", 40,
                                      unlocked ? Color.white : HubTheme.Muted, TextAnchor.MiddleCenter, 0.05f, 0.05f, 0.95f, 0.95f);
            ini.fontStyle = FontStyle.Bold;

            UIFactory.Label(card.transform, b.Name, 22, unlocked ? HubTheme.Marble : HubTheme.Muted, TextAnchor.MiddleLeft, 0.33f, 0.68f, 0.97f, 0.92f);
            UIFactory.Label(card.transform, UnitData.RoleLabel(b.Role), 16, HubTheme.Gold, TextAnchor.MiddleLeft, 0.33f, 0.50f, 0.97f, 0.68f);
            UIFactory.Label(card.transform, unlocked ? "UNLOCKED" : "LOCKED", 14, unlocked ? Theme.Good : HubTheme.Danger,
                            TextAnchor.MiddleLeft, 0.33f, 0.30f, 0.97f, 0.50f);
            string foot = unlocked ? "Battle data is not made yet." : (a != null ? a.Desc : "");
            UIFactory.Label(card.transform, foot, 13, HubTheme.Muted, TextAnchor.UpperLeft, 0.04f, 0.03f, 0.96f, 0.28f);
        }

        static string CostumeLine(string who)
        {
            var list = AchievementCatalog.CostumesFor(who);
            if (list.Count == 0) return "Costumes: none yet";
            var names = new List<string>();
            for (int i = 0; i < list.Count; i++) if (list[i].Unlocked) names.Add(list[i].RewardName);
            string s = "Costumes " + names.Count + "/" + list.Count;
            if (names.Count > 0) s += ": " + string.Join(", ", names.ToArray());
            return s;
        }
    }
}