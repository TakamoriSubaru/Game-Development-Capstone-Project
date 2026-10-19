using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Peta node gaya Slay the Spire. Klik node yang terhubung dengan posisimu untuk masuk.
    public class MapView : View
    {
        const float W = 1100f, H = 520f;      // ukuran kanvas peta (piksel referensi)
        RectTransform content;
        Text header;
        GameObject legend;
        float zoom = 1f;

        protected override void Build()
        {
            UIFactory.Panel(Root, "Bg", Theme.Slate, 0f, 0f, 1f, 1f, false);

            var scrollRoot = UIFactory.NewRect(Root, "Scroll");
            UIFactory.Stretch(scrollRoot, 0f, 0f, 1f, 1f);
            var bgImg = scrollRoot.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0f);
            var sr = scrollRoot.gameObject.AddComponent<ScrollRect>();
            var vp = UIFactory.NewRect(scrollRoot, "Viewport");
            UIFactory.Stretch(vp, 0f, 0f, 1f, 1f);
            vp.gameObject.AddComponent<RectMask2D>();

            content = UIFactory.NewRect(vp, "Content");
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(W, H);
            var cImg = content.gameObject.AddComponent<Image>();
            cImg.color = new Color(0f, 0f, 0f, 0f);

            sr.viewport = vp;
            sr.content = content;
            sr.horizontal = true;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            header = UIFactory.Label(Root, "", 24, Theme.Ink, TextAnchor.UpperLeft, 0.02f, 0.90f, 0.6f, 0.99f);
            UIFactory.Btn(Root, "ZoomIn", "Zoom In", Theme.Card, Theme.Ink, 0.80f, 0.02f, 0.89f, 0.09f, () => SetZoom(zoom + 0.25f), 18);
            UIFactory.Btn(Root, "ZoomOut", "Zoom Out", Theme.Card, Theme.Ink, 0.90f, 0.02f, 0.99f, 0.09f, () => SetZoom(zoom - 0.25f), 18);
            UIFactory.Btn(Root, "LegendBtn", "Legend", Theme.Card, Theme.Ink, 0.01f, 0.02f, 0.10f, 0.09f, ToggleLegend, 18);

            BuildLegend();
        }

        void SetZoom(float z)
        {
            zoom = Mathf.Clamp(z, 0.75f, 2f);
            content.localScale = new Vector3(zoom, zoom, 1f);
        }

        void ToggleLegend() { legend.SetActive(!legend.activeSelf); }

        void BuildLegend()
        {
            var box = UIFactory.Panel(Root, "Legend", Theme.Panel, 0.02f, 0.12f, 0.62f, 0.88f, true);
            legend = box.gameObject;
            var order = NodeInfo.LegendOrder;
            for (int i = 0; i < order.Length; i++)
            {
                int col = i % 2, row = i / 2;
                float x0 = 0.02f + col * 0.49f, x1 = x0 + 0.47f;
                float y1 = 0.97f - row * 0.19f, y0 = y1 - 0.17f;
                var cell = UIFactory.Panel(box.transform, "Item", Theme.Card, x0, y0, x1, y1, true);
                UIFactory.Label(cell.transform, NodeInfo.Glyph(order[i]) + "  " + NodeInfo.Name(order[i]), 18, Theme.Ink,
                                TextAnchor.MiddleLeft, 0.03f, 0.58f, 0.97f, 0.96f);
                UIFactory.Label(cell.transform, NodeInfo.Desc(order[i]), 14, Theme.Line, TextAnchor.UpperLeft, 0.03f, 0.04f, 0.97f, 0.58f);
            }
            legend.SetActive(false);
        }

        // Posisi node: kolom merata, baris dibagi rata, ditambah sedikit geser tetap per node supaya terlihat alami.
        Vector2 NodePos(MapGraph g, MapNode n)
        {
            float margin = 70f;
            float x = margin + n.Layer * (W - 2f * margin) / Mathf.Max(1, g.LayerCount - 1);
            float y = H - H * (n.Row + 1) / (n.RowCount + 1);
            bool edge = n.Layer == 0 || n.Layer == g.LayerCount - 1;
            if (!edge)
            {
                x += Hash(n.Id * 2 + 1) * 16f;
                y += Hash(n.Id * 2 + 2) * 18f;
            }
            return new Vector2(x, y);
        }

        static float Hash(int v)
        {
            int h = v * 73856093 ^ 19349663;
            return ((h & 0xFFFF) / 65535f) * 2f - 1f;
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(content);
            legend.SetActive(false);
            var g = DungeonState.Graph;
            if (g == null) return;

            header.text = DungeonState.CurrentPack.Name + "   Floor " + DungeonState.Floor + "  Act " + DungeonState.Act
                          + "   Boss: " + DungeonState.CurrentPack.Boss;
            SetZoom(zoom);

            var reachable = DungeonState.ReachableIds();
            var pos = new Dictionary<int, Vector2>();
            for (int i = 0; i < g.Nodes.Count; i++) pos[g.Nodes[i].Id] = NodePos(g, g.Nodes[i]);

            // jalur dulu (di belakang), lalu node
            for (int i = 0; i < g.Nodes.Count; i++)
            {
                var n = g.Nodes[i];
                for (int k = 0; k < n.Next.Count; k++)
                {
                    var m = g.Nodes[n.Next[k]];
                    bool walked = n.Visited && m.Visited && DungeonState.CurrentNode >= 0;
                    bool active = n.Id == DungeonState.CurrentNode && reachable.Contains(m.Id);
                    Color c = active ? Theme.Highlight : (walked ? Theme.Ink : Theme.Line);
                    UIFactory.Curve(content, pos[n.Id], pos[m.Id], active ? 6f : 3f, c);
                }
            }

            for (int i = 0; i < g.Nodes.Count; i++) DrawNode(g.Nodes[i], pos[g.Nodes[i].Id], reachable);
        }

        void DrawNode(MapNode n, Vector2 p, List<int> reachable)
        {
            bool isBoss = n.Type == NodeType.Boss;
            float size = isBoss ? 78f : 58f;
            bool canEnter = reachable.Contains(n.Id);
            bool current = n.Id == DungeonState.CurrentNode;

            Color ring = canEnter || current ? Theme.Highlight : Theme.Line;
            Color fill = n.Visited ? Theme.Hex("#9AA4B8") : (canEnter ? Color.white : Theme.Hex("#C9D3E6"));

            UIFactory.Circle(content, "Ring", p, size + 8f, ring);
            var inner = UIFactory.Circle(content, "Node" + n.Id, p, size, fill);
            var b = inner.gameObject.AddComponent<Button>();
            b.targetGraphic = inner;
            int id = n.Id;
            b.onClick.AddListener(() => Host.OnNodeClicked(id));

            var glyph = UIFactory.Label(inner.transform, NodeInfo.Glyph(n.Type), isBoss ? 18 : 24,
                                        n.Visited ? Theme.Line : Theme.Ink, TextAnchor.MiddleCenter, 0.08f, 0.08f, 0.92f, 0.92f);
            glyph.fontStyle = FontStyle.Bold;
            if (n.WasRandom)
                UIFactory.Label(inner.transform, "?", 12, Theme.Line, TextAnchor.UpperRight, 0.55f, 0.62f, 0.95f, 0.98f);
            if (current)
                UIFactory.Label(inner.transform, "YOU", 11, Theme.Bad, TextAnchor.LowerCenter, 0.1f, 0.0f, 0.9f, 0.3f);
        }
    }
}
