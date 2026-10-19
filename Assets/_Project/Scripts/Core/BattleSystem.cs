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

    // Logika battle murni (tanpa UnityEngine) supaya mudah dites.
    // Alur: Begin() -> loop { NextActor() -> UseSkill() } sampai Outcome != Ongoing.
    public class BattleSystem
    {
        public const float AvBase = 10000f;
        public const float GuardCutDefault = 0.5f;

        public readonly List<Unit> Allies = new List<Unit>();
        public readonly List<Unit> Enemies = new List<Unit>();
        public readonly List<Unit> All = new List<Unit>();
        public readonly List<BattleEvent> TurnStartEvents = new List<BattleEvent>();
        public BattleOutcome Outcome { get; private set; }
        public Unit Current { get; private set; }
        public event Action<string> OnLog;

        readonly Random rng;

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

        public void Begin()
        {
            for (int i = 0; i < All.Count; i++)
                All[i].Av = AvBase / Math.Max(1, All[i].Data.Speed);
            Current = null;
            CheckOutcome();
        }

        // Pilih unit berikutnya yang bertindak (speed-based initiative). Mengembalikan null jika battle selesai.
        public Unit NextActor()
        {
            TurnStartEvents.Clear();
            int guard = 0;
            while (Outcome == BattleOutcome.Ongoing && guard++ < 1000)
            {
                Unit next = null;
                for (int i = 0; i < All.Count; i++)
                {
                    Unit u = All[i];
                    if (!u.Alive) continue;
                    if (next == null || u.Av < next.Av - 0.0001f ||
                        (Math.Abs(u.Av - next.Av) <= 0.0001f && u.Data.Speed > next.Data.Speed))
                        next = u;
                }
                if (next == null) { CheckOutcome(); break; }

                float step = next.Av;
                for (int i = 0; i < All.Count; i++)
                    if (All[i].Alive) All[i].Av -= step;
                next.Av = 0f;
                Current = next;

                StartTurn(next);
                CheckOutcome();
                if (Outcome != BattleOutcome.Ongoing) { Current = null; return null; }
                if (next.Alive) return next;
            }
            Current = null;
            return null;
        }

        // Awal giliran: Bleed berjalan, lalu durasi semua status berkurang.
        void StartTurn(Unit u)
        {
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

        // Urutan giliran ke depan (termasuk giliran sekarang) untuk bar turn order.
        public List<Unit> PreviewOrder(int count)
        {
            var units = new List<Unit>();
            var av = new List<float>();
            for (int i = 0; i < All.Count; i++)
            {
                if (!All[i].Alive) continue;
                units.Add(All[i]);
                av.Add(All[i] == Current ? 0f : All[i].Av);
            }
            var result = new List<Unit>();
            if (units.Count == 0) return result;
            for (int n = 0; n < count; n++)
            {
                int best = 0;
                for (int i = 1; i < units.Count; i++)
                {
                    if (av[i] < av[best] - 0.0001f ||
                        (Math.Abs(av[i] - av[best]) <= 0.0001f && units[i].Data.Speed > units[best].Data.Speed))
                        best = i;
                }
                float step = av[best];
                for (int i = 0; i < av.Count; i++) av[i] -= step;
                av[best] = AvBase / Math.Max(1, units[best].Data.Speed);
                result.Add(units[best]);
            }
            return result;
        }

        List<Unit> Opponents(Unit actor) { return actor.IsEnemy ? Allies : Enemies; }
        List<Unit> Friends(Unit actor) { return actor.IsEnemy ? Enemies : Allies; }

        public bool IsValidTarget(Skill skill, Unit actor, Unit target)
        {
            if (target == null || !target.Alive) return false;
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
                    foreach (var u in Opponents(actor)) if (u.Alive) list.Add(u);
                    break;
                case TargetType.AllAllies:
                    foreach (var u in Friends(actor)) if (u.Alive) list.Add(u);
                    break;
                case TargetType.Self:
                    list.Add(actor);
                    break;
                default:
                    if (chosen != null && chosen.Alive) list.Add(chosen);
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

            actor.Av = AvBase / Math.Max(1, actor.Data.Speed);
            CheckOutcome();
            return events;
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
            }
        }

        void CheckOutcome()
        {
            bool anyAlly = false, anyEnemy = false;
            for (int i = 0; i < Allies.Count; i++) if (Allies[i].Alive) anyAlly = true;
            for (int i = 0; i < Enemies.Count; i++) if (Enemies[i].Alive) anyEnemy = true;
            if (!anyEnemy) Outcome = BattleOutcome.Victory;
            else if (!anyAlly) Outcome = BattleOutcome.Defeat;
            else Outcome = BattleOutcome.Ongoing;
        }

        // AI sederhana untuk musuh (dan untuk tes otomatis).
        public ChosenAction ChooseAction(Unit actor)
        {
            var skills = actor.Data.GetEquipped();
            var usable = new List<Skill>();
            for (int i = 0; i < skills.Count; i++)
            {
                Skill s = skills[i];
                if (s.Kind == SkillKind.Heal)
                {
                    bool hurt = false;
                    foreach (var f in Friends(actor))
                        if (f.Alive && f.Hp < f.MaxHp * 0.7f) hurt = true;
                    if (!hurt) continue;
                }
                if (ValidTargets(s, actor).Count == 0 && s.NeedsTargetPick) continue;
                usable.Add(s);
            }
            if (usable.Count == 0) usable.AddRange(skills);
            Skill pick = usable[rng.Next(usable.Count)];

            Unit target = null;
            if (pick.NeedsTargetPick)
            {
                var targets = ValidTargets(pick, actor);
                if (pick.Kind == SkillKind.Heal)
                {
                    target = targets[0];
                    foreach (var t in targets)
                        if ((float)t.Hp / t.MaxHp < (float)target.Hp / target.MaxHp) target = t;
                }
                else
                {
                    target = targets[rng.Next(targets.Count)];
                }
            }
            return new ChosenAction { Skill = pick, Target = target };
        }
    }
}
