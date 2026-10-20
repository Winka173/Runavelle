using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Uniform-grid spatial hash for unit proximity queries (rebuilt once per tick).</summary>
    public sealed class SpatialHash
    {
        private readonly int _w, _h;
        private readonly float _cell;
        private readonly List<Unit>[] _cells;

        public SpatialHash(int mapW, int mapH, float cell)
        {
            _cell = cell;
            _w = (int)Math.Ceiling(mapW / cell) + 1;
            _h = (int)Math.Ceiling(mapH / cell) + 1;
            _cells = new List<Unit>[_w * _h];
            for (int i = 0; i < _cells.Length; i++) _cells[i] = new List<Unit>(8);
        }

        public void Clear()
        {
            for (int i = 0; i < _cells.Length; i++) _cells[i].Clear();
        }

        public void Insert(Unit u)
        {
            int cx = MathUtil.Clamp((int)(u.Position.X / _cell), 0, _w - 1);
            int cy = MathUtil.Clamp((int)(u.Position.Y / _cell), 0, _h - 1);
            _cells[cy * _w + cx].Add(u);
        }

        public void Query(Vec2 p, float r, List<Unit> results)
        {
            int x0 = MathUtil.Clamp((int)((p.X - r) / _cell), 0, _w - 1), x1 = MathUtil.Clamp((int)((p.X + r) / _cell), 0, _w - 1);
            int y0 = MathUtil.Clamp((int)((p.Y - r) / _cell), 0, _h - 1), y1 = MathUtil.Clamp((int)((p.Y + r) / _cell), 0, _h - 1);
            float r2 = r * r;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var list = _cells[y * _w + x];
                    for (int i = 0; i < list.Count; i++)
                    {
                        var u = list[i];
                        float rr = r + u.Radius;
                        if (Vec2.SqrDistance(u.Position, p) <= rr * rr || Vec2.SqrDistance(u.Position, p) <= r2) results.Add(u);
                    }
                }
        }
    }

    /// <summary>All mutable simulation state for one run. Systems operate on it; presentation only reads it.</summary>
    public sealed class World
    {
        public const float Dt = 0.05f; // 20 Hz fixed tick
        public const int MaxReactionsPerTick = 24;

        public readonly ContentDatabase Content;
        public readonly MapDefinition Map;
        public readonly RunModifiers Mods = new RunModifiers();
        public readonly RunAnalytics Analytics = new RunAnalytics();
        public Rng Rng;
        public float Time;
        public int Tick;
        public int Night;
        public bool IsNight;
        public int ReactionsThisTick;

        public readonly List<Building> Buildings = new List<Building>();
        public readonly List<Unit> Units = new List<Unit>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        public readonly List<Zone> Zones = new List<Zone>();
        public readonly List<Telegraph> Telegraphs = new List<Telegraph>();
        public readonly Dictionary<int, Actor> ById = new Dictionary<int, Actor>();
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public readonly int[] Occupancy;
        public readonly SpatialHash Friendly;
        public readonly SpatialHash Hostile;
        public Building Castle;
        public Unit PlayerGeneral;
        public readonly List<Unit> AIGenerals = new List<Unit>();
        public float CastleGuardTimer, CastleGuardAmount;
        public int TopologyVersion;
        /// <summary>Hazard state: high tide / mist density / etc. controlled by the HazardSystem.</summary>
        public bool TideHigh;
        /// <summary>Last Bastion decree already fired this night (cap: once per night).</summary>
        public bool LastBastionUsed;
        /// <summary>World.Time when the current night started (for night-relative analytics).</summary>
        public float NightStartTime;
        /// <summary>Thin-ice tiles cracked open by Fire this night (impassable water until dawn). Key = y * width + x.</summary>
        public readonly HashSet<int> CrackedIce = new HashSet<int>();
        public int NextId = 1;
        /// <summary>Bounties / shadow-mark mana earned during the night; paid at dawn.</summary>
        public int PendingGold, PendingMana;
        public readonly List<Unit> DeadEnemies = new List<Unit>();
        /// <summary>Per-night once-only flags (Arcane Rebirth, Double Ultimate) and reaction counter (Elemental Overflow).</summary>
        public bool RebirthUsed, DoubleUltUsed;
        public int ReactionCounter;
        private readonly List<Unit> _scratch = new List<Unit>(64);

        public World(ContentDatabase content, MapDefinition map, ulong seed)
        {
            Content = content;
            Map = map;
            Rng = Rng.Derive(seed, 7);
            Occupancy = new int[map.Width * map.Height];
            for (int i = 0; i < Occupancy.Length; i++) Occupancy[i] = -1;
            Friendly = new SpatialHash(map.Width, map.Height, 2f);
            Hostile = new SpatialHash(map.Width, map.Height, 2f);
        }

        public int AllocId() => NextId++;

        public void Emit(SimEventType type, Vec2 pos, float value = 0f, ElementId e = ElementId.None, int actor = -1, string text = null, int other = -1, ReactionId r = ReactionId.None)
        {
            if (Events.Count > 4000) return; // presentation not draining (headless) – cap memory
            Events.Add(new SimEvent { Type = type, Position = pos, Value = value, Element = e, ActorId = actor, OtherId = other, Text = text, Reaction = r });
        }

        public Actor Get(int id) => id >= 0 && ById.TryGetValue(id, out var a) ? a : null;

        public Building BuildingAt(int x, int y)
        {
            if (!Map.InBounds(x, y)) return null;
            int id = Occupancy[y * Map.Width + x];
            return id >= 0 ? ById[id] as Building : null;
        }

        public bool InBounds(Int2 t) => Map.InBounds(t.X, t.Y);

        public bool TerrainPassable(int x, int y)
        {
            if (!Map.InBounds(x, y)) return false;
            var t = Map.Get(x, y);
            if (t == TileType.Shallows && TideHigh) return false;
            if (t == TileType.Ice && CrackedIce.Count > 0 && CrackedIce.Contains(y * Map.Width + x)) return false;
            return MapDefinition.GroundPassable(t);
        }

        /// <summary>Ground passability for friendly units: terrain + no structure except open gates / ruins.</summary>
        public bool FriendlyPassable(int x, int y)
        {
            if (!TerrainPassable(x, y)) return false;
            var b = BuildingAt(x, y);
            return b == null || b.Ruined || b.IsGate;
        }

        // ---------------------------------------------------------------- pooled scratch lists (no per-hit allocations)
        private readonly Stack<List<Unit>> _listPool = new Stack<List<Unit>>();
        public List<Unit> RentList() => _listPool.Count > 0 ? _listPool.Pop() : new List<Unit>(32);
        public void ReturnList(List<Unit> l) { l.Clear(); _listPool.Push(l); }

        public void RebuildSpatial()
        {
            Friendly.Clear();
            Hostile.Clear();
            for (int i = 0; i < Units.Count; i++)
            {
                var u = Units[i];
                if (u.Dead) continue;
                if (u.Team == Team.Player) Friendly.Insert(u); else Hostile.Insert(u);
            }
        }

        /// <summary>Living, targetable units of <paramref name="team"/> within r (unordered, reuses an internal list).</summary>
        public List<Unit> UnitsInRadius(Team team, Vec2 p, float r, List<Unit> into = null)
        {
            var list = into ?? _scratch;
            list.Clear();
            (team == Team.Player ? Friendly : Hostile).Query(p, r, list);
            for (int i = list.Count - 1; i >= 0; i--)
                if (!list[i].Targetable) list.RemoveAt(i);
            return list;
        }

        public float DistanceToActor(Vec2 p, Actor a)
        {
            if (a is Building b)
            {
                float cx = MathUtil.Clamp(p.X, b.Origin.X, b.Origin.X + b.Size);
                float cy = MathUtil.Clamp(p.Y, b.Origin.Y, b.Origin.Y + b.Size);
                return Vec2.Distance(p, new Vec2(cx, cy));
            }
            return Math.Max(0f, Vec2.Distance(p, a.Position) - a.Radius);
        }

        public Vec2 ClosestPoint(Vec2 p, Actor a)
        {
            if (a is Building b)
                return new Vec2(MathUtil.Clamp(p.X, b.Origin.X + 0.05f, b.Origin.X + b.Size - 0.05f), MathUtil.Clamp(p.Y, b.Origin.Y + 0.05f, b.Origin.Y + b.Size - 0.05f));
            return a.Position;
        }

        public IEnumerable<Building> LiveBuildings()
        {
            for (int i = 0; i < Buildings.Count; i++)
                if (!Buildings[i].Dead && !Buildings[i].Ruined) yield return Buildings[i];
        }

        public int CountFamily(BuildingFamily f)
        {
            int n = 0;
            foreach (var b in Buildings) if (!b.Dead && b.Def.Family == f) n++;
            return n;
        }

        public static Scope ScopeOf(Building b)
        {
            switch (b.Def.Family)
            {
                case BuildingFamily.Castle: return Scope.Castle;
                case BuildingFamily.House: return Scope.Houses;
                case BuildingFamily.ManaWell: return Scope.Wells;
                case BuildingFamily.Wall: return b.Level.IsGate ? Scope.Gates : Scope.Walls;
                case BuildingFamily.ArcherTower: return Scope.ArcherTowers;
                case BuildingFamily.MagicTower: return Scope.MagicTowers;
                case BuildingFamily.Barracks: return Scope.Barracks;
                default: return Scope.Shrines;
            }
        }

        public Scope ScopeOf(Actor a)
        {
            if (a is Building b) return ScopeOf(b);
            var u = (Unit)a;
            if (u.Team == Team.Enemy) return u.IsBoss ? Scope.Bosses : Scope.Enemies;
            if (u.Role == UnitRole.General) return u.IsPlayerControlled ? Scope.PlayerGeneral : Scope.AIGenerals;
            return Scope.Soldiers;
        }

        public SoldierClass ClassOf(Actor a) => a is Unit u ? u.Def.Class : SoldierClass.None;
    }
}
