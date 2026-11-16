using UnityEngine;
using UnityEngine.UI;

namespace Oddyseus.UI
{
    // Layar 5: Achievements. Daftar scroll 16 baris: lencana hadiah, judul + deskripsi, bar progres, keadaan.
    // Keadaan: "NOT ACTIVE YET" (belum ada pelacak), berjalan (bar + n/tujuan), selesai (bar penuh + centang).
    public class AchievementsPage : Page
    {
        RectTransform content;
        Text counter;

        protected override void Build()
        {
            Ui.TitleBar(C, "Achievements", () => Host.Go(PageId.Hub));
            counter = Ui.Txt(C, "", 24, Theme.DarkBrown, TextAnchor.MiddleRight, 0.60f, 0.775f, 0.95f, 0.855f, true);

            Ui.VScroll(C, "Scroll", 0.05f, 0.05f, 0.95f, 0.76f, out content);
            content.gameObject.AddComponent<GridLayoutGroup>();
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rg = content.gameObject.AddComponent<ResponsiveGrid>();
            rg.Cols = 1; rg.Spacing = 8f; rg.Aspect = 0.062f; rg.Pad = 6f;
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(content);
            int done = 0;
            foreach (var a in AchievementCatalog.All)
            {
                if (a.Done) done++;
                Row(a);
            }
            counter.text = "Unlocked " + done + "/" + AchievementCatalog.All.Length;
        }

        static string RewardName(AchievementInfo a)
        {
            switch (a.Reward)
            {
                case RewardType.Character:
                    foreach (var l in LegendCatalog.All) if (l.Id == a.RewardId) return l.Name;
                    return a.RewardId;
                case RewardType.Costume:
                    foreach (var l in LegendCatalog.All) if (l.Id == a.RewardId) return l.Name + " costume";
                    return "Costume";
                default:
                    return a.RewardId;   // Emblem: nama dewa; Title: teks gelar
            }
        }

        void Row(AchievementInfo a)
        {
            bool done = a.Done;
            var row = Ui.Sliced(content, "Row" + a.Id, "frames/frame_row_wide", 0f, 0f, 1f, 1f, 0.4f);
            if (!a.Active && !done) row.color = new Color(1f, 1f, 1f, 0.8f);

            Ui.RewardBadge(row.transform, a.Reward, 0.02f, 0.14f, 0.16f, 0.86f);
            Ui.Txt(row.transform, a.Title, 21, Theme.DarkBrown, TextAnchor.LowerLeft, 0.18f, 0.50f, 0.62f, 0.92f, true);
            Ui.Txt(row.transform, a.Desc + "  <color=#" + ColorUtility.ToHtmlStringRGB(Theme.Muted) + ">Reward: " + RewardName(a) + "</color>",
                   16, Theme.DarkBrown, TextAnchor.UpperLeft, 0.18f, 0.08f, 0.62f, 0.50f);

            if (!a.Active && !done)
            {
                Ui.Txt(row.transform, "NOT ACTIVE YET", 17, Theme.Muted, TextAnchor.MiddleCenter, 0.64f, 0.15f, 0.93f, 0.85f, true);
                return;
            }
            float frac = done ? 1f : (a.Goal > 0 ? (float)a.Current / a.Goal : 0f);
            Ui.ProgressBar(row.transform, frac, 0.64f, 0.50f, 0.88f, 0.84f, 0.3f);
            Ui.Txt(row.transform, (done ? a.Goal : a.Current) + " / " + a.Goal, 15, Theme.DarkBrown, TextAnchor.UpperCenter, 0.64f, 0.12f, 0.88f, 0.50f);
            if (done) Ui.FitPic(row.transform, "Check", "icons/icon_ui_check", 0.90f, 0.14f, 0.97f, 0.86f);
        }
    }
}
