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
        RectTransform backdrop;                        // latar panggung, dibangun ulang tiap battle sesuai lingkungan
        Text stallText, bannerText, logText, synergyText, blessText;

        BattleSystem bs;
        Encounter enc;
        Stage stage = Stage.Done;
        Unit current;
        Unit hudUnit;                                  // unit yang ditampilkan di HUD bawah (giliran terakhir)
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

            // Layout seperti referensi baru:
            //   kiri   : urutan giliran (atas) + Synergy (bawah)
            //   tengah : panggung (atas) + HUD hero dan skill (bawah)
            //   kanan  : Blessing (atas) + log battle (bawah)
            // Relic ada di top bar (DungeonState.Equipment), bukan di layar ini.
            field = UIFactory.NewRect(Root, "Field");
            UIFactory.Stretch(field, 0.16f, 0.29f, 0.845f, 0.995f);
            backdrop = UIFactory.NewRect(field, "Backdrop");
            UIFactory.Stretch(backdrop, 0f, 0f, 1f, 1f);
            cardsRoot = UIFactory.NewRect(field, "Cards");
            UIFactory.Stretch(cardsRoot, 0f, 0f, 1f, 1f);
            fxLayer = UIFactory.NewRect(field, "Fx");
            UIFactory.Stretch(fxLayer, 0f, 0f, 1f, 1f);
            bannerText = UIFactory.Label(field, "", 24, HubTheme.Marble, TextAnchor.MiddleCenter, 0.1f, 0.88f, 0.9f, 0.99f);

            // Kiri atas: ronde dan urutan giliran (vertikal).
            var orderCard = HubUI.Card(Root, "OrderPanel", HubTheme.Deep, 0.005f, 0.37f, 0.155f, 0.995f, HubTheme.Bronze);
            orderBar = orderCard.rectTransform;
            stallText = UIFactory.Label(Root, "", 15, HubTheme.Gold, TextAnchor.MiddleCenter, 0.17f, 0.945f, 0.835f, 0.995f);

            // Kiri bawah: Synergy aktif.
            var synCard = HubUI.Card(Root, "SynergyPanel", HubTheme.Deep, 0.005f, 0.005f, 0.155f, 0.365f, HubTheme.Bronze);
            synergyText = UIFactory.Label(synCard.transform, "", 13, HubTheme.Marble, TextAnchor.UpperLeft, 0.06f, 0.03f, 0.96f, 0.97f);

            // Bawah tengah: HUD hero (potret, stat, skill).
            var hud = HubUI.Card(Root, "Hud", HubTheme.Deep, 0.16f, 0.005f, 0.845f, 0.285f, HubTheme.Bronze);
            actionPanel = hud.rectTransform;

            // Kanan atas: Blessing yang dimiliki.
            var blCard = HubUI.Card(Root, "BlessingPanel", HubTheme.Deep, 0.85f, 0.60f, 0.995f, 0.995f, HubTheme.Bronze);
            blessText = UIFactory.Label(blCard.transform, "", 13, HubTheme.Marble, TextAnchor.UpperLeft, 0.06f, 0.03f, 0.96f, 0.97f);

            // Kanan bawah: log battle.
            var lb = HubUI.Card(Root, "LogBox", HubTheme.Deep, 0.85f, 0.005f, 0.995f, 0.595f, HubTheme.Bronze);
            logPanel = lb.rectTransform;
            logText = UIFactory.Label(logPanel, "", 13, HubTheme.Marble, TextAnchor.LowerLeft, 0.06f, 0.02f, 0.96f, 0.98f);

            resultLayer = UIFactory.NewRect(Root, "Result");
            UIFactory.Stretch(resultLayer, 0f, 0f, 1f, 1f);
            resultLayer.gameObject.SetActive(false);

            Root.gameObject.SetActive(false);
        }

        // Tampilan latar per varian Battle Island (GDD bagian 4).
        struct Look
        {
            public string SkyTop, SkyBottom, Ground, Fog, Bars;
            public int Count;
            public float MinW, MaxW;
            public bool Hanging;      // true = menggantung dari atas (stalaktit gua)
        }

        static Look LookFor(BattleEnvironment env)
        {
            switch (env)
            {
                case BattleEnvironment.OpenSea:     // dek kapal: laut biru, tiang kapal
                    return new Look { SkyTop = "#1D3550", SkyBottom = "#3C6A8C", Ground = "#4A3A2A", Fog = "#A9C7DC", Bars = "#2B1F17", Count = 3, MinW = 0.008f, MaxW = 0.014f };
                case BattleEnvironment.CoastalRaid: // pantai: tebing dan pasir
                    return new Look { SkyTop = "#3B5A66", SkyBottom = "#7E9AA3", Ground = "#6B5E45", Fog = "#E6EEF0", Bars = "#2E3F46", Count = 5, MinW = 0.03f, MaxW = 0.07f };
                case BattleEnvironment.CaveLair:    // gua: stalaktit
                    return new Look { SkyTop = "#121216", SkyBottom = "#2C2B2F", Ground = "#2A2724", Fog = "#55505A", Bars = "#0E0E12", Count = 12, MinW = 0.012f, MaxW = 0.04f, Hanging = true };
                case BattleEnvironment.Underworld:  // dunia bawah: pilar pucat dan kabut
                    return new Look { SkyTop = "#1B2036", SkyBottom = "#3E4058", Ground = "#2A2C40", Fog = "#8890C0", Bars = "#50557A", Count = 8, MinW = 0.02f, MaxW = 0.05f };
                default:                            // hutan (seperti referensi): teal gelap, batang pohon, kabut
                    return new Look { SkyTop = "#0F2E33", SkyBottom = "#1F5A58", Ground = "#1B2E2C", Fog = "#6FA8A0", Bars = "#0A1F22", Count = 16, MinW = 0.012f, MaxW = 0.04f };
            }
        }

        // Art opsional dari Assets/Resources/<path>.png (impor sebagai "Sprite (2D and UI)"). Kalau belum ada, dipakai placeholder.
        static readonly Dictionary<string, Sprite> artCache = new Dictionary<string, Sprite>();

        static Sprite LoadArt(string path)
        {
            Sprite s;
            if (!artCache.TryGetValue(path, out s))
            {
                s = Resources.Load<Sprite>(path);
                artCache[path] = s;
            }
            return s;
        }

        static Image Ellipse(Transform parent, string name, Color c, float x0, float y0, float x1, float y1)
        {
            var rt = UIFactory.NewRect(parent, name);
            UIFactory.Stretch(rt, x0, y0, x1, y1);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UIFactory.CircleSprite;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        void ApplyEnvironment(BattleEnvironment env)
        {
            UIFactory.ClearChildren(backdrop);

            // Kalau artist sudah menaruh Assets/Resources/Battle/Backgrounds/<OpenSea|CoastalRaid|CaveLair|Underworld|OpenWilds>.png, pakai itu.
            Sprite art = LoadArt("Battle/Backgrounds/" + env);
            if (art != null)
            {
                var img = HubUI.Flat(backdrop, "Art", Color.white, 0f, 0f, 1f, 1f);
                img.sprite = art;
                img.preserveAspect = false;
                return;
            }

            // Placeholder: langit bergradasi, batang pohon (atau tiang, tebing, stalaktit) berlapis jauh-dekat,
            // tanah gelap, kabut tipis, bercak tanah, dan vinyet di tepi. Seed tetap supaya latarnya sama tiap kali.
            Look k = LookFor(env);
            var rng = new System.Random((int)env * 7919 + 13);
            Color top = Theme.Hex(k.SkyTop), bottom = Theme.Hex(k.SkyBottom);
            const int bands = 10;
            for (int i = 0; i < bands; i++)
                HubUI.Flat(backdrop, "Sky" + i, Color.Lerp(bottom, top, (i + 0.5f) / bands), 0f, i / (float)bands, 1f, (i + 1) / (float)bands);

            Color barCol = Theme.Hex(k.Bars);
            Color mid = Color.Lerp(top, bottom, 0.5f);
            for (int i = 0; i < k.Count; i++)
            {
                float far = (float)rng.NextDouble();                         // 0 = dekat, 1 = jauh
                float w = Mathf.Lerp(k.MinW, k.MaxW, 1f - far * 0.6f);
                float x = (float)rng.NextDouble() * (1f - w);
                Color c = Color.Lerp(barCol, mid, far * 0.65f);
                c.a = Mathf.Lerp(0.95f, 0.6f, far);
                float y0 = k.Hanging ? 0.50f + (float)rng.NextDouble() * 0.30f : 0.30f;
                float y1 = k.Hanging ? 1f : 0.78f + (float)rng.NextDouble() * 0.22f;
                HubUI.Flat(backdrop, "Bar" + i, c, x, y0, x + w, y1);
            }

            HubUI.Flat(backdrop, "Ground", Theme.Hex(k.Ground), 0f, 0f, 1f, 0.42f);
            Color fog = Theme.Hex(k.Fog);
            HubUI.Flat(backdrop, "FogA", new Color(fog.r, fog.g, fog.b, 0.10f), 0f, 0.36f, 1f, 0.50f);
            HubUI.Flat(backdrop, "FogB", new Color(fog.r, fog.g, fog.b, 0.07f), 0f, 0.50f, 1f, 0.64f);
            HubUI.Flat(backdrop, "Shade", new Color(0f, 0f, 0f, 0.28f), 0f, 0f, 1f, 0.12f);
            for (int i = 0; i < 9; i++)
            {
                float cx = (float)rng.NextDouble();
                float cy = 0.06f + (float)rng.NextDouble() * 0.30f;
                float w = 0.05f + (float)rng.NextDouble() * 0.09f;
                Ellipse(backdrop, "Patch" + i, new Color(0f, 0f, 0f, 0.14f), cx - w, cy - w * 0.2f, cx + w, cy + w * 0.2f);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = 0.30f - i * 0.07f, x = i * 0.025f;
                HubUI.Flat(backdrop, "VigL" + i, new Color(0f, 0f, 0f, a), x, 0f, x + 0.025f, 1f);
                HubUI.Flat(backdrop, "VigR" + i, new Color(0f, 0f, 0f, a), 1f - x - 0.025f, 0f, 1f - x, 1f);
            }
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
                if (lineup.Count > 0) toast("Party not ready: ready characters were picked automatically.");
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
            bs.ApplyRelics(DungeonState.Equipment);   // relic dari top bar memberi bonus stat sebelum Begin()
            bs.SetResources(RunState.Food, RunState.Gold);
            log.Clear();
            usedBlessings.Clear();
            bs.OnLog += AddLog;
            bs.Begin();

            onFinished = finished;
            current = null;
            hudUnit = null;
            pendingSkill = null;
            stage = Stage.Busy;
            Active = true;
            resultLayer.gameObject.SetActive(false);
            Root.gameObject.SetActive(true);
            Root.SetAsLastSibling();
            ApplyEnvironment(env);
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
            if (fled > 0) extra += "Enemies fled: " + fled + " (no reward).\n";
            if (lostFood + lostGold > 0) extra += "Stolen goods lost: Food " + lostFood + ", Gold " + lostGold + ".\n";

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
            if (stage != Stage.Choose && stage != Stage.Target) return;   // saat memilih target, skill lain boleh dipilih
            if (s.NeedsTargetPick)
            {
                pendingSkill = s;
                stage = Stage.Target;
                Redraw();
            }
            else
            {
                pendingSkill = null;
                DoSkill(s, null);
            }
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
            if (BlessingsAvailable() == 0) { toast("No Blessing available."); return; }
            stage = Stage.Blessing;
            Redraw();
        }

        void OnBlessing(int index)
        {
            if (stage != Stage.Blessing) return;
            if (usedBlessings.Contains(index)) { toast("This Blessing was already used this battle."); return; }
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
            DrawSynergies();
            DrawBlessings();
            DrawActions();
            logText.text = string.Join("\n", log.ToArray());
        }

        void AddLog(string s)
        {
            log.Add(s);
            while (log.Count > 9) log.RemoveAt(0);
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
            UIFactory.Label(orderBar, "ROUND " + bs.Round, 16, HubTheme.Gold, TextAnchor.MiddleCenter, 0f, 0.935f, 1f, 1f);
            var order = bs.TurnOrder;
            int n = Mathf.Min(order.Count, 8);
            const float rowH = 0.112f;
            for (int i = 0; i < n; i++)
            {
                Unit u = order[i];
                float y1 = 0.93f - i * rowH, y0 = y1 - rowH + 0.012f;
                Color fill = u.IsEnemy ? Color.Lerp(HubTheme.Deep, HubTheme.Danger, 0.7f) : Color.Lerp(HubTheme.Deep, HubTheme.Teal, 0.8f);
                var chip = UIFactory.Panel(orderBar, "Chip" + i, fill, 0.06f, y0, 0.36f, y1, false);
                HubTheme.Frame(chip, u == current ? HubTheme.Gold : HubTheme.Bronze);
                UIFactory.Label(chip.transform, ShortTag(u), 16, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);
                float mid = y0 + (y1 - y0) * 0.5f;
                UIFactory.Label(orderBar, DisplayName(u), 12, u == current ? HubTheme.Gold : HubTheme.Marble, TextAnchor.LowerLeft, 0.40f, mid, 1f, y1);
                UIFactory.Label(orderBar, "SPD " + u.Speed, 11, HubTheme.Muted, TextAnchor.UpperLeft, 0.40f, y0, 1f, mid);
            }
        }

        // Formasi dua baris per sisi seperti Limbus Company: barisan depan lebih rendah dan besar, barisan belakang
        // lebih tinggi, lebih kecil, dan bergeser setengah kolom. Unit ke-1 paling dekat dengan tengah panggung.
        const float FrontFeetY = 0.23f;     // tinggi kaki barisan depan (koordinat field 0..1)
        const float BackLift = 0.12f;       // barisan belakang berdiri lebih tinggi sebesar ini
        const float BackScale = 0.88f;      // dan sedikit lebih kecil

        Rect FigureRect(Unit u)
        {
            bool back;
            return FigureRect(u, out back);
        }

        Rect FigureRect(Unit u, out bool back)
        {
            List<Unit> list = u.IsEnemy ? bs.Enemies : bs.Allies;
            int n = Mathf.Max(1, list.Count);
            int i = Mathf.Max(0, list.IndexOf(u));
            const float areaW = 0.47f;
            float areaX0 = u.IsEnemy ? 0.51f : 0.02f;

            if (u.IsEnemy && n == 1)                       // musuh tunggal (mis. Polyphemus): besar di tengah area musuh
            {
                back = false;
                const float bw = 0.25f;
                return new Rect(areaX0 + areaW * 0.5f - bw * 0.5f, FrontFeetY, bw, 0.64f);
            }

            back = (i % 2) == 1;                           // selang-seling: depan, belakang, depan, belakang
            int perRow = Mathf.Max(2, (n + 1) / 2);
            float colW = areaW / (perRow + 0.5f);          // sisakan setengah kolom untuk barisan belakang yang bergeser
            float slot = (i / 2) + 0.5f + (back ? 0.5f : 0f);
            float cx = u.IsEnemy ? areaX0 + slot * colW : areaX0 + areaW - slot * colW;

            float scale = back ? BackScale : 1f;
            // Panggung sekarang lebih sempit dan lebih tinggi dari layout lama, jadi lebar diperbesar dan rasio tinggi disesuaikan.
            float w = Mathf.Min(0.126f, (colW * 0.5f - 0.012f) * 1.4f) * scale;
            float h = w * 3.78f;
            float feet = FrontFeetY + (back ? BackLift : 0f);
            return new Rect(cx - w * 0.5f, feet, w, h);
        }

        static Color TextOn(Color c)
        {
            return c.grayscale > 0.55f ? HubTheme.Night : Color.white;
        }

        void DrawCards()
        {
            UIFactory.ClearChildren(cardsRoot);
            List<Unit> valid = null;
            if (stage == Stage.Target && pendingSkill != null && current != null)
                valid = bs.ValidTargets(pendingSkill, current);
            for (int pass = 0; pass < 2; pass++)           // barisan belakang digambar dulu supaya tertutup barisan depan
            {
                bool wantBack = pass == 0;
                for (int i = 0; i < bs.Enemies.Count; i++)
                {
                    bool b;
                    FigureRect(bs.Enemies[i], out b);
                    if (b == wantBack) DrawFigure(bs.Enemies[i], valid);
                }
                for (int i = 0; i < bs.Allies.Count; i++)
                {
                    bool b;
                    FigureRect(bs.Allies[i], out b);
                    if (b == wantBack) DrawFigure(bs.Allies[i], valid);
                }
            }
        }

        void DrawFigure(Unit u, List<Unit> valid)
        {
            bool back;
            Rect r = FigureRect(u, out back);
            bool targetable = valid != null && valid.Contains(u);
            Color dark = new Color(0f, 0f, 0f, 0.55f);

            var fig = UIFactory.NewRect(cardsRoot, "Fig_" + u.Name);
            UIFactory.Stretch(fig, r.x, r.y, r.x + r.width, r.y + r.height);

            // Lingkaran di tanah seperti Limbus: oranye kawan, merah musuh, emas untuk yang sedang jalan, merah terang untuk target.
            Color ring = u.IsEnemy ? new Color(0.78f, 0.22f, 0.18f, 1f) : new Color(0.95f, 0.55f, 0.16f, 1f);
            if (u == current) ring = new Color(1f, 0.84f, 0.29f, 1f);
            if (targetable) ring = new Color(1f, 0.23f, 0.19f, 1f);
            if (!u.Present) ring = new Color(0.30f, 0.30f, 0.32f, 0.8f);
            if (u.Present && (u == current || targetable))
                Ellipse(fig, "Glow", new Color(ring.r, ring.g, ring.b, 0.30f), -0.34f, -0.10f, 1.34f, 0.12f);
            Ellipse(fig, "Ring", ring, -0.22f, -0.07f, 1.22f, 0.09f);
            Ellipse(fig, "RingHole", new Color(0f, 0f, 0f, 0.55f), -0.16f, -0.05f, 1.16f, 0.07f);

            // Info di tanah tepat di bawah kaki: bar HP, angka HP, nama, status.
            var info = UIFactory.NewRect(cardsRoot, "Info_" + u.Name);
            UIFactory.Stretch(info, r.x - 0.012f, r.y - 0.215f, r.x + r.width + 0.012f, r.y - 0.045f);

            if (!u.Present)
            {
                var fallen = UIFactory.Panel(fig, "Fallen", new Color(0.2f, 0.2f, 0.22f, 0.85f), 0.05f, 0.02f, 0.95f, 0.14f, false);
                fallen.raycastTarget = false;
                UIFactory.Label(fig, u.Fled ? "FLED" : "DOWN", 20, u.Fled ? HubTheme.Muted : HubTheme.Danger,
                                TextAnchor.MiddleCenter, 0f, 0.16f, 1f, 0.32f);
                UIFactory.Label(info, DisplayName(u), 13, HubTheme.Muted, TextAnchor.MiddleCenter, 0f, 0.24f, 1f, 0.46f);
                return;
            }

            // Sprite karakter dari Assets/Resources/Battle/Characters/<id>.png kalau ada; kalau belum, blok warna placeholder.
            Color pc = Theme.Hex(u.Data.PortraitHex);
            Sprite art = LoadArt("Battle/Characters/" + u.Data.Id);
            if (art != null)
            {
                var rt = UIFactory.NewRect(fig, "Art");
                UIFactory.Stretch(rt, -0.25f, 0f, 1.25f, 1.05f);
                var im = rt.gameObject.AddComponent<Image>();
                im.sprite = art;
                im.preserveAspect = true;
                im.raycastTarget = false;
            }
            else
            {
                var body = UIFactory.Panel(fig, "Body", pc, 0.14f, 0.04f, 0.86f, 0.66f, false);
                body.raycastTarget = false;
                HubTheme.Frame(body, dark);
                var head = UIFactory.Panel(fig, "Head", Color.Lerp(pc, Color.white, 0.25f), 0.30f, 0.62f, 0.70f, 0.90f, false);
                head.sprite = UIFactory.CircleSprite;
                head.type = Image.Type.Simple;
                head.raycastTarget = false;
                HubTheme.Frame(head, dark);
                UIFactory.Label(body.transform, HubUI.Initials(u.Name), 30, TextOn(pc), TextAnchor.MiddleCenter, 0f, 0.1f, 1f, 0.9f);
            }

            float f = u.MaxHp > 0 ? (float)u.Hp / u.MaxHp : 0f;
            UIFactory.Bar(info, f, u.IsEnemy ? HubTheme.Danger : Theme.Good, 0.04f, 0.72f, 0.96f, 0.92f);
            UIFactory.Label(info, u.Hp + " / " + u.MaxHp, 13, HubTheme.Marble, TextAnchor.MiddleCenter, 0f, 0.48f, 1f, 0.70f);
            UIFactory.Label(info, DisplayName(u), 13, HubTheme.Gold, TextAnchor.MiddleCenter, -0.1f, 0.24f, 1.1f, 0.46f);
            UIFactory.Label(info, StatusLine(u), 11, HubTheme.Muted, TextAnchor.MiddleCenter, -0.1f, 0.0f, 1.1f, 0.22f);

            // Area klik untuk memilih target (merah transparan; menyala saat disorot).
            if (targetable)
            {
                var hit = UIFactory.Panel(cardsRoot, "Hit_" + u.Name, Color.white,
                                          r.x - 0.012f, r.y - 0.215f, r.x + r.width + 0.012f, r.y + r.height, false);
                var b = hit.gameObject.AddComponent<Button>();
                b.targetGraphic = hit;
                HubUI.Tint(b, new Color(0.9f, 0.15f, 0.15f, 0.16f));
                hit.gameObject.AddComponent<ButtonFx>();
                Unit tgt = u;
                b.onClick.AddListener(() => OnTarget(tgt));
            }
        }

        static string StatusLine(Unit u)
        {
            if (u.Statuses.Count == 0) return "";
            var parts = new List<string>();
            for (int i = 0; i < u.Statuses.Count; i++)
                parts.Add(Unit.StatusShort(u.Statuses[i].Type) + " " + u.Statuses[i].TurnsLeft);
            return string.Join("  ", parts.ToArray());
        }

        // Synergy yang aktif untuk party ini (komposisi karakter). Daftar lengkap ada di SynergyCatalog.
        void DrawSynergies()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("SYNERGY");
            if (bs.ActiveSynergies.Count == 0) sb.AppendLine("None active.");
            for (int i = 0; i < bs.ActiveSynergies.Count; i++)
            {
                var a = bs.ActiveSynergies[i];
                sb.AppendLine(a.Name);
                sb.AppendLine("  " + a.ModsLabel);
            }
            synergyText.text = sb.ToString();
        }

        // Blessing yang dimiliki (dari shop atau God Realm). Dipakai lewat tombol BLESSING di HUD, memakai giliran hero.
        void DrawBlessings()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("BLESSINGS");
            if (DungeonState.God.Count == 0) sb.AppendLine("None.");
            for (int i = 0; i < DungeonState.God.Count && i < 6; i++)
            {
                var g = DungeonState.God[i];
                if (g == null) continue;
                sb.AppendLine(g.Name + (usedBlessings.Contains(i) ? " (used)" : ""));
                sb.AppendLine("  " + g.BattlesLeft + " battles left");
            }
            blessText.text = sb.ToString();
        }

        void DrawStall()
        {
            if (bs.IsStalling)
            {
                stallText.text = "STALLING! Enemies can call reinforcements.";
                stallText.color = HubTheme.Danger;
            }
            else if (bs.StallCount >= 1)
            {
                stallText.text = "Stall " + bs.StallCount + "/" + BattleSystem.StallTurns + ": damage the enemy!";
                stallText.color = HubTheme.Gold;
            }
            else stallText.text = "";
        }

        void DrawActions()
        {
            UIFactory.ClearChildren(actionPanel);
            if (current != null) hudUnit = current;
            if (hudUnit != null) DrawHudUnit(hudUnit);

            switch (stage)
            {
                case Stage.Choose:
                    DrawSkillRow();
                    DrawSkillNotes(null);
                    int avail = BlessingsAvailable();
                    HudButton("Blessing", "BLESSING", avail + " available", OnBlessingMenu, avail > 0 ? HubTheme.Gold : HubTheme.Muted);
                    break;
                case Stage.Target:
                    DrawSkillRow();
                    DrawSkillNotes(pendingSkill);
                    HudButton("Cancel", "CANCEL", null, OnCancel, HubTheme.Marble);
                    break;
                case Stage.Blessing:
                    DrawBlessingRow();
                    HudNote("Pick a Blessing (once per battle, costs the turn).");
                    HudButton("Back", "BACK", null, OnCancel, HubTheme.Marble);
                    break;
                case Stage.Busy:
                    HudNote(current != null && current.IsEnemy ? DisplayName(current) + "'s turn..." : "...");
                    break;
                default:
                    HudNote("Battle over.");
                    break;
            }
        }

        // Potret, nama, role, HP, dan stat unit yang sedang jalan (kiri HUD).
        void DrawHudUnit(Unit u)
        {
            Color pc = Theme.Hex(u.Data.PortraitHex);
            var portrait = UIFactory.Panel(actionPanel, "Portrait", pc, 0.012f, 0.50f, 0.105f, 0.95f, false);
            HubTheme.Frame(portrait, u.IsEnemy ? HubTheme.Danger : HubTheme.Bronze);
            UIFactory.Label(portrait.transform, HubUI.Initials(u.Name), 34, TextOn(pc), TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f);
            UIFactory.Label(actionPanel, DisplayName(u), 24, HubTheme.Gold, TextAnchor.LowerLeft, 0.118f, 0.72f, 0.40f, 0.95f);
            UIFactory.Label(actionPanel, UnitData.RoleLabel(u.Data.Role), 15, HubTheme.Muted, TextAnchor.UpperLeft, 0.118f, 0.50f, 0.40f, 0.72f);

            float f = u.MaxHp > 0 ? (float)u.Hp / u.MaxHp : 0f;
            UIFactory.Bar(actionPanel, f, u.IsEnemy ? HubTheme.Danger : Theme.Good, 0.012f, 0.38f, 0.30f, 0.45f);
            UIFactory.Label(actionPanel, "HP " + u.Hp + " / " + u.MaxHp, 14, HubTheme.Marble, TextAnchor.MiddleLeft, 0.012f, 0.26f, 0.30f, 0.38f);
            string stats = "ATK " + Mathf.RoundToInt(u.Data.Atk * u.AtkMult)
                         + "    DEF " + Mathf.RoundToInt(u.Data.Def * u.DefMult)
                         + "    SPD " + u.Speed + " (" + u.SpeedMin + "-" + u.SpeedMax + ")";
            UIFactory.Label(actionPanel, stats, 14, HubTheme.Marble, TextAnchor.MiddleLeft, 0.012f, 0.14f, 0.30f, 0.26f);
            UIFactory.Label(actionPanel, StatusLine(u), 12, HubTheme.Gold, TextAnchor.MiddleLeft, 0.012f, 0.02f, 0.30f, 0.14f);
        }

        // Empat skill terpasang sebagai tombol (atas kanan HUD). Skill yang sedang dipilih untuk target ditandai.
        void DrawSkillRow()
        {
            var skills = current.Data.GetEquipped();
            for (int i = 0; i < skills.Count && i < 4; i++)
            {
                Skill s = skills[i];
                float x0 = 0.42f + i * 0.145f, x1 = x0 + 0.135f;
                bool picked = stage == Stage.Target && pendingSkill == s;
                HubUI.Btn(actionPanel, "Skill" + i, s.Name, SkillSub(s), picked ? HubTheme.Bronze : HubTheme.Panel, HubTheme.Marble,
                          x0, 0.50f, x1, 0.95f, () => OnSkill(s), 15);
            }
        }

        // Keterangan semua skill terpasang (tengah bawah HUD), supaya tidak perlu hover.
        void DrawSkillNotes(Skill selected)
        {
            var skills = current.Data.GetEquipped();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(selected != null ? "Pick a target for " + selected.Name + "." : DisplayName(current) + "  -  choose an action");
            for (int i = 0; i < skills.Count && i < 4; i++)
                sb.AppendLine((i + 1) + ". " + skills[i].Name + ": " + (skills[i].Description ?? ""));
            UIFactory.Label(actionPanel, sb.ToString(), 13, selected != null ? HubTheme.Gold : HubTheme.Marble,
                            TextAnchor.UpperLeft, 0.32f, 0.03f, 0.86f, 0.47f);
        }

        void DrawBlessingRow()
        {
            for (int i = 0; i < DungeonState.God.Count && i < 3; i++)
            {
                var r = DungeonState.God[i];
                if (r == null) continue;
                int idx = i;
                bool used = usedBlessings.Contains(i);
                float x0 = 0.42f + i * 0.19f, x1 = x0 + 0.18f;
                HubUI.Btn(actionPanel, "Bless" + i, r.Name, used ? "Used" : r.BattlesLeft + " battles left",
                          HubTheme.Panel, used ? HubTheme.Muted : HubTheme.Marble, x0, 0.50f, x1, 0.95f, () => OnBlessing(idx), 15);
            }
        }

        void HudNote(string text)
        {
            UIFactory.Label(actionPanel, text, 18, HubTheme.Gold, TextAnchor.MiddleLeft, 0.42f, 0.20f, 0.86f, 0.80f);
        }

        // Tombol aksi di kanan bawah HUD (Blessing / Cancel / Back).
        void HudButton(string name, string title, string sub, Action onClick, Color textColor)
        {
            HubUI.Btn(actionPanel, name, title, sub, HubTheme.Panel, textColor, 0.87f, 0.04f, 0.99f, 0.46f, onClick, 16);
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
                case TargetType.SingleEnemy: target = "1 enemy"; break;
                case TargetType.AllEnemies: target = "all enemies"; break;
                case TargetType.SingleAlly: target = "1 ally"; break;
                case TargetType.AllAllies: target = "all allies"; break;
                default: target = "self"; break;
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
                    case EventKind.Bleed: text = "-" + e.Amount + " Bleed"; col = new Color(0.8f, 0.45f, 0.8f); break;
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
            Rect r = FigureRect(u);
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