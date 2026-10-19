using System.Collections.Generic;
using UnityEngine;

public enum CharacterRole { DPS, Healer, DoTApplier, BufferDebuffer, Tank }

/// <summary>One move. 'tag' is the small label in the Almanac (Role / Base / Defense).</summary>
public class SkillData
{
    public readonly string name, tag, description;

    public SkillData(string name, string tag, string description)
    {
        this.name = name;
        this.tag = tag;
        this.description = description;
    }
}

public class CharacterData
{
    public string name, title, bio;
    public CharacterRole role;
    public int stamina = 3;                 // GDD: 2-3 stamina bars per character
    public Color color = Color.gray;        // tints the placeholder lobby body until real art exists
    public bool lobbyCompanion;             // can this character show up and chat in the lobby?
    public string[] lobbyLines;
    public SkillData[] skills;
}

/// <summary>
/// ALL character info lives here, in one place. Both the lobby companion and the Almanac read from it.
/// To add a character: copy one block below, change the text, done.
/// NOTE: skills / bios / lines are PLACEHOLDERS for the designers to replace.
/// </summary>
public static class CharacterDatabase
{
    public static string RoleLabel(CharacterRole role)
    {
        switch (role)
        {
            case CharacterRole.DoTApplier: return "DoT Applier";
            case CharacterRole.BufferDebuffer: return "Buffer / Debuffer";
            default: return role.ToString();
        }
    }

    /// <summary>Picks a random character that is allowed to be the lobby companion.</summary>
    public static int RandomCompanionIndex()
    {
        var pool = new List<int>();
        for (int i = 0; i < All.Length; i++)
            if (All[i].lobbyCompanion) pool.Add(i);
        if (pool.Count == 0) return 0;
        return pool[Random.Range(0, pool.Count)];
    }

    public static readonly CharacterData[] All =
    {
        new CharacterData
        {
            name = "Odysseus", title = "King of Ithaca", role = CharacterRole.BufferDebuffer, stamina = 3,
            color = new Color(.80f, .65f, .30f), lobbyCompanion = false,
            bio = "Clever, stubborn and far from home. He wins with wits where blades fall short, and carries the whole voyage on his shoulders.",
            skills = new[]
            {
                new SkillData("Cunning Plan", "Role", "Raises the Speed of the whole party for 2 turns."),
                new SkillData("Nobody", "Role", "Marks one enemy. It takes extra damage and misses more often."),
                new SkillData("Bow of Ithaca", "Base", "A steady ranged shot at one enemy."),
                new SkillData("Lash to the Mast", "Defense", "Braces against the next attack, taking much less damage."),
            }
        },
        new CharacterData
        {
            name = "Eurylochus", title = "Second-in-Command", role = CharacterRole.Tank, stamina = 3,
            color = new Color(.45f, .50f, .62f), lobbyCompanion = true,
            bio = "Odysseus's kinsman and most cautious officer. He has learned to distrust every friendly harbour, and he is rarely wrong.",
            lobbyLines = new[]
            {
                "Odysseus, I have buried more men than I care to count. Forgive me if I look twice before stepping through any door.",
                "Every harbour we have trusted has bitten us. I would sooner row past the next one.",
                "You lead, I will hold the line. But do not ask me to stop worrying.",
                "Hunger makes men do foolish things. Keep the bread coming and I will keep the peace.",
            },
            skills = new[]
            {
                new SkillData("Hold the Line", "Role", "Taunts all enemies into attacking him for 2 turns."),
                new SkillData("Prudent Doubt", "Role", "Raises a barrier on himself that absorbs damage."),
                new SkillData("Spear Thrust", "Base", "A firm thrust at one enemy."),
                new SkillData("Back to the Ships", "Defense", "Takes a guard stance, halving damage until his next turn."),
            }
        },
        new CharacterData
        {
            name = "Polites", title = "Trusted Friend", role = CharacterRole.Healer, stamina = 2,
            color = new Color(.35f, .60f, .40f), lobbyCompanion = true,
            bio = "The warmest voice on the ship. Odysseus counts him his dearest comrade, and the crew counts on his steady hands.",
            lobbyLines = new[]
            {
                "There is always room by the fire for one more tired sailor. Sit, Odysseus.",
                "I patched up the lads again. Nobody thanks the one holding the bandages, so, you are welcome.",
                "If a song drifts over the water tonight, I will be the first to ask what it is singing. Tie me down if you must.",
                "Whatever waits at sea, we face it together. That is the only plan I trust.",
            },
            skills = new[]
            {
                new SkillData("Kind Word", "Role", "Heals one ally."),
                new SkillData("Shared Bread", "Role", "Heals the whole party a little."),
                new SkillData("Quick Jab", "Base", "A fast poke at one enemy."),
                new SkillData("Steady Hands", "Defense", "Guards himself and recovers a small amount of HP."),
            }
        },
        new CharacterData
        {
            name = "Elpenor", title = "Youngest of the Crew", role = CharacterRole.DoTApplier, stamina = 2,
            color = new Color(.70f, .35f, .35f), lobbyCompanion = true,
            bio = "Sleepy, unlucky and fond of wine, but surprisingly dangerous once there is something burning in his hand.",
            lobbyLines = new[]
            {
                "Is it morning? I was dreaming of a soft bed. And wine. Mostly wine.",
                "Keep me away from high places, Odysseus. And stairs. Especially stairs.",
                "Fire is my friend, truly. It is everything around the fire that gives me trouble.",
                "Wake me when the adventure starts. Actually, I am up. Mostly up.",
            },
            skills = new[]
            {
                new SkillData("Spilled Wine", "Role", "Poisons one enemy. It takes damage at the start of each of its turns for 3 turns."),
                new SkillData("Burning Brand", "Role", "Sets one enemy alight. Strong damage over time, but it ends sooner than poison."),
                new SkillData("Clumsy Swing", "Base", "A wild swing at one enemy."),
                new SkillData("Stumble Away", "Defense", "Slips out of the way of the next attack."),
            }
        },
        new CharacterData
        {
            name = "Perimedes", title = "Loyal Oarsman", role = CharacterRole.DPS, stamina = 3,
            color = new Color(.55f, .40f, .25f), lobbyCompanion = true,
            bio = "Broad-shouldered and blunt. He rowed through ten years of war and means to row the rest of the way home.",
            lobbyLines = new[]
            {
                "Oar is oiled, arms are ready. Point me at something.",
                "You think and plan, Odysseus. I will do the pulling and the hitting.",
                "Ten years of war and I am still not tired of rowing. Tell nobody.",
                "Ithaca is out there. Every stroke brings it a little closer.",
            },
            skills = new[]
            {
                new SkillData("Oar Smash", "Role", "A heavy blow to one enemy."),
                new SkillData("Rowing Stroke", "Role", "Sweeps across every enemy for light damage."),
                new SkillData("Quick Slash", "Base", "A fast cut at one enemy."),
                new SkillData("Oar Guard", "Defense", "Raises his oar and blocks part of the next hit."),
            }
        },
    };
}
