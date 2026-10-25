namespace Oddyseus.Core
{
    public class PassiveInfo
    {
        public string Name;
        public string Desc;
    }

    // Trait = pasif tetap sejak awal (1 per karakter).
    // Talent = pasif yang terbuka saat run mencapai Floor tertentu (1 per karakter).
    // Semua isi = DRAF untuk disetujui Designer 1. Belum berefek di battle.
    public static class PassiveCatalog
    {
        // Talent terbuka saat run mencapai Floor ini. Ubah angkanya kalau mau lebih cepat atau lambat.
        public static readonly int[] TalentFloors = { 2 };

        static PassiveInfo P(string name, string desc) { return new PassiveInfo { Name = name, Desc = desc }; }

        public static PassiveInfo[] TraitsOf(string unitId)
        {
            switch (unitId)
            {
                case "odysseus": return new[] { P("Cunning Leader", "Buffs he casts last 1 turn longer.") };
                case "polites": return new[] { P("Steadfast", "+10% DEF.") };
                case "eurylochus": return new[] { P("Reckless", "+15% damage dealt, +10% damage taken.") };
                case "elpenor": return new[] { P("Wine-Soaked", "Bleed he applies deals 20% more damage.") };
                case "perimedes": return new[] { P("Steady Hands", "Heals he casts are 15% stronger.") };
                case "eurybates": return new[] { P("Herald's Poise", "Immune to Atk Down.") };
            }
            return new PassiveInfo[0];
        }

        public static PassiveInfo[] TalentsOf(string unitId)
        {
            switch (unitId)
            {
                case "odysseus": return new[] { P("Heart of Ithaca", "Once per battle, survives a lethal hit with 1 HP.") };
                case "polites": return new[] { P("Iron Skin", "+15% Max HP.") };
                case "eurylochus": return new[] { P("Sharp Eye", "First attack each battle deals +30% damage.") };
                case "elpenor": return new[] { P("Spreading Rot", "When a Bleeding enemy dies, Bleed spreads to another enemy.") };
                case "perimedes": return new[] { P("Mender's Vow", "Allies he heals gain +10% DEF for 2 turns.") };
                case "eurybates": return new[] { P("Keen Strike", "Attacks ignore 15% of enemy DEF.") };
            }
            return new PassiveInfo[0];
        }
    }
}