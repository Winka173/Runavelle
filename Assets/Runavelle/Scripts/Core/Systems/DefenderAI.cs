using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>
    /// Autonomous soldiers (no player commands) and auto-firing structures.
    /// Soldiers defend a home area around their Barracks with a bounded leash, score threats by proximity and
    /// threat-to-Castle, then return and reform.
    /// </summary>
    public sealed class DefenderAI
    {
        private readonly World _w;
        private readonly CombatSystem _combat;
        private readonly EffectsSystem _fx;
        private readonly NavigationService _nav;
        private readonly Movement _move;
        private readonly Spawner _spawner;
        private readonly List<Unit> _tmp = new List<Unit>(64);
        private readonly List<Unit> _targets = new List<Unit>(8);

        public DefenderAI(World w, CombatSystem c, EffectsSystem fx, NavigationService nav, Movement m, Spawner s)
        {
            _w = w; _combat = c; _fx = fx; _nav = nav; _move = m; _spawner = s;
        }

        // ------------------------------------------------------------------ soldiers
        public Vec2 FormationSlot(Unit u)
        {
            var b = _w.Get(u.OwnerBuildingId) as Building;
            if (b == null) return u.Home;
            int idx = Math.Max(0, b.Squad.IndexOf(u.Id));
            Vec2 c = b.Center, outward;
            float ahead;
            if ((_w.IsNight || _w.Rally) && b.RallyPost.HasValue)
            {
                // rally: melee line at the post facing the front, ranged squads a step behind it
                c = b.RallyPost.Value;
                outward = b.RallyDir;
                ahead = u.Attack.Range > AiTuning.RangedMinRange ? -AiTuning.RallyRangedBack : 0f;
            }
            else
            {
                // stand in front of the barracks, toward the castle's threats (away from castle)
                outward = _w.Castle != null ? (c - _w.Castle.Center).Normalized : new Vec2(1, 0);
                ahead = b.Size * 0.5f + 0.9f;
            }
            if (outward.SqrMagnitude < 0.01f) outward = new Vec2(1, 0);
            Vec2 side = new Vec2(-outward.Y, outward.X);
            Vec2 slot = c + outward * ahead + side * ((idx % 4) - 1.5f) * 0.7f - outward * (idx / 4) * 0.7f;
            var t = Int2.FromPosition(slot);
            if (!_w.FriendlyPassable(t.X, t.Y)) slot = _nav.NearestFriendlyPassable(t).Center;
            return slot;
        }

        public void TickSoldier(Unit u, float dt)
        {
            float lust = _w.Mods.Has(Hook.Bloodlust) && u.HpFraction < 0.5f ? 1f + _w.Mods.HookValue(Hook.Bloodlust) : 1f;
            u.AttackCooldown -= dt * u.Status.AttackSpeedMult() * u.Buffs.AttackSpeedMult * lust * (1f + _w.Mods.Percent(Stat.AttackSpeed, Scope.Soldiers, u.Def.Class));
            u.SecondaryCooldown -= dt;
            u.AttackAnim = Math.Max(0f, u.AttackAnim - dt);
            if (u.LifeTimer > 0f && (u.LifeTimer -= dt) <= 0f) { u.Dead = true; return; }
            if (u.Status.Stunned) return;
            var barracks = _w.Get(u.OwnerBuildingId) as Building;
            Vec2 home = barracks != null && !barracks.Dead ? barracks.Center : u.Home;
            if (_w.IsNight && barracks != null && barracks.RallyPost.HasValue) home = barracks.RallyPost.Value; // leash anchored at the post
            u.Home = home;

            if (!_w.IsNight)
            {
                u.TargetId = -1;
                FollowPathTo(u, FormationSlot(u), dt, 0.1f);
                return;
            }

            if (_w.Mods.Has(Hook.TacticalRetreat) && u.HpFraction < 0.3f) u.State = UnitState.Retreating;
            if (u.State == UnitState.Retreating)
            {
                if (u.HpFraction > 0.8f) u.State = UnitState.Idle;
                else { FollowPathTo(u, FormationSlot(u), dt, 0.2f); return; }
            }

            u.AiDecisionTimer -= dt;
            if (u.IgnoreTimer > 0f && (u.IgnoreTimer -= dt) <= 0f) u.IgnoreTargetId = -1;
            var target = _w.Get(u.TargetId) as Unit;
            if (u.AiDecisionTimer <= 0f || target == null || !target.Targetable)
            {
                u.AiDecisionTimer = 0.25f + (u.Id % 4) * 0.03f;
                var prev = u.TargetId;
                target = ChooseThreat(u, home);
                u.TargetId = target?.Id ?? -1;
                if (u.TargetId != prev) u.ChaseTimer = 0f;
            }
            if (target == null)
            {
                u.State = UnitState.Returning;
                u.AiReason = "Về đội hình";
                FollowPathTo(u, FormationSlot(u), dt, 0.15f);
                return;
            }
            float range = u.Attack.Range + u.Buffs.RangeBonus;
            float dist = Vec2.Distance(u.Position, target.Position) - target.Radius;
            // ranged spacing: archers/mages step back toward home when a melee enemy is on top of them
            if (range > AiTuning.RangedMinRange && !u.IsFlying && u.AttackCooldown > 0.2f)
            {
                var close = NearestEnemy(u, AiTuning.KiteTrigger);
                if (close != null && Vec2.Distance(u.Position, home) < u.Leash)
                {
                    Vec2 away = (u.Position - close.Position).Normalized;
                    Vec2 toHome = (home - u.Position).Normalized;
                    Vec2 pref = (away + toHome * 0.6f).Normalized;
                    // preferred retreat, then rotations around "away" (walls/buildings behind the archer)
                    for (int k = 0; k < Kite.Length; k++)
                    {
                        Vec2 dir = k == 0 ? pref : Rotate(away, Kite[k]);
                        Vec2 step = dir * AiTuning.KiteStep;
                        if (!_move.LineWalkable(u, u.Position, u.Position + step)) continue;
                        u.State = UnitState.Moving;
                        u.AiReason = "Giữ khoảng cách";
                        _move.MoveTo(u, u.Position + step, dt, 0.1f);
                        return;
                    }
                }
            }
            if (dist <= range)
            {
                u.ChaseTimer = 0f;
                u.AiReason = target.Def.Behavior == EnemyBehavior.Siege || target.Def.Behavior == EnemyBehavior.GateBreaker ? "Chặn công thành" : "Giao chiến";
                u.State = UnitState.Fighting;
                u.FacingX = Math.Sign(target.Position.X - u.Position.X + 0.001f);
                if (u.AttackCooldown <= 0f)
                {
                    u.AttackCooldown = u.Attack.Interval;
                    u.AttackAnim = 0.25f;
                    float dmg = _w.Mods.Apply(u.Attack.Damage, Stat.Damage, Scope.Soldiers, u.Def.Class, u.Attack.Element) * u.Buffs.DamageMult;
                    _fx.Attack(u, target, u.Attack, dmg, u.Attack.ProjectileSpeed > 0 ? (u.Attack.Element == ElementId.None ? 0 : 1) : 0);
                }
                return;
            }
            // Battlemage-style secondary spell when the target is out of melee reach
            if (u.SecondaryAttack != null && u.SecondaryCooldown <= 0f && dist <= u.SecondaryAttack.Range)
            {
                u.SecondaryCooldown = u.SecondaryAttack.Interval;
                float dmg = _w.Mods.Apply(u.SecondaryAttack.Damage, Stat.Damage, Scope.Soldiers, u.Def.Class, u.SecondaryAttack.Element) * u.Buffs.DamageMult;
                _fx.Attack(u, target, u.SecondaryAttack, dmg, 1);
            }
            // pursuit cap: fast units give up a target they cannot reach and return to their post
            if (u.Speed >= AiTuning.FastSpeed && (u.ChaseTimer += dt) > AiTuning.MaxChaseSeconds)
            {
                u.IgnoreTargetId = target.Id;
                u.IgnoreTimer = AiTuning.ChaseCooldown;
                u.TargetId = -1;
                u.ChaseTimer = 0f;
                u.State = UnitState.Returning;
                u.AiReason = "Bỏ truy đuổi, quay về";
                FollowPathTo(u, FormationSlot(u), dt, 0.15f);
                return;
            }
            u.State = UnitState.Moving;
            u.AiReason = "Đánh chặn";
            FollowPathTo(u, target.Position, dt, range * 0.85f);
        }

        private static readonly float[] Kite = { 0f, 0f, 0.785f, -0.785f, 1.571f, -1.571f };
        private static Vec2 Rotate(Vec2 v, float a)
        {
            float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
            return new Vec2(v.X * c - v.Y * s, v.X * s + v.Y * c);
        }

        private Unit NearestEnemy(Unit u, float r)
        {
            Unit best = null;
            float bd = r;
            foreach (var e in _w.UnitsInRadius(Team.Enemy, u.Position, r, _tmp))
            {
                if (e.IsFlying || e.Attack == null || e.Attack.Range > 2f) continue; // only melee threats push archers back
                float d = Vec2.Distance(e.Position, u.Position);
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        /// <summary>Utility scoring: nearby threats, threat to the Castle, enemies hitting our structures, siege priority.</summary>
        private Unit ChooseThreat(Unit u, Vec2 home)
        {
            float leash = u.Leash;
            float search = leash + u.Def.AggroRange;
            Unit best = null;
            float bestScore = float.MinValue;
            bool canHitAir = u.Attack.HitsAir || u.IsFlying || (u.SecondaryAttack != null && u.SecondaryAttack.HitsAir);
            float maxFlow = 60f;
            foreach (var e in _w.UnitsInRadius(Team.Enemy, home, search, _tmp))
            {
                if (e.IsFlying && !canHitAir) continue;
                if (e.Id == u.IgnoreTargetId) continue;
                float dHome = Vec2.Distance(e.Position, home);
                if (dHome > leash + AiTuning.LeashSlack) continue; // bounded pursuit: never chase across the map
                float dSelf = Vec2.Distance(e.Position, u.Position);
                float flow = Math.Min(maxFlow, _nav.DistanceToCastle(e.Position));
                float score = AiTuning.WeightProximity / (1f + dSelf) + (1f - flow / maxFlow) * AiTuning.WeightCastleThreat;
                var tgt = _w.Get(e.TargetId);
                if (tgt != null && tgt.Team == Team.Player) score += tgt.IsStructure ? AiTuning.BonusHittingStructure : AiTuning.BonusHittingUnit;
                if (e.Def.Behavior == EnemyBehavior.Siege || e.Def.Behavior == EnemyBehavior.GateBreaker) score += AiTuning.BonusSiege;
                if (e.IsFlying && u.IsFlying) score += AiTuning.BonusAntiAirMatch;
                if (u.Def.Class == SoldierClass.Spearman && e.IsLarge) score += AiTuning.BonusSpearVsLarge;
                if (InBreach(e)) score += AiTuning.BonusInsideBreach;
                if (e.Id == u.TargetId) score += AiTuning.Stickiness; // stickiness avoids target flapping
                if (score > bestScore || (score == bestScore && best != null && e.Id < best.Id)) { bestScore = score; best = e; }
            }
            return best;
        }

        /// <summary>Enemy standing within 1.5 tiles of a wall that broke this night (the gap the army must plug).</summary>
        private bool InBreach(Unit e)
        {
            var br = _w.Analytics.Breaches;
            for (int i = br.Count - 1; i >= 0; i--)
            {
                if (br[i].Night != _w.Night) break;
                float dx = br[i].X - e.Position.X, dy = br[i].Y - e.Position.Y;
                if (dx * dx + dy * dy <= 2.25f) return true;
            }
            return false;
        }

        private void FollowPathTo(Unit u, Vec2 goal, float dt, float arrive)
        {
            if (Vec2.Distance(u.Position, goal) <= arrive) { u.Path.Clear(); return; }
            if (u.IsFlying || _move.LineWalkable(u, u.Position, goal))
            {
                u.Path.Clear();
                _move.MoveTo(u, goal, dt, arrive);
                return;
            }
            u.RepathTimer -= dt;
            if (u.Path.Count == 0 || u.PathIndex >= u.Path.Count || u.RepathTimer <= 0f)
            {
                u.RepathTimer = 0.8f;
                _nav.FindFriendlyPath(Int2.FromPosition(u.Position), Int2.FromPosition(goal), u.Path, 600);
                u.PathIndex = 0;
            }
            if (u.PathIndex < u.Path.Count)
            {
                if (_move.MoveTo(u, u.Path[u.PathIndex].Center, dt, 0.15f)) u.PathIndex++;
            }
            else _move.MoveTo(u, goal, dt, arrive);
        }

        // ------------------------------------------------------------------ structures
        public void TickStructure(Building b, float dt)
        {
            if (b.Level.Attack == null || b.Ruined || b.Dead || b.DisabledTimer > 0f) return;
            if (!_w.IsNight) return;
            b.AttackCooldown -= dt * b.Status.AttackSpeedMult();
            if (b.AttackCooldown > 0f) return;
            bool secondary = b.Secondary != ElementId.None && (b.ShotCounter % 2 == 1);
            var a = _spawner.BuildingAttack(b, secondary);
            _targets.Clear();
            PickTowerTargets(b, a, _targets);
            if (_targets.Count == 0) { b.AttackCooldown = 0.15f; return; }
            b.AttackCooldown = a.Interval;
            b.ShotCounter++;
            int visual = b.Def.Family == BuildingFamily.MagicTower ? 1 : (a.Element == ElementId.Lightning ? 3 : 0);
            foreach (var t in _targets) _fx.Attack(b, t, a, a.Damage, visual);
        }

        private void PickTowerTargets(Building b, AttackSpec a, List<Unit> into)
        {
            var c = b.Center;
            float r = a.Range + b.Size * 0.5f;
            bool sniper = b.Level.Branch == "A" && b.Def.Family == BuildingFamily.ArcherTower;
            bool eliteHunter = a.Element == ElementId.Shadow;
            var cands = _w.UnitsInRadius(Team.Enemy, c, r, _tmp);
            // score: closest to the castle by flow (threat), snipers prefer large/elite, shadow prefers elites
            for (int pick = 0; pick < Math.Max(1, a.Targets); pick++)
            {
                Unit best = null;
                float bestScore = float.MinValue;
                foreach (var e in cands)
                {
                    if (into.Contains(e)) continue;
                    if (e.IsFlying && !a.HitsAir) continue;
                    float flow = _nav.DistanceToCastle(e.Position);
                    if (float.IsInfinity(flow)) flow = 80f;
                    float score = -flow;
                    if (sniper) score += e.MaxHp * 0.02f + (e.IsElite ? 10f : 0f);
                    if (eliteHunter && e.IsElite) score += 15f;
                    if (e.IsFlying && b.Def.Family == BuildingFamily.ArcherTower) score += 3f;
                    if (score > bestScore || (score == bestScore && best != null && e.Id < best.Id)) { bestScore = score; best = e; }
                }
                if (best == null) break;
                into.Add(best);
            }
        }
    }
}
