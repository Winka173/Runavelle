using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    /// <summary>P3 Breach Crisis: plug-or-fall-back decisions, real movement to the new post, bounded re-path cost, no softlock.</summary>
    public class BreachTests
    {
        /// <summary>Castle ringed by wooden walls (2 tiles out) with a swordsman barracks outside the ring facing the first ground lane.</summary>
        private static (GameSession s, Building barracks, Building gapWall, SpawnLane lane) Ringed(ulong seed = 42)
        {
            var s = T.NewRun("moonstone_valley", "roland", seed);
            s.Economy.Add(new Cost(5000, 5000), "t");
            var lane = s.W.Map.Lanes.First(l => !l.FlyerOnly);
            var c = s.W.Castle;
            var ring = new List<Building>();
            for (int dx = -2; dx <= c.Size + 1; dx++)
                for (int dy = -2; dy <= c.Size + 1; dy++)
                {
                    if (dx > -2 && dx < c.Size + 1 && dy > -2 && dy < c.Size + 1) continue;
                    var b = s.Place("wall_wood", new Int2(c.Origin.X + dx, c.Origin.Y + dy));
                    if (b != null) ring.Add(b);
                }
            var gap = ring.OrderBy(b => Vec2.Distance(b.Center, lane.Center)).First();
            var toLane = (lane.Center - c.Center).Normalized;
            var barracks = s.Place("barracks_swordsman", T.FindSpot(s, "barracks_swordsman", gap.Center + toLane * 3.5f));
            Assert.IsNotNull(barracks);
            return (s, barracks, gap, lane);
        }

        private static void Breach(GameSession s, Building wall) => s.Combat.Kill(wall, DamageInfo.Simple(9999, ElementId.None, 0, SourceKind.Enemy, -1, "test"));

        [Test]
        public void LightPressureSquadPlugsTheGapAndWalksThere()
        {
            var (s, b, gap, _) = Ringed();
            s.StartNight();
            for (int i = 0; i < 20; i++) s.Tick(); // squad spawned and moving
            Breach(s, gap);
            var sw = Stopwatch.StartNew();
            s.Tick(); // decision + flow-field rebuild happen on this tick
            sw.Stop();
            TestContext.WriteLine($"tick after breach: {sw.Elapsed.TotalMilliseconds:0.00} ms (container CPU, not a device)");
            Assert.Less(sw.Elapsed.TotalMilliseconds, 50.0, "re-path after a breach must fit the 20 Hz tick budget");
            var d = s.BreachDecisions.Single(x => x.ActorId == b.Id);
            Assert.AreEqual(BreachOrder.Plug, d.Order, $"power {d.Power} threat {d.Threat}");
            Assert.Less(Vec2.Distance(d.Post, gap.Center), 1.6f);
            var t = Int2.FromPosition(d.Post);
            Assert.IsTrue(s.W.FriendlyPassable(t.X, t.Y));
            for (int i = 0; i < 20 * 8; i++) s.Tick();
            var squad = b.Squad.Select(id => s.W.Get(id) as Unit).Where(u => u != null && !u.Dead).ToList();
            int near = squad.Count(u => Vec2.Distance(u.Position, d.Post) < 2.5f);
            TestContext.WriteLine($"{near}/{squad.Count} at the gap after 8 s");
            Assert.GreaterOrEqual(near, (int)Math.Ceiling(squad.Count * 0.6));
        }

        [Test]
        public void HeavyPressureSquadFallsBackToTheInnerLine()
        {
            var (s, b, gap, lane) = Ringed();
            s.StartNight();
            var outward = (gap.Center - s.W.Castle.Center).Normalized;
            for (int i = 0; i < 12; i++) T.SpawnEnemy(s, "orc_brute", gap.Center + outward * (1.5f + i * 0.1f));
            Breach(s, gap);
            s.Tick();
            var d = s.BreachDecisions.Single(x => x.ActorId == b.Id);
            Assert.AreEqual(BreachOrder.FallBack, d.Order, $"power {d.Power} threat {d.Threat}");
            Assert.Less(Vec2.Distance(d.Post, s.W.Castle.Center), Vec2.Distance(gap.Center, s.W.Castle.Center), "inner line is behind the gap");
            Assert.AreEqual(s.InnerLine(gap.Center).X, d.Post.X, 1e-3);
            s.Tick();
            // the squad's leash anchor is now the inner line (they may still strike enemies that follow them in)
            Assert.IsTrue(b.Squad.Select(id => s.W.Get(id) as Unit).Where(u => u != null && !u.Dead).All(u => Vec2.Distance(u.Home, d.Post) < 1e-3f));
        }

        [Test]
        public void DecisionsInRealRunsRespectTheRule()
        {
            var counts = new Dictionary<BreachOrder, int>();
            foreach (var map in T.Db.MapOrder.Take(6))
            {
                var s = T.NewRun(map, "roland", 21);
                var bot = new AutoPlayer(s, AutoPlayer.Strategy.Army, 21);
                for (int n = 0; n < 10 && s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory; n++)
                {
                    while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat) bot.HandleChoices();
                    if (s.Phase != RunPhase.Day) break;
                    bot.PlayDay(); s.StartNight();
                    while (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
                }
                foreach (var d in s.BreachDecisions)
                {
                    counts[d.Order] = (counts.TryGetValue(d.Order, out int c) ? c : 0) + 1;
                    var t = Int2.FromPosition(d.Post);
                    Assert.IsTrue(s.W.Map.InBounds(t.X, t.Y));
                    if (d.Order == BreachOrder.Plug) Assert.LessOrEqual(d.Threat, d.Power * AiTuning.BreachPlugRatio + 1e-3f);
                    if (d.Order == BreachOrder.FallBack || d.Order == BreachOrder.GeneralInner)
                        Assert.LessOrEqual(Vec2.Distance(d.Post, s.W.Castle.Center), s.W.Castle.Size * 0.5f + AiTuning.InnerLineOffset + 2f);
                }
            }
            TestContext.WriteLine("decisions: " + string.Join(", ", counts.Select(k => k.Key + "=" + k.Value)));
            Assert.Greater(counts.Values.Sum(), 0, "breaches happen in normal play");
        }

        [Test]
        public void SealedCastleDoesNotSoftlockTheNight()
        {
            var (s, _, _, lane) = Ringed(7);
            var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 7);
            s.StartNight();
            for (int i = 0; i < 8; i++) T.SpawnEnemy(s, "orc_brute", lane.Center + new Vec2(i * 0.3f, 0)); // enough pressure to reach the ring
            var last = new Dictionary<int, (Vec2 pos, float since)>();
            int frozen = 0, ticks = 0;
            while (s.Phase == RunPhase.Night && ticks < 20 * 600)
            {
                bot.PlayNightTick(); s.Tick(); ticks++;
                if (ticks % 20 != 0) continue;
                foreach (var e in s.W.Units)
                {
                    if (e.Dead || e.Team != Team.Enemy || e.IsFlying || e.State == UnitState.Burrowed) continue;
                    bool busy = e.State == UnitState.Fighting || e.AttackAnim > 0f || e.Status.Stunned;
                    if (!last.TryGetValue(e.Id, out var l) || busy || Vec2.Distance(l.pos, e.Position) > 0.2f) { last[e.Id] = (e.Position, s.NightTime); continue; }
                    if (s.NightTime - l.since > 15f) { frozen++; last[e.Id] = (e.Position, s.NightTime); TestContext.WriteLine($"frozen {e.Def.Id} at {e.Position.X:0.0},{e.Position.Y:0.0} state {e.State}"); }
                }
            }
            TestContext.WriteLine($"night ended after {ticks / 20f:0}s phase={s.Phase} breaches={s.W.Analytics.Breaches.Count}");
            Assert.AreNotEqual(RunPhase.Night, s.Phase, "the night must resolve");
            Assert.AreEqual(0, frozen, "no enemy idles >15 s without moving or fighting");
            bool wallsHit = s.W.Buildings.Any(b => b.IsWallLike && (b.Ruined || b.Hp < b.MaxHp));
            Assert.IsTrue(wallsHit, "enemies attack the sealing walls instead of waiting");
        }
    }
}
