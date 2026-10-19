using System.Collections.Generic;

namespace Runavelle.Core
{
    public sealed partial class ContentDatabase
    {
        private DecreeDefinition D(string id, string name, DecreeCategory cat, Rarity rar, string desc, params object[] effects)
        {
            var d = new DecreeDefinition { Id = id, Name = name, Category = cat, Rarity = rar, Description = desc,
                Weight = rar == Rarity.Common ? 1f : rar == Rarity.Rare ? 0.6f : 0.3f };
            foreach (var e in effects)
            {
                if (e is Modifier m) d.Modifiers.Add(m);
                else if (e is HookValue h) d.Hooks.Add(h);
                else if (e is Cost c) d.Grant = c;
            }
            Decrees.Add(id, d);
            DecreeOrder.Add(id);
            return d;
        }

        private static Modifier M(Stat s, Scope sc, float pct, float flat = 0f, SoldierClass cls = SoldierClass.None, ElementId el = ElementId.None)
            => new Modifier(s, sc, pct, flat) { Class = cls, Element = el };

        private static HookValue H(Hook h, float v = 1f) => new HookValue(h, v);

        partial void AddExpansionDecreesImpl();

        private void AddDecrees()
        {
            // Economy
            D("royal_treasury", "Ngân Khố Hoàng Gia", DecreeCategory.Economy, Rarity.Common, "Nhận ngay 40 Vàng; thu Vàng +10%.", new Cost(40), M(Stat.GoldIncome, Scope.Any, 0.10f));
            D("bountiful_homes", "Nhà Nhà No Ấm", DecreeCategory.Economy, Rarity.Rare, "Nhà Dân +50% Vàng, nhưng tường -20% máu.", M(Stat.GoldIncome, Scope.Houses, 0.5f), M(Stat.MaxHp, Scope.Walls, -0.2f)).RequireFamily = (int)BuildingFamily.House;
            D("wellspring", "Mạch Nguồn", DecreeCategory.Economy, Rarity.Common, "Giếng Mana +40% Mana.", M(Stat.ManaIncome, Scope.Wells, 0.4f)).RequireFamily = (int)BuildingFamily.ManaWell;
            D("war_chest", "Rương Chiến Lợi", DecreeCategory.Economy, Rarity.Common, "Thưởng chiến thắng mỗi đêm +50%.", M(Stat.NightReward, Scope.Any, 0.5f));
            D("merchant_pact", "Hiệp Ước Thương Nhân", DecreeCategory.Economy, Rarity.Rare, "Thu Vàng +30%, thu Mana -20%.", M(Stat.GoldIncome, Scope.Any, 0.3f), M(Stat.ManaIncome, Scope.Any, -0.2f));
            D("frugal_masons", "Thợ Xây Tằn Tiện", DecreeCategory.Economy, Rarity.Common, "Tường và cổng rẻ hơn 30% Vàng.", M(Stat.CostGold, Scope.Walls, -0.3f));
            // Fortress
            D("living_ramparts", "Thành Lũy Sống", DecreeCategory.Fortress, Rarity.Rare, "Tường và cổng tự hồi 1.5% máu/giây khi không bị đánh 3 giây.", H(Hook.LivingRamparts, 0.015f)).MinNight = 2;
            D("stonework", "Nghề Đá", DecreeCategory.Fortress, Rarity.Common, "Tường và cổng +30% máu.", M(Stat.MaxHp, Scope.Walls, 0.3f));
            D("fortified_keep", "Thành Trì Bất Khuất", DecreeCategory.Fortress, Rarity.Common, "Lâu Đài +30% máu, tên của Lâu Đài +50% sát thương.", M(Stat.MaxHp, Scope.Castle, 0.3f), M(Stat.Damage, Scope.Castle, 0.5f));
            D("last_watch", "Canh Gác Cuối", DecreeCategory.Fortress, Rarity.Rare, "Tháp bắn nhanh hơn 40% khi có tường/cổng trong 5 ô bị hư hại.", H(Hook.LastWatch, 0.4f)).RequireFamily = (int)BuildingFamily.Wall;
            D("watchful_eyes", "Mắt Canh Phòng", DecreeCategory.Fortress, Rarity.Common, "Tháp +1.5 tầm.", M(Stat.Range, Scope.Towers, 0f, 1.5f));
            D("reinforced_gates", "Cổng Gia Cường", DecreeCategory.Fortress, Rarity.Common, "Cổng +60% máu và +3 giáp.", M(Stat.MaxHp, Scope.Gates, 0.6f), M(Stat.Armor, Scope.Gates, 0f, 3f)).RequireFamily = (int)BuildingFamily.Wall;
            // Army
            D("tempered_steel", "Thép Tôi", DecreeCategory.Army, Rarity.Common, "Lính +25% máu.", M(Stat.MaxHp, Scope.Soldiers, 0.25f)).RequireFamily = (int)BuildingFamily.Barracks;
            D("veteran_drill", "Huấn Luyện Kỳ Cựu", DecreeCategory.Army, Rarity.Rare, "Mỗi doanh trại +1 lính.", M(Stat.SquadSize, Scope.Barracks, 0f, 1f)).RequireFamily = (int)BuildingFamily.Barracks;
            D("tactical_retreat", "Rút Lui Chiến Thuật", DecreeCategory.Army, Rarity.Common, "Lính dưới 30% máu rút về doanh trại và hồi 6% máu/giây.", H(Hook.TacticalRetreat, 0.06f)).RequireFamily = (int)BuildingFamily.Barracks;
            D("sharpened_arrows", "Tên Mài Sắc", DecreeCategory.Army, Rarity.Common, "Cung thủ +30% sát thương.", M(Stat.Damage, Scope.Soldiers, 0.3f, cls: SoldierClass.Archer)).RequireClass = SoldierClass.Archer;
            D("war_drums", "Trống Trận", DecreeCategory.Army, Rarity.Common, "Lính đánh nhanh hơn 15%.", M(Stat.AttackSpeed, Scope.Soldiers, 0.15f)).RequireFamily = (int)BuildingFamily.Barracks;
            D("cheap_recruits", "Tân Binh Giá Rẻ", DecreeCategory.Army, Rarity.Common, "Bổ sung lính lúc bình minh rẻ hơn 60%.", M(Stat.ReinforceCost, Scope.Any, -0.6f)).RequireFamily = (int)BuildingFamily.Barracks;
            // General
            D("battle_hymn", "Khúc Ca Chiến Trận", DecreeCategory.General, Rarity.Rare, "Hào quang tướng mạnh gấp đôi khi Lâu Đài dưới 50% máu.", H(Hook.BattleHymn, 1f));
            D("swift_couriers", "Sứ Giả Thần Tốc", DecreeCategory.General, Rarity.Common, "Tướng chạy nhanh hơn 20%, hồi lướt -40%.", M(Stat.MoveSpeed, Scope.Generals, 0.2f), M(Stat.DashCooldown, Scope.Generals, -0.4f));
            D("arcane_reserve", "Kho Dự Trữ Huyền Bí", DecreeCategory.General, Rarity.Rare, "Tối thượng hồi nhanh hơn 30% và mạnh hơn 30%; thu Mana -25%.", M(Stat.UltCooldown, Scope.Generals, -0.3f), M(Stat.UltPower, Scope.Generals, 0.3f), M(Stat.ManaIncome, Scope.Any, -0.25f));
            D("heroic_presence", "Khí Chất Anh Hùng", DecreeCategory.General, Rarity.Common, "Hào quang tướng mạnh hơn 40%.", M(Stat.AuraPower, Scope.Generals, 0.4f));
            // Elemental
            D("storm_arrows", "Tên Bão", DecreeCategory.Elemental, Rarity.Common, "Tháp Cung và cung thủ gây thêm hiệu ứng Sét nhẹ.", H(Hook.StormArrows, 12f));
            D("conductive_rain", "Mưa Dẫn Điện", DecreeCategory.Elemental, Rarity.Rare, "Ướt kéo dài gấp đôi; Điện Giật nảy thêm 2 mục tiêu.", H(Hook.ConductiveRain, 2f));
            D("permafrost", "Băng Vĩnh Cửu", DecreeCategory.Elemental, Rarity.Rare, "Đóng băng lâu hơn 50%, Lạnh cóng làm chậm mạnh hơn.", H(Hook.Permafrost, 0.5f));
            D("wildfire", "Cháy Rừng", DecreeCategory.Elemental, Rarity.Common, "Bỏng +50% sát thương; Bão Lửa lan rộng hơn 50%.", H(Hook.Wildfire, 0.5f));
            D("magma_bastion", "Pháo Đài Dung Nham", DecreeCategory.Elemental, Rarity.Rare, "Vũng Dung Nham tồn tại gấp đôi và đốt gấp đôi.", H(Hook.MagmaBastion, 2f));
            D("elemental_attunement", "Hoà Hợp Nguyên Tố", DecreeCategory.Elemental, Rarity.Common, "Mọi hiệu ứng nguyên tố +30%.", M(Stat.StatusPower, Scope.Any, 0.3f));
            D("reaction_mastery", "Tinh Thông Phản Ứng", DecreeCategory.Elemental, Rarity.Rare, "Sát thương phản ứng +35%.", M(Stat.ReactionDamage, Scope.Any, 0.35f));
            // Cursed trade-offs
            D("glass_cannon", "Đại Bác Thuỷ Tinh", DecreeCategory.Cursed, Rarity.Rare, "Lính +40% sát thương nhưng -25% máu.", M(Stat.Damage, Scope.Soldiers, 0.4f), M(Stat.MaxHp, Scope.Soldiers, -0.25f)).RequireFamily = (int)BuildingFamily.Barracks;
            D("sacrificial_rampart", "Thành Lũy Hiến Tế", DecreeCategory.Cursed, Rarity.Epic, "Mọi đồng minh +20% sát thương, Lâu Đài -25% máu tối đa.", M(Stat.Damage, Scope.Any, 0.2f), M(Stat.MaxHp, Scope.Castle, -0.25f));
            AddExpansionDecreesImpl();
        }

        partial void AddEquipmentImpl();
        private void AddEquipment() => AddEquipmentImpl();
    }
}
