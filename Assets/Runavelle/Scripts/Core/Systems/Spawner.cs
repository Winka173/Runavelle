using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Creates actors and computes their effective (modified) stats from content + run modifiers.</summary>
    public sealed class Spawner
    {
        private readonly World _w;
        public Spawner(World w) { _w = w; }

        private RunModifiers Mods => _w.Mods;

        // ------------------------------------------------------------ buildings
        public Building CreateBuilding(string levelId, Int2 origin)
        {
            var level = _w.Content.Level(levelId);
            var def = _w.Content.BuildingOfLevel(levelId);
            var b = new Building
            {
                Id = _w.AllocId(), Team = Team.Player, Def = def, Level = level, Origin = origin, Size = def.Size,
                Position = new Vec2(origin.X + def.Size * 0.5f, origin.Y + def.Size * 0.5f), DayBuilt = _w.Night, TickBuilt = _w.Tick,
                Radius = def.Size * 0.5f, Invested = level.Cost,
            };
            b.PrevPosition = b.Position;
            b.Primary = level.Element;
            ApplyBuildingStats(b, true);
            _w.Buildings.Add(b);
            _w.ById[b.Id] = b;
            Occupy(b, true);
            if (def.Family == BuildingFamily.Castle) _w.Castle = b;
            return b;
        }

        public void Occupy(Building b, bool on)
        {
            for (int dx = 0; dx < b.Size; dx++)
                for (int dy = 0; dy < b.Size; dy++)
                {
                    int x = b.Origin.X + dx, y = b.Origin.Y + dy;
                    if (_w.Map.InBounds(x, y)) _w.Occupancy[y * _w.Map.Width + x] = on ? b.Id : -1;
                }
            _w.TopologyVersion++;
        }

        public void RemoveBuilding(Building b)
        {
            Occupy(b, false);
            b.Dead = true;
            foreach (var sid in b.Squad)
                if (_w.Get(sid) is Unit s) s.Dead = true;
            b.Squad.Clear();
            _w.Buildings.Remove(b);
            _w.ById.Remove(b.Id);
        }

        /// <summary>Recomputes max HP/armor after level change or modifier change, keeping the HP fraction.</summary>
        public void ApplyBuildingStats(Building b, bool fullHeal)
        {
            float frac = b.MaxHp > 0 ? b.Hp / b.MaxHp : 1f;
            var scope = World.ScopeOf(b);
            b.MaxHp = Mods.Apply(b.Level.MaxHp, Stat.MaxHp, scope);
            b.BaseArmor = b.Level.Armor;
            b.Hp = fullHeal ? b.MaxHp : Math.Max(1f, b.MaxHp * frac);
        }

        public AttackSpec BuildingAttack(Building b, bool secondaryShot)
        {
            if (b.Level.Attack == null) return null;
            var a = b.Level.Attack.Clone();
            var scope = World.ScopeOf(b);
            a.Element = b.AttackElement(secondaryShot);
            if (a.Element != ElementId.None && a.StatusAmount <= 0f) a.StatusAmount = 22f;
            a.Damage = Mods.Apply(a.Damage, Stat.Damage, scope, SoldierClass.None, a.Element) * b.Buffs.DamageMult;
            a.Range = Mods.Apply(a.Range, Stat.Range, scope) + b.Buffs.RangeBonus + (OnHighGround(b) ? 1f : 0f);
            float aspd = (1f + Mods.Percent(Stat.AttackSpeed, scope)) * b.Buffs.AttackSpeedMult;
            if (_w.Mods.Has(Hook.LastWatch) && (scope == Scope.ArcherTowers || scope == Scope.MagicTowers) && DamagedWallNear(b.Center, 5f))
                aspd *= 1f + _w.Mods.HookValue(Hook.LastWatch);
            a.Interval /= Math.Max(0.2f, aspd);
            return a;
        }

        /// <summary>Mist hazard: ground enemies arrive hidden and are revealed this close to defenders (+1 for towers).</summary>
        public const float MistRevealRange = 3.2f;

        /// <summary>Ascension 3+: elites and bosses roll one affix.</summary>
        public bool EliteAffixes;
        public static readonly string[] AffixNames = { "Thần Tốc", "Thành Lũy", "Cuồng Nộ", "Hộ Thuẫn" };

        private void ApplyAffix(Unit u)
        {
            int k = _w.Rng.Range(0, AffixNames.Length);
            u.Affix = AffixNames[k];
            switch (k)
            {
                case 0: u.Speed *= 1.3f; break;
                case 1: u.MaxHp *= 1.4f; u.Hp = u.MaxHp; break;
                case 2: u.Attack.Interval /= 1.3f; break;
                default: u.Status.ShieldHp = u.MaxHp * 0.3f; break;
            }
        }

        public bool OnHighGround(Building b) => _w.Map.Hazard == MapHazard.HighGround && _w.Map.Get(b.Origin.X, b.Origin.Y) == TileType.Plateau;

        private bool DamagedWallNear(Vec2 p, float r)
        {
            foreach (var w in _w.Buildings)
                if (w.IsWallLike && (w.Ruined || w.Hp < w.MaxHp * 0.98f) && Vec2.Distance(w.Center, p) <= r) return true;
            return false;
        }

        public int SquadSize(Building b)
        {
            return Math.Max(1, (int)Math.Round(Mods.Apply(b.Level.SquadSize, Stat.SquadSize, Scope.Barracks, b.Level.Soldier)));
        }

        // ------------------------------------------------------------ units
        private Unit NewUnit(UnitDefinition def, Team team, Vec2 pos, ActorKind kind)
        {
            var u = new Unit
            {
                Id = _w.AllocId(), Def = def, Team = team, Role = def.Role, kind = kind, Position = pos, PrevPosition = pos, Radius = def.Radius,
                MaxHp = def.MaxHp, Hp = def.MaxHp, BaseArmor = def.Armor, Attack = def.Attack.Clone(), Speed = def.Speed, Home = pos, SpawnTime = _w.Time,
                LastProgressPos = pos,
            };
            _w.Units.Add(u);
            _w.ById[u.Id] = u;
            return u;
        }

        /// <summary>
        /// Soldiers appear on a free tile just outside their Barracks (preferring the side facing away from the
        /// Castle). Spawning at the building centre left squads stuck inside the footprint.
        /// </summary>
        public Vec2 SoldierSpawnPoint(Building b)
        {
            Vec2 outward = _w.Castle != null ? (b.Center - _w.Castle.Center).Normalized : new Vec2(1, 0);
            Vec2 best = b.Center;
            float bestScore = float.MaxValue;
            for (int ring = 1; ring <= 3; ring++)
            {
                for (int dx = -ring; dx < b.Size + ring; dx++)
                    for (int dy = -ring; dy < b.Size + ring; dy++)
                    {
                        if (dx > -ring && dx < b.Size + ring - 1 && dy > -ring && dy < b.Size + ring - 1) continue; // perimeter only
                        int x = b.Origin.X + dx, y = b.Origin.Y + dy;
                        if (!_w.FriendlyPassable(x, y)) continue;
                        var c = new Int2(x, y).Center;
                        float score = Vec2.Distance(c, b.Center + outward * (b.Size * 0.5f + 1f));
                        if (score < bestScore) { bestScore = score; best = c; }
                    }
                if (bestScore < float.MaxValue) break;
            }
            return best;
        }

        public Unit SpawnSoldier(Building barracks, Vec2 pos)
        {
            var pt = Int2.FromPosition(pos);
            if (!_w.FriendlyPassable(pt.X, pt.Y)) pos = SoldierSpawnPoint(barracks);
            var def = _w.Content.Unit(_w.Content.SoldierUnitId(barracks.Level.Soldier));
            var u = NewUnit(def, Team.Player, pos, ActorKind.Soldier);
            u.OwnerBuildingId = barracks.Id;
            u.Home = barracks.Center;
            u.Leash = def.Class == SoldierClass.Cavalry || def.Class == SoldierClass.GriffinRider ? 7.5f : 5.5f;
            ApplySoldierStats(u, barracks, true);
            barracks.Squad.Add(u.Id);
            _w.Emit(SimEventType.Spawned, pos, 0, ElementId.None, u.Id);
            return u;
        }

        public void ApplySoldierStats(Unit u, Building barracks, bool fullHeal)
        {
            var lvl = barracks.Level;
            var br = _w.Content.Branch(lvl.SoldierBranchId);
            u.Branch = br;
            var def = u.Def;
            var cls = def.Class;
            float frac = u.MaxHp > 0 ? u.Hp / u.MaxHp : 1f;
            float hp = def.MaxHp * lvl.SoldierHpMult * (br?.HpMult ?? 1f);
            u.MaxHp = Mods.Apply(hp, Stat.MaxHp, Scope.Soldiers, cls);
            u.BaseArmor = def.Armor + lvl.SoldierArmorBonus + (br?.ArmorBonus ?? 0f);
            u.Speed = Mods.Apply(def.Speed * (br?.SpeedMult ?? 1f), Stat.MoveSpeed, Scope.Soldiers, cls);
            var a = def.Attack.Clone();
            a.Damage *= lvl.SoldierDmgMult * (br?.DmgMult ?? 1f);
            if (br != null)
            {
                if (br.Element != ElementId.None) { a.Element = br.Element; a.StatusAmount = Math.Max(a.StatusAmount, 0f) + br.StatusBonus; }
                a.Range += br.RangeBonus;
                a.Interval *= br.IntervalMult;
                if (a.Splash > 0f || br.SplashBonus > 0f) a.Splash += br.SplashBonus;
                a.ChainCount += br.ChainBonus;
                a.SlowOnHit = Math.Max(a.SlowOnHit, br.SlowOnHit);
                a.StunChance = Math.Max(a.StunChance, br.StunChance);
                a.ArmorPierce = Math.Max(a.ArmorPierce, br.ArmorPierce);
                a.LargeMult *= br.LargeMult;
                a.FlyerMult *= br.FlyerMult;
            }
            if (_w.Mods.Has(Hook.StormLegion) && a.Element == ElementId.Lightning) a.ChainCount += (int)_w.Mods.HookValue(Hook.StormLegion);
            if (_w.Mods.Has(Hook.VeteranCore) && u.Veterancy > 0)
            {
                float vet = 1f + _w.Mods.HookValue(Hook.VeteranCore) * u.Veterancy;
                a.Damage *= vet;
                u.MaxHp *= vet;
            }
            if (_w.Mods.Has(Hook.StormArrows) && cls == SoldierClass.Archer && a.Element == ElementId.None)
            {
                a.Element = ElementId.Lightning; a.StatusAmount = _w.Mods.HookValue(Hook.StormArrows);
            }
            u.Attack = a;
            if (def.Secondary != null)
            {
                u.SecondaryAttack = def.Secondary.Clone();
                if (br != null && br.Element != ElementId.None) u.SecondaryAttack.Element = br.Element;
            }
            u.Hp = fullHeal ? u.MaxHp : Math.Max(1f, u.MaxHp * frac);
        }

        public Unit SpawnEnemy(UnitDefinition def, Vec2 pos, string lane, float hpMult)
        {
            var u = NewUnit(def, Team.Enemy, pos, ActorKind.Enemy);
            u.LaneId = lane;
            u.MaxHp = Mods.Apply(def.MaxHp * hpMult, Stat.MaxHp, def.Behavior == EnemyBehavior.Boss ? Scope.Bosses : Scope.Enemies);
            u.Hp = u.MaxHp;
            u.Attack.Damage = Mods.Apply(u.Attack.Damage, Stat.Damage, def.Behavior == EnemyBehavior.Boss ? Scope.Bosses : Scope.Enemies);
            u.Speed = Mods.Apply(u.Speed, Stat.MoveSpeed, Scope.Enemies);
            if (def.Secondary != null) u.SecondaryAttack = def.Secondary.Clone();
            u.BaseArmor = Mods.Apply(def.Armor, Stat.Armor, def.Behavior == EnemyBehavior.Boss ? Scope.Bosses : Scope.Enemies);
            if (EliteAffixes && u.IsElite) ApplyAffix(u);
            u.SummonTimer = def.SummonInterval > 0 ? def.SummonInterval * 0.6f : 0f;
            u.ShiftTimer = def.ShiftInterval;
            if (def.StealthRange > 0f) { u.Stealthed = true; u.StealthRange = def.StealthRange; }
            else if (_w.Map.Hazard == MapHazard.Mist && def.BossId == null && def.Behavior != EnemyBehavior.Elite && !def.Flying)
            { u.Stealthed = true; u.StealthRange = MistRevealRange; }
            if (def.Move == MoveKind.Burrow) { u.State = UnitState.Burrowed; }
            if (def.BossId != null)
            {
                var bd = _w.Content.Bosses[def.BossId];
                u.Boss = new BossRuntime { Def = bd };
                foreach (var a in bd.Phases[0].Abilities) u.Boss.Cooldowns.Add(a.InitialDelay);
            }
            _w.Emit(SimEventType.Spawned, pos, 0, ElementId.None, u.Id);
            return u;
        }

        public Unit SpawnGeneral(GeneralDefinition g, Vec2 pos, bool player, int aiSlot, GeneralEvolution evo)
        {
            var u = NewUnit(g.Body, Team.Player, pos, ActorKind.General);
            u.General = g;
            u.IsPlayerControlled = player;
            u.AiSlot = aiSlot;
            u.Evolution = evo;
            ApplyGeneralStats(u, true);
            u.UltCooldown = 6f;
            if (player) _w.PlayerGeneral = u; else _w.AIGenerals.Add(u);
            return u;
        }

        public void ApplyGeneralStats(Unit u, bool fullHeal)
        {
            var g = u.General;
            var scope = u.IsPlayerControlled ? Scope.PlayerGeneral : Scope.AIGenerals;
            // AI generals are deliberately weaker so the player's positioning still matters.
            float aiMult = u.IsPlayerControlled ? 1f : 0.85f;
            var evo = u.Evolution;
            float frac = u.MaxHp > 0 ? u.Hp / u.MaxHp : 1f;
            u.MaxHp = Mods.Apply(g.Body.MaxHp * aiMult * (evo?.HpMult ?? 1f), Stat.MaxHp, scope);
            u.BaseArmor = Mods.Apply(g.Body.Armor, Stat.Armor, scope);
            u.Speed = Mods.Apply(g.Body.Speed, Stat.MoveSpeed, scope);
            var a = (evo?.AttackOverride ?? g.Body.Attack).Clone();
            if (a.Element == ElementId.None) a.Element = g.Element;
            a.Damage = Mods.Apply(a.Damage * aiMult * (evo?.DmgMult ?? 1f), Stat.Damage, scope, SoldierClass.None, a.Element);
            a.Interval /= Math.Max(0.2f, 1f + Mods.Percent(Stat.AttackSpeed, scope));
            a.HitsAir = true;
            a.Range = Mods.Apply(a.Range, Stat.Range, scope);
            u.Attack = a;
            u.SecondaryElement = ElementId.None;
            if (u.IsPlayerControlled && EquippedWeapon != null && EquippedWeapon.SecondaryElement != a.Element) u.SecondaryElement = EquippedWeapon.SecondaryElement;
            u.Hp = fullHeal ? u.MaxHp : Math.Max(1f, u.MaxHp * frac);
        }

        /// <summary>Set by GameSession from the run's loadout.</summary>
        public EquipmentDefinition EquippedWeapon;

        public GeneralAura EffectiveAura(Unit g) => g.Evolution?.AuraOverride ?? g.General.Aura;

        public float UltCooldown(Unit g)
        {
            var scope = g.IsPlayerControlled ? Scope.PlayerGeneral : Scope.AIGenerals;
            float cd = g.General.Ultimate.Cooldown * (g.Evolution?.UltCooldownMult ?? 1f);
            return Math.Max(5f, cd * (1f + Mods.Percent(Stat.UltCooldown, scope)));
        }

        public float DashCooldown(Unit g)
        {
            var scope = g.IsPlayerControlled ? Scope.PlayerGeneral : Scope.AIGenerals;
            return Math.Max(1.5f, g.General.DashCooldown * (1f + Mods.Percent(Stat.DashCooldown, scope)));
        }
    }
}
