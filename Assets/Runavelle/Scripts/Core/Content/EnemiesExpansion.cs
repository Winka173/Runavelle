namespace Runavelle.Core
{
    /// <summary>Expansion enemies. Phase II adds the Shadow Court (Light + Shadow "eclipse" faction); Phase IV adds the rest.</summary>
    public sealed partial class ContentDatabase
    {
        partial void AddExpansionEnemiesImpl()
        {
            AddShadowCourtPhase2();
            AddFactionsPhase4();
        }

        partial void AddFactionsPhase4Impl();
        private void AddFactionsPhase4() => AddFactionsPhase4Impl();

        private void AddShadowCourtPhase2()
        {
            var S = Faction.ShadowCourt;
            var stalker = Enemy("shade_stalker", "Bóng Ma Rình Rập", EnemyBehavior.Assassin, S,
                "TÀNG HÌNH: chỉ lộ diện khi tới gần lính/tháp hoặc trúng Ánh Sáng. Lẻn tới phá tháp bằng dao Bóng Tối.", 70, 1, 3.2f, 0.27f, 4, 6, 3,
                new AttackSpec { Damage = 14, Interval = 0.8f, Range = 0.9f, Element = ElementId.Shadow, StatusAmount = 24f, StructureMult = 1.6f }, "stealth", "assassin");
            stalker.StealthRange = 2.6f;
            stalker.ElementTaken = Taken((ElementId.Light, 1.5f), (ElementId.Shadow, 0.6f));
            var cultist = Enemy("eclipse_cultist", "Giáo Sĩ Nhật Thực", EnemyBehavior.Buffer, S,
                "Nguyền Ấn ký lên người phòng thủ (họ nhận thêm sát thương). Kẻ địch quanh nó thêm giáp.", 85, 1, 1.8f, 0.27f, 5, 7, 3,
                new AttackSpec { Damage = 9, Interval = 1.5f, Range = 4f, ProjectileSpeed = 9f, Element = ElementId.Shadow, StatusAmount = 40f, HitsAir = true }, "caster", "curse");
            cultist.AuraRadius = 3f; cultist.AuraArmorBonus = 3f; cultist.AuraSpeedMult = 1f;
            cultist.ElementTaken = Taken((ElementId.Light, 1.4f), (ElementId.Shadow, 0.7f));
            var templar = Enemy("fallen_templar", "Thánh Binh Sa Ngã", EnemyBehavior.ShieldCarrier, S,
                "Khiên Ánh Sáng: kẻ địch quanh nó giảm 30% sát thương nhận. Kháng Ánh Sáng, yếu Bóng Tối.", 230, 6, 1.6f, 0.36f, 7, 8, 5,
                new AttackSpec { Damage = 18, Interval = 1.3f, Range = 1.0f, Element = ElementId.Light, StatusAmount = 22f, StructureMult = 1.2f }, "shield", "heavy");
            templar.Large = true; templar.AuraRadius = 2.6f;
            templar.ElementTaken = Taken((ElementId.Light, 0.5f), (ElementId.Shadow, 1.45f));
        }
    }
}
