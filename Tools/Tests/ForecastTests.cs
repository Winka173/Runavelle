using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    /// <summary>P2 / validation scenario 4: the night preview must match the wave that actually spawns.</summary>
    public class ForecastTests
    {
        private static void ToDay(GameSession s, AutoPlayer bot)
        {
            while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory) bot.HandleChoices();
        }

        [Test]
        public void ForecastMatchesWhatActuallySpawns_AllMaps()
        {
            int nightsChecked = 0, specials = 0;
            foreach (var mapId in T.Db.MapOrder)
                foreach (ulong seed in new ulong[] { 3, 17 })
                {
                    var s = T.NewRun(mapId, "roland", seed);
                    var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, seed);
                    for (int n = 0; n < 6; n++)
                    {
                        ToDay(s, bot);
                        if (s.Phase != RunPhase.Day) break;
                        s.Economy.Add(new Cost(300, 150), "test"); // keep the run alive so later nights are exercised
                        bot.PlayDay();
                        var f = s.Forecast;
                        Assert.AreEqual(ForecastConfidence.Exact, f.Confidence);
                        Assert.IsTrue(f.Matches(s.Plan, T.Db, out var d0), d0);
                        s.StartNight();
                        var groups = s.Plan.Groups.ToList();
                        var spawned = new Dictionary<SpawnGroup, int>();
                        while (s.Phase == RunPhase.Night)
                        {
                            bot.PlayNightTick(); s.Tick();
                            foreach (var g in groups) if (g.Spawned > (spawned.TryGetValue(g, out int v) ? v : 0)) spawned[g] = g.Spawned;
                        }
                        if (s.Phase == RunPhase.Defeat) break; // a lost night may end before every group spawned
                        // per unit and per lane: what spawned == what was advertised
                        var byUnit = new Dictionary<string, int>();
                        var byLane = new Dictionary<string, int>();
                        foreach (var kv in spawned)
                        {
                            byUnit[kv.Key.UnitId] = (byUnit.TryGetValue(kv.Key.UnitId, out int a) ? a : 0) + kv.Value;
                            byLane[kv.Key.LaneId] = (byLane.TryGetValue(kv.Key.LaneId, out int b) ? b : 0) + kv.Value;
                        }
                        CollectionAssert.AreEquivalent(f.Composition, byUnit, $"{mapId}/{seed} night {f.Night}: unit mix");
                        foreach (var fr in f.Fronts)
                            Assert.AreEqual(fr.Enemies, byLane.TryGetValue(fr.LaneId, out int c) ? c : 0, $"{mapId}/{seed} night {f.Night}: lane {fr.LaneId}");
                        specials += f.Specials.Count;
                        nightsChecked++;
                    }
                }
            TestContext.WriteLine($"nights checked: {nightsChecked}, special threats advertised: {specials}");
            Assert.GreaterOrEqual(nightsChecked, 60);
        }

        [Test]
        public void ReportScreenForecastOfNextNightIsExactWhenNothingCanChangeIt()
        {
            int exact = 0, estimate = 0;
            foreach (var mapId in T.Db.MapOrder.Take(4))
            {
                var s = T.NewRun(mapId, "lyra", 5);
                var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 5);
                for (int n = 0; n < 7; n++)
                {
                    ToDay(s, bot);
                    if (s.Phase != RunPhase.Day) break;
                    s.Economy.Add(new Cost(300, 150), "test");
                    bot.PlayDay(); s.StartNight();
                    while (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
                    if (s.Phase != RunPhase.Report) break;
                    var next = s.ForecastNext();
                    var taken = s.TakenDecrees.Count;
                    ToDay(s, bot);
                    if (s.Phase != RunPhase.Day) break;
                    Assert.AreEqual(s.Night, next.Night);
                    if (next.Confidence == ForecastConfidence.Exact)
                    {
                        exact++;
                        Assert.AreEqual(taken, s.TakenDecrees.Count, "an Exact forecast must not have a decree draft in between");
                        Assert.IsTrue(s.Forecast.Matches(s.Plan, T.Db, out _));
                        Assert.IsTrue(next.Matches(s.Plan, T.Db, out var d), $"{mapId} night {s.Night}: {d}");
                    }
                    else
                    {
                        estimate++;
                        Assert.IsNotEmpty(next.Uncertainty, "Estimate must say why");
                    }
                }
            }
            TestContext.WriteLine($"exact {exact}, estimate {estimate}");
            Assert.Greater(exact, 0);
            Assert.Greater(estimate, 0);
        }

        [Test]
        public void ForecastNextHasNoSideEffects()
        {
            string Run(bool peek)
            {
                var s = T.NewRun("sunken_ruins", "roland", 9);
                var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 9);
                for (int n = 0; n < 3; n++)
                {
                    ToDay(s, bot);
                    if (peek) { s.ForecastNext(); _ = s.Forecast; }
                    bot.PlayDay(); s.StartNight();
                    while (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
                    if (peek && s.Phase == RunPhase.Report) s.ForecastNext();
                }
                return $"{s.Night}/{s.Economy.Gold}/{s.Economy.Mana}/{s.W.Analytics.TotalKills}/{s.W.Tick}/{string.Join(",", s.TakenDecrees)}";
            }
            var a = Run(false);
            TestContext.WriteLine(a);
            Assert.AreEqual(a, Run(true));
        }

        [Test]
        public void FlyersAndBurrowersAreAdvertisedAsWallBypasses()
        {
            var kinds = new HashSet<SpecialThreatKind>();
            foreach (var mapId in T.Db.MapOrder)
            {
                var map = T.Db.Maps[mapId];
                var wd = new WaveDirector(T.Db, map, 77) { Factions = map.Factions };
                for (int night = 1; night <= 15; night++)
                {
                    var plan = wd.Plan(night, 15);
                    var f = NightForecast.Build(plan, T.Db, map, new Vec2(map.CastleOrigin.X + 1.5f, map.CastleOrigin.Y + 1.5f));
                    foreach (var g in plan.Groups)
                    {
                        var u = T.Db.Unit(g.UnitId);
                        if (u.Flying || u.Move == MoveKind.Burrow)
                        {
                            var sp = f.Specials.FirstOrDefault(x => x.UnitId == g.UnitId && x.LaneId == g.LaneId && (x.Kind == SpecialThreatKind.Flyer || x.Kind == SpecialThreatKind.Burrower));
                            Assert.IsNotNull(sp, $"{mapId} n{night}: {g.UnitId} not advertised");
                            Assert.IsTrue(sp.BypassesWalls);
                            Assert.IsTrue(f.Fronts.Any(fr => fr.LaneId == g.LaneId), "bypass lane must be shown as a front (sky lanes included)");
                        }
                        if (u.Behavior == EnemyBehavior.Boss) Assert.AreEqual(plan.BossUnitId ?? g.UnitId, f.BossUnitId ?? g.UnitId);
                    }
                    foreach (var sp in f.Specials) kinds.Add(sp.Kind);
                    Assert.AreEqual(plan.TotalEnemies, f.TotalEnemies);
                }
            }
            TestContext.WriteLine("special kinds seen: " + string.Join(",", kinds));
            Assert.IsTrue(kinds.Contains(SpecialThreatKind.Flyer) && kinds.Contains(SpecialThreatKind.Siege) && kinds.Contains(SpecialThreatKind.Boss));
        }

        [Test]
        public void CompassIsScreenOriented()
        {
            Assert.AreEqual("Đông", NightForecast.Compass(new Vec2(5, 0)));
            Assert.AreEqual("Bắc", NightForecast.Compass(new Vec2(0, 5)));
            Assert.AreEqual("Tây Nam", NightForecast.Compass(new Vec2(-3, -3)));
            Assert.AreEqual("Nam", NightForecast.Compass(new Vec2(0.4f, -5)));
        }
    }
}
