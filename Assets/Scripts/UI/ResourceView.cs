using UnityEngine;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Tab Resource: tempat menyimpan data Men dan Food (dan Gold). Susunannya mirip tab Equipment.
    public class ResourceView : View
    {
        RectTransform box;

        protected override void Build()
        {
            UIFactory.Label(Root, "RESOURCE", 22, Theme.Ink, TextAnchor.MiddleLeft, 0.03f, 0.88f, 0.65f, 0.97f);
            box = UIFactory.Panel(Root, "Box", Theme.Card, 0.02f, 0.30f, 0.98f, 0.88f, true).rectTransform;

            var notes = UIFactory.Panel(Root, "Notes", Theme.Card, 0.02f, 0.03f, 0.74f, 0.27f, true);
            UIFactory.Label(notes.transform,
                "Men: nyawa run. Habis = run kalah. Tiap karakter gugur mengurangi Men.\n" +
                "Food: dipakai tiap Battle. Tanpa Food, party kehilangan 10% max HP. Dengan Food, party pulih 5% HP setelah menang.",
                18, Theme.Ink, TextAnchor.UpperLeft, 0.03f, 0.08f, 0.97f, 0.92f);

            // Tombol uji sampai Food dan Men berubah lewat node.
            UIFactory.Btn(Root, "TestFood", "+1 Food (test)", Theme.Card, Theme.Ink, 0.76f, 0.15f, 0.98f, 0.26f,
                () => { RunState.Food = Mathf.Min(DungeonState.FoodCap, RunState.Food + 1); Refresh(); }, 16);
            UIFactory.Btn(Root, "TestMen", "-1 Men (test)", Theme.Card, Theme.Ink, 0.76f, 0.03f, 0.98f, 0.14f,
                () => { RunState.Men = Mathf.Max(0, RunState.Men - 1); Refresh(); }, 16);
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(box);
            Stat(0, "MEN", RunState.Men + " / " + RunState.StartMen, (float)RunState.Men / RunState.StartMen, Theme.Good);
            Stat(1, "FOOD", RunState.Food + " / " + DungeonState.FoodCap, (float)RunState.Food / DungeonState.FoodCap, Theme.Highlight);
            Stat(2, "GOLD", RunState.Gold.ToString(), 0f, Theme.Slate);
        }

        void Stat(int i, string name, string value, float fraction, Color barColor)
        {
            float x0 = 0.02f + i * 0.33f, x1 = x0 + 0.30f;
            var card = UIFactory.Panel(box, name, Color.white, x0, 0.06f, x1, 0.94f, true);
            UIFactory.Label(card.transform, name, 24, Theme.Line, TextAnchor.MiddleCenter, 0.05f, 0.76f, 0.95f, 0.95f);
            UIFactory.Label(card.transform, value, 44, Theme.Ink, TextAnchor.MiddleCenter, 0.05f, 0.38f, 0.95f, 0.74f);
            if (fraction > 0f || name != "GOLD")
                UIFactory.Bar(card.transform, fraction, barColor, 0.1f, 0.18f, 0.9f, 0.28f);
        }
    }
}
