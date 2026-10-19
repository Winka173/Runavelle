using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>
    /// Deterministic heuristic player used by the headless balance harness, soak tests and the in-game demo mode.
    /// It only uses the public GameSession API (same as a human). Strategy archetypes let the harness compare builds.
    /// </summary>
    public sealed class AutoPlayer
    {
        public enum Strategy { Balanced, Economy, Towers, Army, Elemental }

        private readonly GameSession _s;
        public Strategy Plan;
        public string SoldierPreference = "swordsman";
        public string MagicSchool = "magic_fire_1";
        public bool UseAiGenerals = true;
        private readonly Rng _rng;
        private float _ultHold;

        public AutoPlayer(GameSession s, Strategy plan, ulong seed)
        {
            _s = s;
            Plan = plan;
            _rng = Rng.Derive(seed, 99);
        }

        private World W => _s.W;

        // ----------------------------------------------------------------- day
        public void PlayDay()
        {
            if (_s.Phase != RunPhase.Day) return;
            int night = _s.Night;
            var lanes = _s.Plan.Lanes;
            int houses = W.CountFamily(BuildingFamily.House);
            int wells = W.CountFamily(BuildingFamily.ManaWell);
            int targetHouses = Plan == Strategy.Economy ? 3 + night / 2 : 2 + night / 3;
            int targetWells = Plan == Strategy.Elemental ? 2 + night / 4 : 1 + night / 5;

            // 1. economy first in the early game
            for (int i = houses; i < Math.Min(targetHouses, 7) && night <= 9; i++) if (!PlaceNear("house_1", CastleCenter, 3, 7)) break;
            for (int i = wells; i < Math.Min(targetWells, 4) && night <= 10; i++) if (!PlaceNear("well_1", CastleCenter, 3, 8)) break;

            // 2. defend each threatened front
            foreach (var laneId in lanes)
            {
                var lane = _s.LaneById(laneId);
                Vec2 front = Vec2.Lerp(CastleCenter, lane.Center, 0.33f);
                int towersNear = CountNear(front, 4.5f, BuildingFamily.ArcherTower) + CountNear(front, 4.5f, BuildingFamily.MagicTower);
                int barracksNear = CountNear(front, 5f, BuildingFamily.Barracks);
                int wantTowers = Plan == Strategy.Towers ? 2 + night / 3 : 1 + night / 3;
                int wantBarracks = Plan == Strategy.Army ? 1 + night / 5 : night >= 2 ? 1 + night / 8 : 0;
                for (int i = towersNear; i < wantTowers; i++)
                {
                    string lvl = (Plan == Strategy.Elemental || i % 2 == 1) && W.Economy_Mana(_s) >= 30 ? MagicSchool : "archer_1";
                    if (!PlaceNear(lvl, front, 1, 5)) break;
                }
                for (int i = barracksNear; i < wantBarracks; i++)
                    if (!PlaceNear("barracks_" + SoldierPreference, front + (lane.Center - CastleCenter).Normalized * 1.5f, 1, 5)) break;
                if (night >= 3) WallAcross(lane, 0.5f);
            }

            // 3. shrine mid-game
            if (night >= 5 && W.CountFamily(BuildingFamily.Shrine) == 0 && lanes.Count > 0)
                PlaceNear("shrine_1", Vec2.Lerp(CastleCenter, _s.LaneById(lanes[0]).Center, 0.25f), 1, 5);

            // 4. AI general
            if (UseAiGenerals)
                for (int slot = 0; slot < _s.AiSlotsAvailable; slot++)
                {
                    if (_s.AiSlotCommissioned[slot]) { _s.AssignAiLane(slot, _s.DefaultLaneFor(slot)); continue; }
                    var avail = _s.AvailableAiGenerals();
                    if (avail.Count > 0 && _s.Economy.Gold > 150) _s.CommissionAiGeneral(slot, avail[0]);
                }

            // 5. spend the rest on upgrades (cheapest meaningful first)
            for (int pass = 0; pass < 6; pass++) if (!UpgradeSomething()) break;
            ImbueTowers();
        }

        private Vec2 CastleCenter => W.Castle.Center;

        private int CountNear(Vec2 p, float r, BuildingFamily f)
        {
            int n = 0;
            foreach (var b in W.Buildings) if (!b.Dead && b.Def.Family == f && Vec2.Distance(b.Center, p) <= r) n++;
            return n;
        }

        private bool PlaceNear(string levelId, Vec2 p, int minR, int maxR)
        {
            var c = Int2.FromPosition(p);
            for (int r = minR; r <= maxR; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                        var o = new Int2(c.X + dx, c.Y + dy);
                        var res = _s.CanPlace(levelId, o);
                        if (!res.Ok)
                        {
                            if (res.Reason != null && res.Reason.StartsWith("Không đủ")) return false;
                            continue;
                        }
                        if (BlocksRoad(o, Content(levelId).Size)) continue;
                        return _s.Place(levelId, o) != null;
                    }
            return false;
        }

        private BuildingDefinition Content(string levelId) => _s.Content.BuildingOfLevel(levelId);

        /// <summary>Keep the castle's own approach tiles free so the bot does not seal itself with houses.</summary>
        private bool BlocksRoad(Int2 o, int size)
        {
            for (int dx = -1; dx <= size; dx++)
                for (int dy = -1; dy <= size; dy++)
                {
                    int x = o.X + dx, y = o.Y + dy;
                    if (!W.Map.InBounds(x, y)) continue;
                    if (W.Map.Get(x, y) == TileType.Dirt && dx >= 0 && dy >= 0 && dx < size && dy < size) return true;
                }
            return false;
        }

        private readonly HashSet<string> _walled = new HashSet<string>();

        /// <summary>Draws a short wall with a gate across the lane road at fraction t from the castle.</summary>
        private void WallAcross(SpawnLane lane, float t)
        {
            if (_walled.Contains(lane.Id)) return;
            Vec2 c = CastleCenter;
            Vec2 dir = (lane.Center - c).Normalized;
            Vec2 side = new Vec2(-dir.Y, dir.X);
            // slide outward until the gate tile is buildable (cliffs/ramps can sit at the preferred spot)
            Vec2 mid = c + (lane.Center - c) * t;
            for (float tt = t; tt <= t + 0.35f; tt += 0.05f)
            {
                var m = c + (lane.Center - c) * tt;
                if (_s.CanPlace("gate_wood", Int2.FromPosition(m)).Ok) { mid = m; break; }
            }
            var a = Int2.FromPosition(mid - side * 3f);
            var b = Int2.FromPosition(mid + side * 3f);
            var gate = Int2.FromPosition(mid);
            if (_s.Economy.Gold < 70) return;
            _s.Place("gate_wood", gate);
            _s.PlaceWallLine("wall_wood", a, b);
            _walled.Add(lane.Id);
        }

        private bool UpgradeSomething()
        {
            Building best = null;
            string bestLevel = null;
            float bestScore = float.MinValue;
            foreach (var b in W.Buildings)
            {
                if (b.Dead || b.Lifetime >= 0f) continue;
                if (b.Ruined)
                {
                    if (_s.Economy.CanAfford(_s.RebuildCost(b))) { _s.Rebuild(b); return true; }
                    continue;
                }
                foreach (var next in b.Level.Next)
                {
                    var r = _s.CanUpgrade(b, next);
                    if (!r.Ok) continue;
                    float score = UpgradeValue(b, _s.Content.Level(next)) / Math.Max(10f, r.Cost.Gold + r.Cost.Mana * 1.3f);
                    score += _rng.NextFloat() * 0.01f;
                    if (score > bestScore) { bestScore = score; best = b; bestLevel = next; }
                }
            }
            if (best == null || _s.Economy.Gold < 25) return false;
            return _s.Upgrade(best, bestLevel);
        }

        private float UpgradeValue(Building b, BuildingLevel next)
        {
            switch (b.Def.Family)
            {
                case BuildingFamily.House: return _s.Night <= 9 ? 6f : 1f;
                case BuildingFamily.ManaWell: return _s.Night <= 9 ? 4f : 1f;
                case BuildingFamily.ArcherTower: return Plan == Strategy.Towers ? 9f : 6f;
                case BuildingFamily.MagicTower: return Plan == Strategy.Elemental ? 9f : 6f;
                case BuildingFamily.Barracks: return Plan == Strategy.Army ? 9f : 5f;
                case BuildingFamily.Castle: return _s.Night >= 6 ? 7f : 2f;
                case BuildingFamily.Wall: return 2f;
                default: return 4f;
            }
        }

        private void ImbueTowers()
        {
            foreach (var b in W.Buildings)
            {
                if (b.Dead || b.Ruined || b.Def.Family != BuildingFamily.ArcherTower) continue;
                if (b.Primary == ElementId.None && _s.Economy.Mana >= 60) _s.Imbue(b, Plan == Strategy.Elemental ? ElementId.Lightning : ElementId.Fire, false);
                if (b.Level.SecondarySlot && b.Secondary == ElementId.None && _s.Economy.Mana >= 80) _s.Imbue(b, ElementId.Water, true);
            }
        }

        // ----------------------------------------------------------------- choices
        public void HandleChoices()
        {
            switch (_s.Phase)
            {
                case RunPhase.Report: _s.ContinueFromReport(); break;
                case RunPhase.DecreeDraft:
                    // prefer non-cursed with modifiers that match the strategy; deterministic
                    string pick = _s.DecreeOffer[0];
                    foreach (var id in _s.DecreeOffer)
                        if (_s.Content.Decrees[id].Category != DecreeCategory.Cursed) { pick = id; break; }
                    _s.ChooseDecree(pick);
                    break;
                case RunPhase.GeneralEvolution: _s.ChooseGeneralEvolution((int)(_rng.NextUInt() % 2)); break;
                case RunPhase.KingdomEvolution: _s.ChooseKingdomEvolution(_s.KingdomEvolutions[(int)(_rng.NextUInt() % (uint)_s.KingdomEvolutions.Count)].Id); break;
            }
        }

        // ----------------------------------------------------------------- night
        /// <summary>Moves the player General to the most dangerous enemy group and uses the ultimate on clusters.</summary>
        public void PlayNightTick()
        {
            var g = W.PlayerGeneral;
            if (g == null || g.State == UnitState.Downed) { _s.SetMoveInput(Vec2.Zero); return; }
            Unit focus = null;
            float best = float.MaxValue;
            foreach (var e in W.Units)
            {
                if (e.Dead || e.Team != Team.Enemy || !e.Targetable) continue;
                float d = _s.Nav.DistanceToCastle(e.Position);
                if (float.IsInfinity(d)) d = Vec2.Distance(e.Position, CastleCenter);
                if (e.IsBoss) d -= 6f;
                if (d < best) { best = d; focus = e; }
            }
            if (focus == null)
            {
                var home = CastleCenter + new Vec2(0, -2.5f);
                var v = home - g.Position;
                _s.SetMoveInput(v.Magnitude > 0.6f ? Steer(g, home) : Vec2.Zero);
                return;
            }
            Vec2 goal = g.HpFraction < 0.25f ? CastleCenter : focus.Position; // kite home when low
            Vec2 to = goal - g.Position;
            float stand = g.Attack.Range * 0.8f;
            _s.SetMoveInput(to.Magnitude > stand ? Steer(g, goal) : Vec2.Zero);
            _ultHold -= World.Dt;
            if (g.UltCooldown <= 0f && _ultHold <= 0f)
            {
                int n = 0;
                foreach (var e in W.Units) if (!e.Dead && e.Team == Team.Enemy && Vec2.Distance(e.Position, g.Position) <= g.General.Ultimate.CastRadius) n++;
                if (n >= 3 || (focus.IsBoss && Vec2.Distance(focus.Position, g.Position) < g.General.Ultimate.CastRadius)) { _s.RequestUltimate(); _ultHold = 1f; }
            }
            if (g.DashCooldown <= 0f && to.Magnitude > 8f && _path.Count == 0) _s.RequestDash();
        }

        private readonly List<Int2> _path = new List<Int2>();
        private int _pathIdx;
        private float _repath;

        /// <summary>Human-like steering: straight when the line is open, otherwise follow an A* path around buildings.</summary>
        private Vec2 Steer(Unit g, Vec2 goal)
        {
            if (_s.Move.LineWalkable(g, g.Position, goal)) { _path.Clear(); return (goal - g.Position).Normalized; }
            _repath -= World.Dt;
            if (_path.Count == 0 || _pathIdx >= _path.Count || _repath <= 0f)
            {
                _repath = 0.5f;
                _s.Nav.FindFriendlyPath(Int2.FromPosition(g.Position), Int2.FromPosition(goal), _path, 900);
                _pathIdx = 0;
            }
            while (_pathIdx < _path.Count && Vec2.Distance(_path[_pathIdx].Center, g.Position) < 0.35f) _pathIdx++;
            if (_pathIdx >= _path.Count) return (goal - g.Position).Normalized;
            return (_path[_pathIdx].Center - g.Position).Normalized;
        }

        /// <summary>Runs the whole run to completion. Returns nights survived.</summary>
        public int RunToEnd(int maxTicks = 20 * 60 * 60 * 3)
        {
            int ticks = 0;
            while (_s.Phase != RunPhase.Victory && _s.Phase != RunPhase.Defeat && ticks++ < maxTicks)
            {
                switch (_s.Phase)
                {
                    case RunPhase.Day:
                        PlayDay();
                        for (int i = 0; i < 20; i++) _s.Tick(); // let soldiers form up
                        _s.StartNight();
                        break;
                    case RunPhase.Night:
                        PlayNightTick();
                        _s.Tick();
                        break;
                    default:
                        HandleChoices();
                        break;
                }
            }
            return _s.Phase == RunPhase.Victory ? _s.TotalNights : _s.Night - 1;
        }
    }

    internal static class WorldBotExt
    {
        public static int Economy_Mana(this World w, GameSession s) => s.Economy.Mana;
    }
}
