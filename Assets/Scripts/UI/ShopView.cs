using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Layar Shop di Rest Stop. Dibuka lewat DungeonController.OpenShop. Tidak menyentuh BattleView.
    public class ShopView : View
    {
        const int Columns = 4;

        List<ShopItem> items = new List<ShopItem>();
        Action<bool> closed;
        bool boughtAny;
        RectTransform grid;
        Text header;

        protected override void Build()
        {
            HubUI.Flat(Root, "Bg", HubTheme.Night, 0f, 0f, 1f, 1f, true);
            header = UIFactory.Label(Root, "", 26, HubTheme.Marble, TextAnchor.MiddleLeft, 0.03f, 0.90f, 0.70f, 0.99f);
            grid = UIFactory.NewRect(Root, "Grid");
            UIFactory.Stretch(grid, 0.03f, 0.14f, 0.97f, 0.88f);
            HubUI.Btn(Root, "Leave", "LEAVE", null, HubTheme.Gold, HubTheme.Night, 0.74f, 0.03f, 0.97f, 0.12f, OnLeave, 22);
            UIFactory.Label(Root, "Buying here uses this Rest Stop: you cannot Rest afterwards.", 14, HubTheme.Muted,
                            TextAnchor.MiddleLeft, 0.03f, 0.03f, 0.72f, 0.12f);
        }

        // Dipanggil DungeonController.OpenShop. closed(true) kalau ada yang dibeli.
        public void Open(int seed, Action<bool> onClosed)
        {
            items = ShopLogic.Generate(seed);
            closed = onClosed;
            boughtAny = false;
            Root.SetAsLastSibling();
            Show(true);
        }

        public override void Refresh()
        {
            header.text = "SHOP     Gold: " + RunState.Gold + "     Food: " + RunState.Food + "/" + DungeonState.FoodCap;
            UIFactory.ClearChildren(grid);

            const float gap = 0.02f;
            float w = (1f - gap * (Columns - 1)) / Columns;
            for (int i = 0; i < items.Count; i++)
            {
                int col = i % Columns, row = i / Columns;
                float x0 = col * (w + gap), x1 = x0 + w;
                float y1 = 1f - row * 0.505f, y0 = y1 - 0.485f;
                DrawItem(items[i], x0, y0, x1, y1);
            }
        }

        void DrawItem(ShopItem it, float x0, float y0, float x1, float y1)
        {
            var card = HubUI.Card(grid, "Item", it.Sold ? HubTheme.Night : HubTheme.Deep, x0, y0, x1, y1,
                                  it.Sold ? HubTheme.Muted : HubTheme.Bronze);
            Transform t = card.transform;
            Color accent = it.Kind == ShopKind.Blessing ? HubTheme.Gold : (it.Kind == ShopKind.Food ? HubTheme.Teal : HubTheme.Marble);
            UIFactory.Label(t, it.Kind.ToString().ToUpper(), 12, accent, TextAnchor.MiddleLeft, 0.06f, 0.88f, 0.94f, 0.98f);
            UIFactory.Label(t, it.Name, 18, it.Sold ? HubTheme.Muted : HubTheme.Marble, TextAnchor.MiddleLeft, 0.06f, 0.70f, 0.94f, 0.88f);
            UIFactory.Label(t, it.Description, 14, HubTheme.Muted, TextAnchor.UpperLeft, 0.06f, 0.30f, 0.94f, 0.70f);

            bool afford = RunState.Gold >= it.Price;
            string label = it.Sold ? "SOLD" : "BUY  " + it.Price;
            Color fill = it.Sold ? HubTheme.Night : (afford ? HubTheme.Gold : HubTheme.Panel);
            Color text = it.Sold ? HubTheme.Muted : (afford ? HubTheme.Night : HubTheme.Muted);
            ShopItem captured = it;
            HubUI.Btn(t, "Buy", label, null, fill, text, 0.08f, 0.04f, 0.92f, 0.26f, () => OnBuy(captured), 16);
        }

        void OnBuy(ShopItem it)
        {
            string msg;
            bool ok = ShopLogic.Buy(it, out msg);
            if (ok) boughtAny = true;
            Host.Toast(msg);
            Host.RefreshBars();
            Refresh();
        }

        void OnLeave()
        {
            Show(false);
            var cb = closed;
            closed = null;
            if (cb != null) cb(boughtAny);
        }
    }
}
