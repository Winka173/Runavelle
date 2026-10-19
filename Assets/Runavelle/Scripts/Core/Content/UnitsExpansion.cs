namespace Runavelle.Core
{
    /// <summary>Phase I: six additional soldier types, each with distinct behaviour (not reskins).</summary>
    public sealed partial class ContentDatabase
    {
        partial void AddExpansionSoldiersImpl()
        {
            // Shield Guardian: intercepts enemy projectiles aimed at nearby allies (EffectsSystem.TryIntercept), formation tank.
            Soldier(SoldierClass.ShieldGuardian, "Vệ Binh Khiên", "Chặn đạn bắn vào đồng minh gần đó (50%), giữ đội hình.", 170, 8, 2.2f, 0.3f, 4f,
                new AttackSpec { Damage = 8, Interval = 1.0f, Range = 0.9f },
                new SoldierBranch { Name = "Vệ Binh Bình Minh", Element = ElementId.Light, StatusBonus = 24f, HpMult = 1.15f, Description = "Đòn Ánh Sáng; khiên sáng chói làm kẻ địch Rạng rỡ." },
                new SoldierBranch { Name = "Vệ Binh Đá", Element = ElementId.Earth, ArmorBonus = 4f, StatusBonus = 20f, Description = "+4 giáp, đòn Đất làm Rung chấn." },
                u => { u.InterceptChance = 0.5f; });
            // Crossbowman: slow heavy armor-piercing bolts.
            Soldier(SoldierClass.Crossbowman, "Nỏ Thủ", "Phát bắn nặng, chậm, xuyên 60% giáp – khắc chế giáp dày.", 80, 2, 2.4f, 0.27f, 5.5f,
                new AttackSpec { Damage = 24, Interval = 2.0f, Range = 5.2f, ArmorPierce = 0.6f, HitsAir = true, ProjectileSpeed = 18f },
                new SoldierBranch { Name = "Nỏ Bóng Đêm", Element = ElementId.Shadow, StatusBonus = 30f, Description = "Mũi tên Ấn ký: mục tiêu nhận thêm sát thương; ưu tiên tinh anh." },
                new SoldierBranch { Name = "Nỏ Hoả Pháo", Element = ElementId.Fire, SplashBonus = 0.9f, StatusBonus = 26f, DmgMult = 0.9f, Description = "Mũi tên nổ: Bỏng diện rộng." });
            // Battlemage: melee strikes + a ranged spell (secondary attack) when targets are out of reach.
            Soldier(SoldierClass.Battlemage, "Chiến Pháp Sư", "Cận chiến bằng kiếm phép, phóng phép tầm xa khi mục tiêu ở xa.", 120, 3, 2.5f, 0.28f, 5f,
                new AttackSpec { Damage = 12, Interval = 1.0f, Range = 1.0f, StatusAmount = 10f },
                new SoldierBranch { Name = "Lôi Kiếm Sư", Element = ElementId.Lightning, StatusBonus = 22f, ChainBonus = 1, Description = "Kiếm và phép Sét nảy mục tiêu." },
                new SoldierBranch { Name = "Băng Kiếm Sư", Element = ElementId.Ice, StatusBonus = 26f, SlowOnHit = 0.2f, Description = "Kiếm và phép Băng làm chậm." },
                u => { u.Secondary = new AttackSpec { Damage = 14, Interval = 2.6f, Range = 3.6f, StatusAmount = 26f, HitsAir = true, ProjectileSpeed = 10f, Splash = 0.6f }; });
            // Elemental Knight: element-imbued bruiser (always elemental, even at tier 1).
            Soldier(SoldierClass.ElementalKnight, "Hiệp Sĩ Nguyên Tố", "Đấu sĩ hạng nặng, đòn đánh luôn mang nguyên tố.", 220, 5, 2.2f, 0.33f, 4.2f,
                new AttackSpec { Damage = 18, Interval = 1.2f, Range = 1.05f, Element = ElementId.Fire, StatusAmount = 22f, Splash = 0.7f },
                new SoldierBranch { Name = "Hiệp Sĩ Hoả Ngục", Element = ElementId.Fire, SplashBonus = 0.4f, StatusBonus = 12f, DmgMult = 1.1f, Description = "Chém lửa diện rộng." },
                new SoldierBranch { Name = "Hiệp Sĩ Thuỷ Triều", Element = ElementId.Water, StatusBonus = 20f, HpMult = 1.15f, Description = "Đòn Nước làm Ướt cả nhóm – mồi cho Sét/Băng." });
            // Griffin Rider: flying interceptor, ignores walls/terrain, double damage vs flyers.
            Soldier(SoldierClass.GriffinRider, "Kỵ Sĩ Sư Ưng", "BAY: vượt tường và địa hình, gấp đôi sát thương lên kẻ bay.", 130, 3, 4.0f, 0.34f, 7f,
                new AttackSpec { Damage = 15, Interval = 1.0f, Range = 1.1f, HitsAir = true, FlyerMult = 2f },
                new SoldierBranch { Name = "Sư Ưng Cuồng Phong", Element = ElementId.Wind, StatusBonus = 24f, SpeedMult = 1.15f, Description = "Cánh gió đẩy lùi và lan hiệu ứng." },
                new SoldierBranch { Name = "Sư Ưng Sấm Sét", Element = ElementId.Lightning, StatusBonus = 24f, ChainBonus = 1, Description = "Móng vuốt Sét nảy mục tiêu." },
                u => { u.Move = MoveKind.Flying; });
            // Rune Golem: durable lane blocker, taunts nearby enemies, limited crowd control.
            Soldier(SoldierClass.RuneGolem, "Golem Cổ Tự", "Chặn đường bền bỉ: buộc kẻ địch gần đó tấn công nó, đòn đánh có thể làm choáng.", 520, 9, 1.6f, 0.45f, 3f,
                new AttackSpec { Damage = 20, Interval = 1.8f, Range = 1.1f, StunChance = 0.15f, Knockback = 0.3f },
                new SoldierBranch { Name = "Golem Núi", Element = ElementId.Earth, StatusBonus = 30f, HpMult = 1.2f, StunChance = 0.25f, Description = "Đòn Đất, 25% choáng." },
                new SoldierBranch { Name = "Golem Băng Hà", Element = ElementId.Ice, StatusBonus = 34f, SlowOnHit = 0.3f, Description = "Đòn Băng làm chậm mạnh." },
                u => { u.TauntRadius = 2.6f; u.Large = true; });
        }
    }
}
