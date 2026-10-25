using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Daftar achievement beserta hadiah (karakter, kostum, emblem, gelar) dan progress.
    public class AchievementsView : HubScreen
    {
        const float RowH = 78f, Gap = 8f;

        protected override string Title { get { return "ACHIEVEMENTS"; } }

        protected override void Populate(RectTransform body)
        {
            var all = AchievementCatalog.All;
            UIFactory.Label(body, "Rewards: extra characters, costumes, emblems and titles.", 16, HubTheme.Muted,
                            TextAnchor.MiddleLeft, 0.03f, 0.90f, 0.60f, 0.98f);
            UIFactory.Label(body, "Unlocked " + AchievementCatalog.UnlockedCount + "/" + all.Count, 20, HubTheme.Gold,
                            TextAnchor.MiddleRight, 0.60f, 0.90f, 0.97f, 0.98f);

            RectTransform content;
            UIFactory.Scroll(body, "List", 0.03f, 0.10f, 0.97f, 0.88f, out content);
            content.sizeDelta = new Vector2(0f, Gap + all.Count * (RowH + Gap));
            for (int i = 0; i < all.Count; i++) DrawRow(content, i, all[i]);

            if (Debug.isDebugBuild)
            {
                HubUI.Btn(body, "DbgClear", "DEBUG: CLEAR ALL PACKS", null, HubTheme.Panel, HubTheme.Muted,
                          0.03f, 0.015f, 0.28f, 0.085f, DebugClearPacks, 14);
                HubUI.Btn(body, "DbgReset", "DEBUG: RESET PROGRESS", null, HubTheme.Panel, HubTheme.Muted,
                          0.29f, 0.015f, 0.54f, 0.085f, DebugReset, 14);
            }
        }

        void DrawRow(RectTransform content, int i, Achievement a)
        {
            bool done = a.Unlocked;
            int cur = Mathf.Min(a.Current, a.Target);

            var rt = UIFactory.NewRect(content, "Row" + i);
            UIFactory.TopAnchored(rt, 0f, 1f, Gap + i * (RowH + Gap), RowH);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UIFactory.RoundedSprite;
            img.type = Image.Type.Sliced;
            img.color = HubTheme.Panel;
            HubTheme.Frame(img, done ? HubTheme.Gold : HubTheme.Bronze);
            var t = rt.transform;

            var tag = UIFactory.Panel(t, "Tag", TagColor(a.Reward), 0.012f, 0.12f, 0.115f, 0.88f, false);
            UIFactory.Label(tag.transform, a.KindTag, 12, Color.white, TextAnchor.MiddleCenter, 0.05f, 0.1f, 0.95f, 0.9f);

            UIFactory.Label(t, a.Name, 22, done ? HubTheme.Gold : HubTheme.Marble, TextAnchor.MiddleLeft, 0.14f, 0.52f, 0.55f, 0.95f);
            UIFactory.Label(t, a.Desc, 15, HubTheme.Muted, TextAnchor.UpperLeft, 0.14f, 0.06f, 0.55f, 0.52f);

            UIFactory.Label(t, a.RewardLabel, 16, HubTheme.Marble, TextAnchor.MiddleLeft, 0.57f, 0.50f, 0.81f, 0.92f);
            UIFactory.Bar(t, done ? 1f : (float)cur / Mathf.Max(1, a.Target), done ? HubTheme.Gold : HubTheme.Teal,
                          0.57f, 0.16f, 0.81f, 0.36f);

            string status = done ? "UNLOCKED" : (a.Tracked ? cur + "/" + a.Target : "NOT ACTIVE\nYET");
            UIFactory.Label(t, status, 16, done ? Theme.Good : HubTheme.Muted, TextAnchor.MiddleCenter, 0.83f, 0.10f, 0.99f, 0.90f);
        }

        static Color TagColor(RewardKind k)
        {
            switch (k)
            {
                case RewardKind.Character: return HubTheme.Teal;
                case RewardKind.Costume: return HubTheme.Bronze;
            }
            return Theme.Hex("#6B7690");
        }

        // Hanya untuk pengujian (muncul di Editor dan Development Build).
        void DebugClearPacks()
        {
            for (int i = 0; i < PackCatalog.All.Length; i++) MetaProgress.MarkPackCleared(PackCatalog.All[i].Id);
            Host.Announce(AchievementCatalog.CheckUnlocks());
            Refresh();
        }

        void DebugReset()
        {
            MetaProgress.ResetAll();
            Refresh();
        }
    }
}