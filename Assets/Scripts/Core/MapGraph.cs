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
        public int Lane;             // jalur tetap (0..LaneCount-1) di peta gaya Slay the Spire
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
        public int LaneCount;        // >0 = peta bergaya Slay the Spire (posisi vertikal pakai Lane)
        public int LayerCount { get { return Layers.Count; } }
        public int EndId { get { return Layers[Layers.Count - 1][0]; } }

        // Peta gabungan gaya Slay the Spire: papan 6 jalur x 9 kolom, 4 jalur acak berjalan dari kiri ke
        // kanan (maju lurus atau miring satu jalur, tanpa garis bersilang), lalu semuanya bertemu di satu Boss.
        // Kolom HubLayer (batas Act 1 dan Act 2) berisi Shop/Rest saja. Ubah 3 angka di bawah untuk mengatur ukuran.
        public const int Lanes = 6;        // tinggi papan (jumlah jalur)
        public const int Columns = 9;      // kolom sebelum Boss
        public const int Paths = 4;        // jumlah jalur awal
        public const int HubLayer = 4;     // kolom Shop/Rest: Act 1 sampai kolom ini, Act 2 sesudahnya

        public static MapGraph Combined(int seed, float godRealmChance)
        {
            var rng = new Random(seed);
            var g = new MapGraph { LaneCount = Lanes };
            int bossLayer = Columns;
            var cell = new int[bossLayer + 1, Lanes];
            for (int l = 0; l <= bossLayer; l++) for (int x = 0; x < Lanes; x++) cell[l, x] = -1;
            var edges = new HashSet<int>();
            Func<int, int, int, int> key = (l, from, to) => (l * 16 + from) * 16 + to;

            Func<int, int, int> getNode = (l, x) =>
            {
                if (cell[l, x] < 0)
                {
                    var n = new MapNode { Id = g.Nodes.Count, Layer = l, Lane = x, Type = NodeType.Random };
                    if (l == 0) n.Type = NodeType.Battle;
                    else if (l == HubLayer) n.Type = rng.Next(2) == 0 ? NodeType.Shop : NodeType.Rest;
                    g.Nodes.Add(n);
                    cell[l, x] = n.Id;
                }
                return cell[l, x];
            };

            // jalur awal: Paths jalur berbeda, diacak
            var lanes = new List<int>();
            for (int x = 0; x < Lanes; x++) lanes.Add(x);
            for (int i = lanes.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = lanes[i]; lanes[i] = lanes[j]; lanes[j] = t; }

            for (int p = 0; p < Paths; p++)
            {
                int x = lanes[p];
                for (int l = 0; l < bossLayer - 1; l++)
                {
                    var opts = new List<int>();
                    for (int d = -1; d <= 1; d++)
                    {
                        int nx = x + d;
                        if (nx < 0 || nx >= Lanes) continue;
                        if (d == 1 && edges.Contains(key(l, x + 1, x))) continue;   // hindari silang
                        if (d == -1 && edges.Contains(key(l, x - 1, x))) continue;
                        opts.Add(nx);
                    }
                    int pick = opts[rng.Next(opts.Count)];
                    g.Link(getNode(l, x), getNode(l + 1, pick));
                    edges.Add(key(l, x, pick));
                    x = pick;
                }
                // kolom terakhir sebelum Boss
                var boss = new MapNode();
                int bossId = cell[bossLayer, 0];
                if (bossId < 0)
                {
                    boss = new MapNode { Id = g.Nodes.Count, Layer = bossLayer, Lane = Lanes / 2, Type = NodeType.Boss };
                    g.Nodes.Add(boss);
                    cell[bossLayer, 0] = boss.Id;
                    bossId = boss.Id;
                }
                g.Link(getNode(bossLayer - 1, x), bossId);
            }

            for (int l = 0; l <= bossLayer; l++)
            {
                var ids = new List<int>();
                for (int x = 0; x < Lanes; x++) if (cell[l, x] >= 0) ids.Add(cell[l, x]);
                for (int r = 0; r < ids.Count; r++) { g.Nodes[ids[r]].Row = r; g.Nodes[ids[r]].RowCount = ids.Count; }
                g.Layers.Add(ids);
            }

            g.PlaceGodRealm(rng, godRealmChance);
            return g;
        }

        // God Realm menggantikan satu node Random (bukan di kolom pertama, tengah, atau terakhir).
        void PlaceGodRealm(Random rng, float chance)
        {
            if (rng.NextDouble() >= chance) return;
            var candidates = new List<MapNode>();
            for (int i = 0; i < Nodes.Count; i++)
                if (Nodes[i].Type == NodeType.Random && Nodes[i].Layer > 0 && Nodes[i].Layer < LayerCount - 1)
                    candidates.Add(Nodes[i]);
            if (candidates.Count > 0) candidates[rng.Next(candidates.Count)].Type = NodeType.GodRealm;
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

        internal void Link(int from, int to)
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
