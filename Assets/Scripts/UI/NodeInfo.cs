using Oddyseus.Core;
using UnityEngine;

namespace Oddyseus.UI
{
    // Teks dan simbol tiap jenis node. Teks diambil dari wireframe Map Select.
    public static class NodeInfo
    {
        public static readonly NodeType[] LegendOrder =
        {
            NodeType.Random, NodeType.Battle, NodeType.Event, NodeType.Shop,
            NodeType.Rest, NodeType.Elite, NodeType.Resource, NodeType.Boss, NodeType.GodRealm
        };

        // Simbol sementara (huruf) sampai ikon dari Artist siap.
        public static string Glyph(NodeType t)
        {
            switch (t)
            {
                case NodeType.Battle: return "X";
                case NodeType.Random: return "?";
                case NodeType.Event: return "!";
                case NodeType.Shop: return "$";
                case NodeType.Rest: return "Zz";
                case NodeType.Elite: return "E";
                case NodeType.Resource: return "Rs";
                case NodeType.Boss: return "BOSS";
                case NodeType.GodRealm: return "G";
            }
            return "?";
        }

        public static string Name(NodeType t)
        {
            switch (t)
            {
                case NodeType.GodRealm: return "God Realm";
                case NodeType.Random: return "Random Node";
                default: return t + " Node";
            }
        }

        public static string Desc(NodeType t)
        {
            switch (t)
            {
                case NodeType.Random: return "Becomes one of 5 node types when entered: Battle, Event, Shop, Elite, or Resource. Never a Boss.";
                case NodeType.Battle: return "Battle normal enemies. Gain Gold and some Food.";
                case NodeType.Event: return "A chance to battle, gain Food, equipment, Stamina, or even debuffs.";
                case NodeType.Shop: return "Exchange your Gold for equipment, Food, and Stamina.";
                case NodeType.Rest: return "Rest to recover Stamina.";
                case NodeType.Elite: return "Battle harder enemies. Gain Gold and equipment.";
                case NodeType.Resource: return "Gain Food (and sometimes Men), often at a cost.";
                case NodeType.Boss: return "Battle the Boss. Gain Gold and the Boss's equipment.";
                case NodeType.GodRealm: return "Exchange your Men and Stamina for a god's Blessing and equipment.";
            }
            return "";
        }

        public static string EnvName(BattleEnvironment e)
        {
            switch (e)
            {
                case BattleEnvironment.OpenSea: return "Open Sea";
                case BattleEnvironment.CoastalRaid: return "Coastal Raid";
                case BattleEnvironment.CaveLair: return "Cave Lair";
                case BattleEnvironment.Underworld: return "Underworld";
                default: return "Open Wilds";
            }
        }

        public static Color EnvColor(BattleEnvironment e)
        {
            switch (e)
            {
                case BattleEnvironment.OpenSea: return Theme.Hex("#7FB3E8");
                case BattleEnvironment.CoastalRaid: return Theme.Hex("#E8A86B");
                case BattleEnvironment.CaveLair: return Theme.Hex("#A88B6B");
                case BattleEnvironment.Underworld: return Theme.Hex("#9A93B8");
                default: return Theme.Hex("#9CC88A");
            }
        }
    }
}
