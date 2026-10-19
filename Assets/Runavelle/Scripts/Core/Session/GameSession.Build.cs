using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Daytime construction API: place, drag walls, upgrade, imbue, sell, rebuild, undo.</summary>
    public sealed partial class GameSession
    {
        public const int SpawnNoBuildRadius = 2;

        public Cost CostOf(BuildingLevel level)
        {
            var def = Content.BuildingOfLevel(level.Id);
            var scope = def.Family == BuildingFamily.Wall ? (level.IsGate ? Scope.Gates : Scope.Walls) : ScopeOfFamily(def.Family);
            int g = (int)Math.Round(W.Mods.Apply(level.Cost.Gold, Stat.CostGold, scope));
            int m = (int)Math.Round(W.Mods.Apply(level.Cost.Mana, Stat.CostMana, scope));
            return new Cost(g, m);
        }

        public static Scope ScopeOfFamily(BuildingFamily f)
        {
            switch (f)
            {
                case BuildingFamily.Castle: return Scope.Castle;
                case BuildingFamily.House: return Scope.Houses;
                case BuildingFamily.ManaWell: return Scope.Wells;
                case BuildingFamily.Wall: return Scope.Walls;
                case BuildingFamily.ArcherTower: return Scope.ArcherTowers;
                case BuildingFamily.MagicTower: return Scope.MagicTowers;
                case BuildingFamily.Barracks: return Scope.Barracks;
                default: return Scope.Shrines;
            }
        }

        public bool IsLevelUnlocked(BuildingLevel l) => l.RequiresUnlock == null || Setup.UnlockedGenerals.Contains(l.RequiresUnlock);

        private bool NearSpawn(int x, int y)
        {
            foreach (var lane in W.Map.Lanes)
                foreach (var t in lane.Tiles)
                    if (Math.Abs(t.X - x) <= SpawnNoBuildRadius && Math.Abs(t.Y - y) <= SpawnNoBuildRadius) return true;
            return false;
        }

        public PlaceResult CanPlace(string levelId, Int2 origin)
        {
            if (Phase != RunPhase.Day) return PlaceResult.Fail("Chỉ xây vào ban ngày");
            if (!Content.Levels.ContainsKey(levelId)) return PlaceResult.Fail("Không rõ công trình");
            var level = Content.Level(levelId);
            var def = Content.BuildingOfLevel(levelId);
            if (!def.PlayerBuildable) return PlaceResult.Fail("Không thể xây thêm");
            if (BannedFamilies.Contains(def.Family)) return PlaceResult.Fail("Bị cấm trong thử thách này");
            if (Array.IndexOf(def.RootLevels, levelId) < 0) return PlaceResult.Fail("Phải nâng cấp, không xây trực tiếp");
            if (!IsLevelUnlocked(level)) return PlaceResult.Fail("Chưa mở khoá");
            if (W.CountFamily(def.Family) >= def.MaxCount) return PlaceResult.Fail("Đã đạt số lượng tối đa (" + def.MaxCount + ")");
            if (LimitBuildingSlots > 0 && CountPlayerBuildings() >= LimitBuildingSlots && def.Family != BuildingFamily.Wall) return PlaceResult.Fail("Hết ô xây (Thăng Cấp)");
            for (int dx = 0; dx < def.Size; dx++)
                for (int dy = 0; dy < def.Size; dy++)
                {
                    int x = origin.X + dx, y = origin.Y + dy;
                    if (!W.Map.InBounds(x, y)) return PlaceResult.Fail("Ngoài bản đồ");
                    if (!MapDefinition.Buildable(W.Map.Get(x, y))) return PlaceResult.Fail("Địa hình không xây được");
                    if (W.BuildingAt(x, y) != null) return PlaceResult.Fail("Đã có công trình");
                    if (NearSpawn(x, y)) return PlaceResult.Fail("Quá gần cửa xuất quân địch");
                }
            if (def.Family != BuildingFamily.Wall && SealsGeneral(origin, def.Size)) return PlaceResult.Fail("Sẽ chặn kín lối ra của Tướng");
            var cost = CostOf(level);
            if (RoyalArchitectAvailable && def.Family != BuildingFamily.Wall) cost = new Cost(0, cost.Mana);
            if (!Economy.CanAfford(cost)) return new PlaceResult { Ok = false, Reason = "Không đủ " + (cost.Gold > Economy.Gold ? "Vàng" : "Mana"), Cost = cost };
            return new PlaceResult { Ok = true, Cost = cost };
        }

        private readonly Queue<Int2> _sealQ = new Queue<Int2>();
        private bool[] _sealSeen;

        /// <summary>
        /// True when a building at origin would cut the General's respawn point off from every ground lane
        /// (houses ringed around the castle used to trap the General for the whole night). Walls are exempt: walls + gates are a deliberate choice.
        /// </summary>
        public bool SealsGeneral(Int2 origin, int size)
        {
            var start = Int2.FromPosition(Generals_RespawnPoint());
            if (!ReachesLane(start, origin, 0)) return false; // already enclosed (e.g. by walls): not this building's fault
            return !ReachesLane(start, origin, size);
        }

        private bool ReachesLane(Int2 start, Int2 o, int size)
        {
            var m = W.Map;
            if (_sealSeen == null || _sealSeen.Length != m.Width * m.Height) _sealSeen = new bool[m.Width * m.Height];
            else Array.Clear(_sealSeen, 0, _sealSeen.Length);
            bool Blocked(int x, int y) => x >= o.X && y >= o.Y && x < o.X + size && y < o.Y + size;
            if (Blocked(start.X, start.Y)) return false;
            var goal = new HashSet<int>();
            foreach (var l in m.Lanes) if (!l.FlyerOnly) foreach (var t in l.Tiles) goal.Add(t.Y * m.Width + t.X);
            _sealQ.Clear();
            _sealQ.Enqueue(start);
            _sealSeen[start.Y * m.Width + start.X] = true;
            while (_sealQ.Count > 0)
            {
                var c = _sealQ.Dequeue();
                if (goal.Contains(c.Y * m.Width + c.X)) return true;
                for (int k = 0; k < 4; k++)
                {
                    int nx = c.X + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = c.Y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    if (!m.InBounds(nx, ny) || _sealSeen[ny * m.Width + nx] || Blocked(nx, ny)) continue;
                    _sealSeen[ny * m.Width + nx] = true;
                    if (!W.FriendlyPassable(nx, ny)) continue;
                    _sealQ.Enqueue(new Int2(nx, ny));
                }
            }
            return false;
        }

        private int _royalArchitectNight;
        public bool RoyalArchitectAvailable => W.Mods.Has(Hook.RoyalArchitect) && _royalArchitectNight != Night;

        private int CountPlayerBuildings()
        {
            int n = 0;
            foreach (var b in W.Buildings) if (!b.Dead && b.Def.PlayerBuildable && b.Def.Family != BuildingFamily.Wall && b.Lifetime < 0) n++;
            return n;
        }

        public Building Place(string levelId, Int2 origin)
        {
            var r = CanPlace(levelId, origin);
            if (!r.Ok) return null;
            if (!Economy.TrySpend(r.Cost, "build:" + levelId)) return null;
            if (RoyalArchitectAvailable && Content.BuildingOfLevel(levelId).Family != BuildingFamily.Wall) _royalArchitectNight = Night;
            var b = Spawner.CreateBuilding(levelId, origin);
            b.Invested = r.Cost;
            UndoStack.Add(b.Id);
            W.Analytics.BuildLog.Add($"N{Night} build {levelId} @{origin}");
            if (b.Def.Family == BuildingFamily.Barracks)
                for (int i = 0; i < Spawner.SquadSize(b); i++) Spawner.SpawnSoldier(b, b.Center);
            W.Emit(SimEventType.Building, b.Center, 0, b.Primary, b.Id, b.Level.Name);
            return b;
        }

        /// <summary>Tiles of an orthogonal L-shaped wall line from a to b (dominant axis first).</summary>
        public static List<Int2> WallLine(Int2 a, Int2 b)
        {
            var list = new List<Int2>();
            bool xFirst = Math.Abs(b.X - a.X) >= Math.Abs(b.Y - a.Y);
            int x = a.X, y = a.Y;
            list.Add(new Int2(x, y));
            if (xFirst)
            {
                while (x != b.X) { x += Math.Sign(b.X - x); list.Add(new Int2(x, y)); }
                while (y != b.Y) { y += Math.Sign(b.Y - y); list.Add(new Int2(x, y)); }
            }
            else
            {
                while (y != b.Y) { y += Math.Sign(b.Y - y); list.Add(new Int2(x, y)); }
                while (x != b.X) { x += Math.Sign(b.X - x); list.Add(new Int2(x, y)); }
            }
            return list;
        }

        /// <summary>Preview: which tiles of a drag are placeable and the total cost.</summary>
        public Cost PreviewWallLine(string levelId, Int2 a, Int2 b, List<Int2> valid, List<Int2> invalid)
        {
            valid?.Clear(); invalid?.Clear();
            var cost = CostOf(Content.Level(levelId));
            int n = 0;
            int gold = Economy.Gold, mana = Economy.Mana;
            foreach (var t in WallLine(a, b))
            {
                var r = CanPlaceIgnoringCost(levelId, t);
                bool ok = r && gold >= cost.Gold * (n + 1) && mana >= cost.Mana * (n + 1);
                if (ok) { n++; valid?.Add(t); } else invalid?.Add(t);
            }
            return new Cost(cost.Gold * n, cost.Mana * n);
        }

        private bool CanPlaceIgnoringCost(string levelId, Int2 t)
        {
            var r = CanPlace(levelId, t);
            return r.Ok || (r.Reason != null && r.Reason.StartsWith("Không đủ"));
        }

        public int PlaceWallLine(string levelId, Int2 a, Int2 b)
        {
            int placed = 0;
            foreach (var t in WallLine(a, b))
                if (Place(levelId, t) != null) placed++;
            return placed;
        }

        public PlaceResult CanUpgrade(Building b, string toLevelId)
        {
            if (Phase != RunPhase.Day) return PlaceResult.Fail("Chỉ nâng cấp vào ban ngày");
            if (b == null || b.Dead) return PlaceResult.Fail("Không có công trình");
            if (b.Ruined) return PlaceResult.Fail("Hãy xây lại trước");
            if (Array.IndexOf(b.Level.Next, toLevelId) < 0) return PlaceResult.Fail("Không thể nâng lên cấp này");
            var level = Content.Level(toLevelId);
            if (!IsLevelUnlocked(level)) return PlaceResult.Fail("Chưa mở khoá");
            var cost = CostOf(level);
            if (!Economy.CanAfford(cost)) return new PlaceResult { Ok = false, Reason = "Không đủ " + (cost.Gold > Economy.Gold ? "Vàng" : "Mana"), Cost = cost };
            return new PlaceResult { Ok = true, Cost = cost };
        }

        public bool Upgrade(Building b, string toLevelId)
        {
            var r = CanUpgrade(b, toLevelId);
            if (!r.Ok || !Economy.TrySpend(r.Cost, "upgrade:" + toLevelId)) return false;
            var newLevel = Content.Level(toLevelId);
            b.Level = newLevel;
            b.Invested = b.Invested + r.Cost;
            if (newLevel.Element != ElementId.None && b.Def.Family != BuildingFamily.ArcherTower && b.Def.Family != BuildingFamily.Wall) b.Primary = newLevel.Element;
            Spawner.ApplyBuildingStats(b, true);
            if (b.Def.Family == BuildingFamily.Barracks) RefreshSquad(b, true);
            W.Analytics.BuildLog.Add($"N{Night} upgrade {toLevelId}");
            W.Emit(SimEventType.Upgrade, b.Center, newLevel.Tier, b.Primary, b.Id, newLevel.Name);
            return true;
        }

        private void RefreshSquad(Building b, bool topUp)
        {
            foreach (var id in b.Squad) if (W.Get(id) is Unit s) Spawner.ApplySoldierStats(s, b, true);
            if (!topUp) return;
            int missing = Spawner.SquadSize(b) - b.Squad.Count;
            for (int i = 0; i < missing; i++) Spawner.SpawnSoldier(b, b.Center);
        }

        public PlaceResult CanImbue(Building b, ElementId e, bool secondary)
        {
            if (Phase != RunPhase.Day) return PlaceResult.Fail("Chỉ ấn nguyên tố ban ngày");
            bool wild = W.Mods.Has(Hook.WildMagic) && b != null && b.Def.Family == BuildingFamily.MagicTower && b.Level.Tier >= 2;
            bool slot2 = b != null && (b.Level.SecondarySlot || wild);
            if (b == null || b.Ruined || !b.Level.CanImbue && !(secondary && slot2)) return PlaceResult.Fail("Công trình này không ấn được");
            if (!Elements.IsValid(e) || e == ElementId.None) return PlaceResult.Fail("Nguyên tố không hợp lệ");
            if (secondary)
            {
                if (!slot2) return PlaceResult.Fail("Cần cấp 3 để mở ô phụ");
                if (b.Primary == ElementId.None) return PlaceResult.Fail("Hãy ấn nguyên tố chính trước");
                if (e == b.Primary) return PlaceResult.Fail("Ô phụ phải khác nguyên tố chính");
            }
            var cost = secondary ? ContentDatabase.ImbueSecondaryCost : ContentDatabase.ImbuePrimaryCost;
            if (!Economy.CanAfford(cost)) return new PlaceResult { Ok = false, Reason = "Không đủ " + (cost.Gold > Economy.Gold ? "Vàng" : "Mana"), Cost = cost };
            return new PlaceResult { Ok = true, Cost = cost };
        }

        public bool Imbue(Building b, ElementId e, bool secondary)
        {
            var r = CanImbue(b, e, secondary);
            if (!r.Ok || !Economy.TrySpend(r.Cost, "imbue")) return false;
            if (secondary) b.Secondary = e; else b.Primary = e;
            b.Invested = b.Invested + r.Cost;
            W.Emit(SimEventType.Upgrade, b.Center, 0, e, b.Id, "imbue");
            return true;
        }

        public Cost SellValue(Building b)
        {
            if (b == null || b.Def.Family == BuildingFamily.Castle) return Cost.Free;
            bool today = UndoStack.Contains(b.Id) && b.DayBuilt == Night;
            return today ? b.Invested : b.Invested.Scaled(b.Ruined ? 0.25f : 0.5f);
        }

        public bool Sell(Building b)
        {
            if (Phase != RunPhase.Day || b == null || b.Dead || b.Def.Family == BuildingFamily.Castle || b.Lifetime >= 0f) return false;
            var refund = SellValue(b);
            Spawner.RemoveBuilding(b);
            UndoStack.Remove(b.Id);
            Economy.Add(refund, "sell");
            W.Analytics.BuildLog.Add($"N{Night} sell {b.Level.Id}");
            W.Emit(SimEventType.Sell, b.Center, refund.Gold, ElementId.None, b.Id, b.Level.Name);
            return true;
        }

        /// <summary>Undo the most recent placement made this day (full refund).</summary>
        public bool UndoLast()
        {
            if (Phase != RunPhase.Day) return false;
            while (UndoStack.Count > 0)
            {
                int id = UndoStack[UndoStack.Count - 1];
                UndoStack.RemoveAt(UndoStack.Count - 1);
                if (W.Get(id) is Building b && !b.Dead)
                {
                    var refund = b.Invested;
                    Spawner.RemoveBuilding(b);
                    Economy.Add(refund, "undo");
                    return true;
                }
            }
            return false;
        }

        public Cost RebuildCost(Building b) => new Cost(Math.Max(1, (int)Math.Round(b.Invested.Gold * 0.4f)), (int)Math.Round(b.Invested.Mana * 0.25f));

        public bool Rebuild(Building b)
        {
            if (Phase != RunPhase.Day || b == null || !b.Ruined) return false;
            var cost = RebuildCost(b);
            if (!Economy.TrySpend(cost, "rebuild")) return false;
            b.Ruined = false;
            Spawner.ApplyBuildingStats(b, true);
            b.Hp = b.MaxHp;
            W.TopologyVersion++;
            if (b.Def.Family == BuildingFamily.Barracks) RefreshSquad(b, true);
            W.Emit(SimEventType.Building, b.Center, 0, b.Primary, b.Id, b.Level.Name);
            return true;
        }

        /// <summary>Re-applies run modifiers to every actor (after decrees / evolutions / equipment).</summary>
        public void RecomputeAllStats()
        {
            foreach (var b in W.Buildings)
            {
                if (b.Dead) continue;
                Spawner.ApplyBuildingStats(b, false);
                if (b.Def.Family == BuildingFamily.Barracks && !b.Ruined) RefreshSquad(b, Phase == RunPhase.Day || Phase == RunPhase.DecreeDraft || Phase == RunPhase.KingdomEvolution || Phase == RunPhase.GeneralEvolution || Phase == RunPhase.Report);
            }
            foreach (var g in AllGenerals()) Spawner.ApplyGeneralStats(g, false);
        }

        /// <summary>Projected income at next dawn (for the HUD).</summary>
        public Cost ProjectedIncome()
        {
            int gold = 0, mana = 0;
            foreach (var b in W.Buildings)
            {
                if (b.Dead || b.Ruined || b.Level.IncomePerDawn.IsFree) continue;
                var sc = World.ScopeOf(b);
                gold += (int)Math.Round(W.Mods.Apply(b.Level.IncomePerDawn.Gold, Stat.GoldIncome, sc));
                mana += (int)Math.Round(W.Mods.Apply(b.Level.IncomePerDawn.Mana, Stat.ManaIncome, sc));
            }
            float rewardMult = 1f + W.Mods.Percent(Stat.NightReward, Scope.Any);
            return new Cost(gold + (int)Math.Round((24 + Night * 4) * rewardMult), mana + (int)Math.Round((8 + Night) * rewardMult));
        }
    }
}
