namespace Runavelle.Core
{
    /// <summary>Phase IV: the remaining factions. Each enemy differs in behaviour (movement, targeting, ability), not just numbers.</summary>
    public sealed partial class ContentDatabase
    {
        partial void AddFactionsPhase4Impl()
        {
            // ---------------- Undead Legion: swarms, revival, curses
            var U = Faction.UndeadLegion;
            var zombie = Enemy("zombie_shambler", "Xác Sống Lê Bước", EnemyBehavior.Melee, U, "Chậm mà lì: đứng dậy một lần sau khi gục (trừ khi chết vì Lửa/Ánh Sáng).", 110, 2, 1.2f, 0.3f, 2, 2, 1,
                new AttackSpec { Damage = 9, Interval = 1.3f, Range = 0.8f }, "swarm", "undying");
            zombie.ElementTaken = Taken((ElementId.Fire, 1.4f), (ElementId.Light, 1.6f));
            var barcher = Enemy("bone_archer", "Cung Thủ Xương", EnemyBehavior.Ranged, U, "Bắn tên từ xa, ưu tiên lính.", 45, 1, 2.0f, 0.25f, 2, 3, 2,
                new AttackSpec { Damage = 8, Interval = 1.3f, Range = 4.5f, ProjectileSpeed = 12f, HitsAir = true }, "ranged");
            barcher.ElementTaken = Taken((ElementId.Earth, 1.4f), (ElementId.Light, 1.5f));
            var necro = Enemy("necromancer", "Thầy Chiêu Hồn", EnemyBehavior.Necromancer, U, "Hồi sinh kẻ địch đã chết gần đó mỗi 6 giây. Ưu tiên tiêu diệt.", 110, 1, 1.6f, 0.28f, 7, 7, 5,
                new AttackSpec { Damage = 10, Interval = 1.6f, Range = 4f, ProjectileSpeed = 9f, Element = ElementId.Shadow, StatusAmount = 20f, HitsAir = true }, "caster", "summoner");
            necro.SummonInterval = 6f; necro.SummonCount = 2;
            necro.ElementTaken = Taken((ElementId.Light, 1.6f), (ElementId.Shadow, 0.6f));
            var ghoul = Enemy("ghoul", "Ma Cà Rồng Đói", EnemyBehavior.Skirmisher, U, "Đánh rồi lùi, nhắm vào lính đơn độc.", 60, 1, 3.4f, 0.26f, 2, 4, 2,
                new AttackSpec { Damage = 11, Interval = 0.9f, Range = 0.9f }, "fast");
            var banshee = Enemy("banshee", "Nữ Thần Than Khóc", EnemyBehavior.Flyer, U, "KẺ BAY: tiếng thét Ấn ký và làm chậm người phòng thủ.", 60, 0, 2.6f, 0.28f, 4, 8, 3,
                new AttackSpec { Damage = 7, Interval = 1.4f, Range = 2.6f, ProjectileSpeed = 10f, Element = ElementId.Shadow, StatusAmount = 30f, SlowOnHit = 0.3f, HitsAir = true }, "flyer", "curse");
            banshee.Move = MoveKind.Flying;
            banshee.ElementTaken = Taken((ElementId.Light, 1.6f));
            var crypt = Enemy("crypt_knight", "Hiệp Sĩ Hầm Mộ", EnemyBehavior.Elite, U, "Tinh anh: đồng minh quanh nó nhanh hơn 15% và thêm giáp.", 420, 9, 1.7f, 0.38f, 10, 10, 7,
                new AttackSpec { Damage = 24, Interval = 1.2f, Range = 1.05f, StructureMult = 1.3f }, "elite", "heavy");
            crypt.Large = true; crypt.CcResist = 0.4f; crypt.AuraRadius = 3f; crypt.AuraArmorBonus = 2f; crypt.AuraSpeedMult = 1.15f;
            crypt.ElementTaken = Taken((ElementId.Light, 1.4f));

            // ---------------- Siege Constructs: armored golems, wall smash, engines
            var K = Faction.SiegeConstructs;
            var sgolem = Enemy("siege_golem", "Golem Công Thành", EnemyBehavior.Siege, K, "Cỗ máy phá tường giáp dày; bỏ qua lính. Yếu Sét.", 300, 12, 1.1f, 0.45f, 8, 6, 6,
                new AttackSpec { Damage = 34, Interval = 2.2f, Range = 1.1f, StructureMult = 3f }, "siege", "armored");
            sgolem.Large = true; sgolem.AggroRange = 0f;
            sgolem.ElementTaken = Taken((ElementId.Lightning, 1.5f), (ElementId.Earth, 0.6f));
            var cata = Enemy("catapult", "Máy Bắn Đá", EnemyBehavior.Artillery, K, "Pháo binh: bắn đá tầm rất xa vào công trình (không bắn được ở gần).", 160, 4, 1.0f, 0.42f, 7, 7, 5,
                new AttackSpec { Damage = 30, Interval = 3.0f, Range = 7.5f, MinRange = 2.2f, ProjectileSpeed = 8f, Splash = 1.0f, StructureMult = 2f }, "siege", "artillery");
            cata.Large = true;
            var drone = Enemy("clockwork_drone", "Bọ Máy Bay", EnemyBehavior.Flyer, K, "Bầy KẺ BAY nhỏ: nhanh, mỏng, lao vào tháp.", 28, 1, 3.4f, 0.22f, 1, 4, 1,
                new AttackSpec { Damage = 5, Interval = 0.9f, Range = 0.8f, StructureMult = 1.5f }, "flyer", "swarm");
            drone.Move = MoveKind.Flying;
            drone.ElementTaken = Taken((ElementId.Lightning, 1.6f), (ElementId.Water, 1.3f));
            var sentinel = Enemy("rune_sentinel", "Lính Gác Cổ Tự", EnemyBehavior.ShieldCarrier, K, "Khiên phép: kẻ địch quanh nó giảm 30% sát thương nhận.", 260, 8, 1.5f, 0.38f, 7, 9, 5,
                new AttackSpec { Damage = 16, Interval = 1.3f, Range = 1.0f }, "shield", "armored");
            sentinel.Large = true; sentinel.AuraRadius = 2.6f;
            sentinel.ElementTaken = Taken((ElementId.Lightning, 1.4f));
            var roller = Enemy("boulder_roller", "Đá Lăn", EnemyBehavior.Siege, K, "Lăn rất nhanh tới tường rồi đập mạnh. Máu ít.", 90, 6, 3.6f, 0.4f, 4, 7, 3,
                new AttackSpec { Damage = 60, Interval = 3.0f, Range = 1.0f, StructureMult = 2f }, "siege", "fast");
            roller.AggroRange = 0f;

            // ---------------- Ashborn: burn and explosive rush
            var A = Faction.Ashborn;
            var runner = Enemy("ember_runner", "Kẻ Lao Than Hồng", EnemyBehavior.Bomber, A, "Lao tới và TỰ NỔ (Lửa) cạnh công trình hoặc lính. Hạ nó từ xa!", 40, 0, 3.6f, 0.24f, 2, 3, 2,
                new AttackSpec { Damage = 55, Interval = 1f, Range = 0.8f, Splash = 1.6f, Element = ElementId.Fire, StatusAmount = 40f, StructureMult = 1.5f }, "bomber", "fast");
            runner.ElementTaken = Taken((ElementId.Fire, 0.5f), (ElementId.Water, 1.5f), (ElementId.Ice, 1.4f));
            var mbrute = Enemy("magma_brute", "Quỷ Dung Nham", EnemyBehavior.Melee, A, "Đấu sĩ nóng chảy: đòn đánh gây Bỏng diện rộng. Kháng Lửa, yếu Nước/Băng.", 240, 5, 1.5f, 0.4f, 6, 6, 4,
                new AttackSpec { Damage = 20, Interval = 1.3f, Range = 1.0f, Splash = 1.0f, Element = ElementId.Fire, StatusAmount = 26f }, "heavy");
            mbrute.Large = true;
            mbrute.ElementTaken = Taken((ElementId.Fire, 0.4f), (ElementId.Water, 1.5f), (ElementId.Ice, 1.4f));
            var ccaster = Enemy("cinder_caster", "Pháp Sư Tàn Tro", EnemyBehavior.Ranged, A, "Ném cầu lửa nổ vùng vào nhóm lính.", 60, 0, 2.0f, 0.26f, 3, 5, 2,
                new AttackSpec { Damage = 12, Interval = 1.8f, Range = 4.5f, ProjectileSpeed = 8f, Splash = 1.2f, Element = ElementId.Fire, StatusAmount = 30f, HitsAir = true }, "ranged", "caster");
            ccaster.ElementTaken = Taken((ElementId.Fire, 0.5f), (ElementId.Water, 1.4f));
            var burrower = Enemy("ash_burrower", "Sâu Tro Đào Hầm", EnemyBehavior.Burrower, A, "ĐỘN THỔ: đào dưới tường, trồi lên gần Lâu Đài (báo trước 1 giây).", 130, 4, 2.0f, 0.34f, 5, 7, 4,
                new AttackSpec { Damage = 18, Interval = 1.2f, Range = 1.0f, Element = ElementId.Fire, StatusAmount = 20f, StructureMult = 1.5f }, "burrow");
            burrower.Move = MoveKind.Burrow;
            var priest = Enemy("flame_priest", "Tư Tế Lửa", EnemyBehavior.Healer, A, "Hồi máu cho kẻ địch xung quanh (12/giây).", 80, 1, 1.7f, 0.27f, 5, 8, 4,
                new AttackSpec { Damage = 8, Interval = 1.6f, Range = 3.5f, ProjectileSpeed = 9f, Element = ElementId.Fire, StatusAmount = 18f, HitsAir = true }, "caster", "healer");
            priest.HealPerSecond = 12f;

            // ---------------- Frostbound: slow and freeze disruption
            var F = Faction.Frostbound;
            var fwolf = Enemy("frost_wolf", "Sói Băng", EnemyBehavior.Melee, F, "Nhanh; cắn làm chậm người phòng thủ.", 70, 1, 3.8f, 0.3f, 3, 3, 2,
                new AttackSpec { Damage = 9, Interval = 0.8f, Range = 0.9f, Element = ElementId.Ice, StatusAmount = 22f, SlowOnHit = 0.25f }, "fast");
            fwolf.ElementTaken = Taken((ElementId.Ice, 0.5f), (ElementId.Fire, 1.4f));
            var rime = Enemy("rime_caster", "Pháp Sư Sương Muối", EnemyBehavior.Ranged, F, "Bắn băng làm Lạnh cóng và đóng băng lính.", 60, 0, 1.9f, 0.26f, 3, 5, 2,
                new AttackSpec { Damage = 10, Interval = 1.5f, Range = 4.2f, ProjectileSpeed = 9f, Element = ElementId.Ice, StatusAmount = 40f, HitsAir = true }, "ranged", "caster");
            rime.ElementTaken = Taken((ElementId.Ice, 0.5f), (ElementId.Fire, 1.5f));
            var ggolem = Enemy("glacier_golem", "Golem Băng Hà", EnemyBehavior.Melee, F, "Khổng lồ bọc băng: giáp rất dày, làm chậm mọi thứ đánh nó (gai băng).", 380, 10, 1.2f, 0.45f, 8, 8, 6,
                new AttackSpec { Damage = 26, Interval = 1.6f, Range = 1.1f, Element = ElementId.Ice, StatusAmount = 30f, Knockback = 0.5f, StructureMult = 1.5f }, "heavy", "armored", "frost_armor");
            ggolem.Large = true; ggolem.CcResist = 0.3f;
            ggolem.ElementTaken = Taken((ElementId.Ice, 0.4f), (ElementId.Fire, 1.5f), (ElementId.Earth, 1.2f));
            var yeti = Enemy("yeti", "Người Tuyết", EnemyBehavior.Melee, F, "Đấm hất văng lính ra xa.", 200, 4, 1.8f, 0.4f, 5, 6, 4,
                new AttackSpec { Damage = 22, Interval = 1.4f, Range = 1.0f, Knockback = 1.2f, Splash = 0.8f }, "heavy");
            yeti.Large = true;
            yeti.ElementTaken = Taken((ElementId.Fire, 1.3f), (ElementId.Ice, 0.6f));
            var fwraith = Enemy("frost_wraith", "U Hồn Băng", EnemyBehavior.Flyer, F, "KẺ BAY: phun sương giá làm tê liệt tháp trong chốc lát.", 80, 1, 2.4f, 0.28f, 4, 9, 3,
                new AttackSpec { Damage = 8, Interval = 1.2f, Range = 2.5f, ProjectileSpeed = 10f, Element = ElementId.Ice, StatusAmount = 50f, StructureMult = 1.5f, HitsAir = true }, "flyer", "tower_freeze");
            fwraith.Move = MoveKind.Flying;
            fwraith.ElementTaken = Taken((ElementId.Fire, 1.5f), (ElementId.Light, 1.3f));

            // ---------------- Shadow Court additions: mana drain and element shifting
            var S = Faction.ShadowCourt;
            var leech = Enemy("mana_leech", "Đỉa Mana", EnemyBehavior.ManaDrainer, S, "Lẻn tới Giếng Mana và hút 4 Mana mỗi cú đánh.", 90, 2, 2.6f, 0.27f, 4, 5, 3,
                new AttackSpec { Damage = 10, Interval = 1.0f, Range = 0.9f, Element = ElementId.Shadow, StatusAmount = 16f }, "drain");
            leech.ManaDrain = 4f;
            var shifter = Enemy("void_shifter", "Kẻ Đổi Dạng", EnemyBehavior.Shifter, S, "Mỗi 6 giây đổi nguyên tố: kháng 50% nguyên tố hiện tại và tấn công bằng nó.", 150, 3, 1.9f, 0.32f, 5, 9, 4,
                new AttackSpec { Damage = 14, Interval = 1.2f, Range = 3.2f, ProjectileSpeed = 10f, StatusAmount = 24f, HitsAir = true }, "caster", "elemental");
            shifter.ShiftInterval = 6f;

            // ---------------- Wild Tempest: fast aerial storms and displacement
            var W = Faction.WildTempest;
            var sharpy = Enemy("storm_harpy", "Nữ Yêu Bão", EnemyBehavior.Flyer, W, "Bầy KẺ BAY gây sốc Sét.", 50, 0, 3.2f, 0.26f, 2, 3, 2,
                new AttackSpec { Damage = 7, Interval = 0.9f, Range = 0.9f, Element = ElementId.Lightning, StatusAmount = 18f }, "flyer", "swarm");
            sharpy.Move = MoveKind.Flying;
            var raider = Enemy("gale_raider", "Kỵ Cướp Gió", EnemyBehavior.Skirmisher, W, "Đánh và rút, đòn Gió hất văng.", 85, 2, 3.8f, 0.3f, 3, 4, 2,
                new AttackSpec { Damage = 12, Interval = 0.9f, Range = 1.0f, Element = ElementId.Wind, StatusAmount = 24f, Knockback = 0.6f }, "fast");
            var tbird = Enemy("thunder_bird", "Chim Sấm", EnemyBehavior.Flyer, W, "KẺ BAY: sét nảy qua 2 mục tiêu.", 110, 2, 2.8f, 0.34f, 5, 8, 4,
                new AttackSpec { Damage = 14, Interval = 1.3f, Range = 3f, ProjectileSpeed = 14f, Element = ElementId.Lightning, StatusAmount = 28f, ChainCount = 2, HitsAir = true }, "flyer");
            tbird.Move = MoveKind.Flying;
            tbird.ElementTaken = Taken((ElementId.Lightning, 0.5f), (ElementId.Earth, 1.3f));
            var cyclone = Enemy("cyclone_shaman", "Pháp Sư Lốc Xoáy", EnemyBehavior.Buffer, W, "Lốc xoáy hất lính ra khỏi vị trí; kẻ địch quanh nó nhanh hơn 25%.", 90, 1, 2.0f, 0.27f, 5, 6, 3,
                new AttackSpec { Damage = 8, Interval = 2.0f, Range = 3.6f, ProjectileSpeed = 10f, Splash = 1.4f, Knockback = 1.0f, Element = ElementId.Wind, StatusAmount = 30f, HitsAir = true }, "caster");
            cyclone.AuraRadius = 3f; cyclone.AuraSpeedMult = 1.25f;
            var lancer = Enemy("sky_lancer", "Thương Kỵ Bầu Trời", EnemyBehavior.Elite, W, "Tinh anh BAY: lao xuống đâm xuyên, kháng khống chế.", 360, 5, 3.0f, 0.4f, 10, 11, 7,
                new AttackSpec { Damage = 30, Interval = 1.2f, Range = 1.2f, Element = ElementId.Wind, StatusAmount = 24f, StructureMult = 1.2f }, "elite", "flyer");
            lancer.Move = MoveKind.Flying; lancer.Large = true; lancer.CcResist = 0.5f;
        }
    }
}
