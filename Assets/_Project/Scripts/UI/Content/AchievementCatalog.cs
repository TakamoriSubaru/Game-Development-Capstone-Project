using System;

namespace Oddyseus.UI
{
    public enum RewardType { Character, Costume, Emblem, Title }

    public class AchievementInfo
    {
        public int Id;
        public RewardType Reward;
        public string Title, Desc, RewardId;
        public int Goal;
        public Func<int> Progress;   // null = belum ada pelacak -> "NOT ACTIVE YET"

        public bool Active { get { return Progress != null; } }
        public int Current { get { return Progress != null ? Math.Min(Progress(), Goal) : 0; } }
        public bool Done { get { return MetaProgress.AchievementDone(Id) || (Active && Current >= Goal); } }
    }

    // PLACEHOLDER: GDD v0.7 belum mendefinisikan achievement. 16 baris ini hanya memenuhi tata letak layar
    // (4 jenis hadiah). Hanya dua yang punya pelacak nyata, dari MetaProgress; sisanya "NOT ACTIVE YET".
    public static class AchievementCatalog
    {
        static AchievementInfo A(int id, RewardType t, string title, string desc, int goal, string rewardId, Func<int> p = null)
        {
            return new AchievementInfo { Id = id, Reward = t, Title = title, Desc = desc, Goal = goal, RewardId = rewardId, Progress = p };
        }

        public static readonly AchievementInfo[] All =
        {
            A(1, RewardType.Character, "Shades of Nekyia", "Clear Floor 3.", 1, "ajax"),
            A(2, RewardType.Character, "Enchanted Garden", "Clear Circe's Garden.", 1, "circe"),
            A(3, RewardType.Character, "Faithful Nurse", "Reach Ithaca with the whole crew alive.", 1, "eurycleia"),
            A(4, RewardType.Costume, "First Blood", "Win 10 battles.", 10, "odysseus"),
            A(5, RewardType.Costume, "Shield Wall", "Win 10 battles with Polites fielded.", 10, "polites"),
            A(6, RewardType.Costume, "Skirmisher", "Win 10 battles with Eurylochus fielded.", 10, "eurylochus"),
            A(7, RewardType.Costume, "Cursed Fire", "Apply 50 damage-over-time effects.", 50, "elpenor"),
            A(8, RewardType.Costume, "Field Medic", "Heal 1000 HP in total.", 1000, "perimedes"),
            A(9, RewardType.Emblem, "Wine and Revelry", "Visit 3 God Realms.", 3, "dionysus"),
            A(10, RewardType.Emblem, "Love's Favour", "Spend 500 Gold on Blessings.", 500, "aphrodite"),
            A(11, RewardType.Emblem, "Hearth and Home", "Clear a Floor without losing any Men.", 1, "hestia"),
            A(12, RewardType.Emblem, "Retribution", "Defeat 5 bosses.", 5, "nemesis"),
            A(13, RewardType.Title, "Wanderer", "Discover 10 Packs.", 10, "Wanderer", () => MetaProgress.DiscoveredCount),
            A(14, RewardType.Title, "Sacker of Cities", "Clear Floor 1.", 1, "Sacker of Cities"),
            A(15, RewardType.Title, "Man of Many Ways", "Find 30 relics.", 30, "Man of Many Ways", () => MetaProgress.RelicFoundCount),
            A(16, RewardType.Title, "Homecoming", "Reach Ithaca.", 1, "Homecoming"),
        };

        public static AchievementInfo ForCharacter(string legendId)
        {
            foreach (var a in All) if (a.Reward == RewardType.Character && a.RewardId == legendId) return a;
            return null;
        }
    }
}
