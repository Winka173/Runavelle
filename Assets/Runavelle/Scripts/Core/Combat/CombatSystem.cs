using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Per-tick aura buffs on an actor (recomputed by AuraSystem; never persisted).</summary>
    public sealed class AuraBuffs
    {
        public float DamageMult = 1f, ArmorBonus, AttackSpeedMult = 1f, RangeBonus, StatusMult = 1f, Regen, DamageReduction, MoveMult = 1f;
        public ElementId GrantElement;
        public float GrantStatus;
        public float EnemyVuln;
        public float EnemySlow;
        public bool Cleanse;

        public void Reset()
        {
            DamageMult = 1f; ArmorBonus = 0f; AttackSpeedMult = 1f; RangeBonus = 0f; StatusMult = 1f; Regen = 0f; DamageReduction = 0f; MoveMult = 1f;
            GrantElement = ElementId.None; GrantStatus = 0f; EnemyVuln = 0f; EnemySlow = 0f; Cleanse = false;
        }
    }

    /// <summary>
    /// The single damage / status / reaction pipeline shared by towers, soldiers, Generals, AI Generals, enemies and bosses.
    /// Reaction-generated damage has Generation &gt; 0 and can never trigger further reactions (no infinite chains);
    /// a global per-tick budget caps reaction count.
    /// </summary>
    public sealed class CombatSystem
    {
        private readonly World _w;
        public Action<Actor, DamageInfo> OnKilled;
        private readonly List<Unit> _tmp = new List<Unit>(32);
        private readonly List<Unit> _tmp2 = new List<Unit>(32);
        private readonly HashSet<int> _chainHit = new HashSet<int>();

        public CombatSystem(World w) { _w = w; }

        private RunModifiers Mods => _w.Mods;

        public static AuraBuffs BuffsOf(Actor a) => a.Buffs;

        public float EffectiveArmor(Actor t)
        {
            float armor = t.BaseArmor + BuffsOf(t).ArmorBonus;
            if (t.Team == Team.Player) armor = Mods.Apply(armor, Stat.Armor, _w.ScopeOf(t), _w.ClassOf(t));
            float shaken = t.Status.Gauge[(int)ElementId.Earth] / StatusState.MaxGauge;
            armor *= 1f - 0.5f * shaken;
            if (t.Status.ArmorShredTimer > 0f) armor *= 0.5f;
            if (t is Unit u && u.ExhaustedTimer > 0f) armor *= 0.5f;
            return Math.Max(0f, armor);
        }

        /// <summary>Applies one hit. Returns damage dealt after mitigation.</summary>
        public float Deal(Actor target, DamageInfo info)
        {
            if (target == null || target.Dead || (target is Building b0 && b0.Ruined)) return 0f;
            float dmg = info.Amount;
            if (dmg <= 0f && info.Status <= 0f) return 0f;

            // --- multipliers
            dmg *= target.TakenMult(info.Element);
            if (target.IsStructure)
            {
                dmg *= info.StructureMult <= 0f ? 1f : info.StructureMult;
                if (target is Building gb && gb.IsGate) dmg *= info.GateMult <= 0f ? 1f : info.GateMult;
            }
            if (target.IsLarge) dmg *= info.LargeMult <= 0f ? 1f : info.LargeMult;
            if (target.IsFlying) dmg *= info.FlyerMult <= 0f ? 1f : info.FlyerMult;
            if (target.IsBoss && info.Source != SourceKind.Enemy) dmg *= 1f + Mods.Percent(Stat.BossDamage, Scope.Any);
            float vuln = target.Status.VulnAmount + BuffsOf(target).EnemyVuln;
            if (target.Status.Gauge[(int)ElementId.Shadow] >= StatusState.ReactThreshold) vuln += 0.2f;
            if (target.Status.Gauge[(int)ElementId.Light] >= StatusState.ReactThreshold) vuln += 0.1f;
            if (target is Unit ex && ex.ExhaustedTimer > 0f) vuln += 0.5f;
            if (target is Unit bu && bu.Boss != null) dmg *= bu.Boss.Def.Phases[bu.Boss.PhaseIndex].DamageTakenMult;
            dmg *= 1f + vuln;
            dmg *= 1f + Mods.Percent(Stat.DamageTaken, _w.ScopeOf(target), _w.ClassOf(target));
            float dr = BuffsOf(target).DamageReduction + target.Status.GuardAmount;
            if (target == _w.Castle && _w.CastleGuardTimer > 0f) dr += _w.CastleGuardAmount;
            dmg *= 1f - Math.Min(0.8f, dr);

            // --- armor: physical hits only, never below 25%
            if (info.Element == ElementId.None && !info.IsDot)
            {
                float armor = EffectiveArmor(target) * (1f - info.ArmorPierce);
                dmg = Math.Max(dmg * 0.25f, dmg - armor);
            }

            // --- shields absorb
            if (target.Status.ShieldHp > 0f && dmg > 0f)
            {
                float absorbed = Math.Min(target.Status.ShieldHp, dmg);
                target.Status.ShieldHp -= absorbed;
                dmg -= absorbed;
            }
            if (target is Building sb && sb.ShieldHp > 0f && dmg > 0f)
            {
                float absorbed = Math.Min(sb.ShieldHp, dmg);
                sb.ShieldHp -= absorbed;
                dmg -= absorbed;
            }
            // Generals/units with i-frames (dash)
            if (target is Unit iu && iu.IFrames > 0f) dmg = 0f;

            if (dmg > 0f)
            {
                target.Hp -= dmg;
                target.LastHitTime = _w.Time;
                if (info.Source != SourceKind.Enemy) _w.Analytics.AddDamage(info.Source, info.SourceName, dmg);
                if (target == _w.Castle && info.Source == SourceKind.Enemy) _w.Analytics.AddCastleDamage(info.SourceName, dmg);
                if (info.IsReaction) _w.Analytics.AddReactionDamage(_lastReaction, dmg);
                _w.Emit(SimEventType.Hit, target.Position, dmg, info.Element, target.Id, null, info.SourceActorId);
            }

            var attacker = _w.Get(info.SourceActorId);
            // --- reflect (Bastion walls / Fortress castle) vs melee attackers
            if (target is Building rb && rb.Level.ReflectFraction > 0f && attacker is Unit au && au.Team == Team.Enemy && info.Source == SourceKind.Enemy && au.Attack.ProjectileSpeed <= 0f && dmg > 0f)
            {
                float refl = dmg * (rb.Level.ReflectFraction + (_w.Mods.Has(Hook.ThornWalls) ? _w.Mods.HookValue(Hook.ThornWalls) : 0f));
                Deal(au, DamageInfo.Simple(refl, ElementId.None, 0f, SourceKind.Wall, rb.Id, rb.Level.Name));
            }
            // --- rune walls imbue attackers
            if (target is Building iw && iw.IsWallLike && iw.Primary != ElementId.None && attacker is Unit aw && aw.Team == Team.Enemy && !info.IsReaction)
            {
                var wi = DamageInfo.Simple(4f, iw.Primary, 22f, SourceKind.Wall, iw.Id, iw.Level.Name);
                Deal(aw, wi);
            }
            // Radiant targets heal nearby allies when struck by the player's side
            if (target.Team == Team.Enemy && info.Source != SourceKind.Enemy && target.Status.Gauge[(int)ElementId.Light] >= StatusState.ReactThreshold && target.Status.ReactionIcd[31] <= 0f)
            {
                target.Status.ReactionIcd[31] = 0.5f;
                foreach (var ally in _w.UnitsInRadius(Team.Player, target.Position, 2.2f, _tmp2))
                    Heal(ally, ally.MaxHp * 0.015f + 2f);
            }

            if (target.Hp <= 0f)
            {
                Kill(target, info);
                return dmg;
            }

            // --- status application / reactions
            if (info.Element != ElementId.None && info.Status > 0f)
                ApplyElement(target, info.Element, info.Status, info);

            if (target is Unit tu)
            {
                if (info.Knockback > 0f && !tu.IsBoss)
                    Knock(tu, info.Origin, info.Knockback * (tu.IsLarge ? 0.4f : 1f));
                if (info.SlowOnHit > 0f) tu.Status.AddSlow(info.SlowOnHit, 1.5f);
                if (info.StunChance > 0f && _w.Rng.Chance(info.StunChance))
                    tu.Status.StunTimer = Math.Max(tu.Status.StunTimer, tu.Status.CcDuration(0.6f, tu.Def.CcResist));
            }

            // --- chain (lightning etc.)
            if (info.ChainCount > 0 && info.Generation == 0)
            {
                _chainHit.Clear();
                _chainHit.Add(target.Id);
                Actor from = target;
                var chainInfo = info;
                chainInfo.ChainCount = 0;
                chainInfo.Amount *= 0.7f;
                for (int i = 0; i < info.ChainCount; i++)
                {
                    var next = NearestUnit(target.Team, from.Position, info.ChainRange > 0 ? info.ChainRange : 2.5f, _chainHit);
                    if (next == null) break;
                    _chainHit.Add(next.Id);
                    _w.Emit(SimEventType.Reaction, next.Position, 0f, info.Element, next.Id, "chain", from.Id);
                    chainInfo.Origin = from.Position;
                    Deal(next, chainInfo);
                    from = next;
                }
            }
            return dmg;
        }

        public Unit NearestUnit(Team team, Vec2 p, float r, HashSet<int> exclude)
        {
            Unit best = null;
            float bd = float.MaxValue;
            foreach (var u in _w.UnitsInRadius(team, p, r, _tmp))
            {
                if (exclude != null && exclude.Contains(u.Id)) continue;
                float d = Vec2.SqrDistance(u.Position, p);
                if (d < bd || (d == bd && best != null && u.Id < best.Id)) { bd = d; best = u; }
            }
            return best;
        }

        public void Knock(Unit u, Vec2 origin, float dist)
        {
            if (u.Role == UnitRole.General && u.Team == Team.Player) dist *= 0.5f;
            Vec2 dir = (u.Position - origin).Normalized;
            if (dir.SqrMagnitude < 0.01f) dir = new Vec2(1, 0);
            Vec2 target = u.Position + dir * dist;
            if (u.IsFlying || PositionWalkable(u, target)) u.Position = target;
        }

        public bool PositionWalkable(Unit u, Vec2 p)
        {
            var t = Int2.FromPosition(p);
            if (u.IsFlying) return _w.Map.InBounds(t.X, t.Y);
            if (!_w.TerrainPassable(t.X, t.Y)) return false;
            var b = _w.BuildingAt(t.X, t.Y);
            if (b == null || b.Ruined) return true;
            return u.Team == Team.Player && b.IsGate;
        }

        public void Heal(Actor a, float amount)
        {
            if (a.Dead || amount <= 0f || (a is Building bb && bb.Ruined)) return;
            float before = a.Hp;
            a.Hp = Math.Min(a.MaxHp, a.Hp + amount);
            if (a.Hp - before > 4f) _w.Emit(SimEventType.Heal, a.Position, a.Hp - before, ElementId.Light, a.Id);
        }

        // ----------------------------------------------------------------- elements & reactions
        private ReactionId _lastReaction;

        public float StatusPower(Actor source)
        {
            if (source == null || source.Team == Team.Enemy) return 1f;
            float m = BuffsOf(source).StatusMult;
            return m * (1f + Mods.Percent(Stat.StatusPower, _w.ScopeOf(source), _w.ClassOf(source)));
        }

        public void ApplyElement(Actor target, ElementId e, float amount, DamageInfo src)
        {
            if (target.Dead || e == ElementId.None || amount <= 0f) return;
            if (target.IsStructure && target.Team == Team.Player && e != ElementId.Light) amount *= 0.5f;
            var st = target.Status;
            var aura = st.DominantAura();
            if (src.Generation == 0 && aura != ElementId.None && aura != e)
            {
                var r = ReactionTable.Get(aura, e);
                if (r != null && st.ReactionIcd[(int)r.Id % 30] <= 0f && _w.ReactionsThisTick < World.MaxReactionsPerTick)
                {
                    _w.ReactionsThisTick++;
                    st.ReactionIcd[(int)r.Id % 30] = r.Cooldown;
                    st.Gauge[(int)aura] = Math.Max(0f, st.Gauge[(int)aura] - r.Consume);
                    amount = Math.Max(0f, amount - r.Consume);
                    TriggerReaction(r, target, src);
                    if (target.Dead) return;
                }
            }
            st.Gauge[(int)e] = Math.Min(StatusState.MaxGauge, st.Gauge[(int)e] + amount);
            st.AppliedBy[(int)e] = src.SourceActorId;
            st.AppliedKind[(int)e] = src.Source;
            if (e == ElementId.Ice && st.Gauge[(int)ElementId.Ice] >= StatusState.MaxGauge && target is Unit fu)
            {
                float perm = _w.Mods.Has(Hook.Permafrost) ? 1f + _w.Mods.HookValue(Hook.Permafrost) : 1f;
                st.FreezeTimer = st.CcDuration(1.6f * perm, fu.Def.CcResist);
                st.Gauge[(int)ElementId.Ice] = 30f;
                _w.Emit(SimEventType.Freeze, target.Position, st.FreezeTimer, ElementId.Ice, target.Id);
            }
            if (e == ElementId.Wind && target is Unit wu && !wu.IsBoss && src.Generation == 0)
                Knock(wu, src.Origin, 0.25f);
        }

        private float ReactionScale(Actor target, DamageInfo src)
        {
            float m = 1f + Mods.Percent(Stat.ReactionDamage, Scope.Any);
            // Catalyst towers / Arcane Citadel auras near the reaction
            float best = 1f;
            foreach (var b in _w.Buildings)
            {
                if (b.Dead || b.Ruined || b.Level.Aura == null || b.Level.Aura.ReactionMult <= 1f) continue;
                if (Vec2.Distance(b.Center, target.Position) <= b.Level.Aura.Radius) best = Math.Max(best, b.Level.Aura.ReactionMult);
            }
            return m * best;
        }

        private DamageInfo ReactionHit(DamageInfo src, float amount, ElementId e)
        {
            var d = DamageInfo.Simple(amount, e, 0f, src.Source, src.SourceActorId, src.SourceName);
            d.IsReaction = true;
            d.Generation = src.Generation + 1;
            d.Origin = src.Origin;
            return d;
        }

        private void Aoe(Team victims, Vec2 p, float r, DamageInfo d, Action<Unit> extra = null)
        {
            _tmp2.Clear();
            _w.UnitsInRadius(victims, p, r, _tmp2);
            var copy = _tmp2.ToArray();
            foreach (var u in copy)
            {
                Deal(u, d);
                if (!u.Dead) extra?.Invoke(u);
            }
        }

        private void TriggerReaction(ReactionDefinition r, Actor t, DamageInfo src)
        {
            _lastReaction = r.Id;
            Vec2 p = t.Position;
            Team victims = t.Team;
            float a = Math.Max(4f, src.Amount);
            float k = ReactionScale(t, src);
            _w.Analytics.AddReaction(r.Id, 0f);
            _w.Analytics.MaxReactionsInTick = Math.Max(_w.Analytics.MaxReactionsInTick, _w.ReactionsThisTick);
            _w.Emit(SimEventType.Reaction, p, 0f, src.Element, t.Id, r.Name, src.SourceActorId, r.Id);
            var tu = t as Unit;
            switch (r.Id)
            {
                case ReactionId.SteamBurst:
                    Aoe(victims, p, 1.6f, ReactionHit(src, (18 + 0.8f * a) * k, ElementId.Water), u => u.Status.AddAttackSlow(0.3f, 3f));
                    break;
                case ReactionId.Overload:
                    var ov = ReactionHit(src, (30 + 1.2f * a) * k, ElementId.Fire);
                    ov.Knockback = 0.8f; ov.Origin = p;
                    Aoe(victims, p, 2.0f, ov);
                    break;
                case ReactionId.Melt:
                    t.Status.Gauge[(int)ElementId.Ice] = 0f;
                    Deal(t, ReactionHit(src, (15 + 2f * a) * k, ElementId.Fire));
                    break;
                case ReactionId.Firestorm:
                {
                    float rad = 2.5f * (1f + (Mods.Has(Hook.Wildfire) ? Mods.HookValue(Hook.Wildfire) : 0f));
                    var fs = ReactionHit(src, 8f * k, ElementId.Fire);
                    Aoe(victims, p, rad, fs, u => { u.Status.Gauge[(int)ElementId.Fire] = Math.Min(100f, u.Status.Gauge[(int)ElementId.Fire] + 35f); u.Status.AppliedBy[(int)ElementId.Fire] = src.SourceActorId; u.Status.AppliedKind[(int)ElementId.Fire] = src.Source; });
                    break;
                }
                case ReactionId.Magma:
                {
                    float mult = Mods.Has(Hook.MagmaBastion) ? Mods.HookValue(Hook.MagmaBastion) : 1f;
                    SpawnZone(ZoneKind.Lava, victims == Team.Enemy ? Team.Player : Team.Enemy, p, 1.5f, 3f * mult, 14f * k * mult, ElementId.Fire, 8f, src);
                    break;
                }
                case ReactionId.Electrocharge:
                {
                    int jumps = 4 + (Mods.Has(Hook.ConductiveRain) ? (int)Mods.HookValue(Hook.ConductiveRain) : 0);
                    var ec = ReactionHit(src, (16 + 0.6f * a) * k, ElementId.Lightning);
                    Deal(t, ec);
                    _tmp2.Clear();
                    _w.UnitsInRadius(victims, p, 3.5f, _tmp2);
                    _tmp2.Sort((x, y) => Vec2.SqrDistance(x.Position, p).CompareTo(Vec2.SqrDistance(y.Position, p)));
                    int n = 0;
                    foreach (var u in _tmp2.ToArray())
                    {
                        if (n >= jumps) break;
                        if (u == t || u.Status.Gauge[(int)ElementId.Water] < StatusState.ReactThreshold) continue;
                        u.Status.Gauge[(int)ElementId.Water] -= 20f;
                        _w.Emit(SimEventType.Reaction, u.Position, 0f, ElementId.Lightning, u.Id, "arc", t.Id, r.Id);
                        Deal(u, ec);
                        n++;
                    }
                    break;
                }
                case ReactionId.FlashFreeze:
                    if (tu != null)
                    {
                        float perm = Mods.Has(Hook.Permafrost) ? 1f + Mods.HookValue(Hook.Permafrost) : 1f;
                        t.Status.FreezeTimer = Math.Max(t.Status.FreezeTimer, t.Status.CcDuration(2.0f * perm, tu.Def.CcResist));
                        _w.Emit(SimEventType.Freeze, p, t.Status.FreezeTimer, ElementId.Ice, t.Id);
                    }
                    break;
                case ReactionId.Rainfall:
                    SpawnZone(ZoneKind.Rain, OtherTeam(victims), p, 2.5f, 4f, 0f, ElementId.Water, 14f, src);
                    break;
                case ReactionId.Mire:
                    SpawnZone(ZoneKind.Mud, OtherTeam(victims), p, 2.0f, 3.5f, 0f, ElementId.None, 0.5f, src);
                    break;
                case ReactionId.Superconduct:
                    Aoe(victims, p, 1.8f, ReactionHit(src, (10 + 0.5f * a) * k, ElementId.Ice), u => u.Status.ArmorShredTimer = 5f);
                    t.Status.ArmorShredTimer = 5f;
                    break;
                case ReactionId.StaticStorm:
                {
                    var ss = ReactionHit(src, (18 + 0.7f * a) * k, ElementId.Lightning);
                    _tmp2.Clear();
                    _w.UnitsInRadius(victims, p, 3f, _tmp2);
                    _tmp2.Sort((x, y) => x.Id.CompareTo(y.Id));
                    int n = 0;
                    foreach (var u in _tmp2.ToArray())
                    {
                        if (n >= 3 || u == t) continue;
                        _w.Emit(SimEventType.Reaction, u.Position, 0f, ElementId.Lightning, u.Id, "arc", t.Id, r.Id);
                        Deal(u, ss);
                        n++;
                    }
                    break;
                }
                case ReactionId.Magnetize:
                    Aoe(victims, p, 2.5f, ReactionHit(src, (8 + 0.4f * a) * k, ElementId.Lightning), u =>
                    {
                        if (u.IsBoss) return;
                        Vec2 dir = p - u.Position;
                        if (dir.Magnitude > 0.4f && PositionWalkable(u, u.Position + dir.Normalized * Math.Min(1f, dir.Magnitude - 0.3f)))
                            u.Position += dir.Normalized * Math.Min(1f, dir.Magnitude - 0.3f);
                        u.Status.StunTimer = Math.Max(u.Status.StunTimer, u.Status.CcDuration(0.6f, u.Def.CcResist));
                    });
                    break;
                case ReactionId.Blizzard:
                    SpawnZone(ZoneKind.Blizzard, OtherTeam(victims), p, 2.2f, 4f, 0f, ElementId.Ice, 14f, src);
                    break;
                case ReactionId.IceSpikes:
                {
                    var spikes = ReactionHit(src, (20 + 0.9f * a) * k, ElementId.Earth);
                    _tmp2.Clear();
                    _w.UnitsInRadius(victims, p, 1.5f, _tmp2);
                    foreach (var u in _tmp2.ToArray())
                    {
                        var d = spikes;
                        if (u.Status.Frozen) d.Amount *= 2f;
                        Deal(u, d);
                        if (!u.Dead) u.Status.RootTimer = Math.Max(u.Status.RootTimer, u.Status.CcDuration(1.0f, u.Def.CcResist));
                    }
                    break;
                }
                case ReactionId.Sandstorm:
                    SpawnZone(ZoneKind.Sand, OtherTeam(victims), p, 2.2f, 4f, 6f * k, ElementId.None, 0.4f, src);
                    break;
                case ReactionId.Eclipse:
                    Aoe(victims, p, 1.8f, ReactionHit(src, (12 + 0.5f * a) * k, ElementId.Shadow), u => u.Status.AddVulnerability(0.4f, 5f));
                    t.Status.AddVulnerability(0.4f, 5f);
                    break;
                case ReactionId.Fortify:
                {
                    Team allies = OtherTeam(victims);
                    foreach (var b in _w.Buildings)
                    {
                        if (b.Dead || b.Ruined || b.Team != allies) continue;
                        if (Vec2.Distance(b.Center, p) > 3.2f) continue;
                        b.ShieldHp = Math.Min(b.MaxHp * 0.25f, b.ShieldHp + b.MaxHp * 0.12f);
                    }
                    _tmp2.Clear();
                    foreach (var u in _w.UnitsInRadius(allies, p, 3.2f, _tmp2))
                        u.Status.ShieldHp = Math.Min(u.MaxHp * 0.25f, u.Status.ShieldHp + u.MaxHp * 0.12f);
                    Deal(t, ReactionHit(src, (8 + 0.4f * a) * k, ElementId.Earth));
                    break;
                }
                case ReactionId.HauntedArc:
                {
                    var ha = ReactionHit(src, (22 + 0.6f * a) * k, ElementId.Lightning);
                    _tmp2.Clear();
                    _w.UnitsInRadius(victims, p, 5f, _tmp2);
                    int n = 0;
                    foreach (var u in _tmp2.ToArray())
                    {
                        if (n >= 8) break;
                        if (u != t && u.Status.Gauge[(int)ElementId.Shadow] < StatusState.ReactThreshold) continue;
                        _w.Emit(SimEventType.Reaction, u.Position, 0f, ElementId.Shadow, u.Id, "arc", t.Id, r.Id);
                        Deal(u, ha);
                        n++;
                    }
                    break;
                }
                case ReactionId.SolarFlare:
                    Aoe(victims, p, 1.8f, ReactionHit(src, (20 + 0.8f * a) * k, ElementId.Light), u => u.Status.BlindTimer = Math.Max(u.Status.BlindTimer, 2f));
                    break;
                case ReactionId.SoulFrost:
                    if (tu != null && !tu.IsElite && t.HpFraction < 0.18f)
                    {
                        var ex = ReactionHit(src, t.Hp + 1f, ElementId.Shadow);
                        ex.Amount = t.Hp / Math.Max(0.01f, t.TakenMult(ElementId.Shadow)) * 3f + 10f;
                        Deal(t, ex);
                    }
                    else Deal(t, ReactionHit(src, (40 + 1.5f * a) * k, ElementId.Shadow));
                    break;
                case ReactionId.Purify:
                {
                    Team allies = OtherTeam(victims);
                    foreach (var b in _w.Buildings)
                        if (!b.Dead && !b.Ruined && b.Team == allies && Vec2.Distance(b.Center, p) <= 3.2f) Heal(b, b.MaxHp * 0.08f);
                    _tmp2.Clear();
                    foreach (var u in _w.UnitsInRadius(allies, p, 3.2f, _tmp2).ToArray())
                    {
                        Heal(u, u.MaxHp * 0.08f);
                        u.Status.Cleanse();
                    }
                    Deal(t, ReactionHit(src, (10 + 0.4f * a) * k, ElementId.Light));
                    break;
                }
                default: // Resonance
                    Deal(t, ReactionHit(src, (6 + 0.3f * a) * k, src.Element));
                    break;
            }
            if (Mods.Has(Hook.GoldOnReaction) && _w.Rng.Chance(0.1f)) _w.PendingGold += (int)Mods.HookValue(Hook.GoldOnReaction);
        }

        private static Team OtherTeam(Team t) => t == Team.Player ? Team.Enemy : Team.Player;

        public Zone SpawnZone(ZoneKind kind, Team owner, Vec2 p, float radius, float duration, float dps, ElementId e, float status, DamageInfo src)
        {
            var z = new Zone { Id = _w.AllocId(), Kind = kind, Team = owner, Position = p, Radius = radius, Remaining = duration, Duration = duration, Damage = dps,
                Element = e, Status = status, Source = src.Source, SourceActorId = src.SourceActorId, SourceName = src.SourceName };
            _w.Zones.Add(z);
            return z;
        }

        // ----------------------------------------------------------------- death
        public void Kill(Actor t, DamageInfo info)
        {
            if (t.Dead) return;
            t.Hp = 0f;
            if (t is Building b)
            {
                if (b.Def.Family == BuildingFamily.Castle)
                {
                    t.Dead = true;
                    _w.Emit(SimEventType.BuildingDestroyed, b.Center, 0, ElementId.None, b.Id, "castle");
                    OnKilled?.Invoke(t, info);
                    return;
                }
                b.Ruined = true;
                b.Status.Clear();
                _w.TopologyVersion++;
                _w.Analytics.NightBuildingsLost++;
                if (b.IsWallLike)
                {
                    _w.Analytics.Breaches.Add(new BreachRecord { Night = _w.Night, X = b.Center.X, Y = b.Center.Y, LevelId = b.Level.Id, Cause = info.SourceName });
                    _w.Emit(SimEventType.WallBreached, b.Center, 0, ElementId.None, b.Id, b.Level.Name);
                }
                else _w.Emit(SimEventType.BuildingDestroyed, b.Center, 0, ElementId.None, b.Id, b.Level.Name);
                OnKilled?.Invoke(t, info);
                return;
            }
            var u = (Unit)t;
            if (u.Role == UnitRole.General)
            {
                u.State = UnitState.Downed;
                u.DownedTimer = 12f;
                u.Status.Clear();
                _w.Emit(SimEventType.GeneralDowned, u.Position, 12f, ElementId.None, u.Id, u.Def.Name);
                OnKilled?.Invoke(t, info);
                return;
            }
            u.Dead = true;
            if (u.Team == Team.Enemy)
            {
                _w.Analytics.AddKill(u.Def.Id);
                if (!u.Summoned) _w.PendingGold += (int)Math.Round(_w.Mods.Apply(u.Def.Bounty, Stat.Bounty, Scope.Any));
                if (u.Status.Gauge[(int)ElementId.Shadow] >= StatusState.ReactThreshold) _w.PendingMana += 1;
                _w.DeadEnemies.Add(u);
            }
            else
            {
                _w.Analytics.SoldiersLost++;
                _w.Analytics.NightSoldiersLost++;
            }
            _w.Emit(SimEventType.Kill, u.Position, 0, info.Element, u.Id, u.Def.Id, info.SourceActorId);
            OnKilled?.Invoke(t, info);
        }
    }
}
