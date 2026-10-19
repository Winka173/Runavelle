using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>
    /// Recomputes aura buffs (shrines, resonance wells, General leadership, enemy buffers) at a bounded rate (4 Hz),
    /// and ticks status decay, burning DoT and regeneration every tick.
    /// </summary>
    public sealed class AuraSystem
    {
        private readonly World _w;
        private readonly CombatSystem _combat;
        private readonly Spawner _spawner;
        private float _auraTimer;
        private float _dotTimer;
        private readonly List<Unit> _tmp = new List<Unit>(64);

        public AuraSystem(World w, CombatSystem combat, Spawner spawner) { _w = w; _combat = combat; _spawner = spawner; }

        public void Tick(float dt)
        {
            _auraTimer -= dt;
            if (_auraTimer <= 0f)
            {
                _auraTimer = 0.25f;
                RecomputeAuras();
            }
            float waterDecay = _w.Mods.Has(Hook.ConductiveRain) ? 0.5f : 1f;
            for (int i = 0; i < _w.Units.Count; i++)
            {
                var u = _w.Units[i];
                if (u.Dead) continue;
                u.Status.Tick(dt, waterDecay);
                u.IFrames = Math.Max(0f, u.IFrames - dt);
                u.ExhaustedTimer = Math.Max(0f, u.ExhaustedTimer - dt);
                if (u.Team == Team.Player && u.State != UnitState.Downed)
                {
                    float regen = u.Buffs.Regen;
                    if (u.State == UnitState.Retreating && _w.Mods.Has(Hook.TacticalRetreat)) regen += _w.Mods.HookValue(Hook.TacticalRetreat);
                    if (regen > 0f && u.Hp < u.MaxHp) u.Hp = Math.Min(u.MaxHp, u.Hp + u.MaxHp * regen * dt);
                }
                if (u.Team == Team.Enemy && u.Def.HealPerSecond > 0f) HealAllies(u, dt);
            }
            for (int i = 0; i < _w.Buildings.Count; i++)
            {
                var b = _w.Buildings[i];
                if (b.Dead || b.Ruined) continue;
                b.Status.Tick(dt, waterDecay);
                b.DisabledTimer = Math.Max(0f, b.DisabledTimer - dt);
                if (!_w.IsNight || b.Hp >= b.MaxHp) continue;
                float regen = b.Buffs.Regen;
                bool idle = _w.Time - b.LastHitTime > 4f;
                if (idle && b.Level.RegenFraction > 0f) regen += b.Level.RegenFraction;
                if (b.IsWallLike && _w.Time - b.LastHitTime > 3f && _w.Mods.Has(Hook.LivingRamparts)) regen += _w.Mods.HookValue(Hook.LivingRamparts);
                if (regen > 0f) b.Hp = Math.Min(b.MaxHp, b.Hp + b.MaxHp * regen * dt);
            }
            _dotTimer -= dt;
            if (_dotTimer <= 0f)
            {
                _dotTimer = 0.5f;
                ApplyBurns(0.5f);
            }
        }

        private void HealAllies(Unit healer, float dt)
        {
            if (healer.Status.Stunned) return;
            foreach (var u in _w.UnitsInRadius(Team.Enemy, healer.Position, 3.5f, _tmp))
                if (u != healer && u.Hp < u.MaxHp) u.Hp = Math.Min(u.MaxHp, u.Hp + healer.Def.HealPerSecond * dt);
        }

        private void ApplyBurns(float interval)
        {
            float wild = _w.Mods.Has(Hook.Wildfire) ? 1f + _w.Mods.HookValue(Hook.Wildfire) : 1f;
            int fire = (int)ElementId.Fire;
            for (int i = 0; i < _w.Units.Count; i++)
            {
                var u = _w.Units[i];
                if (u.Dead || u.Status.Gauge[fire] <= 0f) continue;
                float dps = u.Status.Gauge[fire] / StatusState.MaxGauge * 14f * (u.Team == Team.Enemy ? wild : 0.6f);
                var src = u.Status.AppliedKind[fire];
                var d = DamageInfo.Simple(dps * interval, ElementId.Fire, 0f, src == SourceKind.None ? SourceKind.Zone : src, u.Status.AppliedBy[fire], "Bỏng");
                d.IsDot = true;
                d.Generation = 1;
                _combat.Deal(u, d);
            }
        }

        private void RecomputeAuras()
        {
            foreach (var u in _w.Units) u.Buffs.Reset();
            foreach (var b in _w.Buildings) b.Buffs.Reset();

            // Structure auras
            foreach (var src in _w.Buildings)
            {
                if (src.Dead || src.Ruined || src.Level.Aura == null || src.Level.Aura.Radius <= 0f) continue;
                var a = src.Level.Aura;
                var c = src.Center;
                if (a.AffectsSoldiers && !a.OnlyMagicTowers)
                    foreach (var u in _w.UnitsInRadius(Team.Player, c, a.Radius, _tmp)) ApplyStructureAura(u.Buffs, a);
                if (a.AffectsStructures || a.OnlyMagicTowers)
                    foreach (var b in _w.Buildings)
                    {
                        if (b.Dead || b.Ruined || b == src) continue;
                        if (a.OnlyMagicTowers && b.Def.Family != BuildingFamily.MagicTower) continue;
                        if (Vec2.Distance(b.Center, c) <= a.Radius + b.Size * 0.5f) ApplyStructureAura(b.Buffs, a);
                    }
                if (a.Cleanse && _w.IsNight)
                    foreach (var u in _w.UnitsInRadius(Team.Player, c, a.Radius, _tmp)) if (_w.Rng.Chance(0.12f)) u.Status.Cleanse();
            }

            // General auras (player + AI generals)
            ApplyGeneralAura(_w.PlayerGeneral);
            foreach (var g in _w.AIGenerals) ApplyGeneralAura(g);

            // Enemy buffer / elite auras
            foreach (var e in _w.Units)
            {
                if (e.Dead || e.Team != Team.Enemy || e.Def.AuraRadius <= 0f || e.Status.Stunned) continue;
                foreach (var u in _w.UnitsInRadius(Team.Enemy, e.Position, e.Def.AuraRadius, _tmp))
                {
                    if (u == e) continue;
                    u.Buffs.MoveMult = Math.Max(u.Buffs.MoveMult, e.Def.AuraSpeedMult);
                    u.Buffs.ArmorBonus = Math.Max(u.Buffs.ArmorBonus, e.Def.AuraArmorBonus);
                }
            }
        }

        private static void ApplyStructureAura(AuraBuffs b, StructureAura a)
        {
            // Auras of the same kind do not stack: strongest wins.
            b.DamageMult = Math.Max(b.DamageMult, a.DamageMult);
            b.ArmorBonus = Math.Max(b.ArmorBonus, a.ArmorBonus);
            b.Regen = Math.Max(b.Regen, a.RegenFraction);
            b.StatusMult = Math.Max(b.StatusMult, a.StatusMult);
            b.AttackSpeedMult = Math.Max(b.AttackSpeedMult, a.AttackSpeedMult);
            b.DamageReduction = Math.Max(b.DamageReduction, a.DamageReduction);
        }

        private void ApplyGeneralAura(Unit g)
        {
            if (g == null || g.Dead || g.State == UnitState.Downed) return;
            var aura = _spawner.EffectiveAura(g);
            if (aura == null) return;
            var scope = g.IsPlayerControlled ? Scope.PlayerGeneral : Scope.AIGenerals;
            float power = (1f + _w.Mods.Percent(Stat.AuraPower, scope)) * (g.Evolution?.AuraMult ?? 1f) * (g.IsPlayerControlled ? 1f : 0.8f);
            if (_w.Mods.Has(Hook.BattleHymn) && _w.Castle != null && _w.Castle.HpFraction < 0.5f) power *= 2f;
            float radius = aura.Radius * (1f + 0.25f * (power - 1f));
            float Scale(float mult) => 1f + (mult - 1f) * power;
            foreach (var u in _w.UnitsInRadius(Team.Player, g.Position, radius, _tmp))
            {
                if (u == g || u.Role == UnitRole.General) continue;
                if (aura.OnlyClass != SoldierClass.None && u.Def.Class != aura.OnlyClass && u.Def.Class != aura.OnlyClass2) continue;
                var b = u.Buffs;
                b.DamageMult = Math.Max(b.DamageMult, Scale(aura.DamageMult));
                b.ArmorBonus = Math.Max(b.ArmorBonus, aura.ArmorBonus * power);
                b.AttackSpeedMult = Math.Max(b.AttackSpeedMult, Scale(aura.AttackSpeedMult));
                b.RangeBonus = Math.Max(b.RangeBonus, aura.RangeBonus * power);
                b.StatusMult = Math.Max(b.StatusMult, Scale(aura.StatusMult));
                b.Regen = Math.Max(b.Regen, aura.RegenFraction * power);
                b.MoveMult = Math.Max(b.MoveMult, Scale(aura.MoveSpeedMult));
                if (aura.GrantElement != ElementId.None) { b.GrantElement = aura.GrantElement; b.GrantStatus = aura.GrantStatus * power; }
            }
            if (aura.AffectsTowers || aura.AffectsWalls)
                foreach (var bld in _w.Buildings)
                {
                    if (bld.Dead || bld.Ruined) continue;
                    bool tower = bld.Def.Family == BuildingFamily.ArcherTower || bld.Def.Family == BuildingFamily.MagicTower;
                    if (!(tower && aura.AffectsTowers) && !(bld.IsWallLike && aura.AffectsWalls) && !(aura.AffectsTowers && aura.RegenFraction > 0f)) continue;
                    if (Vec2.Distance(bld.Center, g.Position) > radius + bld.Size * 0.5f) continue;
                    var b = bld.Buffs;
                    b.AttackSpeedMult = Math.Max(b.AttackSpeedMult, Scale(aura.AttackSpeedMult));
                    b.RangeBonus = Math.Max(b.RangeBonus, aura.RangeBonus * power);
                    b.StatusMult = Math.Max(b.StatusMult, Scale(aura.StatusMult));
                    b.Regen = Math.Max(b.Regen, aura.RegenFraction * power);
                    b.DamageMult = Math.Max(b.DamageMult, Scale(aura.DamageMult));
                    b.ArmorBonus = Math.Max(b.ArmorBonus, aura.ArmorBonus * power);
                }
            if (aura.EnemyVulnerability > 0f || aura.EnemySlow > 0f)
                foreach (var e in _w.UnitsInRadius(Team.Enemy, g.Position, radius, _tmp))
                {
                    e.Buffs.EnemyVuln = Math.Max(e.Buffs.EnemyVuln, aura.EnemyVulnerability * power);
                    if (aura.EnemySlow > 0f) e.Status.AddSlow(Math.Min(0.6f, aura.EnemySlow * power), 0.4f);
                }
        }
    }
}
