using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Attacks, projectiles, ground zones and telegraphed effects.</summary>
    public sealed class EffectsSystem
    {
        private readonly World _w;
        private readonly CombatSystem _combat;
        private readonly List<Unit> _tmp = new List<Unit>(64);

        public EffectsSystem(World w, CombatSystem combat) { _w = w; _combat = combat; }

        public static SourceKind SourceOf(Actor a)
        {
            if (a is Building b)
            {
                switch (b.Def.Family)
                {
                    case BuildingFamily.Castle: return SourceKind.Castle;
                    case BuildingFamily.ArcherTower: return SourceKind.ArcherTower;
                    case BuildingFamily.MagicTower: return SourceKind.MagicTower;
                    default: return SourceKind.Wall;
                }
            }
            var u = (Unit)a;
            if (u.Team == Team.Enemy) return SourceKind.Enemy;
            if (u.Role == UnitRole.General) return u.IsPlayerControlled ? SourceKind.PlayerGeneral : SourceKind.AIGeneral;
            return SourceKind.Soldier;
        }

        public static string NameOf(Actor a)
        {
            if (a is Building b) return b.Level.Name;
            var u = (Unit)a;
            if (u.Team == Team.Player && u.Role == UnitRole.Soldier && u.Branch != null) return u.Branch.Name;
            return u.Def.Name;
        }

        public DamageInfo MakeInfo(Actor attacker, AttackSpec a, float damage)
        {
            var d = DamageInfo.Simple(damage, a.Element, a.StatusAmount * _combat.StatusPower(attacker), SourceOf(attacker), attacker.Id, NameOf(attacker));
            d.ArmorPierce = a.ArmorPierce;
            d.StructureMult = a.StructureMult;
            d.GateMult = a.GateMult;
            d.LargeMult = a.LargeMult;
            d.FlyerMult = a.FlyerMult;
            d.Knockback = a.Knockback;
            d.Origin = attacker.Position;
            d.SlowOnHit = a.SlowOnHit;
            d.StunChance = a.StunChance;
            d.ChainCount = a.ChainCount;
            d.ChainRange = a.ChainRange;
            return d;
        }

        /// <summary>Executes an attack (melee hit or projectile launch) from attacker to target.</summary>
        public void Attack(Actor attacker, Actor target, AttackSpec a, float damage, int visual = 0)
        {
            var info = MakeInfo(attacker, a, damage);
            // Blind (Solar Flare) makes attacks miss half the time
            if (attacker.Status.BlindTimer > 0f && _w.Rng.Chance(0.5f)) return;
            // Aura-granted element (e.g. Kaelen's fire) rides on a second application
            var grant = attacker.Buffs.GrantElement;
            if (grant == ElementId.None && attacker is Unit su && su.SecondaryElement != ElementId.None) { grant = su.SecondaryElement; }
            if (a.ProjectileSpeed <= 0f)
            {
                _w.Emit(SimEventType.MeleeSwing, attacker.Position, 0, a.Element, attacker.Id, null, target.Id);
                Impact(target, target.Position, info, a.Splash);
                if (grant != ElementId.None && !target.Dead) _combat.ApplyElement(target, grant, attacker.Buffs.GrantElement != ElementId.None ? attacker.Buffs.GrantStatus : 18f, info);
                return;
            }
            var p = new Projectile
            {
                Id = _w.AllocId(), Team = attacker.Team, Position = attacker.Position, PrevPosition = attacker.Position, TargetId = target.Id,
                TargetPos = _w.ClosestPoint(attacker.Position, target), Speed = a.ProjectileSpeed, Payload = info, Splash = a.Splash, Element = a.Element, Visual = visual,
            };
            if (grant != ElementId.None) p.GrantElement = grant;
            p.GrantStatus = attacker.Buffs.GrantElement != ElementId.None ? attacker.Buffs.GrantStatus : 18f;
            _w.Projectiles.Add(p);
            _w.Emit(SimEventType.ProjectileFired, attacker.Position, 0, a.Element, attacker.Id, null, target.Id);
        }

        private void Impact(Actor target, Vec2 at, DamageInfo info, float splash)
        {
            if (splash > 0f)
            {
                Team victims = info.Source == SourceKind.Enemy ? Team.Player : Team.Enemy;
                var hits = _w.UnitsInRadius(victims, at, splash, _tmp).ToArray();
                bool primaryHit = false;
                foreach (var u in hits)
                {
                    if (u == target) primaryHit = true;
                    var d = info;
                    if (u != target) { d.Amount *= 0.7f; d.ChainCount = 0; }
                    _combat.Deal(u, d);
                }
                if (!primaryHit && target != null && !target.Dead) _combat.Deal(target, info);
            }
            else if (target != null) _combat.Deal(target, info);
        }

        public void TickProjectiles(float dt)
        {
            for (int i = 0; i < _w.Projectiles.Count; i++)
            {
                var p = _w.Projectiles[i];
                if (p.Dead) continue;
                p.PrevPosition = p.Position;
                var target = _w.Get(p.TargetId);
                if (target != null && !target.Dead && target.Targetable) p.TargetPos = _w.ClosestPoint(p.Position, target);
                p.Position = Vec2.MoveTowards(p.Position, p.TargetPos, p.Speed * dt);
                if (Vec2.SqrDistance(p.Position, p.TargetPos) > 0.01f) continue;
                p.Dead = true;
                if (target != null && !target.Dead && target.Targetable)
                {
                    // Shield Guardians intercept enemy projectiles aimed at nearby allies
                    if (p.Team == Team.Enemy && target.Team == Team.Player && TryIntercept(target, p)) continue;
                    Impact(target, p.TargetPos, p.Payload, p.Splash);
                    if (p.GrantElement != ElementId.None && !target.Dead) _combat.ApplyElement(target, p.GrantElement, p.GrantStatus, p.Payload);
                }
                else if (p.Splash > 0f) Impact(null, p.TargetPos, p.Payload, p.Splash);
            }
            _w.Projectiles.RemoveAll(p => p.Dead);
        }

        private bool TryIntercept(Actor target, Projectile p)
        {
            foreach (var u in _w.UnitsInRadius(Team.Player, target.Position, 1.8f, _tmp))
            {
                if (u.Def.InterceptChance <= 0f || u == target || u.Status.Stunned) continue;
                if (!_w.Rng.Chance(u.Def.InterceptChance)) continue;
                var d = p.Payload;
                d.Amount *= 0.4f;
                _combat.Deal(u, d);
                _w.Emit(SimEventType.Hit, u.Position, 0, ElementId.None, u.Id, "block");
                return true;
            }
            return false;
        }

        public void TickZones(float dt)
        {
            for (int i = 0; i < _w.Zones.Count; i++)
            {
                var z = _w.Zones[i];
                z.Remaining -= dt;
                z.TickTimer -= dt;
                if (z.TickTimer > 0f) continue;
                z.TickTimer = 0.5f;
                Team victims = z.Team == Team.Player ? Team.Enemy : Team.Player;
                foreach (var u in _w.UnitsInRadius(victims, z.Position, z.Radius, _tmp).ToArray())
                {
                    if (u.IsFlying && (z.Kind == ZoneKind.Lava || z.Kind == ZoneKind.Mud)) continue;
                    switch (z.Kind)
                    {
                        case ZoneKind.Mud: u.Status.AddSlow(z.Status, 0.7f); break;
                        case ZoneKind.Sand: u.Status.AddAttackSlow(z.Status, 0.7f); break;
                        case ZoneKind.Gloom: u.Status.AddVulnerability(0.2f, 0.7f); break;
                        case ZoneKind.Gust: u.Status.AddSlow(0.35f, 0.7f); break;
                    }
                    if (z.Damage > 0f || z.Element != ElementId.None)
                    {
                        var d = DamageInfo.Simple(z.Damage * 0.5f, z.Element, z.Kind == ZoneKind.Mud || z.Kind == ZoneKind.Sand ? 0f : z.Status, z.Source, z.SourceActorId, z.SourceName);
                        d.IsDot = true;
                        d.Generation = 1;
                        _combat.Deal(u, d);
                    }
                }
                if (z.Kind == ZoneKind.Radiance)
                    foreach (var u in _w.UnitsInRadius(z.Team, z.Position, z.Radius, _tmp)) _combat.Heal(u, u.MaxHp * 0.02f);
            }
            _w.Zones.RemoveAll(z => z.Remaining <= 0f);
        }

        public Telegraph AddTelegraph(Vec2 pos, float radius, float delay, string label, ElementId e, Action resolve)
        {
            var t = new Telegraph { Id = _w.AllocId(), Position = pos, Radius = radius, Remaining = delay, Total = delay, Label = label, Element = e, OnResolve = resolve };
            _w.Telegraphs.Add(t);
            _w.Emit(SimEventType.Telegraph, pos, delay, e, -1, label);
            return t;
        }

        public void TickTelegraphs(float dt)
        {
            for (int i = 0; i < _w.Telegraphs.Count; i++)
            {
                var t = _w.Telegraphs[i];
                t.Remaining -= dt;
                if (t.Remaining <= 0f) t.OnResolve?.Invoke();
            }
            _w.Telegraphs.RemoveAll(t => t.Remaining <= 0f);
        }

        /// <summary>Area damage helper used by abilities and bosses (hits units and optionally structures).</summary>
        public void AreaDamage(Team victims, Vec2 p, float r, DamageInfo d, bool structures)
        {
            foreach (var u in _w.UnitsInRadius(victims, p, r, _tmp).ToArray()) _combat.Deal(u, d);
            if (!structures) return;
            foreach (var b in _w.Buildings.ToArray())
            {
                if (b.Dead || b.Ruined || b.Team != victims) continue;
                if (_w.DistanceToActor(p, b) <= r) _combat.Deal(b, d);
            }
        }
    }
}
