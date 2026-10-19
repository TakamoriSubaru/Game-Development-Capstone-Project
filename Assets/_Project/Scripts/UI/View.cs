using UnityEngine;

namespace Oddyseus.UI
{
    // Dasar semua layar di dalam area Dungeon Mode. Tiap layar dibangun sekali lalu di-Refresh saat ditampilkan.
    public abstract class View
    {
        public RectTransform Root;
        protected DungeonController Host;

        public void Init(RectTransform parent, DungeonController host, string name)
        {
            Host = host;
            Root = UIFactory.NewRect(parent, name);
            UIFactory.Stretch(Root, 0f, 0f, 1f, 1f);
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
