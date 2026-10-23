using System;
using System.Collections.Generic;

namespace Oddyseus.Core
{
    public class BattleEvent
    {
        public Unit Target;
        public int Amount;
        public EventKind Kind;
        public string Text;
        public bool Killed;
    }

    public class ChosenAction
    {
        public Skill Skill;
        public Unit Target;   // null untuk skill AllEnemies/AllAllies/Self
    }

    // Aksi yang dipilih AI untuk satu giliran musuh.
    public class EnemyAction
    {
        public EnemyActionType Type;
        public Skill Skill;   // null untuk Defend generik, Steal, Flee, CallReinforcements
        public Unit Target;   // null untuk skill AllEnemies/AllAllies/Self
        public string Label;  // teks singkat untuk UI, mis. "Slash" atau "Kabur"
    }

    // Logika battle murni (tanpa UnityEngine) supaya mudah dites.
    // Aturan:
    //  - Initiative di-roll ulang setiap ronde (Speed + acak). Satu ronde selesai saat semua unit yang hadir sudah bertindak.
    //  - Satu giliran = satu aksi. Pemain: 4 skill ter-equip atau Blessing. Tidak ada kabur.
    //  - Musuh: Attack, Defend, Buff, Debuff, Heal, Steal (sesuai EnemyBehavior) + Kabur dan Call Reinforcements (dengan syarat).
    //  - Stalling: 2 giliran pemain berturut-turut tanpa damage ke musuh.
    // Alur: Begin() -> loop { NextActor() -> (UseSkill | UseBlessing | PerformEnemyTurn) } sampai Outcome != Ongoing.
    public class BattleSystem
    {
        public const int InitiativeRoll = 20;            // initiative = Speed + acak 0..20
        public const float GuardCutDefault = 0.5f;
        public const int StallTurns = 2;
        public const int EnemyHealCooldown = 3;          // giliran musuh itu yang terblokir setelah Heal
        public const float HealHpThreshold = 0.5f;       // Heal hanya kalau ada sekutu di bawah 50% HP
        public const int MaxReinforcementCalls = 2;      // per battle
        public const float ReinforceWeightLone = 0.3f;   // musuh tinggal sendirian: langka
        public const float ReinforceWeightStalling = 3f; // pemain stalling: sering
        public const int StealFoodAmount = 1;
        public const int StealGoldAmount = 8;

        public readonly List<Unit> Allies = new List<Unit>();
        public readonly List<Unit> Enemies = new List<Unit>();
        public readonly List<Unit> All = new List<Unit>();
        public readonly List<BattleEvent> TurnStartEvents = new List<BattleEvent>();
        public readonly List<Unit> FledUnits = new List<Unit>();
        public BattleOutcome Outcome { get; private set; }
        public Unit Current { get; private set; }
        public int Round { get; private set; }
        public int StallCount { get; private set; }
        public int ReinforcementCalls { get; private set; }
        public int PlayerFood { get; private set; }
        public int PlayerGold { get; private set; }
        public string[] ReinforcementIds;                // id musuh yang bisa dipanggil; null = tidak ada bantuan
        public event Action<string> OnLog;

        public bool IsStalling { get { return StallCount >= StallTurns; } }

        readonly Random rng;
        List<Unit> order = new List<Unit>();
        int pos = -1;

        public BattleSystem(IEnumerable<Unit> allies, IEnumerable<Unit> enemies, int seed)
        {
            Allies.AddRange(allies);
            Enemies.AddRange(enemies);
            All.AddRange(Allies);
            All.AddRange(Enemies);
            rng = new Random(seed);
            Outcome = BattleOutcome.Ongoing;
        }

        void Log(string s)
        {
            if (OnLog != null) OnLog(s);
        }

        // Isi dengan RunState.Food dan RunState.Gold sebelum Begin(), supaya aksi Steal tahu apa yang bisa dicuri.
        public void SetResources(int food, int gold)
        {
            PlayerFood = Math.Max(0, food);
            PlayerGold = Math.Max(0, gold);
        }

        // Panggil sebelum RunState.CommitBattle. Curian yang dibawa kabur hilang permanen; curian dari musuh yang mati sudah kembali.
        public void ApplyResourcesToRun()
        {
            RunState.Food = PlayerFood;
            RunState.Gold = PlayerGold;
        }

        public void Begin()
        {
            Round = 0;
            pos = -1;
            StallCount = 0;
            ReinforcementCalls = 0;
            order = new List<Unit>();
            Current = null;
            CheckOutcome();
            if (Outcome == BattleOutcome.Ongoing) StartRound();
        }

        // Roll initiative semua unit yang hadir dan susun urutan ronde baru.
        void StartRound()
        {
            Round++;
            var roll = new Dictionary<Unit, float>();
            var list = new List<Unit>();
            for (int i = 0; i < All.Count; i++)
            {
                Unit u = All[i];
                if (!u.Present) continue;
                // pecahan kecil memutus seri secara acak
                roll[u] = u.Data.Speed + rng.Next(0, InitiativeRoll + 1) + (float)rng.NextDouble() * 0.5f;
                list.Add(u);
            }
            list.Sort((a, b) => roll[b].CompareTo(roll[a]));
            order = list;
            pos = -1;
            Log("Ronde " + Round + ".");
        }

        // Giliran tersisa di ronde ini, termasuk unit yang sedang bertindak. Untuk daftar urutan di kiri atas.
        public List<Unit> TurnOrder
        {
            get
            {
                var r = new List<Unit>();
                for (int i = Math.Max(0, pos); i < order.Count; i++)
                    if (order[i].Present) r.Add(order[i]);
                return r;
            }
        }

        // Unit berikutnya yang bertindak. Ronde baru (roll ulang) dimulai otomatis. Null kalau battle selesai.
        public Unit NextActor()
        {
            TurnStartEvents.Clear();
            int guard = 0;
            while (Outcome == BattleOutcome.Ongoing && guard++ < 1000)
            {
                pos++;
                if (pos >= order.Count)
                {
                    StartRound();
                    pos = 0;
                    if (order.Count == 0) { CheckOutcome(); break; }
                }
                Unit next = order[pos];
                if (!next.Present) continue;

                Current = next;
                StartTurn(next);
                CheckOutcome();
                if (Outcome != BattleOutcome.Ongoing) { Current = null; return null; }
                if (next.Present) return next;
            }
            Current = null;
            return null;
        }

        // Awal giliran: cooldown Heal turun, Bleed berjalan, lalu durasi semua status berkurang.
        void StartTurn(Unit u)
        {
            if (u.HealCooldown > 0) u.HealCooldown--;
            var bleed = u.Get(StatusType.Bleed);
            if (bleed != null)
            {
                int dmg = Math.Max(1, (int)Math.Round(u.MaxHp * bleed.Potency));
                ApplyDamage(u, dmg, TurnStartEvents, EventKind.Bleed);
                Log(u.Name + " terkena Luka: -" + dmg + " HP.");
            }
            for (int i = u.Statuses.Count - 1; i >= 0; i--)
            {
                u.Statuses[i].TurnsLeft--;
                if (u.Statuses[i].TurnsLeft <= 0) u.Statuses.RemoveAt(i);
            }
            if (!u.Alive) u.Statuses.Clear();
        }

        List<Unit> Opponents(Unit actor) { return actor.IsEnemy ? Allies : Enemies; }
        List<Unit> Friends(Unit actor) { return actor.IsEnemy ? Enemies : Allies; }

        public bool IsValidTarget(Skill skill, Unit actor, Unit target)
        {
            if (target == null || !target.Present) return false;
            switch (skill.Target)
            {
                case TargetType.SingleEnemy: return target.IsEnemy != actor.IsEnemy;
                case TargetType.SingleAlly: return target.IsEnemy == actor.IsEnemy;
                case TargetType.Self: return target == actor;
            }
            return false;
        }

        public List<Unit> ValidTargets(Skill skill, Unit actor)
        {
            var list = new List<Unit>();
            for (int i = 0; i < All.Count; i++)
                if (IsValidTarget(skill, actor, All[i])) list.Add(All[i]);
            return list;
        }

        List<Unit> ResolveTargets(Skill skill, Unit actor, Unit chosen)
        {
            var list = new List<Unit>();
            switch (skill.Target)
            {
                case TargetType.AllEnemies:
                    foreach (var u in Opponents(actor)) if (u.Present) list.Add(u);
                    break;
                case TargetType.AllAllies:
                    foreach (var u in Friends(actor)) if (u.Present) list.Add(u);
                    break;
                case TargetType.Self:
                    list.Add(actor);
                    break;
                default:
                    if (chosen != null && chosen.Present) list.Add(chosen);
                    break;
            }
            return list;
        }

        // Jalankan skill oleh Current. Mengembalikan daftar event untuk ditampilkan UI.
        public List<BattleEvent> UseSkill(Skill skill, Unit chosenTarget)
        {
            var events = new List<BattleEvent>();
            if (Current == null || Outcome != BattleOutcome.Ongoing) return events;
            Unit actor = Current;

            if (skill.NeedsTargetPick && !IsValidTarget(skill, actor, chosenTarget))
            {
                Log("Target tidak valid.");
                return events;
            }

            Log(actor.Name + " memakai " + skill.Name + ".");
            var targets = ResolveTargets(skill, actor, chosenTarget);
            for (int i = 0; i < targets.Count; i++)
            {
                Unit t = targets[i];
                if (skill.Kind == SkillKind.Damage)
                {
                    int dmg = CalcDamage(actor, t, skill.Power);
                    ApplyDamage(t, dmg, events, EventKind.Damage);
                    Log("  " + t.Name + " -" + dmg + " HP" + (t.Alive ? "." : " dan gugur."));
                    if (skill.HasStatus && t.Alive) AddStatusEvent(t, skill, events);
                }
                else if (skill.Kind == SkillKind.Heal)
                {
                    int heal = Math.Max(1, (int)Math.Round(actor.Data.Atk * actor.AtkMult * skill.Power));
                    int before = t.Hp;
                    t.Hp = Math.Min(t.MaxHp, t.Hp + heal);
                    int real = t.Hp - before;
                    events.Add(new BattleEvent { Target = t, Amount = real, Kind = EventKind.Heal });
                    Log("  " + t.Name + " +" + real + " HP.");
                }
                else
                {
                    AddStatusEvent(t, skill, events);
                }
            }

            EndAction(actor, events);
            return events;
        }

        // Blessing memakai giliran karakter yang sedang bertindak. Efeknya diberikan pemanggil
        // (data Blessing belum ada). Kalau efeknya memberi damage ke musuh, stalling di-reset.
        public List<BattleEvent> UseBlessing(Func<BattleSystem, Unit, List<BattleEvent>> effect)
        {
            var events = new List<BattleEvent>();
            if (Current == null || Current.IsEnemy || Outcome != BattleOutcome.Ongoing) return events;
            Unit actor = Current;
            Log(actor.Name + " memakai Blessing.");
            if (effect != null)
            {
                var r = effect(this, actor);
                if (r != null) events.AddRange(r);
            }
            EndAction(actor, events);
            return events;
        }

        // Akhir aksi: perbarui hitungan stalling (hanya giliran pemain) dan cek hasil battle.
        void EndAction(Unit actor, List<BattleEvent> events)
        {
            if (!actor.IsEnemy)
            {
                bool hitEnemy = false;
                for (int i = 0; i < events.Count; i++)
                    if (events[i].Kind == EventKind.Damage && events[i].Target != null && events[i].Target.IsEnemy) hitEnemy = true;
                StallCount = hitEnemy ? 0 : StallCount + 1;
            }
            CheckOutcome();
        }

        void AddStatusEvent(Unit t, Skill skill, List<BattleEvent> events)
        {
            t.AddStatus(skill.Status, skill.Duration, skill.Potency);
            events.Add(new BattleEvent
            {
                Target = t, Kind = EventKind.StatusApplied,
                Text = Unit.StatusShort(skill.Status)
            });
            Log("  " + t.Name + ": " + Unit.StatusShort(skill.Status) + ".");
        }

        public int CalcDamage(Unit actor, Unit target, float power)
        {
            float raw = actor.Data.Atk * actor.AtkMult * power;
            float def = target.Data.Def * target.DefMult;
            float dmg = raw * 100f / (100f + Math.Max(0f, def));
            var guard = target.Get(StatusType.Guard);
            if (guard != null) dmg *= (1f - guard.Potency);
            return Math.Max(1, (int)Math.Round(dmg));
        }

        void ApplyDamage(Unit t, int dmg, List<BattleEvent> events, EventKind kind)
        {
            t.Hp = Math.Max(0, t.Hp - dmg);
            events.Add(new BattleEvent { Target = t, Amount = dmg, Kind = kind, Killed = !t.Alive });
            if (!t.Alive)
            {
                t.Statuses.Clear();
                Log(t.Name + " gugur.");
                // Pencuri yang mati mengembalikan curiannya.
                if (t.IsEnemy && (t.StolenFood > 0 || t.StolenGold > 0))
                {
                    PlayerFood += t.StolenFood;
                    PlayerGold += t.StolenGold;
                    events.Add(new BattleEvent { Target = t, Kind = EventKind.Info,
                        Text = "Curian kembali: Food " + t.StolenFood + ", Gold " + t.StolenGold });
                    Log("  Curian kembali ke pemain.");
                    t.StolenFood = 0;
                    t.StolenGold = 0;
                }
            }
        }

        void CheckOutcome()
        {
            bool anyAlly = false, anyEnemy = false;
            for (int i = 0; i < Allies.Count; i++) if (Allies[i].Present) anyAlly = true;
            for (int i = 0; i < Enemies.Count; i++) if (Enemies[i].Present) anyEnemy = true;
            if (!anyEnemy) Outcome = BattleOutcome.Victory;
            else if (!anyAlly) Outcome = BattleOutcome.Defeat;
            else Outcome = BattleOutcome.Ongoing;
        }

        // ---------- AI musuh ----------

        class Candidate
        {
            public EnemyAction Action;
            public float Weight;
        }

        int PresentEnemyCount()
        {
            int n = 0;
            for (int i = 0; i < Enemies.Count; i++) if (Enemies[i].Present) n++;
            return n;
        }

        bool Usable(Skill s, Unit actor)
        {
            if (s.NeedsTargetPick) return ValidTargets(s, actor).Count > 0;
            return ResolveTargets(s, actor, null).Count > 0;
        }

        // Pilih skill status yang masih berguna (ada target yang belum terkena statusnya).
        bool PickStatusSkill(List<Skill> skills, Unit actor, out Skill pick, out Unit pickTarget)
        {
            var okSkills = new List<Skill>();
            var okTargets = new List<Unit>();
            for (int i = 0; i < skills.Count; i++)
            {
                Skill s = skills[i];
                if (!s.HasStatus) continue;
                var pool = s.NeedsTargetPick ? ValidTargets(s, actor) : ResolveTargets(s, actor, null);
                var lacking = new List<Unit>();
                for (int k = 0; k < pool.Count; k++)
                    if (pool[k].Get(s.Status) == null) lacking.Add(pool[k]);
                if (lacking.Count == 0) continue;
                okSkills.Add(s);
                okTargets.Add(s.NeedsTargetPick ? lacking[rng.Next(lacking.Count)] : null);
            }
            if (okSkills.Count == 0) { pick = null; pickTarget = null; return false; }
            int idx = rng.Next(okSkills.Count);
            pick = okSkills[idx];
            pickTarget = okTargets[idx];
            return true;
        }

        // Heal hanya kalau cooldown habis dan ada sekutu di bawah HealHpThreshold.
        bool PickHeal(List<Skill> skills, Unit actor, out Skill pick, out Unit pickTarget)
        {
            pick = null; pickTarget = null;
            if (actor.HealCooldown > 0) return false;
            var okSkills = new List<Skill>();
            var okTargets = new List<Unit>();
            for (int i = 0; i < skills.Count; i++)
            {
                Skill s = skills[i];
                var pool = s.NeedsTargetPick ? ValidTargets(s, actor) : ResolveTargets(s, actor, null);
                Unit lowest = null;
                for (int k = 0; k < pool.Count; k++)
                {
                    if (pool[k].Hp >= pool[k].MaxHp * HealHpThreshold) continue;
                    if (lowest == null || (float)pool[k].Hp / pool[k].MaxHp < (float)lowest.Hp / lowest.MaxHp) lowest = pool[k];
                }
                if (lowest == null) continue;
                okSkills.Add(s);
                okTargets.Add(s.NeedsTargetPick ? lowest : null);
            }
            if (okSkills.Count == 0) return false;
            int idx = rng.Next(okSkills.Count);
            pick = okSkills[idx];
            pickTarget = okTargets[idx];
            return true;
        }

        public EnemyAction ChooseEnemyAction(Unit actor)
        {
            EnemyBehavior b = EnemyCatalog.BehaviorOf(actor.Data.Id);
            var skills = actor.Data.GetEquipped();
            var attack = new List<Skill>();
            var buff = new List<Skill>();
            var debuff = new List<Skill>();
            var heal = new List<Skill>();
            Skill guardSkill = null;
            for (int i = 0; i < skills.Count; i++)
            {
                Skill s = skills[i];
                if (s.Kind == SkillKind.Damage) attack.Add(s);
                else if (s.Kind == SkillKind.Heal) heal.Add(s);
                else if (s.Status == StatusType.Guard && s.Target == TargetType.Self) guardSkill = s;
                else if (s.Target == TargetType.SingleEnemy || s.Target == TargetType.AllEnemies) debuff.Add(s);
                else buff.Add(s);
            }

            var cands = new List<Candidate>();
            Skill sk; Unit tg;

            if (b.Attack > 0)
            {
                var usable = new List<Skill>();
                for (int i = 0; i < attack.Count; i++) if (Usable(attack[i], actor)) usable.Add(attack[i]);
                if (usable.Count > 0)
                {
                    sk = usable[rng.Next(usable.Count)];
                    tg = null;
                    if (sk.NeedsTargetPick)
                    {
                        var t = ValidTargets(sk, actor);
                        tg = t[rng.Next(t.Count)];
                    }
                    cands.Add(new Candidate { Weight = b.Attack, Action = new EnemyAction { Type = EnemyActionType.Attack, Skill = sk, Target = tg, Label = sk.Name } });
                }
            }
            if (b.Defend > 0 && actor.Get(StatusType.Guard) == null)
                cands.Add(new Candidate { Weight = b.Defend, Action = new EnemyAction { Type = EnemyActionType.Defend, Skill = guardSkill, Label = guardSkill != null ? guardSkill.Name : "Defend" } });
            if (b.Buff > 0 && PickStatusSkill(buff, actor, out sk, out tg))
                cands.Add(new Candidate { Weight = b.Buff, Action = new EnemyAction { Type = EnemyActionType.Buff, Skill = sk, Target = tg, Label = sk.Name } });
            if (b.Debuff > 0 && PickStatusSkill(debuff, actor, out sk, out tg))
                cands.Add(new Candidate { Weight = b.Debuff, Action = new EnemyAction { Type = EnemyActionType.Debuff, Skill = sk, Target = tg, Label = sk.Name } });
            if (b.Heal > 0 && PickHeal(heal, actor, out sk, out tg))
                cands.Add(new Candidate { Weight = b.Heal, Action = new EnemyAction { Type = EnemyActionType.Heal, Skill = sk, Target = tg, Label = sk.Name } });
            if (b.Steal > 0 && (PlayerFood > 0 || PlayerGold > 0))
                cands.Add(new Candidate { Weight = b.Steal, Action = new EnemyAction { Type = EnemyActionType.Steal, Label = "Steal" } });

            // Kabur: hanya kalau sendirian, atau perannya Debuffer atau Thief.
            bool alone = PresentEnemyCount() <= 1;
            if (b.CanFlee && b.Flee > 0 && (alone || b.Role != EnemyRole.Fighter))
            {
                float w = b.Flee;
                if (alone && actor.Hp < actor.MaxHp * 0.4f) w *= 3f;
                cands.Add(new Candidate { Weight = w, Action = new EnemyAction { Type = EnemyActionType.Flee, Label = "Kabur" } });
            }

            // Call Reinforcements: hanya kalau tinggal 1 musuh (langka) atau pemain stalling (sering).
            if (b.CanCallReinforcements && ReinforcementIds != null && ReinforcementIds.Length > 0 &&
                ReinforcementCalls < MaxReinforcementCalls && (alone || IsStalling))
            {
                float w = IsStalling ? ReinforceWeightStalling : ReinforceWeightLone;
                cands.Add(new Candidate { Weight = w, Action = new EnemyAction { Type = EnemyActionType.CallReinforcements, Label = "Call Reinforcements" } });
            }

            if (cands.Count == 0)
                return new EnemyAction { Type = EnemyActionType.Defend, Skill = guardSkill, Label = "Defend" };

            float total = 0f;
            for (int i = 0; i < cands.Count; i++) total += cands[i].Weight;
            float r = (float)rng.NextDouble() * total;
            for (int i = 0; i < cands.Count; i++)
            {
                r -= cands[i].Weight;
                if (r <= 0f) return cands[i].Action;
            }
            return cands[cands.Count - 1].Action;
        }

        // Pilih dan jalankan satu giliran musuh (Current).
        public List<BattleEvent> PerformEnemyTurn()
        {
            if (Current == null || !Current.IsEnemy) return new List<BattleEvent>();
            return ExecuteEnemyAction(ChooseEnemyAction(Current));
        }

        public List<BattleEvent> ExecuteEnemyAction(EnemyAction a)
        {
            var events = new List<BattleEvent>();
            if (Current == null || a == null || Outcome != BattleOutcome.Ongoing) return events;
            Unit actor = Current;

            switch (a.Type)
            {
                case EnemyActionType.Attack:
                case EnemyActionType.Buff:
                case EnemyActionType.Debuff:
                    return UseSkill(a.Skill, a.Target);

                case EnemyActionType.Heal:
                    events = UseSkill(a.Skill, a.Target);
                    actor.HealCooldown = EnemyHealCooldown + 1;   // +1 karena turun satu di awal giliran berikutnya
                    return events;

                case EnemyActionType.Defend:
                    if (a.Skill != null) return UseSkill(a.Skill, null);
                    actor.AddStatus(StatusType.Guard, 1, GuardCutDefault);
                    events.Add(new BattleEvent { Target = actor, Kind = EventKind.StatusApplied, Text = Unit.StatusShort(StatusType.Guard) });
                    Log(actor.Name + " bertahan.");
                    break;

                case EnemyActionType.Steal:
                    events.Add(DoSteal(actor));
                    break;

                case EnemyActionType.Flee:
                    actor.Fled = true;
                    actor.Statuses.Clear();
                    FledUnits.Add(actor);
                    events.Add(new BattleEvent { Target = actor, Kind = EventKind.Info, Text = "Kabur" });
                    Log(actor.Name + " kabur" + (actor.StolenFood + actor.StolenGold > 0 ? " membawa curiannya." : "."));
                    break;

                case EnemyActionType.CallReinforcements:
                    events.Add(DoCallReinforcements(actor));
                    break;
            }
            EndAction(actor, events);
            return events;
        }

        BattleEvent DoSteal(Unit thief)
        {
            bool canFood = PlayerFood > 0, canGold = PlayerGold > 0;
            string text;
            if (canFood && (!canGold || rng.Next(2) == 0))
            {
                int n = Math.Min(StealFoodAmount, PlayerFood);
                PlayerFood -= n;
                thief.StolenFood += n;
                text = "Curi Food " + n;
            }
            else if (canGold)
            {
                int n = Math.Min(StealGoldAmount, PlayerGold);
                PlayerGold -= n;
                thief.StolenGold += n;
                text = "Curi Gold " + n;
            }
            else text = "Tidak ada yang bisa dicuri";
            Log(thief.Name + ": " + text + ".");
            return new BattleEvent { Target = thief, Kind = EventKind.Info, Text = text };
        }

        BattleEvent DoCallReinforcements(Unit caller)
        {
            ReinforcementCalls++;
            StallCount = 0;   // supaya stalling tidak langsung memicu panggilan lagi
            string id = ReinforcementIds[rng.Next(ReinforcementIds.Length)];
            UnitData d = GameData.CreateEnemy(id);
            if (d == null)
            {
                Log(caller.Name + " memanggil bantuan, tapi tidak ada yang datang.");
                return new BattleEvent { Target = caller, Kind = EventKind.Info, Text = "Bantuan gagal" };
            }
            var u = new Unit(d, true, d.MaxHp);
            Enemies.Add(u);
            All.Add(u);   // ikut roll initiative di ronde berikutnya
            Log(caller.Name + " memanggil bantuan: " + u.Name + ".");
            return new BattleEvent { Target = u, Kind = EventKind.Info, Text = "Bantuan: " + u.Name };
        }
    }
}
