using System;
using System.Collections.Generic;

namespace Oddyseus.Core
{
    public enum ShopKind { Food, Boon, Blessing }

    public class ShopItem
    {
        public ShopKind Kind;
        public string Name;
        public string Description;
        public int Price;
        public int FoodAmount;      // hanya Food
        public Relic Relic;         // hanya Boon dan Blessing
        public bool Sold;
    }

    // Isi Shop di Rest Stop (GDD bagian 3 dan 6.2): Food, Boon/equipment, dan Blessing yang dibeli dengan Gold.
    // Harga Blessing dari BlessingCatalog (GDD 6.2). Harga Food dan daftar Boon = SAMPEL sampai AI konten mengisi.
    public static class ShopLogic
    {
        public const int FoodPrice = 15;           // ASUMSI: harga 1 Food
        public const int BoonCount = 3;
        public const int BlessingCount = 2;

        class BoonDef { public string Name, Desc; public int Price; }

        // SAMPEL: ganti atau tambah di sini (hanya teks; efek di battle dikerjakan terpisah).
        static readonly BoonDef[] BoonPool =
        {
            new BoonDef { Name = "Bronze Greaves", Desc = "+DEF for the whole run.", Price = 60 },
            new BoonDef { Name = "Sailor's Knife", Desc = "+ATK for the whole run.", Price = 60 },
            new BoonDef { Name = "Oiled Oars", Desc = "+SPD for the whole run.", Price = 55 },
            new BoonDef { Name = "Salt Charm", Desc = "Heal a little after each battle.", Price = 70 },
            new BoonDef { Name = "Lucky Coin", Desc = "+Gold from battles.", Price = 65 },
            new BoonDef { Name = "Tarred Cloak", Desc = "Take less damage from Bleed.", Price = 50 },
        };

        // Stok tetap untuk seed yang sama: 1 Food, 3 Food, 3 Boon, 2 Blessing.
        public static List<ShopItem> Generate(int seed)
        {
            var rng = new Random(seed);
            var items = new List<ShopItem>();
            items.Add(new ShopItem { Kind = ShopKind.Food, Name = "Food x1", Description = "Feeds the crew for one battle.", FoodAmount = 1, Price = FoodPrice });
            items.Add(new ShopItem { Kind = ShopKind.Food, Name = "Food x3", Description = "A full cask of supplies.", FoodAmount = 3, Price = FoodPrice * 3 - 5 });

            var boons = new List<BoonDef>(BoonPool);
            for (int i = 0; i < BoonCount && boons.Count > 0; i++)
            {
                int k = rng.Next(boons.Count);
                var b = boons[k]; boons.RemoveAt(k);
                items.Add(new ShopItem
                {
                    Kind = ShopKind.Boon, Name = b.Name, Description = b.Desc, Price = b.Price,
                    Relic = new Relic { Name = b.Name, Description = b.Desc, IsGod = false }
                });
            }

            var gods = new List<BlessingInfo>(BlessingCatalog.All);
            for (int i = 0; i < BlessingCount && gods.Count > 0; i++)
            {
                int k = rng.Next(gods.Count);
                var g = gods[k]; gods.RemoveAt(k);
                items.Add(new ShopItem
                {
                    Kind = ShopKind.Blessing, Name = g.God, Description = g.Effect + " (" + g.Target + ")", Price = g.Gold,
                    Relic = new Relic { Name = g.God, Description = g.Effect + " (" + g.Target + ")", IsGod = true }
                });
            }
            return items;
        }

        // Beli satu barang. Mengembalikan true kalau berhasil; message selalu berisi teks untuk pemain.
        public static bool Buy(ShopItem item, out string message)
        {
            if (item == null) { message = "Nothing to buy."; return false; }
            if (item.Sold) { message = "Already sold."; return false; }
            if (RunState.Gold < item.Price) { message = "Not enough Gold (" + item.Price + " needed)."; return false; }

            if (item.Kind == ShopKind.Food)
            {
                if (RunState.Food >= DungeonState.FoodCap) { message = "Food is already full (" + DungeonState.FoodCap + ")."; return false; }
                int room = DungeonState.FoodCap - RunState.Food;
                RunState.Food += Math.Min(room, item.FoodAmount);
                RunState.Gold -= item.Price;
                item.Sold = true;
                message = "Bought " + item.Name + (room < item.FoodAmount ? " (capped at " + DungeonState.FoodCap + ")." : ".");
                return true;
            }

            if (!DungeonState.AddRelic(item.Relic)) { message = "Cannot carry this."; return false; }
            RunState.Gold -= item.Price;
            item.Sold = true;
            message = "Bought " + item.Name + ".";
            return true;
        }
    }
}
