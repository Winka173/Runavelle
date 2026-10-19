using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    public class PhaseFourTests
    {
        private static readonly string[] AllMaps = { "moonstone_valley", "stormcliff", "ember_highlands", "frosthaven", "sunken_ruins", "shadowfen", "dawnspire", "shattered_crown" };

        private static IEnumerable<UnitDefinition> RegularEnemies =>
            T.Db.Units.Values.Where(u => u.Role == UnitRole.Enemy && u.Behavior != EnemyBehavior.Boss && u.UnlockNight < 99);

        [Test]
        public void EightMapsInUnlockOrderAllValid()
        {
            CollectionAssert.AreEqual(AllMaps, T.Db.MapOrder);
            foreach (var id in AllMaps)
            {
                var m = T.Db.Maps[id];
                Assert.IsNull(m.Validate(), id);
                Assert.AreEqual(3, m.Bosses.Length, id);
                foreach (var b in m.Bosses) Assert.IsTrue(T.Db.Units.ContainsKey(b) && T.Db.Units[b].BossId != null, id + " boss " + b);
                Assert.IsNotEmpty(m.Factions, id);
            }
            // every hazard is used by exactly one map (Moonstone is the hazard-free tutorial map)
            var hazards = AllMaps.Select(id => T.Db.Maps[id].Hazard).ToList();
            Assert.AreEqual(hazards.Count, hazards.Distinct().Count());
            Assert.AreEqual(MapHazard.None, T.Db.Maps["moonstone_valley"].Hazard);
        }

        [Test]
        public void FactionsEnemiesAndBossesMeetExpansionScope()
        {
            Assert.That(RegularEnemies.Count(), Is.InRange(38, 46));
            foreach (Faction f in Enum.GetValues(typeof(Faction)))
            {
                if (f == Faction.None) continue;
                Assert.GreaterOrEqual(RegularEnemies.Count(u => u.Faction == f), 4, f + " needs a real roster");
            }
            Assert.That(T.Db.Bosses.Count, Is.InRange(10, 12));
            foreach (var b in T.Db.Bosses.Values)
            {
                Assert.GreaterOrEqual(b.Phases.Count, 2, b.Id);
                Assert.IsFalse(string.IsNullOrEmpty(b.Counterplay), b.Id);
                Assert.IsTrue(T.Db.Units.Values.Any(u => u.BossId == b.Id), b.Id + " has a body");
                foreach (var p in b.Phases) foreach (var a in p.Abilities)
                    if (a.SummonId != null) Assert.IsTrue(T.Db.Units.ContainsKey(a.SummonId), b.Id + " summons " + a.SummonId);
            }
            // every faction appears on at least one map
            foreach (Faction f in Enum.GetValues(typeof(Faction)))
                if (f != Faction.None) Assert.IsTrue(AllMaps.Any(m => T.Db.Maps[m].Factions.Contains(f)), f.ToString());
        }

        [Test]
        public void MapBossRotationDrivesNights5_10_15()
        {
            foreach (var id in AllMaps)
            {
                var s = T.NewRun(id);
                var m = T.Db.Maps[id];
                for (int i = 0; i < 3; i++)
                {
                    int night = (i + 1) * 5;
                    var plan = s.Waves.Plan(night, 15, n => m.Bosses[n / 5 - 1]);
                    Assert.AreEqual(m.Bosses[i], plan.BossUnitId, id + " night " + night);
                }
            }
        }

        [Test]
        public void EarlyNightsAlwaysHaveEnemiesAndAirIsCapped()
        {
            foreach (var id in AllMaps)
                for (ulong seed = 1; seed <= 6; seed++)
                {
                    var s = T.NewRun(id, seed: seed);
                    for (int n = 1; n <= 6; n++)
                    {
                        var plan = s.Waves.Plan(n, 15);
                        Assert.Greater(plan.Groups.Sum(g => g.Count), 0, $"{id} night {n} empty");
                        int flyers = plan.Groups.Where(g => T.Db.Unit(g.UnitId).Flying && g.UnitId != plan.BossUnitId).Sum(g => g.Count);
                        Assert.LessOrEqual(flyers, 2 + n * 3 / 2, $"{id} night {n} seed {seed}");
                    }
                }
        }

        [Test]
        public void HighTideFloodsShallowsButEveryLaneStillReachesTheCastle()
        {
            var s = T.NewRun("sunken_ruins");
            var m = s.W.Map;
            int shallows = Enumerable.Range(0, m.Width * m.Height).Count(i => m.Tiles[i] == TileType.Shallows);
            Assert.Greater(shallows, 10);
            Assert.IsFalse(s.W.TideHigh, "night 1 is low tide");
            s.W.TideHigh = true;
            s.W.TopologyVersion++;
            var sh = Enumerable.Range(0, m.Width * m.Height).First(i => m.Tiles[i] == TileType.Shallows);
            Assert.IsFalse(s.W.TerrainPassable(sh % m.Width, sh / m.Width));
            foreach (var lane in m.Lanes.Where(l => !l.FlyerOnly))
                Assert.IsFalse(float.IsInfinity(s.Nav.DistanceToCastle(lane.Center)), lane.Id + " cut off at high tide");
        }

        [Test]
        public void BomberSelfDestructsAgainstAWallWithoutBounty()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(500), "t");
            var spot = T.FindSpot(s, "wall_wood", s.W.Castle.Center + new Vec2(6, 0));
            var wall = s.Place("wall_wood", spot);
            s.StartNight();
            var runner = T.SpawnEnemy(s, "ember_runner", spot.Center + new Vec2(0.9f, 0));
            runner.TargetId = wall.Id;
            float hp0 = wall.Hp;
            for (int i = 0; i < 40 && !runner.Dead; i++) s.Tick();
            Assert.IsTrue(runner.Dead, "exploded");
            Assert.Less(wall.Hp, hp0, "blast damaged the wall");
            Assert.AreEqual(0, s.W.PendingGold, "suicide pays no bounty");
        }

        [Test]
        public void ZombieRisesOnceUnlessBurned()
        {
            var s = T.NewRun();
            s.StartNight();
            var z = T.SpawnEnemy(s, "zombie_shambler", s.W.Castle.Center + new Vec2(10, 0));
            s.Combat.Deal(z, DamageInfo.Simple(9999, ElementId.None, 0, SourceKind.Soldier, -1, "t"));
            Assert.IsFalse(z.Dead, "rises once");
            Assert.AreEqual(z.MaxHp * 0.4f, z.Hp, 1f);
            s.Combat.Deal(z, DamageInfo.Simple(9999, ElementId.None, 0, SourceKind.Soldier, -1, "t"));
            Assert.IsTrue(z.Dead, "second death is final");
            var z2 = T.SpawnEnemy(s, "zombie_shambler", s.W.Castle.Center + new Vec2(10, 1));
            s.Combat.Deal(z2, DamageInfo.Simple(9999, ElementId.Fire, 0, SourceKind.Soldier, -1, "t"));
            Assert.IsTrue(z2.Dead, "fire prevents rising");
        }

        [Test]
        public void FrostWraithDisablesTowersAndGlacierArmorSlowsMelee()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(500), "t");
            var tower = s.Place("archer_1", T.FindSpot(s, "archer_1", s.W.Castle.Center + new Vec2(6, 0)));
            s.StartNight();
            var wraith = T.SpawnEnemy(s, "frost_wraith", tower.Center + new Vec2(1.5f, 0));
            wraith.TargetId = tower.Id;
            wraith.AttackCooldown = 0f;
            for (int i = 0; i < 10 && tower.DisabledTimer <= 0f; i++) s.Tick();
            Assert.Greater(tower.DisabledTimer, 0f, "tower frozen");

            var golem = T.SpawnEnemy(s, "glacier_golem", s.W.Castle.Center + new Vec2(-8, 0));
            var g = s.W.PlayerGeneral;
            s.Combat.Deal(golem, DamageInfo.Simple(10, ElementId.None, 0, SourceKind.PlayerGeneral, g.Id, "t"));
            if (g.Attack.ProjectileSpeed <= 0f) Assert.Greater(g.Status.SlowAmount, 0f, "melee attacker chilled");
        }

        [Test]
        public void MistHidesGroundEnemiesButNotBossesOrFlyers()
        {
            var s = T.NewRun("shadowfen");
            s.StartNight();
            Assert.IsTrue(T.SpawnEnemy(s, "goblin", new Vec2(3, 14)).Stealthed);
            Assert.IsFalse(T.SpawnEnemy(s, "storm_harpy", new Vec2(3, 14)).Stealthed);
            Assert.IsFalse(T.SpawnEnemy(s, "boss_void_commander", new Vec2(3, 14)).Stealthed);
            var plain = T.NewRun("moonstone_valley");
            Assert.IsFalse(T.SpawnEnemy(plain, "goblin", new Vec2(30, 14)).Stealthed);
        }

        [Test]
        public void BurningEnemyCracksThinIceUntilDawn()
        {
            var s = T.NewRun("frosthaven");
            var m = s.W.Map;
            int idx = Enumerable.Range(0, m.Width * m.Height).First(i => m.Tiles[i] == TileType.Ice);
            var tile = new Int2(idx % m.Width, idx / m.Width);
            s.StartNight();
            var e = T.SpawnEnemy(s, "orc_brute", tile.Center);
            e.Status.Gauge[(int)ElementId.Fire] = 30f;
            for (int i = 0; i < 12; i++) s.Tick();
            Assert.IsTrue(s.W.CrackedIce.Contains(idx), "ice cracked");
            Assert.IsFalse(s.W.TerrainPassable(tile.X, tile.Y));
            Assert.Less(e.Hp, e.MaxHp, "fell into the water");
            s.ApplyDayHazards();
            Assert.IsTrue(s.W.TerrainPassable(tile.X, tile.Y), "refrozen at dawn");
        }

        [Test]
        public void LightningAndVentsStrikeDuringTheNight()
        {
            foreach (var (map, tag) in new[] { ("stormcliff", "lightning"), ("ember_highlands", "vent") })
            {
                var s = T.NewRun(map);
                s.StartNight();
                T.SpawnEnemy(s, "orc_brute", s.W.Castle.Center + new Vec2(9, 0));
                bool seen = false;
                for (int i = 0; i < 20 * 20 && !seen; i++)
                {
                    s.Tick();
                    seen = s.W.Events.Any(ev => ev.Type == SimEventType.Hazard && ev.Text == tag);
                    s.W.Events.Clear();
                }
                Assert.IsTrue(seen, map + " hazard fired");
            }
        }

        [Test]
        public void SideObjectiveIsDeterministicAndPaysWhenMet()
        {
            var a = T.NewRun(seed: 77);
            var b = T.NewRun(seed: 77);
            Assert.IsNotNull(a.Objective);
            Assert.AreEqual(a.Objective.Id, b.Objective.Id);
            var rep = new NightReport { Duration = 10, CastleDamage = 0, SoldiersLost = 0, BuildingsLost = 0 };
            Assert.IsTrue(a.ObjectiveMet(rep), "a flawless quick night meets every objective");
            var bad = new NightReport { Duration = 9999, CastleDamage = 99999, SoldiersLost = 50, BuildingsLost = 9 };
            if (a.Objective.Id != "general_alive") Assert.IsFalse(a.ObjectiveMet(bad));
        }

        [Test]
        public void HousesCannotSealTheGeneralIn()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(5000, 5000), "t");
            int placed = 0, refused = 0;
            var c = Int2.FromPosition(s.W.Castle.Center);
            for (int r = 2; r <= 4; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                        var o = new Int2(c.X + dx, c.Y + dy);
                        var res = s.CanPlace("house_1", o);
                        if (res.Ok) { s.Place("house_1", o); placed++; }
                        else if (res.Reason.Contains("Tướng")) refused++;
                    }
            Assert.Greater(refused, 0, "at least one placement refused to keep an exit");
            var path = new List<Int2>();
            var start = Int2.FromPosition(s.W.PlayerGeneral.Position);
            var lane = s.W.Map.Lanes.First(l => !l.FlyerOnly).Tiles[0];
            Assert.IsTrue(s.Nav.FindFriendlyPath(start, lane, path, 5000), "General can still walk out");
        }

        [Test]
        public void NewMapsSurviveAnAutoplayedOpening()
        {
            foreach (var id in new[] { "stormcliff", "frosthaven", "shadowfen", "dawnspire", "shattered_crown" })
            {
                var s = T.NewRun(id, seed: 5);
                var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 5);
                for (int ticks = 0; ticks < 20 * 60 * 20 && s.Night <= 3 && s.Phase != RunPhase.Defeat; ticks++)
                {
                    if (s.Phase == RunPhase.Day) { bot.PlayDay(); s.StartNight(); }
                    else if (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
                    else bot.HandleChoices();
                }
                Assert.AreNotEqual(RunPhase.Defeat, s.Phase, id + " lost in the first three nights");
            }
        }
    }
}
