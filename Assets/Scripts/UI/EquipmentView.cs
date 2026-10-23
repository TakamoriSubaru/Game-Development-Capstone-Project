using UnityEngine;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Tab Equipment: tempat menyimpan semua relic. Dibagi 7 slot equipment dan 3 slot Blessing dewa (God Realm).
    public class EquipmentView : View
    {
        RectTransform equipBox, godBox, detail;
        Relic selected;

        protected override void Build()
        {
            UIFactory.Label(Root, "EQUIPMENT  (7)", 22, Theme.Ink, TextAnchor.MiddleLeft, 0.03f, 0.88f, 0.65f, 0.97f);
            equipBox = UIFactory.Panel(Root, "EquipBox", Theme.Card, 0.02f, 0.30f, 0.66f, 0.88f, true).rectTransform;

            UIFactory.Label(Root, "GOD'S BLESSING  (3)", 22, Theme.Ink, TextAnchor.MiddleLeft, 0.69f, 0.88f, 0.98f, 0.97f);
            godBox = UIFactory.Panel(Root, "GodBox", Theme.Card, 0.68f, 0.30f, 0.98f, 0.88f, true).rectTransform;

            detail = UIFactory.Panel(Root, "Detail", Theme.Card, 0.02f, 0.03f, 0.74f, 0.27f, true).rectTransform;

            // Tombol uji sampai relic sungguhan didapat dari node.
            UIFactory.Btn(Root, "TestEquip", "+ Test Equipment", Theme.Card, Theme.Ink, 0.76f, 0.15f, 0.98f, 0.26f, AddTestEquipment, 16);
            UIFactory.Btn(Root, "TestGod", "+ Test Blessing", Theme.Card, Theme.Ink, 0.76f, 0.03f, 0.98f, 0.14f, AddTestBlessing, 16);
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(equipBox);
            UIFactory.ClearChildren(godBox);

            // 7 slot: 4 di baris atas, 3 di baris bawah
            for (int i = 0; i < DungeonState.EquipSlots; i++)
            {
                int row = i / 4, col = i % 4;
                float x0 = 0.03f + col * 0.24f, x1 = x0 + 0.21f;
                float y1 = 0.96f - row * 0.48f, y0 = y1 - 0.44f;
                Slot(equipBox, DungeonState.Equipment[i], x0, y0, x1, y1);
            }
            // 3 slot dewa, bertumpuk
            for (int i = 0; i < DungeonState.GodSlots; i++)
            {
                float y1 = 0.97f - i * 0.325f, y0 = y1 - 0.30f;
                Slot(godBox, DungeonState.God[i], 0.06f, y0, 0.94f, y1);
            }
            ShowDetail();
        }

        void Slot(RectTransform parent, Relic r, float x0, float y0, float x1, float y1)
        {
            Relic relic = r;
            var b = UIFactory.Btn(parent, "Slot", relic != null ? relic.Name : "Empty",
                                  relic != null ? Theme.Gold : new Color(1f, 1f, 1f, 0.4f), Theme.Ink, x0, y0, x1, y1,
                                  () => { selected = relic; ShowDetail(); }, 16);
            if (relic == null) b.interactable = true;
        }

        void ShowDetail()
        {
            UIFactory.ClearChildren(detail);
            if (selected == null)
            {
                UIFactory.Label(detail, "Pilih relic untuk melihat detailnya.", 18, Theme.Line, TextAnchor.MiddleLeft, 0.03f, 0.1f, 0.97f, 0.9f);
                return;
            }
            UIFactory.Label(detail, selected.Name + (selected.IsGod ? "   (Blessing)" : "   (Equipment)"), 22, Theme.Ink,
                            TextAnchor.UpperLeft, 0.03f, 0.58f, 0.97f, 0.95f);
            UIFactory.Label(detail, selected.Description, 18, Theme.Ink, TextAnchor.UpperLeft, 0.03f, 0.08f, 0.97f, 0.58f);
        }

        int testCount;

        void AddTestEquipment()
        {
            testCount++;
            var r = new Relic { Name = "Test Relic " + testCount, Description = "Relic uji #" + testCount + ". Efek asli menyusul dari data Pack." };
            if (!DungeonState.AddRelic(r)) Host.Toast("Slot equipment penuh (7).");
            Refresh();
        }

        void AddTestBlessing()
        {
            testCount++;
            var r = new Relic { Name = "Test Blessing " + testCount, Description = "Blessing uji #" + testCount + ".", IsGod = true };
            if (!DungeonState.AddRelic(r)) Host.Toast("Slot Blessing penuh (3).");
            Refresh();
        }
    }
}
