using System.Collections.Generic;

namespace Oddyseus.Core
{
    // Data statis satu karakter atau musuh. Pool skill = 8 untuk karakter (GDD 5.2), 4 dipasang.
    public class UnitData
    {
        public string Id;
        public string Name;
        public Role Role;
        public int MaxHp;
        public int Atk;
        public int Def;
        // Speed ala Limbus: tiap ronde unit me-roll angka di antara SpeedMin dan SpeedMax. Angka tertinggi bertindak duluan.
        public int SpeedMin = 3;
        public int SpeedMax = 5;
        public int Speed { get { return (SpeedMin + SpeedMax) / 2; } }   // rata-rata, hanya untuk tampilan
        public string SpeedLabel { get { return SpeedMin == SpeedMax ? SpeedMin.ToString() : SpeedMin + "-" + SpeedMax; } }
        public string PortraitHex = "#888888";   // warna placeholder sampai ada portrait dari artist
        public List<Skill> Skills = new List<Skill>();
        public int[] Equipped = { 0, 1, 2, 3 };  // indeks ke Skills

        public List<Skill> GetEquipped()
        {
            var list = new List<Skill>();
            if (Equipped == null)
            {
                list.AddRange(Skills);
                return list;
            }
            for (int i = 0; i < Equipped.Length; i++)
            {
                int idx = Equipped[i];
                if (idx >= 0 && idx < Skills.Count) list.Add(Skills[idx]);
            }
            return list;
        }

        public static string RoleLabel(Role r)
        {
            switch (r)
            {
                case Role.DPS: return "DPS";
                case Role.Healer: return "Healer";
                case Role.DoTApplier: return "DoT";
                case Role.BufferDebuffer: return "Buffer/Debuffer";
                case Role.Tank: return "Tank";
            }
            return r.ToString();
        }
    }
}
