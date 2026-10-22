using System;
using System.Collections.Generic;

namespace Oddyseus.Core
{
    public class MapNode
    {
        public int Id;
        public int Layer;            // kolom dari kiri (0 = awal)
        public int Row;              // baris dalam kolom
        public int RowCount;         // jumlah node di kolom ini
        public NodeType Type;        // Random = tampil "?" sampai dimasuki
        public bool WasRandom;       // true jika node ini tadinya "?" lalu terbuka
        public bool Visited;
        public List<int> Next = new List<int>();
        public List<int> Prev = new List<int>();
    }

    // Satu kolom peta: ukuran acak antara MinSize dan MaxSize, tipe tetap atau Random.
    public class LayerSpec
    {
        public int MinSize, MaxSize;
        public NodeType Type;
        public bool ShopOrRest;      // jika true: tiap node jadi Shop atau Rest (acak)

        public LayerSpec(int min, int max, NodeType type, bool shopOrRest = false)
        {
            MinSize = min; MaxSize = max; Type = type; ShopOrRest = shopOrRest;
        }
    }

    // Peta gaya Slay the Spire: node dan jalur acak, semua jalur bertemu di satu node akhir.
    // Tanpa UnityEngine supaya bisa dites.
    public class MapGraph
    {
        public readonly List<MapNode> Nodes = new List<MapNode>();
        public readonly List<List<int>> Layers = new List<List<int>>();
        public int LayerCount { get { return Layers.Count; } }
        public int EndId { get { return Layers[Layers.Count - 1][0]; } }

        // Satu peta gabungan, 7 kolom: Act 1 (kolom 0-3) dan Act 2 (kolom 4-6) menyambung lewat satu
        // node Shop/Rest di tengah (HubLayer), lalu berakhir di Boss.
        public const int HubLayer = 3;

        public static MapGraph Combined(int seed, float godRealmChance)
        {
            return Generate(seed, new[]
            {
                new LayerSpec(1, 1, NodeType.Battle),
                new LayerSpec(2, 3, NodeType.Random),
                new LayerSpec(2, 3, NodeType.Random),
                new LayerSpec(1, 1, NodeType.Shop, true),   // batas Act 1 dan Act 2
                new LayerSpec(2, 3, NodeType.Random),
                new LayerSpec(2, 3, NodeType.Random),
                new LayerSpec(1, 1, NodeType.Boss),
            }, godRealmChance);
        }

        // Act dari sebuah kolom: kolom 0..HubLayer = Act 1, sisanya Act 2.
        public static int ActOfLayer(int layer) { return layer <= HubLayer ? 1 : 2; }

        public static MapGraph Generate(int seed, LayerSpec[] specs, float godRealmChance)
        {
            var rng = new Random(seed);
            var g = new MapGraph();

            for (int l = 0; l < specs.Length; l++)
            {
                var s = specs[l];
                int size = s.MinSize + rng.Next(s.MaxSize - s.MinSize + 1);
                var ids = new List<int>();
                for (int r = 0; r < size; r++)
                {
                    var n = new MapNode { Id = g.Nodes.Count, Layer = l, Row = r, RowCount = size, Type = s.Type };
                    if (s.ShopOrRest) n.Type = rng.Next(2) == 0 ? NodeType.Shop : NodeType.Rest;
                    g.Nodes.Add(n);
                    ids.Add(n.Id);
                }
                g.Layers.Add(ids);
            }

            for (int l = 0; l + 1 < g.Layers.Count; l++) g.Connect(rng, g.Layers[l], g.Layers[l + 1]);

            // God Realm menggantikan satu node Random (bukan di kolom pertama atau terakhir).
            if (rng.NextDouble() < godRealmChance)
            {
                var candidates = new List<MapNode>();
                for (int i = 0; i < g.Nodes.Count; i++)
                    if (g.Nodes[i].Type == NodeType.Random && g.Nodes[i].Layer > 0 && g.Nodes[i].Layer < g.LayerCount - 1)
                        candidates.Add(g.Nodes[i]);
                if (candidates.Count > 0) candidates[rng.Next(candidates.Count)].Type = NodeType.GodRealm;
            }
            return g;
        }

        // Sambungan "tangga": setiap node di kedua kolom pasti punya jalur, dan garis tidak saling silang.
        void Connect(Random rng, List<int> a, List<int> b)
        {
            int i = 0, j = 0;
            Link(a[i], b[j]);
            while (i < a.Count - 1 || j < b.Count - 1)
            {
                bool canI = i < a.Count - 1, canJ = j < b.Count - 1;
                bool advanceI;
                if (canI && canJ)
                {
                    float pi = (float)i / (a.Count - 1), pj = (float)j / (b.Count - 1);
                    advanceI = pi < pj ? true : (pi > pj ? false : rng.Next(2) == 0);
                    if (rng.NextDouble() < 0.25) advanceI = !advanceI;
                }
                else advanceI = canI;
                if (advanceI) i++; else j++;
                Link(a[i], b[j]);
            }
        }

        void Link(int from, int to)
        {
            if (Nodes[from].Next.Contains(to)) return;
            Nodes[from].Next.Add(to);
            Nodes[to].Prev.Add(from);
        }

        // Dipakai tes: semua node bisa dicapai dari kolom awal dan semua jalur berakhir di node akhir.
        public bool IsValid(out string why)
        {
            why = "";
            if (Layers[Layers.Count - 1].Count != 1) { why = "node akhir tidak tunggal"; return false; }
            for (int i = 0; i < Nodes.Count; i++)
            {
                var n = Nodes[i];
                if (n.Layer > 0 && n.Prev.Count == 0) { why = "node " + n.Id + " tanpa jalur masuk"; return false; }
                if (n.Layer < LayerCount - 1 && n.Next.Count == 0) { why = "node " + n.Id + " buntu"; return false; }
                for (int k = 0; k < n.Next.Count; k++)
                    if (Nodes[n.Next[k]].Layer != n.Layer + 1) { why = "jalur melompat kolom"; return false; }
            }
            return true;
        }
    }
}
