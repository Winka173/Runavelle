using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    public enum TileType : byte { Grass, Dirt, Mountain, Chasm, Water, Forest, Lava, Ice, Shallows, Plateau, Sand, Bridge }

    public enum MapHazard { None, LightningStrikes, LavaVents, Tide, Mist, HighGround, RuinedWalls, ThinIce }

    public sealed class SpawnLane
    {
        public string Id;
        public string Name;
        public List<Int2> Tiles = new List<Int2>();
        /// <summary>Only flyers spawn here (exceptional flank access; always telegraphed).</summary>
        public bool FlyerOnly;
        /// <summary>Lane only becomes an attack front from this night on (narrow crossings open late).</summary>
        public int MinNight = 1;
        public Vec2 Center
        {
            get
            {
                float x = 0, y = 0;
                foreach (var t in Tiles) { x += t.X + 0.5f; y += t.Y + 0.5f; }
                return Tiles.Count > 0 ? new Vec2(x / Tiles.Count, y / Tiles.Count) : Vec2.Zero;
            }
        }
    }

    public sealed class MapDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public string ProtectedSides;
        public string Theme;
        public int Width;
        public int Height;
        public TileType[] Tiles;
        public Int2 CastleOrigin;
        public List<SpawnLane> Lanes = new List<SpawnLane>();
        public MapHazard Hazard;
        /// <summary>Pre-placed ruined wall remnants (repairable cheaply) for maps that have them.</summary>
        public List<Int2> RuinWalls = new List<Int2>();
        /// <summary>Boss ids for nights 5 / 10 / 15 on this map (null entries fall back to the default rotation).</summary>
        public string[] Bosses = new string[3];
        public int Difficulty = 1;
        /// <summary>Enemy factions that attack on this map (wave pools are filtered by these).</summary>
        public List<Faction> Factions = new List<Faction> { Faction.Marauders, Faction.ShadowCourt };
        public int UnlockOrder;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public TileType Get(int x, int y) => Tiles[y * Width + x];
        public void Set(int x, int y, TileType t) { if (InBounds(x, y)) Tiles[y * Width + x] = t; }

        public static bool GroundPassable(TileType t)
        {
            switch (t)
            {
                case TileType.Mountain: case TileType.Chasm: case TileType.Water: case TileType.Forest: case TileType.Lava: return false;
                default: return true;
            }
        }

        public static bool Buildable(TileType t)
        {
            switch (t)
            {
                case TileType.Grass: case TileType.Dirt: case TileType.Plateau: case TileType.Sand: return true;
                default: return false;
            }
        }

        /// <summary>Returns null if valid, otherwise a reason. Every ground lane must reach the castle.</summary>
        public string Validate()
        {
            if (Width < 16 || Height < 12 || Tiles == null || Tiles.Length != Width * Height) return "bad size";
            for (int dx = 0; dx < 3; dx++)
                for (int dy = 0; dy < 3; dy++)
                    if (!Buildable(Get(CastleOrigin.X + dx, CastleOrigin.Y + dy))) return "castle footprint not buildable";
            if (Lanes.Count < 2) return "needs at least 2 lanes";
            int groundLanes = 0;
            var reach = FloodFromCastle();
            foreach (var lane in Lanes)
            {
                if (lane.Tiles.Count == 0) return "empty lane " + lane.Id;
                if (lane.FlyerOnly) continue;
                groundLanes++;
                foreach (var t in lane.Tiles)
                {
                    if (!InBounds(t.X, t.Y) || !GroundPassable(Get(t.X, t.Y))) return "lane " + lane.Id + " tile not passable " + t;
                    if (!reach[t.Y * Width + t.X]) return "lane " + lane.Id + " cannot reach castle from " + t;
                }
            }
            if (groundLanes < 1) return "no ground lane";
            // design lock: 1-2 naturally protected sides => at least one full map edge mostly impassable
            int protectedEdges = 0;
            if (EdgeBlockedFraction(0) > 0.7f) protectedEdges++;
            if (EdgeBlockedFraction(1) > 0.7f) protectedEdges++;
            if (EdgeBlockedFraction(2) > 0.7f) protectedEdges++;
            if (EdgeBlockedFraction(3) > 0.7f) protectedEdges++;
            if (protectedEdges < 1 || protectedEdges > 2) return "expected 1-2 naturally protected sides, found " + protectedEdges;
            return null;
        }

        /// <summary>edge: 0=north,1=east,2=south,3=west. Fraction of the outer 3-tile band that is impassable.</summary>
        public float EdgeBlockedFraction(int edge)
        {
            int blocked = 0, total = 0;
            int len = edge % 2 == 0 ? Width : Height;
            for (int i = 0; i < len; i++)
                for (int d = 0; d < 3; d++)
                {
                    int x, y;
                    switch (edge)
                    {
                        case 0: x = i; y = Height - 1 - d; break;
                        case 1: x = Width - 1 - d; y = i; break;
                        case 2: x = i; y = d; break;
                        default: x = d; y = i; break;
                    }
                    total++;
                    if (!GroundPassable(Get(x, y))) blocked++;
                }
            return total == 0 ? 0 : blocked / (float)total;
        }

        public bool[] FloodFromCastle()
        {
            var seen = new bool[Width * Height];
            var q = new Queue<Int2>();
            for (int dx = 0; dx < 3; dx++)
                for (int dy = 0; dy < 3; dy++)
                {
                    var c = new Int2(CastleOrigin.X + dx, CastleOrigin.Y + dy);
                    seen[c.Y * Width + c.X] = true;
                    q.Enqueue(c);
                }
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                for (int k = 0; k < 4; k++)
                {
                    int nx = c.X + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = c.Y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    if (!InBounds(nx, ny) || seen[ny * Width + nx] || !GroundPassable(Get(nx, ny))) continue;
                    seen[ny * Width + nx] = true;
                    q.Enqueue(new Int2(nx, ny));
                }
            }
            return seen;
        }
    }

    /// <summary>Deterministic helper for authoring curated maps in code (fixed seeds; never random at runtime).</summary>
    public sealed class MapBuilder
    {
        public readonly MapDefinition Map;
        private readonly Rng _rng;

        public MapBuilder(string id, int w, int h, ulong authorSeed)
        {
            Map = new MapDefinition { Id = id, Width = w, Height = h, Tiles = new TileType[w * h] };
            _rng = new Rng(authorSeed);
        }

        public MapBuilder Fill(int x0, int y0, int x1, int y1, TileType t)
        {
            for (int y = Math.Max(0, y0); y <= Math.Min(Map.Height - 1, y1); y++)
                for (int x = Math.Max(0, x0); x <= Math.Min(Map.Width - 1, x1); x++)
                    Map.Set(x, y, t);
            return this;
        }

        /// <summary>Ragged band along an edge. edge: 0=N,1=E,2=S,3=W.</summary>
        public MapBuilder EdgeBand(int edge, int minDepth, int maxDepth, TileType t, int from = 0, int to = int.MaxValue)
        {
            int len = edge % 2 == 0 ? Map.Width : Map.Height;
            int depth = (minDepth + maxDepth) / 2;
            for (int i = Math.Max(0, from); i < Math.Min(len, to); i++)
            {
                depth = MathUtil.Clamp(depth + _rng.Range(-1, 2), minDepth, maxDepth);
                for (int d = 0; d < depth; d++)
                {
                    switch (edge)
                    {
                        case 0: Map.Set(i, Map.Height - 1 - d, t); break;
                        case 1: Map.Set(Map.Width - 1 - d, i, t); break;
                        case 2: Map.Set(i, d, t); break;
                        default: Map.Set(d, i, t); break;
                    }
                }
            }
            return this;
        }

        public MapBuilder Blob(int cx, int cy, int r, TileType t, float density = 0.85f)
        {
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    float d = (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d <= r - 0.5f || (d <= r + 0.5f && _rng.Chance(density - 0.4f))) Map.Set(x, y, t);
                }
            return this;
        }

        public MapBuilder Scatter(int x0, int y0, int x1, int y1, TileType t, float chance, TileType only = TileType.Grass)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (Map.InBounds(x, y) && Map.Get(x, y) == only && _rng.Chance(chance)) Map.Set(x, y, t);
            return this;
        }

        public MapBuilder Path(Int2 a, Int2 b, TileType t, int width = 1)
        {
            int x = a.X, y = a.Y;
            int guard = 0;
            while ((x != b.X || y != b.Y) && guard++ < 2000)
            {
                for (int w = 0; w < width; w++)
                {
                    Map.Set(x + (Math.Abs(b.Y - a.Y) > Math.Abs(b.X - a.X) ? w : 0), y + (Math.Abs(b.Y - a.Y) > Math.Abs(b.X - a.X) ? 0 : w), t);
                }
                bool horizontal = Math.Abs(b.X - x) > Math.Abs(b.Y - y) ? !_rng.Chance(0.2f) : _rng.Chance(0.2f);
                if (horizontal && x != b.X) x += Math.Sign(b.X - x);
                else if (y != b.Y) y += Math.Sign(b.Y - y);
                else x += Math.Sign(b.X - x);
            }
            Map.Set(b.X, b.Y, t);
            return this;
        }

        public MapBuilder Castle(int x, int y)
        {
            Map.CastleOrigin = new Int2(x, y);
            Fill(x - 2, y - 2, x + 4, y + 4, TileType.Grass);
            return this;
        }

        /// <summary>Lane: tiles in a line along a map edge, forced passable.</summary>
        public MapBuilder Lane(string id, string name, int edge, int from, int to, bool flyerOnly = false, int minNight = 1)
        {
            var lane = new SpawnLane { Id = id, Name = name, FlyerOnly = flyerOnly, MinNight = minNight };
            for (int i = from; i <= to; i++)
            {
                Int2 p;
                switch (edge)
                {
                    case 0: p = new Int2(i, Map.Height - 1); break;
                    case 1: p = new Int2(Map.Width - 1, i); break;
                    case 2: p = new Int2(i, 0); break;
                    default: p = new Int2(0, i); break;
                }
                if (!flyerOnly && !MapDefinition.GroundPassable(Map.Get(p.X, p.Y))) Map.Set(p.X, p.Y, TileType.Dirt);
                lane.Tiles.Add(p);
            }
            Map.Lanes.Add(lane);
            return this;
        }

        /// <summary>Carves a dirt road from the lane centre toward the castle so every ground lane is reachable.</summary>
        public MapBuilder RoadFromLane(string laneId, int width = 2)
        {
            var lane = Map.Lanes.Find(l => l.Id == laneId);
            var c = lane.Tiles[lane.Tiles.Count / 2];
            Path(c, new Int2(Map.CastleOrigin.X + 1, Map.CastleOrigin.Y + 1), TileType.Dirt, width);
            return this;
        }
    }
}
