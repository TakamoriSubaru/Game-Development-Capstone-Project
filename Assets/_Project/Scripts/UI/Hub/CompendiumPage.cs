using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Layar 3: Pack Compendium. Tab Floor 1..4, lima gulungan per floor, penghitung Discovered / Cleared.
    // Nama Pack, tipe (mis. "Coastal Raid / Bleed") dan Boss dibaca dari PackCatalog, sumber data yang sama dengan hub.
    public class CompendiumPage : Page
    {
        // Jendela seni di tiap sprite gulungan (piksel dari scrolls/scroll_layout.json): x0, y0 (dari atas), x1, y1; ukuran gambar.
        // normal 99,112,334,488 / 434x779 ; hover 99,112,335,488 / 434x779 ; undiscovered 99,112,334,488 / 433x779 ; cleared 96,112,336,488 / 433x779
        const float ScrollAspect = 434f / 779f;

        TabBar tabs;
        RectTransform row;
        Text counters;
        int floor = 1;

        protected override void Build()
        {
            Ui.TitleBar(C, "Pack Compendium", () => Host.Go(PageId.Hub));
            tabs = new TabBar(C, new[] { "Floor 1", "Floor 2", "Floor 3", "Floor 4" }, 0.05f, 0.775f, 0.58f, 0.855f, i => { floor = i + 1; Fill(); });

            row = UIFactory.NewRect(C, "Row");
            UIFactory.Stretch(row, 0f, 0.125f, 1f, 0.76f);

            Ui.Sliced(C, "CounterPlaque", "frames/frame_row_wide", 0.27f, 0.025f, 0.73f, 0.105f, 0.45f);
            counters = Ui.Txt(C, "", 22, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.30f, 0.03f, 0.70f, 0.10f, true);
        }

        public override void Refresh()
        {
            tabs.Select(floor - 1, false);
            Fill();
        }

        void Fill()
        {
            counters.text = "Discovered " + MetaProgress.DiscoveredCount + "/" + PackCatalog.All.Length
                          + "  |  Cleared " + MetaProgress.ClearedCount + "/" + PackCatalog.All.Length;
            UIFactory.ClearChildren(row);
            var packs = PackCatalog.ForFloor(floor);
            const float w = 0.187f, gap = 0.008f;
            float total = packs.Count * w + (packs.Count - 1) * gap;
            float x0 = (1f - total) * 0.5f;
            for (int i = 0; i < packs.Count; i++)
            {
                float a = x0 + i * (w + gap);
                Card(packs[i], a, a + w);
            }
        }

        static string EnvLabel(BattleEnvironment e)
        {
            switch (e)
            {
                case BattleEnvironment.OpenSea: return "Open Sea";
                case BattleEnvironment.CoastalRaid: return "Coastal Raid";
                case BattleEnvironment.CaveLair: return "Cave Lair";
                case BattleEnvironment.Underworld: return "Underworld";
                case BattleEnvironment.OpenWilds: return "Open Wilds";
            }
            return e.ToString();
        }

        void Card(PackInfo p, float x0, float x1)
        {
            bool found = MetaProgress.PackDiscovered(p.Id);
            bool cleared = MetaProgress.PackCleared(p.Id);
            string state = !found ? "undiscovered" : (cleared ? "cleared" : "normal");

            var frame = Ui.FitRect(row, "Pack" + p.Id, x0, 0f, x1, 1f, ScrollAspect);

            // 1) bidang seni di belakang gulungan (jendela gulungan transparan)
            float wx0 = cleared ? 96f / 433f : 99f / 434f, wx1 = cleared ? 336f / 433f : 334f / 434f;
            var win = UIFactory.NewRect(frame, "Window");
            UIFactory.Stretch(win, wx0, 1f - 488f / 779f, wx1, 1f - 112f / 779f);
            win.gameObject.AddComponent<RectMask2D>();
            if (found)
            {
                var art = UIFactory.NewRect(win, "Art");
                UIFactory.Stretch(art, 0f, 0f, 1f, 1f);
                var fit = art.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = 320f / 857f;
                var ai = art.gameObject.AddComponent<Image>();
                ai.sprite = UISprites.Get("packs/pack_" + p.Id.ToString("00") + "_" + Slug(p.Id));
                ai.raycastTarget = false;
            }
            else
            {
                var fill = win.gameObject.AddComponent<Image>();
                fill.color = new Color(Theme.DarkBrown.r, Theme.DarkBrown.g, Theme.DarkBrown.b, 0.92f);
                fill.raycastTarget = false;
            }

            // 2) gulungan (state normal / undiscovered / cleared; hover hanya untuk yang normal)
            var rt = UIFactory.NewRect(frame, "Scroll");
            UIFactory.Stretch(rt, 0f, 0f, 1f, 1f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISprites.Get("scrolls/scroll_" + state);
            img.raycastTarget = true;
            var sc = rt.gameObject.AddComponent<ScrollCard>();
            sc.Img = img;
            sc.Normal = img.sprite;
            sc.Scaled = frame;
            sc.Hover = state == "normal" ? UISprites.Get("scrolls/scroll_hover") : null;

            // 3) teks di atas gulungan: nama di ribbon, tipe, Boss, Equipment
            var rib = Ui.Ribbon(frame, found ? "gold" : "bluegrey", found ? p.Name : "???", 17, 0.03f, 0.285f, 0.97f, 0.47f);
            string theme = string.IsNullOrEmpty(p.Theme) ? "" : " / " + p.Theme;
            Ui.Txt(frame, found ? EnvLabel(p.Env) + theme : "Undiscovered", 14, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.20f, 0.225f, 0.80f, 0.285f);
            Ui.Txt(frame, "BOSS: " + (cleared ? p.Boss : "???"), 14, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.20f, 0.165f, 0.80f, 0.225f, true);
            // Data equipment per Pack belum ada di game. "???" sampai Pack tuntas; setelahnya "-" (TODO: isi dari data final).
            Ui.Txt(frame, "EQUIPMENT: " + (cleared ? "-" : "???"), 14, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.20f, 0.105f, 0.80f, 0.165f, true);
            if (!cleared)
                Ui.Txt(frame, "Defeat the boss to reveal.", 12, Theme.Muted, TextAnchor.MiddleCenter, 0.20f, 0.05f, 0.80f, 0.105f);
        }

        // Nama file lukisan Pack: pack_<nn>_<slug>. Urutan dan slug sama dengan manifest aset.
        static readonly string[] Slugs =
        {
            "ismaros", "lotus_shore", "cave_of_polyphemus", "triton_strait", "isle_of_aeolus",
            "laestrygonian_harbor", "garden_of_circe", "siren_rocks", "thrinacia", "planctae_rocks",
            "gates_of_nekyia", "tartarus_punishment", "persephones_threshold", "scyllas_lair", "charybdis_whirlpool",
            "ogygia", "wrath_of_poseidon", "athenas_mind_palace", "ithaca_shore", "hall_of_ithaca",
        };

        static string Slug(int id) { return Slugs[id - 1]; }
    }
}
