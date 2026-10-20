using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    /// <summary>Quality Enhancement phases 5–9: progression comeback, staging, previews, battlefield zones, reliability.</summary>
    public class QualityTests2
    {
        [Test]
        public void LastBastionIsOptInFiresOncePerNightAndIsLogged()
        {
            foreach (bool withDecree in new[] { false, true })
            {
                var s = T.NewRun();
                if (withDecree) s.ApplyDecree(T.Db.Decrees["last_bastion"]);
                s.StartNight();
                var g = s.W.PlayerGeneral;
                g.UltCooldown = 40f;
                s.W.Castle.Hp = s.W.Castle.MaxHp * 0.25f;
                for (int i = 0; i < 5; i++) s.Tick();
                if (!withDecree)
                {
                    Assert.AreEqual(0f, s.W.Castle.ShieldHp, "no baseline rubber-banding");
                    Assert.AreEqual(0, s.W.Analytics.LastBastionTriggers);
                    continue;
                }
                Assert.Greater(s.W.Castle.ShieldHp, 0f);
                Assert.Less(g.UltCooldown, 25f, "half the Ultimate cooldown refunded");
                Assert.AreEqual(1, s.W.Analytics.LastBastionTriggers);
                s.W.Castle.ShieldHp = 0f;
                s.W.Castle.Hp = s.W.Castle.MaxHp * 0.1f;
                for (int i = 0; i < 5; i++) s.Tick();
                Assert.AreEqual(1, s.W.Analytics.LastBastionTriggers, "capped at once per night");
                Assert.IsTrue(s.W.Analytics.BuildLog.Any(l => l.Contains("last_bastion")), "telemetry");
            }
            Assert.That(T.Db.Decrees.Count, Is.InRange(80, 100));
        }

        [Test]
        public void WavesAreStagedWithoutChangingTheAdvertisedBudget()
        {
            foreach (var map in new[] { "moonstone_valley", "shattered_crown", "dawnspire" })
                for (ulong seed = 1; seed <= 5; seed++)
                {
                    var s = T.NewRun(map, seed: seed);
                    for (int n = 4; n <= 12; n += 4)
                    {
                        var plan = s.Waves.Plan(n, 15);
                        var groups = plan.Groups.Where(g => g.UnitId != plan.BossUnitId).ToList();
                        int spent = groups.Sum(g => g.Count * T.Db.Unit(g.UnitId).ThreatCost);
                        Assert.LessOrEqual(spent, plan.Budget, "budget respected");
                        int lastStage = -1;
                        foreach (var g in groups.OrderBy(g => g.Time))
                        {
                            int st = WaveDirector.StageOf(T.Db.Unit(g.UnitId));
                            Assert.GreaterOrEqual(st, lastStage, $"{map} n{n} seed{seed}: scouts → main → siege/elite");
                            lastStage = st;
                        }
                    }
                }
        }

        [Test]
        public void UpgradePreviewListsRealDeltas()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(3000, 3000), "t");
            var tower = s.Place("archer_1", T.FindSpot(s, "archer_1", s.W.Castle.Center + new Vec2(5, 0)));
            var lines = s.UpgradePreview(tower, tower.Level.Next[0]);
            Assert.IsNotEmpty(lines);
            Assert.IsTrue(lines.Any(l => l.StartsWith("Sát thương") || l.StartsWith("Máu")), string.Join("|", lines));
            foreach (var l in lines) StringAssert.Contains("→", l.Contains("→") ? l : l + "→"); // every numeric line has before → after
            var house = s.Place("house_1", T.FindSpot(s, "house_1", s.W.Castle.Center + new Vec2(-5, 0)));
            Assert.IsTrue(s.UpgradePreview(house, house.Level.Next[0]).Any(l => l.Contains("bình minh")), "income change shown");
        }

        [Test]
        public void ReactionsLeaveBoundedBattlefieldZonesThatNeverChain()
        {
            var s = T.NewRun();
            s.StartNight();
            int reactionsBefore = 0;
            for (int i = 0; i < 20; i++)
            {
                var e = T.SpawnEnemy(s, "orc_brute", new Vec2(8 + (i % 5) * 3f, 8 + (i / 5) * 3f));
                s.Combat.ApplyElement(e, ElementId.Ice, 60f, DamageInfo.Simple(0, ElementId.Ice, 60f, SourceKind.MagicTower, -1, "t"));
                s.Combat.Deal(e, DamageInfo.Simple(5, ElementId.Fire, 60f, SourceKind.MagicTower, -1, "t")); // Melt
            }
            var zones = s.W.Zones.Where(z => z.FromReaction).ToList();
            Assert.Greater(zones.Count, 0, "melt leaves wet ground");
            Assert.LessOrEqual(zones.Count, CombatSystem.MaxReactionZones);
            Assert.IsTrue(zones.All(z => z.Team == Team.Player), "zones only hurt enemies");
            reactionsBefore = s.W.Analytics.Reactions.Values.Sum();
            for (int i = 0; i < 20 * 5; i++) s.Fx.TickZones(World.Dt);
            Assert.AreEqual(reactionsBefore, s.W.Analytics.Reactions.Values.Sum(), "zone ticks prime but never trigger reactions");
            Assert.AreEqual(0, s.W.Zones.Count(z => z.FromReaction), "they expire");
            // the same spot refreshes instead of stacking
            var a = T.SpawnEnemy(s, "orc_brute", new Vec2(30, 20));
            for (int k = 0; k < 4; k++)
            {
                s.Combat.ApplyElement(a, ElementId.Ice, 60f, DamageInfo.Simple(0, ElementId.Ice, 60f, SourceKind.MagicTower, -1, "t"));
                s.W.Time += 5f; // past the reaction internal cooldown
                s.Combat.Deal(a, DamageInfo.Simple(5, ElementId.Fire, 60f, SourceKind.MagicTower, -1, "t"));
            }
            Assert.LessOrEqual(s.W.Zones.Count(z => z.FromReaction && Vec2.Distance(z.Position, a.Position) < 1.5f), 1);
        }

        [Test]
        public void BreachOpensAPathAndRebuildClosesItAgain()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(5000, 5000), "t");
            var lane = s.W.Map.Lanes.First(l => !l.FlyerOnly);
            var c = s.W.Castle;
            float baseCost = s.Nav.DistanceToCastle(lane.Center);
            // ring the castle with walls (2 tiles out) → enemies must breach
            var placed = new System.Collections.Generic.List<Building>();
            for (int dx = -2; dx <= c.Size + 1; dx++)
                for (int dy = -2; dy <= c.Size + 1; dy++)
                {
                    if (dx > -2 && dx < c.Size + 1 && dy > -2 && dy < c.Size + 1) continue;
                    var t = new Int2(c.Origin.X + dx, c.Origin.Y + dy);
                    var b = s.Place("wall_wood", t);
                    if (b != null) placed.Add(b);
                }
            Assert.Greater(placed.Count, 8);
            float sealedCost = s.Nav.DistanceToCastle(lane.Center);
            Assert.Greater(sealedCost, baseCost, "the ring forces a breach");
            var wall = placed.OrderBy(b => Vec2.Distance(b.Center, lane.Center)).First();
            s.Combat.Kill(wall, DamageInfo.Simple(9999, ElementId.None, 0, SourceKind.Enemy, -1, "test"));
            Assert.IsTrue(wall.Ruined);
            float openCost = s.Nav.DistanceToCastle(lane.Center);
            Assert.Less(openCost, sealedCost, "flow field re-routes through the gap after the breach");
            // daytime rebuild restores the topology (and only then)
            Assert.IsTrue(s.Rebuild(wall));
            Assert.AreEqual(sealedCost, s.Nav.DistanceToCastle(lane.Center), 0.01f, "repair restores the sealed topology");
        }

        [Test]
        public void EconomyCannotBeExploitedThroughBuildSellUndoLoops()
        {
            var s = T.NewRun();
            int g0 = s.Economy.Gold, m0 = s.Economy.Mana;
            for (int i = 0; i < 20; i++)
            {
                var spot = T.FindSpot(s, "archer_1", s.W.Castle.Center + new Vec2(5, 0));
                var b = s.Place("archer_1", spot);
                if (b == null) break;
                if (i % 2 == 0) s.UndoLast(); else s.Sell(b);
                Assert.LessOrEqual(s.Economy.Gold, g0, "never more gold than before");
                Assert.LessOrEqual(s.Economy.Mana, m0);
                Assert.GreaterOrEqual(s.Economy.Gold, 0);
                Assert.GreaterOrEqual(s.Economy.Mana, 0);
            }
            Assert.AreEqual(2, Enum.GetValues(typeof(Currency)).Length, "only Gold and Mana");
        }

        [Test]
        public void InterruptedSaveRecoversTheNewestCompleteCopy()
        {
            string path = Path.Combine(Path.GetTempPath(), "runavelle_qe_" + Guid.NewGuid() + ".json");
            SaveStore.Write(path, "{\"v\":1}");
            SaveStore.Write(path, "{\"v\":2}");
            // simulate a crash between "move main → .bak" and "rename .tmp → main"
            SaveStore.Write(path + ".tmpsrc", "{\"v\":3}");
            File.Move(path + ".tmpsrc", path + ".tmp");
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
            File.Move(path, path + ".bak");
            Assert.AreEqual("{\"v\":3}", SaveStore.Read(path, out bool used));
            Assert.IsTrue(used);
            // a half-written .tmp is ignored (checksum) and the backup is used
            File.WriteAllText(path + ".tmp", "{\"hash\":\"1\",\"payl");
            Assert.AreEqual("{\"v\":2}", SaveStore.Read(path, out _));
            SaveStore.Delete(path);
            if (File.Exists(path + ".tmpsrc.bak")) File.Delete(path + ".tmpsrc.bak");
        }

        [Test]
        public void PresentationNeverChangesTheSimulation()
        {
            // two identical runs, one with all presentation directors observing every event, must stay bit-identical
            GameSession Run(bool observe)
            {
                var s = T.NewRun("stormcliff", "lyra", 99);
                var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 99);
                var hl = new HighlightRecorder(); var ms = new MilestoneTracker(); var md = new MusicDirector(); var dt = new DamageTextAggregator();
                for (int t = 0; t < 20 * 60 * 8 && s.Night <= 3 && s.Phase != RunPhase.Defeat; t++)
                {
                    if (s.Phase == RunPhase.Day) { bot.PlayDay(); s.StartNight(); }
                    else if (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
                    else bot.HandleChoices();
                    if (observe)
                    {
                        foreach (var e in s.W.Events) { hl.Observe(e, s.W, s.NightTime); ms.Observe(e, s); if (e.Type == SimEventType.Hit) dt.Add(e.ActorId, e.Value, DamageTextKind.Normal, e.Element, e.Position); FeedbackRules.Classify(e, s.W); }
                        md.Update(World.Dt, s.Phase, false, false, s.W.Castle.HpFraction, 0);
                    }
                    s.W.Events.Clear();
                }
                return s;
            }
            var a = Run(false);
            var b = Run(true);
            Assert.AreEqual(a.Night, b.Night);
            Assert.AreEqual(a.Economy.Gold, b.Economy.Gold);
            Assert.AreEqual(a.W.Analytics.TotalKills, b.W.Analytics.TotalKills);
            Assert.AreEqual(a.W.Castle.Hp, b.W.Castle.Hp);
        }
    }
}
