namespace Oddyseus.Core
{
    public class StatusEffect
    {
        public StatusType Type;
        public int TurnsLeft;     // dikurangi di AWAL giliran pemilik (setelah efek Bleed berjalan)
        public float Potency;     // AtkUp/DefUp/AtkDown/DefDown = persen (0.25 = 25%), Bleed = persen max HP per giliran, Guard = pengurangan damage
    }

    public class Skill
    {
        public string Name;
        public string Description;
        public SkillKind Kind;
        public TargetType Target;
        public float Power;               // pengali ATK untuk damage atau heal
        public bool HasStatus;
        public StatusType Status;
        public int Duration;
        public float Potency;

        public bool NeedsTargetPick
        {
            get { return Target == TargetType.SingleEnemy || Target == TargetType.SingleAlly; }
        }

        public static Skill Dmg(string name, string desc, TargetType target, float power)
        {
            return new Skill { Name = name, Description = desc, Kind = SkillKind.Damage, Target = target, Power = power };
        }

        public static Skill DmgFx(string name, string desc, TargetType target, float power, StatusType st, int dur, float pot)
        {
            return new Skill
            {
                Name = name, Description = desc, Kind = SkillKind.Damage, Target = target, Power = power,
                HasStatus = true, Status = st, Duration = dur, Potency = pot
            };
        }

        public static Skill Heal(string name, string desc, TargetType target, float power)
        {
            return new Skill { Name = name, Description = desc, Kind = SkillKind.Heal, Target = target, Power = power };
        }

        public static Skill Fx(string name, string desc, TargetType target, StatusType st, int dur, float pot)
        {
            return new Skill
            {
                Name = name, Description = desc, Kind = SkillKind.Status, Target = target,
                HasStatus = true, Status = st, Duration = dur, Potency = pot
            };
        }
    }
}
