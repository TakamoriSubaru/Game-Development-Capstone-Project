using System;
using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Layar pembuka Dungeon Mode, tampil sebelum Select Map. Dibangun penuh lewat kode.
    // Logika (ENTER/CONTINUE, NEW RUN, status, kru) sama seperti sebelumnya; hanya tampilannya yang baru.
    public class HubView : View
    {
        Text statusText, menVal, foodVal, goldVal;
        readonly Image[] slotImgs = new Image[RunState.PartySize];
        Button enterBtn, newRunBtn;

        protected override void Build()
        {
            ParchUI.Background(Root, true, true, true, false);
            ParchUI.Solid(Root, "Block", new Color(0f, 0f, 0f, 0f), 0f, 0f, 1f, 1f).raycastTarget = true;
            var safe = ParchUI.SafeArea(Root);

            ParchUI.TitleBar(safe, "DUNGEON MODE", () => SceneFader.LoadScene(GameScenes.Lobby), false);

            // --- kompas: teks judul asli di cakram kosong ---
            var comp = ParchUI.FitPic(safe, "Compass", "hub/compass_medallion", 0.335f, 0.25f, 0.665f, 0.86f);
            ParchUI.Txt(comp.transform, "ODDYSEUS", 40, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.33f, 0.50f, 0.67f, 0.60f, true);
            ParchUI.Pic(comp.transform, "Rule", "ornaments/divider_diamond", 0.37f, 0.485f, 0.63f, 0.505f, false);
            ParchUI.Txt(comp.transform, "JOURNEY'S END", 18, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.34f, 0.41f, 0.66f, 0.485f, true);
            ParchUI.Txt(safe, "Chart your own course home.", 20, Parch.Muted, TextAnchor.MiddleCenter, 0.30f, 0.17f, 0.70f, 0.24f);

            // --- menu kiri ---
            string[] icons = { "icons/icon_menu_legends", "icons/icon_menu_packs", "icons/icon_menu_armory", "icons/icon_menu_achievements" };
            string[] titles = { "LEGENDS", "PACK COMPENDIUM", "ARMORY", "ACHIEVEMENTS" };
            string[] subs = { "Your roster", "Packs, bosses, equipment", "Equipment and artifacts", "Unlock characters and costumes" };
            Action[] go = { () => Host.OpenLegends(), () => Host.OpenCompendium(), () => Host.OpenArmory(), () => Host.OpenAchievements() };
            for (int i = 0; i < 4; i++)
            {
                float y1 = 0.845f - i * 0.16f;
                ParchUI.MenuButton(safe, "Menu" + i, icons[i], titles[i], subs[i], 0.03f, y1 - 0.14f, 0.335f, y1, go[i]);
            }

            // --- panel run (kanan) ---
            var panel = ParchUI.Panel9(safe, "RunPanel", 0.685f, 0.17f, 0.975f, 0.835f, 0.45f).rectTransform;
            statusText = ParchUI.Txt(panel, "", 26, Parch.DarkBrown, TextAnchor.UpperLeft, 0.09f, 0.78f, 0.92f, 0.94f, true);
            ParchUI.Pic(panel, "Rule", "ornaments/divider_diamond", 0.09f, 0.745f, 0.91f, 0.775f, false);
            ParchUI.Txt(panel, "CREW", 20, Parch.DarkBrown, TextAnchor.MiddleLeft, 0.09f, 0.665f, 0.50f, 0.74f, true);
            for (int i = 0; i < slotImgs.Length; i++)
            {
                float x0 = 0.08f + i * 0.2275f;
                slotImgs[i] = ParchUI.FitPic(panel, "Slot" + i, "portraits/portrait_locked_round", x0, 0.47f, x0 + 0.205f, 0.655f);
            }

            string[] ric = { "icons/icon_res_men", "icons/icon_res_food", "icons/icon_res_gold" };
            string[] rname = { "Men", "Food", "Gold" };
            var vals = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                float x0 = 0.08f + i * 0.31f;
                ParchUI.FitPic(panel, "Res" + i, ric[i], x0, 0.355f, x0 + 0.115f, 0.45f);
                vals[i] = ParchUI.Txt(panel, "", 24, Parch.DarkBrown, TextAnchor.LowerLeft, x0 + 0.12f, 0.395f, x0 + 0.29f, 0.45f, true);
                ParchUI.Txt(panel, rname[i], 14, Parch.Muted, TextAnchor.UpperLeft, x0 + 0.12f, 0.35f, x0 + 0.29f, 0.395f);
            }
            menVal = vals[0]; foodVal = vals[1]; goldVal = vals[2];

            enterBtn = ParchUI.Pill(panel, "Enter", "ENTER", "parchment", ParchUI.GoldTint, 0.08f, 0.18f, 0.92f, 0.32f,
                                    () => Host.EnterFromHub(), 30, 0.6f);
            newRunBtn = ParchUI.Pill(panel, "NewRun", "NEW RUN", "alt", 0.08f, 0.05f, 0.92f, 0.145f, OnNewRun, 20, 0.5f);
        }

        public override void Refresh()
        {
            bool progress = DungeonState.InMap || DungeonState.Floor > 1;
            // Tanpa progres tombol emas = ENTER (memulai run dari Select Map); dengan progres = CONTINUE.
            enterBtn.GetComponent<UiBtn>().Label.text = progress ? "CONTINUE" : "ENTER";
            newRunBtn.gameObject.SetActive(progress);

            if (DungeonState.InMap && DungeonState.CurrentPack != null)
                statusText.text = "Floor " + DungeonState.Floor + "/" + DungeonState.Floors + " - "
                                  + DungeonState.CurrentPack.Name + "\nAct " + DungeonState.Act;
            else if (DungeonState.Floor > 1)
                statusText.text = "Floor " + DungeonState.Floor + "/" + DungeonState.Floors + "\nChoose a Map";
            else
                statusText.text = "Before Entry";

            for (int i = 0; i < slotImgs.Length; i++)
            {
                if (i < DungeonState.Party.Count && DungeonState.Party[i] < RunState.Roster.Count)
                {
                    var cs = RunState.Roster[DungeonState.Party[i]];
                    var sp = UISprites.Get("portraits/portrait_" + cs.Data.Id + "_round");
                    slotImgs[i].sprite = sp != null ? sp : UISprites.Get("portraits/portrait_locked_round");
                    slotImgs[i].color = cs.Alive ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.8f);
                }
                else
                {
                    slotImgs[i].sprite = UISprites.Get("portraits/portrait_locked_round");
                    slotImgs[i].color = new Color(1f, 1f, 1f, 0.55f);
                }
            }

            menVal.text = RunState.Men.ToString();
            foodVal.text = RunState.Food.ToString();
            goldVal.text = RunState.Gold.ToString();
        }

        void OnNewRun()
        {
            Host.ShowModal("Start a new run?", "Progress run yang sedang berjalan akan dihapus.",
                           new[] { "Cancel", "New Run" },
                           new Action[] { null, () => { DungeonState.ResetDungeon(); Refresh(); } });
        }
    }
}