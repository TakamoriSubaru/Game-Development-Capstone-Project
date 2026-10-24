using System;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Layar node non-battle: Resource dan Rest Stop. Memakai pop-up DungeonController supaya konsisten dengan layar lain.
    // Tidak menyentuh BattleView atau BattleSystem.
    public static class NodeScreens
    {
        static readonly string[] Stories =
        {
            "A signal fire still burns on the shore, but no ship is in sight. The crew argues over where to search for supplies.",
            "Fresh tracks lead off the beach and into the trees. Something fed here not long ago.",
            "A wrecked ship lies half-sunk in the shallows, its hold still sealed. Nobody knows what is left inside."
        };

        static readonly string[][] Choices =
        {
            new[] { "Beach", "Cliffs", "Cove" },
            new[] { "Tracks", "Stream", "Grove" },
            new[] { "Hold", "Deck", "Cabin" }
        };

        // Node Resource: tiga pilihan, hasilnya tersembunyi sampai dipilih.
        public static void Resource(DungeonController host, int nodeId, Action done)
        {
            int seed = DungeonState.Seed * 7919 + DungeonState.Floor * 101 + nodeId;
            int story = ((seed % Stories.Length) + Stories.Length) % Stories.Length;
            var outcomes = ResourceNode.Assign(seed);

            var actions = new Action[3];
            for (int i = 0; i < 3; i++)
            {
                var outcome = outcomes[i];
                actions[i] = () =>
                {
                    string result = ResourceNode.Apply(outcome);
                    host.ShowModal("Resource Node", result, new[] { "Continue" }, new Action[] { done });
                };
            }
            host.ShowModal("Resource Node", Stories[story] + "\n\nChoose where to search. You cannot tell what each choice will cost.",
                           Choices[story], actions);
        }

        // Rest Stop: Rest memulihkan HP dan Stamina. Shop belum dibuat (D-10), jadi hanya memberi tahu lalu kembali ke pilihan.
        public static void RestStop(DungeonController host, Action done)
        {
            host.ShowModal("Rest Stop",
                "A quiet harbor. The crew can rest or trade.\n\nRest: restore HP and 1 Stamina.\nShop: Food, equipment and Blessings (coming soon).",
                new[] { "Shop", "Rest" },
                new Action[]
                {
                    () => { host.Toast("Shop is not built yet. Choose Rest for now."); RestStop(host, done); },
                    () => host.ShowModal("Rest Stop", Oddyseus.Core.RestStop.Rest(), new[] { "Continue" }, new Action[] { done })
                });
        }
    }
}
