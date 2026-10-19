using System;

namespace Runavelle.Core
{
    public sealed partial class ContentDatabase
    {
        private UnitDefinition AddUnit(UnitDefinition u)
        {
            Units.Add(u.Id, u);
            return u;
        }

        private static float[] Taken(params (ElementId e, float mult)[] entries)
        {
            var arr = new float[Elements.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = 1f;
            foreach (var (e, m) in entries) arr[(int)e] = m;
            return arr;
        }

        private void Soldier(SoldierClass c, string name, string desc, float hp, float armor, float speed, float radius, float aggro, AttackSpec atk,
            SoldierBranch a, SoldierBranch b, Action<UnitDefinition> extra = null)
        {
            var u = new UnitDefinition { Id = SoldierUnitId(c), Name = name, Description = desc, Role = UnitRole.Soldier, Class = c,
                MaxHp = hp, Armor = armor, Speed = speed, Radius = radius, AggroRange = aggro, Attack = atk };
            extra?.Invoke(u);
            AddUnit(u);
            SoldierOrder.Add(c);
            a.Id = u.Id + "_a"; a.Class = c;
            b.Id = u.Id + "_b"; b.Class = c;
            SoldierBranches.Add(a.Id, a);
            SoldierBranches.Add(b.Id, b);
        }

        private void AddUnits()
        {
            AddBaseSoldiers();
            AddExpansionSoldiers();
            AddMarauders();
            AddExpansionEnemies();
        }

        private void AddBaseSoldiers()
        {
            Soldier(SoldierClass.Swordsman, "Kiếm Sĩ", "Tiền tuyến chặn đường.", 115, 3, 2.6f, 0.28f, 4.5f,
                new AttackSpec { Damage = 11, Interval = 0.9f, Range = 0.9f },
                new SoldierBranch { Name = "Kiếm Lửa", Element = ElementId.Fire, DmgMult = 1.15f, StatusBonus = 22f, Description = "Kiếm đốt kẻ địch (Bỏng)." },
                new SoldierBranch { Name = "Tường Khiên", ArmorBonus = 3f, HpMult = 1.2f, SquadBonus = 1, Description = "+1 lính, +3 giáp, +20% máu." });
            Soldier(SoldierClass.Spearman, "Lính Giáo", "Tầm với dài, khắc chế kẻ địch to lớn.", 100, 2, 2.6f, 0.28f, 4.5f,
                new AttackSpec { Damage = 12, Interval = 1.05f, Range = 1.35f, LargeMult = 1.9f },
                new SoldierBranch { Name = "Giáo Địa Chấn", Element = ElementId.Earth, StatusBonus = 26f, StunChance = 0.1f, Description = "Giáo Đất làm Rung chấn, 10% gây choáng." },
                new SoldierBranch { Name = "Trường Thương", LargeMult = 1.25f, RangeBonus = 0.35f, DmgMult = 1.1f, Description = "Gấp ~2.4 sát thương lên kẻ to lớn, tầm xa hơn." });
            Soldier(SoldierClass.Archer, "Cung Thủ", "Yểm trợ tầm xa, bắn được kẻ bay.", 65, 0, 2.6f, 0.26f, 5f,
                new AttackSpec { Damage = 9, Interval = 1.0f, Range = 4.6f, HitsAir = true, ProjectileSpeed = 13f },
                new SoldierBranch { Name = "Xạ Thủ Bão", Element = ElementId.Lightning, ChainBonus = 1, StatusBonus = 22f, Description = "Mũi tên Sét nảy sang 1 mục tiêu." },
                new SoldierBranch { Name = "Xạ Thủ Băng", Element = ElementId.Ice, RangeBonus = 1.5f, DmgMult = 1.5f, IntervalMult = 1.35f, SlowOnHit = 0.25f, StatusBonus = 30f, Description = "Bắn tỉa Băng: tầm +1.5, sát thương lớn, làm chậm." });
            Soldier(SoldierClass.Mage, "Pháp Sư", "Sát thương vùng và gây hiệu ứng.", 58, 0, 2.4f, 0.26f, 4.6f,
                new AttackSpec { Damage = 11, Interval = 1.5f, Range = 4.0f, Splash = 1.0f, HitsAir = true, ProjectileSpeed = 8f, StatusAmount = 10f },
                new SoldierBranch { Name = "Hoả Pháp Sư", Element = ElementId.Fire, SplashBonus = 0.3f, StatusBonus = 28f, DmgMult = 1.15f, Description = "Cầu lửa: Bỏng diện rộng." },
                new SoldierBranch { Name = "Thuỷ Pháp Sư", Element = ElementId.Water, SplashBonus = 0.6f, StatusBonus = 38f, Description = "Sóng nước: làm Ướt cả đám – mồi cho Sét/Băng." });
            Soldier(SoldierClass.Knight, "Hiệp Sĩ", "Giáp nặng, giữ tuyến.", 210, 7, 2.2f, 0.32f, 4f,
                new AttackSpec { Damage = 15, Interval = 1.2f, Range = 0.95f },
                new SoldierBranch { Name = "Hiệp Sĩ Đá", Element = ElementId.Earth, ArmorBonus = 3f, StatusBonus = 26f, Description = "Đòn Đất phá giáp; +3 giáp." },
                new SoldierBranch { Name = "Hiệp Sĩ Bình Minh", Element = ElementId.Light, HpMult = 1.15f, StatusBonus = 26f, Description = "Đòn Ánh Sáng: kẻ Rạng rỡ khiến đồng minh quanh nó hồi máu." });
            Soldier(SoldierClass.Cavalry, "Kỵ Binh", "Đánh chặn nhanh, tầm truy đuổi xa nhưng có dây xích vùng.", 140, 3, 4.2f, 0.34f, 6.5f,
                new AttackSpec { Damage = 14, Interval = 1.0f, Range = 1.0f, Knockback = 0.3f },
                new SoldierBranch { Name = "Kỵ Phong", Element = ElementId.Wind, StatusBonus = 24f, SpeedMult = 1.15f, Description = "Xung phong Gió đẩy lùi kẻ địch." },
                new SoldierBranch { Name = "Kỵ Lôi", Element = ElementId.Lightning, StatusBonus = 24f, ChainBonus = 1, Description = "Xung phong Sét nảy sang mục tiêu kế bên." });
        }

        // Expansion soldiers are added in Phase I (see UnitsExpansion.cs).
        partial void AddExpansionSoldiersImpl();
        private void AddExpansionSoldiers() => AddExpansionSoldiersImpl();
        partial void AddExpansionEnemiesImpl();
        private void AddExpansionEnemies() => AddExpansionEnemiesImpl();

        private UnitDefinition Enemy(string id, string name, EnemyBehavior beh, Faction f, string desc, float hp, float armor, float speed, float radius,
            int cost, int unlock, int bounty, AttackSpec atk, params string[] tags)
        {
            return AddUnit(new UnitDefinition { Id = id, Name = name, Role = UnitRole.Enemy, Behavior = beh, Faction = f, Description = desc, MaxHp = hp, Armor = armor,
                Speed = speed, Radius = radius, ThreatCost = cost, UnlockNight = unlock, Bounty = bounty, Attack = atk, Tags = tags,
                AggroRange = atk.Range > 2f ? atk.Range : 1.6f });
        }

        private void AddMarauders()
        {
            var M = Faction.Marauders;
            Enemy("goblin", "Yêu Tinh Chạy", EnemyBehavior.Melee, M, "Bầy đàn: rẻ, nhanh, yếu.", 32, 0, 2.6f, 0.24f, 1, 1, 1,
                new AttackSpec { Damage = 5, Interval = 0.9f, Range = 0.8f }, "swarm");
            Enemy("orc_brute", "Orc Hung Bạo", EnemyBehavior.Melee, M, "Đấu sĩ hạng nặng, to lớn.", 150, 4, 1.6f, 0.36f, 4, 2, 3,
                new AttackSpec { Damage = 16, Interval = 1.3f, Range = 0.95f, StructureMult = 1.2f }, "heavy").Large = true;
            Enemy("kobold_slinger", "Kobold Ném Đá", EnemyBehavior.Ranged, M, "Quấy rối tầm xa: bắn người phòng thủ gần nhất.", 40, 0, 2.1f, 0.24f, 2, 2, 2,
                new AttackSpec { Damage = 7, Interval = 1.4f, Range = 4f, ProjectileSpeed = 10f, HitsAir = true }, "ranged");
            var ram = Enemy("ram_crew", "Đội Xe Húc", EnemyBehavior.Siege, M, "Phá tường: bỏ qua lính, gấp 3 sát thương lên công trình.", 190, 6, 1.3f, 0.4f, 5, 3, 4,
                new AttackSpec { Damage = 26, Interval = 2.0f, Range = 1.0f, StructureMult = 3f }, "siege");
            ram.Large = true; ram.AggroRange = 0f;
            Enemy("wolf_rider", "Kỵ Sói", EnemyBehavior.Melee, M, "Đột kích: rất nhanh.", 72, 1, 4.0f, 0.3f, 3, 4, 2,
                new AttackSpec { Damage = 9, Interval = 0.8f, Range = 0.9f }, "fast");
            var imp = Enemy("fire_imp", "Tiểu Quỷ Lửa", EnemyBehavior.Ranged, M, "Ném lửa gây Bỏng. Kháng Lửa, yếu Nước.", 46, 0, 2.7f, 0.24f, 2, 5, 2,
                new AttackSpec { Damage = 8, Interval = 1.3f, Range = 3.5f, ProjectileSpeed = 9f, Element = ElementId.Fire, StatusAmount = 22f, HitsAir = true }, "ranged", "elemental");
            imp.ElementTaken = Taken((ElementId.Fire, 0.4f), (ElementId.Water, 1.4f));
            Enemy("shieldbearer", "Lính Khiên Tháp", EnemyBehavior.Melee, M, "Giáp dày: vật lý gần như vô dụng. Sát thương nguyên tố bỏ qua giáp.", 125, 12, 1.7f, 0.32f, 4, 6, 3,
                new AttackSpec { Damage = 10, Interval = 1.1f, Range = 0.9f }, "armored");
            var harpy = Enemy("harpy", "Nữ Yêu Điểu", EnemyBehavior.Flyer, M, "KẺ BAY: vượt núi, vực và tường. Chỉ đòn tầm xa đánh trúng.", 55, 0, 2.8f, 0.28f, 3, 6, 2,
                new AttackSpec { Damage = 8, Interval = 1.0f, Range = 0.9f, StructureMult = 1.2f }, "flyer");
            harpy.Move = MoveKind.Flying;
            var shaman = Enemy("hex_shaman", "Pháp Sư Bùa", EnemyBehavior.Buffer, M, "Hỗ trợ: kẻ địch gần đó nhanh hơn 20% và thêm giáp. Bắn bùa Lửa.", 72, 0, 1.9f, 0.26f, 4, 7, 3,
                new AttackSpec { Damage = 9, Interval = 1.6f, Range = 3.5f, ProjectileSpeed = 8f, Element = ElementId.Fire, StatusAmount = 20f, HitsAir = true }, "caster");
            shaman.AuraRadius = 3f; shaman.AuraSpeedMult = 1.2f; shaman.AuraArmorBonus = 3f;
            var ogre = Enemy("ogre_gatesmasher", "Ogre Phá Cổng", EnemyBehavior.GateBreaker, M, "Phá cổng: ưu tiên cổng, gấp 4 sát thương lên cổng.", 270, 5, 1.5f, 0.42f, 7, 8, 5,
                new AttackSpec { Damage = 26, Interval = 1.8f, Range = 1.05f, GateMult = 4f, StructureMult = 1.5f }, "siege", "heavy");
            ogre.Large = true;
            var summ = Enemy("bone_summoner", "Pháp Sư Xương", EnemyBehavior.Summoner, M, "Triệu hồi: dựng 2 Bộ Xương mỗi 7 giây. Diệt nó trước.", 95, 0, 1.6f, 0.28f, 6, 9, 4,
                new AttackSpec { Damage = 8, Interval = 1.5f, Range = 3.5f, ProjectileSpeed = 8f, HitsAir = true }, "caster", "summoner");
            summ.SummonId = "skeleton"; summ.SummonInterval = 7f; summ.SummonCount = 2;
            var sk = Enemy("skeleton", "Bộ Xương", EnemyBehavior.Melee, M, "Lính triệu hồi.", 26, 0, 2.2f, 0.24f, 1, 99, 0,
                new AttackSpec { Damage = 5, Interval = 1.0f, Range = 0.8f }, "swarm", "minion");
            sk.ElementTaken = Taken((ElementId.Earth, 1.5f), (ElementId.Light, 1.5f));
            var champ = Enemy("dark_champion", "Hắc Vô Địch", EnemyBehavior.Elite, M, "Tinh anh: máu khổng lồ, kháng khống chế, kẻ địch gần nó thêm giáp.", 540, 8, 1.8f, 0.4f, 12, 11, 8,
                new AttackSpec { Damage = 26, Interval = 1.2f, Range = 1.05f, StructureMult = 1.3f }, "elite", "heavy");
            champ.Large = true; champ.CcResist = 0.5f; champ.AuraRadius = 3f; champ.AuraArmorBonus = 3f;

            // Boss bodies (mechanics in BossesContent.cs)
            var b1 = Enemy("boss_gatebreaker", "Gã Phá Cổng Khổng Lồ", EnemyBehavior.Boss, M, "Boss đêm 5. Tụ lực đập nát tường (xem vòng đỏ).", 1700, 8, 1.3f, 0.6f, 20, 99, 30,
                new AttackSpec { Damage = 36, Interval = 2.0f, Range = 1.3f, StructureMult = 2.2f }, "boss", "siege");
            b1.Large = true; b1.CcResist = 0.6f; b1.BossId = "gatebreaker";
            var b2 = Enemy("boss_rift_warlock", "Pháp Sư Khe Nứt", EnemyBehavior.Boss, M, "Boss đêm 10. Mở khe nứt tuôn quân, sau đó Kiệt sức (nhận +50% sát thương).", 2300, 4, 1.4f, 0.5f, 25, 99, 40,
                new AttackSpec { Damage = 22, Interval = 1.4f, Range = 4.5f, ProjectileSpeed = 9f, Element = ElementId.Shadow, StatusAmount = 25f, HitsAir = true }, "boss", "caster");
            b2.Large = true; b2.CcResist = 0.6f; b2.BossId = "rift_warlock";
            var b3 = Enemy("boss_siege_lord", "Chúa Tể Công Thành Tro Tàn", EnemyBehavior.Boss, M, "Trùm cuối đêm 15. Ba giai đoạn: hành quân, công thành (xe húc + thiên thạch), cuồng nộ.", 4600, 10, 1.2f, 0.7f, 40, 99, 60,
                new AttackSpec { Damage = 44, Interval = 1.6f, Range = 1.4f, StructureMult = 2f, Element = ElementId.Fire, StatusAmount = 20f, Splash = 1.2f }, "boss", "siege");
            b3.Large = true; b3.CcResist = 0.7f; b3.BossId = "siege_lord";
            b3.ElementTaken = Taken((ElementId.Fire, 0.6f), (ElementId.Water, 1.25f), (ElementId.Ice, 1.25f));
        }
    }
}
