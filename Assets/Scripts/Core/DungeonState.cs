using System;
using System.Collections.Generic;

namespace Oddyseus.Core
{
    public class Relic
    {
        public string Name;
        public string Description;
        public bool IsGod;          // true = Blessing dewa (hangus setelah beberapa battle), false = equipment (permanen)
        public int BattlesLeft;     // hanya Blessing: sisa battle sebelum hangus
    }

    public enum ActEndResult { NextAct, NextFloor, RunComplete }

    // Status Dungeon Mode: Floor, Act, peta, posisi, party, relic. Static supaya bertahan antar scene.
    // Belum ada save ke disk. Men/Food/Gold/Stamina tetap di RunState.
    public static class DungeonState
    {
        public const int Floors = 4;
        public const int MapChoices = 3;
        public const int EquipSlots = 7;             // dipertahankan agar kode lama tetap compile; BUKAN batas lagi
        public const int GodSlots = 3;               // idem
        public const int BlessingBattles = 3;        // Blessing hangus setelah sekian battle (ubah ke 2 kalau mau lebih singkat)
        public const int PartyMax = RunState.PartySize;
        public const int FoodCap = 12;               // ASUMSI dari doc Mekanik Food dan Men

        public static bool Active;
        public static int Seed;
        public static int Floor = 1;
        // Act dihitung dari posisi di peta gabungan (Act 1 sampai node Shop/Rest tengah, lalu Act 2).
        public static int Act
        {
            get { return Graph != null && CurrentNode >= 0 ? MapGraph.ActOfLayer(Graph.Nodes[CurrentNode].Layer) : 1; }
        }
        public static List<PackInfo> Offered = new List<PackInfo>();
        public static PackInfo CurrentPack;
        public static MapGraph Graph;
        public static int CurrentNode = -1;          // -1 = belum masuk node apa pun
        public static List<int> Party = new List<int>();
        public static List<Relic> Equipment = new List<Relic>();   // tanpa batas
        public static List<Relic> God = new List<Relic>();         // Blessing aktif, tanpa batas, hangus lewat TickBlessings

        public static bool InMap { get { return Graph != null; } }

        public static void NewDungeon(int seed)
        {
            Seed = seed;
            RunState.NewRun();
            Floor = 1;
            Graph = null; CurrentPack = null; CurrentNode = -1;
            Equipment = new List<Relic>();
            God = new List<Relic>();
            Party = new List<int>();
            for (int i = 0; i < RunState.Roster.Count && Party.Count < PartyMax; i++) Party.Add(i);
            OfferMaps();
            Active = true;
        }

        public static void EnsureDungeon()
        {
            if (!Active || Offered.Count == 0 && Graph == null) NewDungeon(Environment.TickCount);
        }

        // 3 dari 5 Pack di Floor ini, acak.
        public static void OfferMaps()
        {
            var pool = PackCatalog.ForFloor(Floor);
            var rng = new Random(Seed + Floor * 101);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var t = pool[i]; pool[i] = pool[j]; pool[j] = t;
            }
            Offered = pool.GetRange(0, Math.Min(MapChoices, pool.Count));
        }

        public static void SelectMap(int index)
        {
            CurrentPack = Offered[index];
            StartAct();
        }

        static void StartAct()
        {
            int seed = Seed * 31 + Floor * 1009 + (CurrentPack != null ? CurrentPack.Id : 0);
            float god = CurrentPack != null ? CurrentPack.GodRealmChance : 0f;
            Graph = MapGraph.Combined(seed, god);
            CurrentNode = -1;
        }

        // Tombol Restart: ulang progres di Map ini dari node pertama.
        public static void RestartMap()
        {
            if (CurrentPack == null) return;
            StartAct();
        }

        // Tombol Reset: ulang Dungeon Mode dari awal.
        public static void ResetDungeon()
        {
            NewDungeon(Environment.TickCount);
        }

        public static List<int> ReachableIds()
        {
            var ids = new List<int>();
            if (Graph == null) return ids;
            if (CurrentNode < 0) ids.AddRange(Graph.Layers[0]);
            else ids.AddRange(Graph.Nodes[CurrentNode].Next);
            return ids;
        }

        public static bool CanEnter(int id)
        {
            return ReachableIds().Contains(id);
        }

        // Masuk node. Node "?" ditentukan jenis aslinya saat ini.
        public static MapNode Enter(int id)
        {
            var node = Graph.Nodes[id];
            if (node.Type == NodeType.Random)
            {
                var rng = new Random(Seed * 17 + id * 131 + Floor * 7);
                int roll = rng.Next(100);
                node.Type = roll < 35 ? NodeType.Battle : roll < 60 ? NodeType.Event
                          : roll < 80 ? NodeType.Resource : roll < 90 ? NodeType.Shop : NodeType.Elite;
                node.WasRandom = true;
            }
            node.Visited = true;
            CurrentNode = id;
            return node;
        }

        public static bool AtActEnd
        {
            get { return Graph != null && CurrentNode >= 0 && Graph.Nodes[CurrentNode].Next.Count == 0; }
        }

        public static ActEndResult FinishAct()
        {
            Floor++;
            Graph = null; CurrentPack = null; CurrentNode = -1;
            if (Floor > Floors) return ActEndResult.RunComplete;
            OfferMaps();
            return ActEndResult.NextFloor;
        }

        // ---------- Party ----------

        // Pasang atau lepas karakter dari party. Mengembalikan pesan kegagalan, atau null jika berhasil.
        public static string TogglePartyMember(int rosterIndex)
        {
            if (Party.Contains(rosterIndex)) { Party.Remove(rosterIndex); return null; }
            var c = RunState.Roster[rosterIndex];
            if (!c.Alive) return c.Data.Name + " sudah gugur.";
            if (Party.Count >= PartyMax) return "Party penuh (maks " + PartyMax + ").";
            Party.Add(rosterIndex);
            return null;
        }

        // Pasang atau lepas skill. Maksimal 4 skill terpasang.
        public static string ToggleEquipSkill(UnitData d, int skillIndex)
        {
            var eq = new List<int>(d.Equipped ?? new int[0]);
            if (eq.Contains(skillIndex)) eq.Remove(skillIndex);
            else
            {
                if (eq.Count >= 4) return "Maksimal 4 skill terpasang. Lepas satu dulu.";
                eq.Add(skillIndex);
            }
            eq.Sort();
            d.Equipped = eq.ToArray();
            return null;
        }

        // ---------- Relic ----------

        // Tanpa batas slot. Blessing otomatis diberi masa berlaku BlessingBattles kalau belum diisi.
        public static bool AddRelic(Relic r)
        {
            if (r == null) return false;
            if (r.IsGod)
            {
                if (r.BattlesLeft <= 0) r.BattlesLeft = BlessingBattles;
                God.Add(r);
            }
            else Equipment.Add(r);
            return true;
        }

        // Panggil setiap selesai satu battle. Mengurangi sisa Blessing dan membuang yang habis.
        // Mengembalikan nama Blessing yang baru hangus.
        public static List<string> TickBlessings()
        {
            var expired = new List<string>();
            for (int i = God.Count - 1; i >= 0; i--)
            {
                God[i].BattlesLeft--;
                if (God[i].BattlesLeft <= 0)
                {
                    expired.Insert(0, God[i].Name);
                    God.RemoveAt(i);
                }
            }
            return expired;
        }
    }
}