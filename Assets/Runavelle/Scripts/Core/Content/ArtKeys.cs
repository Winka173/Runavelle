namespace Runavelle.Core
{
    /// <summary>Maps simulation actors/content to generated sprite keys (Resources/Art/&lt;key&gt;.png).</summary>
    public static class ArtKeys
    {
        /// <summary>Wall auto-join mask: 1=N, 2=E, 4=S, 8=W (neighbouring intact walls/gates).</summary>
        public static int WallMask(World w, Building b)
        {
            if (!b.IsWallLike) return 0;
            int m = 0;
            if (IsWall(w, b.Origin.X, b.Origin.Y + 1)) m |= 1;
            if (IsWall(w, b.Origin.X + 1, b.Origin.Y)) m |= 2;
            if (IsWall(w, b.Origin.X, b.Origin.Y - 1)) m |= 4;
            if (IsWall(w, b.Origin.X - 1, b.Origin.Y)) m |= 8;
            return m;
        }

        private static bool IsWall(World w, int x, int y)
        {
            var o = w.BuildingAt(x, y);
            return o != null && o.IsWallLike && !o.Ruined;
        }

        public static string Building(Building b, int wallMask)
        {
            var id = b.Level.Id;
            if (b.Ruined) return "building/ruin_" + b.Size;
            switch (b.Def.Family)
            {
                case BuildingFamily.Castle: return "building/" + id; // castle_1, castle_2, castle_3a, castle_3b
                case BuildingFamily.House: return "building/" + id;
                case BuildingFamily.ManaWell: return "building/" + id;
                case BuildingFamily.Shrine: return "building/" + id;
                case BuildingFamily.ArcherTower: return "building/" + id;
                case BuildingFamily.MagicTower:
                {
                    var parts = id.Split('_'); // magic_<el>_<lvl>
                    return "building/magic_" + parts[2] + "_" + parts[1];
                }
                case BuildingFamily.Barracks:
                {
                    string lvl = b.Level.Tier == 1 ? "1" : b.Level.Tier == 2 ? "2" : b.Level.Branch == "A" ? "legion" : "elite";
                    return "building/barracks_" + lvl;
                }
                default:
                {
                    string mat = id.EndsWith("wood") ? "wood" : id.EndsWith("stone") || id.EndsWith("iron") ? "stone" : id.EndsWith("bastion") ? "bastion" : "rune";
                    if (b.IsGate) return "building/gate_" + mat + ((wallMask & 10) != 0 || (wallMask & 5) == 0 ? "_h" : "_v");
                    return "building/wall_" + mat + "_" + wallMask;
                }
            }
        }

        public static string LevelPreview(BuildingLevel l, ContentDatabase db)
        {
            var def = db.BuildingOfLevel(l.Id);
            switch (def.Family)
            {
                case BuildingFamily.MagicTower: { var p = l.Id.Split('_'); return "building/magic_" + p[2] + "_" + p[1]; }
                case BuildingFamily.Barracks: return "building/barracks_" + (l.Tier == 1 ? "1" : l.Tier == 2 ? "2" : l.Branch == "A" ? "legion" : "elite");
                case BuildingFamily.Wall:
                {
                    string mat = l.Id.EndsWith("wood") ? "wood" : l.Id.EndsWith("stone") || l.Id.EndsWith("iron") ? "stone" : l.Id.EndsWith("bastion") ? "bastion" : "rune";
                    return l.IsGate ? "building/gate_" + mat + "_h" : "building/wall_" + mat + "_10";
                }
                default: return "building/" + l.Id;
            }
        }

        public static string Unit(Unit u, bool attackFrame)
        {
            string f = attackFrame ? "_1" : "_0";
            if (u.Role == UnitRole.General) return "general/" + u.General.Id + f;
            if (u.Team == Team.Enemy) return "enemy/" + u.Def.Id + f;
            string cls = u.Def.Class == SoldierClass.None ? u.Def.Id : u.Def.Class.ToString().ToLowerInvariant();
            if (u.Branch != null) return "unit/" + cls + "_" + u.Branch.Id.Substring(u.Branch.Id.Length - 1) + f;
            return "unit/" + cls + f;
        }

    }
}
