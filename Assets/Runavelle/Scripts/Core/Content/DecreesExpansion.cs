namespace Runavelle.Core
{
    /// <summary>Expansion decrees: Phase II Light/Shadow set; Phase III grows the pool to 80–100.</summary>
    public sealed partial class ContentDatabase
    {
        partial void AddExpansionDecreesImpl()
        {
            AddLightShadowDecrees();
            AddDecreesPhase3();
        }

        partial void AddDecreesPhase3Impl();
        private void AddDecreesPhase3() => AddDecreesPhase3Impl();

        private void AddLightShadowDecrees()
        {
            D("dawn_vigil", "Canh Thức Bình Minh", DecreeCategory.Elemental, Rarity.Common,
                "Kẻ địch Rạng rỡ hồi máu cho đồng minh quanh chúng nhiều gấp 3 khi trúng đòn.", H(Hook.DawnVigil, 2f)).RequireElement = ElementId.Light;
            D("eclipse_pact", "Khế Ước Nhật Thực", DecreeCategory.Elemental, Rarity.Rare,
                "Nhật Thực (Ánh Sáng + Bóng Tối) lan rộng hơn 50% và làm choáng 1 giây.", H(Hook.EclipsePact, 1f)).RequireElement = ElementId.Shadow;
            D("shadow_harvest", "Gặt Hái Bóng Đêm", DecreeCategory.Economy, Rarity.Common,
                "Kẻ địch chết khi đang bị Ấn ký trả thêm 2 Mana (tổng 3).", H(Hook.ShadowHarvest, 2f)).RequireElement = ElementId.Shadow;
            D("radiant_walls", "Tường Rạng Rỡ", DecreeCategory.Fortress, Rarity.Rare,
                "Kẻ đánh vào tường/cổng bị Rạng rỡ (lộ diện kẻ tàng hình, mồi phản ứng Ánh Sáng).", H(Hook.RadiantWalls, 18f)).RequireFamily = (int)BuildingFamily.Wall;
            D("umbral_sacrifice", "Hiến Tế Bóng Đêm", DecreeCategory.Cursed, Rarity.Rare,
                "Lính tử trận nổ bóng tối (45 sát thương + Ấn ký quanh xác), nhưng lính -10% máu.",
                H(Hook.ShadowSacrifice, 45f), M(Stat.MaxHp, Scope.Soldiers, -0.1f)).RequireFamily = (int)BuildingFamily.Barracks;
            D("luminous_arsenal", "Kho Vũ Khí Quang Minh", DecreeCategory.Elemental, Rarity.Common,
                "Mọi đòn Ánh Sáng và Bóng Tối +20% sát thương.", M(Stat.Damage, Scope.Any, 0.2f, el: ElementId.Light), M(Stat.Damage, Scope.Any, 0.2f, el: ElementId.Shadow));
        }
    }
}
