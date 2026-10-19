using System.Collections.Generic;

namespace Runavelle.Core
{
    public sealed partial class ContentDatabase
    {
        private GeneralDefinition AddGeneral(GeneralDefinition g)
        {
            g.Body.Id = "general_" + g.Id;
            g.Body.Name = g.Name;
            g.Body.Role = UnitRole.General;
            g.Body.Attack.HitsAir = true;
            if (g.Body.Attack.Element == ElementId.None) g.Body.Attack.Element = g.Element;
            Generals.Add(g.Id, g);
            GeneralOrder.Add(g.Id);
            return g;
        }

        private static AbilityEffect Fx(AbilityEffectKind k, float radius = 0, float dmg = 0, ElementId e = ElementId.None, float status = 0, int count = 0, float duration = 0, float value = 0, ZoneKind zone = ZoneKind.None, string summon = null)
            => new AbilityEffect { Kind = k, Radius = radius, Damage = dmg, Element = e, Status = status, Count = count, Duration = duration, Value = value, Zone = zone, SummonId = summon };

        partial void AddExpansionGeneralsImpl();
        partial void AddGeneralEvolutionsImpl();

        private void AddGenerals()
        {
            AddGeneral(new GeneralDefinition
            {
                Id = "roland", Name = "Roland", Title = "Thống Soái Sắt", Group = GeneralGroup.Commander, Element = ElementId.Earth, Ai = AiStyle.Frontline,
                Unlock = UnlockRule.Default, R = 0.72f, G = 0.55f, B = 0.32f,
                Description = "Chỉ huy bộ binh. Giữ chỗ vỡ tường như một bức tường sắt và làm lính quanh mình cứng cáp hơn.",
                Body = new UnitDefinition { MaxHp = 680, Armor = 7, Speed = 3.6f, Radius = 0.36f, AggroRange = 3f,
                    Attack = new AttackSpec { Damage = 24, Interval = 0.85f, Range = 1.3f, Splash = 0.8f, StatusAmount = 22f } },
                Aura = new GeneralAura { Description = "Lính gần Roland: +4 giáp và hồi 1% máu/giây.", Radius = 4.5f, ArmorBonus = 4f, RegenFraction = 0.01f },
                Ultimate = new UltimateSpec { Name = "Đập Thành Lũy", Cooldown = 28f, CastRadius = 3.2f,
                    Description = "Đập Đất (80 sát thương, Rung chấn mạnh) quanh Roland; đồng minh trong 5 ô giảm 50% sát thương nhận trong 6 giây.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.AoeDamage, 3.2f, 80, ElementId.Earth, 70), Fx(AbilityEffectKind.AllyShield, 5f, value: 0.5f, duration: 6f) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "lyra", Name = "Lyra", Title = "Kẻ Gọi Bão", Group = GeneralGroup.Battlemage, Element = ElementId.Lightning, Ai = AiStyle.Ranged,
                Unlock = UnlockRule.Default, R = 0.45f, G = 0.6f, B = 1f,
                Description = "Pháp sư Sét đánh xa. Có Lyra ở gần, mọi đòn đánh của đồng minh gây hiệu ứng nguyên tố mạnh hơn.",
                Body = new UnitDefinition { MaxHp = 430, Armor = 2, Speed = 3.8f, Radius = 0.32f, AggroRange = 4.5f,
                    Attack = new AttackSpec { Damage = 17, Interval = 0.8f, Range = 4.6f, ProjectileSpeed = 18f, StatusAmount = 26f } },
                Aura = new GeneralAura { Description = "Lính và tháp gần Lyra: +50% hiệu ứng nguyên tố.", Radius = 5f, StatusMult = 1.5f, AffectsTowers = true },
                Ultimate = new UltimateSpec { Name = "Chuỗi Cuồng Phong", Cooldown = 26f, CastRadius = 4.5f,
                    Description = "Gọi mưa (vùng Ướt) rồi sét nảy qua 8 kẻ địch (60 sát thương mỗi kẻ) – Điện Giật tức thì.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.Zone, 3.5f, 0, ElementId.Water, 30, duration: 5f, zone: ZoneKind.Rain), Fx(AbilityEffectKind.ChainDamage, 4.5f, 60, ElementId.Lightning, 40, 8) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "kaelen", Name = "Kaelen", Title = "Hiệp Sĩ Lửa", Group = GeneralGroup.Vanguard, Element = ElementId.Fire, Ai = AiStyle.Frontline,
                Unlock = UnlockRule.Default, R = 1f, G = 0.42f, B = 0.2f,
                Description = "Mũi nhọn Lửa. Đánh rất mạnh; lính gần anh ta đốt cháy kẻ địch.",
                Body = new UnitDefinition { MaxHp = 560, Armor = 5, Speed = 3.9f, Radius = 0.34f, AggroRange = 3f,
                    Attack = new AttackSpec { Damage = 27, Interval = 0.75f, Range = 1.35f, Splash = 0.9f, StatusAmount = 25f } },
                Aura = new GeneralAura { Description = "Lính gần Kaelen: +15% sát thương và đòn đánh gây Lửa.", Radius = 4.5f, DamageMult = 1.15f, GrantElement = ElementId.Fire, GrantStatus = 12f },
                Ultimate = new UltimateSpec { Name = "Quét Lửa", Cooldown = 24f, CastRadius = 3.2f,
                    Description = "Lửa quét quanh Kaelen (120 sát thương), để lại mặt đất cháy 5 giây.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.AoeDamage, 3.2f, 120, ElementId.Fire, 60), Fx(AbilityEffectKind.Zone, 2.6f, 14, ElementId.Fire, 15, duration: 5f, zone: ZoneKind.Burning) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "elowen", Name = "Elowen", Title = "Xạ Thủ Gió", Group = GeneralGroup.Specialist, Element = ElementId.Wind, Ai = AiStyle.Ranged,
                Unlock = UnlockRule.ReachNight5, R = 0.45f, G = 0.9f, B = 0.6f,
                Description = "Cung thủ Gió. Bắn nhanh, tầm xa; cung thủ và tháp gần cô bắn nhanh và xa hơn.",
                Body = new UnitDefinition { MaxHp = 410, Armor = 2, Speed = 4.1f, Radius = 0.3f, AggroRange = 5.5f,
                    Attack = new AttackSpec { Damage = 13, Interval = 0.55f, Range = 5.6f, ProjectileSpeed = 20f, StatusAmount = 14f } },
                Aura = new GeneralAura { Description = "Cung thủ, nỏ thủ và tháp gần Elowen: +25% tốc đánh, +1 tầm.", Radius = 5f, AttackSpeedMult = 1.25f, RangeBonus = 1f, AffectsTowers = true,
                    OnlyClass = SoldierClass.Archer, OnlyClass2 = SoldierClass.Crossbowman },
                Ultimate = new UltimateSpec { Name = "Loạt Tên Cuồng Phong", Cooldown = 24f, CastRadius = 7f,
                    Description = "12 mũi tên gió trút xuống kẻ địch trong 7 ô (32 sát thương mỗi mũi, đẩy lùi).",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.Volley, 7f, 32, ElementId.Wind, 30, 12, value: 0.6f) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "seraphine", Name = "Seraphine", Title = "Người Giữ Bình Minh", Group = GeneralGroup.Commander, Element = ElementId.Light, Ai = AiStyle.Support,
                Unlock = UnlockRule.ReachNight10, R = 1f, G = 0.9f, B = 0.5f,
                Description = "Hỗ trợ Ánh Sáng. Duy trì tiền tuyến và có thể che chắn Lâu Đài lúc nguy cấp.",
                Body = new UnitDefinition { MaxHp = 480, Armor = 4, Speed = 3.7f, Radius = 0.32f, AggroRange = 3.6f,
                    Attack = new AttackSpec { Damage = 15, Interval = 0.9f, Range = 3.6f, ProjectileSpeed = 14f, StatusAmount = 30f } },
                Aura = new GeneralAura { Description = "Lính và công trình gần Seraphine hồi 2.5% máu/giây.", Radius = 5f, RegenFraction = 0.025f, AffectsTowers = true, AffectsWalls = true },
                Ultimate = new UltimateSpec { Name = "Ân Điển Bình Minh", Cooldown = 32f, CastRadius = 6f,
                    Description = "Hồi 35% máu cho đồng minh và công trình trong 6 ô, nổ Rạng rỡ lên kẻ địch, Lâu Đài giảm 60% sát thương nhận trong 8 giây.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.AllyHeal, 6f, value: 0.35f), Fx(AbilityEffectKind.AoeDamage, 4f, 50, ElementId.Light, 60), Fx(AbilityEffectKind.CastleShield, value: 0.6f, duration: 8f) } },
            });
            AddGeneral(new GeneralDefinition
            {
                Id = "varric", Name = "Varric", Title = "Thống Lĩnh Bóng Đêm", Group = GeneralGroup.Commander, Element = ElementId.Shadow, Ai = AiStyle.Frontline,
                Unlock = UnlockRule.FirstVictory, R = 0.62f, G = 0.4f, B = 0.85f,
                Description = "Kẻ gây rối Bóng Tối. Đánh dấu xe công thành và tinh anh để cả đội quân dồn hỏa lực.",
                Body = new UnitDefinition { MaxHp = 560, Armor = 5, Speed = 3.9f, Radius = 0.34f, AggroRange = 3f,
                    Attack = new AttackSpec { Damage = 25, Interval = 0.85f, Range = 1.3f, StatusAmount = 25f } },
                Aura = new GeneralAura { Description = "Kẻ địch gần Varric nhận thêm 15% sát thương từ mọi nguồn.", Radius = 4.5f, EnemyVulnerability = 0.15f },
                Ultimate = new UltimateSpec { Name = "Ấn Diệt Vong", Cooldown = 26f, CastRadius = 6f,
                    Description = "Ấn ký tối đa 10 kẻ địch trong 6 ô (ưu tiên công thành/tinh anh): +35% sát thương nhận trong 8 giây và 40 sát thương Bóng Tối.",
                    Effects = new List<AbilityEffect> { Fx(AbilityEffectKind.Mark, 6f, 40, ElementId.Shadow, 100, 10, 8f, 0.35f) } },
            });
            AddExpansionGeneralsImpl();
            AddGeneralEvolutionsImpl();
        }
    }
}
