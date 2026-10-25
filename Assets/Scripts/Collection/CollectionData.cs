using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// ONLY file in Scripts/Collection that touches the AI-5 data classes (CharacterDatabase, SaveSystem,
/// EquipmentDatabase, BlessingDatabase, AchievementDatabase, PackDatabase, GameState.Run).
/// If AI-5 names or types differ from the contract, fix them here and nowhere else.
///
/// ASSUMED TYPES (not given by the contract, change here if different):
///   SaveData.unlockedCharacters, foundEquipment, unlockedCostumes  = List&lt;string&gt; (ids)
///   SaveData.equippedMoves      = Dictionary&lt;string, List&lt;string&gt;&gt; (charId -> move ids)
///   SaveData.achievementProgress = Dictionary&lt;string, int&gt; (achievement id -> progress)
///   RunState.party              = array of some element type (any name; it must expose a charId
///                                 field/property, or id / characterId / CharId)
///
/// PartyMember is a lightweight view model defined in this file. It does NOT depend on the real
/// element type of RunState.party, so a different AI-5 type name no longer breaks compilation (CS0246).
/// </summary>

/// <summary>Screen-side view of one party slot. `raw` keeps the original AI-5 object.</summary>
public struct PartyMember
{
    public string charId;
    public object raw;
}

public static class CollectionData
{
    public const int MaxEquipped = 4;

    // ---------- View models (so screens never name AI-5 classes) ----------
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

    // ---------- Small converters ----------
    static string S(object o) { return o == null ? "" : o.ToString(); }

    static int I(object o)
    {
        if (o == null) return 0;
        try { return Convert.ToInt32(o); }
        catch (Exception) { return 0; }
    }

    static bool Has(IEnumerable<string> list, string id)
    {
        if (list == null || string.IsNullOrEmpty(id)) return false;
        foreach (string s in list) if (s == id) return true;
        return false;
    }

    public static void EnsureLoaded()
    {
        if (SaveSystem.Data == null) SaveSystem.Load();
    }

    // ---------- Characters ----------
    public static List<CharacterData> Characters()
    {
        List<CharacterData> l = new List<CharacterData>();
        foreach (CharacterData c in CharacterDatabase.All) l.Add(c);
        return l;
    }

    public static CharacterData GetCharacter(string id)
    {
        return CharacterDatabase.Get(id);
    }

    public static bool IsUnlocked(CharacterData c)
    {
        if (c == null) return false;
        EnsureLoaded();
        return c.unlockedByDefault || Has(SaveSystem.Data.unlockedCharacters, c.id);
    }

    public static int UnlockedCount()
    {
        int n = 0;
        foreach (CharacterData c in CharacterDatabase.All) if (IsUnlocked(c)) n++;
        return n;
    }

    // Role key: BUFFER, TANK, DPS, DOT, HEALER
    public static string RoleKey(CharacterData c)
    {
        string r = c == null ? "" : S(c.role).ToLowerInvariant();
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

    public static int CostumesOwned(CharacterData c)
    {
        if (c == null || c.costumes == null) return 0;
        EnsureLoaded();
        int n = 0;
        foreach (CostumeData k in c.costumes) if (Has(SaveSystem.Data.unlockedCostumes, k.id)) n++;
        return n;
    }

    public static int CostumesTotal(CharacterData c)
    {
        return (c == null || c.costumes == null) ? 0 : c.costumes.Length;
    }

    // ---------- Equipped moves ----------
    public static List<string> GetEquippedMoves(CharacterData c)
    {
        List<string> result = new List<string>();
        if (c == null || c.moves == null) return result;
        EnsureLoaded();

        List<string> saved = null;
        var map = SaveSystem.Data.equippedMoves;
        if (map != null) map.TryGetValue(c.id, out saved);

        if (saved != null && saved.Count > 0)
        {
            foreach (string id in saved)
            {
                bool valid = false;
                foreach (SkillData m in c.moves) if (m.id == id) valid = true;
                if (valid && !result.Contains(id) && result.Count < MaxEquipped) result.Add(id);
            }
            if (result.Count > 0) return result;
        }

        foreach (SkillData m in c.moves)
            if (m.equippedByDefault && result.Count < MaxEquipped) result.Add(m.id);
        if (result.Count == 0 && c.moves.Length > 0) result.Add(c.moves[0].id);
        return result;
    }

    public static void SaveEquippedMoves(CharacterData c, List<string> ids)
    {
        if (c == null) return;
        EnsureLoaded();
        if (SaveSystem.Data.equippedMoves == null)
            SaveSystem.Data.equippedMoves = new Dictionary<string, List<string>>();
        SaveSystem.Data.equippedMoves[c.id] = new List<string>(ids);
        SaveSystem.Save();
    }

    // ---------- Run / party ----------
    public static bool InRun { get { return GameState.Run != null; } }

    public static int RunFloor { get { return GameState.Run != null ? GameState.Run.floor : 0; } }

    // Reads the character id from whatever element type RunState.party uses.
    // Tries charId, CharId, characterId, id (field or property) so the real type name never matters.
    static readonly string[] IdNames = { "charId", "CharId", "characterId", "CharacterId", "id", "Id" };

    static string IdOf(object o)
    {
        if (o == null) return "";
        Type t = o.GetType();
        const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (string n in IdNames)
        {
            FieldInfo f = t.GetField(n, F);
            if (f != null) return S(f.GetValue(o));
            PropertyInfo p = t.GetProperty(n, F);
            if (p != null && p.CanRead) return S(p.GetValue(o, null));
        }
        return "";
    }

    static PartyMember Wrap(object o)
    {
        PartyMember m = new PartyMember();
        m.charId = IdOf(o);
        m.raw = o;
        return m;
    }

    public static List<PartyMember> Party()
    {
        List<PartyMember> l = new List<PartyMember>();
        if (GameState.Run == null || GameState.Run.party == null) return l;
        foreach (var p in GameState.Run.party) l.Add(Wrap(p));
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
        if (GameState.Run == null || GameState.Run.party == null) return;
        // `var` + LINQ: no need to name the element type. Assumes party is an array (T[]).
        GameState.Run.party = GameState.Run.party.Where(p => IdOf(p) != charId).ToArray();
    }

    // ---------- Equipment ----------
    public static List<EquipInfo> Equipment()
    {
        EnsureLoaded();
        List<EquipInfo> l = new List<EquipInfo>();
        foreach (var e in EquipmentDatabase.All)
        {
            EquipInfo i = new EquipInfo();
            i.id = S(e.id); i.name = S(e.name); i.effect = S(e.effectText); i.source = S(e.sourceText);
            i.found = Has(SaveSystem.Data.foundEquipment, i.id);
            l.Add(i);
        }
        return l;
    }

    public static string EquipmentName(string id)
    {
        foreach (var e in EquipmentDatabase.All) if (S(e.id) == id) return S(e.name);
        return "";
    }

    // ---------- Blessings ----------
    public static List<BlessingInfo> Blessings()
    {
        List<BlessingInfo> l = new List<BlessingInfo>();
        foreach (var b in BlessingDatabase.All)
        {
            BlessingInfo i = new BlessingInfo();
            i.id = S(b.id); i.god = S(b.god); i.target = S(b.targetText); i.effect = S(b.effectText); i.price = I(b.price);
            l.Add(i);
        }
        return l;
    }

    // ---------- Packs ----------
    public static List<PackInfo> Packs()
    {
        EnsureLoaded();
        List<PackInfo> l = new List<PackInfo>();
        foreach (var p in PackDatabase.All)
        {
            PackInfo i = new PackInfo();
            i.id = S(p.id); i.name = S(p.name); i.floor = I(p.floor); i.bossEquipId = S(p.bossEquipmentId);
            i.bossEquipName = EquipmentName(i.bossEquipId);
            i.bossFound = Has(SaveSystem.Data.foundEquipment, i.bossEquipId);
            l.Add(i);
        }
        return l.OrderBy(x => x.floor).ToList();
    }

    // ---------- Achievements ----------
    public static List<AchInfo> Achievements()
    {
        EnsureLoaded();
        List<AchInfo> l = new List<AchInfo>();
        foreach (var a in AchievementDatabase.All)
        {
            AchInfo i = new AchInfo();
            i.id = S(a.id); i.title = S(a.title); i.description = S(a.description);
            i.rewardType = S(a.rewardType).ToLowerInvariant(); i.rewardId = S(a.rewardId);
            i.target = I(a.target);
            int p = 0;
            var map = SaveSystem.Data.achievementProgress;
            if (map != null) map.TryGetValue(i.id, out p);
            i.progress = p;
            i.rewardText = RewardText(i.rewardType, i.rewardId);
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

    static string RewardText(string type, string rewardId)
    {
        if (type.Contains("char"))
        {
            CharacterData c = CharacterDatabase.Get(rewardId);
            return "Character: " + (c != null ? c.name : rewardId);
        }
        if (type.Contains("costume"))
        {
            foreach (CharacterData c in CharacterDatabase.All)
            {
                if (c.costumes == null) continue;
                foreach (CostumeData k in c.costumes)
                    if (k.id == rewardId) return "Costume: " + c.name + " - " + k.name;
            }
            return "Costume: " + rewardId;
        }
        string t = string.IsNullOrEmpty(type) ? "Reward" : char.ToUpperInvariant(type[0]) + type.Substring(1);
        return t + ": " + rewardId;
    }

    // ---------- Debug tools (Achievements screen, Editor / development builds only) ----------
    // "Clear all packs" = every pack boss equipment becomes found.
    public static void DebugClearAllPacks()
    {
        EnsureLoaded();
        foreach (var p in PackDatabase.All)
        {
            string eq = S(p.bossEquipmentId);
            if (!string.IsNullOrEmpty(eq) && !Has(SaveSystem.Data.foundEquipment, eq))
                SaveSystem.Data.foundEquipment.Add(eq);
        }
        SaveSystem.Save();
    }

    public static void DebugResetProgress()
    {
        SaveSystem.Reset();
        SaveSystem.Save();
    }
}
