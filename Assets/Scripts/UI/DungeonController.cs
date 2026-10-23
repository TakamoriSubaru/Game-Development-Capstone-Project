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
            UIFactory.Panel(canvas, "Bg", HubTheme.Night, 0f, 0f, 1f, 1f, false);

            // Top bar 2 baris: [Party] | Men Food | [Gear] / equipment | blessing | Floor
            topBar = TopBarView.Create(canvas);
            topBar.PartyClicked += () => OnTab(Tab.Party);
            topBar.SettingsClicked += ShowSettings;

            // Area utama: mulai tepat di bawah top bar.
            var frame = UIFactory.Panel(canvas, "Area", HubTheme.Night, 0.03f, 0.03f, 0.97f, 1f, true);
            HubTheme.Frame(frame, HubTheme.Bronze);
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

            // Tombol uji (hanya Editor dan Development Build), tampil di pita bawah Hub.
            if (Debug.isDebugBuild)
            {
                HubUI.Btn(hubView.Root, "DbgRelic", "DEBUG: +RELIC", null, HubTheme.Panel, HubTheme.Muted,
                          0.02f, 0.008f, 0.19f, 0.052f, DebugAddRelic, 14);
                HubUI.Btn(hubView.Root, "DbgBless", "DEBUG: +BLESSING", null, HubTheme.Panel, HubTheme.Muted,
                          0.20f, 0.008f, 0.40f, 0.052f, DebugAddBlessing, 14);
            }

            var toastBg = HubUI.Card(canvas, "Toast", HubTheme.Deep, 0.25f, 0.045f, 0.75f, 0.11f, HubTheme.Gold);
            toast = UIFactory.Label(toastBg.transform, "", 22, HubTheme.Marble, TextAnchor.MiddleCenter, 0.02f, 0f, 0.98f, 1f);
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

        // ---------- Uji ----------

        void DebugAddRelic()
        {
            int n = DungeonState.Equipment.Count + 1;
            DungeonState.AddRelic(new Relic { Name = "Relic " + n, Description = "Debug", IsGod = false });
            RefreshTopBar();
            Toast("Debug: equipment ditambah (" + n + ").");
        }

        void DebugAddBlessing()
        {
            string[] gods = { "Apollo", "Athena", "Ares", "Hephaestus", "Artemis", "Demeter", "Hermes", "Zeus" };
            string g = gods[DungeonState.God.Count % gods.Length];
            DungeonState.AddRelic(new Relic { Name = g, Description = "Debug", IsGod = true });
            RefreshTopBar();
            Toast("Debug: Blessing " + g + " (" + DungeonState.BlessingBattles + " battle).");
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

        // Top bar: Floor, Men, Food, equipment, dan Blessing (dengan sisa battle) dari status run.
        void RefreshTopBar()
        {
            topBar.SetMap(DungeonState.Floor, DungeonState.Floors);
            topBar.SetResources(RunState.Men, RunState.Food);

            var eq = new List<string>();
            for (int i = 0; i < DungeonState.Equipment.Count; i++)
                if (DungeonState.Equipment[i] != null) eq.Add(DungeonState.Equipment[i].Name);
            topBar.SetEquipment(eq);

            var gods = new List<string>();
            for (int i = 0; i < DungeonState.God.Count; i++)
                if (DungeonState.God[i] != null) gods.Add(DungeonState.God[i].Name + "\n" + DungeonState.God[i].BattlesLeft);
            topBar.SetBuffs(gods);
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
            // Sementara battle dianggap selesai saat Continue ditekan; pindahkan ke hasil battle setelah BattleSystem tersambung.
            if (type == NodeType.GodRealm) MetaProgress.Add("stat.godRealms");
            if (type == NodeType.Boss && DungeonState.CurrentPack != null) MetaProgress.MarkPackCleared(DungeonState.CurrentPack.Id);

            string expiredMsg = null;
            if (type == NodeType.Battle || type == NodeType.Elite || type == NodeType.Boss)
            {
                var gone = DungeonState.TickBlessings();
                if (gone.Count > 0) expiredMsg = "Blessing faded: " + string.Join(", ", gone.ToArray());
            }

            var fresh = AchievementCatalog.CheckUnlocks();
            RefreshTopBar();
            if (expiredMsg != null && fresh.Count > 0) Toast(expiredMsg + "  |  Achievement: " + fresh[0].Name);
            else if (expiredMsg != null) Toast(expiredMsg);
            else Announce(fresh);

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

        // Tombol terakhir = aksi utama (emas); tombol lain (mis. Cancel) bergaya sekunder.
        public void ShowModal(string title, string body, string[] labels, Action[] actions)
        {
            var layer = OpenModalLayer();
            var box = HubUI.Card(layer, "Box", HubTheme.Deep, 0.27f, 0.26f, 0.73f, 0.74f, HubTheme.Bronze);
            UIEffects.PopIn(box);
            UIFactory.Label(box.transform, title, 30, HubTheme.Marble, TextAnchor.MiddleLeft, 0.06f, 0.80f, 0.94f, 0.96f);
            HubUI.Flat(box.transform, "Divider", HubTheme.Bronze, 0.06f, 0.785f, 0.94f, 0.792f);
            UIFactory.Label(box.transform, body, 20, HubTheme.Marble, TextAnchor.UpperLeft, 0.06f, 0.30f, 0.94f, 0.76f);
            int n = labels.Length;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                bool primary = i == n - 1;
                float w = 0.9f / n;
                float x0 = 0.05f + i * w + 0.01f, x1 = 0.05f + (i + 1) * w - 0.01f;
                HubUI.Btn(box.transform, "Btn" + i, labels[i], null,
                          primary ? HubTheme.Gold : HubTheme.Panel, primary ? HubTheme.Night : HubTheme.Marble,
                          x0, 0.06f, x1, 0.24f, () => { CloseModal(); if (actions[k] != null) actions[k](); }, 24);
            }
        }

        // ---------- Pengaturan ----------

        void ShowSettings()
        {
            var layer = OpenModalLayer();
            var box = HubUI.Card(layer, "Box", HubTheme.Deep, 0.24f, 0.16f, 0.76f, 0.84f, HubTheme.Bronze);
            UIEffects.PopIn(box);
            var t = box.transform;

            UIFactory.Label(t, "SETTINGS", 30, HubTheme.Marble, TextAnchor.MiddleLeft, 0.05f, 0.86f, 0.70f, 0.97f);
            HubUI.Btn(t, "Close", "X", null, HubTheme.Danger, Color.white, 0.90f, 0.87f, 0.97f, 0.96f, CloseModal, 20);
            HubUI.Flat(t, "Divider", HubTheme.Bronze, 0.05f, 0.845f, 0.95f, 0.852f);

            UIFactory.Label(t, "RUN", 14, HubTheme.Gold, TextAnchor.MiddleLeft, 0.05f, 0.77f, 0.50f, 0.83f);
            HubUI.Btn(t, "Restart", "RESTART MAP", "Back to the first node", HubTheme.Panel, HubTheme.Marble,
                      0.05f, 0.47f, 0.35f, 0.75f, ConfirmRestart, 20);
            HubUI.Btn(t, "Reset", "RESET DUNGEON", "Start the run over", HubTheme.Panel, HubTheme.Marble,
                      0.36f, 0.47f, 0.64f, 0.75f, ConfirmReset, 20);
            HubUI.Btn(t, "Exit", "EXIT", "Back to the Lobby", HubTheme.Panel, HubTheme.Marble,
                      0.65f, 0.47f, 0.95f, 0.75f, ExitToMenu, 20);

            UIFactory.Label(t, "AUDIO", 14, HubTheme.Gold, TextAnchor.MiddleLeft, 0.05f, 0.38f, 0.50f, 0.44f);
            VolumeRow(t, "Music", "music", 0.25f, 0.35f, true);
            VolumeRow(t, "SFX", "sfx", 0.10f, 0.20f, false);
        }

        void VolumeRow(Transform parent, string label, string key, float y0, float y1, bool master)
        {
            UIFactory.Label(parent, label, 20, HubTheme.Marble, TextAnchor.MiddleLeft, 0.05f, y0, 0.20f, y1);
            var pct = UIFactory.Label(parent, "", 18, HubTheme.Gold, TextAnchor.MiddleRight, 0.86f, y0, 0.95f, y1);
            float v = PlayerPrefs.GetFloat(key, 0.7f);
            pct.text = Mathf.RoundToInt(v * 100f) + "%";
            UIFactory.MakeSlider(parent, label + "Slider", 0.22f, y0, 0.85f, y1, v, val =>
            {
                PlayerPrefs.SetFloat(key, val);
                if (master) AudioListener.volume = val;
                pct.text = Mathf.RoundToInt(val * 100f) + "%";
            });
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