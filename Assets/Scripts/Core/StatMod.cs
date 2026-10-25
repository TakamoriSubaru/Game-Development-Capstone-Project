using System;

namespace Oddyseus.Core
{
    // Stat yang bisa diubah oleh Synergy dan Relic.
    //  Atk, Def, Heal : persen. 0.15 = +15%, -0.10 = -10%.
    //  Speed          : flat, menggeser SpeedMin dan SpeedMax sekaligus. 1 = +1, -1 = -1.
    public enum StatKey { Atk, Def, Heal, Speed }

    public class StatMod
    {
        public StatKey Stat;
        public float Value;

        public StatMod(StatKey stat, float value)
        {
            Stat = stat;
            Value = value;
        }

        // Teks singkat untuk UI, mis. "+15% ATK" atau "-1 SPD".
        public string Label
        {
            get
            {
                string sign = Value >= 0f ? "+" : "-";
                float v = Math.Abs(Value);
                switch (Stat)
                {
                    case StatKey.Atk: return sign + Math.Round(v * 100f) + "% ATK";
                    case StatKey.Def: return sign + Math.Round(v * 100f) + "% DEF";
                    case StatKey.Heal: return sign + Math.Round(v * 100f) + "% Healing";
                    default: return sign + Math.Round(v) + " SPD";
                }
            }
        }
    }
}
