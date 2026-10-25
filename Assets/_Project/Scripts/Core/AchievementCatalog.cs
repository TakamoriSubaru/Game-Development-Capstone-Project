using System;
using System.Collections.Generic;

namespace Oddyseus.Core
{
    public enum RewardKind { Character, Costume, Emblem, Title }

    public class Achievement
    {
        public string Id;
        public string Name;
        public string Desc;
        public RewardKind Reward;
        public string RewardName;       // nama karakter, kostum, emblem, atau gelar
        public string ForCharacter;     // pemilik kostum (khusus Reward = Costume)
        public int Target = 1;
        public Func<int> Progress;
        public bool Tracked = true;     // false: sistem pendukungnya (battle, Synergy, ending) belum ada

        public int Current { get { return Progress == null ? 0 : Math.Max(0, Progress()); } }
        public bool Unlocked { get { return MetaProgress.IsUnlocked(Id); } }

        public string KindTag
        {
            get
            {
                switch (Reward)
                {
                    case RewardKind.Character: return "CHARACTER";
                    case RewardKind.Costume: return "COSTUME";
                    case RewardKind.Emblem: return "EMBLEM";
                }
                return "TITLE";
            }
        }

        public string RewardLabel
        {
            get
            {
                switch (Reward)
                {
                    case RewardKind.Character: return "Character: " + RewardName;
                    case RewardKind.Costume: return "Costume: " + ForCharacter + " - " + RewardName;
                    case RewardKind.Emblem: return "Hub emblem: " + RewardName;
                }
                return "Title: " + RewardName;
            }
        }
    }

    public class BonusCharacter
    {
        public string Name;
        public Role Role;
        public string AchievementId;
    }

    // Daftar achievement = DRAF untuk disetujui tim. Karakter bonus belum punya data battle (UnitData).
    public static class AchievementCatalog
    {
        public static readonly List<Achievement> All = Build();

        public static readonly BonusCharacter[] Bonus =
        {
            new BonusCharacter { Name = "Ajax the Great", Role = Role.Tank, AchievementId = "ajax" },
            new BonusCharacter { Name = "Circe", Role = Role.DoTApplier, AchievementId = "circe" },
            new BonusCharacter { Name = "Eurycleia", Role = Role.Healer, AchievementId = "eurycleia" },
        };

        static List<Achievement> Build()
        {
            var l = new List<Achievement>();

            // Karakter bonus
            l.Add(new Achievement
            {
                Id = "ajax", Name = "Ajax Remembered", Desc = "Defeat the Shade of Ajax at the Gate of Nekyia.",
                Reward = RewardKind.Character, RewardName = "Ajax the Great",
                Target = 1, Progress = () => MetaProgress.IsPackCleared(11) ? 1 : 0
            });
            l.Add(new Achievement
            {
                Id = "circe", Name = "Mistress of Aeaea", Desc = "Defeat Circe in Circe's Garden.",
                Reward = RewardKind.Character, RewardName = "Circe",
                Target = 1, Progress = () => MetaProgress.IsPackCleared(7) ? 1 : 0
            });
            l.Add(new Achievement
            {
                Id = "eurycleia", Name = "No Man Left Behind", Desc = "Reach Ithaca with the entire crew alive (Good Ending).",
                Reward = RewardKind.Character, RewardName = "Eurycleia",
                Target = 1, Progress = () => MetaProgress.Get("stat.goodEndings"), Tracked = false
            });

            // Kostum
            l.Add(new Achievement
            {
                Id = "beggar", Name = "Homecoming", Desc = "Complete a Dungeon run.",
                Reward = RewardKind.Costume, ForCharacter = "Odysseus", RewardName = "Beggar of Ithaca",
                Target = 1, Progress = () => MetaProgress.Get("stat.runsCompleted")
            });
            Syn(l, "brothers", "Brothers in Arms", "Polites", "Shieldmate");
            Syn(l, "doubt", "Doubt in the Ranks", "Eurylochus", "Mutineer's Cloak");
            Syn(l, "night", "Night Raiders", "Diomedes", "Night Raider");
            Syn(l, "dearest", "Dearest Companion", "Odysseus", "Wanderer");
            Syn(l, "herald", "Herald's Word", "Eurybates", "Herald's Regalia");
            Syn(l, "watch", "Watch Over the Young", "Elpenor", "Rooftop Reveler");
            Syn(l, "crew", "Crew of Ithaca", "Odysseus", "King of Ithaca");
            Syn(l, "belly", "Hidden in the Belly", "Epeius", "Horse Builder");
            Syn(l, "rams", "Under the Rams", "Perimedes", "Ram Rider");

            // Kosmetik
            l.Add(new Achievement
            {
                Id = "cartographer", Name = "Cartographer", Desc = "Discover all 20 Packs.",
                Reward = RewardKind.Emblem, RewardName = "Old Map",
                Target = 20, Progress = () => MetaProgress.PackSeenCount
            });
            l.Add(new Achievement
            {
                Id = "giantslayer", Name = "Giant Slayer", Desc = "Defeat the boss of 10 different Packs.",
                Reward = RewardKind.Title, RewardName = "Bane of Monsters",
                Target = 10, Progress = () => MetaProgress.PackClearedCount
            });
            l.Add(new Achievement
            {
                Id = "divine", Name = "Divine Favor", Desc = "Enter 5 God Realms.",
                Reward = RewardKind.Emblem, RewardName = "Golden Emblem",
                Target = 5, Progress = () => MetaProgress.Get("stat.godRealms")
            });
            return l;
        }

        // Kostum Synergy: menang 5 battle dengan Synergy aktif. Belum bisa dilacak (butuh battle dan Synergy).
        static void Syn(List<Achievement> l, string id, string synergy, string who, string costume)
        {
            string key = "syn." + id;
            l.Add(new Achievement
            {
                Id = "syn_" + id, Name = synergy, Desc = "Win 5 battles with the " + synergy + " synergy active.",
                Reward = RewardKind.Costume, ForCharacter = who, RewardName = costume,
                Target = 5, Progress = () => MetaProgress.Get(key), Tracked = false
            });
        }

        public static Achievement Find(string id)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].Id == id) return All[i];
            return null;
        }

        public static int UnlockedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < All.Count; i++) if (All[i].Unlocked) n++;
                return n;
            }
        }

        public static List<Achievement> CostumesFor(string character)
        {
            var list = new List<Achievement>();
            for (int i = 0; i < All.Count; i++)
                if (All[i].Reward == RewardKind.Costume &&
                    string.Equals(All[i].ForCharacter, character, StringComparison.OrdinalIgnoreCase))
                    list.Add(All[i]);
            return list;
        }

        // Tandai achievement yang baru tercapai dan kembalikan daftarnya.
        public static List<Achievement> CheckUnlocks()
        {
            var fresh = new List<Achievement>();
            for (int i = 0; i < All.Count; i++)
            {
                var a = All[i];
                if (!a.Unlocked && a.Current >= a.Target)
                {
                    MetaProgress.MarkUnlocked(a.Id);
                    fresh.Add(a);
                }
            }
            return fresh;
        }
    }
}