using System;
using System.Collections.Generic;

namespace Oddyseus.Core
{
    public class CharacterState
    {
        public UnitData Data;
        public int Hp;
        public bool Alive;
        public int Stamina;
        public int MaxStamina;
    }

    // Status satu run Dungeon Mode. Static supaya bertahan antar scene. Belum ada save ke disk.
    public static class RunState
    {
        // Angka awal = tebakan placeholder, ubah sesuai keputusan Designer 2.
        public const int StartMen = 12;
        public const int StartFood = 4;
        public const int StartGold = 0;
        public const int DefaultMaxStamina = 2;
        public const int MenLostPerDeath = 1;          // ASUMSI: tiap karakter gugur mengurangi Men
        public const float NoFoodHpLoss = 0.10f;       // GDD 6.1
        public const float FoodRegen = 0.05f;          // GDD 6.1
        public const int FoodCostPerBattle = 1;        // ASUMSI: 1 Food terpakai tiap masuk Battle Island
        public const int PartySize = 4;

        public static bool Active;
        public static int Men, Food, Gold, BattlesWon;
        public static List<CharacterState> Roster = new List<CharacterState>();
        public static List<int> Fielded = new List<int>();
        public static BattleEnvironment SelectedEnv = BattleEnvironment.CoastalRaid;
        public static bool FoodWasSufficient;

        public static bool RunOver { get { return Men <= 0; } }

        public static void NewRun()
        {
            Men = StartMen; Food = StartFood; Gold = StartGold; BattlesWon = 0;
            Roster.Clear();
            Fielded.Clear();
            foreach (var d in GameData.CreateRoster())
                Roster.Add(new CharacterState { Data = d, Hp = d.MaxHp, Alive = true, Stamina = DefaultMaxStamina, MaxStamina = DefaultMaxStamina });
            Active = true;
        }

        public static void EnsureRun()
        {
            if (!Active || Roster.Count == 0) NewRun();
        }

        public static bool CanField(CharacterState c)
        {
            return c.Alive && c.Stamina > 0;
        }

        // Dipanggil saat masuk Battle Island. Mengembalikan catatan untuk ditampilkan.
        public static string PrepareBattle(List<int> picks)
        {
            Fielded = new List<int>(picks);
            string note = "";
            for (int i = 0; i < Fielded.Count; i++) Roster[Fielded[i]].Stamina = Math.Max(0, Roster[Fielded[i]].Stamina - 1);

            if (Food >= FoodCostPerBattle)
            {
                Food -= FoodCostPerBattle;
                FoodWasSufficient = true;
            }
            else
            {
                FoodWasSufficient = false;
                for (int i = 0; i < Fielded.Count; i++)
                {
                    var c = Roster[Fielded[i]];
                    int loss = (int)Math.Ceiling(c.Data.MaxHp * NoFoodHpLoss);
                    c.Hp = Math.Max(1, c.Hp - loss);   // tidak sampai gugur karena lapar
                }
                note = "Tanpa Food: party kehilangan 10% max HP.";
            }
            return note;
        }

        public static List<Unit> BuildAllies()
        {
            var list = new List<Unit>();
            for (int i = 0; i < Fielded.Count; i++)
            {
                var c = Roster[Fielded[i]];
                list.Add(new Unit(c.Data, false, c.Hp) { RosterIndex = Fielded[i] });
            }
            return list;
        }

        // Simpan hasil battle ke run. Mengembalikan ringkasan teks.
        public static string CommitBattle(List<Unit> allies, bool victory, int goldReward)
        {
            int deaths = 0;
            var lines = new List<string>();
            for (int i = 0; i < allies.Count; i++)
            {
                var u = allies[i];
                var c = Roster[u.RosterIndex];
                c.Hp = u.Hp;
                if (!u.Alive)
                {
                    c.Alive = false;
                    c.Hp = 0;
                    deaths++;
                }
            }
            if (deaths > 0)
            {
                Men = Math.Max(0, Men - deaths * MenLostPerDeath);
                lines.Add("Gugur: " + deaths + " (Men -" + (deaths * MenLostPerDeath) + ").");
            }
            if (victory)
            {
                BattlesWon++;
                Gold += goldReward;
                lines.Add("Gold +" + goldReward + ".");
                if (FoodWasSufficient)
                {
                    for (int i = 0; i < allies.Count; i++)
                    {
                        var c = Roster[allies[i].RosterIndex];
                        if (!c.Alive) continue;
                        int heal = (int)Math.Ceiling(c.Data.MaxHp * FoodRegen);
                        c.Hp = Math.Min(c.Data.MaxHp, c.Hp + heal);
                    }
                    lines.Add("Food cukup: seluruh party pulih 5% max HP.");
                }
            }
            else
            {
                lines.Add("Party kalah.");
            }
            if (Men <= 0) lines.Add("Men habis. Run berakhir.");
            return string.Join("\n", lines.ToArray());
        }

        // Titik pemulihan Stamina (rest stop). Jumlah pemulihan masih TBD di GDD, di sini +1.
        public static int RestStop()
        {
            int n = 0;
            for (int i = 0; i < Roster.Count; i++)
            {
                var c = Roster[i];
                if (c.Alive && c.Stamina < c.MaxStamina) { c.Stamina++; n++; }
            }
            return n;
        }
    }
}
