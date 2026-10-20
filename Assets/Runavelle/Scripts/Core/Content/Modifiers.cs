using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Numeric stats that run-level effects (decrees, equipment, evolutions, ascension) can modify.</summary>
    public enum Stat
    {
        Damage, AttackSpeed, Range, MaxHp, Armor, MoveSpeed, StatusPower, SquadSize,
        GoldIncome, ManaIncome, CostGold, CostMana, ReactionDamage, UltCooldown, UltPower, AuraPower,
        DashCooldown, NightReward, ReinforceCost, Regen, DamageTaken, BossDamage, Bounty
    }

    /// <summary>What a modifier applies to.</summary>
    public enum Scope
    {
        Any, Soldiers, Towers, ArcherTowers, MagicTowers, Walls, Gates, Castle, PlayerGeneral, AIGenerals, Generals,
        Houses, Wells, Shrines, Barracks, Structures, Enemies, Bosses
    }

    public sealed class Modifier
    {
        public Stat Stat;
        public Scope Scope;
        /// <summary>Additive percent, e.g. 0.2 = +20%. Summed, then applied once (no double dipping).</summary>
        public float Percent;
        /// <summary>Flat addition applied before percent.</summary>
        public float Flat;
        public SoldierClass Class = SoldierClass.None;
        public ElementId Element = ElementId.None;

        public Modifier() { }
        public Modifier(Stat stat, Scope scope, float percent, float flat = 0f)
        {
            Stat = stat; Scope = scope; Percent = percent; Flat = flat;
        }

        public string Describe()
        {
            string target = Scope == Scope.Any ? "" : " (" + Loc.Scope(Scope) + (Class != SoldierClass.None ? ": " + Loc.Soldier(Class) : "") + (Element != ElementId.None ? ", " + Loc.Element(Element) : "") + ")";
            string v = Percent != 0f ? (Percent > 0 ? "+" : "") + (int)Math.Round(Percent * 100) + "%" : "";
            if (Flat != 0f) v += (v.Length > 0 ? " " : "") + (Flat > 0 ? "+" : "") + Flat;
            return Loc.Stat(Stat) + " " + v + target;
        }
    }

    /// <summary>Special behaviour switches. Each one is implemented by a real system (grep the enum name).</summary>
    public enum Hook
    {
        None,
        WallsMendOnUltimate, StormLegion, ArcaneRebirth, SacrificialRampart, LivingRamparts, StormArrows,
        BattleHymn, MagmaBastion, LastWatch, ConductiveRain, Permafrost, Wildfire, TacticalRetreat,
        FirstStrike, Bloodlust, EchoUltimate, GoldOnReaction, ManaOnKill, ThornWalls, RadiantWalls,
        ShadowHarvest, EmergencyMasons, VeteranCore, ElementalOverflow, SiegeBreakers, SkyWatch, GeneralLifesteal,
        ManaShieldCastle, ReinforcedGates, FrostWalls, ChainOverload, EclipsePact, DawnVigil, CursedGold,
        GlassCannon, BurdenOfCrown, Insomnia, HeavyCrown, WildMagic, MercenaryLegion, IronDiscipline,
        RoyalArchitect, QuickMasons, FieldMedics, Pathfinders, TowerRebuild, HeroicPresence, DoubleUltCharge, ShadowSacrifice,
        LegionReinforce, FastRevive, SeerLantern, LastBastion
    }

    public sealed class HookValue
    {
        public Hook Hook;
        public float Value;
        public HookValue() { }
        public HookValue(Hook h, float v) { Hook = h; Value = v; }
    }

    /// <summary>
    /// Aggregated run-level modifiers. Percentages from all sources are summed per (stat, scope) query and applied once.
    /// </summary>
    public sealed class RunModifiers
    {
        private readonly List<Modifier> _mods = new List<Modifier>();
        private readonly Dictionary<Hook, float> _hooks = new Dictionary<Hook, float>();
        private readonly Dictionary<long, float> _cachePct = new Dictionary<long, float>();
        private readonly Dictionary<long, float> _cacheFlat = new Dictionary<long, float>();

        public IReadOnlyList<Modifier> All => _mods;
        public IEnumerable<KeyValuePair<Hook, float>> Hooks => _hooks;

        public void Clear()
        {
            _mods.Clear();
            _hooks.Clear();
            Invalidate();
        }

        public void Add(Modifier m)
        {
            _mods.Add(m);
            Invalidate();
        }

        public void AddRange(IEnumerable<Modifier> ms)
        {
            if (ms == null) return;
            foreach (var m in ms) _mods.Add(m);
            Invalidate();
        }

        public void AddHook(Hook h, float value)
        {
            _hooks.TryGetValue(h, out float cur);
            _hooks[h] = cur + (value == 0f ? 1f : value);
        }

        public bool Has(Hook h) => _hooks.ContainsKey(h);
        public float HookValue(Hook h) => _hooks.TryGetValue(h, out float v) ? v : 0f;

        private void Invalidate()
        {
            _cachePct.Clear();
            _cacheFlat.Clear();
        }

        private static bool ScopeMatches(Scope modScope, Scope query)
        {
            if (modScope == Scope.Any || modScope == query) return true;
            switch (modScope)
            {
                case Scope.Towers: return query == Scope.ArcherTowers || query == Scope.MagicTowers;
                case Scope.Generals: return query == Scope.PlayerGeneral || query == Scope.AIGenerals;
                case Scope.Structures:
                    return query == Scope.Towers || query == Scope.ArcherTowers || query == Scope.MagicTowers || query == Scope.Walls ||
                           query == Scope.Gates || query == Scope.Castle || query == Scope.Houses || query == Scope.Wells ||
                           query == Scope.Shrines || query == Scope.Barracks;
                case Scope.Walls: return query == Scope.Gates;
                case Scope.Enemies: return query == Scope.Bosses;
                default: return false;
            }
        }

        private static bool PlayerSide(Scope s) => s != Scope.Enemies && s != Scope.Bosses;

        /// <summary>Summed percent for a stat (0 = unchanged).</summary>
        public float Percent(Stat stat, Scope scope, SoldierClass cls = SoldierClass.None, ElementId element = ElementId.None)
        {
            long key = Key(stat, scope, cls, element);
            if (_cachePct.TryGetValue(key, out float cached)) return cached;
            float sum = 0f, flat = 0f;
            for (int i = 0; i < _mods.Count; i++)
            {
                var m = _mods[i];
                if (m.Stat != stat) continue;
                if (m.Scope == Scope.Any && !PlayerSide(scope)) continue; // "Any" never buffs enemies
                if (!ScopeMatches(m.Scope, scope)) continue;
                if (m.Class != SoldierClass.None && m.Class != cls) continue;
                if (m.Element != ElementId.None && m.Element != element) continue;
                sum += m.Percent;
                flat += m.Flat;
            }
            _cachePct[key] = sum;
            _cacheFlat[key] = flat;
            return sum;
        }

        public float Flat(Stat stat, Scope scope, SoldierClass cls = SoldierClass.None, ElementId element = ElementId.None)
        {
            Percent(stat, scope, cls, element);
            return _cacheFlat[Key(stat, scope, cls, element)];
        }

        /// <summary>(base + flat) * (1 + percent), floored at 0.</summary>
        public float Apply(float baseValue, Stat stat, Scope scope, SoldierClass cls = SoldierClass.None, ElementId element = ElementId.None)
        {
            float p = Percent(stat, scope, cls, element);
            float f = Flat(stat, scope, cls, element);
            return Math.Max(0f, (baseValue + f) * (1f + p));
        }

        private static long Key(Stat s, Scope sc, SoldierClass c, ElementId e) => ((long)s << 24) | ((long)sc << 16) | ((long)c << 8) | (long)e;
    }
}
