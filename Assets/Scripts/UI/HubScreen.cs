using UnityEngine;

namespace Oddyseus.UI
{
    // Dasar layar turunan Hub (Legends, Compendium, Armory, Achievements): latar, judul, tombol Back.
    // Isi dibangun ulang tiap Refresh() lewat Populate().
    public abstract class HubScreen : View
    {
        protected RectTransform Body;

        protected abstract string Title { get; }
        protected abstract void Populate(RectTransform body);

        protected override void Build()
        {
            HubUI.Flat(Root, "Bg", HubTheme.Night, 0f, 0f, 1f, 1f, true);
            HubUI.Flat(Root, "TopBand", HubTheme.Deep, 0f, 0.92f, 1f, 1f);
            HubUI.Flat(Root, "TopLine", HubTheme.Bronze, 0f, 0.916f, 1f, 0.92f);
            UIFactory.Label(Root, Title, 28, HubTheme.Marble, TextAnchor.MiddleCenter, 0.30f, 0.93f, 0.70f, 0.99f);
            HubUI.Btn(Root, "Back", "< BACK", null, HubTheme.Panel, HubTheme.Marble,
                      0.015f, 0.93f, 0.10f, 0.99f, () => Host.CloseSub(this), 20);

            Body = UIFactory.NewRect(Root, "Body");
            UIFactory.Stretch(Body, 0f, 0f, 1f, 0.915f);
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(Body);
            Populate(Body);
        }
    }
}