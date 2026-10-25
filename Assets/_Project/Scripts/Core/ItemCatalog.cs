namespace Oddyseus.Core
{
    public class EquipmentInfo
    {
        public int PackId;
        public string Name;
        public string Effect;
    }

    // Satu equipment khusus per Pack, pasti didapat setelah boss Pack itu (GDD 3.2).
    // Nama dan efek = DRAF untuk disetujui Designer 2.
    public static class EquipmentCatalog
    {
        static EquipmentInfo E(int pack, string name, string effect)
        {
            return new EquipmentInfo { PackId = pack, Name = name, Effect = effect };
        }

        public static readonly EquipmentInfo[] All =
        {
            E(1, "Cicone Spearhead", "Attacks can inflict Bleed."),
            E(2, "Lotus Garland", "Enemies start the battle with Atk Down."),
            E(3, "Club Splinter", "+Atk, -Speed."),
            E(4, "Triton's Conch", "One ally starts the battle with Guard."),
            E(5, "Bag of Winds", "Allies gain +Speed in round 1."),

            E(6, "Boulder Sling", "+Atk in round 1."),
            E(7, "Moly Herb", "Immune to Atk Down and Def Down."),
            E(8, "Wax Earplugs", "Debuffs on your party last 1 turn shorter."),
            E(9, "Cattle Bell", "+25% Gold from battles."),
            E(10, "Rock Plate", "+Def while HP is below 50%."),

            E(11, "Ajax's Shield Shard", "Reflects 15% of damage taken."),
            E(12, "Boulder Chip", "+Def, -Speed."),
            E(13, "Gorgon's Eye", "One random enemy loses its first turn."),
            E(14, "Scylla's Fang", "Attacks on all enemies deal +25% damage."),
            E(15, "Whirlpool Vial", "Heal 10% of damage dealt."),

            E(16, "Lock of Calypso", "+1 Stamina for everyone at the start of each Map."),
            E(17, "Trident Prong", "+30% Atk for the first two turns."),
            E(18, "Owl's Insight", "\"?\" nodes reveal their true type."),
            E(19, "Suitor's Bow", "The first attack of each battle always crits."),
            E(20, "Great Bow", "Odysseus's first attack hits all enemies."),
        };

        public static EquipmentInfo ForPack(int packId)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].PackId == packId) return All[i];
            return null;
        }

        public static PackInfo PackOf(EquipmentInfo e)
        {
            for (int i = 0; i < PackCatalog.All.Length; i++) if (PackCatalog.All[i].Id == e.PackId) return PackCatalog.All[i];
            return null;
        }
    }

    public class BlessingInfo
    {
        public string God;
        public string Effect;
        public string Target;
        public int Gold;
    }

    // Data dari GDD v0.7 bagian 6.2. Harga masih estimasi.
    public static class BlessingCatalog
    {
        static BlessingInfo B(string god, string effect, string target, int gold)
        {
            return new BlessingInfo { God = god, Effect = effect, Target = target, Gold = gold };
        }

        public static readonly BlessingInfo[] All =
        {
            B("Apollo", "Large heal.", "1 character", 80),
            B("Athena", "Speed up; reveals the next island type.", "Party", 100),
            B("Ares", "Large Attack boost.", "1 character", 90),
            B("Hephaestus", "Large Defense boost.", "1 character", 90),
            B("Artemis", "Higher crit chance; attacks cannot miss.", "1 character", 80),
            B("Demeter", "Adds Food and boosts HP regen.", "Party", 70),
            B("Hermes", "Character acts first in the turn order.", "1 character", 60),
            B("Zeus", "Lightning strike on all enemies.", "Party", 150),
        };
    }
}