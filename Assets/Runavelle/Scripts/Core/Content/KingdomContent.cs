using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Phase III: Kingdom Evolution (one choice after night 7) and General equipment (Weapon + Relic).</summary>
    public sealed partial class ContentDatabase
    {
        public readonly List<KingdomEvolutionDef> Kingdoms = new List<KingdomEvolutionDef>();

        private void AddKingdoms()
        {
            Kingdoms.Add(new KingdomEvolutionDef
            {
                Id = "fortress", Name = "Pháo Đài",
                Description = "Tường và cổng +40% máu, +3 giáp, phản 15% sát thương cận chiến. Tường đổ tự xây lại miễn phí mỗi bình minh. Lâu Đài +25% máu, tháp +1 tầm.",
                Modifiers = { new Modifier(Stat.MaxHp, Scope.Walls, 0.4f), new Modifier(Stat.Armor, Scope.Walls, 0f, 3f), new Modifier(Stat.MaxHp, Scope.Castle, 0.25f), new Modifier(Stat.Range, Scope.Towers, 0f, 1f) },
                Hooks = { new HookValue(Hook.ThornWalls, 0.15f), new HookValue(Hook.EmergencyMasons, 1f) },
            });
            Kingdoms.Add(new KingdomEvolutionDef
            {
                Id = "legion", Name = "Quân Đoàn",
                Description = "Mỗi doanh trại +1 lính và bổ sung 1 lính mỗi 22 giây trong đêm. Bổ sung lúc bình minh miễn phí. Hào quang tướng +30%.",
                Modifiers = { new Modifier(Stat.SquadSize, Scope.Barracks, 0f, 1f), new Modifier(Stat.ReinforceCost, Scope.Any, -1f), new Modifier(Stat.AuraPower, Scope.Generals, 0.3f) },
                Hooks = { new HookValue(Hook.LegionReinforce, 22f) },
            });
            Kingdoms.Add(new KingdomEvolutionDef
            {
                Id = "arcanum", Name = "Huyền Bí",
                Description = "Thu Mana +40%, thu Vàng -15%. Tháp Phép +25% sát thương và mở ô nguyên tố phụ từ cấp 2. Phản ứng +30% sát thương; cứ 5 phản ứng nhận 1 Mana.",
                Modifiers = { new Modifier(Stat.ManaIncome, Scope.Any, 0.4f), new Modifier(Stat.GoldIncome, Scope.Any, -0.15f), new Modifier(Stat.Damage, Scope.MagicTowers, 0.25f), new Modifier(Stat.ReactionDamage, Scope.Any, 0.3f) },
                Hooks = { new HookValue(Hook.WildMagic, 1f), new HookValue(Hook.ElementalOverflow, 5f) },
            });
        }

        private EquipmentDefinition Eq(string id, EquipmentSlot slot, string name, string desc, UnlockRule unlock, int value, params object[] effects)
        {
            var e = new EquipmentDefinition { Id = id, Slot = slot, Name = name, Description = desc, Unlock = unlock, UnlockValue = value };
            foreach (var x in effects)
            {
                if (x is Modifier m) e.Modifiers.Add(m);
                else if (x is HookValue h) e.Hooks.Add(h);
                else if (x is ElementId el) e.SecondaryElement = el;
            }
            Equipment.Add(id, e);
            return e;
        }

        partial void AddEquipmentImpl()
        {
            AddKingdoms();
            // Weapons: change how the General's basic attack behaves (secondary element or attack profile).
            Eq("dawnblade", EquipmentSlot.Weapon, "Kiếm Bình Minh", "Đòn đánh của tướng thêm Ánh Sáng (nguyên tố phụ).", UnlockRule.Default, 0, ElementId.Light);
            Eq("duskfang", EquipmentSlot.Weapon, "Nanh Hoàng Hôn", "Đòn đánh của tướng thêm Bóng Tối (Ấn ký).", UnlockRule.Default, 0, ElementId.Shadow);
            Eq("stormbrand", EquipmentSlot.Weapon, "Ấn Bão", "Đòn đánh của tướng thêm Sét.", UnlockRule.ReachNight5, 0, ElementId.Lightning);
            Eq("frostbite", EquipmentSlot.Weapon, "Răng Băng", "Đòn đánh của tướng thêm Băng (làm chậm, đóng băng).", UnlockRule.ReachNight5, 0, ElementId.Ice);
            Eq("emberheart", EquipmentSlot.Weapon, "Tim Than Hồng", "Đòn đánh của tướng thêm Lửa.", UnlockRule.ReachNight10, 0, ElementId.Fire);
            Eq("tideglaive", EquipmentSlot.Weapon, "Đao Thuỷ Triều", "Đòn đánh của tướng thêm Nước.", UnlockRule.ReachNight10, 0, ElementId.Water);
            Eq("giants_maul", EquipmentSlot.Weapon, "Chuỳ Khổng Lồ", "Tướng +40% sát thương, -20% tốc đánh.", UnlockRule.FirstVictory, 0,
                new Modifier(Stat.Damage, Scope.PlayerGeneral, 0.4f), new Modifier(Stat.AttackSpeed, Scope.PlayerGeneral, -0.2f));
            Eq("swift_rapier", EquipmentSlot.Weapon, "Kiếm Thoăn Thoắt", "Tướng +40% tốc đánh, -15% sát thương.", UnlockRule.MasteryPoints, 40,
                new Modifier(Stat.AttackSpeed, Scope.PlayerGeneral, 0.4f), new Modifier(Stat.Damage, Scope.PlayerGeneral, -0.15f));
            // Relics: change survivability, tempo or the kingdom around the General.
            Eq("phoenix_charm", EquipmentSlot.Relic, "Bùa Phượng Hoàng", "Khi gục ngã, tướng hồi sinh sau 5 giây thay vì 12.", UnlockRule.Default, 0, new HookValue(Hook.FastRevive, 5f));
            Eq("marshal_seal", EquipmentSlot.Relic, "Ấn Thống Soái", "Hào quang tướng mạnh hơn 40%.", UnlockRule.Default, 0, new Modifier(Stat.AuraPower, Scope.PlayerGeneral, 0.4f));
            Eq("golden_hourglass", EquipmentSlot.Relic, "Đồng Hồ Cát Vàng", "Hồi tối thượng -25%.", UnlockRule.ReachNight5, 0, new Modifier(Stat.UltCooldown, Scope.PlayerGeneral, -0.25f));
            Eq("ancestral_ward", EquipmentSlot.Relic, "Khiên Tổ Tiên", "Tướng +30% máu, +3 giáp.", UnlockRule.ReachNight5, 0,
                new Modifier(Stat.MaxHp, Scope.PlayerGeneral, 0.3f), new Modifier(Stat.Armor, Scope.PlayerGeneral, 0f, 3f));
            Eq("windstep_boots", EquipmentSlot.Relic, "Giày Lướt Gió", "Hồi lướt -50%, tướng chạy nhanh hơn 10%.", UnlockRule.ReachNight10, 0,
                new Modifier(Stat.DashCooldown, Scope.PlayerGeneral, -0.5f), new Modifier(Stat.MoveSpeed, Scope.PlayerGeneral, 0.1f));
            Eq("treasure_crown", EquipmentSlot.Relic, "Vương Miện Kho Báu", "Thu Vàng +15%, nhưng tướng -10% sát thương.", UnlockRule.FirstVictory, 0,
                new Modifier(Stat.GoldIncome, Scope.Any, 0.15f), new Modifier(Stat.Damage, Scope.PlayerGeneral, -0.1f));
            Eq("resonance_gem", EquipmentSlot.Relic, "Ngọc Cộng Hưởng", "Sát thương phản ứng +25%.", UnlockRule.Victories2, 0, new Modifier(Stat.ReactionDamage, Scope.Any, 0.25f));
            Eq("seer_lantern", EquipmentSlot.Relic, "Đèn Tiên Tri", "Tướng làm lộ diện kẻ tàng hình trong 4.5 ô và +1 tầm đánh.", UnlockRule.MasteryPoints, 80,
                new HookValue(Hook.SeerLantern, 4.5f), new Modifier(Stat.Range, Scope.PlayerGeneral, 0f, 1f));
        }
    }
}
