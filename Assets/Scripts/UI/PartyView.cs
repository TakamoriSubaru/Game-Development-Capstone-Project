using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Tab Party: susun party, lihat karakter, pasang move (maks 4 dari 8).
    // Trait dan Talent ada di bagian PASSIVE yang terpisah dan hanya baca (tidak bisa di-equip).
    public class PartyView : View
    {
        class Entry
        {
            public string Title, Sub, Head, Desc;
            public Color Accent, Fill;
        }

        const int MoveSlots = 8;      // sel 0..7 = Move, 8 = Trait, 9 = Talent
        const int TraitSel = 8, TalentSel = 9;
        const float MaxAtk = 40f, MaxDef = 30f, MaxSpd = 120f;   // acuan panjang bar stat

        int viewIndex = -1;      // indeks roster yang sedang dilihat
        int sel = 0;             // tile yang dipilih (0..9)

        RectTransform partyBox, listContent, center, grid, detail;
        Button addBtn, equipBtn;

        protected override void Build()
        {
            HubUI.Flat(Root, "Bg", HubTheme.Night, 0f, 0f, 1f, 1f, true);

            // Kiri: party dan daftar karakter.
            UIFactory.Label(Root, "PARTY", 16, HubTheme.Gold, TextAnchor.MiddleLeft, 0.02f, 0.955f, 0.37f, 0.995f);
            var pb = HubUI.Card(Root, "PartyBox", HubTheme.Deep, 0.02f, 0.60f, 0.37f, 0.95f, HubTheme.Bronze);
            partyBox = pb.rectTransform;

            UIFactory.Label(Root, "CHARACTERS", 16, HubTheme.Gold, TextAnchor.MiddleLeft, 0.02f, 0.555f, 0.37f, 0.595f);
            var lb = HubUI.Card(Root, "ListBox", HubTheme.Deep, 0.02f, 0.03f, 0.37f, 0.55f, HubTheme.Bronze);
            UIFactory.Scroll(lb.transform, "ListScroll", 0.02f, 0.02f, 0.98f, 0.98f, out listContent);

            // Tengah: karakter yang dilihat.
            var c = UIFactory.NewRect(Root, "Center");
            UIFactory.Stretch(c, 0.38f, 0.40f, 0.70f, 0.99f);
            center = c;
            addBtn = HubUI.Btn(Root, "AddRemove", "ADD TO PARTY", null, HubTheme.Panel, HubTheme.Marble,
                               0.40f, 0.31f, 0.68f, 0.385f, OnAddRemove, 18);

            // Kanan: bagian MOVES (8 move), garis pemisah, lalu bagian PASSIVE (Trait dan Talent).
            grid = UIFactory.NewRect(Root, "Grid");
            UIFactory.Stretch(grid, 0.72f, 0.32f, 0.99f, 0.97f);

            // Bawah: detail.
            var dp = HubUI.Card(Root, "Detail", HubTheme.Deep, 0.38f, 0.03f, 0.99f, 0.29f, HubTheme.Bronze);
            detail = dp.rectTransform;
            equipBtn = HubUI.Btn(detail, "EquipToggle", "EQUIP", null, HubTheme.Gold, HubTheme.Night,
                                 0.80f, 0.15f, 0.97f, 0.85f, OnEquipToggle, 20);
        }

        public override void Refresh()
        {
            var roster = RunState.Roster;
            if (viewIndex < 0 || viewIndex >= roster.Count)
                viewIndex = DungeonState.Party.Count > 0 ? DungeonState.Party[0] : 0;
            var c = roster[viewIndex];

            BuildPartySlots();
            BuildList();
            BuildCenter(c);
            BuildGrid(c);
            BuildDetail(c);
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
                    Color col = Theme.Hex(c.Data.PortraitHex);
                    if (!c.Alive) col = col * 0.4f;
                    var b = HubUI.Btn(partyBox, "Slot" + i, "", null, col, Color.white, x0, 0.05f, x1, 0.95f,
                                      () => { viewIndex = idx; sel = 0; Refresh(); }, 16);
                    var t = b.transform;
                    UIFactory.Label(t, Initials(c.Data.Name), 26, Color.white, TextAnchor.MiddleCenter, 0.05f, 0.42f, 0.95f, 0.88f);
                    UIFactory.Label(t, UnitData.RoleLabel(c.Data.Role), 12, Color.white, TextAnchor.MiddleCenter, 0.02f, 0.22f, 0.98f, 0.42f);
                    StaminaBar(t, c, 0.1f, 0.06f, 0.9f, 0.16f);
                    if (idx == viewIndex) HubUI.Flat(t, "Sel", HubTheme.Gold, 0.05f, 0.94f, 0.95f, 0.99f);
                }
                else
                {
                    var e = HubUI.Card(partyBox, "Empty" + i, new Color(1f, 1f, 1f, 0.05f), x0, 0.05f, x1, 0.95f, HubTheme.Bronze);
                    UIFactory.Label(e.transform, "Empty", 14, HubTheme.Muted, TextAnchor.MiddleCenter, 0.05f, 0.3f, 0.95f, 0.7f);
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

                var rt = UIFactory.NewRect(listContent, "Char" + i);
                UIFactory.TopAnchored(rt, x0, x1, gap + row * (cellH + gap), cellH);
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = UIFactory.RoundedSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                HubTheme.Frame(img, idx == viewIndex ? HubTheme.Gold : HubTheme.Bronze);
                var b = rt.gameObject.AddComponent<Button>();
                b.targetGraphic = img;
                HubUI.Tint(b, idx == viewIndex ? HubTheme.Panel : HubTheme.Night);
                rt.gameObject.AddComponent<ButtonFx>();
                b.onClick.AddListener(() => { viewIndex = idx; sel = 0; Refresh(); });
                var t = rt;

                Color portrait = c.Alive ? Theme.Hex(c.Data.PortraitHex) : new Color(0.45f, 0.45f, 0.45f);
                var pimg = UIFactory.Panel(t, "Portrait", portrait, 0.2f, 0.42f, 0.8f, 0.94f, false);
                UIFactory.Label(pimg.transform, Initials(c.Data.Name), 28, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);
                UIFactory.Label(t, c.Data.Name + (c.Alive ? "" : " (Fallen)"), 14, HubTheme.Marble, TextAnchor.MiddleCenter, 0.02f, 0.22f, 0.98f, 0.42f);
                UIFactory.Label(t, UnitData.RoleLabel(c.Data.Role), 12, HubTheme.Muted, TextAnchor.MiddleCenter, 0.02f, 0.12f, 0.98f, 0.24f);
                StaminaBar(t, c, 0.12f, 0.03f, 0.88f, 0.11f);
                if (idx == 0) UIFactory.Label(t, "LEADER", 11, HubTheme.Gold, TextAnchor.UpperLeft, 0.03f, 0.88f, 0.5f, 0.99f);
                if (DungeonState.Party.Contains(idx))
                {
                    var tag = UIFactory.Panel(t, "InParty", HubTheme.Teal, 0.62f, 0.86f, 0.98f, 0.98f, false);
                    UIFactory.Label(tag.transform, "PARTY", 11, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);
                }
            }
        }

        void BuildCenter(CharacterState c)
        {
            UIFactory.ClearChildren(center);
            UIFactory.Label(center, c.Data.Name, 32, HubTheme.Marble, TextAnchor.MiddleCenter, 0f, 0.88f, 1f, 1f);
            var p = UIFactory.Panel(center, "Portrait", Theme.Hex(c.Data.PortraitHex), 0.22f, 0.30f, 0.78f, 0.86f, false);
            HubTheme.Frame(p, HubTheme.Gold);
            UIFactory.Label(p.transform, Initials(c.Data.Name), 64, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);

            var badge = HubUI.Card(center, "Role", HubTheme.Teal, 0.25f, 0.16f, 0.75f, 0.26f, HubTheme.Bronze);
            UIFactory.Label(badge.transform, UnitData.RoleLabel(c.Data.Role).ToUpper(), 16, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);

            int eq = EquippedList(c.Data).Count;
            UIFactory.Label(center, "Moves equipped " + eq + "/4", 16, eq >= 4 ? HubTheme.Gold : HubTheme.Muted,
                            TextAnchor.MiddleCenter, 0f, 0.02f, 1f, 0.13f);

            UIFactory.SetBtnText(addBtn, DungeonState.Party.Contains(viewIndex) ? "REMOVE FROM PARTY" : "ADD TO PARTY");
        }

        // Isi satu tile. s: 0..7 = Move, 8 = Trait, 9 = Talent. null kalau kosong.
        Entry EntryAt(CharacterState c, int s)
        {
            var d = c.Data;

            if (s < MoveSlots)
            {
                if (s >= d.Skills.Count) return null;
                var sk = d.Skills[s];
                bool on = EquippedList(d).Contains(s);
                string power = sk.Kind == SkillKind.Status ? "" : "  x" + sk.Power.ToString("0.0#");
                return new Entry
                {
                    Title = sk.Name,
                    Sub = sk.Kind.ToString().ToUpper() + (on ? "  |  EQUIPPED" : ""),
                    Head = sk.Name + power,
                    Desc = sk.Description + "\nTarget: " + Nice(sk.Target.ToString()),
                    Accent = KindColor(sk.Kind),
                    Fill = on ? Color.Lerp(HubTheme.Panel, HubTheme.Teal, 0.35f) : HubTheme.Panel
                };
            }

            if (s == TraitSel)
            {
                var traits = PassiveCatalog.TraitsOf(d.Id);
                if (traits.Length == 0) return null;
                return new Entry
                {
                    Title = traits[0].Name, Sub = "TRAIT  |  ALWAYS ON", Head = traits[0].Name,
                    Desc = traits[0].Desc, Accent = HubTheme.Gold, Fill = HubTheme.Deep
                };
            }

            var talents = PassiveCatalog.TalentsOf(d.Id);
            if (talents.Length == 0) return null;
            int floor = PassiveCatalog.TalentFloors.Length > 0 ? PassiveCatalog.TalentFloors[0] : 99;
            bool open = DungeonState.Floor >= floor;
            return new Entry
            {
                Title = talents[0].Name,
                Sub = open ? "TALENT  |  ACTIVE" : "TALENT  |  LOCKED  |  FLOOR " + floor,
                Head = talents[0].Name,
                Desc = open ? talents[0].Desc : "Unlocks when the run reaches Floor " + floor + ".",
                Accent = open ? HubTheme.Teal : HubTheme.Muted,
                Fill = HubTheme.Deep
            };
        }

        void BuildGrid(CharacterState c)
        {
            UIFactory.ClearChildren(grid);
            if (sel < 0 || sel > TalentSel) sel = 0;

            // Judul bagian MOVES.
            int eq = EquippedList(c.Data).Count;
            UIFactory.Label(grid, "MOVES  " + eq + "/4  (tap to equip)", 14, HubTheme.Gold, TextAnchor.MiddleLeft,
                            0f, 0.95f, 1f, 1f);

            for (int i = 0; i <= TalentSel; i++)
            {
                int idx = i;
                bool isMove = i < MoveSlots;
                float x0, x1, y0, y1;
                if (isMove)
                {
                    int col = i % 2, row = i / 2;
                    x0 = col == 0 ? 0f : 0.52f; x1 = col == 0 ? 0.48f : 1f;
                    y1 = 0.945f - row * 0.17f; y0 = y1 - 0.155f;
                }
                else
                {
                    x0 = i == TraitSel ? 0f : 0.52f; x1 = i == TraitSel ? 0.48f : 1f;
                    y0 = 0f; y1 = 0.175f;
                }

                var e = EntryAt(c, i);
                if (e != null)
                {
                    var img = UIFactory.Panel(grid, "Tile" + i, Color.white, x0, y0, x1, y1, false);
                    var dimBronze = new Color(HubTheme.Bronze.r, HubTheme.Bronze.g, HubTheme.Bronze.b, 0.45f);
                    HubTheme.Frame(img, i == sel ? HubTheme.Gold : (isMove ? HubTheme.Bronze : dimBronze));
                    var b = img.gameObject.AddComponent<Button>();
                    b.targetGraphic = img;
                    HubUI.Tint(b, e.Fill);
                    // Efek tombol hanya untuk Move. Passive tampil redup dan tidak terlihat bisa di-equip.
                    if (isMove) img.gameObject.AddComponent<ButtonFx>();
                    b.onClick.AddListener(() => { sel = idx; BuildGrid(c); BuildDetail(c); });

                    if (isMove)
                    {
                        HubUI.Flat(img.transform, "Accent", e.Accent, 0f, 0.10f, 0.04f, 0.90f);
                        UIFactory.Label(img.transform, e.Title, 15, HubTheme.Marble, TextAnchor.MiddleLeft, 0.08f, 0.42f, 0.97f, 0.95f);
                        UIFactory.Label(img.transform, e.Sub, 11, HubTheme.Muted, TextAnchor.MiddleLeft, 0.08f, 0.05f, 0.97f, 0.42f);
                    }
                    else
                    {
                        UIFactory.Label(img.transform, e.Title, 14, HubTheme.Muted, TextAnchor.MiddleCenter, 0.04f, 0.42f, 0.96f, 0.95f);
                        UIFactory.Label(img.transform, e.Sub, 10, e.Accent, TextAnchor.MiddleCenter, 0.04f, 0.05f, 0.96f, 0.42f);
                    }
                }
                else
                {
                    var em = UIFactory.Panel(grid, "Empty" + i, new Color(1f, 1f, 1f, 0.04f), x0, y0, x1, y1, false);
                    UIFactory.Label(em.transform, "-", 18, HubTheme.Muted, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);
                }
            }

            // Garis pemisah dan judul bagian PASSIVE.
            HubUI.Flat(grid, "PassiveDivider", HubTheme.Bronze, 0f, 0.255f, 1f, 0.262f);
            UIFactory.Label(grid, "PASSIVE  (cannot be equipped)", 14, HubTheme.Muted, TextAnchor.MiddleLeft,
                            0f, 0.19f, 1f, 0.25f);
        }

        void BuildDetail(CharacterState c)
        {
            // hapus semua isi kecuali tombol equip
            for (int i = detail.childCount - 1; i >= 0; i--)
            {
                var ch = detail.GetChild(i);
                if (ch.gameObject != equipBtn.gameObject) Object.Destroy(ch.gameObject);
            }
            var d = c.Data;

            // Stat dengan bar.
            UIFactory.Label(detail, "STATS", 13, HubTheme.Gold, TextAnchor.UpperLeft, 0.02f, 0.84f, 0.30f, 0.98f);
            StatRow(0, "HP", c.Hp + "/" + d.MaxHp, d.MaxHp > 0 ? (float)c.Hp / d.MaxHp : 0f, Theme.Good);
            StatRow(1, "ATK", d.Atk.ToString(), d.Atk / MaxAtk, HubTheme.Danger);
            StatRow(2, "DEF", d.Def.ToString(), d.Def / MaxDef, HubTheme.Teal);
            StatRow(3, "SPD", d.Speed.ToString(), d.Speed / MaxSpd, HubTheme.Gold);

            HubUI.Flat(detail, "Divider", HubTheme.Bronze, 0.325f, 0.08f, 0.330f, 0.92f);

            var e = EntryAt(c, sel);
            if (e != null)
            {
                UIFactory.Label(detail, e.Head, 22, HubTheme.Marble, TextAnchor.MiddleLeft, 0.35f, 0.68f, 0.78f, 0.96f);
                UIFactory.Label(detail, e.Sub, 13, e.Accent, TextAnchor.MiddleLeft, 0.35f, 0.54f, 0.78f, 0.68f);
                UIFactory.Label(detail, e.Desc, 17, HubTheme.Marble, TextAnchor.UpperLeft, 0.35f, 0.05f, 0.78f, 0.54f);
            }
            else
            {
                UIFactory.Label(detail, "Nothing here yet.", 18, HubTheme.Muted, TextAnchor.UpperLeft, 0.35f, 0.60f, 0.78f, 0.95f);
            }

            // Equip hanya untuk Move. Trait dan Talent hanya baca.
            bool showEquip = sel < MoveSlots && e != null;
            equipBtn.gameObject.SetActive(showEquip);
            if (showEquip) UIFactory.SetBtnText(equipBtn, EquippedList(d).Contains(sel) ? "UNEQUIP" : "EQUIP");
            else if (e != null)
                UIFactory.Label(detail, "Passive\nCannot be equipped", 14, HubTheme.Muted, TextAnchor.MiddleCenter,
                                0.80f, 0.15f, 0.97f, 0.85f);
        }

        void StatRow(int row, string label, string value, float fraction, Color color)
        {
            float y1 = 0.82f - row * 0.20f, y0 = y1 - 0.16f;
            UIFactory.Label(detail, label, 13, HubTheme.Muted, TextAnchor.MiddleLeft, 0.02f, y0, 0.08f, y1);
            UIFactory.Bar(detail, Mathf.Clamp01(fraction), color, 0.09f, y0 + 0.03f, 0.21f, y1 - 0.03f);
            UIFactory.Label(detail, value, 13, HubTheme.Marble, TextAnchor.MiddleLeft, 0.22f, y0, 0.32f, y1);
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
            var c = RunState.Roster[viewIndex];
            string msg = DungeonState.ToggleEquipSkill(c.Data, sel);
            if (msg != null) Host.Toast(msg);
            BuildCenter(c);
            BuildGrid(c);
            BuildDetail(c);
        }

        // ---------- util ----------

        static List<int> EquippedList(UnitData d)
        {
            return new List<int>(d.Equipped ?? new int[0]);
        }

        static Color KindColor(SkillKind k)
        {
            switch (k)
            {
                case SkillKind.Damage: return HubTheme.Danger;
                case SkillKind.Heal: return HubTheme.Teal;
            }
            return HubTheme.Gold;
        }

        // "SingleEnemy" -> "Single Enemy"
        static string Nice(string s)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (i > 0 && char.IsUpper(s[i])) sb.Append(' ');
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

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