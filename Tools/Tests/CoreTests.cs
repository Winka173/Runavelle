using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    public static class T
    {
        public static readonly ContentDatabase Db = ContentDatabase.Create();

        public static GameSession NewRun(string map = "moonstone_valley", string general = "roland", ulong seed = 42)
            => new GameSession(Db, new RunSetup { MapId = map, GeneralId = general, Seed = seed });

        public static Int2 FindSpot(GameSession s, string level, Vec2 near)
        {
            var c = Int2.FromPosition(near);
            for (int r = 1; r < 12; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                        if (s.CanPlace(level, new Int2(c.X + dx, c.Y + dy)).Ok) return new Int2(c.X + dx, c.Y + dy);
            throw new Exception("no spot for " + level);
        }

        public static Unit SpawnEnemy(GameSession s, string id, Vec2 p) => s.Spawner.SpawnEnemy(s.Content.Unit(id), p, s.W.Map.Lanes[0].Id, 1f);
    }

    public class DesignLockTests
    {
        [Test] public void ContentValidates() => Assert.DoesNotThrow(() => ContentDatabase.Create().Validate());

        [Test]
        public void ExactlyEightBuildingFamilies()
        {
            Assert.AreEqual(8, Enum.GetValues(typeof(BuildingFamily)).Length);
            Assert.AreEqual(8, T.Db.Buildings.Count);
            Assert.AreEqual(8, T.Db.Buildings.Values.Select(b => b.Family).Distinct().Count());
        }

        [Test]
        public void ExactlyEightElementsAllowList()
        {
            Assert.AreEqual(9, Enum.GetValues(typeof(ElementId)).Length, "8 elements + None");
            Assert.AreEqual(8, Elements.All.Length);
            Assert.IsFalse(Elements.IsValid(9));
            foreach (var u in T.Db.Units.Values) Assert.IsTrue(Elements.IsValid(u.Attack.Element), u.Id);
            foreach (var l in T.Db.Levels.Values) Assert.IsTrue(Elements.IsValid(l.Element), l.Id);
        }

        [Test]
        public void OnlyTwoCurrencies() => Assert.AreEqual(2, Enum.GetValues(typeof(Currency)).Length);

        [Test]
        public void EveryElementPairResolves()
        {
            int bespoke = 0;
            foreach (var a in Elements.All)
                foreach (var b in Elements.All)
                {
                    if (a == b) { Assert.IsNull(ReactionTable.Get(a, b)); continue; }
                    var r = ReactionTable.Get(a, b);
                    Assert.IsNotNull(r);
                    if (r.Id != ReactionId.Resonance && a < b) bespoke++;
                }
            Assert.That(bespoke, Is.InRange(18, 22));
        }

        [Test]
        public void TiersCappedAtThreeWithOneFinalChoice()
        {
            foreach (var l in T.Db.Levels.Values)
            {
                Assert.LessOrEqual(l.Tier, 3, l.Id);
                if (l.Tier == 2 && l.Next.Length > 0) Assert.LessOrEqual(l.Next.Length, 2, l.Id);
            }
        }

        [Test]
        public void NoFullElementalImmunity()
        {
            foreach (var u in T.Db.Units.Values)
                if (u.ElementTaken != null) Assert.IsTrue(u.ElementTaken.All(x => x > 0f), u.Id);
        }
    }

    public class EconomyTests
    {
        [Test]
        public void NeverNegative()
        {
            var e = new Economy(10, 5);
            Assert.IsFalse(e.TrySpend(new Cost(11, 0), "x"));
            Assert.IsFalse(e.TrySpend(new Cost(-5, 0), "x"));
            e.Add(new Cost(-100, -100), "x");
            Assert.AreEqual(10, e.Gold);
            Assert.AreEqual(5, e.Mana);
            Assert.IsTrue(e.TrySpend(new Cost(10, 5), "x"));
            Assert.AreEqual(0, e.Gold);
            Assert.AreEqual(0, e.Mana);
        }

        [Test]
        public void PlaceSellSameDayFullRefundThenHalf()
        {
            var s = T.NewRun();
            int g0 = s.Economy.Gold;
            var spot = T.FindSpot(s, "house_1", s.W.Castle.Center);
            var b = s.Place("house_1", spot);
            Assert.NotNull(b);
            Assert.AreEqual(g0 - 40, s.Economy.Gold);
            Assert.IsTrue(s.Sell(b));
            Assert.AreEqual(g0, s.Economy.Gold);
            b = s.Place("house_1", spot);
            s.UndoStack.Clear();
            Assert.IsTrue(s.Sell(b));
            Assert.AreEqual(g0 - 20, s.Economy.Gold);
        }

        [Test]
        public void CannotBuildAtNightOrNearSpawn()
        {
            var s = T.NewRun();
            var lane = s.W.Map.Lanes[0].Tiles[0];
            Assert.IsFalse(s.CanPlace("house_1", lane).Ok);
            var spot = T.FindSpot(s, "house_1", s.W.Castle.Center);
            s.StartNight();
            Assert.IsFalse(s.CanPlace("house_1", spot).Ok);
        }

        [Test]
        public void UndoRefundsLastPlacement()
        {
            var s = T.NewRun();
            int g0 = s.Economy.Gold;
            s.Place("archer_1", T.FindSpot(s, "archer_1", s.W.Castle.Center + new Vec2(5, 0)));
            Assert.IsTrue(s.UndoLast());
            Assert.AreEqual(g0, s.Economy.Gold);
        }
    }

    public class WallAndPathTests
    {
        [Test]
        public void WallLineIsContiguousLShape()
        {
            var line = GameSession.WallLine(new Int2(2, 2), new Int2(6, 4));
            Assert.AreEqual(7, line.Count);
            for (int i = 1; i < line.Count; i++) Assert.AreEqual(1, Math.Abs(line[i].X - line[i - 1].X) + Math.Abs(line[i].Y - line[i - 1].Y));
        }

        [Test]
        public void GateLetsFriendliesThroughButNotEnemies()
        {
            var s = T.NewRun();
            var spot = T.FindSpot(s, "gate_wood", s.W.Castle.Center + new Vec2(6, 0));
            var gate = s.Place("gate_wood", spot);
            Assert.IsTrue(s.W.FriendlyPassable(spot.X, spot.Y));
            var enemy = T.SpawnEnemy(s, "goblin", spot.Center + new Vec2(1, 0));
            Assert.IsFalse(s.Combat.PositionWalkable(enemy, spot.Center));
            Assert.IsTrue(gate.IsGate);
        }

        [Test]
        public void SealedCastleStillHasBreachRoute()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(5000), "test");
            var c = s.W.Castle.Origin;
            // ring of walls around the castle at distance 2
            s.PlaceWallLine("wall_wood", new Int2(c.X - 2, c.Y - 2), new Int2(c.X + 4, c.Y - 2));
            s.PlaceWallLine("wall_wood", new Int2(c.X + 4, c.Y - 2), new Int2(c.X + 4, c.Y + 4));
            s.PlaceWallLine("wall_wood", new Int2(c.X + 4, c.Y + 4), new Int2(c.X - 2, c.Y + 4));
            s.PlaceWallLine("wall_wood", new Int2(c.X - 2, c.Y + 4), new Int2(c.X - 2, c.Y - 2));
            foreach (var lane in s.W.Map.Lanes.Where(l => !l.FlyerOnly))
            {
                float d = s.Nav.DistanceToCastle(lane.Tiles[0].Center);
                Assert.IsFalse(float.IsInfinity(d), "lane " + lane.Id + " must still have a (breach) route");
            }
            // an enemy next to the ring will target a wall rather than freeze
            var e = T.SpawnEnemy(s, "goblin", new Vec2(c.X - 3.5f, c.Y + 1.5f));
            s.StartNight();
            for (int i = 0; i < 200; i++) s.Tick();
            Assert.IsTrue(s.W.Get(e.TargetId) is Building || e.Dead, "enemy should attack a wall when sealed out");
        }

        [Test]
        public void TopologyRebuildOnlyOnChange()
        {
            var s = T.NewRun();
            s.Nav.DistanceToCastle(new Vec2(30, 15));
            int r0 = s.Nav.Rebuilds;
            for (int i = 0; i < 50; i++) s.Nav.DistanceToCastle(new Vec2(30, 15));
            Assert.AreEqual(r0, s.Nav.Rebuilds);
            s.Place("wall_wood", T.FindSpot(s, "wall_wood", new Vec2(30, 15)));
            s.Nav.DistanceToCastle(new Vec2(30, 15));
            Assert.AreEqual(r0 + 1, s.Nav.Rebuilds);
        }
    }

    public class StatusReactionTests
    {
        private static (GameSession s, Unit e) Setup()
        {
            var s = T.NewRun();
            s.StartNight();
            var e = T.SpawnEnemy(s, "orc_brute", s.W.Castle.Center + new Vec2(8, 0));
            e.MaxHp = e.Hp = 100000;
            s.W.RebuildSpatial();
            return (s, e);
        }

        private static DamageInfo Hit(ElementId el, float status, int gen = 0)
        {
            var d = DamageInfo.Simple(10, el, status, SourceKind.MagicTower, -1, "test");
            d.Generation = gen;
            return d;
        }

        [Test]
        public void IceBuildupFreezesThenDecays()
        {
            var (s, e) = Setup();
            for (int i = 0; i < 4; i++) s.Combat.Deal(e, Hit(ElementId.Ice, 30));
            Assert.IsTrue(e.Status.Frozen);
            float g = e.Status.Gauge[(int)ElementId.Ice];
            e.Status.Tick(1f, 1f);
            Assert.Less(e.Status.Gauge[(int)ElementId.Ice], g);
        }

        [Test]
        public void WetPlusLightningElectrocharges()
        {
            var (s, e) = Setup();
            s.Combat.Deal(e, Hit(ElementId.Water, 40));
            s.Combat.Deal(e, Hit(ElementId.Lightning, 30));
            Assert.AreEqual(1, s.W.Analytics.Reactions[ReactionId.Electrocharge]);
        }

        [Test]
        public void ReactionOrderIndependent()
        {
            var (s1, e1) = Setup();
            s1.Combat.Deal(e1, Hit(ElementId.Fire, 40));
            s1.Combat.Deal(e1, Hit(ElementId.Water, 40));
            var (s2, e2) = Setup();
            s2.Combat.Deal(e2, Hit(ElementId.Water, 40));
            s2.Combat.Deal(e2, Hit(ElementId.Fire, 40));
            Assert.AreEqual(1, s1.W.Analytics.Reactions[ReactionId.SteamBurst]);
            Assert.AreEqual(1, s2.W.Analytics.Reactions[ReactionId.SteamBurst]);
        }

        [Test]
        public void ReactionGeneratedDamageCannotChain()
        {
            var (s, e) = Setup();
            s.Combat.Deal(e, Hit(ElementId.Water, 40));
            s.Combat.Deal(e, Hit(ElementId.Lightning, 30, gen: 1));
            Assert.IsFalse(s.W.Analytics.Reactions.ContainsKey(ReactionId.Electrocharge));
        }

        [Test]
        public void InternalCooldownAndBudgetCapReactions()
        {
            var (s, e) = Setup();
            for (int i = 0; i < 10; i++) { s.Combat.Deal(e, Hit(ElementId.Water, 40)); s.Combat.Deal(e, Hit(ElementId.Lightning, 40)); }
            Assert.AreEqual(1, s.W.Analytics.Reactions[ReactionId.Electrocharge], "per-target ICD");
            var many = new List<Unit>();
            for (int i = 0; i < 60; i++) many.Add(T.SpawnEnemy(s, "goblin", s.W.Castle.Center + new Vec2(10 + i * 0.01f, 3)));
            s.W.ReactionsThisTick = 0;
            foreach (var m in many) { m.MaxHp = m.Hp = 10000; s.Combat.Deal(m, Hit(ElementId.Fire, 40)); s.Combat.Deal(m, Hit(ElementId.Water, 40)); }
            Assert.LessOrEqual(s.W.ReactionsThisTick, World.MaxReactionsPerTick);
        }

        [Test]
        public void LightShadowEclipseExposes()
        {
            var (s, e) = Setup();
            s.Combat.Deal(e, Hit(ElementId.Light, 40));
            s.Combat.Deal(e, Hit(ElementId.Shadow, 40));
            Assert.AreEqual(1, s.W.Analytics.Reactions[ReactionId.Eclipse]);
            Assert.GreaterOrEqual(e.Status.VulnAmount, 0.4f);
        }

        [Test]
        public void BossControlDiminishes()
        {
            var st = new StatusState();
            float a = st.CcDuration(2f, 0.6f);
            float b = st.CcDuration(2f, 0.6f);
            Assert.Less(b, a);
            Assert.Greater(b, 0f);
        }

        [Test]
        public void ElementalDamageIgnoresArmorPhysicalDoesNot()
        {
            var (s, _) = Setup();
            var sb = T.SpawnEnemy(s, "shieldbearer", s.W.Castle.Center + new Vec2(6, 4));
            float phys = s.Combat.Deal(sb, DamageInfo.Simple(20, ElementId.None, 0, SourceKind.ArcherTower, -1, "t"));
            float fire = s.Combat.Deal(sb, DamageInfo.Simple(20, ElementId.Fire, 0, SourceKind.MagicTower, -1, "t"));
            Assert.Less(phys, fire);
        }
    }

    public class RunTests
    {
        [Test]
        public void DeterministicForSameSeed()
        {
            string Run(ulong seed)
            {
                var s = T.NewRun("sunken_ruins", "lyra", seed);
                var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, seed);
                for (int n = 0; n < 4 && s.Phase != RunPhase.Defeat; n++)
                {
                    while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat) bot.HandleChoices();
                    if (s.Phase == RunPhase.Defeat) break;
                    bot.PlayDay(); s.StartNight();
                    while (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
                }
                return $"{s.Night}/{s.Economy.Gold}/{s.Economy.Mana}/{s.W.Analytics.TotalKills}/{s.W.Tick}/{string.Join(",", s.TakenDecrees)}";
            }
            Assert.AreEqual(Run(7), Run(7));
            Assert.AreNotEqual(Run(7), Run(8));
        }

        [Test]
        public void WavePlansHaveBossesAndValidLanes()
        {
            foreach (var mapId in T.Db.MapOrder)
            {
                var wd = new WaveDirector(T.Db, T.Db.Maps[mapId], 99);
                for (int n = 1; n <= 15; n++)
                {
                    var p = wd.Plan(n, 15);
                    Assert.Greater(p.TotalEnemies, 0);
                    foreach (var g in p.Groups)
                    {
                        var lane = T.Db.Maps[mapId].Lanes.Find(l => l.Id == g.LaneId);
                        Assert.NotNull(lane);
                        if (lane.FlyerOnly) Assert.IsTrue(T.Db.Unit(g.UnitId).Flying, "only flyers on sky lanes");
                    }
                    bool boss = p.Groups.Any(g => T.Db.Unit(g.UnitId).Behavior == EnemyBehavior.Boss);
                    Assert.AreEqual(n == 5 || n == 10 || n == 15, boss, $"{mapId} night {n}");
                }
            }
        }

        [Test]
        public void DraftIsDeterministicDistinctAndEligible()
        {
            var a = T.NewRun(seed: 5); var b = T.NewRun(seed: 5);
            a.GenerateDraft(); b.GenerateDraft();
            CollectionAssert.AreEqual(a.DecreeOffer, b.DecreeOffer);
            Assert.AreEqual(3, a.DecreeOffer.Distinct().Count());
            foreach (var id in a.DecreeOffer) Assert.IsTrue(a.DecreeEligible(T.Db.Decrees[id]));
            Assert.IsFalse(a.DecreeOffer.All(id => T.Db.Decrees[id].Category == DecreeCategory.Cursed));
        }

        [Test]
        public void MapsAreValidWithOneOrTwoProtectedSides()
        {
            foreach (var m in T.Db.Maps.Values) Assert.IsNull(m.Validate(), m.Id);
        }

        [Test]
        public void SaveRoundTripAndBackupFallback()
        {
            var s = T.NewRun("moonstone_valley", "kaelen", 77);
            var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 77);
            for (int n = 0; n < 3; n++)
            {
                while (s.Phase != RunPhase.Day) bot.HandleChoices();
                bot.PlayDay(); s.StartNight();
                while (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
                if (s.Phase == RunPhase.Defeat) Assert.Inconclusive("bot lost early");
            }
            while (!RunSave.CanSave(s)) bot.HandleChoices();
            string json = RunSave.Serialize(s);
            var r = RunSave.Deserialize(T.Db, json);
            Assert.AreEqual(s.Night, r.Night);
            Assert.AreEqual(s.Economy.Gold, r.Economy.Gold);
            Assert.AreEqual(s.Economy.Mana, r.Economy.Mana);
            Assert.AreEqual(s.W.Buildings.Count(b => !b.Dead), r.W.Buildings.Count(b => !b.Dead));
            CollectionAssert.AreEqual(s.TakenDecrees, r.TakenDecrees);
            Assert.AreEqual(s.W.Castle.Level.Id, r.W.Castle.Level.Id);

            string path = Path.Combine(Path.GetTempPath(), "runavelle_test_" + Guid.NewGuid() + ".json");
            SaveStore.Write(path, json);
            SaveStore.Write(path, json + " ");
            File.WriteAllText(path, "{corrupt");
            var back = SaveStore.Read(path, out bool usedBackup);
            Assert.IsTrue(usedBackup);
            Assert.AreEqual(json, back);
            SaveStore.Delete(path);
        }

        [Test]
        public void MigratesVersion0Save()
        {
            var s = T.NewRun();
            var root = (Dictionary<string, object>)MiniJson.Parse(RunSave.Serialize(s));
            root.Remove("phase"); root.Remove("ai"); root["saveVersion"] = 0.0; root["castleTier"] = 2.0;
            var loaded = RunSave.Deserialize(T.Db, MiniJson.Write(root));
            Assert.AreEqual("castle_2", loaded.W.Castle.Level.Id);
            Assert.AreEqual(RunPhase.Day, loaded.Phase);
        }

        [Test]
        public void SoldiersStayLeashedAndGeneralsHoldSectors()
        {
            var s = T.NewRun("sunken_ruins", "lyra", 3);
            var bot = new AutoPlayer(s, AutoPlayer.Strategy.Army, 3);
            int checks = 0;
            float worstSoldier = 0f, worstAi = 0f;
            for (int n = 0; n < 9 && s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory; n++)
            {
                while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat) bot.HandleChoices();
                if (s.Phase == RunPhase.Defeat) break;
                s.Economy.Add(new Cost(300, 100), "test");
                bot.PlayDay(); s.StartNight();
                while (s.Phase == RunPhase.Night)
                {
                    bot.PlayNightTick(); s.Tick();
                    if (s.W.Tick % 10 != 0) continue;
                    foreach (var u in s.W.Units)
                    {
                        if (u.Dead || u.Team != Team.Player) continue;
                        // walking back to the (rally) post is not a chase: only pursuit states count
                        if (u.Role == UnitRole.Soldier && u.State != UnitState.Retreating && u.State != UnitState.Returning)
                            worstSoldier = Math.Max(worstSoldier, Vec2.Distance(u.Position, u.Home) - (u.Leash + u.Attack.Range + 3f));
                        if (u.Role == UnitRole.General && !u.IsPlayerControlled && u.State != UnitState.Retreating && u.State != UnitState.Downed && s.Generals.Posts.TryGetValue(u.AiSlot, out var post))
                            worstAi = Math.Max(worstAi, Vec2.Distance(u.Position, post) - (7.5f + 1f + u.Attack.Range + 3f + 1.5f)); // +1.5: path detours around flooded shallows (Tide)
                        checks++;
                    }
                }
            }
            Assert.Greater(checks, 100);
            Assert.LessOrEqual(worstSoldier, 0.01f, "soldier chased beyond its leash");
            Assert.LessOrEqual(worstAi, 0.01f, "AI general wandered off its sector");
        }

        [Test]
        public void FullRunsCompleteWithoutExceptions()
        {
            foreach (var map in T.Db.MapOrder)
            {
                var s = new GameSession(T.Db, new RunSetup { MapId = map, GeneralId = "kaelen", Seed = 11 });
                var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 11);
                bot.RunToEnd();
                Assert.IsTrue(s.Phase == RunPhase.Victory || s.Phase == RunPhase.Defeat, map);
            }
        }
    }
}
