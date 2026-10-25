using System;
using System.Collections.Generic;

namespace Oddyseus.Core
{
    // Unit yang sedang bertarung (data runtime).
    public class Unit
    {
        public UnitData Data;
        public bool IsEnemy;
        public int Hp;
        public bool Alive { get { return Hp > 0; } }
        public bool Fled;                // musuh yang kabur: keluar dari battle tanpa hadiah
        public bool Present { get { return Hp > 0 && !Fled; } }   // masih ikut bertarung
        public float Av;                 // tidak dipakai lagi (initiative sekarang di-roll per ronde)
        public int RosterIndex = -1;     // indeks di RunState.Roster (khusus ally)
        public int HealCooldown;         // giliran musuh ini yang masih terblokir untuk Heal
        public int StolenFood, StolenGold;   // curian yang dibawa (kembali ke pemain kalau unit ini mati)
        public List<StatusEffect> Statuses = new List<StatusEffect>();

        // Bonus dari Synergy dan Relic. Atk/Def/Heal = persen (0.15 = +15%), SpeedBonus = flat untuk min dan max.
        public float AtkBonus, DefBonus, HealBonus;
        public int SpeedBonus;
        public int Speed;                // hasil roll ronde ini (0 sebelum ronde pertama)

        public int SpeedMin { get { return Math.Max(1, Data.SpeedMin + SpeedBonus); } }
        public int SpeedMax { get { return Math.Max(SpeedMin, Data.SpeedMax + SpeedBonus); } }

        public int RollSpeed(Random rng)
        {
            Speed = rng.Next(SpeedMin, SpeedMax + 1);
            return Speed;
        }

        public void AddMod(StatMod m)
        {
            switch (m.Stat)
            {
                case StatKey.Atk: AtkBonus += m.Value; break;
                case StatKey.Def: DefBonus += m.Value; break;
                case StatKey.Heal: HealBonus += m.Value; break;
                case StatKey.Speed: SpeedBonus += (int)Math.Round(m.Value); break;
            }
        }

        public Unit(UnitData data, bool isEnemy, int hp)
        {
            Data = data;
            IsEnemy = isEnemy;
            Hp = Math.Max(0, Math.Min(hp, data.MaxHp));
        }

        public string Name { get { return Data.Name; } }
        public int MaxHp { get { return Data.MaxHp; } }

        public float AtkMult { get { return Mult(StatusType.AtkUp, StatusType.AtkDown) * Math.Max(0.2f, 1f + AtkBonus); } }
        public float DefMult { get { return Mult(StatusType.DefUp, StatusType.DefDown) * Math.Max(0.2f, 1f + DefBonus); } }

        float Mult(StatusType up, StatusType down)
        {
            float m = 1f;
            for (int i = 0; i < Statuses.Count; i++)
            {
                if (Statuses[i].Type == up) m += Statuses[i].Potency;
                else if (Statuses[i].Type == down) m -= Statuses[i].Potency;
            }
            return Math.Max(0.2f, m);
        }

        public StatusEffect Get(StatusType t)
        {
            for (int i = 0; i < Statuses.Count; i++)
                if (Statuses[i].Type == t) return Statuses[i];
            return null;
        }

        public void AddStatus(StatusType t, int turns, float potency)
        {
            var existing = Get(t);
            if (existing != null)
            {
                existing.TurnsLeft = Math.Max(existing.TurnsLeft, turns);
                existing.Potency = Math.Max(existing.Potency, potency);
                return;
            }
            Statuses.Add(new StatusEffect { Type = t, TurnsLeft = turns, Potency = potency });
        }

        public static string StatusShort(StatusType t)
        {
            switch (t)
            {
                case StatusType.AtkUp: return "ATK+";
                case StatusType.AtkDown: return "ATK-";
                case StatusType.DefUp: return "DEF+";
                case StatusType.DefDown: return "DEF-";
                case StatusType.Bleed: return "Bleed";
                case StatusType.Guard: return "Guard";
            }
            return t.ToString();
        }
    }
}
