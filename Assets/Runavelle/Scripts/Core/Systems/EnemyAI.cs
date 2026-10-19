using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>
    /// Enemy behaviour. Ground enemies follow a castle flow field; when the next step is a structure they attack it
    /// (breach) instead of freezing. Flyers path straight to the castle. Special archetypes add their own rules.
    /// </summary>
    public sealed class EnemyAI
    {
        private readonly World _w;
        private readonly CombatSystem _combat;
        private readonly EffectsSystem _fx;
        private readonly NavigationService _nav;
        private readonly Movement _move;
        private readonly Spawner _spawner;
        private readonly List<Unit> _tmp = new List<Unit>(32);
        public Func<string, Vec2, string, Unit> SpawnSummon;
        public int ManaDrained;

        public EnemyAI(World w, CombatSystem c, EffectsSystem fx, NavigationService nav, Movement m, Spawner s)
        {
            _w = w; _combat = c; _fx = fx; _nav = nav; _move = m; _spawner = s;
        }

        public void Tick(Unit u, float dt)
        {
            u.AttackCooldown -= dt * u.Status.AttackSpeedMult();
            u.SecondaryCooldown -= dt;
            u.AttackAnim = Math.Max(0f, u.AttackAnim - dt);
            if (u.LifeTimer > 0f && (u.LifeTimer -= dt) <= 0f) { _combat.Kill(u, DamageInfo.Simple(0, ElementId.None, 0, SourceKind.None, -1, "")); return; }
            if (u.Status.Stunned) return;
            Specials(u, dt);
            if (u.Dead) return;

            if (u.State == UnitState.Burrowed) { TickBurrowed(u, dt); return; }
            if (u.State == UnitState.Fleeing) { u.BurrowTimer -= dt; if (u.BurrowTimer <= 0) u.State = UnitState.Moving; else { _move.MoveTo(u, u.Home, dt); return; } }

            u.AiDecisionTimer -= dt;
            Actor target = _w.Get(u.TargetId);
            if (u.AiDecisionTimer <= 0f || target == null || !target.Targetable)
            {
                u.AiDecisionTimer = 0.3f + (u.Id % 5) * 0.02f;
                target = ChooseTarget(u);
                u.TargetId = target?.Id ?? -1;
            }

            if (target != null && target.Targetable)
            {
                float range = u.Attack.Range + (target is Building ? 0.15f : 0f);
                float dist = _w.DistanceToActor(u.Position, target);
                if (dist <= range && dist >= u.Attack.MinRange)
                {
                    u.State = UnitState.Fighting;
                    if (target.Position.X != u.Position.X) u.FacingX = Math.Sign(target.Position.X - u.Position.X);
                    if (u.AttackCooldown <= 0f)
                    {
                        u.AttackCooldown = u.Attack.Interval;
                        u.AttackAnim = 0.25f;
                        _fx.Attack(u, target, u.Attack, u.Attack.Damage, u.Attack.Element == ElementId.None ? 2 : 1);
                        if (u.Def.ManaDrain > 0f && target is Building wb && wb.Def.Family == BuildingFamily.ManaWell) ManaDrained += (int)u.Def.ManaDrain;
                        if (u.Def.Behavior == EnemyBehavior.Skirmisher) { u.State = UnitState.Fleeing; u.BurrowTimer = 0.9f; u.Home = u.Position + (u.Position - target.Position).Normalized * 1.8f; }
                    }
                    if (u.SecondaryAttack != null && u.SecondaryCooldown <= 0f && dist <= u.SecondaryAttack.Range)
                    {
                        u.SecondaryCooldown = u.SecondaryAttack.Interval;
                        _fx.Attack(u, target, u.SecondaryAttack, u.SecondaryAttack.Damage, 1);
                    }
                    return;
                }
                if (target is Unit tu && (u.Def.Behavior != EnemyBehavior.Siege) && dist < u.Def.AggroRange + 1.5f && !u.IsFlying)
                {
                    // chase a nearby defender directly when the line is open, else keep advancing
                    if (_move.LineWalkable(u, u.Position, tu.Position)) { _move.MoveTo(u, tu.Position, dt, range * 0.8f); return; }
                }
            }
            Advance(u, dt);
        }

        private void Advance(Unit u, float dt)
        {
            u.State = UnitState.Moving;
            if (u.IsFlying)
            {
                var goal = FlyerGoal(u);
                _move.MoveTo(u, goal, dt, 0.3f);
                return;
            }
            var kind = FlowFor(u);
            var tile = Int2.FromPosition(u.Position);
            var next = _nav.NextStep(tile, kind);
            if (next == tile)
            {
                // At the castle or stuck: head for the castle centre
                if (_w.Castle != null) _move.MoveTo(u, _w.Castle.Center, dt, 1.4f);
                return;
            }
            var blocker = _w.BuildingAt(next.X, next.Y);
            if (blocker != null && !blocker.Ruined && blocker.Def.Family != BuildingFamily.Castle)
            {
                // breach: attack the structure in the way
                u.TargetId = blocker.Id;
                u.AiDecisionTimer = 0.5f;
                if (_w.DistanceToActor(u.Position, blocker) > u.Attack.Range) _move.MoveTo(u, next.Center, dt, u.Attack.Range * 0.7f);
                return;
            }
            Vec2 before = u.Position;
            _move.MoveTo(u, next.Center, dt, 0.05f);
            Progress(u, dt, before);
        }

        private void Progress(Unit u, float dt, Vec2 before)
        {
            if (Vec2.SqrDistance(u.Position, u.LastProgressPos) > 0.25f) { u.LastProgressPos = u.Position; u.StuckTimer = 0f; return; }
            u.StuckTimer += dt;
            if (u.StuckTimer > 4f)
            {
                // Unstick: nudge toward the tile centre of the flow's next step, ignoring separation
                u.StuckTimer = 0f;
                var t = Int2.FromPosition(u.Position);
                u.Position = Vec2.MoveTowards(u.Position, t.Center, 0.3f);
            }
        }

        private FlowKind FlowFor(Unit u)
        {
            switch (u.Def.Behavior)
            {
                case EnemyBehavior.GateBreaker: return FlowKind.GateBreaker;
                case EnemyBehavior.Siege: return FlowKind.Siege;
                default: return FlowKind.Standard;
            }
        }

        private Vec2 FlyerGoal(Unit u)
        {
            // Harpies dive at the nearest tower within 6 tiles, otherwise the castle
            Building best = null;
            float bd = 6f;
            foreach (var b in _w.Buildings)
            {
                if (b.Dead || b.Ruined || (b.Def.Family != BuildingFamily.ArcherTower && b.Def.Family != BuildingFamily.MagicTower)) continue;
                float d = Vec2.Distance(b.Center, u.Position);
                if (d < bd) { bd = d; best = b; }
            }
            return best != null ? best.Center : (_w.Castle?.Center ?? u.Position);
        }

        /// <summary>Picks what to attack right now (or null to keep advancing).</summary>
        private Actor ChooseTarget(Unit u)
        {
            var beh = u.Def.Behavior;
            // Taunt (Rune Golem) overrides for non-siege enemies
            if (beh != EnemyBehavior.Siege && beh != EnemyBehavior.Boss && !u.IsFlying)
                foreach (var f in _w.UnitsInRadius(Team.Player, u.Position, 3.2f, _tmp))
                    if (f.Def.TauntRadius > 0f && Vec2.Distance(f.Position, u.Position) <= f.Def.TauntRadius) return f;

            switch (beh)
            {
                case EnemyBehavior.Siege:
                {
                    // rams only hit structures: the castle if adjacent, otherwise whatever blocks them (handled by Advance)
                    if (_w.Castle != null && _w.DistanceToActor(u.Position, _w.Castle) <= u.Attack.Range + 0.2f) return _w.Castle;
                    return StructureInRange(u, u.Attack.Range + 0.3f, false);
                }
                case EnemyBehavior.Artillery:
                    return (Actor)StructureInRange(u, u.Attack.Range, true) ?? NearestFriendly(u, u.Attack.Range);
                case EnemyBehavior.ManaDrainer:
                {
                    var well = NearestBuilding(u, BuildingFamily.ManaWell, 7f);
                    if (well != null) return well;
                    break;
                }
                case EnemyBehavior.Assassin:
                {
                    var tower = NearestTower(u, 6f);
                    if (tower != null) return tower;
                    break;
                }
            }
            float aggro = u.Def.AggroRange;
            if (u.Attack.Range > 2f) aggro = Math.Max(aggro, u.Attack.Range);
            var unit = NearestFriendly(u, aggro);
            if (unit != null && (u.Attack.HitsAir || !unit.IsFlying)) return unit;
            if (_w.Castle != null && _w.DistanceToActor(u.Position, _w.Castle) <= u.Attack.Range + 0.2f) return _w.Castle;
            // ranged enemies also shoot structures in range
            if (u.Attack.Range > 2f) return StructureInRange(u, u.Attack.Range, true);
            if (u.IsFlying) return StructureInRange(u, u.Attack.Range + 0.3f, false);
            return null;
        }

        private Unit NearestFriendly(Unit u, float r)
        {
            Unit best = null;
            float bd = float.MaxValue;
            foreach (var f in _w.UnitsInRadius(Team.Player, u.Position, r, _tmp))
            {
                if (f.IsFlying && !u.Attack.HitsAir) continue;
                float d = Vec2.SqrDistance(f.Position, u.Position);
                if (d < bd || (d == bd && f.Id < best.Id)) { bd = d; best = f; }
            }
            return best;
        }

        private Building StructureInRange(Unit u, float r, bool preferTowers)
        {
            Building best = null;
            float bd = float.MaxValue;
            foreach (var b in _w.Buildings)
            {
                if (b.Dead || b.Ruined) continue;
                float d = _w.DistanceToActor(u.Position, b);
                if (d > r) continue;
                if (preferTowers && (b.Def.Family == BuildingFamily.ArcherTower || b.Def.Family == BuildingFamily.MagicTower)) d -= 3f;
                if (d < bd) { bd = d; best = b; }
            }
            return best;
        }

        private Building NearestBuilding(Unit u, BuildingFamily f, float r)
        {
            Building best = null;
            float bd = r;
            foreach (var b in _w.Buildings)
            {
                if (b.Dead || b.Ruined || b.Def.Family != f) continue;
                float d = _w.DistanceToActor(u.Position, b);
                if (d < bd) { bd = d; best = b; }
            }
            return best;
        }

        private Building NearestTower(Unit u, float r)
        {
            var a = NearestBuilding(u, BuildingFamily.ArcherTower, r);
            var m = NearestBuilding(u, BuildingFamily.MagicTower, r);
            if (a == null) return m;
            if (m == null) return a;
            return _w.DistanceToActor(u.Position, a) <= _w.DistanceToActor(u.Position, m) ? a : m;
        }

        // ---------------------------------------------------------------- special behaviours
        private void Specials(Unit u, float dt)
        {
            var def = u.Def;
            if (def.SummonInterval > 0f && def.SummonId != null && SpawnSummon != null)
            {
                u.SummonTimer -= dt;
                if (u.SummonTimer <= 0f)
                {
                    u.SummonTimer = def.SummonInterval;
                    for (int i = 0; i < def.SummonCount; i++)
                    {
                        var p = u.Position + new Vec2(_w.Rng.Range(-0.8f, 0.8f), _w.Rng.Range(-0.8f, 0.8f));
                        if (!_combat.PositionWalkable(u, p)) p = u.Position;
                        SpawnSummon(def.SummonId, p, u.LaneId);
                    }
                    _w.Emit(SimEventType.Summon, u.Position, def.SummonCount, ElementId.Shadow, u.Id, def.Name);
                }
            }
            if (def.Behavior == EnemyBehavior.Necromancer)
            {
                u.SummonTimer -= dt;
                if (u.SummonTimer <= 0f)
                {
                    u.SummonTimer = Math.Max(5f, def.SummonInterval);
                    int revived = 0;
                    for (int i = _w.DeadEnemies.Count - 1; i >= 0 && revived < Math.Max(1, def.SummonCount); i--)
                    {
                        var d = _w.DeadEnemies[i];
                        if (d.IsBoss || d.IsElite || d.ReviveCount > 0 || Vec2.Distance(d.Position, u.Position) > 4f) continue;
                        _w.DeadEnemies.RemoveAt(i);
                        d.Dead = false; d.Hp = d.MaxHp * 0.5f; d.ReviveCount++; d.Status.Clear(); d.Summoned = true;
                        if (!_w.Units.Contains(d)) _w.Units.Add(d);
                        _w.ById[d.Id] = d;
                        _w.Emit(SimEventType.Summon, d.Position, 1, ElementId.Shadow, d.Id, "revive");
                        revived++;
                    }
                }
            }
            if (def.ShiftInterval > 0f)
            {
                u.ShiftTimer -= dt;
                if (u.ShiftTimer <= 0f)
                {
                    u.ShiftTimer = def.ShiftInterval;
                    var e = Elements.All[_w.Rng.Range(0, 6)];
                    u.ShiftedResist = e;
                    u.Attack.Element = e;
                    u.Attack.StatusAmount = Math.Max(u.Attack.StatusAmount, 20f);
                    _w.Emit(SimEventType.StatusApplied, u.Position, 0, e, u.Id, "shift");
                }
            }
            if (u.Stealthed)
            {
                // revealed near defenders/towers or when damaged; Light status also reveals
                bool reveal = u.Status.Gauge[(int)ElementId.Light] > 0f || _w.Time - u.LastHitTime < 0.5f;
                if (!reveal)
                    foreach (var f in _w.UnitsInRadius(Team.Player, u.Position, def.StealthRange, _tmp)) { reveal = true; break; }
                if (!reveal)
                    foreach (var b in _w.Buildings)
                        if (!b.Dead && !b.Ruined && b.Level.Attack != null && Vec2.Distance(b.Center, u.Position) <= def.StealthRange + 1f) { reveal = true; break; }
                if (!reveal && _w.Mods.Has(Hook.SeerLantern) && _w.PlayerGeneral != null && _w.PlayerGeneral.State != UnitState.Downed &&
                    Vec2.Distance(_w.PlayerGeneral.Position, u.Position) <= _w.Mods.HookValue(Hook.SeerLantern)) reveal = true;
                if (reveal) { u.Stealthed = false; _w.Emit(SimEventType.Stealth, u.Position, 0, ElementId.Shadow, u.Id, "reveal"); }
            }
            if (def.Behavior == EnemyBehavior.ShieldCarrier)
                foreach (var e in _w.UnitsInRadius(Team.Enemy, u.Position, Math.Max(2f, def.AuraRadius), _tmp))
                    if (e != u) e.Status.AddGuard(0.3f, 0.5f);
        }

        private void TickBurrowed(Unit u, float dt)
        {
            var tile = Int2.FromPosition(u.Position);
            float flowDist = _nav.DistanceToCastle(u.Position, FlowKind.Burrow);
            u.BurrowTimer += dt;
            bool insideWalls = _nav.DistanceToCastle(u.Position, FlowKind.Standard) > flowDist + 6f;
            if (flowDist < 6f || (insideWalls && u.BurrowTimer > 2f) || u.BurrowTimer > 40f)
            {
                // emerge with a short telegraph so the player can react
                u.State = UnitState.Moving;
                u.Status.StunTimer = 1.0f;
                _w.Emit(SimEventType.Telegraph, u.Position, 1f, ElementId.Earth, u.Id, "Trồi lên!");
                return;
            }
            var next = _nav.NextStep(tile, FlowKind.Burrow);
            _move.MoveTo(u, next.Center, dt, 0.05f);
        }
    }
}
