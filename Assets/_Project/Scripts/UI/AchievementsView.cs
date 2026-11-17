using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Daftar achievement beserta hadiah (karakter, kostum, emblem, gelar) dan progress.
    // Keadaan baris: NOT ACTIVE YET (belum ada pelacak), berjalan (bar + n/target), selesai (bar penuh + centang).
    public class AchievementsView : HubScreen
    {
        protected override string Title { get { return "ACHIEVEMENTS"; } }
        protected override bool RedStrip { get { return true; } }

        protected override void Populate(RectTransform body)
        {
            var all = AchievementCatalog.All;
            ParchUI.Txt(body, "Rewards: extra characters, costumes, emblems and titles.", 18, Parch.DarkBrown, TextAnchor.MiddleLeft, 0.03f, 0.80f, 0.60f, 0.86f);
            ParchUI.Txt(body, "Unlocked " + AchievementCatalog.UnlockedCount + "/" + all.Count, 22, Parch.DarkBrown, TextAnchor.MiddleRight, 0.60f, 0.80f, 0.97f, 0.86f, true);

            RectTransform list;
            ParchUI.VScroll(body, "List", 0.025f, 0.10f, 0.975f, 0.795f, out list);
            list.gameObject.AddComponent<GridLayoutGroup>();
            var fit = list.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rg = list.gameObject.AddComponent<ResponsiveGrid>();
            rg.Cols = 1; rg.Spacing = 8f; rg.Aspect = 0.078f; rg.Pad = 4f;
            for (int i = 0; i < all.Count; i++) Row(list, all[i]);

            if (Debug.isDebugBuild)
            {
                ParchUI.Pill(body, "DbgClear", "DEBUG: CLEAR ALL PACKS", "alt", 0.07f, 0.02f, 0.30f, 0.085f, DebugClearPacks, 15, 0.4f);
                ParchUI.Pill(body, "DbgReset", "DEBUG: RESET PROGRESS", "alt", 0.31f, 0.02f, 0.54f, 0.085f, DebugReset, 15, 0.4f);
            }
        }

        static void Row(RectTransform list, Achievement a)
        {
            bool done = a.Unlocked;
            int cur = Mathf.Min(a.Current, a.Target);
            var row = ParchUI.Sliced(list, "Row", "frames/frame_row_wide", 0f, 0f, 1f, 1f, 0.4f);
            var t = row.transform;
            if (!a.Tracked && !done) row.color = new Color(1f, 1f, 1f, 0.85f);

            ParchUI.RewardBadge(t, a.Reward, 0.012f, 0.12f, 0.15f, 0.88f);
            ParchUI.Txt(t, a.Name, 24, Parch.DarkBrown, TextAnchor.LowerLeft, 0.165f, 0.50f, 0.54f, 0.95f, true);
            ParchUI.Txt(t, a.Desc, 17, Parch.DarkBrown, TextAnchor.UpperLeft, 0.165f, 0.05f, 0.54f, 0.50f);
            ParchUI.Solid(t, "Divider", new Color(Parch.DarkBrown.r, Parch.DarkBrown.g, Parch.DarkBrown.b, 0.5f), 0.553f, 0.18f, 0.5545f, 0.82f);

            ParchUI.Txt(t, a.RewardLabel, 17, Parch.DarkBrown, TextAnchor.MiddleLeft, 0.57f, 0.55f, 0.96f, 0.92f);
            if (!a.Tracked && !done)
            {
                ParchUI.Txt(t, "NOT ACTIVE YET", 16, Parch.Muted, TextAnchor.MiddleRight, 0.70f, 0.08f, 0.96f, 0.52f, true);
                return;
            }
            ParchUI.ProgressBar(t, done ? 1f : (float)cur / Mathf.Max(1, a.Target), 0.57f, 0.16f, 0.88f, 0.46f, 0.3f);
            if (done) ParchUI.FitPic(t, "Check", "icons/icon_ui_check", 0.90f, 0.12f, 0.975f, 0.88f);
            else ParchUI.Txt(t, cur + "/" + a.Target, 17, Parch.DarkBrown, TextAnchor.MiddleRight, 0.885f, 0.12f, 0.975f, 0.50f);
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
