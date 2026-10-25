using UnityEngine;
using UnityEngine.UI;

namespace Oddyseus.UI
{
    // Palet gelap "laut malam + perunggu" untuk Hub dan layar turunannya.
    public static class HubTheme
    {
        public static readonly Color Night = Theme.Hex("#0B1B2B");
        public static readonly Color Deep = Theme.Hex("#12293D");
        public static readonly Color Panel = Theme.Hex("#17344D");
        public static readonly Color Bronze = Theme.Hex("#B8863B");
        public static readonly Color Gold = Theme.Hex("#E8C872");
        public static readonly Color Marble = Theme.Hex("#E9E4D4");
        public static readonly Color Teal = Theme.Hex("#2A9D8F");
        public static readonly Color Danger = Theme.Hex("#B23A3A");
        public static readonly Color Muted = Theme.Hex("#8FA6BA");

        // Garis tepi berwarna pada sebuah panel.
        public static void Frame(Image img, Color c)
        {
            var o = img.gameObject.AddComponent<Outline>();
            o.effectColor = c;
            o.effectDistance = new Vector2(2f, -2f);
        }
    }
}