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
        public float Av;                 // action value: makin kecil makin cepat dapat giliran
        public int RosterIndex = -1;     // indeks di RunState.Roster (khusus ally)
        public List<StatusEffect> Statuses = new List<StatusEffect>();

        public Unit(UnitData data, bool isEnemy, int hp)
        {
            Data = data;
            IsEnemy = isEnemy;
            Hp = Math.Max(0, Math.Min(hp, data.MaxHp));
        }

        public string Name { get { return Data.Name; } }
        public int MaxHp { get { return Data.MaxHp; } }

        public float AtkMult { get { return Mult(StatusType.AtkUp, StatusType.AtkDown); } }
        public float DefMult { get { return Mult(StatusType.DefUp, StatusType.DefDown); } }

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
                case StatusType.Bleed: return "Luka";
                case StatusType.Guard: return "Jaga";
            }
            return t.ToString();
        }
    }
}
