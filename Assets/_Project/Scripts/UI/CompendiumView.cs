using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Riset tiap Pack: ??? sebelum pernah ditawarkan, nama dan tema setelah terlihat, boss dan equipment setelah boss kalah.
    // Lima gulungan per floor (normal, hover, undiscovered, cleared) dengan lukisan Pack di jendelanya.
    public class CompendiumView : HubScreen
    {
        const float ScrollAspect = 434f / 779f;   // sprite gulungan 434x779; jendela seni dari scrolls/scroll_layout.json
        int floor = 1;

        protected override string Title { get { return "PACK COMPENDIUM"; } }

        protected override void Populate(RectTransform body)
        {
            ParchUI.Txt(body, "Packs, bosses, equipment", 18, Parch.DarkBrown, TextAnchor.MiddleLeft, 0.045f, 0.80f, 0.45f, 0.865f);
            ParchUI.Txt(body, "Discovered " + MetaProgress.PackSeenCount + "/" + PackCatalog.All.Length +
                        "   |   Cleared " + MetaProgress.PackClearedCount + "/" + PackCatalog.All.Length,
                        20, Parch.DarkBrown, TextAnchor.MiddleRight, 0.55f, 0.80f, 0.955f, 0.865f);

            var names = new string[DungeonState.Floors];
            for (int f = 0; f < names.Length; f++) names[f] = "FLOOR " + (f + 1);
            var tabs = new TabBar(body, names, 0.045f, 0.725f, 0.60f, 0.8f, i => { floor = i + 1; Refresh(); });
            tabs.Select(floor - 1, false);

            var row = UIFactory.NewRect(body, "Row");
            UIFactory.Stretch(row, 0f, 0.085f, 1f, 0.715f);
            var packs = PackCatalog.ForFloor(floor);
            const float w = 0.187f, gap = 0.008f;
            float x0 = (1f - (packs.Count * w + (packs.Count - 1) * gap)) * 0.5f;
            for (int i = 0; i < packs.Count; i++)
            {
                float a = x0 + i * (w + gap);
                Card(row, packs[i], a, a + w);
            }
        }

        void Card(RectTransform row, PackInfo p, float x0, float x1)
        {
            bool seen = MetaProgress.IsPackSeen(p.Id);
            bool cleared = MetaProgress.IsPackCleared(p.Id);
            string state = !seen ? "undiscovered" : (cleared ? "cleared" : "normal");
            var frame = ParchUI.FitRect(row, "Pack" + p.Id, x0, 0f, x1, 1f, ScrollAspect);

            // 1) bidang seni di belakang gulungan (jendela sprite gulungan transparan)
            float wx0 = cleared ? 96f / 433f : 99f / 434f, wx1 = cleared ? 336f / 433f : 334f / 434f;
            var win = UIFactory.NewRect(frame, "Window");
            UIFactory.Stretch(win, wx0, 1f - 488f / 779f, wx1, 1f - 112f / 779f);
            win.gameObject.AddComponent<RectMask2D>();
            if (seen)
            {
                var art = UIFactory.NewRect(win, "Art");
                UIFactory.Stretch(art, 0f, 0f, 1f, 1f);
                var fit = art.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = 320f / 857f;
                var ai = art.gameObject.AddComponent<Image>();
                ai.sprite = UISprites.Get("packs/pack_" + p.Id.ToString("00") + "_" + Slugs[p.Id - 1]);
                ai.raycastTarget = false;
            }
            else
            {
                var fill = win.gameObject.AddComponent<Image>();
                fill.color = new Color(Parch.DarkBrown.r, Parch.DarkBrown.g, Parch.DarkBrown.b, 0.92f);
                fill.raycastTarget = false;
            }

            // 2) gulungan; hover hanya untuk keadaan normal, ditekan = sedikit mengecil
            var rt = UIFactory.NewRect(frame, "Scroll");
            UIFactory.Stretch(rt, 0f, 0f, 1f, 1f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISprites.Get("scrolls/scroll_" + state);
            img.raycastTarget = true;
            var sc = rt.gameObject.AddComponent<ScrollCard>();
            sc.Img = img; sc.Normal = img.sprite; sc.Scaled = frame;
            sc.Hover = state == "normal" ? UISprites.Get("scrolls/scroll_hover") : null;

            // 3) teks: nama di ribbon, tipe, Boss, Equipment (semuanya teks asli)
            ParchUI.Ribbon(frame, seen ? (cleared ? "gold" : "terracotta") : "bluegrey", seen ? p.Name : "???", 17, 0.03f, 0.275f, 0.97f, 0.465f);
            string god = seen && p.GodRealmChance > 0f ? "God Realm " + Mathf.RoundToInt(p.GodRealmChance * 100f) + "%" : "";
            ParchUI.Txt(frame, seen ? NodeInfo.EnvName(p.Env) + " / " + p.Theme : "Undiscovered", 14, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.20f, 0.225f, 0.80f, 0.285f);
            ParchUI.Txt(frame, "BOSS: " + (cleared ? p.Boss : "???"), 14, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.20f, 0.165f, 0.80f, 0.225f, true);
            var eq = EquipmentCatalog.ForPack(p.Id);
            ParchUI.Txt(frame, "EQUIPMENT: " + (cleared && eq != null ? eq.Name : "???"), 13, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.20f, 0.105f, 0.80f, 0.165f, true);
            ParchUI.Txt(frame, cleared ? god : "Defeat the boss to reveal.", 12,
                        cleared ? Parch.Olive : Parch.Muted, TextAnchor.MiddleCenter, 0.20f, 0.05f, 0.80f, 0.105f);
        }

        // Nama file lukisan Pack: pack_<nn>_<slug> (urutan sama dengan manifest aset).
        static readonly string[] Slugs =
        {
            "ismaros", "lotus_shore", "cave_of_polyphemus", "triton_strait", "isle_of_aeolus",
            "laestrygonian_harbor", "garden_of_circe", "siren_rocks", "thrinacia", "planctae_rocks",
            "gates_of_nekyia", "tartarus_punishment", "persephones_threshold", "scyllas_lair", "charybdis_whirlpool",
            "ogygia", "wrath_of_poseidon", "athenas_mind_palace", "ithaca_shore", "hall_of_ithaca",
        };
    }
}