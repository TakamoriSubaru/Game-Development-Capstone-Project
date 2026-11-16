using System;
using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Layar 1: Dungeon Mode hub. Kompas di tengah (judul = teks asli di cakram kosong), menu di kiri, panel run di kanan.
    public class HubPage : Page
    {
        RectTransform panel;
        Text floorText, packText;
        Image[] crewPics = new Image[4];
        Text[] crewNames = new Text[4];
        Text menVal, foodVal, goldVal;
        Button continueBtn;

        protected override void Build()
        {
            Ui.Pic(C, "KeyStrip", "ornaments/key_strip_brown", 0f, 0.972f, 1f, 1f, false);
            Ui.Pill(C, "BackToLobby", "Lobby", "parchment", 0.022f, 0.905f, 0.14f, 0.965f, Host.BackToLobby, 22, 0.5f);

            // --- kompas ---
            var comp = Ui.FitPic(C, "Compass", "hub/compass_medallion", 0.265f, 0.05f, 0.735f, 0.95f);
            Ui.Txt(comp.transform, "ODDYSEUS", 38, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.36f, 0.52f, 0.64f, 0.62f, true);
            Ui.Txt(comp.transform, "Journey's End", 18, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.36f, 0.455f, 0.64f, 0.52f);
            Ui.Pic(comp.transform, "Rule", "ornaments/divider_diamond", 0.37f, 0.43f, 0.63f, 0.455f, false);
            Ui.Txt(comp.transform, "DUNGEON MODE", 20, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.36f, 0.355f, 0.64f, 0.425f, true);

            // --- menu kiri ---
            string[] icons = { "icons/icon_menu_legends", "icons/icon_menu_packs", "icons/icon_menu_armory", "icons/icon_menu_achievements" };
            string[] titles = { "Legends", "Pack Compendium", "Armory", "Achievements" };
            string[] subs = { "Your roster", "Packs, bosses, equipment", "Equipment and artifacts", "Unlock characters and costumes" };
            PageId[] dest = { PageId.Legends, PageId.Compendium, PageId.Armory, PageId.Achievements };
            for (int i = 0; i < 4; i++)
            {
                PageId target = dest[i];
                float y1 = 0.80f - i * 0.15f;
                Ui.MenuButton(C, "Menu" + i, icons[i], titles[i], subs[i], 0.03f, y1 - 0.12f, 0.285f, y1, () => Host.Go(target));
            }

            // --- panel run (kanan) ---
            var p = Ui.Panel9(C, "RunPanel", 0.715f, 0.09f, 0.975f, 0.88f, 0.45f);
            panel = p.rectTransform;
            floorText = Ui.Txt(panel, "", 26, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.08f, 0.855f, 0.92f, 0.94f, true);
            packText = Ui.Txt(panel, "", 20, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.08f, 0.79f, 0.92f, 0.86f);
            Ui.Pic(panel, "Rule", "ornaments/divider_leaf_diamond", 0.10f, 0.765f, 0.90f, 0.79f, false);

            for (int i = 0; i < 4; i++)
            {
                float x0 = 0.07f + i * 0.2325f;
                crewPics[i] = Ui.FitPic(panel, "Crew" + i, "portraits/portrait_locked_round", x0, 0.60f, x0 + 0.215f, 0.75f);
                crewNames[i] = Ui.Txt(panel, "", 13, Theme.DarkBrown, TextAnchor.UpperCenter, x0 - 0.01f, 0.545f, x0 + 0.225f, 0.60f);
            }

            string[] ric = { "icons/icon_res_men", "icons/icon_res_food", "icons/icon_res_gold" };
            string[] rname = { "Men", "Food", "Gold" };
            Text[] vals = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                float x0 = 0.06f + i * 0.31f;
                Ui.FitPic(panel, "Res" + i, ric[i], x0, 0.42f, x0 + 0.13f, 0.51f);
                vals[i] = Ui.Txt(panel, "", 22, Theme.DarkBrown, TextAnchor.LowerLeft, x0 + 0.135f, 0.455f, x0 + 0.30f, 0.51f, true);
                Ui.Txt(panel, rname[i], 13, Theme.Muted, TextAnchor.UpperLeft, x0 + 0.135f, 0.415f, x0 + 0.30f, 0.46f);
            }
            menVal = vals[0]; foodVal = vals[1]; goldVal = vals[2];

            continueBtn = Ui.Pill(panel, "Continue", "CONTINUE", "parchment", Ui.GoldTint, 0.10f, 0.245f, 0.90f, 0.35f, OnContinue, 26, 0.6f);
            Ui.Pill(panel, "NewRun", "NEW RUN", "terracotta", 0.10f, 0.105f, 0.90f, 0.21f, OnNewRun, 26, 0.6f);

            // --- debug: hanya di debug build (Editor dan Development Build) ---
            if (Debug.isDebugBuild)
            {
                Ui.Txt(C, "DEBUG", 14, Theme.Muted, TextAnchor.LowerLeft, 0.03f, 0.135f, 0.2f, 0.17f, true);
                Ui.Pill(C, "DbgUnlock", "Unlock all", "alt", 0.03f, 0.065f, 0.155f, 0.13f, () => { MetaProgress.DebugUnlockAll(); Host.RefreshCurrent(); }, 16, 0.4f);
                Ui.Pill(C, "DbgReset", "Reset meta", "alt", 0.165f, 0.065f, 0.29f, 0.13f, () => { MetaProgress.DebugReset(); Host.RefreshCurrent(); }, 16, 0.4f);
            }
        }

        public override void Refresh()
        {
            bool run = DungeonState.Active;
            floorText.text = run ? "Floor " + DungeonState.Floor + " · Act " + DungeonState.Act : "No run in progress";
            packText.text = !run ? "Set sail from Ismaros" : (DungeonState.CurrentPack != null ? DungeonState.CurrentPack.Name : "Choose a Pack");

            // Kru: 4 anggota party run aktif; kalau belum ada run, pratinjau 4 Legends pertama yang terbuka (diredupkan).
            string[] ids = new string[4];
            int n = 0;
            if (run && DungeonState.Party.Count > 0 && RunState.Roster.Count > 0)
            {
                for (int i = 0; i < DungeonState.Party.Count && n < 4; i++)
                {
                    int idx = DungeonState.Party[i];
                    if (idx >= 0 && idx < RunState.Roster.Count) ids[n++] = RunState.Roster[idx].Data.Id;
                }
            }
            else
            {
                foreach (var l in LegendCatalog.All) { if (n < 4 && l.Unlocked && l.Data != null) ids[n++] = l.Id; }
            }
            for (int i = 0; i < 4; i++)
            {
                bool has = ids[i] != null;
                crewPics[i].sprite = UISprites.Get(has ? "portraits/portrait_" + ids[i] + "_round" : "portraits/portrait_locked_round");
                crewPics[i].color = has && run ? Color.white : new Color(1f, 1f, 1f, has ? 0.7f : 0.5f);
                crewNames[i].text = has ? NameOf(ids[i]) : "";
            }

            bool live = run && RunState.Active;
            menVal.text = (live ? RunState.Men : RunState.StartMen).ToString();
            foodVal.text = (live ? RunState.Food : RunState.StartFood).ToString();
            goldVal.text = (live ? RunState.Gold : RunState.StartGold).ToString();

            Ui.SetEnabled(continueBtn, run);   // tidak ada save = tidak bisa lanjut
        }

        static string NameOf(string id)
        {
            foreach (var l in LegendCatalog.All) if (l.Id == id) return l.Name;
            return id;
        }

        void OnContinue() { Host.OpenDungeon(); }

        void OnNewRun()
        {
            string body = DungeonState.Active
                ? "Your current run will be abandoned and a new crew will set sail."
                : "A new crew will set sail from the shores of Troy.";
            Host.ShowModal("Begin a new run?", body, new[] { "Cancel", "Begin" },
                new Action[] { null, () => { DungeonState.ResetDungeon(); Host.OpenDungeon(); } });
        }
    }
}
