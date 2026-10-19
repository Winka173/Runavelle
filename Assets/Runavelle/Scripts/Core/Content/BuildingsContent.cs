using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    public sealed partial class ContentDatabase
    {
        private BuildingDefinition AddBuilding(BuildingDefinition b)
        {
            Buildings.Add(b.Id, b);
            BuildingOrder.Add(b.Id);
            return b;
        }

        private BuildingLevel L(string buildingId, BuildingLevel l)
        {
            Levels.Add(l.Id, l);
            LevelOwner.Add(l.Id, buildingId);
            return l;
        }

        private static AttackSpec Arrow(float dmg, float interval, float range, int targets = 1) =>
            new AttackSpec { Damage = dmg, Interval = interval, Range = range, Targets = targets, HitsAir = true, ProjectileSpeed = 15f };

        private void AddBuildings()
        {
            // 1. Castle
            AddBuilding(new BuildingDefinition { Id = "castle", Family = BuildingFamily.Castle, Name = "Lâu Đài", Size = 3, PlayerBuildable = false, BreachCost = 0f, MaxCount = 1,
                Description = "Trái tim vương quốc. Mất Lâu Đài là thua. Hồi đầy máu mỗi bình minh.", RootLevels = new[] { "castle_1" } });
            L("castle", new BuildingLevel { Id = "castle_1", Name = "Thành Trì", Tier = 1, MaxHp = 1300, Armor = 3,
                Description = "Bắn tên vào kẻ địch gần đó.", Attack = Arrow(14, 1.0f, 5.5f), Next = new[] { "castle_2" } });
            L("castle", new BuildingLevel { Id = "castle_2", Name = "Thành Trì Kiên Cố", Tier = 2, Cost = new Cost(120), MaxHp = 1900, Armor = 5,
                Description = "Tường dày hơn, tên mạnh hơn.", Attack = Arrow(19, 0.9f, 6f), Next = new[] { "castle_3a", "castle_3b" } });
            L("castle", new BuildingLevel { Id = "castle_3a", Name = "Pháo Đài Hoàng Gia", Tier = 3, Branch = "A", Cost = new Cost(170, 40), MaxHp = 2800, Armor = 7, ReflectFraction = 0.15f,
                Description = "Bền nhất; bắn 2 mục tiêu; phản 15% sát thương cận chiến.", Attack = Arrow(22, 0.85f, 6.5f, 2) });
            L("castle", new BuildingLevel { Id = "castle_3b", Name = "Thành Phép Thuật", Tier = 3, Branch = "B", Cost = new Cost(100, 100), MaxHp = 2200, Armor = 5, IncomePerDawn = new Cost(0, 12),
                Description = "+12 Mana mỗi bình minh; phóng tia Sét; phản ứng quanh lâu đài mạnh hơn 30%.",
                Attack = new AttackSpec { Damage = 28, Interval = 1.0f, Range = 6.5f, HitsAir = true, ProjectileSpeed = 18f, Element = ElementId.Lightning, StatusAmount = 30f },
                Aura = new StructureAura { Radius = 6.5f, ReactionMult = 1.3f, AffectsSoldiers = true, AffectsStructures = true } });

            // 2. House
            AddBuilding(new BuildingDefinition { Id = "house", Family = BuildingFamily.House, Name = "Nhà Dân", Size = 2, BreachCost = 6f,
                Description = "Tạo Vàng mỗi bình minh.", RootLevels = new[] { "house_1" } });
            L("house", new BuildingLevel { Id = "house_1", Name = "Nhà Tranh", Tier = 1, Cost = new Cost(40), MaxHp = 160, IncomePerDawn = new Cost(12), Description = "+12 Vàng mỗi bình minh.", Next = new[] { "house_2" } });
            L("house", new BuildingLevel { Id = "house_2", Name = "Trang Viên", Tier = 2, Cost = new Cost(60), MaxHp = 230, Armor = 1, IncomePerDawn = new Cost(22), Description = "+22 Vàng mỗi bình minh.", Next = new[] { "house_3a", "house_3b" } });
            L("house", new BuildingLevel { Id = "house_3a", Name = "Dinh Thương Gia", Tier = 3, Branch = "A", Cost = new Cost(90), MaxHp = 320, Armor = 2, IncomePerDawn = new Cost(36), Description = "+36 Vàng mỗi bình minh." });
            L("house", new BuildingLevel { Id = "house_3b", Name = "Nhà Pháp Thuật", Tier = 3, Branch = "B", Cost = new Cost(70), MaxHp = 300, Armor = 2, IncomePerDawn = new Cost(20, 10), Description = "Chuyển một phần thu nhập: +20 Vàng và +10 Mana mỗi bình minh." });

            // 3. Mana Well
            AddBuilding(new BuildingDefinition { Id = "mana_well", Family = BuildingFamily.ManaWell, Name = "Giếng Mana", Size = 2, BreachCost = 6f,
                Description = "Tạo Mana mỗi bình minh.", RootLevels = new[] { "well_1" } });
            L("mana_well", new BuildingLevel { Id = "well_1", Name = "Giếng Mana", Tier = 1, Cost = new Cost(35), MaxHp = 150, IncomePerDawn = new Cost(0, 9), Description = "+9 Mana mỗi bình minh.", Next = new[] { "well_2" } });
            L("mana_well", new BuildingLevel { Id = "well_2", Name = "Suối Mana", Tier = 2, Cost = new Cost(50, 10), MaxHp = 210, IncomePerDawn = new Cost(0, 16), Description = "+16 Mana mỗi bình minh.", Next = new[] { "well_3a", "well_3b" } });
            L("mana_well", new BuildingLevel { Id = "well_3a", Name = "Giếng Sâu", Tier = 3, Branch = "A", Cost = new Cost(80, 20), MaxHp = 280, IncomePerDawn = new Cost(0, 27), Description = "+27 Mana mỗi bình minh." });
            L("mana_well", new BuildingLevel { Id = "well_3b", Name = "Giếng Cộng Hưởng", Tier = 3, Branch = "B", Cost = new Cost(60, 30), MaxHp = 280, IncomePerDawn = new Cost(0, 16),
                Description = "+16 Mana; Tháp Phép trong 5 ô đánh nhanh hơn 20% và gây hiệu ứng mạnh hơn 30%.",
                Aura = new StructureAura { Radius = 5f, AttackSpeedMult = 1.2f, StatusMult = 1.3f, OnlyMagicTowers = true, AffectsSoldiers = false } });

            // 4. Wall & Gate
            AddBuilding(new BuildingDefinition { Id = "wall", Family = BuildingFamily.Wall, Name = "Tường & Cổng", Size = 1, BreachCost = 9f, IsLinear = true,
                Description = "Chặn kẻ địch đi bộ. Kéo để xây đoạn tường. Cổng cho lính đi qua nhưng kẻ địch phải phá.", RootLevels = new[] { "wall_wood", "gate_wood" } });
            L("wall", new BuildingLevel { Id = "wall_wood", Name = "Tường Gỗ", Tier = 1, Cost = new Cost(4), MaxHp = 170, Armor = 2, Description = "Hàng rào gỗ rẻ.", Next = new[] { "wall_stone" } });
            L("wall", new BuildingLevel { Id = "wall_stone", Name = "Tường Đá", Tier = 2, Cost = new Cost(7), MaxHp = 400, Armor = 4, Description = "Tường đá chắc chắn.", Next = new[] { "wall_bastion", "wall_rune" } });
            L("wall", new BuildingLevel { Id = "wall_bastion", Name = "Tường Thành Lũy", Tier = 3, Branch = "A", Cost = new Cost(10), MaxHp = 720, Armor = 7, ReflectFraction = 0.25f, Description = "Cực bền; phản 25% sát thương cận chiến." });
            L("wall", new BuildingLevel { Id = "wall_rune", Name = "Tường Cổ Tự", Tier = 3, Branch = "B", Cost = new Cost(5, 4), MaxHp = 540, Armor = 5, RegenFraction = 0.01f, CanImbue = true,
                Description = "Tự hồi khi không bị đánh 4 giây. Có thể ấn nguyên tố: kẻ đánh vào tường bị dính nguyên tố đó." });
            L("wall", new BuildingLevel { Id = "gate_wood", Name = "Cổng Gỗ", Tier = 1, Cost = new Cost(12), MaxHp = 230, Armor = 2, IsGate = true, Description = "Lính đi qua được, kẻ địch phải phá.", Next = new[] { "gate_iron" } });
            L("wall", new BuildingLevel { Id = "gate_iron", Name = "Cổng Sắt", Tier = 2, Cost = new Cost(16), MaxHp = 500, Armor = 5, IsGate = true, Description = "Cổng gia cố.", Next = new[] { "gate_bastion", "gate_rune" } });
            L("wall", new BuildingLevel { Id = "gate_bastion", Name = "Cổng Thành Lũy", Tier = 3, Branch = "A", Cost = new Cost(20), MaxHp = 880, Armor = 8, IsGate = true, ReflectFraction = 0.25f, Description = "Cực bền; phản 25% sát thương cận chiến." });
            L("wall", new BuildingLevel { Id = "gate_rune", Name = "Cổng Cổ Tự", Tier = 3, Branch = "B", Cost = new Cost(10, 6), MaxHp = 660, Armor = 6, IsGate = true, RegenFraction = 0.01f, CanImbue = true,
                Description = "Tự hồi; có thể ấn nguyên tố lên kẻ tấn công." });

            // 5. Archer Tower
            AddBuilding(new BuildingDefinition { Id = "archer_tower", Family = BuildingFamily.ArcherTower, Name = "Tháp Cung", Size = 2, BreachCost = 8f,
                Description = "Tự động bắn, bắn được kẻ bay. Có thể ấn nguyên tố cho mũi tên (tốn Mana).", RootLevels = new[] { "archer_1" } });
            L("archer_tower", new BuildingLevel { Id = "archer_1", Name = "Tháp Cung", Tier = 1, Cost = new Cost(50), MaxHp = 260, Armor = 3, CanImbue = true,
                Description = "Tên: 11 sát thương, tầm 5.5.", Attack = Arrow(11, 0.9f, 5.5f), Next = new[] { "archer_2" } });
            L("archer_tower", new BuildingLevel { Id = "archer_2", Name = "Tháp Cung Gia Cố", Tier = 2, Cost = new Cost(60, 5), MaxHp = 330, Armor = 4, CanImbue = true,
                Description = "Tên: 15 sát thương, tầm 6.", Attack = Arrow(15, 0.85f, 6f), Next = new[] { "archer_3a", "archer_3b" } });
            L("archer_tower", new BuildingLevel { Id = "archer_3a", Name = "Tháp Bắn Tỉa", Tier = 3, Branch = "A", Cost = new Cost(95, 20), MaxHp = 380, Armor = 5, CanImbue = true, SecondarySlot = true,
                Description = "Phát bắn nặng tầm xa: 50 sát thương, xuyên 50% giáp, tầm 8. Thêm ô nguyên tố phụ.",
                Attack = new AttackSpec { Damage = 50, Interval = 2.0f, Range = 8f, ArmorPierce = 0.5f, LargeMult = 1.3f, HitsAir = true, ProjectileSpeed = 24f } });
            L("archer_tower", new BuildingLevel { Id = "archer_3b", Name = "Tháp Liên Xạ", Tier = 3, Branch = "B", Cost = new Cost(95, 20), MaxHp = 380, Armor = 5, CanImbue = true, SecondarySlot = true,
                Description = "Mưa tên: 3 mục tiêu mỗi loạt, 12 sát thương. Thêm ô nguyên tố phụ.", Attack = Arrow(12, 0.75f, 6.3f, 3) });

            // 6. Magic Tower
            var roots = new List<string>();
            foreach (var e in Elements.All) roots.Add("magic_" + Elements.Info(e).Key + "_1");
            AddBuilding(new BuildingDefinition { Id = "magic_tower", Family = BuildingFamily.MagicTower, Name = "Tháp Phép", Size = 2, BreachCost = 8f,
                Description = "Thi triển phép nguyên tố. Chọn trường phái khi xây.", RootLevels = roots.ToArray() });
            AddMagicSchool(ElementId.Fire, new AttackSpec { Damage = 14, Interval = 1.2f, Range = 5f, Splash = 1.0f, StatusAmount = 30f, HitsAir = true, ProjectileSpeed = 9f }, "Cầu lửa nổ vùng, gây Bỏng.");
            AddMagicSchool(ElementId.Water, new AttackSpec { Damage = 8, Interval = 1.0f, Range = 5f, Splash = 1.3f, StatusAmount = 40f, HitsAir = true, ProjectileSpeed = 9f }, "Sóng nước diện rộng làm Ướt – nền cho phản ứng.");
            AddMagicSchool(ElementId.Lightning, new AttackSpec { Damage = 15, Interval = 1.1f, Range = 5f, ChainCount = 2, StatusAmount = 28f, HitsAir = true }, "Tia sét nảy qua 3 mục tiêu.");
            AddMagicSchool(ElementId.Ice, new AttackSpec { Damage = 10, Interval = 1.1f, Range = 5f, Splash = 0.8f, StatusAmount = 36f, HitsAir = true, ProjectileSpeed = 9f }, "Băng làm chậm rồi đóng băng.");
            AddMagicSchool(ElementId.Wind, new AttackSpec { Damage = 10, Interval = 1.0f, Range = 5.2f, Splash = 1.0f, StatusAmount = 30f, Knockback = 0.5f, HitsAir = true, ProjectileSpeed = 12f }, "Lốc gió đẩy lùi và lan hiệu ứng.");
            AddMagicSchool(ElementId.Earth, new AttackSpec { Damage = 19, Interval = 1.3f, Range = 4.6f, Splash = 0.8f, StatusAmount = 36f, LargeMult = 1.3f, ProjectileSpeed = 8f }, "Đá tảng nặng, phá giáp. Không bắn được kẻ bay.");
            AddMagicSchool(ElementId.Light, new AttackSpec { Damage = 12, Interval = 1.0f, Range = 5.2f, StatusAmount = 32f, HitsAir = true }, "Tia sáng: Rạng rỡ khiến đồng minh gần mục tiêu hồi máu khi đánh trúng.");
            AddMagicSchool(ElementId.Shadow, new AttackSpec { Damage = 13, Interval = 1.1f, Range = 5.2f, StatusAmount = 34f, HitsAir = true, ProjectileSpeed = 11f }, "Cầu bóng tối: Ấn ký khiến mục tiêu nhận thêm sát thương, ưu tiên tinh anh.");

            // 7. Barracks (soldier type chosen on build; evolution at T2; Legion vs Elite at T3)
            var barrackRoots = new List<string>();
            foreach (var c in SoldierOrder) barrackRoots.Add("barracks_" + SoldierUnitId(c));
            AddBuilding(new BuildingDefinition { Id = "barracks", Family = BuildingFamily.Barracks, Name = "Doanh Trại", Size = 2, BreachCost = 8f,
                Description = "Huấn luyện một đội lính tự động bảo vệ quanh doanh trại. Lính tử trận trở lại lúc bình minh (tốn ít Vàng).",
                RootLevels = barrackRoots.ToArray() });
            foreach (var c in SoldierOrder) AddBarracks(c);

            // 8. Magic Shrine
            AddBuilding(new BuildingDefinition { Id = "shrine", Family = BuildingFamily.Shrine, Name = "Điện Thờ Phép", Size = 2, BreachCost = 7f, MaxCount = 4,
                Description = "Ban phước hào quang cho mọi thứ xung quanh. Không cần nối dây.", RootLevels = new[] { "shrine_1" } });
            L("shrine", new BuildingLevel { Id = "shrine_1", Name = "Điện Thờ", Tier = 1, Cost = new Cost(20, 35), MaxHp = 200, Armor = 2,
                Description = "Đồng minh trong 4.5 ô: +10% sát thương, +1 giáp.", Aura = new StructureAura { Radius = 4.5f, DamageMult = 1.1f, ArmorBonus = 1f }, Next = new[] { "shrine_2" } });
            L("shrine", new BuildingLevel { Id = "shrine_2", Name = "Đại Điện Thờ", Tier = 2, Cost = new Cost(25, 50), MaxHp = 260, Armor = 3,
                Description = "Trong 5.5 ô: +18% sát thương, +2 giáp, hồi 0.5% máu/giây.", Aura = new StructureAura { Radius = 5.5f, DamageMult = 1.18f, ArmorBonus = 2f, RegenFraction = 0.005f },
                Next = new[] { "shrine_3a", "shrine_3b" } });
            L("shrine", new BuildingLevel { Id = "shrine_3a", Name = "Thánh Đường", Tier = 3, Branch = "A", Cost = new Cost(30, 80), MaxHp = 320, Armor = 4,
                Description = "Trong 6.5 ô: hồi 2% máu/giây, giảm 15% sát thương nhận, định kỳ xoá hiệu ứng xấu.",
                Aura = new StructureAura { Radius = 6.5f, RegenFraction = 0.02f, DamageReduction = 0.15f, ArmorBonus = 2f, Cleanse = true } });
            L("shrine", new BuildingLevel { Id = "shrine_3b", Name = "Tế Đàn Chiến Tranh", Tier = 3, Branch = "B", Cost = new Cost(30, 80), MaxHp = 320, Armor = 4,
                Description = "Trong 6.5 ô: +35% sát thương, +15% tốc đánh, +40% hiệu ứng nguyên tố.",
                Aura = new StructureAura { Radius = 6.5f, DamageMult = 1.35f, AttackSpeedMult = 1.15f, StatusMult = 1.4f } });
        }

        private void AddMagicSchool(ElementId e, AttackSpec baseAttack, string flavor)
        {
            var info = Elements.Info(e);
            string k = info.Key;
            var a1 = baseAttack.Clone();
            a1.Element = e;
            L("magic_tower", new BuildingLevel { Id = $"magic_{k}_1", Name = "Tháp " + info.Name, Tier = 1, Cost = new Cost(40, 30), MaxHp = 220, Armor = 2, Element = e,
                Description = flavor, Attack = a1, Next = new[] { $"magic_{k}_2" } });
            var a2 = a1.Clone();
            a2.Damage *= 1.6f; a2.StatusAmount *= 1.3f; a2.Splash = a2.Splash > 0 ? a2.Splash + 0.3f : 0f; a2.Range += 0.5f;
            L("magic_tower", new BuildingLevel { Id = $"magic_{k}_2", Name = "Tháp " + info.Name + " Thượng Cấp", Tier = 2, Cost = new Cost(40, 45), MaxHp = 280, Armor = 3, Element = e,
                Description = flavor + " Mạnh hơn.", Attack = a2, Next = new[] { $"magic_{k}_spire", $"magic_{k}_catalyst" } });
            var aS = a2.Clone();
            aS.Damage *= 2.0f; aS.Range += 1f; aS.StatusAmount *= 1.3f;
            L("magic_tower", new BuildingLevel { Id = $"magic_{k}_spire", Name = "Tháp Nguyên Tố " + info.Name, Tier = 3, Branch = "A", Cost = new Cost(60, 70), MaxHp = 340, Armor = 4, Element = e,
                Description = $"Chuyên {info.Name}: gấp đôi sát thương, tầm +1.", Attack = aS });
            var aC = a2.Clone();
            aC.StatusAmount *= 1.2f;
            L("magic_tower", new BuildingLevel { Id = $"magic_{k}_catalyst", Name = "Tháp Xúc Tác " + info.Name, Tier = 3, Branch = "B", Cost = new Cost(60, 70), MaxHp = 340, Armor = 4, Element = e,
                SecondarySlot = true, Description = "Mở ô nguyên tố phụ; phản ứng trong 4.5 ô mạnh hơn 35%.", Attack = aC,
                Aura = new StructureAura { Radius = 4.5f, ReactionMult = 1.35f } });
        }

        private void AddBarracks(SoldierClass c)
        {
            string u = SoldierUnitId(c);
            var unit = Units[u];
            var ba = SoldierBranches[u + "_a"];
            var bb = SoldierBranches[u + "_b"];
            Cost cost = BarracksCost(c);
            int squad = BaseSquad(c);
            L("barracks", new BuildingLevel { Id = "barracks_" + u, Name = "Doanh Trại " + unit.Name, Tier = 1, Cost = cost, MaxHp = 300, Armor = 3,
                Soldier = c, SquadSize = squad, Description = $"Đội {squad} {unit.Name}. {unit.Description}", Next = new[] { $"barracks_{u}_a", $"barracks_{u}_b" } });
            foreach (var br in new[] { ba, bb })
            {
                string id = "barracks_" + br.Id;
                L("barracks", new BuildingLevel { Id = id, Name = br.Name, Tier = 2, Cost = new Cost(50, 15), MaxHp = 380, Armor = 4, Soldier = c, SoldierBranchId = br.Id,
                    SquadSize = squad + br.SquadBonus, Element = br.Element, SoldierHpMult = 1.15f, SoldierDmgMult = 1.15f, Description = br.Description,
                    Next = new[] { id + "_legion", id + "_elite" } });
                L("barracks", new BuildingLevel { Id = id + "_legion", Name = "Sảnh Quân Đoàn · " + br.Name, Tier = 3, Branch = "A", Cost = new Cost(80, 20), MaxHp = 460, Armor = 5,
                    Soldier = c, SoldierBranchId = br.Id, SquadSize = squad + br.SquadBonus + 2, Element = br.Element, SoldierHpMult = 1.25f, SoldierDmgMult = 1.2f, NightRespawnInterval = 18f,
                    Description = br.Description + " Quân Đoàn: +2 lính và bổ sung 1 lính mỗi 18 giây trong đêm." });
                L("barracks", new BuildingLevel { Id = id + "_elite", Name = "Học Viện Tinh Nhuệ · " + br.Name, Tier = 3, Branch = "B", Cost = new Cost(80, 30), MaxHp = 460, Armor = 5,
                    Soldier = c, SoldierBranchId = br.Id, SquadSize = squad + br.SquadBonus, Element = br.Element, SoldierHpMult = 1.7f, SoldierDmgMult = 1.6f, SoldierArmorBonus = 3f,
                    Description = br.Description + " Tinh Nhuệ: lính +70% máu, +60% sát thương, +3 giáp." });
            }
        }

        private static Cost BarracksCost(SoldierClass c)
        {
            switch (c)
            {
                case SoldierClass.Swordsman: return new Cost(50);
                case SoldierClass.Spearman: return new Cost(55);
                case SoldierClass.Archer: return new Cost(55);
                case SoldierClass.Mage: return new Cost(40, 20);
                case SoldierClass.Knight: return new Cost(70, 10);
                case SoldierClass.Cavalry: return new Cost(75);
                case SoldierClass.ShieldGuardian: return new Cost(60);
                case SoldierClass.Crossbowman: return new Cost(60);
                case SoldierClass.Battlemage: return new Cost(45, 30);
                case SoldierClass.ElementalKnight: return new Cost(60, 30);
                case SoldierClass.GriffinRider: return new Cost(70, 20);
                default: return new Cost(70, 25);
            }
        }

        private static int BaseSquad(SoldierClass c)
        {
            switch (c)
            {
                case SoldierClass.Mage: case SoldierClass.Knight: case SoldierClass.Cavalry: case SoldierClass.Battlemage:
                case SoldierClass.ElementalKnight: case SoldierClass.GriffinRider: return 2;
                case SoldierClass.RuneGolem: return 1;
                default: return 3;
            }
        }
    }
}
