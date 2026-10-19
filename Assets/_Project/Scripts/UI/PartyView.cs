using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Tab Party: susun party 4 orang, lihat karakter, pasang skill (maks 4 dari 8).
    public class PartyView : View
    {
        int viewIndex = -1;      // indeks roster yang sedang dilihat
        int selMove = 0;         // skill yang dipilih (0..7)

        RectTransform partyBox, listContent, center, movesBox, detail;
        Button addBtn, equipBtn;

        protected override void Build()
        {
            var pb = UIFactory.Panel(Root, "PartyBox", Theme.Card, 0.02f, 0.60f, 0.37f, 0.95f, true);
            partyBox = pb.rectTransform;
            UIFactory.Label(Root, "Party", 20, Theme.Ink, TextAnchor.MiddleCenter, 0.02f, 0.945f, 0.37f, 0.995f);

            UIFactory.Label(Root, "Character List", 20, Theme.Ink, TextAnchor.MiddleCenter, 0.02f, 0.545f, 0.37f, 0.60f);
            var lb = UIFactory.Panel(Root, "ListBox", Theme.Card, 0.02f, 0.03f, 0.37f, 0.545f, true);
            UIFactory.Scroll(lb.transform, "ListScroll", 0.02f, 0.02f, 0.98f, 0.98f, out listContent);

            var c = UIFactory.NewRect(Root, "Center");
            UIFactory.Stretch(c, 0.38f, 0.30f, 0.70f, 0.99f);
            center = c;
            UIFactory.Btn(Root, "Traits", "Traits", Theme.Card, Theme.Ink, 0.38f, 0.31f, 0.48f, 0.39f, () => Host.Toast("Traits belum dibuat."), 18);
            UIFactory.Btn(Root, "Talents", "Talents", Theme.Card, Theme.Ink, 0.49f, 0.31f, 0.59f, 0.39f, () => Host.Toast("Talents belum dibuat."), 18);
            addBtn = UIFactory.Btn(Root, "AddRemove", "Add to Party", Theme.Card, Theme.Ink, 0.60f, 0.31f, 0.70f, 0.39f, OnAddRemove, 16);

            movesBox = UIFactory.NewRect(Root, "Moves");
            UIFactory.Stretch(movesBox, 0.72f, 0.32f, 0.99f, 0.97f);

            var dp = UIFactory.Panel(Root, "Detail", Theme.Card, 0.38f, 0.03f, 0.99f, 0.29f, true);
            detail = dp.rectTransform;
            equipBtn = UIFactory.Btn(detail, "EquipToggle", "Equip /\nUnequip", Theme.Card, Theme.Ink, 0.83f, 0.15f, 0.97f, 0.85f, OnEquipToggle, 16);
        }

        public override void Refresh()
        {
            var roster = RunState.Roster;
            if (viewIndex < 0 || viewIndex >= roster.Count)
                viewIndex = DungeonState.Party.Count > 0 ? DungeonState.Party[0] : 0;

            BuildPartySlots();
            BuildList();
            BuildCenter();
            BuildMoves();
            BuildDetail();
        }

        // ---------- bagian-bagian layar ----------

        void BuildPartySlots()
        {
            UIFactory.ClearChildren(partyBox);
            for (int i = 0; i < DungeonState.PartyMax; i++)
            {
                float x0 = 0.02f + i * 0.245f, x1 = x0 + 0.23f;
                if (i < DungeonState.Party.Count)
                {
                    int idx = DungeonState.Party[i];
                    var c = RunState.Roster[idx];
                    var b = UIFactory.Btn(partyBox, "Slot" + i, "", Theme.Hex(c.Data.PortraitHex), Theme.Ink, x0, 0.05f, x1, 0.95f,
                                          () => { viewIndex = idx; Refresh(); }, 16);
                    UIFactory.Label(b.transform, Initials(c.Data.Name), 26, Color.white, TextAnchor.MiddleCenter, 0.05f, 0.42f, 0.95f, 0.88f);
                    UIFactory.Label(b.transform, UnitData.RoleLabel(c.Data.Role), 12, Color.white, TextAnchor.MiddleCenter, 0.02f, 0.22f, 0.98f, 0.42f);
                    StaminaBar(b.transform, c, 0.1f, 0.06f, 0.9f, 0.16f);
                }
                else
                {
                    var e = UIFactory.Panel(partyBox, "Empty" + i, new Color(1f, 1f, 1f, 0.35f), x0, 0.05f, x1, 0.95f, true);
                    UIFactory.Label(e.transform, "Empty", 14, Theme.Line, TextAnchor.MiddleCenter, 0.05f, 0.3f, 0.95f, 0.7f);
                }
            }
        }

        void BuildList()
        {
            UIFactory.ClearChildren(listContent);
            var roster = RunState.Roster;
            const float cellH = 150f, gap = 8f;
            int rows = (roster.Count + 1) / 2;
            listContent.sizeDelta = new Vector2(0f, rows * (cellH + gap) + gap);

            for (int i = 0; i < roster.Count; i++)
            {
                int idx = i;
                var c = roster[i];
                int col = i % 2, row = i / 2;
                float x0 = col == 0 ? 0.03f : 0.52f, x1 = col == 0 ? 0.48f : 0.97f;

                Color fill = idx == viewIndex ? Theme.Gold : Color.white;
                var b = UIFactory.Btn(listContent, "Char" + i, "", fill, Theme.Ink, 0f, 0f, 1f, 1f, () => { viewIndex = idx; selMove = 0; Refresh(); }, 14);
                var rt = b.GetComponent<RectTransform>();
                UIFactory.TopAnchored(rt, x0, x1, gap + row * (cellH + gap), cellH);
                var t = b.transform;

                Color portrait = c.Alive ? Theme.Hex(c.Data.PortraitHex) : new Color(0.45f, 0.45f, 0.45f);
                var pimg = UIFactory.Panel(t, "Portrait", portrait, 0.2f, 0.42f, 0.8f, 0.94f, false);
                UIFactory.Label(pimg.transform, Initials(c.Data.Name), 28, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);
                UIFactory.Label(t, c.Data.Name + (c.Alive ? "" : " (Fallen)"), 14, Theme.Ink, TextAnchor.MiddleCenter, 0.02f, 0.22f, 0.98f, 0.42f);
                UIFactory.Label(t, UnitData.RoleLabel(c.Data.Role), 12, Theme.Line, TextAnchor.MiddleCenter, 0.02f, 0.12f, 0.98f, 0.24f);
                StaminaBar(t, c, 0.12f, 0.03f, 0.88f, 0.11f);
                if (idx == 0) UIFactory.Label(t, "LEADER", 11, Theme.Ink, TextAnchor.UpperLeft, 0.03f, 0.88f, 0.5f, 0.99f);
                if (DungeonState.Party.Contains(idx))
                {
                    var tag = UIFactory.Panel(t, "InParty", Theme.Highlight, 0.62f, 0.86f, 0.98f, 0.98f, false);
                    UIFactory.Label(tag.transform, "PARTY", 11, Theme.Ink, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);
                }
            }
        }

        void BuildCenter()
        {
            UIFactory.ClearChildren(center);
            var c = RunState.Roster[viewIndex];
            UIFactory.Label(center, c.Data.Name, 32, Theme.Ink, TextAnchor.MiddleCenter, 0f, 0.84f, 1f, 1f);
            var p = UIFactory.Panel(center, "Portrait", Theme.Hex(c.Data.PortraitHex), 0.2f, 0.12f, 0.8f, 0.82f, true);
            UIFactory.Label(p.transform, Initials(c.Data.Name), 64, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);
            UIFactory.Label(center, UnitData.RoleLabel(c.Data.Role), 18, Theme.Line, TextAnchor.MiddleCenter, 0f, 0f, 1f, 0.11f);

            UIFactory.SetBtnText(addBtn, DungeonState.Party.Contains(viewIndex) ? "Remove from Party" : "Add to Party");
        }

        void BuildMoves()
        {
            UIFactory.ClearChildren(movesBox);
            var c = RunState.Roster[viewIndex];
            var equipped = new List<int>(c.Data.Equipped ?? new int[0]);
            for (int i = 0; i < 8; i++)
            {
                int idx = i;
                int col = i % 2, row = i / 2;
                float x0 = col == 0 ? 0f : 0.52f, x1 = col == 0 ? 0.48f : 1f;
                float y1 = 1f - row * 0.25f, y0 = y1 - 0.22f;
                bool has = i < c.Data.Skills.Count;
                Color fill = !has ? new Color(1f, 1f, 1f, 0.3f) : (equipped.Contains(i) ? Theme.Gold : Theme.Card);
                string label = has ? c.Data.Skills[i].Name : "Move " + (i + 1);
                var b = UIFactory.Btn(movesBox, "Move" + i, label, fill, Theme.Ink, x0, y0, x1, y1, () => { selMove = idx; BuildMoves(); BuildDetail(); }, 15);
                if (i == selMove)
                {
                    var o = b.GetComponent<Outline>();
                    if (o != null) { o.effectColor = Theme.Highlight; o.effectDistance = new Vector2(4f, -4f); }
                }
            }
        }

        void BuildDetail()
        {
            // hapus semua isi kecuali tombol equip
            for (int i = detail.childCount - 1; i >= 0; i--)
            {
                var ch = detail.GetChild(i);
                if (ch.gameObject != equipBtn.gameObject) Object.Destroy(ch.gameObject);
            }
            var c = RunState.Roster[viewIndex];
            var d = c.Data;

            string stats = "HP   " + c.Hp + " / " + d.MaxHp + "\nATK  " + d.Atk + "\nDEF  " + d.Def + "\nSPD  " + d.Speed;
            UIFactory.Label(detail, "STATS", 14, Theme.Line, TextAnchor.UpperLeft, 0.02f, 0.80f, 0.25f, 0.97f);
            UIFactory.Label(detail, stats, 18, Theme.Ink, TextAnchor.UpperLeft, 0.02f, 0.05f, 0.27f, 0.82f);

            if (selMove < d.Skills.Count)
            {
                var s = d.Skills[selMove];
                bool eq = new List<int>(d.Equipped ?? new int[0]).Contains(selMove);
                string power = s.Kind == SkillKind.Status ? "" : "  (x" + s.Power.ToString("0.0#") + ")";
                UIFactory.Label(detail, "Move " + (selMove + 1) + ": " + s.Name + power + (eq ? "   [Equipped]" : ""), 20, Theme.Ink,
                                TextAnchor.UpperLeft, 0.30f, 0.62f, 0.80f, 0.95f);
                UIFactory.Label(detail, s.Description + "\nTarget: " + s.Target, 17, Theme.Ink, TextAnchor.UpperLeft, 0.30f, 0.08f, 0.80f, 0.62f);
                UIFactory.SetBtnText(equipBtn, eq ? "Unequip" : "Equip");
                equipBtn.gameObject.SetActive(true);
            }
            else
            {
                UIFactory.Label(detail, "Move " + (selMove + 1) + " belum ada.", 18, Theme.Line, TextAnchor.UpperLeft, 0.30f, 0.62f, 0.80f, 0.95f);
                equipBtn.gameObject.SetActive(false);
            }
        }

        // ---------- aksi ----------

        void OnAddRemove()
        {
            string msg = DungeonState.TogglePartyMember(viewIndex);
            if (msg != null) Host.Toast(msg);
            Refresh();
        }

        void OnEquipToggle()
        {
            var d = RunState.Roster[viewIndex].Data;
            string msg = DungeonState.ToggleEquipSkill(d, selMove);
            if (msg != null) Host.Toast(msg);
            BuildMoves();
            BuildDetail();
        }

        // ---------- util ----------

        static void StaminaBar(Transform parent, CharacterState c, float x0, float y0, float x1, float y1)
        {
            float f = c.MaxStamina > 0 ? (float)c.Stamina / c.MaxStamina : 0f;
            UIFactory.Bar(parent, f, f > 0.5f ? Theme.Good : (f > 0f ? Theme.Highlight : Theme.Bad), x0, y0, x1, y1);
        }

        static string Initials(string name)
        {
            return string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1);
        }
    }
}
