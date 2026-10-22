using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Peta node gaya Slay the Spire, tema gelap. Klik node yang terhubung dengan posisimu untuk masuk.
    public class MapView : View
    {
        const float Margin = 90f;        // jarak peta ke tepi kanvas
        const float LayerGap = 140f;     // jarak antar layer (horizontal)
        const float LaneGap = 150f;      // jarak antar jalur (vertikal)
        const float MinNodeDist = 100f;  // jarak minimal antar pusat node

        float W = 1300f, H = 680f;       // ukuran kanvas peta, dihitung ulang tiap Refresh
        RectTransform content, viewport;
        ScrollRect scroll;
        GameObject legend;
        float zoom = 1f;
        Vector2 focus = new Vector2(650f, 340f);   // titik di kanvas yang dipusatkan di layar

        protected override void Build()
        {
            HubUI.Flat(Root, "Bg", HubTheme.Night, 0f, 0f, 1f, 1f, true);

            var scrollRoot = UIFactory.NewRect(Root, "Scroll");
            UIFactory.Stretch(scrollRoot, 0f, 0f, 1f, 1f);
            var bgImg = scrollRoot.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0f);
            scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            viewport = UIFactory.NewRect(scrollRoot, "Viewport");
            UIFactory.Stretch(viewport, 0f, 0f, 1f, 1f);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = UIFactory.NewRect(viewport, "Content");
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(W, H);
            var cImg = content.gameObject.AddComponent<Image>();
            cImg.color = new Color(0f, 0f, 0f, 0f);

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Tombol bantu.
            HubUI.Btn(Root, "LegendBtn", "LEGEND", null, HubTheme.Panel, HubTheme.Marble, 0.01f, 0.02f, 0.11f, 0.09f, ToggleLegend, 16);
            HubUI.Btn(Root, "ZoomOut", "-", null, HubTheme.Panel, HubTheme.Marble, 0.90f, 0.02f, 0.95f, 0.09f, () => ZoomBy(-0.15f), 24);
            HubUI.Btn(Root, "ZoomIn", "+", null, HubTheme.Panel, HubTheme.Marble, 0.95f, 0.02f, 0.99f, 0.09f, () => ZoomBy(0.15f), 24);

            BuildLegend();
        }

        void ZoomBy(float d)
        {
            zoom = Mathf.Clamp(zoom + d, 0.5f, 2f);
            content.localScale = new Vector3(zoom, zoom, 1f);
            FocusOn(focus);
        }

        // Pusatkan titik p (koordinat kanvas) di tengah layar, dibatasi agar tidak keluar dari tepi peta.
        void FocusOn(Vector2 p)
        {
            focus = p;
            content.localScale = new Vector3(zoom, zoom, 1f);
            var vr = viewport.rect;
            Vector2 off = (p - new Vector2(W * 0.5f, H * 0.5f)) * zoom;
            float mx = Mathf.Max(0f, (W * zoom - vr.width) * 0.5f);
            float my = Mathf.Max(0f, (H * zoom - vr.height) * 0.5f);
            scroll.StopMovement();
            content.anchoredPosition = new Vector2(Mathf.Clamp(-off.x, -mx, mx), Mathf.Clamp(-off.y, -my, my));
        }

        void ToggleLegend() { legend.SetActive(!legend.activeSelf); }

        void BuildLegend()
        {
            var box = HubUI.Card(Root, "Legend", new Color(HubTheme.Deep.r, HubTheme.Deep.g, HubTheme.Deep.b, 0.97f),
                                 0.02f, 0.12f, 0.62f, 0.95f, HubTheme.Bronze);
            box.raycastTarget = true;
            legend = box.gameObject;
            var order = NodeInfo.LegendOrder;
            for (int i = 0; i < order.Length; i++)
            {
                int col = i % 2, row = i / 2;
                float x0 = 0.02f + col * 0.49f, x1 = x0 + 0.47f;
                float y1 = 0.97f - row * 0.19f, y0 = y1 - 0.17f;
                var cell = HubUI.Card(box.transform, "Item", HubTheme.Panel, x0, y0, x1, y1, HubTheme.Bronze);
                HubUI.Flat(cell.transform, "Dot", NodeColor(order[i]), 0f, 0.10f, 0.03f, 0.90f);
                UIFactory.Label(cell.transform, NodeInfo.Glyph(order[i]) + "  " + NodeInfo.Name(order[i]), 18, HubTheme.Marble,
                                TextAnchor.MiddleLeft, 0.06f, 0.58f, 0.97f, 0.96f);
                UIFactory.Label(cell.transform, NodeInfo.Desc(order[i]), 14, HubTheme.Muted, TextAnchor.UpperLeft,
                                0.06f, 0.04f, 0.97f, 0.58f);
            }
            legend.SetActive(false);
        }

        // Warna dasar tiap jenis node.
        static Color NodeColor(NodeType t)
        {
            if (t == NodeType.Boss) return HubTheme.Danger;
            if (t == NodeType.Elite) return Theme.Hex("#7A3A6B");
            if (t == NodeType.Battle) return Theme.Hex("#9A5A3A");
            if (t == NodeType.Event) return HubTheme.Teal;
            if (t == NodeType.Resource) return Theme.Hex("#5C8A4A");
            if (t == NodeType.Shop) return HubTheme.Gold;
            if (t == NodeType.GodRealm) return Theme.Hex("#D9B84A");
            if (t == NodeType.Random) return Theme.Hex("#6B7690");
            return HubTheme.Bronze;
        }

        static bool IsLight(Color c)
        {
            return (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) > 0.6f;
        }

        // Ukuran kanvas mengikuti jumlah layer dan jalur supaya node tidak berdesakan.
        void LayoutSize(MapGraph g)
        {
            W = Mathf.Max(1300f, 2f * Margin + (g.LayerCount - 1) * LayerGap);
            int rows = 1;
            if (g.LaneCount > 0) rows = g.LaneCount;
            else
                for (int i = 0; i < g.Nodes.Count; i++) rows = Mathf.Max(rows, g.Nodes[i].RowCount);
            H = Mathf.Max(680f, 2f * Margin + (rows - 1) * LaneGap);
            content.sizeDelta = new Vector2(W, H);
        }

        // Posisi node: kolom merata, baris dibagi rata, ditambah sedikit geser tetap per node supaya terlihat alami.
        Vector2 NodePos(MapGraph g, MapNode n)
        {
            float x = Margin + n.Layer * (W - 2f * Margin) / Mathf.Max(1, g.LayerCount - 1);
            float y = H - H * (n.Row + 1) / (n.RowCount + 1);
            if (g.LaneCount > 0)
            {
                // papan jalur tetap: Boss di tengah, node lain sesuai jalurnya
                float lane = n.Layer == g.LayerCount - 1 ? (g.LaneCount - 1) / 2f : n.Lane;
                y = H - Margin - lane * (H - 2f * Margin) / Mathf.Max(1, g.LaneCount - 1);
            }
            bool edge = n.Layer == 0 || n.Layer == g.LayerCount - 1;
            if (!edge)
            {
                x += Hash(n.Id * 2 + 1) * 14f;
                y += Hash(n.Id * 2 + 2) * 16f;
            }
            return new Vector2(x, y);
        }

        // Dorong node yang terlalu dekat supaya tidak saling menumpuk.
        void Separate(MapGraph g, Dictionary<int, Vector2> pos)
        {
            for (int it = 0; it < 14; it++)
            {
                bool moved = false;
                for (int i = 0; i < g.Nodes.Count; i++)
                {
                    for (int j = i + 1; j < g.Nodes.Count; j++)
                    {
                        int a = g.Nodes[i].Id, b = g.Nodes[j].Id;
                        Vector2 d = pos[a] - pos[b];
                        float m = d.magnitude;
                        if (m >= MinNodeDist) continue;
                        if (m < 0.01f) d = new Vector2(0f, 1f); else d /= m;
                        float push = (MinNodeDist - m) * 0.5f + 0.5f;
                        Vector2 off = new Vector2(d.x * 0.6f, d.y) * push;
                        pos[a] += off;
                        pos[b] -= off;
                        moved = true;
                    }
                }
                if (!moved) break;
            }
            for (int i = 0; i < g.Nodes.Count; i++)
            {
                int id = g.Nodes[i].Id;
                Vector2 p = pos[id];
                p.x = Mathf.Clamp(p.x, 50f, W - 50f);
                p.y = Mathf.Clamp(p.y, 50f, H - 50f);
                pos[id] = p;
            }
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

            LayoutSize(g);

            var reachable = DungeonState.ReachableIds();
            var pos = new Dictionary<int, Vector2>();
            for (int i = 0; i < g.Nodes.Count; i++) pos[g.Nodes[i].Id] = NodePos(g, g.Nodes[i]);
            Separate(g, pos);

            // jalur dulu (di belakang), lalu node
            for (int i = 0; i < g.Nodes.Count; i++)
            {
                var n = g.Nodes[i];
                for (int k = 0; k < n.Next.Count; k++)
                {
                    var m = g.Nodes[n.Next[k]];
                    bool walked = n.Visited && m.Visited && DungeonState.CurrentNode >= 0;
                    bool active = n.Id == DungeonState.CurrentNode && reachable.Contains(m.Id);
                    Color c;
                    float w;
                    if (active) { c = HubTheme.Gold; w = 6f; }
                    else if (walked) { c = HubTheme.Bronze; w = 4f; }
                    else { c = new Color(HubTheme.Muted.r, HubTheme.Muted.g, HubTheme.Muted.b, 0.35f); w = 3f; }
                    UIFactory.Curve(content, pos[n.Id], pos[m.Id], w, c);
                }
            }

            for (int i = 0; i < g.Nodes.Count; i++) DrawNode(g.Nodes[i], pos[g.Nodes[i].Id], reachable);

            // Pusatkan layar: ke node yang sedang dipijak, atau ke node-node yang bisa dipilih.
            Canvas.ForceUpdateCanvases();
            Vector2 target;
            if (DungeonState.CurrentNode >= 0 && pos.ContainsKey(DungeonState.CurrentNode))
            {
                target = pos[DungeonState.CurrentNode];
            }
            else
            {
                Vector2 sum = Vector2.zero;
                int cnt = 0;
                for (int i = 0; i < reachable.Count; i++)
                    if (pos.ContainsKey(reachable[i])) { sum += pos[reachable[i]]; cnt++; }
                target = cnt > 0 ? sum / cnt : new Vector2(W * 0.5f, H * 0.5f);
            }
            FocusOn(target);
        }

        void DrawNode(MapNode n, Vector2 p, List<int> reachable)
        {
            bool isBoss = n.Type == NodeType.Boss;
            float size = isBoss ? 78f : 58f;
            bool canEnter = reachable.Contains(n.Id);
            bool current = n.Id == DungeonState.CurrentNode;

            Color baseCol = NodeColor(n.Type);
            Color fill;
            if (n.Visited) fill = Color.Lerp(baseCol, HubTheme.Deep, 0.65f);
            else if (canEnter) fill = baseCol;
            else fill = Color.Lerp(baseCol, HubTheme.Deep, 0.35f);

            // cahaya untuk node yang bisa dimasuk
            if (canEnter)
            {
                var glow = UIFactory.Circle(content, "Glow", p, size + 30f,
                                            new Color(HubTheme.Gold.r, HubTheme.Gold.g, HubTheme.Gold.b, 0.22f));
                glow.raycastTarget = false;
            }

            Color ring = (canEnter || current) ? HubTheme.Gold : HubTheme.Bronze;
            var ringImg = UIFactory.Circle(content, "Ring", p, size + 8f, ring);
            ringImg.raycastTarget = false;

            var inner = UIFactory.Circle(content, "Node" + n.Id, p, size, fill);
            var b = inner.gameObject.AddComponent<Button>();
            b.targetGraphic = inner;
            int id = n.Id;
            b.onClick.AddListener(() => Host.OnNodeClicked(id));

            Color glyphCol = IsLight(fill) ? HubTheme.Night : Color.white;
            if (n.Visited) glyphCol = HubTheme.Muted;
            var glyph = UIFactory.Label(inner.transform, NodeInfo.Glyph(n.Type), isBoss ? 18 : 24,
                                        glyphCol, TextAnchor.MiddleCenter, 0.08f, 0.08f, 0.92f, 0.92f);
            glyph.fontStyle = FontStyle.Bold;
            if (n.WasRandom)
                UIFactory.Label(inner.transform, "?", 12, HubTheme.Gold, TextAnchor.UpperRight, 0.55f, 0.62f, 0.95f, 0.98f);
            if (current)
                UIFactory.Label(inner.transform, "YOU", 11, HubTheme.Gold, TextAnchor.LowerCenter, 0.1f, 0.0f, 0.9f, 0.3f);
        }
    }
}