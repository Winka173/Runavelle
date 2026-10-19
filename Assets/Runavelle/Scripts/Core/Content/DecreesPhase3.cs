namespace Runavelle.Core
{
    /// <summary>Phase III decree batch (pool grows to ~94). Every hook used here is implemented by a system; see DecreeHookTests.</summary>
    public sealed partial class ContentDatabase
    {
        partial void AddDecreesPhase3Impl()
        {
            var E = DecreeCategory.Economy; var F = DecreeCategory.Fortress; var A = DecreeCategory.Army; var G = DecreeCategory.General; var L = DecreeCategory.Elemental; var C = DecreeCategory.Cursed;
            var c = Rarity.Common; var r = Rarity.Rare; var e = Rarity.Epic;
            int house = (int)BuildingFamily.House, well = (int)BuildingFamily.ManaWell, wall = (int)BuildingFamily.Wall, arch = (int)BuildingFamily.ArcherTower,
                magic = (int)BuildingFamily.MagicTower, barr = (int)BuildingFamily.Barracks;

            // ---------------- Economy
            D("tax_reform", "Cải Cách Thuế", E, c, "Thu Vàng +15%, thu Mana -5%.", M(Stat.GoldIncome, Scope.Any, 0.15f), M(Stat.ManaIncome, Scope.Any, -0.05f));
            D("mana_tithe", "Thuế Mana", E, c, "Thu Mana +20%.", M(Stat.ManaIncome, Scope.Any, 0.2f));
            D("frugal_architects", "Kiến Trúc Sư Tằn Tiện", E, c, "Mọi công trình rẻ hơn 12% Vàng.", M(Stat.CostGold, Scope.Structures, -0.12f));
            D("royal_architect", "Kiến Trúc Sư Hoàng Gia", E, r, "Công trình (không phải tường) đầu tiên mỗi ngày miễn phí Vàng.", H(Hook.RoyalArchitect, 1f)).MinNight = 3;
            D("mercantile_guild", "Phường Buôn", E, c, "Nhà Dân +25% Vàng.", M(Stat.GoldIncome, Scope.Houses, 0.25f)).RequireFamily = house;
            D("well_keepers", "Người Giữ Giếng", E, c, "Giếng Mana +25% Mana và +50% máu.", M(Stat.ManaIncome, Scope.Wells, 0.25f), M(Stat.MaxHp, Scope.Wells, 0.5f)).RequireFamily = well;
            D("bounty_hunters", "Thợ Săn Tiền Thưởng", E, c, "Tiền thưởng hạ gục gấp đôi.", M(Stat.Bounty, Scope.Any, 1f));
            D("elemental_overflow", "Tràn Đầy Nguyên Tố", E, r, "Cứ 6 phản ứng nguyên tố nhận 1 Mana.", H(Hook.ElementalOverflow, 6f));
            D("mana_on_kill", "Linh Hồn Kết Tinh", E, c, "Mỗi lần hạ gục có 12% cơ hội nhận 1 Mana.", H(Hook.ManaOnKill, 0.12f));
            D("spoils_of_war", "Chiến Lợi Phẩm", E, c, "Nhận ngay 25 Vàng; thưởng đêm +20%.", new Cost(25), M(Stat.NightReward, Scope.Any, 0.2f));
            // ---------------- Fortress
            D("emergency_masons", "Thợ Xây Khẩn Cấp", F, r, "Tường và cổng bị phá tự xây lại miễn phí mỗi bình minh.", H(Hook.EmergencyMasons, 1f)).RequireFamily = wall;
            D("thorn_walls", "Tường Gai", F, c, "Mọi tường/cổng phản thêm 15% sát thương cận chiến.", H(Hook.ThornWalls, 0.15f)).RequireFamily = wall;
            D("frost_walls", "Tường Sương Giá", F, r, "Kẻ đánh vào tường/cổng bị Lạnh cóng (chậm dần, đóng băng).", H(Hook.FrostWalls, 20f)).RequireFamily = wall;
            D("heavy_masonry", "Xây Đá Nặng", F, c, "Tường/cổng +20% máu, +2 giáp.", M(Stat.MaxHp, Scope.Walls, 0.2f), M(Stat.Armor, Scope.Walls, 0f, 2f)).RequireFamily = wall;
            D("tower_rebuild", "Đội Thợ Tháp", F, c, "Tháp bị phá tự xây lại lúc bình minh (tốn 50% giá xây lại).", H(Hook.TowerRebuild, 0.5f)).RequireFamily = arch;
            D("keep_ballistae", "Nỏ Thành Trì", F, c, "Lâu Đài +40% sát thương, +1 tầm.", M(Stat.Damage, Scope.Castle, 0.4f), M(Stat.Range, Scope.Castle, 0f, 1f));
            D("mana_shield_castle", "Khiên Mana Lâu Đài", F, r, "Lâu Đài nhận ít hơn 25% sát thương; tốn 10 Mana mỗi bình minh để duy trì.", H(Hook.ManaShieldCastle, 0.25f));
            D("watchtower_guild", "Hội Tháp Canh", F, c, "Tháp Cung +20% sát thương.", M(Stat.Damage, Scope.ArcherTowers, 0.2f)).RequireFamily = arch;
            D("arcane_masonry", "Nề Huyền Bí", F, c, "Tháp Phép +30% máu, +10% sát thương.", M(Stat.MaxHp, Scope.MagicTowers, 0.3f), M(Stat.Damage, Scope.MagicTowers, 0.1f)).RequireFamily = magic;
            D("siege_breakers", "Phá Công Thành", F, r, "Mọi đồng minh +35% sát thương lên xe công thành, kẻ phá cổng và boss.", H(Hook.SiegeBreakers, 0.35f));
            D("sky_watch", "Canh Trời", F, c, "Mọi đồng minh +40% sát thương lên kẻ bay.", H(Hook.SkyWatch, 0.4f)).MinNight = 4;
            D("living_fortress", "Pháo Đài Sống", F, r, "Khi tướng dùng tối thượng, tường/cổng trong 8 ô hồi 15% máu.", H(Hook.WallsMendOnUltimate, 0.15f)).RequireFamily = wall;
            // ---------------- Army
            D("storm_legion", "Quân Đoàn Bão", A, r, "Lính dùng Sét nảy thêm 1 mục tiêu.", H(Hook.StormLegion, 1f)).RequireFamily = barr;
            D("arcane_rebirth", "Tái Sinh Huyền Bí", A, r, "Lính đầu tiên tử trận mỗi đêm sống lại với 50% máu.", H(Hook.ArcaneRebirth, 1f)).RequireFamily = barr;
            D("first_strike", "Đòn Phủ Đầu", A, c, "Đòn đầu tiên lên kẻ địch còn đầy máu gây thêm 50% sát thương.", H(Hook.FirstStrike, 0.5f));
            D("bloodlust", "Khát Máu", A, c, "Lính dưới 50% máu đánh nhanh hơn 35%.", H(Hook.Bloodlust, 0.35f)).RequireFamily = barr;
            D("veteran_core", "Lõi Kỳ Cựu", A, r, "Lính sống sót qua đêm +8% máu và sát thương (tối đa 3 lần).", H(Hook.VeteranCore, 0.08f)).RequireFamily = barr;
            D("field_medics", "Quân Y Dã Chiến", A, c, "Lính không giao chiến hồi 3% máu/giây.", H(Hook.FieldMedics, 0.03f)).RequireFamily = barr;
            D("iron_discipline", "Kỷ Luật Thép", A, c, "Lính +3 giáp nhưng chạy chậm hơn 10%.", M(Stat.Armor, Scope.Soldiers, 0f, 3f), M(Stat.MoveSpeed, Scope.Soldiers, -0.1f)).RequireFamily = barr;
            D("pathfinders", "Trinh Sát", A, c, "Lính và tướng chạy nhanh hơn 20%.", M(Stat.MoveSpeed, Scope.Soldiers, 0.2f), M(Stat.MoveSpeed, Scope.Generals, 0.1f));
            D("spear_wall", "Rừng Giáo", A, c, "Lính Giáo +40% sát thương.", M(Stat.Damage, Scope.Soldiers, 0.4f, cls: SoldierClass.Spearman)).RequireClass = SoldierClass.Spearman;
            D("knightly_order", "Hiệp Sĩ Đoàn", A, c, "Hiệp Sĩ và Hiệp Sĩ Nguyên Tố +30% máu.", M(Stat.MaxHp, Scope.Soldiers, 0.3f, cls: SoldierClass.Knight), M(Stat.MaxHp, Scope.Soldiers, 0.3f, cls: SoldierClass.ElementalKnight));
            D("mage_academy", "Học Viện Pháp Thuật", A, c, "Pháp Sư và Chiến Pháp Sư +15% sát thương, +25% hiệu ứng.",
                M(Stat.Damage, Scope.Soldiers, 0.15f, cls: SoldierClass.Mage), M(Stat.StatusPower, Scope.Soldiers, 0.25f, cls: SoldierClass.Mage),
                M(Stat.Damage, Scope.Soldiers, 0.15f, cls: SoldierClass.Battlemage), M(Stat.StatusPower, Scope.Soldiers, 0.25f, cls: SoldierClass.Battlemage));
            D("cavalry_charge", "Xung Phong", A, c, "Kỵ Binh và Kỵ Sĩ Sư Ưng +30% sát thương.", M(Stat.Damage, Scope.Soldiers, 0.3f, cls: SoldierClass.Cavalry), M(Stat.Damage, Scope.Soldiers, 0.3f, cls: SoldierClass.GriffinRider));
            D("mercenary_legion", "Lính Đánh Thuê", A, r, "Bổ sung lính lúc bình minh miễn phí, nhưng thu Vàng -10%.", M(Stat.ReinforceCost, Scope.Any, -1f), M(Stat.GoldIncome, Scope.Any, -0.1f)).RequireFamily = barr;
            // ---------------- General
            D("heroic_presence_keep", "Anh Hùng Giữ Thành", G, c, "Tướng nhận ít hơn 30% sát thương khi ở trong 6 ô quanh Lâu Đài.", H(Hook.HeroicPresence, 0.3f));
            D("echo_ultimate", "Tiếng Vọng Tối Thượng", G, r, "Sau khi dùng tối thượng, thời gian hồi giảm thêm 30%.", H(Hook.EchoUltimate, 0.3f));
            D("double_ult", "Tối Thượng Kép", G, e, "Tối thượng đầu tiên mỗi đêm có thể dùng lại ngay một lần.", H(Hook.DoubleUltCharge, 1f)).MinNight = 4;
            D("lifesteal_blade", "Lưỡi Hút Máu", G, c, "Tướng hồi 10% sát thương đòn thường gây ra.", H(Hook.GeneralLifesteal, 0.1f));
            D("warlord", "Lãnh Chúa", G, c, "Tướng +25% sát thương.", M(Stat.Damage, Scope.Generals, 0.25f));
            D("iron_will", "Ý Chí Sắt", G, c, "Tướng +30% máu.", M(Stat.MaxHp, Scope.Generals, 0.3f));
            D("quick_step", "Bước Chân Nhanh", G, c, "Hồi lướt -50%.", M(Stat.DashCooldown, Scope.Generals, -0.5f));
            D("commanders_banner", "Cờ Chỉ Huy", G, r, "Hào quang +30%; tướng đồng minh +20% sát thương.", M(Stat.AuraPower, Scope.Generals, 0.3f), M(Stat.Damage, Scope.AIGenerals, 0.2f)).MinNight = 4;
            // ---------------- Elemental
            D("chain_overload", "Quá Tải Dây Chuyền", L, c, "Quá Tải (Lửa + Sét) nổ rộng hơn 50%.", H(Hook.ChainOverload, 0.5f));
            D("hydro_mastery", "Tinh Thông Nước", L, c, "Hiệu ứng Nước +40%.", M(Stat.StatusPower, Scope.Any, 0.4f, el: ElementId.Water)).RequireElement = ElementId.Water;
            D("pyro_mastery", "Tinh Thông Lửa", L, c, "Sát thương Lửa +20%.", M(Stat.Damage, Scope.Any, 0.2f, el: ElementId.Fire)).RequireElement = ElementId.Fire;
            D("storm_mastery", "Tinh Thông Sét", L, c, "Sát thương Sét +20%.", M(Stat.Damage, Scope.Any, 0.2f, el: ElementId.Lightning)).RequireElement = ElementId.Lightning;
            D("earth_mastery", "Tinh Thông Đất", L, c, "Hiệu ứng Đất +40%.", M(Stat.StatusPower, Scope.Any, 0.4f, el: ElementId.Earth)).RequireElement = ElementId.Earth;
            D("wind_mastery", "Tinh Thông Gió", L, c, "Sát thương Gió +25%.", M(Stat.Damage, Scope.Any, 0.25f, el: ElementId.Wind)).RequireElement = ElementId.Wind;
            D("frost_mastery", "Tinh Thông Băng", L, c, "Hiệu ứng Băng +40%.", M(Stat.StatusPower, Scope.Any, 0.4f, el: ElementId.Ice)).RequireElement = ElementId.Ice;
            D("catalyst_network", "Mạng Xúc Tác", L, r, "Sát thương phản ứng +20%; Tháp Phép gây hiệu ứng mạnh hơn 20%.", M(Stat.ReactionDamage, Scope.Any, 0.2f), M(Stat.StatusPower, Scope.MagicTowers, 0.2f)).RequireFamily = magic;
            // ---------------- Cursed trade-offs
            D("insomnia", "Mất Ngủ", C, r, "Mỗi đêm thêm 20% kẻ địch, nhưng thưởng đêm +50%.", H(Hook.Insomnia, 0.2f), M(Stat.NightReward, Scope.Any, 0.5f));
            D("heavy_crown", "Vương Miện Nặng", C, r, "Lâu Đài +50% máu, nhưng mọi công trình đắt hơn 15% Vàng.", M(Stat.MaxHp, Scope.Castle, 0.5f), M(Stat.CostGold, Scope.Structures, 0.15f));
            D("burden_of_crown", "Gánh Nặng Vương Quyền", C, r, "Tướng +30% sát thương nhưng nhận thêm 25% sát thương.", M(Stat.Damage, Scope.Generals, 0.3f), M(Stat.DamageTaken, Scope.Generals, 0.25f));
            D("cursed_gold", "Vàng Nguyền", C, r, "Thu Vàng +40%, nhưng mất 10 Mana mỗi bình minh.", M(Stat.GoldIncome, Scope.Any, 0.4f), H(Hook.CursedGold, 10f)).Excludes = new[] { "merchant_pact" };
            D("blood_moon", "Trăng Máu", C, e, "Kẻ địch +15% máu; tiền thưởng gấp đôi và thưởng đêm +30%.", M(Stat.MaxHp, Scope.Enemies, 0.15f), M(Stat.Bounty, Scope.Any, 1f), M(Stat.NightReward, Scope.Any, 0.3f)).MinNight = 3;
            D("reckless_army", "Quân Liều Mạng", C, c, "Lính +30% sát thương nhưng nhận thêm 30% sát thương.", M(Stat.Damage, Scope.Soldiers, 0.3f), M(Stat.DamageTaken, Scope.Soldiers, 0.3f)).RequireFamily = barr;
            Decrees["merchant_pact"].Excludes = new[] { "cursed_gold" };
        }
    }
}
