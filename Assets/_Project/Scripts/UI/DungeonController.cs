using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    public enum Tab { Home, Equipment, Resource, Party }

    // Pasang di satu GameObject kosong di scene Dungeon.
    // Mengatur bar atas (tab), layar-layar (pilih map, peta, party, equipment, resource), pop-up, dan pengaturan.
    public class DungeonController : MonoBehaviour
    {
        RectTransform canvas, area, modalLayer;
        Button equipBtn, resBtn, partyBtn;
        SelectMapView selectView;
        MapView mapView;
        PartyView partyView;
        EquipmentView equipView;
        ResourceView resView;
        Tab tab = Tab.Home;
        Text toast;
        GameObject toastBox;
        float toastUntil;

        void Awake()
        {
            DungeonState.EnsureDungeon();
            UIFactory.EnsureEventSystem();
            Build();
            ShowTab(Tab.Home);
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

            // Bar atas sesuai wireframe: Equipment, Resource, Party, Gear.
            equipBtn = UIFactory.Btn(canvas, "EquipTab", "EQUIPMENT", Theme.Card, Theme.Ink, 0.03f, 0.90f, 0.46f, 0.98f, () => OnTab(Tab.Equipment), 26);
            resBtn = UIFactory.Btn(canvas, "ResourceTab", "RESOURCE", Theme.Card, Theme.Ink, 0.47f, 0.90f, 0.61f, 0.98f, () => OnTab(Tab.Resource), 22);
            partyBtn = UIFactory.Btn(canvas, "PartyTab", "PARTY", Theme.Card, Theme.Ink, 0.62f, 0.90f, 0.80f, 0.98f, () => OnTab(Tab.Party), 24);
            UIFactory.Btn(canvas, "GearBtn", "GEAR", Theme.Card, Theme.Ink, 0.81f, 0.90f, 0.97f, 0.98f, ShowSettings, 22);

            var frame = UIFactory.Panel(canvas, "Area", Theme.Panel, 0.03f, 0.03f, 0.97f, 0.88f, true);
            area = frame.rectTransform;

            selectView = new SelectMapView(); selectView.Init(area, this, "SelectMap");
            mapView = new MapView(); mapView.Init(area, this, "Map");
            partyView = new PartyView(); partyView.Init(area, this, "Party");
            equipView = new EquipmentView(); equipView.Init(area, this, "Equipment");
            resView = new ResourceView(); resView.Init(area, this, "Resource");

            var toastBg = UIFactory.Panel(canvas, "Toast", Theme.Dark, 0.25f, 0.045f, 0.75f, 0.11f, false);
            toast = UIFactory.Label(toastBg.transform, "", 22, Color.white, TextAnchor.MiddleCenter, 0.02f, 0f, 0.98f, 1f);
            toastBox = toastBg.gameObject;
            toastBox.SetActive(false);

            var ml = UIFactory.NewRect(canvas, "ModalLayer");
            UIFactory.Stretch(ml, 0f, 0f, 1f, 1f);
            modalLayer = ml;
            modalLayer.gameObject.SetActive(false);
        }

        // ---------- Tab ----------

        void OnTab(Tab t)
        {
            ShowTab(tab == t ? Tab.Home : t);
        }

        public void ShowTab(Tab t)
        {
            tab = t;
            selectView.Show(false); mapView.Show(false); partyView.Show(false);
            equipView.Show(false); resView.Show(false);
            switch (t)
            {
                case Tab.Equipment: equipView.Show(true); break;
                case Tab.Resource: resView.Show(true); break;
                case Tab.Party: partyView.Show(true); break;
                default:
                    if (DungeonState.InMap) mapView.Show(true); else selectView.Show(true);
                    break;
            }
            UIFactory.SetBtnFill(equipBtn, t == Tab.Equipment ? Theme.TabActive : Theme.Card);
            UIFactory.SetBtnFill(resBtn, t == Tab.Resource ? Theme.TabActive : Theme.Card);
            UIFactory.SetBtnFill(partyBtn, t == Tab.Party ? Theme.TabActive : Theme.Card);
        }

        public void RefreshHome() { ShowTab(Tab.Home); }

        public void Toast(string msg)
        {
            toast.text = msg;
            toastBox.SetActive(true);
            toastBox.transform.SetAsLastSibling();
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
            mapView.Refresh();
            string body = NodeInfo.Desc(node.Type);
            if (node.WasRandom) body = "The ? node became: " + NodeInfo.Name(node.Type) + ".\n\n" + body;
            body += "\n\n(Placeholder: mekanik node ini belum dibuat. Tekan Continue untuk lanjut.)";
            ShowModal(NodeInfo.Name(node.Type), body, new[] { "Continue" }, new Action[] { AfterNode });
        }

        void AfterNode()
        {
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
                    break;
            }
        }

        void FinishRun()
        {
            DungeonState.Active = false;
            SceneManager.LoadScene(SceneNames.MainMenu);
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
            // Belum ada save ke disk: progres hanya bertahan selama aplikasi terbuka.
            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
