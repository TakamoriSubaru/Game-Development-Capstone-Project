using System.Collections.Generic;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    public class LegendInfo
    {
        public string Id, Name;
        public UnitData Data;          // null = belum ada data game (karakter terkunci)
        public string UnlockHint
        {
            get
            {
                var a = AchievementCatalog.ForCharacter(Id);
                return a != null ? "Unlock: " + a.Title : "Unlock: TBD";
            }
        }
        public string RoundPath { get { return "portraits/portrait_" + Id + "_round"; } }
        public string SquarePath { get { return "portraits/portrait_" + Id; } }
        public bool Unlocked { get { return MetaProgress.LegendUnlocked(Id); } }
    }

    // 9 Legends sesuai aset potret. Enam pertama memakai data asli dari GameData (peran, HP/ATK/DEF/SPD dibaca dari sana,
    // BUKAN dari mockup). Tiga terakhir belum punya data karakter, jadi tampil terkunci.
    public static class LegendCatalog
    {
        public const int CostumesPerLegend = 3;   // PLACEHOLDER: sistem kostum belum ada

        static LegendInfo[] all;

        public static LegendInfo[] All
        {
            get
            {
                if (all != null) return all;
                var byId = new Dictionary<string, UnitData>();
                foreach (var u in GameData.CreateRoster()) byId[u.Id] = u;
                string[] ids = { "odysseus", "polites", "eurylochus", "elpenor", "perimedes", "eurybates", "ajax", "circe", "eurycleia" };
                string[] names = { "Odysseus", "Polites", "Eurylochus", "Elpenor", "Perimedes", "Eurybates", "Ajax the Great", "Circe", "Eurycleia" };
                all = new LegendInfo[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    UnitData u;
                    byId.TryGetValue(ids[i], out u);
                    all[i] = new LegendInfo { Id = ids[i], Name = u != null ? u.Name : names[i], Data = u };
                }
                return all;
            }
        }

        public static int UnlockedCount
        {
            get { int c = 0; foreach (var l in All) if (l.Unlocked) c++; return c; }
        }

        // Kostum yang dimiliki: PLACEHOLDER. Hanya kostum bawaan (1) untuk Legend yang terbuka.
        public static int CostumesOwned(LegendInfo l) { return l.Unlocked ? 1 : 0; }
    }
}
