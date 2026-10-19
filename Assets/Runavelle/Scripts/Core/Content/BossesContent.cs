using System.Collections.Generic;

namespace Runavelle.Core
{
    public sealed partial class ContentDatabase
    {
        private static BossAbility A(BossAbilityKind k, string name, float cd, float delay, float telegraph, float radius = 2f, float dmg = 0f,
            ElementId e = ElementId.None, float status = 0f, int count = 1, string summon = null, float value = 0f, float exhaust = 0f)
            => new BossAbility { Kind = k, Name = name, Cooldown = cd, InitialDelay = delay, Telegraph = telegraph, Radius = radius, Damage = dmg, Element = e,
                Status = status, Count = count, SummonId = summon, Value = value, ExhaustAfter = exhaust };

        partial void AddExpansionBossesImpl();

        private void AddBosses()
        {
            Bosses.Add("gatebreaker", new BossDefinition
            {
                Id = "gatebreaker", Name = "Gã Phá Cổng Khổng Lồ",
                Counterplay = "Vòng đỏ báo trước cú đập 1.6 giây. Kéo tướng tới đánh trong lúc hắn tụ lực; tường đá chịu được 1 cú.",
                Phases = new List<BossPhase>
                {
                    new BossPhase { Name = "Hành quân", Abilities = { A(BossAbilityKind.Slam, "Cú Đập Phá Thành", 9f, 5f, 1.6f, 2.2f, 170f, value: 1f) } },
                    new BossPhase { Name = "Giận dữ", StartsAtHp = 0.4f, SpeedMult = 1.25f, AttackSpeedMult = 1.2f, Announcement = "Gã Phá Cổng nổi giận!",
                        Abilities = { A(BossAbilityKind.Slam, "Cú Đập Phá Thành", 6.5f, 1f, 1.4f, 2.5f, 190f, value: 1f), A(BossAbilityKind.Charge, "Lao Húc", 12f, 3f, 1.2f, 1.2f, 90f, value: 5f) } },
                },
            });
            Bosses.Add("rift_warlock", new BossDefinition
            {
                Id = "rift_warlock", Name = "Pháp Sư Khe Nứt",
                Counterplay = "Mỗi lần mở khe nứt hắn bị Kiệt sức 4 giây (nhận +50% sát thương). Lính tầm xa và tháp gần mặt trận hắn đi qua sẽ dọn quân triệu hồi.",
                Phases = new List<BossPhase>
                {
                    new BossPhase { Name = "Khe nứt", Abilities = { A(BossAbilityKind.Summon, "Mở Khe Nứt", 14f, 6f, 2.0f, 2.0f, count: 4, summon: "skeleton", exhaust: 4f) } },
                    new BossPhase { Name = "Dịch chuyển", StartsAtHp = 0.5f, Announcement = "Pháp Sư Khe Nứt bẻ cong không gian!",
                        Abilities = { A(BossAbilityKind.Summon, "Mở Khe Nứt", 11f, 2f, 2.0f, 2.0f, count: 5, summon: "goblin", exhaust: 4f),
                                      A(BossAbilityKind.Teleport, "Bước Hư Không", 16f, 5f, 1.0f, value: 6f) } },
                },
            });
            Bosses.Add("siege_lord", new BossDefinition
            {
                Id = "siege_lord", Name = "Chúa Tể Công Thành Tro Tàn",
                Counterplay = "Giai đoạn 2 gọi xe húc và thiên thạch (vòng cam báo trước 2 giây) – rời khỏi vòng. Giai đoạn 3 cuồng nộ: dùng tối thượng và khống chế. Kháng Lửa, yếu Nước/Băng.",
                Phases = new List<BossPhase>
                {
                    new BossPhase { Name = "Hành quân", Abilities = { A(BossAbilityKind.Breath, "Hơi Thở Tro", 10f, 6f, 1.2f, 3f, 70f, ElementId.Fire, 40f) } },
                    new BossPhase { Name = "Công thành", StartsAtHp = 0.66f, Announcement = "Chúa Tể gọi cỗ máy công thành!",
                        Abilities = { A(BossAbilityKind.Summon, "Gọi Xe Húc", 18f, 1f, 2.0f, 2.5f, count: 2, summon: "ram_crew"),
                                      A(BossAbilityKind.Meteor, "Mưa Thiên Thạch", 9f, 4f, 2.0f, 1.6f, 110f, ElementId.Fire, 40f, 3) } },
                    new BossPhase { Name = "Cuồng nộ", StartsAtHp = 0.33f, SpeedMult = 1.4f, AttackSpeedMult = 1.3f, Announcement = "Chúa Tể Tro Tàn cuồng nộ!",
                        Abilities = { A(BossAbilityKind.Meteor, "Mưa Thiên Thạch", 6f, 1f, 1.8f, 1.6f, 120f, ElementId.Fire, 40f, 4),
                                      A(BossAbilityKind.Breath, "Hơi Thở Tro", 8f, 3f, 1.0f, 3.5f, 85f, ElementId.Fire, 40f) } },
                },
            });
            AddExpansionBossesImpl();
        }
    }
}
