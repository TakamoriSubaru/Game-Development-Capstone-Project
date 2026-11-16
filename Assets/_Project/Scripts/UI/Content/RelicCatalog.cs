using System.Collections.Generic;

namespace Oddyseus.UI
{
    public enum RelicTier { Common, Uncommon, Rare, Legendary }

    // Data tampilan satu relic/equipment untuk Armory.
    // Stat, Effect, dan Source TIDAK ada di manifest aset maupun di GameData saat ini, jadi dikosongkan.
    // Isi dari data game yang final (mis. Stat = "+Atk, -Speed"); Armory menampilkan "-" selama masih kosong.
    public class RelicInfo
    {
        public int N;
        public string Code, Name;
        public RelicTier Tier;
        public string IconPath;      // path di Resources/UI, tanpa ekstensi
        public string Stat = "";     // contoh: "+Atk, -Speed"  (TODO: data final)
        public string Effect = "";   // (TODO: data final)
        public string SourcePack = "";   // nama Pack asal drop (TODO: data final)

        public string TierLabel
        {
            get { return Tier == RelicTier.Legendary ? "Boss / Legendary" : Tier.ToString(); }
        }

        public string FramePath
        {
            get { return "relic_frames/frame_" + Tier.ToString().ToLowerInvariant(); }
        }
    }

    // DIHASILKAN dari docs/ui-assets/manifest.json (60 relic, 15 per tier). Jangan edit nomor/kode/nama manual.
    public static class RelicCatalog
    {
        static RelicInfo R(int n, string code, string name, RelicTier tier, string icon)
        {
            return new RelicInfo { N = n, Code = code, Name = name, Tier = tier, IconPath = icon };
        }

        public static readonly RelicInfo[] All =
        {
            R(1, "IS", "Ithacan Sandal", RelicTier.Common, "relics/relic_01_is"),
            R(2, "OB", "Olive Branch of Peace", RelicTier.Common, "relics/relic_02_ob"),
            R(3, "WO", "Wooden Oar", RelicTier.Common, "relics/relic_03_wo"),
            R(4, "BS", "Bronze Shield Rim", RelicTier.Common, "relics/relic_04_bs"),
            R(5, "PC", "Phaestus Clay Jar", RelicTier.Common, "relics/relic_05_pc"),
            R(6, "FA", "Flint Arrowhead", RelicTier.Common, "relics/relic_06_fa"),
            R(7, "TC", "Traveler's Cloak", RelicTier.Common, "relics/relic_07_tc"),
            R(8, "SC", "Sea Shell Charm", RelicTier.Common, "relics/relic_08_sc"),
            R(9, "LW", "Laurel Wreath", RelicTier.Common, "relics/relic_09_lw"),
            R(10, "SD", "Spartan Dagger", RelicTier.Common, "relics/relic_10_sd"),
            R(11, "CA", "Ceramic Amphora", RelicTier.Common, "relics/relic_11_ca"),
            R(12, "WB", "Worn Leather Belt", RelicTier.Common, "relics/relic_12_wb"),
            R(13, "HF", "Harpy Feather", RelicTier.Common, "relics/relic_13_hf"),
            R(14, "FN", "Fisherman's Net", RelicTier.Common, "relics/relic_14_fn"),
            R(15, "NT", "Nymph's Tear", RelicTier.Common, "relics/relic_15_nt"),
            R(16, "AW", "Aeolus' Wind Bag", RelicTier.Uncommon, "relics/relic_16_aw"),
            R(17, "CH", "Circe's Herb (Moly)", RelicTier.Uncommon, "relics/relic_17_ch"),
            R(18, "CE", "Cyclops Eye Gem", RelicTier.Uncommon, "relics/relic_18_ce"),
            R(19, "LF", "Lotus Flower", RelicTier.Uncommon, "relics/relic_19_lf"),
            R(20, "SE", "Sirens' Echo Shell", RelicTier.Uncommon, "relics/relic_20_se"),
            R(21, "ST", "Scylla's Tooth", RelicTier.Uncommon, "relics/relic_21_st"),
            R(22, "CW", "Charybdis Whirlpool Rune", RelicTier.Uncommon, "relics/relic_22_cw"),
            R(23, "TW", "Trojan Wooden Chip", RelicTier.Uncommon, "relics/relic_23_tw"),
            R(24, "HC", "Helios' Flame Charm", RelicTier.Uncommon, "relics/relic_24_hc"),
            R(25, "CV", "Calypso's Veil", RelicTier.Uncommon, "relics/relic_25_cv"),
            R(26, "HS", "Hermes' Winged Sandal", RelicTier.Uncommon, "relics/relic_26_hs"),
            R(27, "HH", "Hephaestus Hammer", RelicTier.Uncommon, "relics/relic_27_hh"),
            R(28, "SY", "Stygian Coin", RelicTier.Uncommon, "relics/relic_28_sy"),
            R(29, "GS", "Gorgon Shield Scale", RelicTier.Uncommon, "relics/relic_29_gs"),
            R(30, "PF", "Pan's Flute", RelicTier.Uncommon, "relics/relic_30_pf"),
            R(31, "BO", "Bow of Odysseus", RelicTier.Rare, "relics/relic_31_bo"),
            R(32, "PS", "Penelope's Shroud", RelicTier.Rare, "relics/relic_32_ps"),
            R(33, "AH", "Argus' Hundred Eyes", RelicTier.Rare, "relics/relic_33_ah"),
            R(34, "GF", "Golden Fleece", RelicTier.Rare, "relics/relic_34_gf"),
            R(35, "PE", "Pegasus Feather Footwear", RelicTier.Rare, "relics/relic_35_pe"),
            R(36, "PB", "Pandora's Mythic Box", RelicTier.Rare, "relics/relic_36_pb"),
            R(37, "MG", "Medusa's Gaze Mirror", RelicTier.Rare, "relics/relic_37_mg"),
            R(38, "CD", "Caduceus Staff", RelicTier.Rare, "relics/relic_38_cd"),
            R(39, "MH", "Minotaur Horn", RelicTier.Rare, "relics/relic_39_mh"),
            R(40, "CT", "Chiron's Teaching Scroll", RelicTier.Rare, "relics/relic_40_ct"),
            R(41, "PP", "Persephone's Pomegranate", RelicTier.Rare, "relics/relic_41_pp"),
            R(42, "AC", "Ambrosia Chalice", RelicTier.Rare, "relics/relic_42_ac"),
            R(43, "OL", "Orpheus' Lyre", RelicTier.Rare, "relics/relic_43_ol"),
            R(44, "HV", "Hydra's Venom Vial", RelicTier.Rare, "relics/relic_44_hv"),
            R(45, "AG", "Achilles' Heel Gauntlet", RelicTier.Rare, "relics/relic_45_ag"),
            R(46, "CO", "Crown of Olympus", RelicTier.Legendary, "relics/relic_46_co"),
            R(47, "HT", "Heart of the Titan", RelicTier.Legendary, "relics/relic_47_ht"),
            R(48, "PT", "Poseidon's Trident", RelicTier.Legendary, "relics/relic_48_pt"),
            R(49, "ZT", "Zeus' Thunder Crest", RelicTier.Legendary, "relics/relic_49_zt"),
            R(50, "HR", "Hades' Sovereign Ring", RelicTier.Legendary, "relics/relic_50_hr"),
            R(51, "AE", "Athena's Aegis Core", RelicTier.Legendary, "relics/relic_51_ae"),
            R(52, "AB", "Ares' War Banner", RelicTier.Legendary, "relics/relic_52_ab"),
            R(53, "DG", "Dionysus' Golden Goblet", RelicTier.Legendary, "relics/relic_53_dg"),
            R(54, "AS", "Apollo's Sun Chariot", RelicTier.Legendary, "relics/relic_54_as"),
            R(55, "CB", "Cerberus Collar", RelicTier.Legendary, "relics/relic_55_cb"),
            R(56, "PM", "Prometheus' Eternal Flame", RelicTier.Legendary, "relics/relic_56_pm"),
            R(57, "GA", "Golden Apple of Discord", RelicTier.Legendary, "relics/relic_57_ga"),
            R(58, "HD", "Helm of Darkness", RelicTier.Legendary, "relics/relic_58_hd"),
            R(59, "HG", "Chrono's Hourglass", RelicTier.Legendary, "relics/relic_59_hg"),
            R(60, "OT", "Odyssey's Triumph Crest", RelicTier.Legendary, "relics/relic_60_ot"),
        };

        public const int Total = 60;
    }
}
