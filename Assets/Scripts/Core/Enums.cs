namespace Oddyseus.Core
{
    // Peran karakter (GDD bagian 5.1)
    public enum Role { DPS, Healer, DoTApplier, BufferDebuffer, Tank }

    public enum SkillKind { Damage, Heal, Status }

    public enum TargetType { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Self }

    public enum StatusType { AtkUp, AtkDown, DefUp, DefDown, Bleed, Guard }

    // Varian latar Battle Island (GDD bagian 4)
    public enum BattleEnvironment { OpenSea, CoastalRaid, CaveLair, Underworld, OpenWilds }

    public enum BattleOutcome { Ongoing, Victory, Defeat }

    public enum EventKind { Damage, Heal, StatusApplied, Bleed, Info }

    // Jenis node di peta Dungeon Mode (wireframe Map Select)
    public enum NodeType { Battle, Random, Event, Shop, Rest, Elite, Resource, Boss, GodRealm }
}
