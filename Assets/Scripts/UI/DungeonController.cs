using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Nilai Equipment dan Resource dipertahankan agar kode lain (view) yang memakainya tetap compile.
    public enum Tab { Home, Equipment, Resource, Party }

    // Pasang di satu GameObject kosong di scene Dungeon.
    // Mengatur Hub dan layar turunannya, top bar permanen, layar-layar run (pilih map, peta, party), pop-up, dan pengaturan.
    public class DungeonController : MonoBehaviour
    {
        RectTransform canvas, area, modalLayer;
        TopBarView topBar;
        HubView hubView;
        LegendsView legendsView;
        CompendiumView compendiumView;
        ArmoryView armoryView;
        AchievementsView achievementsView;
        SelectMapView selectView;
        MapView mapView;
        PartyView partyView;
        Tab tab = Tab.Home;
        Text toast;
        GameObject toastBox;
        float toastUntil;

        void Awake()
        {
            MetaProgress.SavePath = System.IO.Path.Combine(Application.persistentDataPath, "oddyseus_meta.txt");
            MetaProgress.Load();

            DungeonState.EnsureDungeon();
            UIFactory.EnsureEventSystem();
            Build();
            ShowTab(Tab.Home);
            ShowHub();
        }

        void Update()
        {
            if (toastBox != null && toastBox.activeSelf && Time.unscaledTime > toastUntil)
                toastBox.SetActive(false);
        }

        void Build()
        {
            canvas = UIFactory.CreateCanvas("DungeonCanvas");
            UIFactory.Panel(canvas, "Bg", Theme.Bg, 0f, 0f, 1f, 1f, false);

            // Top bar 2 baris: [Party] | Men Food | [Gear] / slot equipment | buff | Map
            topBar = TopBarView.Create(canvas);
            topBar.PartyClicked += () => OnTab(Tab.Party);
            topBar.SettingsClicked += ShowSettings;

            // Area utama: mulai tepat di bawah top bar.
            var frame = UIFactory.Panel(canvas, "Area", Theme.Panel, 0.03f, 0.03f, 0.97f, 1f, true);
            var fr = frame.rectTransform;
            fr.offsetMax = new Vector2(fr.offsetMax.x, -(TopBarView.BarHeight + 10f));
            area = fr;

            selectView = new SelectMapView(); selectView.Init(area, this, "SelectMap");
            mapView = new MapView(); mapView.Init(area, this, "Map");
            partyView = new PartyView(); partyView.Init(area, this, "Party");

            // Hub dan layar turunannya menutupi seluruh layar; toast dan modal dibuat sesudahnya agar tetap di atas.
            hubView = new HubView(); hubView.Init(canvas, this, "Hub");
            legendsView = new LegendsView(); legendsView.Init(canvas, this, "Legends");
            compendiumView = new CompendiumView(); compendiumView.Init(canvas, this, "Compendium");
            armoryView = new ArmoryView(); armoryView.Init(canvas, this, "Armory");
            achievementsView = new AchievementsView(); achievementsView.Init(canvas, this, "Achievements");

            var toastBg = UIFactory.Panel(canvas, "Toast", Theme.Dark, 0.25f, 0.045f, 0.75f, 0.11f, false);
            toast = UIFactory.Label(toastBg.transform, "", 22, Color.white, TextAnchor.MiddleCenter, 0.02f, 0f, 0.98f, 1f);
            toastBox = toastBg.gameObject;
            toastBox.SetActive(false);

            var ml = UIFactory.NewRect(canvas, "ModalLayer");
            UIFactory.Stretch(ml, 0f, 0f, 1f, 1f);
            modalLayer = ml;
            modalLayer.gameObject.SetActive(false);
        }

        // ---------- Hub ----------

        public void ShowHub()
        {
            hubView.Show(true);
            UIEffects.PopIn(hubView.Root);
        }

        // Tombol Enter/Continue di Hub: tutup Hub, lanjut ke peta atau Select Map.
        public void EnterFromHub()
        {
            hubView.Show(false);
            ShowTab(Tab.Home);
        }

        public void OpenLegends() { OpenSub(legendsView); }
        public void OpenCompendium() { OpenSub(compendiumView); }
        public void OpenArmory() { OpenSub(armoryView); }
        public void OpenAchievements() { OpenSub(achievementsView); }

        void OpenSub(View v)
        {
            v.Show(true);
            UIEffects.PopIn(v.Root);
        }

        public void CloseSub(View v)
        {
            v.Show(false);
        }

        // ---------- Achievement ----------

        void CheckAchievements() { Announce(AchievementCatalog.CheckUnlocks()); }

        public void Announce(List<Achievement> fresh)
        {
            if (fresh == null || fresh.Count == 0) return;
            string msg = "Achievement unlocked: " + fresh[0].Name;
            if (fresh.Count > 1) msg += " (+" + (fresh.Count - 1) + " more)";
            Toast(msg);
        }

        // Pack yang ditawarkan di Select Map dianggap "ditemukan" (Pack Compendium).
        void MarkOfferedSeen()
        {
            for (int i = 0; i < DungeonState.Offered.Count; i++) MetaProgress.MarkPackSeen(DungeonState.Offered[i].Id);
            CheckAchievements();
        }

        // Isi top bar. Hanya yang sudah pasti ada di DungeonState yang disambungkan.
        void RefreshTopBar()
        {
            topBar.SetMap(DungeonState.Floor, DungeonState.Floors);
            // TODO (butuh DungeonState.cs, EquipmentView.cs, ResourceView.cs):
            // topBar.SetResources(men, food);
            // topBar.SetEquipment(namaItem);
            // topBar.SetBuffs(namaBuff);
        }

        // ---------- Tab ----------

        void OnTab(Tab t)
        {
            ShowTab(tab == t ? Tab.Home : t);
        }

        public void ShowTab(Tab t)
        {
            // Equipment dan Resource sudah pindah ke top bar, bukan layar lagi.
            if (t == Tab.Equipment || t == Tab.Resource) t = Tab.Home;
            tab = t;
            selectView.Show(false); mapView.Show(false); partyView.Show(false);
            switch (t)
            {
                case Tab.Party: partyView.Show(true); break;
                default:
                    if (DungeonState.InMap) mapView.Show(true);
                    else { MarkOfferedSeen(); selectView.Show(true); }
                    break;
            }
            RefreshTopBar();
        }

        public void RefreshHome() { ShowTab(Tab.Home); }

        public void Toast(string msg)
        {
            toast.text = msg;
            toastBox.SetActive(true);
            toastBox.transform.SetAsLastSibling();
            UIEffects.PopIn(toastBox.transform);
            toastUntil = Time.unscaledTime + 2.2f;
        }

        // ---------- Pilih map dan node ----------

        public void OnMapChosen(int index)
        {
            DungeonState.SelectMap(index);
            ShowTab(Tab.Home);
        }

        public void OnNodeClicked(int id)
        {
            if (!DungeonState.CanEnter(id))
            {
                Toast("Pilih node yang terhubung dengan posisimu.");
                return;
            }
            var node = DungeonState.Enter(id);
            var type = node.Type;
            mapView.Refresh();
            string body = NodeInfo.Desc(node.Type);
            if (node.WasRandom) body = "The ? node became: " + NodeInfo.Name(node.Type) + ".\n\n" + body;
            body += "\n\n(Placeholder: mekanik node ini belum dibuat. Tekan Continue untuk lanjut.)";
            ShowModal(NodeInfo.Name(node.Type), body, new[] { "Continue" }, new Action[] { () => AfterNode(type) });
        }

        void AfterNode(NodeType type)
        {
            // Sementara boss dihitung kalah saat Continue ditekan; pindahkan ke hasil battle setelah BattleSystem tersambung.
            if (type == NodeType.GodRealm) MetaProgress.Add("stat.godRealms");
            if (type == NodeType.Boss && DungeonState.CurrentPack != null) MetaProgress.MarkPackCleared(DungeonState.CurrentPack.Id);
            CheckAchievements();

            if (DungeonState.AtActEnd) { ActEnd(); return; }
            mapView.Refresh();
        }

        void ActEnd()
        {
            int actDone = DungeonState.Act, floorDone = DungeonState.Floor;
            var res = DungeonState.FinishAct();
            switch (res)
            {
                case ActEndResult.NextAct:
                    ShowModal("Act " + actDone + " complete", "Act 2 begins.", new[] { "Continue" }, new Action[] { RefreshHome });
                    break;
                case ActEndResult.NextFloor:
                    ShowModal("Floor " + floorDone + " cleared", "Choose a Map for Floor " + DungeonState.Floor + ".",
                              new[] { "Continue" }, new Action[] { RefreshHome });
                    break;
                default:
                    ShowModal("Dungeon complete", "You cleared all " + DungeonState.Floors + " Floors.",
                              new[] { "Main Menu" }, new Action[] { FinishRun });
                    MetaProgress.Add("stat.runsCompleted");
                    CheckAchievements();   // dipanggil setelah modal dibuka agar toast tampil di atasnya
                    break;
            }
        }

        void FinishRun()
        {
            DungeonState.Active = false;
            SceneFader.LoadScene(GameScenes.Lobby);
        }

        // ---------- Pop-up ----------

        public void CloseModal()
        {
            UIFactory.ClearChildren(modalLayer);
            modalLayer.gameObject.SetActive(false);
        }

        RectTransform OpenModalLayer()
        {
            UIFactory.ClearChildren(modalLayer);
            modalLayer.gameObject.SetActive(true);
            modalLayer.SetAsLastSibling();
            UIFactory.Panel(modalLayer, "Dim", Theme.Dim, 0f, 0f, 1f, 1f, false);
            return modalLayer;
        }

        public void ShowModal(string title, string body, string[] labels, Action[] actions)
        {
            var layer = OpenModalLayer();
            var box = UIFactory.Panel(layer, "Box", Theme.Dark, 0.27f, 0.28f, 0.73f, 0.72f, false);
            UIEffects.PopIn(box);
            UIFactory.Label(box.transform, title, 32, Color.white, TextAnchor.MiddleCenter, 0.05f, 0.78f, 0.95f, 0.96f);
            UIFactory.Label(box.transform, body, 22, new Color(0.9f, 0.9f, 0.9f), TextAnchor.UpperLeft, 0.06f, 0.30f, 0.94f, 0.78f);
            int n = labels.Length;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                float w = 0.9f / n;
                float x0 = 0.05f + i * w + 0.01f, x1 = 0.05f + (i + 1) * w - 0.01f;
                UIFactory.Btn(box.transform, "Btn" + i, labels[i], Theme.Highlight, Theme.Ink, x0, 0.06f, x1, 0.24f,
                    () => { CloseModal(); if (actions[k] != null) actions[k](); }, 24);
            }
        }

        // ---------- Pengaturan ----------

        void ShowSettings()
        {
            var layer = OpenModalLayer();
            var box = UIFactory.Panel(layer, "Box", Theme.Dark, 0.25f, 0.30f, 0.75f, 0.70f, false);
            UIEffects.PopIn(box);
            Color yellow = Theme.Highlight;
            UIFactory.Btn(box.transform, "Restart", "RESTART\nMap", yellow, Theme.Ink, 0.06f, 0.38f, 0.32f, 0.88f, ConfirmRestart, 22);
            UIFactory.Btn(box.transform, "Reset", "RESET\nDungeon", yellow, Theme.Ink, 0.37f, 0.38f, 0.63f, 0.88f, ConfirmReset, 22);
            UIFactory.Btn(box.transform, "Exit", "EXIT\nMain Menu", yellow, Theme.Ink, 0.68f, 0.38f, 0.94f, 0.88f, ExitToMenu, 22);

            UIFactory.Label(box.transform, "Music", 18, Color.white, TextAnchor.MiddleLeft, 0.06f, 0.20f, 0.22f, 0.32f);
            UIFactory.MakeSlider(box.transform, "MusicSlider", 0.24f, 0.20f, 0.94f, 0.32f, PlayerPrefs.GetFloat("music", 0.7f),
                v => { PlayerPrefs.SetFloat("music", v); AudioListener.volume = v; });
            UIFactory.Label(box.transform, "SFX", 18, Color.white, TextAnchor.MiddleLeft, 0.06f, 0.05f, 0.22f, 0.17f);
            UIFactory.MakeSlider(box.transform, "SfxSlider", 0.24f, 0.05f, 0.94f, 0.17f, PlayerPrefs.GetFloat("sfx", 0.7f),
                v => PlayerPrefs.SetFloat("sfx", v));

            var close = UIFactory.Btn(layer, "Close", "X", Theme.Bad, Color.white, 0.735f, 0.67f, 0.77f, 0.73f, CloseModal, 22);
            close.transform.SetAsLastSibling();
        }

        void ConfirmRestart()
        {
            if (DungeonState.CurrentPack == null) { CloseModal(); Toast("Belum ada Map yang dipilih."); return; }
            ShowModal("Restart Map?", "Progress di Map ini kembali ke node pertama (Act 1).",
                      new[] { "Cancel", "Restart" }, new Action[] { null, () => { DungeonState.RestartMap(); ShowTab(Tab.Home); } });
        }

        void ConfirmReset()
        {
            ShowModal("Reset Dungeon?", "Seluruh run Dungeon Mode diulang dari awal (Men, Food, party, relic ikut ter-reset).",
                      new[] { "Cancel", "Reset" }, new Action[] { null, () => { DungeonState.ResetDungeon(); ShowTab(Tab.Home); } });
        }

        void ExitToMenu()
        {
            // Belum ada save run ke disk: progres run hanya bertahan selama aplikasi terbuka (progres meta tersimpan di file).
            SceneFader.LoadScene(GameScenes.Lobby);
        }
    }
}