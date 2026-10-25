namespace Oddyseus.Core
{
    // Efek battle untuk relic (DungeonState.Equipment), dicari lewat nama. Nama harus sama dengan EquipmentCatalog.
    // Relic yang tidak ada di sini tetap bisa dimiliki dan tampil di top bar, tapi belum berefek di battle.
    //
    // MENAMBAH EFEK = tambah satu case. Hanya efek stat (ATK/DEF/Heal/Speed) yang didukung di sini.
    // Efek lain (Bleed, Guard di awal battle, crit, dan sebagainya) butuh hook baru di BattleSystem.
    public static class RelicEffects
    {
        static readonly StatMod[] None = new StatMod[0];

        public static StatMod[] For(string relicName)
        {
            switch (relicName)
            {
                case "Club Splinter":  return new[] { new StatMod(StatKey.Atk, 0.15f), new StatMod(StatKey.Speed, -1f) };
                case "Boulder Chip":   return new[] { new StatMod(StatKey.Def, 0.15f), new StatMod(StatKey.Speed, -1f) };
                case "Bag of Winds":   return new[] { new StatMod(StatKey.Speed, 1f) };   // GDD: +Speed di ronde 1. Disederhanakan: +1 sepanjang battle.
                case "Boulder Sling":  return new[] { new StatMod(StatKey.Atk, 0.10f) };  // GDD: +Atk di ronde 1. Disederhanakan.
            }
            return None;
        }
    }
}
