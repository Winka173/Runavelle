using System.Collections.Generic;
using Runavelle.Core;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>Read-only presentation of the simulation World using pooled SpriteRenderers.</summary>
    public sealed class WorldView
    {
        private const int OrderTerrain = -32000, OrderGround = -31000, OrderNight = -30500, OrderLight = -30400;
        public static int SortY(float y) => 10000 - Mathf.RoundToInt(y * 40f);

        private readonly GameSession _s;
        private readonly Transform _root;
        private readonly Dictionary<int, UnitView> _units = new Dictionary<int, UnitView>();
        private readonly Dictionary<int, BuildingView> _buildings = new Dictionary<int, BuildingView>();
        private readonly Dictionary<int, SpriteRenderer> _projectiles = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, SpriteRenderer> _zones = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, TelegraphView> _telegraphs = new Dictionary<int, TelegraphView>();
        private readonly Stack<SpriteRenderer> _srPool = new Stack<SpriteRenderer>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _remove = new List<int>();
        private SpriteRenderer _night;
        private readonly List<SpriteRenderer> _laneMarkers = new List<SpriteRenderer>();
        private float _nightBlend;
        public readonly Vfx Vfx;
        public float Alpha;
        public float Time;

        public WorldView(GameSession s, Transform parent)
        {
            _s = s;
            _root = new GameObject("World").transform;
            _root.SetParent(parent, false);
            Vfx = new Vfx(_root);
            BuildTerrain();
            _night = NewSR("night", ArtLibrary.Get("fx/pixel"), OrderNight);
            _night.transform.localScale = new Vector3(s.W.Map.Width * 8 + 400, s.W.Map.Height * 8 + 400, 1);
            _night.transform.position = new Vector3(s.W.Map.Width / 2f, s.W.Map.Height / 2f, 0);
            _night.color = new Color(0.1f, 0.08f, 0.25f, 0f);
        }

        public void Destroy() => Object.Destroy(_root.gameObject);

        private SpriteRenderer NewSR(string name, Sprite sprite, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ?? _root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private SpriteRenderer Rent(Sprite sprite, int order)
        {
            var sr = _srPool.Count > 0 ? _srPool.Pop() : NewSR("pooled", sprite, order);
            sr.gameObject.SetActive(true);
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = Color.white;
            sr.transform.localScale = Vector3.one;
            sr.transform.rotation = Quaternion.identity;
            sr.flipX = false;
            return sr;
        }

        private void Return(SpriteRenderer sr)
        {
            sr.gameObject.SetActive(false);
            _srPool.Push(sr);
        }

        // ------------------------------------------------------------------ terrain
        private void BuildTerrain()
        {
            var map = _s.W.Map;
            var terrain = new GameObject("Terrain").transform;
            terrain.SetParent(_root, false);
            var rng = new System.Random(map.Id.GetHashCode());
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    var t = map.Get(x, y);
                    string key = TerrainKey(t, x, y, rng);
                    int blobOrder = BlobOrder(t);
                    if (blobOrder >= 0 && IsEdge(map, x, y, t))
                    {
                        var nb = DominantNeighbour(map, x, y, t);
                        key = BlobOrder(nb) >= 0 && BlobOrder(nb) < blobOrder ? "terrain/" + TerrainName(nb) + "_0" : "terrain/grass_0";
                        var blob = NewSR("blob", ArtLibrary.Get("terrain/" + TerrainName(t) + "_blob_" + rng.Next(3)), OrderTerrain + 2 + blobOrder, terrain);
                        blob.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);
                    }
                    var sr = NewSR("t", ArtLibrary.Get(key), OrderTerrain, terrain);
                    sr.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);
                    sr.transform.localScale = new Vector3(1.01f, 1.01f, 1f);
                    // chasm/water edge darkening for depth
                    if (t == TileType.Chasm || t == TileType.Water)
                    {
                        bool landAbove = map.InBounds(x, y + 1) && MapDefinition.GroundPassable(map.Get(x, y + 1));
                        if (landAbove)
                        {
                            var lip = NewSR("lip", ArtLibrary.Get("fx/pixel"), OrderTerrain + 1, terrain);
                            lip.transform.position = new Vector3(x + 0.5f, y + 0.92f, 0);
                            lip.transform.localScale = new Vector3(8f, 1.3f, 1f);
                            lip.color = t == TileType.Chasm ? new Color(0.55f, 0.42f, 0.32f, 1f) : new Color(0.85f, 0.95f, 1f, 0.55f);
                        }
                    }
                    // decor
                    if (t == TileType.Mountain && rng.NextDouble() < 0.55)
                        Decor(terrain, "decor/mountain_" + rng.Next(4), x + 0.5f + (float)(rng.NextDouble() - 0.5) * 0.4f, y + 0.3f, 0.9f + (float)rng.NextDouble() * 0.5f);
                    else if (t == TileType.Forest && rng.NextDouble() < 0.85)
                        Decor(terrain, "decor/tree_" + rng.Next(4), x + 0.5f + (float)(rng.NextDouble() - 0.5) * 0.5f, y + 0.25f, 0.9f + (float)rng.NextDouble() * 0.3f);
                    else if ((t == TileType.Grass) && rng.NextDouble() < 0.03)
                        Decor(terrain, rng.NextDouble() < 0.5 ? "decor/rock_0" : "decor/reeds_0", x + 0.5f, y + 0.4f, 0.8f);
                    else if (t == TileType.Chasm && rng.NextDouble() < 0.04)
                        Decor(terrain, "decor/crystal_0", x + 0.5f, y + 0.4f, 0.8f);
                }
        }

        private static readonly TileType[] BlobKinds = { TileType.Plateau, TileType.Sand, TileType.Dirt, TileType.Ice, TileType.Shallows, TileType.Water, TileType.Lava, TileType.Chasm };
        private static int BlobOrder(TileType t) => System.Array.IndexOf(BlobKinds, t);
        private static string TerrainName(TileType t) => t.ToString().ToLowerInvariant();

        private static TileType DominantNeighbour(MapDefinition m, int x, int y, TileType self)
        {
            var counts = new Dictionary<TileType, int>();
            foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                if (!m.InBounds(x + d.Item1, y + d.Item2)) continue;
                var n = m.Get(x + d.Item1, y + d.Item2);
                if (n == self) continue;
                counts.TryGetValue(n, out int c);
                counts[n] = c + 1;
            }
            TileType best = TileType.Grass; int bc = 0;
            foreach (var kv in counts) if (kv.Value > bc) { bc = kv.Value; best = kv.Key; }
            return best;
        }

        private static bool IsEdge(MapDefinition m, int x, int y, TileType t)
        {
            return (m.InBounds(x + 1, y) && m.Get(x + 1, y) != t) || (m.InBounds(x - 1, y) && m.Get(x - 1, y) != t) ||
                   (m.InBounds(x, y + 1) && m.Get(x, y + 1) != t) || (m.InBounds(x, y - 1) && m.Get(x, y - 1) != t);
        }

        private void Decor(Transform parent, string key, float x, float y, float scale)
        {
            var sr = NewSR("d", ArtLibrary.Get(key), SortY(y), parent);
            sr.transform.position = new Vector3(x, y, 0);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static string TerrainKey(TileType t, int x, int y, System.Random rng)
        {
            switch (t)
            {
                case TileType.Grass: return "terrain/grass_" + (rng.NextDouble() < 0.08 ? 3 : rng.Next(3));
                case TileType.Dirt: return "terrain/dirt_" + rng.Next(2);
                case TileType.Sand: return "terrain/sand_" + rng.Next(2);
                case TileType.Plateau: return "terrain/plateau_" + rng.Next(2);
                case TileType.Ice: return "terrain/ice_" + rng.Next(2);
                case TileType.Water: return "terrain/water_" + rng.Next(2);
                case TileType.Shallows: return "terrain/shallows_" + rng.Next(2);
                case TileType.Chasm: return "terrain/chasm_" + rng.Next(2);
                case TileType.Lava: return "terrain/lava_" + rng.Next(2);
                case TileType.Bridge: return "terrain/bridge_0";
                case TileType.Mountain: return "terrain/mountain_" + rng.Next(2);
                default: return "terrain/grass_" + rng.Next(3); // forest: trees are decor on grass
            }
        }

        // ------------------------------------------------------------------ frame sync
        public void Sync(float dt, GameSettings settings, bool showLaneForecast)
        {
            Time += dt;
            var w = _s.W;
            float target = w.IsNight ? 0.42f : 0f;
            _nightBlend = Mathf.MoveTowards(_nightBlend, target, dt * 0.5f);
            _night.color = new Color(0.08f, 0.07f, 0.24f, _nightBlend);

            // buildings
            _seen.Clear();
            foreach (var b in w.Buildings)
            {
                if (b.Dead) continue;
                _seen.Add(b.Id);
                if (!_buildings.TryGetValue(b.Id, out var bv)) { bv = new BuildingView(this, b); _buildings[b.Id] = bv; }
                bv.Sync(b, WallMask(b), _nightBlend, settings);
            }
            _remove.Clear();
            foreach (var kv in _buildings) if (!_seen.Contains(kv.Key)) _remove.Add(kv.Key);
            foreach (var id in _remove) { _buildings[id].Destroy(); _buildings.Remove(id); }

            // units
            _seen.Clear();
            foreach (var u in w.Units)
            {
                if (u.Dead) continue;
                _seen.Add(u.Id);
                if (!_units.TryGetValue(u.Id, out var uv)) { uv = new UnitView(this, u); _units[u.Id] = uv; }
                uv.Sync(u, Alpha, dt, Time, _nightBlend, settings);
            }
            _remove.Clear();
            foreach (var kv in _units) if (!_seen.Contains(kv.Key)) _remove.Add(kv.Key);
            foreach (var id in _remove) { _units[id].Destroy(); _units.Remove(id); }

            // projectiles
            _seen.Clear();
            foreach (var p in w.Projectiles)
            {
                if (p.Dead) continue;
                _seen.Add(p.Id);
                if (!_projectiles.TryGetValue(p.Id, out var sr))
                {
                    string key = p.Visual == 0 ? "fx/arrow" : p.Visual == 2 ? "fx/rock" : p.Visual == 3 ? "fx/bolt" : "fx/orb";
                    sr = Rent(ArtLibrary.Get(key), 30000);
                    sr.color = p.Element == ElementId.None ? Color.white : ArtLibrary.ElementColor(p.Element);
                    if (p.Visual == 0 && p.Element != ElementId.None) sr.color = Color.Lerp(Color.white, ArtLibrary.ElementColor(p.Element), 0.6f);
                    float sc = p.Visual == 1 ? 0.55f : p.Visual == 3 ? 0.7f : 0.7f;
                    if (p.Team == Team.Enemy && p.Element == ElementId.None) sr.color = new Color(1f, 0.6f, 0.55f);
                    sr.transform.localScale = new Vector3(sc, sc, 1);
                    _projectiles[p.Id] = sr;
                }
                var pos = Vector2.Lerp(V(p.PrevPosition), V(p.Position), Alpha);
                sr.transform.position = new Vector3(pos.x, pos.y + 0.45f, 0);
                var dir = V(p.TargetPos) - V(p.Position);
                if (dir.sqrMagnitude > 0.0001f && p.Visual != 1) sr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                if (p.Visual == 1 && settings.VfxLevel >= 2 && Random.value < 0.25f) Vfx.Trail(pos + new Vector2(0, 0.45f), sr.color);
            }
            _remove.Clear();
            foreach (var kv in _projectiles) if (!_seen.Contains(kv.Key)) _remove.Add(kv.Key);
            foreach (var id in _remove) { Return(_projectiles[id]); _projectiles.Remove(id); }

            // zones
            _seen.Clear();
            foreach (var z in w.Zones)
            {
                _seen.Add(z.Id);
                if (!_zones.TryGetValue(z.Id, out var sr)) { sr = Rent(ArtLibrary.Get("fx/zone"), OrderGround + 5); _zones[z.Id] = sr; }
                sr.transform.position = new Vector3(z.Position.X, z.Position.Y, 0);
                float d = z.Radius * 2f / 1f;
                float pulse = 1f + Mathf.Sin(Time * 4f + z.Id) * 0.03f;
                sr.transform.localScale = new Vector3(d * pulse, d * pulse, 1);
                var c = ZoneColor(z.Kind, z.Element);
                c.a *= Mathf.Clamp01(z.Remaining / 0.5f);
                sr.color = c;
                if (settings.VfxLevel >= 1 && Random.value < 0.15f * z.Radius) Vfx.ZoneMote(new Vector2(z.Position.X, z.Position.Y), z.Radius, c);
            }
            _remove.Clear();
            foreach (var kv in _zones) if (!_seen.Contains(kv.Key)) _remove.Add(kv.Key);
            foreach (var id in _remove) { Return(_zones[id]); _zones.Remove(id); }

            // telegraphs (always drawn above cosmetic VFX)
            _seen.Clear();
            foreach (var t in w.Telegraphs)
            {
                _seen.Add(t.Id);
                if (!_telegraphs.TryGetValue(t.Id, out var tv)) { tv = new TelegraphView(this); _telegraphs[t.Id] = tv; }
                tv.Sync(t, Time, settings);
            }
            _remove.Clear();
            foreach (var kv in _telegraphs) if (!_seen.Contains(kv.Key)) _remove.Add(kv.Key);
            foreach (var id in _remove) { _telegraphs[id].Destroy(this); _telegraphs.Remove(id); }

            SyncKingdom();
            SyncHazards();
            SyncLaneMarkers(showLaneForecast);
            Vfx.Update(dt, settings);
        }

        // ------------------------------------------------------------------ map hazards (Phase IV)
        private readonly List<SpriteRenderer> _hazardTiles = new List<SpriteRenderer>();
        private int _hazardVersion = -1;
        private SpriteRenderer _mist;

        /// <summary>Flooded shallows (high tide), cracked thin ice and night mist are drawn over the baked terrain.</summary>
        private void SyncHazards()
        {
            var w = _s.W;
            var map = w.Map;
            int version = w.TopologyVersion * 2 + (w.TideHigh ? 1 : 0);
            if (version != _hazardVersion)
            {
                _hazardVersion = version;
                foreach (var sr in _hazardTiles) Return(sr);
                _hazardTiles.Clear();
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                    {
                        var t = map.Get(x, y);
                        bool flooded = t == TileType.Shallows && w.TideHigh;
                        bool cracked = t == TileType.Ice && w.CrackedIce.Contains(y * map.Width + x);
                        if (!flooded && !cracked) continue;
                        var sr = Rent(ArtLibrary.Get("terrain/water_0"), OrderTerrain + 20);
                        sr.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);
                        sr.transform.localScale = new Vector3(1.01f, 1.01f, 1f);
                        sr.color = cracked ? new Color(0.75f, 0.9f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.85f);
                        _hazardTiles.Add(sr);
                    }
            }
            if (map.Hazard == MapHazard.Mist)
            {
                if (_mist == null)
                {
                    _mist = NewSR("mist", ArtLibrary.Get("fx/pixel"), OrderNight - 1);
                    _mist.transform.localScale = new Vector3(map.Width * 8 + 400, map.Height * 8 + 400, 1);
                    _mist.transform.position = new Vector3(map.Width / 2f, map.Height / 2f, 0);
                }
                float a = w.IsNight ? 0.16f + Mathf.Sin(Time * 0.4f) * 0.03f : 0.05f;
                _mist.color = new Color(0.78f, 0.8f, 0.86f, a);
            }
        }

        private SpriteRenderer _emblem, _kingdomRing;

        /// <summary>Kingdom Evolution is visible on the castle (banner) and as a coloured ground aura.</summary>
        private void SyncKingdom()
        {
            var k = _s.KingdomEvolution;
            if (k == null) { if (_emblem != null) { _emblem.gameObject.SetActive(false); _kingdomRing.gameObject.SetActive(false); } return; }
            if (_emblem == null)
            {
                _emblem = NewSR("emblem", ArtLibrary.Get("building/emblem_" + k), 0);
                _kingdomRing = NewSR("kingdomRing", ArtLibrary.Get("fx/ring_thick"), OrderGround + 2);
            }
            _emblem.gameObject.SetActive(true);
            _kingdomRing.gameObject.SetActive(true);
            _emblem.sprite = ArtLibrary.Get("building/emblem_" + k);
            var c = _s.W.Castle;
            _emblem.transform.position = new Vector3(c.Origin.X + 0.2f, c.Origin.Y + 2.6f, 0);
            _emblem.sortingOrder = SortY(c.Origin.Y) + 5;
            _kingdomRing.transform.position = new Vector3(c.Center.X, c.Center.Y, 0);
            float pulse = 6.5f + Mathf.Sin(Time * 1.5f) * 0.15f;
            _kingdomRing.transform.localScale = new Vector3(pulse, pulse, 1);
            _kingdomRing.color = k == "fortress" ? new Color(0.75f, 0.75f, 0.85f, 0.35f) : k == "legion" ? new Color(1f, 0.45f, 0.35f, 0.35f) : new Color(0.7f, 0.5f, 1f, 0.4f);
        }

        private void SyncLaneMarkers(bool show)
        {
            var plan = _s.Plan;
            int needed = 0;
            if (show && plan != null)
                foreach (var laneId in plan.Lanes)
                {
                    var lane = _s.LaneById(laneId);
                    if (lane == null) continue;
                    var c = lane.Center;
                    var toCastle = (V(_s.W.Castle.Center) - V(c)).normalized;
                    var p = V(c) + toCastle * 1.5f;
                    if (_laneMarkers.Count <= needed) _laneMarkers.Add(NewSR("lane", ArtLibrary.Get("fx/arrowhead"), 31000));
                    var sr = _laneMarkers[needed++];
                    sr.gameObject.SetActive(true);
                    float bob = Mathf.Sin(Time * 3f) * 0.2f;
                    sr.transform.position = new Vector3(p.x + toCastle.x * bob, p.y + toCastle.y * bob, 0);
                    sr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(toCastle.y, toCastle.x) * Mathf.Rad2Deg - 90f);
                    sr.transform.localScale = new Vector3(1.2f, 1.2f, 1);
                    sr.color = new Color(0.95f, 0.3f, 0.25f, 0.9f);
                }
            for (int i = needed; i < _laneMarkers.Count; i++) _laneMarkers[i].gameObject.SetActive(false);
        }

        private static Color ZoneColor(ZoneKind k, ElementId e)
        {
            switch (k)
            {
                case ZoneKind.Mud: return new Color(0.45f, 0.33f, 0.2f, 0.45f);
                case ZoneKind.Sand: return new Color(0.9f, 0.78f, 0.45f, 0.4f);
                case ZoneKind.Gloom: return new Color(0.5f, 0.25f, 0.8f, 0.4f);
                case ZoneKind.Radiance: return new Color(1f, 0.95f, 0.6f, 0.35f);
                default: return ArtLibrary.ElementColor(e == ElementId.None ? ElementId.Fire : e, 0.38f);
            }
        }

        public int WallMask(Building b) => ArtKeys.WallMask(_s.W, b);

        public static Vector2 V(Vec2 v) => new Vector2(v.X, v.Y);

        public UnitView UnitViewOf(int id) => _units.TryGetValue(id, out var v) ? v : null;
        public BuildingView BuildingViewOf(int id) => _buildings.TryGetValue(id, out var v) ? v : null;

        // ------------------------------------------------------------------ nested views
        public sealed class UnitView
        {
            private readonly SpriteRenderer _body, _shadow, _ring, _hpBg, _hpFill, _status, _light;
            private readonly GameObject _go;
            public float Flash;
            private readonly float _baseScale;
            private Vector2 _smoothPos;
            private bool _first = true;

            public UnitView(WorldView v, Unit u)
            {
                _go = new GameObject("u" + u.Id);
                _go.transform.SetParent(v._root, false);
                _shadow = v.NewSR("shadow", ArtLibrary.Get("fx/shadow"), OrderGround + 10, _go.transform);
                _shadow.color = new Color(0, 0, 0, 0.35f);
                _ring = v.NewSR("ring", ArtLibrary.Get("fx/ring"), OrderGround + 11, _go.transform);
                _body = v.NewSR("body", ArtLibrary.Get(ArtLibrary.UnitKey(u, false)), 0, _go.transform);
                _hpBg = v.NewSR("hpbg", ArtLibrary.Get("fx/pixel"), 29000, _go.transform);
                _hpFill = v.NewSR("hp", ArtLibrary.Get("fx/pixel"), 29001, _go.transform);
                _status = v.NewSR("st", ArtLibrary.Get("icon/el_none"), 29002, _go.transform);
                _light = v.NewSR("light", ArtLibrary.Get("fx/glow"), OrderLight, _go.transform);
                _baseScale = u.Role == UnitRole.General ? 1.0f : u.IsBoss ? 1.0f : 1f;
                bool friendly = u.Team == Team.Player;
                _ring.color = u.Role == UnitRole.General ? new Color(1f, 0.85f, 0.35f, u.IsPlayerControlled ? 0.95f : 0.6f) : friendly ? new Color(0.35f, 0.75f, 1f, 0.55f) : new Color(1f, 0.3f, 0.25f, 0.6f);
                float r = Mathf.Max(0.5f, u.Radius * 2.6f);
                _ring.transform.localScale = new Vector3(r, r * 0.5f, 1);
                _shadow.transform.localScale = new Vector3(r * 0.9f, r * 0.9f, 1);
                _light.color = u.Role == UnitRole.General ? new Color(1f, 0.9f, 0.6f, 0f) : new Color(0, 0, 0, 0);
                _light.transform.localScale = new Vector3(5f, 5f, 1f);
            }

            public void Sync(Unit u, float alpha, float dt, float time, float night, GameSettings s)
            {
                var pos = Vector2.Lerp(V(u.PrevPosition), V(u.Position), alpha);
                if (_first) { _smoothPos = pos; _first = false; }
                _smoothPos = Vector2.Lerp(_smoothPos, pos, 1f - Mathf.Exp(-dt * 30f));
                bool moving = Vector2.Distance(V(u.PrevPosition), V(u.Position)) > 0.001f;
                float bob = moving ? Mathf.Abs(Mathf.Sin(time * 11f + u.Id)) * 0.07f : Mathf.Sin(time * 2.5f + u.Id) * 0.012f;
                float fly = u.IsFlying ? 0.55f + Mathf.Sin(time * 4f + u.Id) * 0.08f : 0f;
                _go.transform.position = new Vector3(_smoothPos.x, _smoothPos.y, 0);
                _body.transform.localPosition = new Vector3(0, bob + fly, 0);
                bool atk = u.AttackAnim > 0.05f;
                _body.sprite = ArtLibrary.Get(ArtLibrary.UnitKey(u, atk));
                _body.flipX = u.FacingX < 0;
                float squash = atk ? 1f + u.AttackAnim * 0.25f : 1f;
                _body.transform.localScale = new Vector3(_baseScale * squash, _baseScale / squash, 1);
                _body.sortingOrder = SortY(_smoothPos.y) + (u.IsFlying ? 2000 : 0);
                // downed / frozen / flashes
                Color c = Color.white;
                if (u.State == UnitState.Downed) c = new Color(0.5f, 0.5f, 0.6f, 0.45f);
                else if (u.Stealthed) c = new Color(0.6f, 0.5f, 0.9f, 0.35f);
                else if (u.State == UnitState.Burrowed) c = new Color(0.5f, 0.4f, 0.3f, 0.25f);
                if (u.Status.Frozen) c = Color.Lerp(c, new Color(0.6f, 0.9f, 1f), 0.6f);
                if (u.ExhaustedTimer > 0f) c = Color.Lerp(c, new Color(0.7f, 0.7f, 0.7f), 0.4f);
                if (Flash > 0f)
                {
                    Flash -= dt;
                    float f = s.ReducedFlashes ? 0.25f : 0.6f;
                    c = Color.Lerp(c, u.Team == Team.Enemy ? new Color(1f, 1f, 1f) : new Color(1f, 0.4f, 0.4f), f);
                }
                _body.color = c;
                _shadow.transform.localPosition = new Vector3(0, -0.02f, 0);
                _ring.gameObject.SetActive(u.State != UnitState.Burrowed);
                // hp bar only when damaged (or generals/bosses)
                bool showHp = (u.Hp < u.MaxHp - 0.5f || u.Role == UnitRole.General || u.IsBoss) && u.State != UnitState.Downed && !u.Stealthed;
                _hpBg.gameObject.SetActive(showHp);
                _hpFill.gameObject.SetActive(showHp);
                if (showHp)
                {
                    float w = u.IsBoss ? 1.6f : u.Role == UnitRole.General ? 1.0f : 0.7f;
                    float h = _body.sprite.bounds.size.y * _baseScale;
                    float y = Mathf.Min(h, 2.6f) + 0.1f + fly;
                    _hpBg.transform.localPosition = new Vector3(0, y, 0);
                    _hpBg.transform.localScale = new Vector3(w * 8f + 0.6f, 1.2f, 1);
                    _hpBg.color = new Color(0.1f, 0.08f, 0.14f, 0.85f);
                    float t = Mathf.Clamp01(u.HpFraction);
                    _hpFill.transform.localPosition = new Vector3(-w / 2f + w * t / 2f, y, 0);
                    _hpFill.transform.localScale = new Vector3(w * 8f * t, 0.8f, 1);
                    _hpFill.color = u.Team == Team.Player ? (u.Role == UnitRole.General ? new Color(1f, 0.82f, 0.3f) : new Color(0.45f, 0.85f, 0.4f)) : new Color(0.92f, 0.3f, 0.25f);
                    if (u.Status.ShieldHp > 0f) _hpFill.color = new Color(0.6f, 0.85f, 1f);
                }
                var aura = u.Status.DominantAura();
                bool showStatus = aura != ElementId.None && s.ElementGlyphs;
                _status.gameObject.SetActive(showStatus);
                if (showStatus)
                {
                    _status.sprite = ArtLibrary.Get(ArtLibrary.ElementIcon(aura));
                    float h = Mathf.Min(_body.sprite.bounds.size.y, 2.6f);
                    _status.transform.localPosition = new Vector3(0.45f, h + 0.25f + fly, 0);
                    _status.transform.localScale = new Vector3(0.38f, 0.38f, 1);
                }
                if (u.Role == UnitRole.General) _light.color = new Color(1f, 0.92f, 0.65f, night * 0.55f);
            }

            public void Destroy() => Object.Destroy(_go);
        }

        public sealed class BuildingView
        {
            private readonly GameObject _go;
            private readonly SpriteRenderer _sr, _hpBg, _hpFill, _el, _el2, _light, _disabled;
            public float Flash;
            private string _key;

            public BuildingView(WorldView v, Building b)
            {
                _go = new GameObject("b" + b.Id);
                _go.transform.SetParent(v._root, false);
                _sr = v.NewSR("sprite", null, 0, _go.transform);
                _hpBg = v.NewSR("hpbg", ArtLibrary.Get("fx/pixel"), 29000, _go.transform);
                _hpFill = v.NewSR("hp", ArtLibrary.Get("fx/pixel"), 29001, _go.transform);
                _el = v.NewSR("el", ArtLibrary.Get("icon/el_none"), 29002, _go.transform);
                _el2 = v.NewSR("el2", ArtLibrary.Get("icon/el_none"), 29002, _go.transform);
                _light = v.NewSR("light", ArtLibrary.Get("fx/glow"), OrderLight, _go.transform);
                _disabled = v.NewSR("off", ArtLibrary.Get("icon/el_ice"), 29003, _go.transform);
            }

            public void Sync(Building b, int mask, float night, GameSettings s)
            {
                var key = ArtLibrary.BuildingKey(b, mask);
                if (key != _key) { _sr.sprite = ArtLibrary.Get(key); _key = key; }
                _go.transform.position = new Vector3(b.Center.X, b.Center.Y, 0);
                _sr.sortingOrder = SortY(b.Origin.Y + 0.05f);
                Color c = Color.white;
                if (b.Lifetime >= 0f) c = new Color(0.85f, 0.85f, 1f, 0.85f);
                if (Flash > 0f) { Flash -= UnityEngine.Time.deltaTime; c = Color.Lerp(c, new Color(1f, 0.55f, 0.5f), s.ReducedFlashes ? 0.2f : 0.5f); }
                _sr.color = c;
                bool showHp = !b.Ruined && b.Hp < b.MaxHp - 0.5f;
                _hpBg.gameObject.SetActive(showHp);
                _hpFill.gameObject.SetActive(showHp);
                if (showHp)
                {
                    float w = b.Size * 0.7f;
                    float y = b.Size * 0.5f + (b.IsWallLike ? 0.9f : 1.2f);
                    _hpBg.transform.localPosition = new Vector3(0, y, 0);
                    _hpBg.transform.localScale = new Vector3(w * 8f + 0.6f, 1.2f, 1);
                    _hpBg.color = new Color(0.1f, 0.08f, 0.14f, 0.85f);
                    float t = b.HpFraction;
                    _hpFill.transform.localPosition = new Vector3(-w / 2f + w * t / 2f, y, 0);
                    _hpFill.transform.localScale = new Vector3(w * 8f * t, 0.8f, 1);
                    _hpFill.color = b.ShieldHp > 0f ? new Color(0.6f, 0.85f, 1f) : t < 0.35f ? new Color(1f, 0.45f, 0.2f) : new Color(0.95f, 0.85f, 0.4f);
                }
                bool imbued = !b.Ruined && (b.Primary != ElementId.None) && (b.Def.Family == BuildingFamily.ArcherTower || b.IsWallLike || b.Secondary != ElementId.None);
                _el.gameObject.SetActive(imbued);
                _el2.gameObject.SetActive(imbued && b.Secondary != ElementId.None);
                if (imbued)
                {
                    _el.sprite = ArtLibrary.Get(ArtLibrary.ElementIcon(b.Primary));
                    _el.transform.localPosition = new Vector3(-b.Size * 0.3f, -b.Size * 0.35f, 0);
                    _el.transform.localScale = new Vector3(0.35f, 0.35f, 1);
                    if (b.Secondary != ElementId.None)
                    {
                        _el2.sprite = ArtLibrary.Get(ArtLibrary.ElementIcon(b.Secondary));
                        _el2.transform.localPosition = new Vector3(-b.Size * 0.3f + 0.4f, -b.Size * 0.35f, 0);
                        _el2.transform.localScale = new Vector3(0.35f, 0.35f, 1);
                    }
                }
                _disabled.gameObject.SetActive(b.DisabledTimer > 0f);
                if (b.DisabledTimer > 0f) { _disabled.transform.localPosition = new Vector3(0, b.Size * 0.7f, 0); _disabled.transform.localScale = new Vector3(0.5f, 0.5f, 1); }
                // night lights: towers, castle, wells, shrines glow
                bool lit = !b.Ruined && (b.Def.Family == BuildingFamily.Castle || b.Def.Family == BuildingFamily.MagicTower || b.Def.Family == BuildingFamily.ManaWell ||
                                         b.Def.Family == BuildingFamily.Shrine || b.Def.Family == BuildingFamily.ArcherTower || b.Def.Family == BuildingFamily.House);
                if (lit)
                {
                    var lc = b.Def.Family == BuildingFamily.MagicTower ? ArtLibrary.ElementColor(b.Primary == ElementId.None ? ElementId.Light : b.Primary) :
                             b.Def.Family == BuildingFamily.ManaWell ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.82f, 0.5f);
                    lc.a = night * (b.Def.Family == BuildingFamily.House ? 0.35f : 0.6f);
                    _light.color = lc;
                    float sc = b.Size * 2.6f;
                    _light.transform.localScale = new Vector3(sc, sc, 1);
                }
                else _light.color = new Color(0, 0, 0, 0);
            }

            public void Destroy() => Object.Destroy(_go);
        }

        private sealed class TelegraphView
        {
            private readonly SpriteRenderer _ring, _fill;
            private readonly List<SpriteRenderer> _dots = new List<SpriteRenderer>();

            public TelegraphView(WorldView v)
            {
                _ring = v.Rent(ArtLibrary.Get("fx/ring_thick"), 31500);
                _fill = v.Rent(ArtLibrary.Get("fx/disc"), 31499);
            }

            public void Sync(Telegraph t, float time, GameSettings s)
            {
                float prog = 1f - Mathf.Clamp01(t.Remaining / Mathf.Max(0.01f, t.Total));
                var col = t.Element == ElementId.Fire ? new Color(1f, 0.55f, 0.15f) : t.Element == ElementId.Ice ? new Color(0.5f, 0.85f, 1f) : t.Element == ElementId.Shadow ? new Color(0.75f, 0.4f, 1f) : new Color(1f, 0.2f, 0.2f);
                float d = t.Radius * 2f;
                Vector2 c = t.Line ? (V(t.Position) + V(t.LineEnd)) * 0.5f : V(t.Position);
                _ring.transform.position = new Vector3(c.x, c.y, 0);
                _fill.transform.position = _ring.transform.position;
                if (t.Line)
                {
                    var dir = V(t.LineEnd) - V(t.Position);
                    _ring.transform.rotation = _fill.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                    _ring.transform.localScale = new Vector3(dir.magnitude + d, d, 1);
                    _fill.transform.localScale = new Vector3((dir.magnitude + d) * prog, d * 0.9f, 1);
                }
                else
                {
                    _ring.transform.localScale = new Vector3(d, d, 1);
                    _fill.transform.localScale = new Vector3(d * prog, d * prog, 1);
                }
                float pulse = 0.75f + 0.25f * Mathf.Sin(time * 14f);
                _ring.color = new Color(col.r, col.g, col.b, pulse);
                _fill.color = new Color(col.r, col.g, col.b, 0.32f);
            }

            public void Destroy(WorldView v)
            {
                v.Return(_ring);
                v.Return(_fill);
            }
        }
    }
}
