using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    public enum FlowKind { Standard = 0, GateBreaker = 1, Burrow = 2, Siege = 3 }

    /// <summary>
    /// Enemy flow fields (Dijkstra from the Castle; structures are traversable at a "breach cost" so enemies attack
    /// walls instead of freezing when sealed out) + friendly A*. Fields are rebuilt lazily only when the topology
    /// version changes (build / destroy / repair / tide), never per frame.
    /// </summary>
    public sealed class NavigationService
    {
        private readonly World _w;
        private readonly float[][] _dist = new float[4][];
        private readonly int[] _version = { -1, -1, -1, -1 };
        private readonly int _width, _height;
        public int Rebuilds { get; private set; }

        private static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] DY = { 0, 0, 1, -1, 1, -1, 1, -1 };
        private static readonly float[] DC = { 1, 1, 1, 1, 1.4142f, 1.4142f, 1.4142f, 1.4142f };

        public NavigationService(World w)
        {
            _w = w;
            _width = w.Map.Width;
            _height = w.Map.Height;
            for (int i = 0; i < 4; i++) _dist[i] = new float[_width * _height];
            _flowHeap = new MinHeap(_width * _height);
            _pathHeap = new MinHeap(256);
            _costCache = new float[_width * _height];
        }

        // reused buffers: flow-field rebuilds and A* searches allocate nothing after warm-up
        private readonly MinHeap _flowHeap, _pathHeap;
        private readonly float[] _costCache;

        public float[] Field(FlowKind kind)
        {
            int k = (int)kind;
            if (_version[k] != _w.TopologyVersion) Build(kind);
            return _dist[k];
        }

        private float TileCost(int x, int y, FlowKind kind)
        {
            if (!_w.TerrainPassable(x, y)) return float.PositiveInfinity;
            float baseCost = _w.Map.Get(x, y) == TileType.Ice ? 0.85f : 1f;
            if (kind == FlowKind.Burrow) return baseCost;
            var b = _w.BuildingAt(x, y);
            if (b == null || b.Ruined || b.Dead) return baseCost;
            if (b.Def.Family == BuildingFamily.Castle) return 0f;
            float breach = b.Def.BreachCost * (0.6f + 0.4f * Math.Min(2f, b.MaxHp / 400f));
            if (kind == FlowKind.GateBreaker) breach = b.IsGate ? 1.5f : breach * 1.8f;
            if (kind == FlowKind.Siege) breach *= b.IsWallLike ? 0.5f : 1f;
            return baseCost + breach;
        }

        private void Build(FlowKind kind)
        {
            Rebuilds++;
            var dist = _dist[(int)kind];
            for (int i = 0; i < dist.Length; i++) dist[i] = float.PositiveInfinity;
            var open = _flowHeap;
            open.Count = 0;
            var c = _w.Map.CastleOrigin;
            for (int dx = 0; dx < 3; dx++)
                for (int dy = 0; dy < 3; dy++)
                {
                    int idx = (c.Y + dy) * _width + c.X + dx;
                    dist[idx] = 0f;
                    open.Push(idx, 0f);
                }
            var costCache = _costCache;
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                    costCache[y * _width + x] = TileCost(x, y, kind);
            while (open.Count > 0)
            {
                open.Pop(out int cur, out float d);
                if (d > dist[cur]) continue;
                int cx = cur % _width, cy = cur / _width;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + DX[k], ny = cy + DY[k];
                    if (nx < 0 || ny < 0 || nx >= _width || ny >= _height) continue;
                    int ni = ny * _width + nx;
                    float tc = costCache[ni];
                    if (float.IsPositiveInfinity(tc)) continue;
                    if (k >= 4 && (float.IsPositiveInfinity(costCache[cy * _width + nx]) || float.IsPositiveInfinity(costCache[ny * _width + cx]))) continue;
                    // enemies walk ni -> cur, so they pay for *entering* cur (breach cost is charged on the wall tile itself)
                    float enter = costCache[cur];
                    float nd = d + DC[k] + (enter > 1f ? enter - 1f : 0f);
                    if (nd < dist[ni])
                    {
                        dist[ni] = nd;
                        open.Push(ni, nd);
                    }
                }
            }
            _version[(int)kind] = _w.TopologyVersion;
        }

        /// <summary>Best neighbouring tile to step toward the castle (by flow), or the same tile if none.</summary>
        public Int2 NextStep(Int2 from, FlowKind kind)
        {
            var dist = Field(kind);
            if (!_w.Map.InBounds(from.X, from.Y)) return from;
            float best = dist[from.Y * _width + from.X];
            Int2 bestT = from;
            for (int k = 0; k < 8; k++)
            {
                int nx = from.X + DX[k], ny = from.Y + DY[k];
                if (nx < 0 || ny < 0 || nx >= _width || ny >= _height) continue;
                float d = dist[ny * _width + nx];
                if (k >= 4)
                {
                    // no diagonal corner cutting around blocked tiles or structures
                    if (!StepOpen(from.X + DX[k], from.Y, kind) || !StepOpen(from.X, from.Y + DY[k], kind)) continue;
                }
                if (d < best - 0.001f) { best = d; bestT = new Int2(nx, ny); }
            }
            return bestT;
        }

        private bool StepOpen(int x, int y, FlowKind kind)
        {
            if (!_w.TerrainPassable(x, y)) return false;
            if (kind == FlowKind.Burrow) return true;
            var b = _w.BuildingAt(x, y);
            return b == null || b.Ruined || b.Def.Family == BuildingFamily.Castle;
        }

        public float DistanceToCastle(Vec2 p, FlowKind kind = FlowKind.Standard)
        {
            var t = Int2.FromPosition(p);
            if (!_w.Map.InBounds(t.X, t.Y)) return float.PositiveInfinity;
            return Field(kind)[t.Y * _width + t.X];
        }

        // ----------------------------------------------------------- friendly A*
        private float[] _g;
        private int[] _parent;
        private int[] _stamp;
        private int _stampId;

        /// <summary>A* for friendly ground units (gates passable). Writes tiles into path; false if unreachable.</summary>
        public bool FindFriendlyPath(Int2 start, Int2 goal, List<Int2> path, int maxNodes = 900)
        {
            path.Clear();
            int n = _width * _height;
            if (_g == null) { _g = new float[n]; _parent = new int[n]; _stamp = new int[n]; }
            if (!_w.Map.InBounds(start.X, start.Y) || !_w.Map.InBounds(goal.X, goal.Y)) return false;
            if (!_w.FriendlyPassable(goal.X, goal.Y)) goal = NearestFriendlyPassable(goal);
            _stampId++;
            var open = _pathHeap;
            open.Count = 0;
            int s = start.Y * _width + start.X, gIdx = goal.Y * _width + goal.X;
            _g[s] = 0; _parent[s] = -1; _stamp[s] = _stampId;
            open.Push(s, Heur(start, goal));
            int expanded = 0;
            int bestIdx = s; float bestH = Heur(start, goal);
            while (open.Count > 0 && expanded++ < maxNodes)
            {
                open.Pop(out int cur, out _);
                if (cur == gIdx) { bestIdx = cur; break; }
                int cx = cur % _width, cy = cur / _width;
                float h = Heur(new Int2(cx, cy), goal);
                if (h < bestH) { bestH = h; bestIdx = cur; }
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + DX[k], ny = cy + DY[k];
                    if (!_w.FriendlyPassable(nx, ny)) continue;
                    if (k >= 4 && (!_w.FriendlyPassable(cx + DX[k], cy) || !_w.FriendlyPassable(cx, cy + DY[k]))) continue;
                    int ni = ny * _width + nx;
                    float ng = _g[cur] + DC[k];
                    if (_stamp[ni] == _stampId && ng >= _g[ni]) continue;
                    _stamp[ni] = _stampId; _g[ni] = ng; _parent[ni] = cur;
                    open.Push(ni, ng + Heur(new Int2(nx, ny), goal));
                }
            }
            int at = bestIdx;
            int guard = 0;
            while (at >= 0 && at != s && guard++ < n)
            {
                path.Add(new Int2(at % _width, at / _width));
                at = _parent[at];
            }
            path.Reverse();
            return bestIdx == gIdx;
        }

        public Int2 NearestFriendlyPassable(Int2 t)
        {
            for (int r = 0; r < 6; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r && _w.FriendlyPassable(t.X + dx, t.Y + dy)) return new Int2(t.X + dx, t.Y + dy);
            return t;
        }

        private static float Heur(Int2 a, Int2 b)
        {
            int dx = Math.Abs(a.X - b.X), dy = Math.Abs(a.Y - b.Y);
            return Math.Max(dx, dy) + 0.4142f * Math.Min(dx, dy);
        }

        /// <summary>Binary min-heap of (index, priority).</summary>
        private sealed class MinHeap
        {
            private int[] _items;
            private float[] _pri;
            public int Count;

            public MinHeap(int cap) { _items = new int[cap]; _pri = new float[cap]; }

            public void Push(int item, float p)
            {
                if (Count == _items.Length) { Array.Resize(ref _items, Count * 2); Array.Resize(ref _pri, Count * 2); }
                int i = Count++;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (_pri[parent] <= p) break;
                    _items[i] = _items[parent]; _pri[i] = _pri[parent];
                    i = parent;
                }
                _items[i] = item; _pri[i] = p;
            }

            public void Pop(out int item, out float p)
            {
                item = _items[0]; p = _pri[0];
                int lastI = _items[--Count]; float lastP = _pri[Count];
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1;
                    if (l >= Count) break;
                    int r = l + 1;
                    int m = r < Count && _pri[r] < _pri[l] ? r : l;
                    if (_pri[m] >= lastP) break;
                    _items[i] = _items[m]; _pri[i] = _pri[m];
                    i = m;
                }
                _items[i] = lastI; _pri[i] = lastP;
            }
        }
    }
}
