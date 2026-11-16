using System;
using System.Collections.Generic;
using UnityEngine;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Progres meta (di luar satu run) yang ditampilkan layar Legends, Compendium, Armory, Achievements.
    // Belum ada sistem save di game, jadi ini disimpan di PlayerPrefs sebagai JSON sederhana.
    // Belum ada hook gameplay yang memanggil Mark*; gameplay logic sengaja tidak diubah di UI pass ini.
    public static class MetaProgress
    {
        [Serializable]
        class Dto
        {
            public List<int> discovered = new List<int>();
            public List<int> cleared = new List<int>();
            public List<int> relics = new List<int>();
            public List<int> achievements = new List<int>();
            public List<string> legends = new List<string>();
        }

        const string Key = "oddyseus_meta_v1";
        static Dto d;

        static Dto Data
        {
            get
            {
                if (d != null) return d;
                string json = PlayerPrefs.GetString(Key, "");
                if (!string.IsNullOrEmpty(json)) { try { d = JsonUtility.FromJson<Dto>(json); } catch (Exception) { d = null; } }
                if (d == null) d = Defaults();
                return d;
            }
        }

        // Keadaan awal: 6 Legends dari GameData terbuka, Pack 1 ditemukan, sisanya kosong.
        static Dto Defaults()
        {
            var n = new Dto();
            n.discovered.Add(1);
            foreach (var u in GameData.CreateRoster()) n.legends.Add(u.Id);
            return n;
        }

        static void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data)); PlayerPrefs.Save(); }

        // ---------- Pack ----------
        // Pack yang sedang ditawarkan atau dimainkan di run aktif ikut dihitung "ditemukan" (hanya dibaca, tidak ditulis).
        public static bool PackDiscovered(int id)
        {
            if (Data.discovered.Contains(id) || Data.cleared.Contains(id)) return true;
            if (DungeonState.Active)
            {
                if (DungeonState.CurrentPack != null && DungeonState.CurrentPack.Id == id) return true;
                for (int i = 0; i < DungeonState.Offered.Count; i++) if (DungeonState.Offered[i].Id == id) return true;
            }
            return false;
        }
        public static bool PackCleared(int id) { return Data.cleared.Contains(id); }
        public static int DiscoveredCount { get { int c = 0; for (int i = 1; i <= PackCatalog.All.Length; i++) if (PackDiscovered(i)) c++; return c; } }
        public static int ClearedCount { get { return Data.cleared.Count; } }
        public static void MarkPackDiscovered(int id) { if (!Data.discovered.Contains(id)) { Data.discovered.Add(id); Save(); } }
        public static void MarkPackCleared(int id) { MarkPackDiscovered(id); if (!Data.cleared.Contains(id)) { Data.cleared.Add(id); Save(); } }

        // ---------- Relic ----------
        public static bool RelicFound(int n) { return Data.relics.Contains(n); }
        public static int RelicFoundCount { get { return Data.relics.Count; } }
        public static void MarkRelicFound(int n) { if (!Data.relics.Contains(n)) { Data.relics.Add(n); Save(); } }

        // ---------- Legends ----------
        public static bool LegendUnlocked(string id) { return Data.legends.Contains(id); }
        public static int LegendUnlockedCount { get { return Data.legends.Count; } }
        public static void UnlockLegend(string id) { if (!Data.legends.Contains(id)) { Data.legends.Add(id); Save(); } }

        // ---------- Achievements ----------
        public static bool AchievementDone(int id) { return Data.achievements.Contains(id); }
        public static void MarkAchievementDone(int id) { if (!Data.achievements.Contains(id)) { Data.achievements.Add(id); Save(); } }

        // ---------- Debug (hanya dipanggil dari tombol debug di debug build) ----------
        public static void DebugUnlockAll()
        {
            var n = new Dto();
            for (int i = 1; i <= PackCatalog.All.Length; i++) { n.discovered.Add(i); if (i % 2 == 1) n.cleared.Add(i); }
            for (int i = 1; i <= RelicCatalog.Total; i += 2) n.relics.Add(i);
            for (int i = 1; i <= AchievementCatalog.All.Length; i += 3) n.achievements.Add(i);
            foreach (var l in LegendCatalog.All) n.legends.Add(l.Id);
            d = n; Save();
        }

        public static void DebugReset() { d = Defaults(); Save(); }
    }
}
