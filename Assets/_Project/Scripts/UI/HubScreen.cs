using UnityEngine;

namespace Oddyseus.UI
{
    // Dasar layar turunan Hub (Legends, Compendium, Armory, Achievements): latar parchment + vignette,
    // judul + BACK, dan area isi 16:9 (Body). Isi dibangun ulang tiap Refresh() lewat Populate().
    // Koordinat di Populate = pecahan layar 16:9 (0..1); pita judul ada di y 0.87 ke atas.
    public abstract class HubScreen : View
    {
        protected RectTransform Body;

        protected abstract string Title { get; }
        protected abstract void Populate(RectTransform body);

        protected virtual bool RedStrip { get { return false; } }

        protected override void Build()
        {
            ParchUI.Background(Root, true, true, true, false);
            ParchUI.Solid(Root, "Block", new Color(0f, 0f, 0f, 0f), 0f, 0f, 1f, 1f).raycastTarget = true;

            Body = ParchUI.SafeArea(Root);
            ParchUI.TitleBar(Body, Title, () => Host.CloseSub(this), RedStrip);

            var content = UIFactory.NewRect(Body, "Content");
            UIFactory.Stretch(content, 0f, 0f, 1f, 1f);
            Body = content;
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(Body);
            Populate(Body);
        }
    }
}