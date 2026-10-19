using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>
    /// Player General (direct movement + auto attack + ultimate + dash) and allied AI Generals (sector defence with
    /// utility scoring). Both use the same ability executor and combat pipeline.
    /// </summary>
    public sealed class GeneralSystem
    {
        private readonly World _w;
        private readonly CombatSystem _combat;
        private readonly EffectsSystem _fx;
        private readonly NavigationService _nav;
        private readonly Movement _move;
        private readonly Spawner _spawner;
        private readonly List<Unit> _tmp = new List<Unit>(64);
        public bool UltimateRequested, DashRequested;
        public Func<string, Vec2, Building> SpawnBarrier;
        public Func<string, Vec2, float, Unit> SpawnAllySummon;
        /// <summary>AI sector posts by slot (assigned in daytime).</summary>
        public readonly Dictionary<int, Vec2> Posts = new Dictionary<int, Vec2>();

        public GeneralSystem(World w, CombatSystem c, EffectsSystem fx, NavigationService nav, Movement m, Spawner s)
        {
            _w = w; _combat = c; _fx = fx; _nav = nav; _move = m; _spawner = s;
        }

        private Scope ScopeOf(Unit g) => g.IsPlayerControlled ? Scope.PlayerGeneral : Scope.AIGenerals;

        public void Tick(Unit g, float dt)
        {
            if (g == null || g.Dead) return;
            g.AttackCooldown -= dt * g.Status.AttackSpeedMult();
            g.UltCooldown -= dt;
            g.DashCooldown -= dt;
            g.AttackAnim = Math.Max(0f, g.AttackAnim - dt);
            if (g.State == UnitState.Downed)
            {
                g.DownedTimer -= dt;
                if (g.DownedTimer <= 0f)
                {
                    g.State = UnitState.Idle;
                    g.Hp = g.MaxHp * 0.6f;
                    g.Position = RespawnPoint();
                    g.PrevPosition = g.Position;
                    g.IFrames = 1.5f;
                    _w.Emit(SimEventType.GeneralRevived, g.Position, 0, ElementId.Light, g.Id, g.Def.Name);
                }
                return;
            }
            if (g.IsPlayerControlled) TickPlayer(g, dt);
            else TickAI(g, dt);
            AutoAttack(g);
        }

        private Vec2 RespawnPoint()
        {
            var c = _w.Castle != null ? _w.Castle.Center : new Vec2(_w.Map.Width / 2f, _w.Map.Height / 2f);
            var t = _nav.NearestFriendlyPassable(Int2.FromPosition(c + new Vec2(0, -2.2f)));
            return t.Center;
        }

        // ------------------------------------------------------------------ player
        private void TickPlayer(Unit g, float dt)
        {
            var input = g.MoveInput.ClampMagnitude(1f);
            if (g.Status.Stunned) input = Vec2.Zero;
            float speed = _move.SpeedOf(g);
            // short acceleration / deceleration for responsive but weighty feel
            Vec2 desired = input * speed;
            float accel = input.SqrMagnitude > 0.01f ? 28f : 36f;
            g.Velocity = Vec2.MoveTowards(g.Velocity, desired, accel * dt);
            if (g.Velocity.SqrMagnitude > 1e-4f)
            {
                _move.Step(g, g.Velocity * dt);
                if (Math.Abs(g.Velocity.X) > 0.05f) g.FacingX = Math.Sign(g.Velocity.X);
                g.State = UnitState.Moving;
            }
            else g.State = UnitState.Idle;

            if (DashRequested)
            {
                DashRequested = false;
                if (g.DashCooldown <= 0f && !g.Status.Rooted) Dash(g, input.SqrMagnitude > 0.01f ? input.Normalized : new Vec2(g.FacingX, 0));
            }
            if (UltimateRequested)
            {
                UltimateRequested = false;
                if (g.UltCooldown <= 0f && !g.Status.Stunned) CastUltimate(g);
            }
        }

        public void Dash(Unit g, Vec2 dir)
        {
            g.DashCooldown = _spawner.DashCooldown(g);
            g.IFrames = 0.35f;
            float dist = g.General.DashDistance;
            int steps = 10;
            for (int i = 0; i < steps; i++) _move.Step(g, dir * (dist / steps));
            _w.Emit(SimEventType.Dash, g.Position, 0, g.General.Element, g.Id);
        }

        private void AutoAttack(Unit g)
        {
            if (g.Status.Stunned || g.AttackCooldown > 0f) return;
            float range = g.Attack.Range + g.Radius;
            Unit best = null;
            float bd = float.MaxValue;
            var cur = _w.Get(g.TargetId) as Unit;
            if (cur != null && cur.Targetable && Vec2.Distance(cur.Position, g.Position) - cur.Radius <= range) best = cur;
            else
                foreach (var e in _w.UnitsInRadius(Team.Enemy, g.Position, range + 0.5f, _tmp))
                {
                    float d = Vec2.SqrDistance(e.Position, g.Position);
                    if (e.IsElite) d -= 1f;
                    if (d < bd || (d == bd && best != null && e.Id < best.Id)) { bd = d; best = e; }
                }
            if (best == null || Vec2.Distance(best.Position, g.Position) - best.Radius > range) return;
            g.TargetId = best.Id;
            g.AttackCooldown = g.Attack.Interval;
            g.AttackAnim = 0.25f;
            if (Math.Abs(best.Position.X - g.Position.X) > 0.05f && g.Velocity.SqrMagnitude < 0.5f) g.FacingX = Math.Sign(best.Position.X - g.Position.X);
            float dmg = g.Attack.Damage * g.Buffs.DamageMult;
            _fx.Attack(g, best, g.Attack, dmg, 1);
            if (_w.Mods.Has(Hook.GeneralLifesteal)) _combat.Heal(g, dmg * _w.Mods.HookValue(Hook.GeneralLifesteal));
        }

        // ------------------------------------------------------------------ ultimate
        public void CastUltimate(Unit g)
        {
            var ult = g.General.Ultimate;
            g.UltCooldown = _spawner.UltCooldown(g);
            float power = 1f + _w.Mods.Percent(Stat.UltPower, ScopeOf(g));
            power *= g.Evolution?.UltPowerMult ?? 1f;
            if (!g.IsPlayerControlled) power *= 0.8f;
            _w.Emit(SimEventType.UltimateCast, g.Position, ult.CastRadius, g.General.Element, g.Id, ult.Name);
            foreach (var e in ult.Effects) Execute(g, e, power);
            if (g.Evolution != null) foreach (var e in g.Evolution.ExtraUltEffects) Execute(g, e, power);
            if (_w.Mods.Has(Hook.WallsMendOnUltimate))
                foreach (var b in _w.Buildings)
                    if (b.IsWallLike && !b.Ruined && !b.Dead && Vec2.Distance(b.Center, g.Position) <= 8f) _combat.Heal(b, b.MaxHp * _w.Mods.HookValue(Hook.WallsMendOnUltimate));
            if (_w.Mods.Has(Hook.EchoUltimate) && g.IsPlayerControlled) g.UltCooldown *= 1f - _w.Mods.HookValue(Hook.EchoUltimate);
        }

        private DamageInfo Info(Unit g, AbilityEffect e, float power)
        {
            var d = DamageInfo.Simple(e.Damage * power, e.Element, e.Status * _combat.StatusPower(g), EffectsSystem.SourceOf(g), g.Id, g.General.Ultimate.Name);
            d.Origin = g.Position;
            return d;
        }

        public void Execute(Unit g, AbilityEffect e, float power)
        {
            Vec2 p = g.Position;
            switch (e.Kind)
            {
                case AbilityEffectKind.AoeDamage:
                    _fx.AreaDamage(Team.Enemy, p, e.Radius, Info(g, e, power), false);
                    break;
                case AbilityEffectKind.ChainDamage:
                {
                    var hit = new HashSet<int>();
                    Vec2 from = p;
                    for (int i = 0; i < e.Count; i++)
                    {
                        var next = _combat.NearestUnit(Team.Enemy, from, i == 0 ? e.Radius : 3.5f, hit);
                        if (next == null) break;
                        hit.Add(next.Id);
                        _w.Emit(SimEventType.Reaction, next.Position, 0, e.Element, next.Id, "arc", -1);
                        var d = Info(g, e, power);
                        d.Origin = from;
                        _combat.Deal(next, d);
                        from = next.Position;
                    }
                    break;
                }
                case AbilityEffectKind.Zone:
                {
                    Vec2 at = ClusterCenter(p, Math.Max(e.Radius, 4f)) ?? p;
                    var d = Info(g, e, power);
                    _combat.SpawnZone(e.Zone, Team.Player, at, e.Radius, e.Duration, e.Damage * power, e.Element, e.Status, d);
                    break;
                }
                case AbilityEffectKind.AllyShield:
                    foreach (var u in _w.UnitsInRadius(Team.Player, p, e.Radius, _tmp)) u.Status.AddGuard(Math.Min(0.75f, e.Value * Math.Min(power, 1.3f)), e.Duration);
                    break;
                case AbilityEffectKind.AllyHeal:
                    foreach (var u in _w.UnitsInRadius(Team.Player, p, e.Radius, _tmp)) _combat.Heal(u, u.MaxHp * e.Value * power);
                    foreach (var b in _w.Buildings) if (!b.Dead && !b.Ruined && Vec2.Distance(b.Center, p) <= e.Radius) _combat.Heal(b, b.MaxHp * e.Value * power);
                    break;
                case AbilityEffectKind.Volley:
                {
                    var targets = _w.UnitsInRadius(Team.Enemy, p, e.Radius, _tmp).ToArray();
                    if (targets.Length == 0) break;
                    Array.Sort(targets, (a, b) => a.Id.CompareTo(b.Id));
                    for (int i = 0; i < e.Count; i++)
                    {
                        var t = targets[_w.Rng.Range(0, targets.Length)];
                        if (t.Dead) continue;
                        var d = Info(g, e, power);
                        d.Knockback = e.Value;
                        _combat.Deal(t, d);
                        _w.Emit(SimEventType.ProjectileFired, p, 0, e.Element, g.Id, "volley", t.Id);
                    }
                    break;
                }
                case AbilityEffectKind.Mark:
                {
                    var list = new List<Unit>(_w.UnitsInRadius(Team.Enemy, p, e.Radius, _tmp));
                    list.Sort((a, b) => Priority(b).CompareTo(Priority(a)) != 0 ? Priority(b).CompareTo(Priority(a)) : a.Id.CompareTo(b.Id));
                    for (int i = 0; i < Math.Min(e.Count, list.Count); i++)
                    {
                        list[i].Status.AddVulnerability(e.Value, e.Duration);
                        _combat.Deal(list[i], Info(g, e, power));
                    }
                    break;
                }
                case AbilityEffectKind.CastleShield:
                    _w.CastleGuardAmount = Math.Max(_w.CastleGuardAmount, e.Value);
                    _w.CastleGuardTimer = Math.Max(_w.CastleGuardTimer, e.Duration);
                    break;
                case AbilityEffectKind.Barrier:
                    if (SpawnBarrier != null)
                    {
                        var dir = FacingTowardThreat(g);
                        var side = new Vec2(-dir.Y, dir.X);
                        for (int i = 0; i < e.Count; i++)
                        {
                            float o = i - (e.Count - 1) / 2f;
                            var pos = p + dir * e.Radius + side * o;
                            var b = SpawnBarrier("wall_stone", pos);
                            if (b != null) b.Lifetime = e.Duration;
                        }
                    }
                    break;
                case AbilityEffectKind.LineStrike:
                {
                    var dir = g.MoveInput.SqrMagnitude > 0.01f ? g.MoveInput.Normalized : FacingTowardThreat(g);
                    Vec2 start = p;
                    int steps = 12;
                    var hit = new HashSet<int>();
                    for (int i = 0; i < steps; i++)
                    {
                        _move.Step(g, dir * (e.Value / steps));
                        foreach (var u in _w.UnitsInRadius(Team.Enemy, g.Position, e.Radius, _tmp).ToArray())
                            if (hit.Add(u.Id)) _combat.Deal(u, Info(g, e, power));
                    }
                    g.IFrames = 0.4f;
                    _w.Emit(SimEventType.Dash, g.Position, Vec2.Distance(start, g.Position), e.Element, g.Id);
                    break;
                }
                case AbilityEffectKind.Summon:
                    if (SpawnAllySummon != null)
                        for (int i = 0; i < e.Count; i++)
                            SpawnAllySummon(e.SummonId, p + new Vec2((i % 3 - 1) * 0.8f, (i / 3 - 0.5f) * 0.8f), e.Duration);
                    break;
                case AbilityEffectKind.Pull:
                    foreach (var u in _w.UnitsInRadius(Team.Enemy, p, e.Radius, _tmp).ToArray())
                    {
                        if (u.IsBoss) continue;
                        Vec2 d = p - u.Position;
                        float m = Math.Min(e.Value, Math.Max(0f, d.Magnitude - 0.8f));
                        if (m > 0 && _combat.PositionWalkable(u, u.Position + d.Normalized * m)) u.Position += d.Normalized * m;
                        if (e.Damage > 0) _combat.Deal(u, Info(g, e, power));
                    }
                    break;
                case AbilityEffectKind.Knockwave:
                    foreach (var u in _w.UnitsInRadius(Team.Enemy, p, e.Radius, _tmp).ToArray())
                    {
                        var d = Info(g, e, power);
                        d.Knockback = e.Value;
                        _combat.Deal(u, d);
                    }
                    break;
                case AbilityEffectKind.FreezeArea:
                    foreach (var u in _w.UnitsInRadius(Team.Enemy, p, e.Radius, _tmp).ToArray())
                    {
                        u.Status.FreezeTimer = Math.Max(u.Status.FreezeTimer, u.Status.CcDuration(e.Duration, u.Def.CcResist));
                        _w.Emit(SimEventType.Freeze, u.Position, u.Status.FreezeTimer, ElementId.Ice, u.Id);
                        if (e.Damage > 0) _combat.Deal(u, Info(g, e, power));
                    }
                    break;
                case AbilityEffectKind.Execute:
                {
                    var list = new List<Unit>(_w.UnitsInRadius(Team.Enemy, p, e.Radius, _tmp));
                    list.Sort((a, b) => a.HpFraction.CompareTo(b.HpFraction) != 0 ? a.HpFraction.CompareTo(b.HpFraction) : a.Id.CompareTo(b.Id));
                    for (int i = 0; i < Math.Min(Math.Max(1, e.Count), list.Count); i++)
                    {
                        var t = list[i];
                        var d = Info(g, e, power);
                        if (!t.IsElite && t.HpFraction <= e.Value) d.Amount = t.Hp * 4f + 50f;
                        else d.Amount *= 1f + 2f * (1f - t.HpFraction);
                        _combat.Deal(t, d);
                    }
                    break;
                }
                case AbilityEffectKind.TroopFrenzy:
                    foreach (var u in _w.UnitsInRadius(Team.Player, p, e.Radius, _tmp)) u.Status.AddFrenzy(e.Value * power, e.Duration);
                    break;
                case AbilityEffectKind.RepairStructures:
                    foreach (var b in _w.Buildings)
                    {
                        if (b.Dead || Vec2.Distance(b.Center, p) > e.Radius) continue;
                        if (b.Ruined && b.IsWallLike)
                        {
                            b.Ruined = false; b.Hp = b.MaxHp * 0.25f; _w.TopologyVersion++;
                            _w.Emit(SimEventType.Building, b.Center, 0, ElementId.Earth, b.Id, "restored");
                        }
                        else if (!b.Ruined) _combat.Heal(b, b.MaxHp * e.Value * power);
                    }
                    break;
                case AbilityEffectKind.SelfBuff:
                    g.Status.AddGuard(Math.Min(0.7f, e.Value), e.Duration);
                    g.Status.AddFrenzy(e.Value, e.Duration);
                    break;
            }
        }

        private static float Priority(Unit u) => (u.IsBoss ? 4f : 0f) + (u.IsElite ? 2f : 0f) + (u.Def.Behavior == EnemyBehavior.Siege || u.Def.Behavior == EnemyBehavior.GateBreaker ? 2f : 0f) + u.MaxHp * 0.001f;

        private Vec2 FacingTowardThreat(Unit g)
        {
            var e = _combat.NearestUnit(Team.Enemy, g.Position, 10f, null);
            if (e != null) return (e.Position - g.Position).Normalized;
            return new Vec2(g.FacingX, 0);
        }

        private Vec2? ClusterCenter(Vec2 p, float r)
        {
            var list = _w.UnitsInRadius(Team.Enemy, p, r, _tmp);
            if (list.Count == 0) return null;
            Vec2 s = Vec2.Zero;
            foreach (var u in list) s += u.Position;
            return s / list.Count;
        }

        // ------------------------------------------------------------------ AI generals
        /// <summary>Default post: 40% of the way from the castle toward the lane, snapped to a passable tile.</summary>
        public Vec2 PostForLane(SpawnLane lane)
        {
            var c = _w.Castle.Center;
            var p = Vec2.Lerp(c, lane.Center, 0.4f);
            return _nav.NearestFriendlyPassable(Int2.FromPosition(p)).Center;
        }

        private void TickAI(Unit g, float dt)
        {
            Vec2 post = Posts.TryGetValue(g.AiSlot, out var pp) ? pp : g.Home;
            g.Home = post;
            if (!_w.IsNight) { MoveTowards(g, post, dt, 0.3f); return; }

            // retreat when badly hurt; regenerate near the castle
            if (g.HpFraction < 0.3f) g.State = UnitState.Retreating;
            if (g.State == UnitState.Retreating)
            {
                g.AiReason = "Rút lui hồi phục";
                var safe = RespawnPoint();
                if (MoveTowards(g, safe, dt, 1f)) g.Hp = Math.Min(g.MaxHp, g.Hp + g.MaxHp * 0.04f * dt);
                if (g.HpFraction > 0.75f) g.State = UnitState.Idle;
                return;
            }

            g.AiDecisionTimer -= dt;
            if (g.AiDecisionTimer <= 0f)
            {
                g.AiDecisionTimer = 0.4f;
                var best = Decide(g, post, out string reason);
                g.TargetId = best?.Id ?? -1;
                g.AiReason = reason;
                if (ShouldUlt(g)) CastUltimate(g);
            }
            var t = _w.Get(g.TargetId) as Unit;
            if (t != null && t.Targetable)
            {
                float want = g.General.Ai == AiStyle.Frontline ? g.Attack.Range * 0.8f : g.Attack.Range * 0.85f;
                MoveTowards(g, t.Position, dt, want);
            }
            else MoveTowards(g, post, dt, 0.4f);
        }

        private Unit Decide(Unit g, Vec2 post, out string reason)
        {
            const float leash = 7.5f;
            Unit best = null;
            float bestScore = float.MinValue;
            reason = "Giữ vị trí";
            string bestReason = reason;
            foreach (var e in _w.UnitsInRadius(Team.Enemy, post, leash + 3f, _tmp))
            {
                if (Vec2.Distance(e.Position, post) > leash + 1f) continue;
                float dist = Vec2.Distance(e.Position, g.Position);
                float flow = Math.Min(60f, _nav.DistanceToCastle(e.Position));
                float sThreat = (1f - flow / 60f) * 2f;
                float sNear = 3f / (1f + dist);
                float sSiege = e.Def.Behavior == EnemyBehavior.Siege || e.Def.Behavior == EnemyBehavior.GateBreaker ? 1.5f : 0f;
                float sBoss = e.IsElite ? 1f : 0f;
                var aura = e.Status.DominantAura();
                float sCombo = aura != ElementId.None && aura != g.General.Element && ReactionTable.Get(aura, g.General.Element)?.Id != ReactionId.Resonance ? 1.2f : 0f;
                var tgt = _w.Get(e.TargetId);
                float sProtect = tgt != null && tgt.Team == Team.Player && tgt.HpFraction < 0.5f ? 1f : 0f;
                float score = sThreat + sNear + sSiege + sBoss + sCombo + sProtect;
                if (score > bestScore || (score == bestScore && best != null && e.Id < best.Id))
                {
                    bestScore = score;
                    best = e;
                    bestReason = sSiege > 0 ? "Chặn công thành" : sCombo > 0 ? "Kích phản ứng " + ReactionTable.Get(aura, g.General.Element).Name : sProtect > 0 ? "Bảo vệ đồng minh" : sThreat > 1.2f ? "Đe doạ lâu đài" : "Kẻ địch gần";
                }
            }
            reason = best != null ? bestReason : reason;
            return best;
        }

        private bool ShouldUlt(Unit g)
        {
            if (g.UltCooldown > 0f || g.Status.Stunned) return false;
            var ult = g.General.Ultimate;
            int n = 0;
            bool elite = false;
            foreach (var e in _w.UnitsInRadius(Team.Enemy, g.Position, ult.CastRadius, _tmp)) { n++; elite |= e.IsElite; }
            if (g.General.Ai == AiStyle.Support)
            {
                int hurt = 0;
                foreach (var u in _w.UnitsInRadius(Team.Player, g.Position, ult.CastRadius, _tmp)) if (u.HpFraction < 0.5f) hurt++;
                return hurt >= 3 || (_w.Castle != null && _w.Castle.HpFraction < 0.4f && n >= 2);
            }
            return n >= 4 || (elite && n >= 1);
        }

        private bool MoveTowards(Unit g, Vec2 p, float dt, float arrive)
        {
            if (Vec2.Distance(g.Position, p) <= arrive) return true;
            if (_move.LineWalkable(g, g.Position, p)) return _move.MoveTo(g, p, dt, arrive);
            g.RepathTimer -= dt;
            if (g.Path.Count == 0 || g.PathIndex >= g.Path.Count || g.RepathTimer <= 0f)
            {
                g.RepathTimer = 0.7f;
                _nav.FindFriendlyPath(Int2.FromPosition(g.Position), Int2.FromPosition(p), g.Path, 700);
                g.PathIndex = 0;
            }
            if (g.PathIndex < g.Path.Count && _move.MoveTo(g, g.Path[g.PathIndex].Center, dt, 0.15f)) g.PathIndex++;
            return false;
        }
    }
}
