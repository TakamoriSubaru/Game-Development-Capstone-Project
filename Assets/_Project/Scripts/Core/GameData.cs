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
        static UnitData Make(string id, string name, Role role, int hp, int atk, int def, int spd, string hex, params Skill[] skills)
        {
            var d = new UnitData { Id = id, Name = name, Role = role, MaxHp = hp, Atk = atk, Def = def, Speed = spd, PortraitHex = hex };
            d.Skills.AddRange(skills);
            return d;
        }

        // Urutan 4 skill pertama = skill yang dipasang: [inti, inti kedua, dasar, defense]. 4 sisanya = pool cadangan.
        public static List<UnitData> CreateRoster()
        {
            var list = new List<UnitData>();

            list.Add(Make("odysseus", "Odysseus", Role.BufferDebuffer, 100, 22, 12, 105, "#C9A24A",
                Skill.Fx("Rally the Crew", "Seluruh kru: ATK +25%.", TargetType.AllAllies, StatusType.AtkUp, 3, 0.25f),
                Skill.Fx("Mark the Weakness", "1 musuh: DEF -25%.", TargetType.SingleEnemy, StatusType.DefDown, 3, 0.25f),
                Skill.Dmg("Spear Thrust", "Serang 1 musuh.", TargetType.SingleEnemy, 1.0f),
                Skill.Fx("Raise Shield", "Diri sendiri: damage diterima -50% sampai gilirannya lagi.", TargetType.Self, StatusType.Guard, 1, 0.5f),
                Skill.Dmg("Wooden Horse Gambit", "Serang semua musuh (lemah).", TargetType.AllEnemies, 0.6f),
                Skill.Fx("Voice of Command", "1 kawan: ATK +40%.", TargetType.SingleAlly, StatusType.AtkUp, 3, 0.4f),
                Skill.DmgFx("Feint", "Serang 1 musuh dan ATK-nya -25%.", TargetType.SingleEnemy, 0.7f, StatusType.AtkDown, 2, 0.25f),
                Skill.Heal("Second Wind", "Pulihkan HP diri sendiri.", TargetType.Self, 0.8f)));

            list.Add(Make("polites", "Polites", Role.Tank, 150, 16, 22, 90, "#5C7A9E",
                Skill.Dmg("Shield Bash", "Serang 1 musuh.", TargetType.SingleEnemy, 1.0f),
                Skill.DmgFx("Taunting Roar", "Serang 1 musuh dan ATK-nya -25%.", TargetType.SingleEnemy, 0.6f, StatusType.AtkDown, 2, 0.25f),
                Skill.Dmg("Spear Jab", "Serang 1 musuh (ringan).", TargetType.SingleEnemy, 0.8f),
                Skill.Fx("Hold the Line", "Diri sendiri: damage diterima -60%.", TargetType.Self, StatusType.Guard, 1, 0.6f),
                Skill.Fx("Shield Wall", "Seluruh kru: damage diterima -30%.", TargetType.AllAllies, StatusType.Guard, 1, 0.3f),
                Skill.Fx("Brace", "Diri sendiri: DEF +40%.", TargetType.Self, StatusType.DefUp, 3, 0.4f),
                Skill.Dmg("Sweeping Bash", "Serang semua musuh (lemah).", TargetType.AllEnemies, 0.5f),
                Skill.Heal("Endure", "Pulihkan HP diri sendiri.", TargetType.Self, 1.5f)));

            list.Add(Make("eurylochus", "Eurylochus", Role.DPS, 90, 30, 8, 110, "#A5483A",
                Skill.Dmg("Reckless Slash", "Serang kuat 1 musuh.", TargetType.SingleEnemy, 1.3f),
                Skill.Dmg("Spear Volley", "Serang semua musuh.", TargetType.AllEnemies, 0.7f),
                Skill.Dmg("Sword Strike", "Serang 1 musuh.", TargetType.SingleEnemy, 1.0f),
                Skill.Fx("Duck and Cover", "Diri sendiri: damage diterima -50%.", TargetType.Self, StatusType.Guard, 1, 0.5f),
                Skill.DmgFx("Doubt the Plan", "Serang 1 musuh dan DEF-nya -20%.", TargetType.SingleEnemy, 0.8f, StatusType.DefDown, 3, 0.2f),
                Skill.Dmg("Desperate Lunge", "Serang sangat kuat 1 musuh.", TargetType.SingleEnemy, 1.6f),
                Skill.Fx("Bloodlust", "Diri sendiri: ATK +30%.", TargetType.Self, StatusType.AtkUp, 3, 0.3f),
                Skill.Heal("Bandage", "Pulihkan HP diri sendiri.", TargetType.Self, 0.8f)));

            list.Add(Make("elpenor", "Elpenor", Role.DoTApplier, 85, 24, 8, 100, "#7E8B52",
                Skill.DmgFx("Poison Arrow", "Serang 1 musuh + Luka 6% max HP per giliran.", TargetType.SingleEnemy, 0.6f, StatusType.Bleed, 3, 0.06f),
                Skill.DmgFx("Burning Brand", "Serang semua musuh + Luka 5%.", TargetType.AllEnemies, 0.4f, StatusType.Bleed, 3, 0.05f),
                Skill.Dmg("Dagger", "Serang 1 musuh.", TargetType.SingleEnemy, 1.0f),
                Skill.Fx("Slip Away", "Diri sendiri: damage diterima -50%.", TargetType.Self, StatusType.Guard, 1, 0.5f),
                Skill.DmgFx("Rotten Wine", "Serang 1 musuh + Luka 8% (4 giliran).", TargetType.SingleEnemy, 0.5f, StatusType.Bleed, 4, 0.08f),
                Skill.DmgFx("Cut Deep", "Serang 1 musuh + Luka 10% (2 giliran).", TargetType.SingleEnemy, 0.8f, StatusType.Bleed, 2, 0.1f),
                Skill.Fx("Weaken", "1 musuh: ATK -30%.", TargetType.SingleEnemy, StatusType.AtkDown, 3, 0.3f),
                Skill.Heal("Drink", "Pulihkan HP diri sendiri.", TargetType.Self, 0.9f)));

            list.Add(Make("perimedes", "Perimedes", Role.Healer, 95, 20, 10, 98, "#6FA8A0",
                Skill.Heal("Salve", "Pulihkan 1 kawan.", TargetType.SingleAlly, 1.8f),
                Skill.Heal("Mend the Crew", "Pulihkan seluruh kru.", TargetType.AllAllies, 1.0f),
                Skill.Dmg("Staff Strike", "Serang 1 musuh.", TargetType.SingleEnemy, 0.9f),
                Skill.Fx("Brace", "Diri sendiri: damage diterima -50%.", TargetType.Self, StatusType.Guard, 1, 0.5f),
                Skill.Fx("Fortify", "1 kawan: DEF +30%.", TargetType.SingleAlly, StatusType.DefUp, 3, 0.3f),
                Skill.Heal("Blessing of Rest", "Pulihkan besar 1 kawan.", TargetType.SingleAlly, 2.4f),
                Skill.Fx("Shaming Words", "1 musuh: ATK -25%.", TargetType.SingleEnemy, StatusType.AtkDown, 3, 0.25f),
                Skill.Dmg("Staff Sweep", "Serang semua musuh (lemah).", TargetType.AllEnemies, 0.5f)));

            list.Add(Make("eurybates", "Eurybates", Role.DPS, 105, 26, 14, 102, "#B88AB0",
                Skill.Dmg("Heavy Blow", "Serang kuat 1 musuh.", TargetType.SingleEnemy, 1.2f),
                Skill.Dmg("Whirling Strike", "Serang semua musuh.", TargetType.AllEnemies, 0.65f),
                Skill.Dmg("Sword Strike", "Serang 1 musuh.", TargetType.SingleEnemy, 1.0f),
                Skill.Fx("Parry", "Diri sendiri: damage diterima -60%.", TargetType.Self, StatusType.Guard, 1, 0.6f),
                Skill.DmgFx("Pierce Armor", "Serang 1 musuh dan DEF-nya -25%.", TargetType.SingleEnemy, 0.9f, StatusType.DefDown, 3, 0.25f),
                Skill.Fx("Battle Fury", "Diri sendiri: ATK +35%.", TargetType.Self, StatusType.AtkUp, 3, 0.35f),
                Skill.DmgFx("Pommel Strike", "Serang 1 musuh dan ATK-nya -20%.", TargetType.SingleEnemy, 0.7f, StatusType.AtkDown, 2, 0.2f),
                Skill.Heal("Catch Breath", "Pulihkan HP diri sendiri.", TargetType.Self, 1.0f)));

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
                    return Make(id, "Cicone Warrior", Role.DPS, 70, 18, 8, 95, "#8C6D4F",
                        Skill.Dmg("Slash", "Serang 1 lawan.", TargetType.SingleEnemy, 1.0f),
                        Skill.Dmg("Shield Bash", "Serang ringan 1 lawan.", TargetType.SingleEnemy, 0.8f));
                case "cicone_archer":
                    return Make(id, "Cicone Archer", Role.DoTApplier, 50, 20, 5, 110, "#9A7E5A",
                        Skill.Dmg("Arrow", "Serang 1 lawan.", TargetType.SingleEnemy, 1.0f),
                        Skill.DmgFx("Poison Arrow", "Serang + Luka 5%.", TargetType.SingleEnemy, 0.6f, StatusType.Bleed, 3, 0.05f));
                case "cicone_chief":
                    return Make(id, "Cicone Chieftain", Role.Tank, 120, 24, 14, 90, "#6B4E3A",
                        Skill.Dmg("Cleave", "Serang semua lawan (lemah).", TargetType.AllEnemies, 0.6f),
                        Skill.Dmg("Heavy Blow", "Serang kuat 1 lawan.", TargetType.SingleEnemy, 1.2f),
                        Skill.Fx("Battle Cry", "Seluruh pasukan: ATK +25%.", TargetType.AllAllies, StatusType.AtkUp, 3, 0.25f));
                case "sea_raider":
                    return Make(id, "Sea Raider", Role.DPS, 65, 19, 8, 100, "#4F6F8C",
                        Skill.Dmg("Boarding Axe", "Serang 1 lawan.", TargetType.SingleEnemy, 1.0f),
                        Skill.DmgFx("Grapple", "Serang ringan + DEF lawan -20%.", TargetType.SingleEnemy, 0.6f, StatusType.DefDown, 3, 0.2f));
                case "polyphemus":
                    return Make(id, "Polyphemus", Role.Tank, 300, 28, 12, 85, "#5A4636",
                        Skill.Dmg("Boulder Slam", "Serang kuat 1 lawan.", TargetType.SingleEnemy, 1.3f),
                        Skill.Dmg("Sweeping Arm", "Serang semua lawan.", TargetType.AllEnemies, 0.7f),
                        Skill.Fx("Roar", "Diri sendiri: ATK +30%.", TargetType.Self, StatusType.AtkUp, 3, 0.3f));
                case "shade":
                    return Make(id, "Restless Shade", Role.DoTApplier, 55, 20, 6, 100, "#8E95A8",
                        Skill.DmgFx("Chill Touch", "Serang + Luka 6%.", TargetType.SingleEnemy, 0.8f, StatusType.Bleed, 3, 0.06f),
                        Skill.Fx("Wail", "1 lawan: ATK -25%.", TargetType.SingleEnemy, StatusType.AtkDown, 3, 0.25f));
                case "wild_beast":
                    return Make(id, "Circe's Beast", Role.DPS, 80, 22, 8, 108, "#7A8C54",
                        Skill.Dmg("Bite", "Serang 1 lawan.", TargetType.SingleEnemy, 1.0f),
                        Skill.Dmg("Maul", "Serang kuat 1 lawan.", TargetType.SingleEnemy, 1.3f));
            }
            return null;
        }

        public static Encounter GetEncounter(BattleEnvironment env)
        {
            switch (env)
            {
                case BattleEnvironment.OpenSea:
                    return new Encounter { Name = "Perompak Laut", Environment = env, GoldReward = 15, EnemyIds = new[] { "sea_raider", "sea_raider", "sea_raider" } };
                case BattleEnvironment.CoastalRaid:
                    return new Encounter { Name = "Serangan Cicones", Environment = env, GoldReward = 15, EnemyIds = new[] { "cicone_warrior", "cicone_archer", "cicone_chief" } };
                case BattleEnvironment.CaveLair:
                    return new Encounter { Name = "Gua Polyphemus", Environment = env, GoldReward = 25, EnemyIds = new[] { "polyphemus" } };
                case BattleEnvironment.Underworld:
                    return new Encounter { Name = "Bayang-bayang Hades", Environment = env, GoldReward = 20, EnemyIds = new[] { "shade", "shade", "shade" } };
                default:
                    return new Encounter { Name = "Binatang Aeaea", Environment = env, GoldReward = 10, EnemyIds = new[] { "wild_beast", "wild_beast" } };
            }
        }
    }
}
