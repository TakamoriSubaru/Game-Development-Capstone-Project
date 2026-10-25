using System;
using System.Collections.Generic;

namespace Oddyseus.Core
{
    public enum ResourceOutcome { Safe, Costly, Perilous }

    // Node Resource (GDD bagian 4): selalu menjadi salah satu dari 3 hasil tersembunyi.
    // (1) Food saja, aman  (2) Food+ dengan HP berkurang  (3) Food++ dengan HP dan Men berkurang.
    // Semua angka = ASUMSI, ubah di sini sesuai keputusan Designer 2.
    public static class ResourceNode
    {
        public const int SafeFood = 2;
        public const int CostlyFood = 4;
        public const int PerilousFood = 6;
        public const float CostlyHpLoss = 0.10f;       // dari Max HP tiap anggota party yang hidup
        public const float PerilousHpLoss = 0.15f;
        public const int PerilousMenLoss = 1;

        // Layar menawarkan 3 pilihan; tiap pilihan terhubung ke satu hasil berbeda, urutannya diacak per node.
        public static ResourceOutcome[] Assign(int seed)
        {
            var list = new List<ResourceOutcome> { ResourceOutcome.Safe, ResourceOutcome.Costly, ResourceOutcome.Perilous };
            var rng = new Random(seed);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var t = list[i]; list[i] = list[j]; list[j] = t;
            }
            return list.ToArray();
        }

        // Terapkan hasil ke run. Mengembalikan ringkasan teks.
        public static string Apply(ResourceOutcome o)
        {
            switch (o)
            {
                case ResourceOutcome.Safe:
                    RunState.Food += SafeFood;
                    return "Food +" + SafeFood + ".";
                case ResourceOutcome.Costly:
                    RunState.Food += CostlyFood;
                    LoseHp(CostlyHpLoss);
                    return "Food +" + CostlyFood + ".\nThe party lost " + (int)(CostlyHpLoss * 100) + "% of max HP.";
                default:
                    RunState.Food += PerilousFood;
                    LoseHp(PerilousHpLoss);
                    RunState.Men = Math.Max(0, RunState.Men - PerilousMenLoss);
                    string s = "Food +" + PerilousFood + ".\nThe party lost " + (int)(PerilousHpLoss * 100)
                               + "% of max HP and " + PerilousMenLoss + " of the crew (Men -" + PerilousMenLoss + ").";
                    if (RunState.Men <= 0) s += "\nMen reached 0. The run is over.";
                    return s;
            }
        }

        // HP berkurang tapi tidak sampai gugur (minimal 1).
        static void LoseHp(float fraction)
        {
            for (int i = 0; i < DungeonState.Party.Count; i++)
            {
                var c = RunState.Roster[DungeonState.Party[i]];
                if (!c.Alive) continue;
                int loss = (int)Math.Ceiling(c.Data.MaxHp * fraction);
                c.Hp = Math.Max(1, c.Hp - loss);
            }
        }
    }

    // Rest Stop (GDD bagian 3): pilih Rest atau Shop. Shop ada di layar (belum dibuat); ini logika Rest.
    public static class RestStop
    {
        public const float RestHeal = 0.30f;           // ASUMSI: pulihkan 30% Max HP anggota party yang hidup
        public const int StaminaRecovery = 1;          // GDD: 1 poin (RunState.RestStop menambah 1)

        public static string Rest()
        {
            int healed = 0;
            for (int i = 0; i < DungeonState.Party.Count; i++)
            {
                var c = RunState.Roster[DungeonState.Party[i]];
                if (!c.Alive) continue;
                int before = c.Hp;
                c.Hp = Math.Min(c.Data.MaxHp, c.Hp + (int)Math.Ceiling(c.Data.MaxHp * RestHeal));
                if (c.Hp > before) healed++;
            }
            int stamina = RunState.RestStop();
            return "The party rested.\nHP restored for " + healed + " of the party (up to " + (int)(RestHeal * 100)
                   + "% of max HP).\nStamina +" + StaminaRecovery + " for " + stamina + " crew member(s).";
        }
    }
}
