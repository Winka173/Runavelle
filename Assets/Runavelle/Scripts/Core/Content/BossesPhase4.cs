using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Phase IV bosses: eight more multi-phase bosses, one rotation per map (see MapDefinition.Bosses).</summary>
    public sealed partial class ContentDatabase
    {
        partial void AddExpansionBossesImpl()
        {
            Bosses.Add("siege_titan", new BossDefinition
            {
                Id = "siege_titan", Name = "Người Khổng Lồ Công Thành",
                Counterplay = "Gọi Đá Lăn đập tường – giữ lính tầm xa sau tường. Giai đoạn 2 hắn bật khiên: dùng sát thương phép/phản ứng phá khiên rồi đánh lúc hắn Kiệt sức.",
                Phases = new List<BossPhase>
                {
                    new BossPhase { Name = "Nghiền nát", Abilities = { A(BossAbilityKind.Slam, "Búa Thép", 10f, 5f, 1.7f, 2.0f, 110f, value: 1f),
                                                                       A(BossAbilityKind.Summon, "Thả Đá Lăn", 22f, 10f, 2.0f, 2f, count: 1, summon: "boulder_roller", exhaust: 3f) } },
                    new BossPhase { Name = "Giáp lõi", StartsAtHp = 0.45f, AttackSpeedMult = 1.2f, Announcement = "Người Khổng Lồ bật khiên lõi!",
                        Abilities = { A(BossAbilityKind.ShieldSelf, "Khiên Lõi", 18f, 1f, 1.2f, 1f, value: 0.18f, exhaust: 4f),
                                      A(BossAbilityKind.Charge, "Lao Phá Thành", 11f, 4f, 1.3f, 1.2f, 100f, value: 5f) } },
                },
            });
            Bosses.Add("storm_serpent", new BossDefinition
            {
                Id = "storm_serpent", Name = "Mãng Xà Bão Tố",
                Counterplay = "KẺ BAY: tường không cản được. Đặt tháp chống bay quanh Lâu Đài; cột sét báo trước 2 giây – rời khỏi vòng. Yếu Băng/Đất.",
                Phases = new List<BossPhase>
                {
                    new BossPhase { Name = "Lượn vòng", Abilities = { A(BossAbilityKind.Breath, "Hơi Thở Sấm", 8f, 4f, 1.2f, 2.8f, 60f, ElementId.Lightning, 35f) } },
                    new BossPhase { Name = "Giông tố", StartsAtHp = 0.55f, SpeedMult = 1.2f, Announcement = "Mãng Xà gọi giông tố!",
                        Abilities = { A(BossAbilityKind.Meteor, "Cột Sét", 9f, 2f, 2.0f, 1.4f, 80f, ElementId.Lightning, 35f, 4),
                                      A(BossAbilityKind.Gust, "Cuồng Phong", 12f, 5f, 1.2f, 2.8f, 30f, ElementId.Wind, 25f, value: 1.6f) } },
                },
            });
            Bosses.Add("void_commander", new BossDefinition
            {
                Id = "void_commander", Name = "Thống Lĩnh Hư Không",
                Counterplay = "Gọi Kẻ Rình Bóng tàng hình – cần Ánh Sáng hoặc lính đứng chốt để lộ diện. Giai đoạn 2 hắn chiếm một tháp 5 giây: đừng để hắn lại gần tháp chủ lực.",
                Phases = new List<BossPhase>
                {
                    new BossPhase { Name = "Lệnh bóng tối", Abilities = { A(BossAbilityKind.Summon, "Đội Rình Bóng", 15f, 5f, 1.8f, 2f, count: 3, summon: "shade_stalker", exhaust: 3f),
                                                                          A(BossAbilityKind.CurseZone, "Vùng Nhật Thực", 12f, 7f, 1.6f, 2.4f, 6f, ElementId.Shadow, 20f, value: 6f) } },
                    new BossPhase { Name = "Chiếm đoạt", StartsAtHp = 0.5f, Announcement = "Thống Lĩnh chiếm đoạt tháp canh!",
                        Abilities = { A(BossAbilityKind.DisruptTower, "Chiếm Tháp", 13f, 2f, 1.5f, 7f, 40f, ElementId.Shadow, 30f, value: 5f, exhaust: 3f),
                                      A(BossAbilityKind.Teleport, "Bước Hư Không", 16f, 6f, 1.0f, value: 5f) } },
                },
            });
            Bosses.Add("frost_queen", new BossDefinition
            {
                Id = "frost_queen", Name = "Nữ Hoàng Băng Giá",
                Counterplay = "Đóng băng mọi tháp quanh mình – tướng và lính phải gánh lúc đó. Lửa phá khiên băng ở giai đoạn 3. Ba giai đoạn.",
                Phases = new List<BossPhase>
                {
                    new BossPhase { Name = "Gió bấc", Abilities = { A(BossAbilityKind.Breath, "Hơi Thở Băng", 9f, 5f, 1.3f, 3f, 70f, ElementId.Ice, 45f),
                                                                   A(BossAbilityKind.Summon, "Bầy Sói Tuyết", 16f, 7f, 1.8f, 2f, count: 3, summon: "frost_wolf") } },
                    new BossPhase { Name = "Băng phong", StartsAtHp = 0.66f, Announcement = "Nữ Hoàng phong ấn tháp canh!",
                        Abilities = { A(BossAbilityKind.FreezeTowers, "Băng Phong Tháp", 15f, 1f, 2.0f, 6f, 30f, ElementId.Ice, 30f, value: 4f, exhaust: 3f),
                                      A(BossAbilityKind.Meteor, "Mưa Băng", 10f, 4f, 1.8f, 1.5f, 90f, ElementId.Ice, 40f, 3) } },
                    new BossPhase { Name = "Mùa đông vĩnh cửu", StartsAtHp = 0.33f, SpeedMult = 1.25f, AttackSpeedMult = 1.25f, Announcement = "Mùa đông vĩnh cửu buông xuống!",
                        Abilities = { A(BossAbilityKind.ShieldSelf, "Khiên Băng", 20f, 1f, 1.2f, 1f, value: 0.12f),
                                      A(BossAbilityKind.Meteor, "Mưa Băng", 7f, 2f, 1.6f, 1.6f, 100f, ElementId.Ice, 45f, 4),
                                      A(BossAbilityKind.FreezeTowers, "Băng Phong Tháp", 14f, 6f, 2.0f, 6f, 30f, ElementId.Ice, 30f, value: 3f) } },
                },
            });
            Bosses.Add("corrupted_king", new BossDefinition
            {
                Id = "corrupted_king", Name = "Vị Vua Sa Ngã",
                Counterplay = "Hồi sinh quân đã chết quanh mình – dọn xác bằng Lửa/Ánh Sáng và giết nhanh tay. Giai đoạn 2 chiếm tháp; giai đoạn 3 cuồng nộ. Yếu Ánh Sáng.",
                Phases = new List<BossPhase>
                {
                    new BossPhase { Name = "Triều đình chết", Abilities = { A(BossAbilityKind.Revive, "Gọi Hồn Triều Thần", 14f, 6f, 2.0f, 5f, count: 5, exhaust: 3f),
                                                                           A(BossAbilityKind.Slam, "Vương Trượng", 8f, 4f, 1.4f, 1.8f, 110f, ElementId.Shadow, 30f) } },
                    new BossPhase { Name = "Lời nguyền", StartsAtHp = 0.66f, Announcement = "Vị Vua nguyền rủa vương quốc!",
                        Abilities = { A(BossAbilityKind.DisruptTower, "Chiếm Tháp", 12f, 2f, 1.5f, 7f, 50f, ElementId.Shadow, 30f, value: 5f),
                                      A(BossAbilityKind.Summon, "Đội Cận Vệ Xương", 16f, 6f, 1.8f, 2f, count: 4, summon: "skeleton") } },
                    new BossPhase { Name = "Cuồng nộ", StartsAtHp = 0.3f, SpeedMult = 1.35f, AttackSpeedMult = 1.3f, Announcement = "Vị Vua Sa Ngã cuồng nộ!",
                        Abilities = { A(BossAbilityKind.Revive, "Gọi Hồn Triều Thần", 11f, 1f, 1.8f, 5f, count: 6, exhaust: 3f),
                                      A(BossAbilityKind.CurseZone, "Vương Miện Bóng Tối", 10f, 3f, 1.6f, 2.8f, 8f, ElementId.Shadow, 25f, value: 6f) } },
                },
            });
            Bosses.Add("bone_colossus", new BossDefinition
            {
                Id = "bone_colossus", Name = "Cự Nhân Xương",
                Counterplay = "Mỗi lần đập đất gọi Xác Sống – Lửa/Ánh Sáng ngăn chúng đứng dậy lần hai. Kiệt sức 4 giây sau khi đập. Yếu Lửa.",
                Phases = new List<BossPhase>
                {
                    new BossPhase { Name = "Bước chân nặng", Abilities = { A(BossAbilityKind.Slam, "Dậm Đất", 10f, 5f, 1.6f, 2.4f, 120f, value: 1f, exhaust: 4f),
                                                                          A(BossAbilityKind.Summon, "Mồ Mở", 15f, 8f, 1.8f, 2f, count: 4, summon: "zombie_shambler") } },
                    new BossPhase { Name = "Mưa xương", StartsAtHp = 0.5f, Announcement = "Cự Nhân Xương phun mưa xương!",
                        Abilities = { A(BossAbilityKind.Meteor, "Mưa Xương", 9f, 2f, 1.8f, 1.4f, 85f, count: 4),
                                      A(BossAbilityKind.Slam, "Dậm Đất", 8f, 4f, 1.4f, 2.6f, 140f, value: 1f, exhaust: 4f) } },
                },
            });
            Bosses.Add("tempest_roc", new BossDefinition
            {
                Id = "tempest_roc", Name = "Đại Bàng Cuồng Phong",
                Counterplay = "KẺ BAY: lao thẳng vào tháp. Cuồng phong hất lính ra xa – đừng dồn lính một chỗ. Giai đoạn 2 gọi bầy Nữ Yêu Bão. Yếu Băng.",
                Phases = new List<BossPhase>
                {
                    new BossPhase { Name = "Săn mồi", Abilities = { A(BossAbilityKind.Gust, "Cánh Cuồng Phong", 9f, 4f, 1.2f, 3f, 45f, ElementId.Wind, 30f, value: 2f) } },
                    new BossPhase { Name = "Bầy đàn", StartsAtHp = 0.5f, SpeedMult = 1.2f, Announcement = "Đại Bàng gọi bầy Nữ Yêu Bão!",
                        Abilities = { A(BossAbilityKind.Summon, "Gọi Bầy", 14f, 1f, 1.6f, 2f, count: 4, summon: "storm_harpy", exhaust: 3f),
                                      A(BossAbilityKind.Breath, "Lốc Xoáy Sét", 9f, 4f, 1.2f, 2.6f, 70f, ElementId.Lightning, 35f) } },
                },
            });
            Bosses.Add("hollow_emperor", new BossDefinition
            {
                Id = "hollow_emperor", Name = "Hoàng Đế Hư Vô",
                Counterplay = "Trùm cuối kết hợp mọi cơ chế: triệu quân, chiếm tháp, đóng băng tháp, mưa thiên thạch. Ba giai đoạn – giữ tối thượng cho giai đoạn 3 và tấn công lúc hắn Kiệt sức.",
                Phases = new List<BossPhase>
                {
                    new BossPhase { Name = "Đế chế rỗng", Abilities = { A(BossAbilityKind.Summon, "Quân Đoàn Hư Vô", 15f, 5f, 1.8f, 2f, count: 4, summon: "eclipse_cultist", exhaust: 3f),
                                                                       A(BossAbilityKind.Slam, "Ấn Đế Vương", 9f, 4f, 1.5f, 2.2f, 140f, ElementId.Shadow, 30f, value: 1f) } },
                    new BossPhase { Name = "Thâu tóm", StartsAtHp = 0.66f, Announcement = "Hoàng Đế thâu tóm tháp canh!",
                        Abilities = { A(BossAbilityKind.DisruptTower, "Chiếm Tháp", 12f, 1f, 1.5f, 7f, 60f, ElementId.Shadow, 30f, value: 5f),
                                      A(BossAbilityKind.FreezeTowers, "Hư Không Phong Ấn", 16f, 5f, 2.0f, 6f, 30f, ElementId.Shadow, 30f, value: 3f),
                                      A(BossAbilityKind.Meteor, "Sao Rơi Hư Vô", 10f, 3f, 2.0f, 1.6f, 110f, ElementId.Shadow, 35f, 3) } },
                    new BossPhase { Name = "Nhật thực toàn phần", StartsAtHp = 0.33f, SpeedMult = 1.3f, AttackSpeedMult = 1.3f, DamageTakenMult = 0.9f, Announcement = "Nhật thực toàn phần!",
                        Abilities = { A(BossAbilityKind.Meteor, "Sao Rơi Hư Vô", 6f, 1f, 1.8f, 1.6f, 120f, ElementId.Shadow, 40f, 4),
                                      A(BossAbilityKind.CurseZone, "Màn Nhật Thực", 11f, 3f, 1.6f, 3f, 8f, ElementId.Shadow, 25f, value: 6f),
                                      A(BossAbilityKind.Revive, "Hồi Sinh Quân Đoàn", 16f, 6f, 2.0f, 5f, count: 5, exhaust: 4f) } },
                },
            });
            AddPhase4BossBodies();
        }

        private void AddPhase4BossBodies()
        {
            var K = Faction.SiegeConstructs; var W = Faction.WildTempest; var S = Faction.ShadowCourt; var F = Faction.Frostbound; var U = Faction.UndeadLegion;
            UnitDefinition Body(string bossId, string name, Faction f, string desc, float hp, float armor, float speed, float radius, int bounty, AttackSpec atk, float cc, bool flying = false)
            {
                var b = Enemy("boss_" + bossId, name, EnemyBehavior.Boss, f, desc, hp, armor, speed, radius, 30, 99, bounty, atk, "boss");
                b.Large = true; b.CcResist = cc; b.BossId = bossId;
                if (flying) b.Move = MoveKind.Flying;
                return b;
            }
            Body("siege_titan", "Người Khổng Lồ Công Thành", K, "Boss đêm 5. Búa thép phá tường, thả Đá Lăn; giai đoạn 2 bật khiên lõi.", 1150, 6, 1.2f, 0.6f, 30,
                new AttackSpec { Damage = 36, Interval = 2.0f, Range = 1.3f, StructureMult = 1.8f }, 0.6f).ElementTaken = Taken((ElementId.Lightning, 1.3f), (ElementId.Fire, 0.8f));
            Body("storm_serpent", "Mãng Xà Bão Tố", W, "Boss đêm 10 (KẺ BAY). Hơi thở sấm, cột sét, cuồng phong.", 2100, 3, 1.6f, 0.55f, 40,
                new AttackSpec { Damage = 24, Interval = 1.4f, Range = 3.5f, ProjectileSpeed = 10f, Element = ElementId.Lightning, StatusAmount = 30f, HitsAir = true }, 0.6f, true)
                .ElementTaken = Taken((ElementId.Ice, 1.3f), (ElementId.Earth, 1.25f), (ElementId.Lightning, 0.5f));
            Body("void_commander", "Thống Lĩnh Hư Không", S, "Boss đêm 10. Gọi Kẻ Rình Bóng tàng hình, chiếm tháp canh.", 2300, 5, 1.4f, 0.5f, 40,
                new AttackSpec { Damage = 26, Interval = 1.3f, Range = 1.3f, Element = ElementId.Shadow, StatusAmount = 25f, Splash = 0.8f }, 0.6f)
                .ElementTaken = Taken((ElementId.Light, 1.4f), (ElementId.Shadow, 0.6f));
            Body("bone_colossus", "Cự Nhân Xương", U, "Boss đêm 10. Dậm đất gọi Xác Sống, mưa xương.", 2500, 7, 1.1f, 0.65f, 40,
                new AttackSpec { Damage = 38, Interval = 1.8f, Range = 1.4f, StructureMult = 1.6f, Splash = 1.0f }, 0.65f)
                .ElementTaken = Taken((ElementId.Fire, 1.3f), (ElementId.Light, 1.3f), (ElementId.Shadow, 0.6f));
            Body("tempest_roc", "Đại Bàng Cuồng Phong", W, "Boss đêm 10 (KẺ BAY). Cuồng phong hất văng, gọi bầy Nữ Yêu Bão.", 2000, 2, 2.0f, 0.55f, 40,
                new AttackSpec { Damage = 30, Interval = 1.3f, Range = 1.2f, StructureMult = 1.5f, Element = ElementId.Wind, StatusAmount = 25f }, 0.6f, true)
                .ElementTaken = Taken((ElementId.Ice, 1.35f), (ElementId.Wind, 0.5f));
            Body("frost_queen", "Nữ Hoàng Băng Giá", F, "Trùm đêm 15. Ba giai đoạn: gió bấc, băng phong tháp, mùa đông vĩnh cửu.", 4500, 6, 1.3f, 0.6f, 60,
                new AttackSpec { Damage = 30, Interval = 1.4f, Range = 4f, ProjectileSpeed = 9f, Element = ElementId.Ice, StatusAmount = 40f, Splash = 1f, HitsAir = true }, 0.7f)
                .ElementTaken = Taken((ElementId.Fire, 1.35f), (ElementId.Ice, 0.4f));
            Body("corrupted_king", "Vị Vua Sa Ngã", U, "Trùm đêm 15. Hồi sinh triều thần, chiếm tháp, cuồng nộ.", 4600, 9, 1.25f, 0.65f, 60,
                new AttackSpec { Damage = 46, Interval = 1.6f, Range = 1.4f, StructureMult = 1.8f, Element = ElementId.Shadow, StatusAmount = 25f, Splash = 1.1f }, 0.7f)
                .ElementTaken = Taken((ElementId.Light, 1.4f), (ElementId.Shadow, 0.5f));
            Body("hollow_emperor", "Hoàng Đế Hư Vô", S, "Trùm cuối kết hợp: triệu quân, chiếm & đóng băng tháp, thiên thạch, hồi sinh.", 5000, 9, 1.25f, 0.7f, 70,
                new AttackSpec { Damage = 48, Interval = 1.5f, Range = 1.5f, StructureMult = 2f, Element = ElementId.Shadow, StatusAmount = 30f, Splash = 1.2f }, 0.75f)
                .ElementTaken = Taken((ElementId.Light, 1.3f), (ElementId.Shadow, 0.5f));
        }
    }
}
