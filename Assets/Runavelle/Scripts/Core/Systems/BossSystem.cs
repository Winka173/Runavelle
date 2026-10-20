using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Data-driven boss phases and telegraphed abilities. Movement/basic attacks come from EnemyAI.</summary>
    public sealed class BossSystem
    {
        private readonly World _w;
        private readonly CombatSystem _combat;
        private readonly EffectsSystem _fx;
        private readonly NavigationService _nav;
        private readonly Movement _move;
        private readonly List<Unit> _tmp = new List<Unit>(32);
        public Func<string, Vec2, string, Unit> SpawnSummon;

        public BossSystem(World w, CombatSystem c, EffectsSystem fx, NavigationService nav, Movement m)
        {
            _w = w; _combat = c; _fx = fx; _nav = nav; _move = m;
        }

        public void Tick(Unit boss, float dt)
        {
            var rt = boss.Boss;
            if (rt == null || boss.Dead) return;
            // phase transitions
            for (int i = rt.Def.Phases.Count - 1; i > rt.PhaseIndex; i--)
            {
                if (boss.HpFraction > rt.Def.Phases[i].StartsAtHp) continue;
                rt.PhaseIndex = i;
                var ph = rt.Def.Phases[i];
                rt.Cooldowns.Clear();
                foreach (var a in ph.Abilities) rt.Cooldowns.Add(a.InitialDelay);
                boss.Speed = boss.Def.Speed * ph.SpeedMult;
                boss.Attack.Interval = boss.Def.Attack.Interval / ph.AttackSpeedMult;
                _w.Emit(SimEventType.BossPhase, boss.Position, i, ElementId.None, boss.Id, ph.Announcement ?? ph.Name);
                break;
            }
            var phase = rt.Def.Phases[rt.PhaseIndex];
            if (boss.Status.Stunned || rt.PendingTelegraph >= 0) return;
            for (int i = 0; i < phase.Abilities.Count; i++)
            {
                rt.Cooldowns[i] -= dt;
                if (rt.Cooldowns[i] > 0f) continue;
                var ab = phase.Abilities[i];
                rt.Cooldowns[i] = ab.Cooldown;
                Begin(boss, ab);
                break;
            }
        }

        private DamageInfo Info(Unit boss, BossAbility ab)
        {
            var d = DamageInfo.Simple(ab.Damage, ab.Element, ab.Status, SourceKind.Enemy, boss.Id, boss.Def.Name + " · " + ab.Name);
            d.Origin = boss.Position;
            d.Generation = 0;
            return d;
        }

        private void Begin(Unit boss, BossAbility ab)
        {
            var rt = boss.Boss;
            switch (ab.Kind)
            {
                case BossAbilityKind.Slam:
                {
                    Actor tgt = _w.Get(boss.TargetId);
                    if (tgt == null || !tgt.Targetable) tgt = NearestStructure(boss.Position, 4f) ?? (Actor)_combat.NearestUnit(Team.Player, boss.Position, 4f, null);
                    if (tgt == null) { rt.Cooldowns[0] = 1.5f; return; }
                    Vec2 at = _w.ClosestPoint(boss.Position, tgt);
                    Telegraph(boss, ab, at, () => { _fx.AreaDamage(Team.Player, at, ab.Radius, Info(boss, ab), ab.Value > 0f); Exhaust(boss, ab); });
                    break;
                }
                case BossAbilityKind.Summon:
                {
                    Vec2 toward = _w.Castle != null ? (_w.Castle.Center - boss.Position).Normalized : Vec2.Zero;
                    Vec2 at = boss.Position + toward * 2f;
                    if (!_combat.PositionWalkable(boss, at)) at = boss.Position;
                    Telegraph(boss, ab, at, () =>
                    {
                        for (int i = 0; i < ab.Count; i++)
                        {
                            var p = at + new Vec2((i % 3 - 1) * 0.7f, (i / 3) * 0.7f - 0.35f);
                            if (!_combat.PositionWalkable(boss, p)) p = at;
                            var s = SpawnSummon?.Invoke(ab.SummonId, p, boss.LaneId);
                            if (s != null) s.Summoned = true;
                        }
                        _w.Emit(SimEventType.Summon, at, ab.Count, ElementId.Shadow, boss.Id, ab.Name);
                        Exhaust(boss, ab);
                    });
                    break;
                }
                case BossAbilityKind.Meteor:
                {
                    var spots = new List<Vec2>();
                    foreach (var b in _w.Buildings)
                        if (!b.Dead && !b.Ruined && Vec2.Distance(b.Center, boss.Position) < 9f) spots.Add(b.Center);
                    foreach (var u in _w.UnitsInRadius(Team.Player, boss.Position, 9f, _tmp)) spots.Add(u.Position);
                    if (spots.Count == 0) spots.Add(boss.Position + new Vec2(1.5f, 0));
                    for (int i = 0; i < ab.Count; i++)
                    {
                        var at = spots[_w.Rng.Range(0, spots.Count)] + new Vec2(_w.Rng.Range(-0.8f, 0.8f), _w.Rng.Range(-0.8f, 0.8f));
                        _fx.AddTelegraph(at, ab.Radius, ab.Telegraph, ab.Name, ab.Element, () => _fx.AreaDamage(Team.Player, at, ab.Radius, Info(boss, ab), true));
                    }
                    break;
                }
                case BossAbilityKind.Charge:
                {
                    Vec2 dir = _w.Castle != null ? (_w.Castle.Center - boss.Position).Normalized : new Vec2(boss.FacingX, 0);
                    Vec2 end = boss.Position + dir * ab.Value;
                    var t = _fx.AddTelegraph(boss.Position, ab.Radius, ab.Telegraph, ab.Name, ab.Element, null);
                    t.Line = true; t.LineEnd = end;
                    rt.PendingTelegraph = t.Id;
                    t.OnResolve = () =>
                    {
                        rt.PendingTelegraph = -1;
                        int steps = 12;
                        var hit = new HashSet<int>();
                        for (int i = 0; i < steps; i++)
                        {
                            Vec2 next = Vec2.Lerp(boss.Position, end, 1f / (steps - i));
                            var tile = Int2.FromPosition(next);
                            var blocker = _w.BuildingAt(tile.X, tile.Y);
                            if (blocker != null && !blocker.Ruined) { _combat.Deal(blocker, Info(boss, ab)); break; }
                            if (!_w.TerrainPassable(tile.X, tile.Y)) break;
                            boss.Position = next;
                            foreach (var u in _w.UnitsInRadius(Team.Player, boss.Position, ab.Radius, _tmp).Snapshot(_w))
                                if (hit.Add(u.Id)) { var d = Info(boss, ab); d.Knockback = 1f; _combat.Deal(u, d); }
                        }
                        Exhaust(boss, ab);
                    };
                    break;
                }
                case BossAbilityKind.FreezeTowers:
                    Telegraph(boss, ab, boss.Position, () =>
                    {
                        foreach (var b in _w.Buildings)
                        {
                            if (b.Dead || b.Ruined || b.Level.Attack == null || Vec2.Distance(b.Center, boss.Position) > ab.Radius) continue;
                            b.DisabledTimer = Math.Max(b.DisabledTimer, ab.Value);
                            _w.Emit(SimEventType.Freeze, b.Center, ab.Value, ElementId.Ice, b.Id, "tower");
                        }
                        _fx.AreaDamage(Team.Player, boss.Position, ab.Radius, Info(boss, ab), false);
                        Exhaust(boss, ab);
                    });
                    break;
                case BossAbilityKind.DisruptTower:
                {
                    Building tower = null;
                    float bd = ab.Radius;
                    foreach (var b in _w.Buildings)
                    {
                        if (b.Dead || b.Ruined || b.Level.Attack == null || b.Def.Family == BuildingFamily.Castle) continue;
                        float d = Vec2.Distance(b.Center, boss.Position);
                        if (d < bd) { bd = d; tower = b; }
                    }
                    if (tower == null) return;
                    Telegraph(boss, ab, tower.Center, () =>
                    {
                        tower.DisabledTimer = Math.Max(tower.DisabledTimer, ab.Value);
                        // the possessed tower lashes out at nearby defenders
                        _fx.AreaDamage(Team.Player, tower.Center, 2.5f, Info(boss, ab), false);
                        _w.Emit(SimEventType.StatusApplied, tower.Center, ab.Value, ElementId.Shadow, tower.Id, "possessed");
                        Exhaust(boss, ab);
                    });
                    break;
                }
                case BossAbilityKind.Teleport:
                {
                    var tile = Int2.FromPosition(boss.Position);
                    for (int i = 0; i < (int)ab.Value; i++) tile = _nav.NextStep(tile, FlowKind.Burrow);
                    Vec2 to = tile.Center;
                    Telegraph(boss, ab, to, () => { boss.Position = to; boss.PrevPosition = to; Exhaust(boss, ab); });
                    break;
                }
                case BossAbilityKind.Revive:
                    Telegraph(boss, ab, boss.Position, () =>
                    {
                        int n = 0;
                        for (int i = _w.DeadEnemies.Count - 1; i >= 0 && n < ab.Count; i--)
                        {
                            var d = _w.DeadEnemies[i];
                            if (d.IsElite || Vec2.Distance(d.Position, boss.Position) > ab.Radius) continue;
                            _w.DeadEnemies.RemoveAt(i);
                            d.Dead = false; d.Hp = d.MaxHp * 0.6f; d.Status.Clear(); d.Summoned = true;
                            if (!_w.Units.Contains(d)) _w.Units.Add(d);
                            _w.ById[d.Id] = d;
                            n++;
                        }
                        _w.Emit(SimEventType.Summon, boss.Position, n, ElementId.Shadow, boss.Id, ab.Name);
                        Exhaust(boss, ab);
                    });
                    break;
                case BossAbilityKind.Gust:
                    Telegraph(boss, ab, boss.Position, () =>
                    {
                        foreach (var u in _w.UnitsInRadius(Team.Player, boss.Position, ab.Radius, _tmp).Snapshot(_w))
                        {
                            var d = Info(boss, ab);
                            d.Knockback = ab.Value;
                            _combat.Deal(u, d);
                        }
                        Exhaust(boss, ab);
                    });
                    break;
                case BossAbilityKind.Breath:
                case BossAbilityKind.TidalWave:
                {
                    Actor tgt = (Actor)_combat.NearestUnit(Team.Player, boss.Position, 6f, null) ?? NearestStructure(boss.Position, 6f);
                    Vec2 dir = tgt != null ? (tgt.Position - boss.Position).Normalized : new Vec2(boss.FacingX, 0);
                    Vec2 at = boss.Position + dir * (ab.Radius * 0.8f);
                    Telegraph(boss, ab, at, () =>
                    {
                        var d = Info(boss, ab);
                        if (ab.Kind == BossAbilityKind.TidalWave) d.Knockback = 1.2f;
                        _fx.AreaDamage(Team.Player, at, ab.Radius, d, true);
                        Exhaust(boss, ab);
                    });
                    break;
                }
                case BossAbilityKind.CurseZone:
                {
                    // darkness falls on the most fortified spot nearby: defenders inside take +20% damage
                    Actor tgt = NearestStructure(boss.Position, 9f) ?? (Actor)_combat.NearestUnit(Team.Player, boss.Position, 9f, null);
                    if (tgt == null) return;
                    Vec2 at = _w.ClosestPoint(boss.Position, tgt);
                    Telegraph(boss, ab, at, () =>
                    {
                        _combat.SpawnZone(ZoneKind.Gloom, Team.Enemy, at, ab.Radius, ab.Value, ab.Damage, ab.Element, ab.Status, Info(boss, ab));
                        Exhaust(boss, ab);
                    });
                    break;
                }
                case BossAbilityKind.ShieldSelf:
                    Telegraph(boss, ab, boss.Position, () => { boss.Status.ShieldHp = boss.MaxHp * ab.Value; Exhaust(boss, ab); });
                    break;
            }
        }

        private void Telegraph(Unit boss, BossAbility ab, Vec2 at, Action resolve)
        {
            var rt = boss.Boss;
            var t = _fx.AddTelegraph(at, ab.Radius, ab.Telegraph, ab.Name, ab.Element, null);
            rt.PendingTelegraph = t.Id;
            boss.Status.RootTimer = Math.Max(boss.Status.RootTimer, ab.Telegraph * 0.8f); // wind-up: boss plants itself
            t.OnResolve = () =>
            {
                rt.PendingTelegraph = -1;
                if (!boss.Dead) resolve();
            };
        }

        private void Exhaust(Unit boss, BossAbility ab)
        {
            if (ab.ExhaustAfter <= 0f) return;
            boss.ExhaustedTimer = ab.ExhaustAfter;
            _w.Emit(SimEventType.StatusApplied, boss.Position, ab.ExhaustAfter, ElementId.None, boss.Id, "exhausted");
        }

        private Building NearestStructure(Vec2 p, float r)
        {
            Building best = null;
            float bd = r;
            foreach (var b in _w.Buildings)
            {
                if (b.Dead || b.Ruined) continue;
                float d = _w.DistanceToActor(p, b);
                if (d < bd) { bd = d; best = b; }
            }
            return best;
        }
    }
}
