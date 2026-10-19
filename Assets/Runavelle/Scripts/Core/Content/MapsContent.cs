namespace Runavelle.Core
{
    public sealed partial class ContentDatabase
    {
        private void AddMap(MapDefinition m)
        {
            Maps.Add(m.Id, m);
            MapOrder.Add(m.Id);
        }

        partial void AddExpansionMapsImpl();

        private void AddMaps()
        {
            // A. Moonstone Valley: mountains north, chasm west -> threats from east and south.
            var a = new MapBuilder("moonstone_valley", 44, 30, 1001)
                .EdgeBand(0, 4, 7, TileType.Mountain)
                .EdgeBand(3, 3, 6, TileType.Chasm)
                .Castle(12, 15)
                .Blob(30, 22, 2, TileType.Forest).Blob(22, 6, 2, TileType.Forest).Blob(36, 9, 1, TileType.Forest)
                .Scatter(6, 6, 40, 24, TileType.Dirt, 0.04f)
                .Lane("east", "Cửa Đông", 1, 12, 16).Lane("south", "Cửa Nam", 2, 24, 28)
                .Lane("north_sky", "Đèo Trời (chỉ kẻ bay)", 0, 18, 22, flyerOnly: true)
                .RoadFromLane("east").RoadFromLane("south");
            a.Map.Name = "Thung Lũng Đá Trăng";
            a.Map.Description = "Núi phía Bắc, vực sâu phía Tây che chở. Kẻ địch tới từ Đông và Nam; kẻ bay có thể vượt núi.";
            a.Map.ProtectedSides = "Bắc (núi), Tây (vực)";
            a.Map.Theme = "valley";
            a.Map.Difficulty = 1;
            a.Map.UnlockOrder = 0;
            AddMap(a.Map);

            // B. Sunken Ruins: ocean east; threats west, north and south.
            var b = new MapBuilder("sunken_ruins", 44, 30, 2002)
                .EdgeBand(1, 5, 9, TileType.Water)
                .Castle(28, 13)
                .Scatter(20, 4, 36, 26, TileType.Sand, 0.10f)
                .Blob(14, 22, 2, TileType.Forest).Blob(10, 8, 2, TileType.Forest).Blob(22, 16, 1, TileType.Forest)
                .Lane("west", "Cửa Tây", 3, 12, 16).Lane("north", "Cửa Bắc", 0, 20, 24).Lane("south", "Cửa Nam", 2, 18, 22)
                .RoadFromLane("west").RoadFromLane("north").RoadFromLane("south");
            for (int y = 0; y < 30; y++)
                for (int x = 30; x < 44; x++)
                    if (b.Map.Get(x, y) == TileType.Grass && x > 33) b.Map.Set(x, y, TileType.Sand);
            b.Map.Name = "Phế Tích Chìm";
            b.Map.Description = "Biển phía Đông che lưng lâu đài. Ba cửa ngõ mở ra phía Tây, Bắc và Nam.";
            b.Map.ProtectedSides = "Đông (biển)";
            b.Map.Theme = "coast";
            b.Map.Difficulty = 2;
            b.Map.UnlockOrder = 1;
            AddMap(b.Map);

            // C. Ember Highlands: steep mountains north and east with a single valley choke; two fronts + a narrow crossing.
            var c = new MapBuilder("ember_highlands", 44, 30, 3003)
                .EdgeBand(0, 5, 8, TileType.Mountain)
                .EdgeBand(1, 4, 7, TileType.Mountain)
                .Castle(22, 14)
                .Fill(30, 18, 34, 22, TileType.Mountain).Fill(8, 18, 12, 21, TileType.Mountain)
                .Blob(16, 8, 2, TileType.Forest).Blob(30, 6, 2, TileType.Forest)
                .Lane("west", "Hẻm Tây", 3, 10, 14).Lane("south", "Thung Nam", 2, 20, 24)
                .Lane("north_pass", "Khe Hẹp Bắc (mở từ đêm 7)", 0, 23, 24, minNight: 7)
                .RoadFromLane("west").RoadFromLane("south").RoadFromLane("north_pass", 1);
            c.Map.Name = "Cao Nguyên Than Hồng";
            c.Map.Description = "Núi dựng đứng phía Bắc và Đông. Hai mặt trận Tây, Nam và một khe hẹp xuyên núi phía Bắc.";
            c.Map.ProtectedSides = "Bắc, Đông (núi) – trừ một khe hẹp";
            c.Map.Theme = "highland";
            c.Map.Difficulty = 3;
            c.Map.UnlockOrder = 2;
            AddMap(c.Map);

            AddExpansionMapsImpl();
        }
    }
}
