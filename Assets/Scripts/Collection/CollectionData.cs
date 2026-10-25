using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Core = Oddyseus.Core;

/// <summary>
/// ONLY file in Scripts/Collection that touches the game data classes. Everything the collection screens
/// need is read from here, so a rename elsewhere is fixed in this file and nowhere else.
///
/// Data sources (all real classes in Oddyseus.Core, no extra save system):
///   Characters   CharacterDatabase (Almanac) + AchievementCatalog.Bonus (locked until the achievement is done)
///                stats / moves / passives come from GameData.CreateRoster() and PassiveCatalog
///   Equipment    EquipmentCatalog; "found" = the boss of its Pack was cleared (MetaProgress.IsPackCleared)
///   Blessings    BlessingCatalog
///   Packs        PackCatalog
///   Achievements AchievementCatalog (progress + unlock state live in MetaProgress)
///   Run / party  RunState.Roster + DungeonState.Party (indices into the roster)
///   Equipped moves  in a run: UnitData.Equipped (shared with the Dungeon hub PartyView);
///                   outside a run: bitmask in MetaProgress under "equip.&lt;charId&gt;"
/// </summary>

/// <summary>Screen-side view of one party slot (a run roster member that is currently in the party).</summary>
public struct PartyMember
{
    public string charId;
    public int hp, maxHp;
    public bool alive;
}

public static class CollectionData
{
    public const int MaxEquipped = 4;

    // ---------- View models (so screens never name Oddyseus.Core classes) ----------
    public struct EquipInfo { public string id, name, effect, source; public bool found; }
    public struct BlessingInfo { public string id, god, target, effect; public int price; }
    public struct PackInfo { public string id, name, bossEquipId, bossEquipName; public int floor; public bool bossFound; }
    public struct AchInfo
    {
        public string id, title, description, rewardType, rewardId, rewardText;
        public int target, progress;
        public bool Active { get { return target > 0; } }
        public bool Done { get { return target > 0 && progress >= target; } }
    }

    // ---------- Setup ----------
    public static void EnsureLoaded()
    {
        // MetaProgress loads lazily on first use; it only needs to know where the file is.
        if (string.IsNullOrEmpty(Core.MetaProgress.SavePath))
            Core.MetaProgress.SavePath = Path.Combine(Application.persistentDataPath, "oddyseus_meta.txt");
    }

    // ---------- Characters ----------
    static List<CharacterData> all;
    static readonly Dictionary<CharacterData, string> bonusAchievement = new Dictionary<CharacterData, string>();

    static void BuildCharacters()
    {
        if (all != null) return;
        all = new List<CharacterData>();
        foreach (CharacterData c in CharacterDatabase.All) all.Add(c);

        // Bonus characters have no battle data yet; they only show up as locked / unlocked cards.
        foreach (Core.BonusCharacter b in Core.AchievementCatalog.Bonus)
        {
            CharacterData c = new CharacterData();
            c.name = b.Name;
            c.title = "Bonus Legend";
            c.bio = "";
            c.role = ToCharacterRole(b.Role);
            c.skills = new SkillData[0];
            all.Add(c);
            bonusAchievement[c] = b.AchievementId;
        }
    }

    static CharacterRole ToCharacterRole(Core.Role r)
    {
        switch (r)
        {
            case Core.Role.Healer: return CharacterRole.Healer;
            case Core.Role.DoTApplier: return CharacterRole.DoTApplier;
            case Core.Role.BufferDebuffer: return CharacterRole.BufferDebuffer;
            case Core.Role.Tank: return CharacterRole.Tank;
            default: return CharacterRole.DPS;
        }
    }

    public static List<CharacterData> Characters()
    {
        BuildCharacters();
        return new List<CharacterData>(all);
    }

    public static CharacterData GetCharacter(string id)
    {
        BuildCharacters();
        if (string.IsNullOrEmpty(id)) return null;
        foreach (CharacterData c in all) if (c.id == id) return c;
        return null;
    }

    public static bool IsUnlocked(CharacterData c)
    {
        if (c == null) return false;
        EnsureLoaded();
        BuildCharacters();
        string achId;
        if (!bonusAchievement.TryGetValue(c, out achId)) return true;   // base crew: always open
        Core.Achievement a = Core.AchievementCatalog.Find(achId);
        return a != null && (a.Unlocked || a.Current >= a.Target);
    }

    public static int UnlockedCount()
    {
        int n = 0;
        foreach (CharacterData c in Characters()) if (IsUnlocked(c)) n++;
        return n;
    }

    /// <summary>Text shown on a locked Legend card (the achievement that unlocks it).</summary>
    public static string UnlockTextOf(CharacterData c)
    {
        string achId;
        if (c == null || !bonusAchievement.TryGetValue(c, out achId)) return "";
        Core.Achievement a = Core.AchievementCatalog.Find(achId);
        return a == null ? "" : a.Desc;
    }

    // ---------- Battle data (stats, moves, passives) ----------
    static Dictionary<string, Core.UnitData> units;

    static Core.UnitData UnitOf(CharacterData c)
    {
        if (c == null) return null;
        if (units == null)
        {
            units = new Dictionary<string, Core.UnitData>();
            foreach (Core.UnitData d in Core.GameData.CreateRoster()) units[d.Id] = d;
        }
        Core.UnitData u;
        return units.TryGetValue(c.id, out u) ? u : null;
    }

    /// <summary>which: 0 = HP, 1 = ATK, 2 = DEF, 3 = SPD (average of the speed range).</summary>
    public static int StatOf(CharacterData c, int which)
    {
        Core.UnitData u = UnitOf(c);
        if (u == null) return 0;
        switch (which)
        {
            case 0: return u.MaxHp;
            case 1: return u.Atk;
            case 2: return u.Def;
            default: return u.Speed;
        }
    }

    static readonly Dictionary<CharacterData, SkillData[]> moveCache = new Dictionary<CharacterData, SkillData[]>();

    /// <summary>The move pool of a character (8 for the base crew), built from the real battle skills.</summary>
    public static SkillData[] MovesOf(CharacterData c)
    {
        if (c == null) return new SkillData[0];
        SkillData[] cached;
        if (moveCache.TryGetValue(c, out cached)) return cached;

        Core.UnitData u = UnitOf(c);
        SkillData[] result;
        if (u != null && u.Skills != null && u.Skills.Count > 0)
        {
            result = new SkillData[u.Skills.Count];
            for (int i = 0; i < result.Length; i++)
            {
                Core.Skill s = u.Skills[i];
                result[i] = new SkillData(s.Name, TagOf(s), s.Description, TargetText(s.Target));
            }
        }
        else result = c.skills ?? new SkillData[0];
        moveCache[c] = result;
        return result;
    }

    static string TagOf(Core.Skill s)
    {
        if (s.Kind == Core.SkillKind.Damage) return "Base";
        if (s.HasStatus && s.Status == Core.StatusType.Guard) return "Defense";
        return "Role";
    }

    static string TargetText(Core.TargetType t)
    {
        switch (t)
        {
            case Core.TargetType.SingleEnemy: return "1 enemy";
            case Core.TargetType.AllEnemies: return "All enemies";
            case Core.TargetType.SingleAlly: return "1 ally";
            case Core.TargetType.AllAllies: return "All allies";
            default: return "Self";
        }
    }

    // Trait = fixed passive, Talent = passive that opens at a Floor. Both come from PassiveCatalog.
    public static string PassiveName(CharacterData c, bool talent)
    {
        Core.PassiveInfo p = Passive(c, talent);
        return p == null ? "" : p.Name;
    }

    public static string PassiveText(CharacterData c, bool talent)
    {
        Core.PassiveInfo p = Passive(c, talent);
        return p == null ? "" : p.Desc;
    }

    static Core.PassiveInfo Passive(CharacterData c, bool talent)
    {
        if (c == null) return null;
        Core.PassiveInfo[] list = talent ? Core.PassiveCatalog.TalentsOf(c.id) : Core.PassiveCatalog.TraitsOf(c.id);
        return list.Length > 0 ? list[0] : null;
    }

    /// <summary>Floor at which the talent opens; 0 when the character has no talent (the screen hides the pill).</summary>
    public static int TalentFloorOf(CharacterData c)
    {
        if (Passive(c, true) == null) return 0;
        int[] floors = Core.PassiveCatalog.TalentFloors;
        return floors.Length > 0 ? floors[0] : 0;
    }

    // ---------- Roles ----------
    // Role key: BUFFER, TANK, DPS, DOT, HEALER. The battle roster (UnitData) wins when it exists, so the
    // collection matches the Dungeon hub; otherwise the Almanac role is used.
    public static string RoleKey(CharacterData c)
    {
        Core.UnitData u = UnitOf(c);
        string r = u != null ? u.Role.ToString() : (c == null ? "" : c.role.ToString());
        r = r.ToLowerInvariant();
        if (r.Contains("buff")) return "BUFFER";
        if (r.Contains("tank")) return "TANK";
        if (r.Contains("heal")) return "HEALER";
        if (r.Contains("dot")) return "DOT";
        return "DPS";
    }

    public static string RoleLabel(string key)
    {
        switch (key)
        {
            case "BUFFER": return "Buffer/Debuffer";
            case "TANK": return "Tank";
            case "HEALER": return "Healer";
            case "DOT": return "DoT";
            default: return "DPS";
        }
    }

    public static Color RoleColor(string key)
    {
        switch (key)
        {
            case "BUFFER": return UITheme.Gold;
            case "TANK": return UITheme.Hex("6FA8DC");
            case "HEALER": return UITheme.Hex("4CC3A5");
            case "DOT": return UITheme.Hex("A3C14E");
            default: return UITheme.Hex("E0605A");
        }
    }

    // ---------- Costumes (AchievementCatalog rewards of kind Costume) ----------
    public static int CostumesOwned(CharacterData c)
    {
        if (c == null) return 0;
        EnsureLoaded();
        int n = 0;
        foreach (Core.Achievement a in Core.AchievementCatalog.CostumesFor(c.name))
            if (a.Unlocked || a.Current >= a.Target) n++;
        return n;
    }

    public static int CostumesTotal(CharacterData c)
    {
        return c == null ? 0 : Core.AchievementCatalog.CostumesFor(c.name).Count;
    }

    // ---------- Run / party ----------
    public static bool InRun { get { return Core.RunState.Active && Core.RunState.Roster.Count > 0; } }

    public static int RunFloor { get { return InRun ? Core.DungeonState.Floor : 0; } }

    public static List<PartyMember> Party()
    {
        List<PartyMember> l = new List<PartyMember>();
        if (!InRun) return l;
        foreach (int idx in Core.DungeonState.Party)
        {
            if (idx < 0 || idx >= Core.RunState.Roster.Count) continue;
            Core.CharacterState s = Core.RunState.Roster[idx];
            PartyMember m = new PartyMember();
            m.charId = s.Data.Id;
            m.hp = s.Hp;
            m.maxHp = s.Data.MaxHp;
            m.alive = s.Alive;
            l.Add(m);
        }
        return l;
    }

    public static bool TryGetPartyMember(string charId, out PartyMember member)
    {
        foreach (PartyMember p in Party())
            if (p.charId == charId) { member = p; return true; }
        member = default(PartyMember);
        return false;
    }

    public static bool InParty(string charId)
    {
        PartyMember m;
        return TryGetPartyMember(charId, out m);
    }

    public static int PartyCount { get { return Party().Count; } }

    public static void RemoveFromParty(string charId)
    {
        if (!InRun) return;
        for (int i = 0; i < Core.RunState.Roster.Count; i++)
        {
            if (Core.RunState.Roster[i].Data.Id != charId) continue;
            // TogglePartyMember removes when present (and would add when absent, so check first).
            if (Core.DungeonState.Party.Contains(i)) Core.DungeonState.TogglePartyMember(i);
            return;
        }
    }

    // ---------- Equipped moves ----------
    static string EquipKey(CharacterData c) { return "equip." + c.id; }

    static Core.UnitData LiveUnit(CharacterData c)
    {
        if (!InRun || c == null) return null;
        foreach (Core.CharacterState s in Core.RunState.Roster)
            if (s.Data.Id == c.id) return s.Data;
        return null;
    }

    public static List<string> GetEquippedMoves(CharacterData c)
    {
        List<string> result = new List<string>();
        SkillData[] moves = MovesOf(c);
        if (c == null || moves.Length == 0) return result;
        EnsureLoaded();

        // In a run the real equipped list is UnitData.Equipped (indices into the skill pool).
        Core.UnitData live = LiveUnit(c);
        if (live != null && live.Equipped != null)
        {
            foreach (int idx in live.Equipped)
                if (idx >= 0 && idx < moves.Length && !result.Contains(moves[idx].id) && result.Count < MaxEquipped)
                    result.Add(moves[idx].id);
            if (result.Count > 0) return result;
        }

        int mask = Core.MetaProgress.Get(EquipKey(c));
        if (mask > 0)
        {
            for (int i = 0; i < moves.Length && i < 30; i++)
                if ((mask & (1 << i)) != 0 && result.Count < MaxEquipped) result.Add(moves[i].id);
            if (result.Count > 0) return result;
        }

        for (int i = 0; i < moves.Length && result.Count < MaxEquipped; i++) result.Add(moves[i].id);
        return result;
    }

    public static void SaveEquippedMoves(CharacterData c, List<string> ids)
    {
        if (c == null || ids == null) return;
        SkillData[] moves = MovesOf(c);
        EnsureLoaded();

        List<int> indices = new List<int>();
        int mask = 0;
        for (int i = 0; i < moves.Length && i < 30; i++)
        {
            if (!ids.Contains(moves[i].id)) continue;
            indices.Add(i);
            mask |= 1 << i;
        }
        Core.MetaProgress.Set(EquipKey(c), mask);

        // Keep the live run in sync so the battle uses the same four moves.
        Core.UnitData live = LiveUnit(c);
        if (live != null && indices.Count > 0) live.Equipped = indices.ToArray();
    }

    // ---------- Equipment ----------
    public static List<EquipInfo> Equipment()
    {
        EnsureLoaded();
        List<EquipInfo> l = new List<EquipInfo>();
        foreach (Core.EquipmentInfo e in Core.EquipmentCatalog.All)
        {
            EquipInfo i = new EquipInfo();
            i.id = e.PackId.ToString();
            i.name = e.Name;
            i.effect = e.Effect;
            Core.PackInfo pack = Core.EquipmentCatalog.PackOf(e);
            i.source = pack == null ? "" : "Boss of " + pack.Name + " (Floor " + pack.Floor + ")";
            i.found = Core.MetaProgress.IsPackCleared(e.PackId);
            l.Add(i);
        }
        return l;
    }

    public static string EquipmentName(string id)
    {
        foreach (Core.EquipmentInfo e in Core.EquipmentCatalog.All)
            if (e.PackId.ToString() == id) return e.Name;
        return "";
    }

    // ---------- Blessings ----------
    public static List<BlessingInfo> Blessings()
    {
        List<BlessingInfo> l = new List<BlessingInfo>();
        foreach (Core.BlessingInfo b in Core.BlessingCatalog.All)
        {
            BlessingInfo i = new BlessingInfo();
            i.id = b.God.ToLowerInvariant();
            i.god = b.God;
            i.target = b.Target;
            i.effect = b.Effect;
            i.price = b.Gold;
            l.Add(i);
        }
        return l;
    }

    // ---------- Packs ----------
    public static List<PackInfo> Packs()
    {
        EnsureLoaded();
        List<PackInfo> l = new List<PackInfo>();
        foreach (Core.PackInfo p in Core.PackCatalog.All)
        {
            PackInfo i = new PackInfo();
            i.id = p.Id.ToString();
            i.name = p.Name;
            i.floor = p.Floor;
            Core.EquipmentInfo boss = Core.EquipmentCatalog.ForPack(p.Id);
            i.bossEquipId = boss == null ? "" : boss.PackId.ToString();
            i.bossEquipName = boss == null ? "" : boss.Name;
            i.bossFound = Core.MetaProgress.IsPackCleared(p.Id);
            l.Add(i);
        }
        return l.OrderBy(x => x.floor).ToList();
    }

    // ---------- Achievements ----------
    public static List<AchInfo> Achievements()
    {
        EnsureLoaded();
        List<AchInfo> l = new List<AchInfo>();
        foreach (Core.Achievement a in Core.AchievementCatalog.All)
        {
            AchInfo i = new AchInfo();
            i.id = a.Id;
            i.title = a.Name;
            i.description = a.Desc;
            i.rewardType = a.Reward.ToString().ToLowerInvariant();   // character, costume, emblem, title
            i.rewardId = a.RewardName;
            i.rewardText = a.RewardLabel;
            i.target = a.Target;
            i.progress = a.Unlocked ? Math.Max(a.Target, a.Current) : a.Current;
            l.Add(i);
        }
        return l;
    }

    public static int AchievementsUnlocked()
    {
        int n = 0;
        foreach (AchInfo a in Achievements()) if (a.Done) n++;
        return n;
    }

    // ---------- Debug tools (Achievements screen, Editor / development builds only) ----------
    // "Clear all packs" = every Pack boss counts as defeated (so every boss equipment becomes found).
    public static void DebugClearAllPacks()
    {
        EnsureLoaded();
        foreach (Core.PackInfo p in Core.PackCatalog.All) Core.MetaProgress.MarkPackCleared(p.Id);
    }

    public static void DebugResetProgress()
    {
        EnsureLoaded();
        Core.MetaProgress.ResetAll();
    }
}
