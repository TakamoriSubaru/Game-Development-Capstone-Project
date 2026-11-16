using UnityEngine;

namespace Oddyseus.UI
{
    // Dasar semua layar Dungeon Hub. Tiap layar: latar parchment + vignette (memenuhi layar) dan SafeArea 16:9 untuk isi.
    public abstract class Page
    {
        public RectTransform Root;
        protected RectTransform C;          // area isi (16:9)
        protected HubController Host;

        public void Init(RectTransform canvas, HubController host, string name, bool vTopRight, bool vBottomLeft, bool vBottomRight, bool vLeftEdge)
        {
            Host = host;
            Root = UIFactory.NewRect(canvas, name);
            UIFactory.Stretch(Root, 0f, 0f, 1f, 1f);
            Ui.Background(Root, vTopRight, vBottomLeft, vBottomRight, vLeftEdge);
            C = Ui.SafeArea(Root);
            Build();
            Root.gameObject.SetActive(false);
        }

        protected abstract void Build();
        public virtual void Refresh() { }

        public void Show(bool on)
        {
            Root.gameObject.SetActive(on);
            if (on) Refresh();
        }
    }
}
