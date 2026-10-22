using System;
using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Memutar sebuah RectTransform pelan-pelan (dekorasi kompas di Hub).
    public class HubSpin : MonoBehaviour
    {
        public float Speed = 6f;
        void Update() { transform.Rotate(0f, 0f, Speed * Time.unscaledDeltaTime); }
    }

    // Layar pembuka Dungeon Mode, tampil sebelum Select Map. Dibangun penuh lewat kode.
    public class HubView : View
    {
        Text statusText, resText;
        readonly Image[] slotImgs = new Image[RunState.PartySize];
        readonly Text[] slotTexts = new Text[RunState.PartySize];
        Button enterBtn;
        GameObject newRunBtn;

        protected override void Build()
        {
            HubUI.Flat(Root, "Bg", HubTheme.Night, 0f, 0f, 1f, 1f, true);
            BuildCompass();

            HubUI.Flat(Root, "TopBand", HubTheme.Deep, 0f, 0.92f, 1f, 1f);
            HubUI.Flat(Root, "TopLine", HubTheme.Bronze, 0f, 0.916f, 1f, 0.92f);
            HubUI.Flat(Root, "BottomBand", HubTheme.Deep, 0f, 0f, 1f, 0.06f);
            HubUI.Flat(Root, "BottomLine", HubTheme.Bronze, 0f, 0.06f, 1f, 0.064f);

            UIFactory.Label(Root, "DUNGEON MODE", 28, HubTheme.Marble, TextAnchor.MiddleCenter, 0.35f, 0.93f, 0.65f, 0.99f);
            HubUI.Btn(Root, "Back", "< BACK", null, HubTheme.Panel, HubTheme.Marble,
                      0.015f, 0.93f, 0.10f, 0.99f, () => SceneFader.LoadScene(GameScenes.Lobby), 20);

            UIFactory.Label(Root, "Chart your own course home.", 20, HubTheme.Muted, TextAnchor.MiddleCenter,
                            0.30f, 0.10f, 0.70f, 0.17f);

            // Menu kiri.
            HubUI.Btn(Root, "Legends", "LEGENDS", "Your roster", HubTheme.Panel, HubTheme.Marble,
                      0.03f, 0.67f, 0.27f, 0.80f, () => Host.OpenLegends(), 26);
            HubUI.Btn(Root, "Compendium", "PACK COMPENDIUM", "Packs, bosses, equipment", HubTheme.Panel, HubTheme.Marble,
                      0.03f, 0.51f, 0.27f, 0.64f, () => Host.OpenCompendium(), 26);
            HubUI.Btn(Root, "Armory", "ARMORY", "Equipment and artifacts", HubTheme.Panel, HubTheme.Marble,
                      0.03f, 0.35f, 0.27f, 0.48f, () => Host.OpenArmory(), 26);
            HubUI.Btn(Root, "Achievements", "ACHIEVEMENTS", "Unlock characters and costumes", HubTheme.Panel, HubTheme.Marble,
                      0.03f, 0.19f, 0.27f, 0.32f, () => Host.OpenAchievements(), 26);

            // Panel kanan.
            var d = HubTheme.Deep;
            var cardFill = new Color(d.r, d.g, d.b, 0.92f);
            var status = HubUI.Card(Root, "StatusCard", cardFill, 0.73f, 0.68f, 0.97f, 0.86f, HubTheme.Bronze);
            UIFactory.Label(status.transform, "EXPLORING", 16, HubTheme.Gold, TextAnchor.UpperLeft, 0.06f, 0.64f, 0.94f, 0.94f);
            statusText = UIFactory.Label(status.transform, "", 24, HubTheme.Marble, TextAnchor.MiddleLeft, 0.06f, 0.06f, 0.94f, 0.64f);

            var crew = HubUI.Card(Root, "CrewCard", cardFill, 0.73f, 0.36f, 0.97f, 0.65f, HubTheme.Bronze);
            UIFactory.Label(crew.transform, "CREW", 16, HubTheme.Gold, TextAnchor.UpperLeft, 0.06f, 0.80f, 0.50f, 0.96f);
            for (int i = 0; i < slotImgs.Length; i++)
            {
                float x0 = 0.06f + i * 0.225f, x1 = x0 + 0.20f;
                slotImgs[i] = UIFactory.Panel(crew.transform, "Slot" + i, HubTheme.Deep, x0, 0.32f, x1, 0.76f, false);
                HubTheme.Frame(slotImgs[i], HubTheme.Bronze);
                slotTexts[i] = UIFactory.Label(slotImgs[i].transform, "-", 26, HubTheme.Marble, TextAnchor.MiddleCenter,
                                               0.1f, 0.1f, 0.9f, 0.9f);
                slotTexts[i].fontStyle = FontStyle.Bold;
            }
            resText = UIFactory.Label(crew.transform, "", 18, HubTheme.Marble, TextAnchor.MiddleLeft, 0.06f, 0.04f, 0.94f, 0.28f);

            enterBtn = HubUI.Btn(Root, "Enter", "ENTER", null, HubTheme.Gold, HubTheme.Night,
                                 0.73f, 0.17f, 0.97f, 0.31f, () => Host.EnterFromHub(), 32);
            var nr = HubUI.Btn(Root, "NewRun", "NEW RUN", null, HubTheme.Panel, HubTheme.Marble,
                               0.73f, 0.08f, 0.97f, 0.15f, OnNewRun, 18);
            newRunBtn = nr.gameObject;
        }

        // Kompas dekoratif di tengah: cincin, tick, dan jarum yang berputar pelan.
        void BuildCompass()
        {
            var box = UIFactory.NewRect(Root, "Compass");
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.pivot = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(460f, 460f);
            box.anchoredPosition = new Vector2(0f, 10f);

            var c = new Vector2(230f, 230f);
            HubUI.Disc(box, "RingOuter", c, 450f, HubTheme.Bronze);
            HubUI.Disc(box, "RingOuterIn", c, 444f, HubTheme.Night);
            HubUI.Disc(box, "Halo", c, 400f, new Color(HubTheme.Teal.r, HubTheme.Teal.g, HubTheme.Teal.b, 0.10f));

            var ticks = SpinLayer(box, "Ticks", 460f, 5f);
            for (int i = 0; i < 24; i++)
            {
                float a = i * 15f * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                bool major = i % 3 == 0;
                UIFactory.Segment(ticks, c + dir * 192f, c + dir * (major ? 218f : 208f), major ? 4f : 2f, HubTheme.Gold);
            }

            var needles = SpinLayer(box, "Needles", 460f, -8f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                bool major = i % 2 == 0;
                UIFactory.Segment(needles, c + dir * 126f, c + dir * (major ? 175f : 150f), major ? 6f : 3f, HubTheme.Bronze);
            }

            HubUI.Disc(box, "DiskRing", c, 262f, HubTheme.Gold);
            HubUI.Disc(box, "Disk", c, 254f, HubTheme.Deep);

            UIFactory.Label(box, "ODDYSEUS", 44, HubTheme.Marble, TextAnchor.MiddleCenter, 0.18f, 0.50f, 0.82f, 0.64f);
            HubUI.Flat(box, "Divider", HubTheme.Bronze, 0.36f, 0.495f, 0.64f, 0.50f);
            UIFactory.Label(box, "JOURNEY'S END", 20, HubTheme.Gold, TextAnchor.MiddleCenter, 0.22f, 0.40f, 0.78f, 0.485f);
        }

        public override void Refresh()
        {
            bool progress = DungeonState.InMap || DungeonState.Floor > 1;
            UIFactory.SetBtnText(enterBtn, progress ? "CONTINUE" : "ENTER");
            newRunBtn.SetActive(progress);

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
                    var col = Theme.Hex(cs.Data.PortraitHex);
                    slotImgs[i].color = cs.Alive ? col : col * 0.4f;
                    string n = string.IsNullOrEmpty(cs.Data.Name) ? "?" : cs.Data.Name.Substring(0, 1);
                    slotTexts[i].text = cs.Alive ? n : "X";
                }
                else
                {
                    slotImgs[i].color = HubTheme.Deep;
                    slotTexts[i].text = "-";
                }
            }

            resText.text = "Men " + RunState.Men + "   Food " + RunState.Food + "   Gold " + RunState.Gold;
        }

        void OnNewRun()
        {
            Host.ShowModal("Start a new run?", "Progress run yang sedang berjalan akan dihapus.",
                           new[] { "Cancel", "New Run" },
                           new Action[] { null, () => { DungeonState.ResetDungeon(); Refresh(); } });
        }

        static RectTransform SpinLayer(Transform parent, string name, float size, float speed)
        {
            var rt = UIFactory.NewRect(parent, name);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            rt.gameObject.AddComponent<HubSpin>().Speed = speed;
            return rt;
        }
    }
}