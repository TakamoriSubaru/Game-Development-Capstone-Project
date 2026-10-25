using System.Collections.Generic;

namespace Oddyseus.Core
{
    public class Encounter
    {
        public string Name;
        public BattleEnvironment Environment;
        public string[] EnemyIds;
        public int GoldReward;
    }

    // SEMUA DATA DI FILE INI ADALAH PLACEHOLDER untuk vertical slice.
    // Designer 1 yang memfinalkan roster, 8 skill per karakter, dan angka balance (GDD 11.2).
    public static class GameData
    {
        static UnitData Make(string id, string name, Role role, int hp, int atk, int def, int spdMin, int spdMax, string hex, params Skill[] skills)
        {
            var d = new UnitData { Id = id, Name = name, Role = role, MaxHp = hp, Atk = atk, Def = def, SpeedMin = spdMin, SpeedMax = spdMax, PortraitHex = hex };
            d.Skills.AddRange(skills);
            return d;
        }

        // Urutan 4 skill pertama = skill yang dipasang: [inti, inti kedua, dasar, defense]. 4 sisanya = pool cadangan.
        public static List<UnitData> CreateRoster()
        {
            var list = new List<UnitData>();

            list.Add(Make("odysseus", "Odysseus", Role.BufferDebuffer, 100, 22, 12, 3, 7, "#C9A24A",
                Skill.Fx("Rally the Crew", "Whole crew: ATK +25%.", TargetType.AllAllies, StatusType.AtkUp, 3, 0.25f),
                Skill.Fx("Mark the Weakness", "1 enemy: DEF -25%.", TargetType.SingleEnemy, StatusType.DefDown, 3, 0.25f),
                Skill.Dmg("Spear Thrust", "Attack 1 enemy.", TargetType.SingleEnemy, 1.0f),
                Skill.Fx("Raise Shield", "Self: damage taken -50% until your next turn.", TargetType.Self, StatusType.Guard, 1, 0.5f),
                Skill.Dmg("Wooden Horse Gambit", "Weak attack on all enemies.", TargetType.AllEnemies, 0.6f),
                Skill.Fx("Voice of Command", "1 ally: ATK +40%.", TargetType.SingleAlly, StatusType.AtkUp, 3, 0.4f),
                Skill.DmgFx("Feint", "Attack 1 enemy and lower its ATK by 25%.", TargetType.SingleEnemy, 0.7f, StatusType.AtkDown, 2, 0.25f),
                Skill.Heal("Second Wind", "Heal yourself.", TargetType.Self, 0.8f)));

            list.Add(Make("polites", "Polites", Role.Tank, 150, 16, 22, 2, 4, "#5C7A9E",
                Skill.Dmg("Shield Bash", "Attack 1 enemy.", TargetType.SingleEnemy, 1.0f),
                Skill.DmgFx("Taunting Roar", "Attack 1 enemy and lower its ATK by 25%.", TargetType.SingleEnemy, 0.6f, StatusType.AtkDown, 2, 0.25f),
                Skill.Dmg("Spear Jab", "Light attack on 1 enemy.", TargetType.SingleEnemy, 0.8f),
                Skill.Fx("Hold the Line", "Self: damage taken -60%.", TargetType.Self, StatusType.Guard, 1, 0.6f),
                Skill.Fx("Shield Wall", "Whole crew: damage taken -30%.", TargetType.AllAllies, StatusType.Guard, 1, 0.3f),
                Skill.Fx("Brace", "Self: DEF +40%.", TargetType.Self, StatusType.DefUp, 3, 0.4f),
                Skill.Dmg("Sweeping Bash", "Weak attack on all enemies.", TargetType.AllEnemies, 0.5f),
                Skill.Heal("Endure", "Heal yourself.", TargetType.Self, 1.5f)));

            list.Add(Make("eurylochus", "Eurylochus", Role.DPS, 90, 30, 8, 4, 8, "#A5483A",
                Skill.Dmg("Reckless Slash", "Strong attack on 1 enemy.", TargetType.SingleEnemy, 1.3f),
                Skill.Dmg("Spear Volley", "Attack all enemies.", TargetType.AllEnemies, 0.7f),
                Skill.Dmg("Sword Strike", "Attack 1 enemy.", TargetType.SingleEnemy, 1.0f),
                Skill.Fx("Duck and Cover", "Self: damage taken -50%.", TargetType.Self, StatusType.Guard, 1, 0.5f),
                Skill.DmgFx("Doubt the Plan", "Attack 1 enemy and lower its DEF by 20%.", TargetType.SingleEnemy, 0.8f, StatusType.DefDown, 3, 0.2f),
                Skill.Dmg("Desperate Lunge", "Very strong attack on 1 enemy.", TargetType.SingleEnemy, 1.6f),
                Skill.Fx("Bloodlust", "Self: ATK +30%.", TargetType.Self, StatusType.AtkUp, 3, 0.3f),
                Skill.Heal("Bandage", "Heal yourself.", TargetType.Self, 0.8f)));

            list.Add(Make("elpenor", "Elpenor", Role.DoTApplier, 85, 24, 8, 3, 6, "#7E8B52",
                Skill.DmgFx("Poison Arrow", "Attack 1 enemy + Bleed 6% max HP per turn.", TargetType.SingleEnemy, 0.6f, StatusType.Bleed, 3, 0.06f),
                Skill.DmgFx("Burning Brand", "Attack all enemies + Bleed 5%.", TargetType.AllEnemies, 0.4f, StatusType.Bleed, 3, 0.05f),
                Skill.Dmg("Dagger", "Attack 1 enemy.", TargetType.SingleEnemy, 1.0f),
                Skill.Fx("Slip Away", "Self: damage taken -50%.", TargetType.Self, StatusType.Guard, 1, 0.5f),
                Skill.DmgFx("Rotten Wine", "Attack 1 enemy + Bleed 8% (4 turns).", TargetType.SingleEnemy, 0.5f, StatusType.Bleed, 4, 0.08f),
                Skill.DmgFx("Cut Deep", "Attack 1 enemy + Bleed 10% (2 turns).", TargetType.SingleEnemy, 0.8f, StatusType.Bleed, 2, 0.1f),
                Skill.Fx("Weaken", "1 enemy: ATK -30%.", TargetType.SingleEnemy, StatusType.AtkDown, 3, 0.3f),
                Skill.Heal("Drink", "Heal yourself.", TargetType.Self, 0.9f)));

            list.Add(Make("perimedes", "Perimedes", Role.Healer, 95, 20, 10, 3, 5, "#6FA8A0",
                Skill.Heal("Salve", "Heal 1 ally.", TargetType.SingleAlly, 1.8f),
                Skill.Heal("Mend the Crew", "Heal the whole crew.", TargetType.AllAllies, 1.0f),
                Skill.Dmg("Staff Strike", "Attack 1 enemy.", TargetType.SingleEnemy, 0.9f),
                Skill.Fx("Brace", "Self: damage taken -50%.", TargetType.Self, StatusType.Guard, 1, 0.5f),
                Skill.Fx("Fortify", "1 ally: DEF +30%.", TargetType.SingleAlly, StatusType.DefUp, 3, 0.3f),
                Skill.Heal("Blessing of Rest", "Big heal on 1 ally.", TargetType.SingleAlly, 2.4f),
                Skill.Fx("Shaming Words", "1 enemy: ATK -25%.", TargetType.SingleEnemy, StatusType.AtkDown, 3, 0.25f),
                Skill.Dmg("Staff Sweep", "Weak attack on all enemies.", TargetType.AllEnemies, 0.5f)));

            list.Add(Make("eurybates", "Eurybates", Role.DPS, 105, 26, 14, 3, 6, "#B88AB0",
                Skill.Dmg("Heavy Blow", "Strong attack on 1 enemy.", TargetType.SingleEnemy, 1.2f),
                Skill.Dmg("Whirling Strike", "Attack all enemies.", TargetType.AllEnemies, 0.65f),
                Skill.Dmg("Sword Strike", "Attack 1 enemy.", TargetType.SingleEnemy, 1.0f),
                Skill.Fx("Parry", "Self: damage taken -60%.", TargetType.Self, StatusType.Guard, 1, 0.6f),
                Skill.DmgFx("Pierce Armor", "Attack 1 enemy and lower its DEF by 25%.", TargetType.SingleEnemy, 0.9f, StatusType.DefDown, 3, 0.25f),
                Skill.Fx("Battle Fury", "Self: ATK +35%.", TargetType.Self, StatusType.AtkUp, 3, 0.35f),
                Skill.DmgFx("Pommel Strike", "Attack 1 enemy and lower its ATK by 20%.", TargetType.SingleEnemy, 0.7f, StatusType.AtkDown, 2, 0.2f),
                Skill.Heal("Catch Breath", "Heal yourself.", TargetType.Self, 1.0f)));

            return list;
        }

        // Pengali kekuatan semua musuh: satu tempat untuk menaikkan atau menurunkan kesulitan.
        public const float EnemyHpScale = 1.5f;
        public const float EnemyAtkScale = 1.6f;

        public static UnitData CreateEnemy(string id)
        {
            var d = BuildEnemy(id);
            if (d == null) return null;
            d.MaxHp = (int)System.Math.Round(d.MaxHp * EnemyHpScale);
            d.Atk = (int)System.Math.Round(d.Atk * EnemyAtkScale);
            return d;
        }

        static UnitData BuildEnemy(string id)
        {
            switch (id)
            {
                case "cicone_warrior":
                    return Make(id, "Cicone Warrior", Role.DPS, 70, 18, 8, 2, 5, "#8C6D4F",
                        Skill.Dmg("Slash", "Attack 1 foe.", TargetType.SingleEnemy, 1.0f),
                        Skill.Dmg("Shield Bash", "Light attack on 1 foe.", TargetType.SingleEnemy, 0.8f));
                case "cicone_archer":
                    return Make(id, "Cicone Archer", Role.DoTApplier, 50, 20, 5, 4, 7, "#9A7E5A",
                        Skill.Dmg("Arrow", "Attack 1 foe.", TargetType.SingleEnemy, 1.0f),
                        Skill.DmgFx("Poison Arrow", "Attack + Bleed 5%.", TargetType.SingleEnemy, 0.6f, StatusType.Bleed, 3, 0.05f));
                case "cicone_chief":
                    return Make(id, "Cicone Chieftain", Role.Tank, 120, 24, 14, 2, 4, "#6B4E3A",
                        Skill.Dmg("Cleave", "Weak attack on all foes.", TargetType.AllEnemies, 0.6f),
                        Skill.Dmg("Heavy Blow", "Strong attack on 1 foe.", TargetType.SingleEnemy, 1.2f),
                        Skill.Fx("Battle Cry", "Whole squad: ATK +25%.", TargetType.AllAllies, StatusType.AtkUp, 3, 0.25f));
                case "sea_raider":
                    return Make(id, "Sea Raider", Role.DPS, 65, 19, 8, 3, 6, "#4F6F8C",
                        Skill.Dmg("Boarding Axe", "Attack 1 foe.", TargetType.SingleEnemy, 1.0f),
                        Skill.DmgFx("Grapple", "Light attack + foe DEF -20%.", TargetType.SingleEnemy, 0.6f, StatusType.DefDown, 3, 0.2f));
                case "polyphemus":
                    return Make(id, "Polyphemus", Role.Tank, 300, 28, 12, 1, 3, "#5A4636",
                        Skill.Dmg("Boulder Slam", "Strong attack on 1 foe.", TargetType.SingleEnemy, 1.3f),
                        Skill.Dmg("Sweeping Arm", "Attack all foes.", TargetType.AllEnemies, 0.7f),
                        Skill.Fx("Roar", "Self: ATK +30%.", TargetType.Self, StatusType.AtkUp, 3, 0.3f));
                case "shade":
                    return Make(id, "Restless Shade", Role.DoTApplier, 55, 20, 6, 3, 6, "#8E95A8",
                        Skill.DmgFx("Chill Touch", "Attack + Bleed 6%.", TargetType.SingleEnemy, 0.8f, StatusType.Bleed, 3, 0.06f),
                        Skill.Fx("Wail", "1 foe: ATK -25%.", TargetType.SingleEnemy, StatusType.AtkDown, 3, 0.25f));
                case "wild_beast":
                    return Make(id, "Circe's Beast", Role.DPS, 80, 22, 8, 4, 7, "#7A8C54",
                        Skill.Dmg("Bite", "Attack 1 foe.", TargetType.SingleEnemy, 1.0f),
                        Skill.Dmg("Maul", "Strong attack on 1 foe.", TargetType.SingleEnemy, 1.3f));
            }
            return null;
        }

        public static Encounter GetEncounter(BattleEnvironment env)
        {
            switch (env)
            {
                case BattleEnvironment.OpenSea:
                    return new Encounter { Name = "Sea Raiders", Environment = env, GoldReward = 15, EnemyIds = new[] { "sea_raider", "sea_raider", "sea_raider" } };
                case BattleEnvironment.CoastalRaid:
                    return new Encounter { Name = "Cicone Raid", Environment = env, GoldReward = 15, EnemyIds = new[] { "cicone_warrior", "cicone_archer", "cicone_chief" } };
                case BattleEnvironment.CaveLair:
                    return new Encounter { Name = "Polyphemus' Cave", Environment = env, GoldReward = 25, EnemyIds = new[] { "polyphemus" } };
                case BattleEnvironment.Underworld:
                    return new Encounter { Name = "Shades of Hades", Environment = env, GoldReward = 20, EnemyIds = new[] { "shade", "shade", "shade" } };
                default:
                    return new Encounter { Name = "Beasts of Aeaea", Environment = env, GoldReward = 10, EnemyIds = new[] { "wild_beast", "wild_beast" } };
            }
        }
    }
}
