using System.Collections.Generic;

namespace Oddyseus.Core
{
    // Satu Pack = satu Map di Dungeon Mode. Data dari doc "Dungeon Mode: 20 Pack dan 4 Floor".
    public class PackInfo
    {
        public int Id;
        public int Floor;
        public string Name;
        public BattleEnvironment Env;
        public string Boss;
        public string Theme;
        public float GodRealmChance;
    }

    public static class PackCatalog
    {
        static PackInfo P(int id, int floor, string name, BattleEnvironment env, string boss, string theme, float god)
        {
            return new PackInfo { Id = id, Floor = floor, Name = name, Env = env, Boss = boss, Theme = theme, GodRealmChance = god };
        }

        public static readonly PackInfo[] All =
        {
            P(1, 1, "Ismaros", BattleEnvironment.CoastalRaid, "Cicone Warlord", "Bleed", 0f),
            P(2, 1, "Lotus Shore", BattleEnvironment.OpenWilds, "Lotus Elder", "Lull", 0f),
            P(3, 1, "Cyclops Cave", BattleEnvironment.CaveLair, "Polyphemus", "Crush", 0f),
            P(4, 1, "Triton's Strait", BattleEnvironment.OpenSea, "Triton", "Guard", 0f),
            P(5, 1, "Aeolus's Isle", BattleEnvironment.OpenWilds, "Aeolus", "Tempo", 1f),

            P(6, 2, "Laestrygonian Harbor", BattleEnvironment.CoastalRaid, "King Antiphates", "Burst", 0f),
            P(7, 2, "Circe's Garden", BattleEnvironment.OpenWilds, "Circe", "Hex", 0f),
            P(8, 2, "Sirens' Reef", BattleEnvironment.OpenSea, "The Sirens", "Charm", 0f),
            P(9, 2, "Thrinacia", BattleEnvironment.OpenWilds, "Lampetia", "Greed", 0.3f),
            P(10, 2, "Planctae", BattleEnvironment.CoastalRaid, "Planctae", "Pinch", 0f),

            P(11, 3, "Gate of Nekyia", BattleEnvironment.Underworld, "Shade of Ajax", "Reflect", 0f),
            P(12, 3, "Tartarus", BattleEnvironment.Underworld, "Sisyphus", "Burden", 0f),
            P(13, 3, "Persephone's Threshold", BattleEnvironment.Underworld, "Gorgon's Shade", "Petrify", 0.3f),
            P(14, 3, "Scylla's Lair", BattleEnvironment.CaveLair, "Scylla", "Multi-Target", 0f),
            P(15, 3, "Charybdis", BattleEnvironment.OpenSea, "Charybdis", "Drain", 0f),

            P(16, 4, "Ogygia", BattleEnvironment.OpenWilds, "Calypso", "Stasis", 0f),
            P(17, 4, "Poseidon's Wrath", BattleEnvironment.OpenSea, "Poseidon", "Storm", 0f),
            P(18, 4, "Athena's Mind-Palace", BattleEnvironment.OpenWilds, "Athena's Trial", "Mirror", 1f),
            P(19, 4, "Ithaca Shore", BattleEnvironment.CoastalRaid, "Eurymachus", "Ambush", 0f),
            P(20, 4, "Ithaca Hall", BattleEnvironment.CaveLair, "Antinous", "Siege", 0f),
        };

        public static List<PackInfo> ForFloor(int floor)
        {
            var list = new List<PackInfo>();
            for (int i = 0; i < All.Length; i++)
                if (All[i].Floor == floor) list.Add(All[i]);
            return list;
        }
    }
}
