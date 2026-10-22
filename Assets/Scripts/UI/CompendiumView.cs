using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Riset tiap Pack: ??? sebelum pernah ditawarkan, nama dan tema setelah terlihat, boss dan equipment setelah boss kalah.
    public class CompendiumView : HubScreen
    {
        int floor = 1;

        protected override string Title { get { return "PACK COMPENDIUM"; } }

        protected override void Populate(RectTransform body)
        {
            for (int f = 1; f <= DungeonState.Floors; f++)
            {
                int ff = f;
                bool on = f == floor;
                float x0 = 0.03f + (f - 1) * 0.125f;
                HubUI.Btn(body, "Floor" + f, "FLOOR " + f, null, on ? HubTheme.Gold : HubTheme.Panel,
                          on ? HubTheme.Night : HubTheme.Marble, x0, 0.90f, x0 + 0.115f, 0.98f,
                          () => { floor = ff; Refresh(); }, 20);
            }
            UIFactory.Label(body, "Discovered " + MetaProgress.PackSeenCount + "/" + PackCatalog.All.Length +
                            "   Cleared " + MetaProgress.PackClearedCount + "/" + PackCatalog.All.Length,
                            18, HubTheme.Gold, TextAnchor.MiddleRight, 0.55f, 0.90f, 0.97f, 0.98f);

            var packs = PackCatalog.ForFloor(floor);
            for (int i = 0; i < packs.Count; i++) DrawPack(body, i, packs[i]);
        }

        void DrawPack(RectTransform body, int i, PackInfo p)
        {
            bool seen = MetaProgress.IsPackSeen(p.Id);
            bool cleared = MetaProgress.IsPackCleared(p.Id);
            float w = 0.17f, gap = 0.0125f;
            float x0 = 0.05f + i * (w + gap);
            var card = HubUI.Card(body, "Pack" + p.Id, seen ? HubTheme.Panel : HubTheme.Deep, x0, 0.05f, x0 + w, 0.86f,
                                  cleared ? HubTheme.Gold : HubTheme.Bronze);
            var t = card.transform;

            if (seen) HubUI.Flat(t, "EnvBar", NodeInfo.EnvColor(p.Env), 0.04f, 0.975f, 0.96f, 0.995f);
            UIFactory.Label(t, "#" + p.Id.ToString("00"), 14, HubTheme.Muted, TextAnchor.MiddleLeft, 0.06f, 0.88f, 0.50f, 0.97f);

            if (!seen)
            {
                UIFactory.Label(t, "???", 30, HubTheme.Muted, TextAnchor.MiddleCenter, 0.06f, 0.55f, 0.94f, 0.85f);
                UIFactory.Label(t, "Undiscovered.\nAppears in Floor " + p.Floor + " maps.", 14, HubTheme.Muted,
                                TextAnchor.UpperCenter, 0.06f, 0.30f, 0.94f, 0.55f);
                return;
            }

            UIFactory.Label(t, p.Name, 24, HubTheme.Marble, TextAnchor.MiddleLeft, 0.06f, 0.70f, 0.94f, 0.88f);
            UIFactory.Label(t, NodeInfo.EnvName(p.Env) + " / " + p.Theme, 14, HubTheme.Marble, TextAnchor.MiddleLeft, 0.06f, 0.60f, 0.94f, 0.70f);
            if (p.GodRealmChance > 0f)
                UIFactory.Label(t, "God Realm " + Mathf.RoundToInt(p.GodRealmChance * 100f) + "%", 12, HubTheme.Teal,
                                TextAnchor.MiddleLeft, 0.06f, 0.52f, 0.94f, 0.60f);
            HubUI.Flat(t, "Divider", HubTheme.Bronze, 0.06f, 0.505f, 0.94f, 0.51f);

            UIFactory.Label(t, "BOSS", 12, HubTheme.Gold, TextAnchor.MiddleLeft, 0.06f, 0.44f, 0.94f, 0.50f);
            UIFactory.Label(t, cleared ? p.Boss : "???", 20, cleared ? HubTheme.Marble : HubTheme.Muted,
                            TextAnchor.MiddleLeft, 0.06f, 0.32f, 0.94f, 0.44f);

            var eq = EquipmentCatalog.ForPack(p.Id);
            UIFactory.Label(t, "EQUIPMENT", 12, HubTheme.Gold, TextAnchor.MiddleLeft, 0.06f, 0.24f, 0.94f, 0.30f);
            UIFactory.Label(t, cleared && eq != null ? eq.Name : "???", 18, cleared ? HubTheme.Marble : HubTheme.Muted,
                            TextAnchor.MiddleLeft, 0.06f, 0.14f, 0.94f, 0.24f);
            UIFactory.Label(t, cleared && eq != null ? eq.Effect : "Defeat the boss to reveal.", 12, HubTheme.Muted,
                            TextAnchor.UpperLeft, 0.06f, 0.02f, 0.94f, 0.14f);
        }
    }
}