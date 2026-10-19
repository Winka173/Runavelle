using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Phase I: ten more Generals (16 total, two per element) and two evolutions for every General.</summary>
    public sealed partial class ContentDatabase
    {
        private static GeneralEvolution Evo(string id, string name, string desc, params AbilityEffect[] extra)
            => new GeneralEvolution { Id = id, Name = name, Description = desc, ExtraUltEffects = new List<AbilityEffect>(extra) };

        partial void AddExpansionGeneralsImpl()
        {
            AddGeneral(new GeneralDefinition
            {
                Id = "tharion", Name = "Tharion", Title = "Hộ Vệ Đá", Group = GeneralGroup.Vanguard, Element = ElementId.Earth, Ai = AiStyle.Frontline,
                Unlock = UnlockRule.ReachNight5, R = 0.6f, G = 0.55f, B = 0.42f,
                Description = "Pháo đài biết đi. Gia cố tường xung quanh và dựng tường đá tạm thời để bịt chỗ vỡ.",
                Body = new UnitDefinition { MaxHp = 760, Armor = 9, Speed = 3.6f, Radius = 0.4f, AggroRange = 2.8f,
                    Attack = new AttackSpec { Damage = 28, Interval = 1.0f, Range = 1.3f, Splash = 0.9f, StatusAmount = 26f, Knockback = 0.3f } },
                Aura = new GeneralAura { Description = "Tường, cổng và lính gần Tharion: +4 giáp, hồi 1.5% máu/giây.", Radius = 4.5f, ArmorBonus = 4f, RegenFraction = 0.015f, AffectsWalls = true },
                Ultimate = new UltimateSpec { Name = "Thành Đá Trỗi Dậy", Cooldown = 24f, CastRadius = 3f,
                    Description = "Dựng 5 đoạn tường đá trong 10 giây về phía kẻ địch gần nhất và đập Đất (110 sát thương, Rung chấn).",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.Barrier, 2.2f, count: 5, duration: 10f), Fx(AbilityEffectKind.AoeDamage, 3f, 110, ElementId.Earth, 60) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "aurelia", Name = "Aurelia", Title = "Thống Soái Bình Minh", Group = GeneralGroup.Commander, Element = ElementId.Light, Ai = AiStyle.Support,
                Unlock = UnlockRule.FirstVictory, R = 1f, G = 0.82f, B = 0.4f,
                Description = "Hiệp sĩ Ánh Sáng. Che chắn đội quân và hàn gắn tường thành giữa trận.",
                Body = new UnitDefinition { MaxHp = 600, Armor = 6, Speed = 3.7f, Radius = 0.35f, AggroRange = 3f,
                    Attack = new AttackSpec { Damage = 26, Interval = 0.9f, Range = 1.6f, StatusAmount = 26f, Splash = 0.7f } },
                Aura = new GeneralAura { Description = "Lính và tường gần Aurelia: +3 giáp, hồi 1% máu/giây.", Radius = 5f, ArmorBonus = 3f, RegenFraction = 0.01f, AffectsWalls = true },
                Ultimate = new UltimateSpec { Name = "Khiên Bình Minh", Cooldown = 28f, CastRadius = 6f,
                    Description = "Đồng minh trong 6 ô giảm 40% sát thương nhận 6 giây; tường/công trình trong 6 ô hồi 25% máu; nổ Ánh Sáng 90.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.AllyShield, 6f, value: 0.4f, duration: 6f), Fx(AbilityEffectKind.RepairStructures, 6f, value: 0.25f), Fx(AbilityEffectKind.AoeDamage, 3.5f, 90, ElementId.Light, 50) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "nyxara", Name = "Nyxara", Title = "Lưỡi Kiếm Bóng Đêm", Group = GeneralGroup.Specialist, Element = ElementId.Shadow, Ai = AiStyle.Frontline,
                Unlock = UnlockRule.Victories2, R = 0.5f, G = 0.35f, B = 0.75f,
                Description = "Sát thủ song đao. Săn tinh anh và xe công thành, kết liễu mục tiêu yếu máu.",
                Body = new UnitDefinition { MaxHp = 430, Armor = 3, Speed = 4.4f, Radius = 0.3f, AggroRange = 3f,
                    Attack = new AttackSpec { Damage = 15, Interval = 0.45f, Range = 1.1f, StatusAmount = 14f } },
                Aura = new GeneralAura { Description = "Kẻ địch gần Nyxara nhận thêm 10% sát thương.", Radius = 4f, EnemyVulnerability = 0.1f },
                Ultimate = new UltimateSpec { Name = "Hành Quyết", Cooldown = 22f, CastRadius = 5f,
                    Description = "Chém 3 kẻ địch yếu máu nhất trong 5 ô (120 sát thương, tăng theo máu đã mất; kết liễu kẻ thường dưới 25%) và Ấn ký chúng.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.Execute, 5f, 120, ElementId.Shadow, 60, 3, value: 0.25f), Fx(AbilityEffectKind.Mark, 5f, 0, ElementId.Shadow, 60, 3, 6f, 0.25f) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "isolde", Name = "Isolde", Title = "Hiền Giả Băng Giá", Group = GeneralGroup.Battlemage, Element = ElementId.Ice, Ai = AiStyle.Ranged,
                Unlock = UnlockRule.ReachNight10, R = 0.6f, G = 0.85f, B = 1f,
                Description = "Pháp sư khống chế. Đóng băng cả một mặt trận để tháp và lính tha hồ dồn sát thương.",
                Body = new UnitDefinition { MaxHp = 420, Armor = 2, Speed = 3.7f, Radius = 0.31f, AggroRange = 4.6f,
                    Attack = new AttackSpec { Damage = 18, Interval = 0.85f, Range = 4.6f, ProjectileSpeed = 14f, StatusAmount = 30f, Splash = 0.7f } },
                Aura = new GeneralAura { Description = "Kẻ địch gần Isolde chậm 15%; tháp gần cô gây hiệu ứng mạnh hơn 30%.", Radius = 5f, EnemySlow = 0.15f, StatusMult = 1.3f, AffectsTowers = true },
                Ultimate = new UltimateSpec { Name = "Vĩnh Đông", Cooldown = 24f, CastRadius = 3.5f,
                    Description = "Đóng băng mọi kẻ địch trong 3.5 ô 2.5 giây (boss kháng dần) và để lại bão tuyết 5 giây.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.FreezeArea, 3.5f, 30, ElementId.Ice, 0, duration: 2.5f), Fx(AbilityEffectKind.Zone, 3f, 0, ElementId.Ice, 16, duration: 5f, zone: ZoneKind.Blizzard) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "maren", Name = "Maren", Title = "Người Gọi Thuỷ Triều", Group = GeneralGroup.Battlemage, Element = ElementId.Water, Ai = AiStyle.Ranged,
                Unlock = UnlockRule.MasteryPoints, UnlockValue = 60, R = 0.25f, G = 0.6f, B = 0.7f,
                Description = "Pháp sư Nước. Sóng thần đẩy lùi cả đám và làm chúng Ướt – mồi hoàn hảo cho Sét và Băng.",
                Body = new UnitDefinition { MaxHp = 470, Armor = 3, Speed = 3.8f, Radius = 0.32f, AggroRange = 4f,
                    Attack = new AttackSpec { Damage = 16, Interval = 0.9f, Range = 4f, ProjectileSpeed = 12f, StatusAmount = 34f, Splash = 0.7f } },
                Aura = new GeneralAura { Description = "Lính gần Maren hồi 1.5% máu/giây; đòn của chúng thêm hiệu ứng Nước.", Radius = 4.5f, RegenFraction = 0.015f, GrantElement = ElementId.Water, GrantStatus = 12f },
                Ultimate = new UltimateSpec { Name = "Sóng Thần", Cooldown = 25f, CastRadius = 4f,
                    Description = "Sóng lớn quanh Maren: 70 sát thương, đẩy lùi 1.5 ô, Ướt nặng; để lại mưa 4 giây.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.Knockwave, 4f, 70, ElementId.Water, 70, value: 1.5f), Fx(AbilityEffectKind.Zone, 3f, 0, ElementId.Water, 20, duration: 4f, zone: ZoneKind.Rain) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "brannoc", Name = "Brannoc", Title = "Thương Kỵ Sấm Sét", Group = GeneralGroup.Specialist, Element = ElementId.Lightning, Ai = AiStyle.Frontline,
                Unlock = UnlockRule.Victories2, R = 0.9f, G = 0.78f, B = 0.3f, DashDistance = 5f, DashCooldown = 4.5f,
                Description = "Kỵ sĩ phản ứng nhanh. Lướt xa nhất và xung phong xuyên qua hàng địch để tới mặt trận bị đe doạ.",
                Body = new UnitDefinition { MaxHp = 560, Armor = 5, Speed = 4.8f, Radius = 0.4f, AggroRange = 3f,
                    Attack = new AttackSpec { Damage = 26, Interval = 0.85f, Range = 1.6f, StatusAmount = 22f, ChainCount = 1 } },
                Aura = new GeneralAura { Description = "Lính gần Brannoc: +20% tốc đánh, +15% tốc chạy.", Radius = 5f, AttackSpeedMult = 1.2f, MoveSpeedMult = 1.15f },
                Ultimate = new UltimateSpec { Name = "Lôi Thương Xung Phong", Cooldown = 16f, CastRadius = 6f,
                    Description = "Xung phong 6 ô theo hướng di chuyển, 110 sát thương Sét lên mọi kẻ trên đường, miễn thương khi lao.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.LineStrike, 1.3f, 110, ElementId.Lightning, 50, value: 6f) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "pyra", Name = "Pyra", Title = "Phù Thuỷ Than Hồng", Group = GeneralGroup.Battlemage, Element = ElementId.Fire, Ai = AiStyle.Ranged,
                Unlock = UnlockRule.Victories3, R = 0.75f, G = 0.25f, B = 0.25f,
                Description = "Pháp sư hủy diệt. Gọi mưa thiên thạch xuống cả một khu vực.",
                Body = new UnitDefinition { MaxHp = 400, Armor = 2, Speed = 3.6f, Radius = 0.31f, AggroRange = 4.6f,
                    Attack = new AttackSpec { Damage = 19, Interval = 1.1f, Range = 4.6f, ProjectileSpeed = 10f, StatusAmount = 28f, Splash = 0.9f } },
                Aura = new GeneralAura { Description = "Tháp và lính gần Pyra: +15% sát thương.", Radius = 4.5f, DamageMult = 1.15f, AffectsTowers = true },
                Ultimate = new UltimateSpec { Name = "Mưa Thiên Thạch", Cooldown = 28f, CastRadius = 6f,
                    Description = "8 thiên thạch rơi lên kẻ địch trong 6 ô (65 sát thương Lửa mỗi viên) và để lại dung nham.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.Volley, 6f, 65, ElementId.Fire, 40, 8, value: 0.4f), Fx(AbilityEffectKind.Zone, 2.2f, 18, ElementId.Fire, 15, duration: 4f, zone: ZoneKind.Lava) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "corvin", Name = "Corvin", Title = "Sứ Giả Bão Tố", Group = GeneralGroup.Commander, Element = ElementId.Wind, Ai = AiStyle.Support,
                Unlock = UnlockRule.MasteryPoints, UnlockValue = 60, R = 0.6f, G = 0.75f, B = 0.85f,
                Description = "Chỉ huy Gió. Đội quân quanh ông di chuyển và đánh nhanh hơn; cuồng phong gom kẻ địch lại một chỗ.",
                Body = new UnitDefinition { MaxHp = 500, Armor = 4, Speed = 4.0f, Radius = 0.33f, AggroRange = 3.5f,
                    Attack = new AttackSpec { Damage = 26, Interval = 0.8f, Range = 2.0f, StatusAmount = 22f, Knockback = 0.3f, Splash = 0.7f } },
                Aura = new GeneralAura { Description = "Lính gần Corvin: +25% tốc chạy, +15% tốc đánh, +10% sát thương.", Radius = 5f, MoveSpeedMult = 1.25f, AttackSpeedMult = 1.15f, DamageMult = 1.1f },
                Ultimate = new UltimateSpec { Name = "Mắt Bão", Cooldown = 26f, CastRadius = 5f,
                    Description = "Hút kẻ địch trong 5 ô về phía Corvin (60 sát thương Gió), tạo vùng gió làm chậm 5 giây; lính gần đó cuồng nộ +45% tốc đánh 6 giây.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.Pull, 5f, 60, ElementId.Wind, 40, value: 2.2f), Fx(AbilityEffectKind.Zone, 3f, 0, ElementId.Wind, 10, duration: 5f, zone: ZoneKind.Gust),
                        Fx(AbilityEffectKind.TroopFrenzy, 6f, value: 0.45f, duration: 6f) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "hilda", Name = "Hilda", Title = "Khiên Băng", Group = GeneralGroup.Vanguard, Element = ElementId.Ice, Ai = AiStyle.Frontline,
                Unlock = UnlockRule.Victories3, R = 0.5f, G = 0.7f, B = 0.85f,
                Description = "Đấu sĩ Băng. Làm chậm mọi thứ quanh mình và dựng tường băng chặn đường.",
                Body = new UnitDefinition { MaxHp = 700, Armor = 8, Speed = 3.4f, Radius = 0.38f, AggroRange = 2.8f,
                    Attack = new AttackSpec { Damage = 27, Interval = 0.95f, Range = 1.3f, Splash = 0.8f, StatusAmount = 26f } },
                Aura = new GeneralAura { Description = "Lính gần Hilda +3 giáp; kẻ địch gần cô chậm 20%.", Radius = 4f, ArmorBonus = 3f, EnemySlow = 0.2f },
                Ultimate = new UltimateSpec { Name = "Thành Băng", Cooldown = 26f, CastRadius = 2.5f,
                    Description = "Dựng 4 đoạn tường băng 8 giây, đóng băng kẻ địch trong 2.5 ô 1.5 giây và tự giảm 40% sát thương nhận 5 giây.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.Barrier, 2f, count: 4, duration: 8f), Fx(AbilityEffectKind.FreezeArea, 3f, 50, ElementId.Ice, 0, duration: 1.8f),
                        Fx(AbilityEffectKind.SelfBuff, value: 0.4f, duration: 5f) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "maelis", Name = "Maelis", Title = "Nữ Hoàng Thuỷ Triều", Group = GeneralGroup.Commander, Element = ElementId.Water, Ai = AiStyle.Support,
                Unlock = UnlockRule.MasteryPoints, UnlockValue = 120, R = 0.3f, G = 0.5f, B = 0.85f,
                Description = "Chỉ huy hỗ trợ. Hồi máu cả mặt trận và che chắn Lâu Đài bằng nước triều.",
                Body = new UnitDefinition { MaxHp = 520, Armor = 4, Speed = 3.7f, Radius = 0.33f, AggroRange = 3.2f,
                    Attack = new AttackSpec { Damage = 20, Interval = 0.9f, Range = 2.0f, StatusAmount = 26f, Splash = 0.5f } },
                Aura = new GeneralAura { Description = "Lính gần Maelis hồi 2% máu/giây và gây hiệu ứng mạnh hơn 25%.", Radius = 5f, RegenFraction = 0.02f, StatusMult = 1.25f },
                Ultimate = new UltimateSpec { Name = "Triều Hồi Sinh", Cooldown = 30f, CastRadius = 6f,
                    Description = "Hồi 30% máu cho đồng minh và công trình trong 6 ô, gọi mưa 5 giây và Lâu Đài giảm 30% sát thương nhận 6 giây.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.AllyHeal, 6f, value: 0.3f), Fx(AbilityEffectKind.Zone, 3.5f, 0, ElementId.Water, 18, duration: 5f, zone: ZoneKind.Rain),
                        Fx(AbilityEffectKind.CastleShield, value: 0.3f, duration: 6f) } },
            });
        }

        partial void AddGeneralEvolutionsImpl()
        {
            void Set(string id, GeneralEvolution a, GeneralEvolution b) => Generals[id].Evolutions = new[] { a, b };
            Set("roland",
                new GeneralEvolution { Id = "roland_marshal", Name = "Thống Soái Thép", Description = "Hào quang mạnh gấp 1.5; tối thượng còn khiến lính trong 6 ô cuồng nộ +35% tốc đánh 6 giây.", AuraMult = 1.5f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.TroopFrenzy, 6f, value: 0.35f, duration: 6f) } },
                new GeneralEvolution { Id = "roland_bastion", Name = "Chúa Tể Pháo Đài", Description = "+25% máu; tối thượng sửa 30% máu tường/công trình trong 6 ô và khôi phục tường đổ.", HpMult = 1.25f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.RepairStructures, 6f, value: 0.3f) } });
            Set("lyra",
                new GeneralEvolution { Id = "lyra_tempest", Name = "Bão Sét Vĩnh Cửu", Description = "Tối thượng nảy thêm 6 mục tiêu với 45 sát thương; +20% sức mạnh tối thượng.", UltPowerMult = 1.2f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.ChainDamage, 6f, 45, ElementId.Lightning, 30, 6) } },
                new GeneralEvolution { Id = "lyra_conductor", Name = "Nhạc Trưởng Nguyên Tố", Description = "Hào quang mạnh gấp 1.6 (hiệu ứng nguyên tố của mọi đồng minh quanh cô); hồi tối thượng -25%.", AuraMult = 1.6f, UltCooldownMult = 0.75f });
            Set("kaelen",
                new GeneralEvolution { Id = "kaelen_dragon", Name = "Hoả Long", Description = "Tối thượng thêm sóng lửa đẩy lùi 1.2 ô (60 sát thương).",
                    ExtraUltEffects = { Fx(AbilityEffectKind.Knockwave, 4f, 60, ElementId.Fire, 40, value: 1.2f) } },
                new GeneralEvolution { Id = "kaelen_undying", Name = "Ngọn Lửa Bất Diệt", Description = "+20% máu, +15% sát thương; tối thượng cho Kaelen giảm 40% sát thương nhận 6 giây.", HpMult = 1.2f, DmgMult = 1.15f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.SelfBuff, value: 0.4f, duration: 6f) } });
            Set("elowen",
                new GeneralEvolution { Id = "elowen_hawkeye", Name = "Mắt Ưng", Description = "Tối thượng bắn thêm 8 mũi tên.",
                    ExtraUltEffects = { Fx(AbilityEffectKind.Volley, 7f, 32, ElementId.Wind, 30, 8, value: 0.6f) } },
                new GeneralEvolution { Id = "elowen_galeborn", Name = "Con Của Gió", Description = "Hào quang mạnh gấp 1.5; tối thượng còn hút kẻ địch trong 5 ô lại một chỗ.", AuraMult = 1.5f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.Pull, 5f, 0, ElementId.Wind, 20, value: 1.5f) } });
            Set("seraphine",
                new GeneralEvolution { Id = "seraphine_guardian", Name = "Thiên Thần Hộ Mệnh", Description = "Tối thượng hồi thêm 20% và che Lâu Đài lâu hơn.", UltPowerMult = 1.3f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.CastleShield, value: 0.6f, duration: 14f) } },
                new GeneralEvolution { Id = "seraphine_judgement", Name = "Phán Quyết Rạng Đông", Description = "Tối thượng để lại vùng Rạng Đông 6 giây hồi máu đồng minh và đốt kẻ địch bằng Ánh Sáng.",
                    ExtraUltEffects = { Fx(AbilityEffectKind.Zone, 3.5f, 16, ElementId.Light, 18, duration: 6f, zone: ZoneKind.Radiance) } });
            Set("varric",
                new GeneralEvolution { Id = "varric_reaper", Name = "Lưỡi Hái Hư Vô", Description = "Tối thượng còn kết liễu 2 kẻ địch yếu nhất (dưới 20% máu).",
                    ExtraUltEffects = { Fx(AbilityEffectKind.Execute, 6f, 80, ElementId.Shadow, 40, 2, value: 0.2f) } },
                new GeneralEvolution { Id = "varric_lord", Name = "Chúa Tể Bóng Đêm", Description = "Hào quang mạnh gấp 1.6; tối thượng phủ vùng u ám 6 giây (kẻ địch nhận thêm 20% sát thương).", AuraMult = 1.6f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.Zone, 3.5f, 0, ElementId.Shadow, 0, duration: 6f, zone: ZoneKind.Gloom) } });
            Set("tharion",
                new GeneralEvolution { Id = "tharion_fortress", Name = "Chúa Pháo Đài", Description = "Tường tạm nhiều hơn 3 đoạn và tồn tại lâu hơn; hào quang tường mạnh gấp 1.5.", AuraMult = 1.5f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.Barrier, 3.2f, count: 3, duration: 12f) } },
                new GeneralEvolution { Id = "tharion_breaker", Name = "Kẻ Phá Đất", Description = "Tối thượng thêm sóng chấn động đẩy lùi (90 sát thương Đất); +15% sát thương.", DmgMult = 1.15f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.Knockwave, 3.5f, 90, ElementId.Earth, 50, value: 1.4f) } });
            Set("aurelia",
                new GeneralEvolution { Id = "aurelia_bastion", Name = "Thành Trì Rạng Rỡ", Description = "Khiên mạnh hơn 30% và Lâu Đài giảm 50% sát thương nhận 6 giây khi dùng tối thượng.", UltPowerMult = 1.3f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.CastleShield, value: 0.5f, duration: 6f) } },
                new GeneralEvolution { Id = "aurelia_vanguard", Name = "Tiên Phong Bình Minh", Description = "Tối thượng mở đầu bằng cú xung phong 5 ô (80 sát thương Ánh Sáng); +15% sát thương.", DmgMult = 1.15f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.LineStrike, 1.2f, 80, ElementId.Light, 50, value: 5f) } });
            Set("nyxara",
                new GeneralEvolution { Id = "nyxara_stalker", Name = "Kẻ Rình Đêm", Description = "Hành quyết 5 mục tiêu; hồi tối thượng -25%.", UltCooldownMult = 0.75f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.Execute, 5f, 90, ElementId.Shadow, 40, 2, value: 0.25f) } },
                new GeneralEvolution { Id = "nyxara_dread", Name = "Chỉ Huy Khiếp Sợ", Description = "Hào quang mạnh gấp 2 và làm chậm kẻ địch 20%.", AuraMult = 2f,
                    AuraOverride = new GeneralAura { Description = "Kẻ địch gần Nyxara nhận thêm 10% sát thương và chậm 20%.", Radius = 4.5f, EnemyVulnerability = 0.1f, EnemySlow = 0.2f } });
            Set("isolde",
                new GeneralEvolution { Id = "isolde_glacier", Name = "Trái Tim Băng Hà", Description = "Đóng băng to hơn và lâu hơn (sức mạnh tối thượng +30%).", UltPowerMult = 1.3f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.FreezeArea, 4.5f, 0, ElementId.Ice, 0, duration: 1.0f) } },
                new GeneralEvolution { Id = "isolde_shatter", Name = "Hiền Giả Vỡ Vụn", Description = "Sau khi đóng băng, gai băng nổ 90 sát thương (gấp đôi lên mục tiêu đang đóng băng nhờ phản ứng).",
                    ExtraUltEffects = { Fx(AbilityEffectKind.AoeDamage, 3.5f, 90, ElementId.Earth, 40) } });
            Set("maren",
                new GeneralEvolution { Id = "maren_tidebreaker", Name = "Kẻ Xé Thuỷ Triều", Description = "Sóng đẩy xa hơn và gây thêm 60 sát thương.", UltPowerMult = 1.2f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.Knockwave, 5f, 60, ElementId.Water, 30, value: 1f) } },
                new GeneralEvolution { Id = "maren_spring", Name = "Suối Chữa Lành", Description = "Tối thượng hồi 30% máu cho đồng minh và công trình trong 6 ô.",
                    ExtraUltEffects = { Fx(AbilityEffectKind.AllyHeal, 6f, value: 0.3f) } });
            Set("brannoc",
                new GeneralEvolution { Id = "brannoc_storm", Name = "Kỵ Sĩ Bão", Description = "Xung phong lần hai ngay sau lần đầu (4 ô, 70 sát thương).",
                    ExtraUltEffects = { Fx(AbilityEffectKind.LineStrike, 1.2f, 70, ElementId.Lightning, 40, value: 4f) } },
                new GeneralEvolution { Id = "brannoc_herald", Name = "Sứ Giả Sấm", Description = "Tối thượng khiến mọi lính trong 7 ô cuồng nộ +40% tốc đánh 6 giây.",
                    ExtraUltEffects = { Fx(AbilityEffectKind.TroopFrenzy, 7f, value: 0.4f, duration: 6f) } });
            Set("pyra",
                new GeneralEvolution { Id = "pyra_cataclysm", Name = "Đại Hoả Tai", Description = "Thêm 6 thiên thạch.",
                    ExtraUltEffects = { Fx(AbilityEffectKind.Volley, 6f, 65, ElementId.Fire, 40, 6, value: 0.4f) } },
                new GeneralEvolution { Id = "pyra_hearth", Name = "Giữ Lửa", Description = "Hào quang mạnh gấp 1.6 và tháp gần Pyra bắn kèm Lửa.", AuraMult = 1.6f,
                    AuraOverride = new GeneralAura { Description = "Tháp và lính gần Pyra: +15% sát thương, đòn đánh thêm Lửa.", Radius = 4.5f, DamageMult = 1.15f, AffectsTowers = true, GrantElement = ElementId.Fire, GrantStatus = 12f } });
            Set("corvin",
                new GeneralEvolution { Id = "corvin_eye", Name = "Tâm Bão", Description = "Lực hút mạnh hơn và gây 60 sát thương Gió.", UltPowerMult = 1.2f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.AoeDamage, 3f, 60, ElementId.Wind, 30) } },
                new GeneralEvolution { Id = "corvin_tailwind", Name = "Thống Soái Gió Xuôi", Description = "Hào quang mạnh gấp 1.6 (tốc chạy/tốc đánh của lính).", AuraMult = 1.6f });
            Set("hilda",
                new GeneralEvolution { Id = "hilda_winterwall", Name = "Bức Tường Mùa Đông", Description = "Thêm 3 đoạn tường băng; +20% máu.", HpMult = 1.2f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.Barrier, 3f, count: 3, duration: 8f) } },
                new GeneralEvolution { Id = "hilda_avalanche", Name = "Tuyết Lở", Description = "Tối thượng thêm tuyết lở đẩy lùi (80 sát thương Băng).", DmgMult = 1.1f,
                    ExtraUltEffects = { Fx(AbilityEffectKind.Knockwave, 3.5f, 80, ElementId.Ice, 50, value: 1.3f) } });
            Set("maelis",
                new GeneralEvolution { Id = "maelis_matron", Name = "Mẫu Hậu Biển Sâu", Description = "Hồi máu tối thượng mạnh hơn 50%.", UltPowerMult = 1.5f },
                new GeneralEvolution { Id = "maelis_admiral", Name = "Đô Đốc Dòng Xoáy", Description = "Tối thượng thêm vùng bùn (chậm 50%) và sóng đẩy lùi.",
                    ExtraUltEffects = { Fx(AbilityEffectKind.Zone, 3f, 0, ElementId.None, 0.5f, duration: 5f, zone: ZoneKind.Mud), Fx(AbilityEffectKind.Knockwave, 3.5f, 40, ElementId.Water, 40, value: 1f) } });
        }
    }
}
