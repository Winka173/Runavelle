using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>
    /// Phase IV maps: five new curated maps (each with a hazard and its own faction mix / boss rotation) and hazard
    /// retrofits for Sunken Ruins (Tide) and Ember Highlands (Lava Vents). Final order: Moonstone, Stormcliff, Ember,
    /// Frosthaven, Sunken, Shadowfen, Dawnspire, Shattered Crown.
    /// </summary>
    public sealed partial class ContentDatabase
    {
        partial void AddExpansionMapsImpl()
        {
            RetrofitPhase4Maps();

            // Stormcliff: sheer cliffs west, chasm south. Lightning strikes both armies.
            var s = new MapBuilder("stormcliff", 44, 30, 4004)
                .EdgeBand(3, 4, 7, TileType.Mountain)
                .EdgeBand(2, 3, 5, TileType.Chasm, 6)
                .Castle(14, 12)
                .Blob(28, 20, 2, TileType.Forest).Blob(33, 8, 2, TileType.Mountain).Blob(22, 24, 1, TileType.Forest)
                .Scatter(8, 6, 40, 26, TileType.Dirt, 0.05f)
                .Lane("east", "Vách Đông", 1, 10, 15).Lane("north", "Đèo Bắc", 0, 26, 31)
                .Lane("west_sky", "Mây Tây (chỉ kẻ bay)", 3, 18, 22, flyerOnly: true)
                .RoadFromLane("east").RoadFromLane("north");
            Finish(s.Map, "Vách Đá Bão Tố", "Vách núi phía Tây, vực sâu phía Nam. Sét đánh định kỳ (báo trước 1.5 giây) – trúng cả quân địch lẫn quân ta.",
                "Tây (vách núi), Nam (vực)", "storm", 2, 1, MapHazard.LightningStrikes,
                new[] { "boss_gatebreaker", "boss_storm_serpent", "boss_tempest_roc" }, Faction.Marauders, Faction.WildTempest);

            // Frosthaven: glacier north, frozen sea west; a frozen lake guards the east approach.
            var f = new MapBuilder("frosthaven", 44, 30, 5005)
                .EdgeBand(0, 4, 7, TileType.Mountain)
                .EdgeBand(3, 3, 5, TileType.Water)
                .Castle(12, 12)
                .Blob(20, 6, 2, TileType.Forest).Blob(34, 22, 1, TileType.Forest)
                .Lane("east", "Mặt Hồ Đông", 1, 12, 16).Lane("south", "Lối Tuyết Nam", 2, 26, 30)
                .RoadFromLane("east").RoadFromLane("south");
            for (int y = 7; y <= 19; y++)
                for (int x = 21; x <= 31; x++)
                    if (f.Map.Get(x, y) != TileType.Mountain && (x - 26) * (x - 26) / 30f + (y - 13) * (y - 13) / 40f <= 1f) f.Map.Set(x, y, TileType.Ice);
            Finish(f.Map, "Cảng Băng Giá", "Sông băng phía Bắc, biển đóng băng phía Tây. Hồ băng mỏng phía Đông: kẻ địch bị Bỏng đứng trên băng làm băng vỡ – rơi xuống nước và chặn đường tới bình minh.",
                "Bắc (sông băng), Tây (biển)", "frost", 3, 3, MapHazard.ThinIce,
                new[] { "boss_gatebreaker", "boss_bone_colossus", "boss_frost_queen" }, Faction.Frostbound, Faction.UndeadLegion);

            // Shadowfen: misty marsh; sea to the east. Ground enemies arrive hidden.
            var m = new MapBuilder("shadowfen", 44, 30, 6006)
                .EdgeBand(1, 5, 8, TileType.Water)
                .Castle(28, 14)
                .Blob(12, 8, 2, TileType.Water).Blob(18, 22, 2, TileType.Water).Blob(8, 18, 1, TileType.Forest).Blob(22, 10, 2, TileType.Forest)
                .Scatter(4, 3, 34, 27, TileType.Dirt, 0.06f)
                .Lane("west", "Đầm Tây", 3, 12, 16).Lane("north", "Lau Sậy Bắc", 0, 16, 20).Lane("south", "Bùn Nam", 2, 22, 26)
                .RoadFromLane("west").RoadFromLane("north").RoadFromLane("south");
            Finish(m.Map, "Đầm Lầy Bóng Tối", "Biển phía Đông. Sương mù dày: kẻ địch mặt đất ẩn mình tới khi lại gần lính/tháp. Ánh Sáng và Đèn Tiên Tri làm lộ diện.",
                "Đông (biển)", "fen", 4, 5, MapHazard.Mist,
                new[] { "boss_gatebreaker", "boss_void_commander", "boss_corrupted_king" }, Faction.ShadowCourt, Faction.UndeadLegion);

            // Dawnspire: castle on a plateau; mountains north, chasm south. Towers on the plateau gain +1 range.
            var d = new MapBuilder("dawnspire", 44, 30, 7007)
                .EdgeBand(0, 4, 6, TileType.Mountain)
                .EdgeBand(2, 3, 5, TileType.Chasm)
                .Castle(21, 13)
                .Fill(15, 8, 29, 20, TileType.Plateau);
            Ring(d.Map, 14, 7, 30, 21, TileType.Mountain);
            d.Blob(8, 20, 1, TileType.Forest).Blob(36, 9, 1, TileType.Forest)
                .Lane("west", "Sườn Tây", 3, 11, 16).Lane("east", "Sườn Đông", 1, 12, 17)
                .Lane("north_sky", "Đỉnh Gió (chỉ kẻ bay)", 0, 18, 26, flyerOnly: true)
                .RoadFromLane("west").RoadFromLane("east");
            Finish(d.Map, "Tháp Bình Minh", "Lâu đài trên cao nguyên bao quanh vách đá, chỉ hai dốc lên Đông/Tây. Tháp xây trên cao nguyên +1 tầm. Núi phía Bắc, vực phía Nam; kẻ bay ồ ạt từ đỉnh gió.",
                "Bắc (núi), Nam (vực)", "plateau", 4, 6, MapHazard.HighGround,
                new[] { "boss_siege_titan", "boss_tempest_roc", "boss_hollow_emperor" }, Faction.WildTempest, Faction.SiegeConstructs);

            // Shattered Crown: ruined capital; chasm south. Old walls lie in ruins and can be rebuilt cheaply.
            var c = new MapBuilder("shattered_crown", 44, 30, 8008)
                .EdgeBand(2, 3, 6, TileType.Chasm)
                .Castle(21, 12)
                .Blob(6, 22, 2, TileType.Forest).Blob(38, 22, 2, TileType.Forest)
                .Scatter(6, 6, 38, 27, TileType.Dirt, 0.08f);
            // collapsed outer rampart (impassable rubble) with three old gateways carved by the roads
            Ring(c.Map, 12, 5, 32, 22, TileType.Mountain);
            c.Lane("west", "Cổng Tây Đổ Nát", 3, 12, 16).Lane("north", "Đại Lộ Bắc", 0, 20, 24).Lane("east", "Cổng Đông Đổ Nát", 1, 12, 16)
                .RoadFromLane("west").RoadFromLane("north").RoadFromLane("east");
            var cm = c.Map;
            // the old gates and the wall stubs beside them lie in ruins: cheap to rebuild
            for (int x = 11; x <= 33; x++)
                for (int y = 4; y <= 23; y++)
                {
                    bool nearRing = x >= 11 && x <= 13 || x >= 31 && x <= 33 || y >= 4 && y <= 6 || y >= 21 && y <= 23;
                    bool inside = x >= 12 && x <= 32 && y >= 5 && y <= 22;
                    bool onRing = inside && (x == 12 || x == 32 || y == 5 || y == 22);
                    if (onRing && nearRing && cm.Get(x, y) == TileType.Dirt) cm.RuinWalls.Add(new Int2(x, y));
                }
            Finish(cm, "Vương Miện Vỡ", "Kinh đô đổ nát, vực sâu phía Nam. Tường thành ngoài sụp thành đống đá với ba cổng cũ – các cổng và tường đổ nát dựng lại rất rẻ. Ba phe cùng tấn công.",
                "Nam (vực)", "ruins", 5, 7, MapHazard.RuinedWalls,
                new[] { "boss_siege_titan", "boss_void_commander", "boss_hollow_emperor" }, Faction.SiegeConstructs, Faction.UndeadLegion, Faction.Marauders);

            MapOrder.Sort((a, b) => Maps[a].UnlockOrder.CompareTo(Maps[b].UnlockOrder));
        }

        /// <summary>Rectangle outline of a tile type (roads carved afterwards become its gateways).</summary>
        private static void Ring(MapDefinition m, int x0, int y0, int x1, int y1, TileType t)
        {
            for (int x = x0; x <= x1; x++) { m.Set(x, y0, t); m.Set(x, y1, t); }
            for (int y = y0; y <= y1; y++) { m.Set(x0, y, t); m.Set(x1, y, t); }
        }

        private void Finish(MapDefinition map, string name, string desc, string sides, string theme, int difficulty, int order, MapHazard hazard, string[] bosses, params Faction[] factions)
        {
            map.Name = name; map.Description = desc; map.ProtectedSides = sides; map.Theme = theme;
            map.Difficulty = difficulty; map.UnlockOrder = order; map.Hazard = hazard; map.Bosses = bosses;
            map.Factions = new List<Faction>(factions);
            AddMap(map);
        }

        private void RetrofitPhase4Maps()
        {
            var moon = Maps["moonstone_valley"];
            moon.Bosses = new[] { "boss_gatebreaker", "boss_rift_warlock", "boss_siege_lord" };

            // Sunken Ruins: tidal shallows across the west and north approaches; high tide on even nights floods them.
            var sunk = Maps["sunken_ruins"];
            for (int y = 9; y <= 19; y++)
                for (int x = 17; x <= 19; x++)
                    if (MapDefinition.GroundPassable(sunk.Get(x, y))) sunk.Set(x, y, TileType.Shallows);
            for (int x = 24; x <= 31; x++)
                for (int y = 19; y <= 20; y++)
                    if (MapDefinition.GroundPassable(sunk.Get(x, y))) sunk.Set(x, y, TileType.Shallows);
            sunk.Hazard = MapHazard.Tide;
            sunk.UnlockOrder = 4;
            sunk.Difficulty = 3;
            sunk.Description = "Biển phía Đông che lưng lâu đài. Ba cửa ngõ Tây, Bắc, Nam. Thủy triều lên vào các đêm chẵn nhấn chìm bãi cạn – đường đi của địch đổi hướng.";
            sunk.Factions = new List<Faction> { Faction.Marauders, Faction.UndeadLegion };
            sunk.Bosses = new[] { "boss_gatebreaker", "boss_storm_serpent", "boss_corrupted_king" };

            // Ember Highlands: lava pools whose vents erupt (telegraphed) and burn both sides.
            var ember = Maps["ember_highlands"];
            foreach (var p in new[] { new Int2(14, 19), new Int2(15, 19), new Int2(14, 20), new Int2(29, 9), new Int2(30, 9), new Int2(30, 10), new Int2(17, 7), new Int2(18, 7) })
                if (ember.Get(p.X, p.Y) == TileType.Grass || ember.Get(p.X, p.Y) == TileType.Forest) ember.Set(p.X, p.Y, TileType.Lava);
            ember.Hazard = MapHazard.LavaVents;
            ember.Description = "Núi dựng đứng phía Bắc và Đông. Hai mặt trận Tây, Nam và một khe hẹp phía Bắc. Miệng dung nham phun trào định kỳ (báo trước 2 giây) – đốt cả hai phe.";
            ember.Factions = new List<Faction> { Faction.Marauders, Faction.Ashborn };
            ember.Bosses = new[] { "boss_siege_titan", "boss_rift_warlock", "boss_siege_lord" };
        }
    }
}
