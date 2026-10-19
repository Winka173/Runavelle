namespace Runavelle.Core
{
    /// <summary>Vietnamese display strings for enums (UI language = Vietnamese).</summary>
    public static class Loc
    {
        public static string Element(ElementId e) => Elements.Name(e);

        public static string Family(BuildingFamily f)
        {
            switch (f)
            {
                case BuildingFamily.Castle: return "Lâu Đài";
                case BuildingFamily.House: return "Nhà Dân";
                case BuildingFamily.ManaWell: return "Giếng Mana";
                case BuildingFamily.Wall: return "Tường & Cổng";
                case BuildingFamily.ArcherTower: return "Tháp Cung";
                case BuildingFamily.MagicTower: return "Tháp Phép";
                case BuildingFamily.Barracks: return "Doanh Trại";
                default: return "Điện Thờ Phép";
            }
        }

        public static string Soldier(SoldierClass c)
        {
            switch (c)
            {
                case SoldierClass.Swordsman: return "Kiếm Sĩ";
                case SoldierClass.Spearman: return "Lính Giáo";
                case SoldierClass.Archer: return "Cung Thủ";
                case SoldierClass.Mage: return "Pháp Sư";
                case SoldierClass.Knight: return "Hiệp Sĩ";
                case SoldierClass.Cavalry: return "Kỵ Binh";
                case SoldierClass.ShieldGuardian: return "Vệ Binh Khiên";
                case SoldierClass.Crossbowman: return "Nỏ Thủ";
                case SoldierClass.Battlemage: return "Chiến Pháp Sư";
                case SoldierClass.ElementalKnight: return "Hiệp Sĩ Nguyên Tố";
                case SoldierClass.GriffinRider: return "Kỵ Sĩ Sư Ưng";
                case SoldierClass.RuneGolem: return "Golem Cổ Tự";
                default: return "";
            }
        }

        public static string Stat(Stat s)
        {
            switch (s)
            {
                case Core.Stat.Damage: return "Sát thương";
                case Core.Stat.AttackSpeed: return "Tốc đánh";
                case Core.Stat.Range: return "Tầm";
                case Core.Stat.MaxHp: return "Máu tối đa";
                case Core.Stat.Armor: return "Giáp";
                case Core.Stat.MoveSpeed: return "Tốc chạy";
                case Core.Stat.StatusPower: return "Hiệu ứng nguyên tố";
                case Core.Stat.SquadSize: return "Quân số";
                case Core.Stat.GoldIncome: return "Thu Vàng";
                case Core.Stat.ManaIncome: return "Thu Mana";
                case Core.Stat.CostGold: return "Giá Vàng";
                case Core.Stat.CostMana: return "Giá Mana";
                case Core.Stat.ReactionDamage: return "Sát thương phản ứng";
                case Core.Stat.UltCooldown: return "Hồi chiêu tối thượng";
                case Core.Stat.UltPower: return "Sức mạnh tối thượng";
                case Core.Stat.AuraPower: return "Sức mạnh hào quang";
                case Core.Stat.DashCooldown: return "Hồi lướt";
                case Core.Stat.NightReward: return "Thưởng đêm";
                case Core.Stat.ReinforceCost: return "Giá bổ sung lính";
                case Core.Stat.Regen: return "Hồi máu";
                case Core.Stat.DamageTaken: return "Sát thương nhận";
                case Core.Stat.BossDamage: return "Sát thương lên boss";
                default: return "Tiền thưởng";
            }
        }

        public static string Scope(Scope s)
        {
            switch (s)
            {
                case Core.Scope.Soldiers: return "lính";
                case Core.Scope.Towers: return "tháp";
                case Core.Scope.ArcherTowers: return "tháp cung";
                case Core.Scope.MagicTowers: return "tháp phép";
                case Core.Scope.Walls: return "tường";
                case Core.Scope.Gates: return "cổng";
                case Core.Scope.Castle: return "lâu đài";
                case Core.Scope.PlayerGeneral: return "tướng của bạn";
                case Core.Scope.AIGenerals: return "tướng đồng minh";
                case Core.Scope.Generals: return "các tướng";
                case Core.Scope.Houses: return "nhà dân";
                case Core.Scope.Wells: return "giếng mana";
                case Core.Scope.Shrines: return "điện thờ";
                case Core.Scope.Barracks: return "doanh trại";
                case Core.Scope.Structures: return "công trình";
                case Core.Scope.Enemies: return "kẻ địch";
                case Core.Scope.Bosses: return "boss";
                default: return "tất cả";
            }
        }

        public static string Faction(Faction f)
        {
            switch (f)
            {
                case Core.Faction.Marauders: return "Đạo Tặc Hoang";
                case Core.Faction.UndeadLegion: return "Quân Đoàn Bất Tử";
                case Core.Faction.SiegeConstructs: return "Cỗ Máy Công Thành";
                case Core.Faction.Ashborn: return "Tộc Tro Tàn";
                case Core.Faction.Frostbound: return "Tộc Băng Giá";
                case Core.Faction.ShadowCourt: return "Triều Đình Bóng Tối";
                case Core.Faction.WildTempest: return "Bão Hoang Dã";
                default: return "";
            }
        }
    }
}
