using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Oddyseus.Core;

namespace Oddyseus.UI
{
    // Layar battle. Dibangun lewat kode seperti view lain dan dipasang di bawah area utama (top bar tetap terlihat).
    // Alur: Begin() -> AdvanceRoutine() mengulang { NextActor -> giliran musuh otomatis | giliran pemain menunggu klik }.
    public class BattleView
    {
        enum Stage { Busy, Choose, Target, Blessing, Done }

        readonly MonoBehaviour host;
        readonly Action<string> toast;

        public RectTransform Root { get; private set; }
        public bool Active { get; private set; }

        RectTransform orderBar, field, cardsRoot, fxLayer, actionPanel, logPanel, resultLayer;
        Text stallText, bannerText, logText;

        BattleSystem bs;
        Encounter enc;
        Stage stage = Stage.Done;
        Unit current;
        Skill pendingSkill;
        Action<bool> onFinished;
        bool lastVictory;
        int session;                                   // naik tiap battle baru atau Abort, supaya coroutine lama berhenti
        readonly List<string> log = new List<string>();
        readonly HashSet<int> usedBlessings = new HashSet<int>();

        BattleView(MonoBehaviour host, Action<string> toast)
        {
            this.host = host;
            this.toast = toast;
        }

        public static BattleView Create(RectTransform parent, MonoBehaviour host, Action<string> toast)
        {
            var v = new BattleView(host, toast);
            v.Build(parent);
            return v;
        }

        // ---------- build ----------

        void Build(RectTransform parent)
        {
            var bg = HubUI.Flat(parent, "Battle", HubTheme.Night, 0f, 0f, 1f, 1f, true);
            Root = bg.rectTransform;

            orderBar = UIFactory.NewRect(Root, "OrderBar");
            UIFactory.Stretch(orderBar, 0.01f, 0.90f, 0.62f, 0.99f);
            stallText = UIFactory.Label(Root, "", 16, HubTheme.Gold, TextAnchor.MiddleRight, 0.63f, 0.90f, 0.99f, 0.99f);

            field = UIFactory.NewRect(Root, "Field");
            UIFactory.Stretch(field, 0.01f, 0.21f, 0.99f, 0.89f);
            cardsRoot = UIFactory.NewRect(field, "Cards");
            UIFactory.Stretch(cardsRoot, 0f, 0f, 1f, 1f);
            fxLayer = UIFactory.NewRect(field, "Fx");
            UIFactory.Stretch(fxLayer, 0f, 0f, 1f, 1f);
            bannerText = UIFactory.Label(field, "", 22, HubTheme.Marble, TextAnchor.MiddleCenter, 0.15f, 0.46f, 0.85f, 0.54f);

            actionPanel = UIFactory.NewRect(Root, "Actions");
            UIFactory.Stretch(actionPanel, 0.01f, 0.01f, 0.62f, 0.20f);

            var lb = HubUI.Card(Root, "LogBox", HubTheme.Deep, 0.63f, 0.01f, 0.99f, 0.20f, HubTheme.Bronze);
            logPanel = lb.rectTransform;
            logText = UIFactory.Label(logPanel, "", 13, HubTheme.Marble, TextAnchor.LowerLeft, 0.03f, 0.04f, 0.97f, 0.96f);

            resultLayer = UIFactory.NewRect(Root, "Result");
            UIFactory.Stretch(resultLayer, 0f, 0f, 1f, 1f);
            resultLayer.gameObject.SetActive(false);

            Root.gameObject.SetActive(false);
        }

        // ---------- mulai dan akhiri ----------

        // Mengembalikan false kalau tidak ada karakter yang bisa bertarung (battle tidak dimulai, tidak ada yang berubah).
        public bool Begin(List<int> picks, BattleEnvironment env, Action<bool> finished)
        {
            var lineup = new List<int>();
            for (int i = 0; i < picks.Count; i++)
            {
                int idx = picks[i];
                if (idx < 0 || idx >= RunState.Roster.Count || lineup.Contains(idx)) continue;
                if (RunState.CanField(RunState.Roster[idx])) lineup.Add(idx);
            }
            if (lineup.Count == 0)
            {
                for (int i = 0; i < RunState.Roster.Count && lineup.Count < RunState.PartySize; i++)
                    if (RunState.CanField(RunState.Roster[i])) lineup.Add(i);
                if (lineup.Count > 0) toast("Party tidak siap: karakter yang siap dipilih otomatis.");
            }
            while (lineup.Count > RunState.PartySize) lineup.RemoveAt(lineup.Count - 1);
            if (lineup.Count == 0) return false;

            enc = GameData.GetEncounter(env);
            var enemies = new List<Unit>();
            for (int i = 0; i < enc.EnemyIds.Length; i++)
            {
                UnitData d = GameData.CreateEnemy(enc.EnemyIds[i]);
                if (d != null) enemies.Add(new Unit(d, true, d.MaxHp));
            }
            if (enemies.Count == 0) return false;

            string note = RunState.PrepareBattle(lineup);   // memotong Stamina dan Food
            if (!string.IsNullOrEmpty(note)) toast(note);

            session++;
            bs = new BattleSystem(RunState.BuildAllies(), enemies, (int)(DateTime.Now.Ticks & 0x7FFFFFFF));
            bs.ReinforcementIds = enc.EnemyIds;
            bs.SetResources(RunState.Food, RunState.Gold);
            log.Clear();
            usedBlessings.Clear();
            bs.OnLog += AddLog;
            bs.Begin();

            onFinished = finished;
            current = null;
            pendingSkill = null;
            stage = Stage.Busy;
            Active = true;
            resultLayer.gameObject.SetActive(false);
            Root.gameObject.SetActive(true);
            Root.SetAsLastSibling();
            SetBanner("");
            Redraw();
            host.StartCoroutine(AdvanceRoutine());
            return true;
        }

        // Batalkan tanpa menyimpan hasil (dipakai saat Restart Map atau Reset Dungeon dari Settings).
        public void Abort()
        {
            session++;
            Active = false;
            onFinished = null;
            bs = null;
            if (Root != null) Root.gameObject.SetActive(false);
            if (resultLayer != null) resultLayer.gameObject.SetActive(false);
        }

        void Finish()
        {
            stage = Stage.Done;
            current = null;
            SetBanner("");
            lastVictory = bs.Outcome == BattleOutcome.Victory;
            bs.ApplyResourcesToRun();
            string summary = RunState.CommitBattle(bs.Allies, lastVictory, enc.GoldReward);

            string extra = "";
            int fled = bs.FledUnits.Count, lostFood = 0, lostGold = 0;
            for (int i = 0; i < bs.FledUnits.Count; i++)
            {
                lostFood += bs.FledUnits[i].StolenFood;
                lostGold += bs.FledUnits[i].StolenGold;
            }
            if (fled > 0) extra += "Musuh kabur: " + fled + " (tanpa hadiah).\n";
            if (lostFood + lostGold > 0) extra += "Curian hilang: Food " + lostFood + ", Gold " + lostGold + ".\n";

            Redraw();
            ShowResult(lastVictory, extra + summary);
        }

        void ShowResult(bool victory, string text)
        {
            resultLayer.gameObject.SetActive(true);
            resultLayer.SetAsLastSibling();
            UIFactory.ClearChildren(resultLayer);
            UIFactory.Panel(resultLayer, "Dim", Theme.Dim, 0f, 0f, 1f, 1f, false);
            var box = HubUI.Card(resultLayer, "Box", HubTheme.Deep, 0.27f, 0.20f, 0.73f, 0.80f, HubTheme.Bronze);
            UIFactory.Label(box.transform, victory ? "VICTORY" : "DEFEAT", 36, victory ? HubTheme.Gold : HubTheme.Danger,
                            TextAnchor.MiddleCenter, 0.06f, 0.80f, 0.94f, 0.96f);
            HubUI.Flat(box.transform, "Divider", HubTheme.Bronze, 0.06f, 0.785f, 0.94f, 0.792f);
            UIFactory.Label(box.transform, text, 20, HubTheme.Marble, TextAnchor.UpperLeft, 0.06f, 0.28f, 0.94f, 0.76f);
            HubUI.Btn(box.transform, "Continue", "Continue", null, HubTheme.Gold, HubTheme.Night,
                      0.28f, 0.06f, 0.72f, 0.22f, OnContinue, 24);
        }

        void OnContinue()
        {
            resultLayer.gameObject.SetActive(false);
            Root.gameObject.SetActive(false);
            Active = false;
            var cb = onFinished;
            onFinished = null;
            if (cb != null) cb(lastVictory);
        }

        // ---------- alur giliran ----------

        IEnumerator AdvanceRoutine()
        {
            int my = session;
            stage = Stage.Busy;
            while (true)
            {
                Unit a = bs.NextActor();
                current = a;

                if (bs.TurnStartEvents.Count > 0)
                {
                    var startEvents = new List<BattleEvent>(bs.TurnStartEvents);
                    Redraw();
                    ShowEvents(startEvents);
                    yield return new WaitForSecondsRealtime(0.7f);
                    if (my != session) yield break;
                }

                if (a == null) { Finish(); yield break; }

                if (!a.IsEnemy)
                {
                    stage = Stage.Choose;
                    Redraw();
                    yield break;
                }

                Redraw();
                yield return new WaitForSecondsRealtime(0.5f);
                if (my != session) yield break;

                EnemyAction act = bs.ChooseEnemyAction(a);
                SetBanner(DisplayName(a) + ": " + act.Label);
                yield return new WaitForSecondsRealtime(0.5f);
                if (my != session) yield break;

                var events = bs.ExecuteEnemyAction(act);
                Redraw();
                ShowEvents(events);
                yield return new WaitForSecondsRealtime(0.9f);
                if (my != session) yield break;
                SetBanner("");
            }
        }

        IEnumerator AfterPlayerRoutine()
        {
            int my = session;
            yield return new WaitForSecondsRealtime(0.9f);
            if (my != session) yield break;
            yield return AdvanceRoutine();
        }

        // ---------- input pemain ----------

        void OnSkill(Skill s)
        {
            if (stage != Stage.Choose) return;
            if (s.NeedsTargetPick)
            {
                pendingSkill = s;
                stage = Stage.Target;
                Redraw();
            }
            else DoSkill(s, null);
        }

        void OnTarget(Unit u)
        {
            if (stage != Stage.Target || pendingSkill == null) return;
            Skill s = pendingSkill;
            pendingSkill = null;
            DoSkill(s, u);
        }

        void DoSkill(Skill s, Unit target)
        {
            stage = Stage.Busy;
            var events = bs.UseSkill(s, target);
            if (events.Count == 0)
            {
                stage = Stage.Choose;
                Redraw();
                return;
            }
            AfterPlayerAction(events);
        }

        void OnCancel()
        {
            if (stage != Stage.Target && stage != Stage.Blessing) return;
            pendingSkill = null;
            stage = Stage.Choose;
            Redraw();
        }

        void OnBlessingMenu()
        {
            if (stage != Stage.Choose) return;
            if (BlessingsAvailable() == 0) { toast("Tidak ada Blessing yang bisa dipakai."); return; }
            stage = Stage.Blessing;
            Redraw();
        }

        void OnBlessing(int index)
        {
            if (stage != Stage.Blessing) return;
            if (usedBlessings.Contains(index)) { toast("Blessing ini sudah dipakai di battle ini."); return; }
            usedBlessings.Add(index);
            stage = Stage.Busy;
            var events = bs.UseBlessing(PlaceholderBlessing);
            AfterPlayerAction(events);
        }

        // PLACEHOLDER sampai spreadsheet Blessing jadi: semua kawan ATK +25% selama 3 giliran.
        List<BattleEvent> PlaceholderBlessing(BattleSystem b, Unit actor)
        {
            var events = new List<BattleEvent>();
            for (int i = 0; i < b.Allies.Count; i++)
            {
                Unit u = b.Allies[i];
                if (!u.Present) continue;
                u.AddStatus(StatusType.AtkUp, 3, 0.25f);
                events.Add(new BattleEvent { Target = u, Kind = EventKind.StatusApplied, Text = Unit.StatusShort(StatusType.AtkUp) });
            }
            return events;
        }

        int BlessingsAvailable()
        {
            int n = 0;
            for (int i = 0; i < DungeonState.God.Count; i++)
                if (DungeonState.God[i] != null && !usedBlessings.Contains(i)) n++;
            return n;
        }

        void AfterPlayerAction(List<BattleEvent> events)
        {
            Redraw();
            ShowEvents(events);
            host.StartCoroutine(AfterPlayerRoutine());
        }

        // ---------- gambar layar ----------

        void Redraw()
        {
            if (bs == null) return;
            DrawOrder();
            DrawCards();
            DrawStall();
            DrawActions();
            logText.text = string.Join("\n", log.ToArray());
        }

        void AddLog(string s)
        {
            log.Add(s);
            while (log.Count > 7) log.RemoveAt(0);
        }

        void SetBanner(string s) { bannerText.text = s; }

        string DisplayName(Unit u)
        {
            if (!u.IsEnemy) return u.Name;
            int same = 0, idx = 0;
            for (int i = 0; i < bs.Enemies.Count; i++)
            {
                if (bs.Enemies[i].Data.Id != u.Data.Id) continue;
                if (bs.Enemies[i] == u) idx = same;
                same++;
            }
            return same > 1 ? u.Name + " " + (char)('A' + idx) : u.Name;
        }

        string ShortTag(Unit u)
        {
            string s = HubUI.Initials(u.Name);
            string full = DisplayName(u);
            if (u.IsEnemy && full.Length > u.Name.Length) s += full.Substring(full.Length - 1);
            return s;
        }

        void DrawOrder()
        {
            UIFactory.ClearChildren(orderBar);
            UIFactory.Label(orderBar, "ROUND " + bs.Round, 16, HubTheme.Gold, TextAnchor.MiddleLeft, 0f, 0f, 0.16f, 1f);
            var order = bs.TurnOrder;
            int n = Mathf.Min(order.Count, 8);
            for (int i = 0; i < n; i++)
            {
                Unit u = order[i];
                float x0 = 0.17f + i * 0.104f, x1 = x0 + 0.094f;
                Color fill = u.IsEnemy ? Color.Lerp(HubTheme.Deep, HubTheme.Danger, 0.7f) : Color.Lerp(HubTheme.Deep, HubTheme.Teal, 0.8f);
                var chip = UIFactory.Panel(orderBar, "Chip" + i, fill, x0, 0.08f, x1, 0.92f, false);
                HubTheme.Frame(chip, u == current ? HubTheme.Gold : HubTheme.Bronze);
                UIFactory.Label(chip.transform, ShortTag(u), 16, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);
            }
        }

        // Posisi kartu dalam koordinat field (0..1). Musuh di atas, pemain di bawah.
        Rect CardRect(Unit u)
        {
            List<Unit> list = u.IsEnemy ? bs.Enemies : bs.Allies;
            int n = Mathf.Max(1, list.Count);
            int i = Mathf.Max(0, list.IndexOf(u));
            const float gap = 0.012f;
            float w = Mathf.Min(0.21f, 0.97f / n - gap);
            float total = n * w + (n - 1) * gap;
            float x0 = (1f - total) * 0.5f + i * (w + gap);
            float y0 = u.IsEnemy ? 0.54f : 0.02f;
            return new Rect(x0, y0, w, 0.44f);
        }

        void DrawCards()
        {
            UIFactory.ClearChildren(cardsRoot);
            List<Unit> valid = null;
            if (stage == Stage.Target && pendingSkill != null && current != null)
                valid = bs.ValidTargets(pendingSkill, current);
            for (int i = 0; i < bs.Enemies.Count; i++) DrawCard(bs.Enemies[i], valid);
            for (int i = 0; i < bs.Allies.Count; i++) DrawCard(bs.Allies[i], valid);
        }

        void DrawCard(Unit u, List<Unit> valid)
        {
            Rect r = CardRect(u);
            bool targetable = valid != null && valid.Contains(u);
            Color fill = u.Present ? HubTheme.Panel : HubTheme.Deep;

            // Kartu yang bisa diklik memakai gambar putih; warna diatur lewat Button (sama seperti HubUI.Btn).
            var img = UIFactory.Panel(cardsRoot, "Card", targetable ? Color.white : fill,
                                      r.x, r.y, r.x + r.width, r.y + r.height, false);
            HubTheme.Frame(img, (targetable || u == current) ? HubTheme.Gold : HubTheme.Bronze);
            if (targetable)
            {
                var b = img.gameObject.AddComponent<Button>();
                b.targetGraphic = img;
                HubUI.Tint(b, fill);
                img.gameObject.AddComponent<ButtonFx>();
                Unit t = u;
                b.onClick.AddListener(() => OnTarget(t));
            }

            Transform tr = img.transform;
            UIFactory.Label(tr, DisplayName(u), 14, HubTheme.Marble, TextAnchor.MiddleCenter, 0.04f, 0.85f, 0.96f, 0.99f);

            Color pc = u.Present ? Theme.Hex(u.Data.PortraitHex) : new Color(0.35f, 0.35f, 0.35f);
            var portrait = UIFactory.Panel(tr, "Portrait", pc, 0.30f, 0.40f, 0.70f, 0.84f, false);
            UIFactory.Label(portrait.transform, HubUI.Initials(u.Name), 26, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);

            float f = u.MaxHp > 0 ? (float)u.Hp / u.MaxHp : 0f;
            UIFactory.Bar(tr, f, u.IsEnemy ? HubTheme.Danger : Theme.Good, 0.08f, 0.28f, 0.92f, 0.36f);
            UIFactory.Label(tr, "HP " + u.Hp + "/" + u.MaxHp, 12, HubTheme.Muted, TextAnchor.MiddleCenter, 0.04f, 0.14f, 0.96f, 0.27f);
            UIFactory.Label(tr, StatusLine(u), 11, HubTheme.Gold, TextAnchor.MiddleCenter, 0.04f, 0.01f, 0.96f, 0.14f);

            if (!u.Present)
                UIFactory.Label(tr, u.Fled ? "FLED" : "DOWN", 22, u.Fled ? HubTheme.Muted : HubTheme.Danger,
                                TextAnchor.MiddleCenter, 0f, 0.40f, 1f, 0.84f);
        }

        static string StatusLine(Unit u)
        {
            if (u.Statuses.Count == 0) return "";
            var parts = new List<string>();
            for (int i = 0; i < u.Statuses.Count; i++)
                parts.Add(Unit.StatusShort(u.Statuses[i].Type) + " " + u.Statuses[i].TurnsLeft);
            return string.Join("  ", parts.ToArray());
        }

        void DrawStall()
        {
            if (bs.IsStalling)
            {
                stallText.text = "STALLING! Musuh bisa memanggil bantuan.";
                stallText.color = HubTheme.Danger;
            }
            else if (bs.StallCount >= 1)
            {
                stallText.text = "Stall " + bs.StallCount + "/" + BattleSystem.StallTurns + ": beri damage ke musuh!";
                stallText.color = HubTheme.Gold;
            }
            else stallText.text = "";
        }

        void DrawActions()
        {
            UIFactory.ClearChildren(actionPanel);
            switch (stage)
            {
                case Stage.Choose: DrawChoose(); break;
                case Stage.Target:
                    Prompt("Pilih target untuk " + pendingSkill.Name + ".", 0.76f);
                    HubUI.Btn(actionPanel, "Cancel", "CANCEL", null, HubTheme.Panel, HubTheme.Marble, 0.73f, 0.04f, 0.99f, 0.72f, OnCancel, 18);
                    break;
                case Stage.Blessing: DrawBlessing(); break;
                case Stage.Busy:
                    Prompt(current != null && current.IsEnemy ? "Giliran " + DisplayName(current) + "..." : "...", 0.30f);
                    break;
                default:
                    Prompt("Battle selesai.", 0.30f);
                    break;
            }
        }

        void Prompt(string text, float y0)
        {
            UIFactory.Label(actionPanel, text, 16, HubTheme.Gold, TextAnchor.MiddleLeft, 0.01f, y0, 0.99f, 0.99f);
        }

        void DrawChoose()
        {
            Prompt(DisplayName(current) + "  -  pilih aksi", 0.76f);
            var skills = current.Data.GetEquipped();
            for (int i = 0; i < skills.Count && i < 4; i++)
            {
                Skill s = skills[i];
                float x0 = i * 0.182f, x1 = x0 + 0.172f;
                HubUI.Btn(actionPanel, "Skill" + i, s.Name, SkillSub(s), HubTheme.Panel, HubTheme.Marble,
                          x0, 0.04f, x1, 0.72f, () => OnSkill(s), 15);
            }
            int avail = BlessingsAvailable();
            HubUI.Btn(actionPanel, "Blessing", "BLESSING", avail + " tersedia", HubTheme.Panel,
                      avail > 0 ? HubTheme.Gold : HubTheme.Muted, 0.74f, 0.04f, 0.99f, 0.72f, OnBlessingMenu, 15);
        }

        void DrawBlessing()
        {
            Prompt("Pilih Blessing (1x per battle, memakai giliran).", 0.76f);
            for (int i = 0; i < DungeonState.God.Count && i < 3; i++)
            {
                var r = DungeonState.God[i];
                if (r == null) continue;
                int idx = i;
                bool used = usedBlessings.Contains(i);
                float x0 = i * 0.245f, x1 = x0 + 0.235f;
                HubUI.Btn(actionPanel, "Bless" + i, r.Name, used ? "Sudah dipakai" : "Sisa " + r.BattlesLeft + " battle",
                          HubTheme.Panel, used ? HubTheme.Muted : HubTheme.Marble, x0, 0.04f, x1, 0.72f, () => OnBlessing(idx), 15);
            }
            HubUI.Btn(actionPanel, "Back", "BACK", null, HubTheme.Panel, HubTheme.Marble, 0.74f, 0.04f, 0.99f, 0.72f, OnCancel, 18);
        }

        static string SkillSub(Skill s)
        {
            string kind;
            switch (s.Kind)
            {
                case SkillKind.Damage: kind = "DMG x" + s.Power.ToString("0.0#"); break;
                case SkillKind.Heal: kind = "HEAL x" + s.Power.ToString("0.0#"); break;
                default: kind = "STATUS"; break;
            }
            string target;
            switch (s.Target)
            {
                case TargetType.SingleEnemy: target = "1 musuh"; break;
                case TargetType.AllEnemies: target = "semua musuh"; break;
                case TargetType.SingleAlly: target = "1 kawan"; break;
                case TargetType.AllAllies: target = "semua kawan"; break;
                default: target = "diri"; break;
            }
            return kind + " | " + target;
        }

        // ---------- angka dan efek ----------

        void ShowEvents(List<BattleEvent> events)
        {
            var counts = new Dictionary<Unit, int>();
            for (int i = 0; i < events.Count; i++)
            {
                BattleEvent e = events[i];
                if (e.Target == null) continue;
                string text = null;
                Color col = Color.white;
                switch (e.Kind)
                {
                    case EventKind.Damage: text = "-" + e.Amount; col = HubTheme.Danger; break;
                    case EventKind.Bleed: text = "-" + e.Amount + " Luka"; col = new Color(0.8f, 0.45f, 0.8f); break;
                    case EventKind.Heal: text = "+" + e.Amount; col = Theme.Good; break;
                    case EventKind.StatusApplied: text = e.Text; col = HubTheme.Gold; break;
                    case EventKind.Info: text = e.Text; col = HubTheme.Marble; break;
                }
                if (string.IsNullOrEmpty(text)) continue;
                int k;
                counts.TryGetValue(e.Target, out k);
                counts[e.Target] = k + 1;
                host.StartCoroutine(PopupRoutine(e.Target, text, col, k));
            }
        }

        IEnumerator PopupRoutine(Unit u, string text, Color col, int slot)
        {
            Rect r = CardRect(u);
            float cx = r.x + r.width * 0.5f;
            float baseY = r.y + r.height * 0.55f + slot * 0.07f;
            Text t = UIFactory.Label(fxLayer, text, 26, col, TextAnchor.MiddleCenter, cx - 0.1f, baseY, cx + 0.1f, baseY + 0.1f);
            RectTransform rt = t.rectTransform;
            float dur = 1.0f, el = 0f;
            while (el < dur)
            {
                if (t == null) yield break;
                el += Time.unscaledDeltaTime;
                float k = Mathf.Min(1f, el / dur);
                float dy = k * 0.08f;
                rt.anchorMin = new Vector2(cx - 0.1f, baseY + dy);
                rt.anchorMax = new Vector2(cx + 0.1f, baseY + dy + 0.1f);
                Color c = col;
                c.a = 1f - k * k;
                t.color = c;
                yield return null;
            }
            if (t != null) UnityEngine.Object.Destroy(t.gameObject);
        }
    }
}