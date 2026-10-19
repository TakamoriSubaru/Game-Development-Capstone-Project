using UnityEngine;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // First Screen: pilih 1 dari 3 Map (Pack) yang ditawarkan acak di Floor ini.
    public class SelectMapView : View
    {
        RectTransform cards;
        UnityEngine.UI.Text title;

        protected override void Build()
        {
            cards = UIFactory.NewRect(Root, "Cards");
            UIFactory.Stretch(cards, 0f, 0f, 1f, 1f);
            title = UIFactory.Label(Root, "", 40, Theme.Ink, TextAnchor.MiddleCenter, 0.2f, 0.02f, 0.8f, 0.13f);
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(cards);
            title.text = "FLOOR " + DungeonState.Floor + "  -  SELECT MAP";
            var offered = DungeonState.Offered;
            for (int i = 0; i < offered.Count; i++)
            {
                int index = i;
                var p = offered[i];
                float x0 = 0.08f + i * 0.29f, x1 = x0 + 0.26f;
                var btn = UIFactory.Btn(cards, "Map" + (i + 1), "", NodeInfo.EnvColor(p.Env), Theme.Ink, x0, 0.17f, x1, 0.93f,
                                        () => Host.OnMapChosen(index), 20);
                var t = btn.transform;
                UIFactory.Label(t, "MAP " + (i + 1), 22, Theme.Ink, TextAnchor.MiddleCenter, 0.05f, 0.88f, 0.95f, 0.97f);
                UIFactory.Label(t, p.Name, 34, Theme.Ink, TextAnchor.MiddleCenter, 0.05f, 0.62f, 0.95f, 0.86f);
                UIFactory.Label(t, NodeInfo.EnvName(p.Env), 22, Theme.Ink, TextAnchor.MiddleCenter, 0.05f, 0.52f, 0.95f, 0.62f);
                UIFactory.Label(t, "Boss: " + p.Boss, 22, Theme.Ink, TextAnchor.MiddleCenter, 0.05f, 0.36f, 0.95f, 0.48f);
                UIFactory.Label(t, "Theme: " + p.Theme, 20, Theme.Ink, TextAnchor.MiddleCenter, 0.05f, 0.26f, 0.95f, 0.36f);
                if (p.GodRealmChance > 0f)
                    UIFactory.Label(t, p.GodRealmChance >= 1f ? "God Realm: guaranteed" : "God Realm: possible", 18, Theme.Ink,
                                    TextAnchor.MiddleCenter, 0.05f, 0.12f, 0.95f, 0.22f);
            }
        }
    }
}
