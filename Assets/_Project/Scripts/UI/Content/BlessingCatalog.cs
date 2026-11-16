namespace Oddyseus.UI
{
    public class BlessingInfo
    {
        public string God, Effect, Target;
        public int Price = -1;           // Gold. -1 = belum ada data
        public int StaminaCost = -1;     // -1 = belum ada data
        public string EmblemPath { get { return "blessings/blessing_" + God.ToLowerInvariant(); } }
        public string RoundPath { get { return EmblemPath + "_round"; } }
    }

    // Delapan pertama dari GDD v0.7 bagian 6.2 (efek, target, harga draft).
    // GDD hanya bilang tiap Blessing "menguras Stamina target", tanpa angka: StaminaCost = 1 adalah ASUMSI.
    // Empat terakhir (Dionysus, Aphrodite, Hestia, Nemesis) ada di aset tapi belum ada di GDD: data TBD.
    public static class BlessingCatalog
    {
        static BlessingInfo B(string god, string effect, string target, int price, int stamina)
        {
            return new BlessingInfo { God = god, Effect = effect, Target = target, Price = price, StaminaCost = stamina };
        }

        public static readonly BlessingInfo[] All =
        {
            B("Apollo", "Large heal", "1 character", 80, 1),
            B("Athena", "Speed up; reveals the next island type", "Party", 100, 1),
            B("Ares", "Large Attack boost", "1 character", 90, 1),
            B("Hephaestus", "Large Defense boost", "1 character", 90, 1),
            B("Artemis", "Higher crit chance; attacks cannot miss", "1 character", 80, 1),
            B("Demeter", "Adds Food and boosts HP regen", "Party", 70, 1),
            B("Hermes", "Character acts first in the turn order", "1 character", 60, 1),
            B("Zeus", "Imbue weapons of party members with lightning", "Party", 150, 1),
            B("Dionysus", "TBD (not in GDD v0.7)", "TBD", -1, -1),
            B("Aphrodite", "TBD (not in GDD v0.7)", "TBD", -1, -1),
            B("Hestia", "TBD (not in GDD v0.7)", "TBD", -1, -1),
            B("Nemesis", "TBD (not in GDD v0.7)", "TBD", -1, -1),
        };
    }
}
