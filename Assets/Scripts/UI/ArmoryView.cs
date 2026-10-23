using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Equipment dari boss tiap Pack (siluet sampai ditemukan) dan Blessing para dewa (GDD 6.2). Klik item untuk detail.
    public class ArmoryView : HubScreen
    {
        bool blessings;
        int sel;

        protected override string Title { get { return "ARMORY"; } }

        protected override void Populate(RectTransform body)
        {
            Tab(body, "EQUIPMENT", !blessings, 0.03f, () => { blessings = false; sel = 0; Refresh(); });
            Tab(body, "BLESSINGS", blessings, 0.18f, () => { blessings = true; sel = 0; Refresh(); });

            int count = blessings ? BlessingCatalog.All.Length : EquipmentCatalog.All.Length;
            int found = 0;
            if (!blessings)
                for (int i = 0; i < count; i++) if (MetaProgress.IsPackCleared(EquipmentCatalog.All[i].PackId)) found++;
            UIFactory.Label(body, blessings ? "Blessings: single-use, drain the target's Stamina." : "Found " + found + "/" + count,
                            18, HubTheme.Gold, TextAnchor.MiddleRight, 0.40f, 0.90f, 0.97f, 0.98f);

            if (sel >= count) sel = 0;
            for (int i = 0; i < count; i++) DrawCell(body, i);
            DrawDetail(body);
        }

        static void Tab(RectTransform body, string label, bool active, float x0, System.Action onClick)
        {
            HubUI.Btn(body, label, label, null, active ? HubTheme.Gold : HubTheme.Panel,
                      active ? HubTheme.Night : HubTheme.Marble, x0, 0.90f, x0 + 0.14f, 0.98f, onClick, 18);
        }

        void DrawCell(RectTransform body, int i)
        {
            int idx = i;
            int col = i % 5, row = i / 5;
            float x0 = 0.03f + col * 0.1225f, x1 = x0 + 0.11f;
            float y1 = 0.86f - row * 0.1767f, y0 = y1 - 0.16f;

            bool known = blessings || MetaProgress.IsPackCleared(EquipmentCatalog.All[i].PackId);
            string name = blessings ? BlessingCatalog.All[i].God : (known ? EquipmentCatalog.All[i].Name : "???");

            var img = UIFactory.Panel(body, "Cell" + i, known ? HubTheme.Panel : HubTheme.Deep, x0, y0, x1, y1, false);
            HubTheme.Frame(img, i == sel ? HubTheme.Gold : HubTheme.Bronze);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => { sel = idx; Refresh(); });
            img.gameObject.AddComponent<ButtonFx>();

            var g = UIFactory.Label(img.transform, known ? HubUI.Initials(name) : "?", 30, known ? HubTheme.Marble : HubTheme.Muted,
                                    TextAnchor.MiddleCenter, 0.05f, 0.36f, 0.95f, 0.95f);
            g.fontStyle = FontStyle.Bold;
            UIFactory.Label(img.transform, name, 11, HubTheme.Muted, TextAnchor.MiddleCenter, 0.04f, 0.03f, 0.96f, 0.36f);
        }

        void DrawDetail(RectTransform body)
        {
            var card = HubUI.Card(body, "Detail", HubTheme.Deep, 0.67f, 0.04f, 0.97f, 0.86f, HubTheme.Bronze);
            var t = card.transform;
            string title, sub, text, foot;

            if (blessings)
            {
                var bl = BlessingCatalog.All[sel];
                title = "Blessing of " + bl.God;
                sub = "Target: " + bl.Target;
                text = bl.Effect;
                foot = "Price: " + bl.Gold + " Gold.\nSingle-use. Gives a massive buff, then drains the target's Stamina.";
            }
            else
            {
                var e = EquipmentCatalog.All[sel];
                var p = EquipmentCatalog.PackOf(e);
                bool known = MetaProgress.IsPackCleared(e.PackId);
                if (known)
                {
                    title = e.Name;
                    sub = p != null ? "From " + p.Name + " (Floor " + p.Floor + ")" : "";
                    text = e.Effect;
                    foot = p != null ? "Dropped by " + p.Boss + "." : "";
                }
                else
                {
                    title = "???";
                    sub = p != null ? "Floor " + p.Floor + " Pack" : "";
                    text = "Not found yet. Defeat a Pack's boss to claim its equipment.";
                    foot = "";
                }
            }

            UIFactory.Label(t, title, 26, HubTheme.Marble, TextAnchor.MiddleLeft, 0.06f, 0.82f, 0.94f, 0.96f);
            UIFactory.Label(t, sub, 15, HubTheme.Gold, TextAnchor.MiddleLeft, 0.06f, 0.72f, 0.94f, 0.82f);
            HubUI.Flat(t, "Divider", HubTheme.Bronze, 0.06f, 0.705f, 0.94f, 0.712f);
            UIFactory.Label(t, text, 18, HubTheme.Marble, TextAnchor.UpperLeft, 0.06f, 0.32f, 0.94f, 0.69f);
            UIFactory.Label(t, foot, 14, HubTheme.Muted, TextAnchor.UpperLeft, 0.06f, 0.04f, 0.94f, 0.30f);
        }
    }
}