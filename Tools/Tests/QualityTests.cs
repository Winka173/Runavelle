using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    /// <summary>Quality Enhancement (game feel) — engine-free presentation rules and the AI changes they rely on.</summary>
    public class QualityTests
    {
        private static SimEvent Ev(SimEventType t, int actor = -1, float value = 0f, string text = null, ReactionId r = ReactionId.None)
            => new SimEvent { Type = t, ActorId = actor, Value = value, Text = text, Reaction = r };

        // ------------------------------------------------------------------ Phase 1: combat feedback
        [Test]
        public void ImpactTiersReflectGameplayImportance()
        {
            var s = T.NewRun();
            s.StartNight();
            var gob = T.SpawnEnemy(s, "goblin", new Vec2(20, 20));
            var elite = T.SpawnEnemy(s, "dark_champion", new Vec2(20, 21));
            var boss = T.SpawnEnemy(s, "boss_gatebreaker", new Vec2(20, 22));
            Assert.AreEqual(ImpactTier.Light, FeedbackRules.Classify(Ev(SimEventType.Kill, gob.Id), s.W));
            Assert.AreEqual(ImpactTier.Heavy, FeedbackRules.Classify(Ev(SimEventType.Kill, elite.Id), s.W));
            Assert.AreEqual(ImpactTier.Epic, FeedbackRules.Classify(Ev(SimEventType.Kill, boss.Id), s.W));
            Assert.AreEqual(ImpactTier.Epic, FeedbackRules.Classify(Ev(SimEventType.UltimateCast), s.W));
            Assert.AreEqual(ImpactTier.Heavy, FeedbackRules.Classify(Ev(SimEventType.WallBreached), s.W));
            Assert.AreEqual(ImpactTier.Medium, FeedbackRules.Classify(Ev(SimEventType.Reaction, gob.Id, 0, "Melt", ReactionId.Melt), s.W));
            Assert.AreEqual(ImpactTier.Light, FeedbackRules.Classify(Ev(SimEventType.Reaction, gob.Id, 0, "arc"), s.W));
            Assert.AreEqual(ImpactTier.Heavy, FeedbackRules.Classify(Ev(SimEventType.Hit, s.W.Castle.Id, 60f), s.W));
            Assert.AreEqual(ImpactTier.Light, FeedbackRules.Classify(Ev(SimEventType.Hit, gob.Id, 10f), s.W));
            Assert.Greater(FeedbackRules.ShakeFor(ImpactTier.Epic), FeedbackRules.ShakeFor(ImpactTier.Heavy));
            Assert.AreEqual(0f, FeedbackRules.ShakeFor(ImpactTier.Light), "light hits never shake the camera");
            Assert.AreEqual(0f, FeedbackRules.LocalHoldFor(ImpactTier.Medium), "hit-stop only for heavy/epic, and only locally");
        }

        [Test]
        public void FeedbackBudgetCapsEffectsPerTierButNeverDropsEpic()
        {
            var b = new FeedbackBudget();
            int light = 0;
            for (int i = 0; i < 200; i++) if (b.Allow(ImpactTier.Light, 0f)) light++;  // 200 hits in the same instant
            Assert.AreEqual((int)FeedbackBudget.Burst[1], light, "only the burst capacity passes at once");
            int later = 0;
            for (int i = 0; i < 200; i++) if (b.Allow(ImpactTier.Light, 1f)) later++;   // one second later
            Assert.LessOrEqual(later, (int)FeedbackBudget.Burst[1]);
            for (int i = 0; i < 20; i++) Assert.IsTrue(b.Allow(ImpactTier.Epic, 1f), "epic moments are never dropped");
            Assert.IsFalse(b.Allow(ImpactTier.None, 1f));
        }

        [Test]
        public void ShakeIsAggregatedCappedScaledAndCanBeTurnedOff()
        {
            var s = new ShakeAggregator();
            for (int i = 0; i < 30; i++) s.Request(0.28f); // AoE hitting 30 enemies
            Assert.AreEqual(0.28f, s.Value, 1e-5, "simultaneous requests take the max, they do not stack");
            s.Request(5f);
            Assert.AreEqual(ShakeAggregator.Cap, s.Value, 1e-5);
            for (int i = 0; i < 60; i++) s.Update(1f / 60f);
            Assert.Less(s.Value, 0.05f, "damped within a second");
            var half = new ShakeAggregator { Strength = 0.5f };
            half.Request(0.4f);
            Assert.AreEqual(0.2f, half.Value, 1e-5);
            var off = new ShakeAggregator { Strength = 0f };
            off.Request(0.5f);
            Assert.AreEqual(0f, off.Value);
        }

        [Test]
        public void DamageTextAggregatesFiltersAndCaps()
        {
            var d = new DamageTextAggregator();
            for (int i = 0; i < 10; i++) d.Add(7, 5f, DamageTextKind.Normal, ElementId.None, Vec2.Zero);
            Assert.AreEqual(1, d.Active.Count, "ten quick hits on one target = one number");
            Assert.AreEqual(50f, d.Active[0].Value, 1e-4);
            d.Update(DamageTextAggregator.MergeWindow + 0.01f);
            d.Add(7, 5f, DamageTextKind.Normal, ElementId.None, Vec2.Zero);
            Assert.AreEqual(2, d.Active.Count, "after the window a new number starts");
            for (int i = 0; i < 100; i++) d.Add(100 + i, 1f + i, DamageTextKind.Normal, ElementId.None, Vec2.Zero);
            Assert.LessOrEqual(d.Active.Count, DamageTextAggregator.MaxActive);
            var imp = new DamageTextAggregator { Mode = DamageTextMode.Important };
            Assert.IsNull(imp.Add(1, 10f, DamageTextKind.Normal, ElementId.None, Vec2.Zero));
            Assert.IsNotNull(imp.Add(1, 10f, DamageTextKind.Boss, ElementId.None, Vec2.Zero));
            var off = new DamageTextAggregator { Mode = DamageTextMode.Off };
            Assert.IsNull(off.Add(1, 999f, DamageTextKind.Boss, ElementId.None, Vec2.Zero));
        }

        [Test]
        public void EightElementsHaveDistinctSilhouettesAndEveryReactionAShowcase()
        {
            Assert.AreEqual(Elements.ElementCap, ElementGrammar.Elements.Count);
            Assert.AreEqual(8, ElementGrammar.Elements.Values.Select(f => f.Shape).Distinct().Count(), "shape differs, not only colour");
            Assert.AreEqual(8, ElementGrammar.Elements.Values.Select(f => f.Motion).Distinct().Count(), "motion differs too");
            foreach (var e in Elements.All) StringAssert.StartsWith("icon/el_", ElementGrammar.For(e).Icon);
            var bespoke = Enum.GetValues(typeof(ReactionId)).Cast<ReactionId>().Where(r => r != ReactionId.None && r != ReactionId.Resonance).ToList();
            Assert.AreEqual(21, bespoke.Count);
            Assert.AreEqual(21, bespoke.Select(r => ElementGrammar.For(r).Showcase).Distinct().Count(), "every reaction has its own showcase");
            Assert.IsTrue(bespoke.Any(r => ElementGrammar.For(r).Weight >= 2) && bespoke.Any(r => ElementGrammar.For(r).Weight == 1));
        }

        // ------------------------------------------------------------------ Phase 4: music
        [Test]
        public void MusicLayersFollowIntensityWithHysteresisAndDwell()
        {
            var m = new MusicDirector();
            Assert.AreEqual(MusicState.Day, m.Update(0.1f, RunPhase.Day, false, false, 1f, 0));
            Assert.AreEqual(MusicState.Dusk, m.Update(0.1f, RunPhase.Day, true, false, 1f, 0), "dusk switches immediately");
            Assert.AreEqual(MusicState.Fight, m.Update(0.1f, RunPhase.Night, false, false, 1f, 0));
            // castle in danger: ramps up, but respects the dwell time before switching
            MusicState st = MusicState.Fight;
            int switches = 0;
            var prev = st;
            for (int i = 0; i < 600; i++)
            {
                bool flicker = i % 2 == 0; // a signal that flickers every frame must not thrash the track
                st = m.Update(0.05f, RunPhase.Night, false, false, flicker ? 0.25f : 0.45f, flicker ? 8 : 6);
                if (st != prev) switches++;
                prev = st;
            }
            Assert.AreEqual(MusicState.Danger, st);
            Assert.LessOrEqual(switches, 1, "no thrashing on a noisy signal");
            // calm again: drops out of danger only after the exit threshold + dwell
            for (int i = 0; i < 400; i++) st = m.Update(0.05f, RunPhase.Night, false, true, 1f, 0);
            Assert.AreEqual(MusicState.Boss, st);
            m.Relief();
            for (int i = 0; i < 40; i++) st = m.Update(0.05f, RunPhase.Night, false, true, 1f, 0);
            Assert.AreEqual(MusicState.Fight, st, "relief beat after a boss defeat");
            Assert.AreEqual(MusicState.Victory, m.Update(0.05f, RunPhase.Victory, false, false, 1f, 0));
            foreach (MusicState x in Enum.GetValues(typeof(MusicState))) StringAssert.StartsWith("music_", MusicDirector.Track(x));
        }

        // ------------------------------------------------------------------ Phase 7: highlights, barks
        [Test]
        public void HighlightsKeepTopSpacedMomentsInABoundedRingBuffer()
        {
            var s = T.NewRun();
            s.StartNight();
            var rec = new HighlightRecorder();
            var boss = T.SpawnEnemy(s, "boss_gatebreaker", new Vec2(20, 20));
            var elite = T.SpawnEnemy(s, "dark_champion", new Vec2(20, 21));
            rec.Observe(Ev(SimEventType.Kill, elite.Id), s.W, 10f);
            rec.Observe(Ev(SimEventType.Kill, boss.Id), s.W, 11f);   // within 3 s of the elite → elite dropped from top
            rec.Observe(Ev(SimEventType.WallBreached, text: "Tường gỗ"), s.W, 30f);
            rec.Observe(Ev(SimEventType.UltimateCast, text: "Búa Trời"), s.W, 50f);
            for (int i = 0; i < 300; i++) rec.Observe(Ev(SimEventType.UltimateCast, text: "x"), s.W, 100f + i * 5f);
            Assert.LessOrEqual(rec.Count, HighlightRecorder.Capacity);
            var fresh = new HighlightRecorder();
            fresh.Observe(Ev(SimEventType.Kill, elite.Id), s.W, 10f);
            fresh.Observe(Ev(SimEventType.Kill, boss.Id), s.W, 11f);
            fresh.Observe(Ev(SimEventType.WallBreached, text: "Tường gỗ"), s.W, 30f);
            var top = fresh.Top(s.Night, 5);
            Assert.AreEqual(2, top.Count);
            Assert.AreEqual(HighlightKind.BossDefeat, top[0].Kind);
            Assert.AreEqual(HighlightKind.WallBreach, top[1].Kind, "time ordered");
            // castle close call fires once per night
            var cc = new HighlightRecorder();
            s.W.Castle.Hp = s.W.Castle.MaxHp * 0.2f;
            for (int i = 0; i < 5; i++) cc.Observe(Ev(SimEventType.Hit, s.W.Castle.Id, 10f), s.W, 20f + i);
            Assert.AreEqual(1, cc.Top(s.Night, 10).Count(h => h.Kind == HighlightKind.CloseCall));
        }

        [Test]
        public void BarksAreSparseCooldownedAndPrioritised()
        {
            var s = T.NewRun();
            var g = s.Spawner.SpawnGeneral(T.Db.Generals["lyra"], s.W.Castle.Center + new Vec2(3, 0), false, 0, null);
            var b = new BarkDirector();
            Assert.IsTrue(b.Say(BarkTrigger.NightStart, g, 0f));
            Assert.IsFalse(b.Say(BarkTrigger.Ultimate, g, 1f), "one line at a time");
            Assert.IsTrue(b.Say(BarkTrigger.WallBreach, g, 2f), "critical line replaces a minor one");
            b.Update(10f);
            Assert.IsFalse(b.Say(BarkTrigger.WallBreach, g, 10.5f), "per-trigger cooldown");
            Assert.IsTrue(b.Say(BarkTrigger.Ultimate, g, 12f));
            Assert.IsFalse(b.Say(BarkTrigger.NightStart, s.W.PlayerGeneral, 40f), "the player's General never barks");
            b.Enabled = false;
            Assert.IsFalse(b.Say(BarkTrigger.BossDefeat, g, 80f));
            foreach (var kv in BarkDirector.Lines) Assert.GreaterOrEqual(kv.Value.Length, 3, kv.Key.ToString());
        }

        // ------------------------------------------------------------------ Phase 5: milestones
        [Test]
        public void MilestonesFireOnceAndPrimeSkipsResumedProgress()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(3000, 3000), "t");
            var m = new MilestoneTracker();
            m.Prime(s);
            for (int i = 0; i < 3; i++) s.Place("house_1", T.FindSpot(s, "house_1", s.W.Castle.Center + new Vec2(-4, i * 2 - 2)));
            m.Check(s);
            Assert.IsTrue(m.TryPop(out var first) && first.Id == "settlement");
            m.Check(s);
            Assert.IsFalse(m.TryPop(out _), "never twice");
            var resumed = new MilestoneTracker();
            resumed.Prime(s);
            Assert.IsFalse(resumed.TryPop(out _), "a reload does not replay celebrations");
            Assert.IsTrue(resumed.Reached.Contains("settlement"));
        }

        // ------------------------------------------------------------------ Phase 2/6: placement verdict
        [Test]
        public void PlacementWarnsWhenABuildingClosesTheLastOpenRoute()
        {
            var s = T.NewRun("dawnspire");
            s.Economy.Add(new Cost(5000, 5000), "t");
            var m = s.W.Map;
            // dawnspire: the plateau ring leaves 2-wide ramps; fill the west ramp except one tile, then test that tile
            var lane = m.Lanes.First(l => l.Id == "west");
            var ramp = new List<Int2>();
            for (int y = 0; y < m.Height; y++)
                if (m.Get(14, y) != TileType.Mountain && s.W.TerrainPassable(14, y)) ramp.Add(new Int2(14, y));
            Assert.Greater(ramp.Count, 0, "ring has a western ramp");
            Assert.IsFalse(s.WouldBlockOpenRoute("wall_wood", ramp[0]), "walls are intentional blockers");
            for (int i = 0; i < ramp.Count - 1; i++) s.Place("wall_wood", ramp[i]);
            var last = ramp[ramp.Count - 1];
            if (s.CanPlace("archer_1", last).Ok)
                Assert.IsTrue(s.WouldBlockOpenRoute("archer_1", last), "the tower would seal the west route");
            Assert.IsFalse(s.WouldBlockOpenRoute("archer_1", T.FindSpot(s, "archer_1", s.W.Castle.Center + new Vec2(0, 3))));
        }

        // ------------------------------------------------------------------ Phase 3: army AI
        [Test]
        public void RangedSoldiersStepBackFromMeleeThreats()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(2000, 2000), "t");
            var b = s.Place("barracks_archer", T.FindSpot(s, "barracks_archer", s.W.Castle.Center + new Vec2(6, 0)));
            Assert.IsNotNull(b);
            for (int i = 0; i < 100; i++) s.Tick(); // squad walks out to its formation during the day
            s.StartNight();
            var archer = (Unit)s.W.Get(b.Squad[0]);
            var orc = T.SpawnEnemy(s, "orc_brute", archer.Position + new Vec2(0.6f, 0));
            orc.Speed = 0f; orc.Attack.Damage = 0f;
            archer.AttackCooldown = 1f;
            float d0 = Vec2.Distance(archer.Position, orc.Position);
            bool kited = false;
            for (int i = 0; i < 10; i++) { s.W.RebuildSpatial(); s.Defenders.TickSoldier(archer, World.Dt); kited |= archer.AiReason == "Giữ khoảng cách"; }
            Assert.Greater(Vec2.Distance(archer.Position, orc.Position), d0, "archer opened distance");
            Assert.IsTrue(kited);
        }

        [Test]
        public void SquadsDeployOutsideTheirBarracks()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(2000, 2000), "t");
            var b = s.Place("barracks_swordsman", T.FindSpot(s, "barracks_swordsman", s.W.Castle.Center + new Vec2(6, 0)));
            for (int i = 0; i < 60; i++) s.Tick();
            foreach (var id in b.Squad)
            {
                var t = Int2.FromPosition(s.W.Get(id).Position);
                Assert.IsNull(s.W.BuildingAt(t.X, t.Y), "soldier stands outside any building");
            }
            // a soldier somehow inside a footprint walks out
            var u = (Unit)s.W.Get(b.Squad[0]);
            u.Position = b.Center;
            for (int i = 0; i < 60; i++) s.Tick();
            var t2 = Int2.FromPosition(u.Position);
            Assert.IsNull(s.W.BuildingAt(t2.X, t2.Y));
        }

        [Test]
        public void FastSoldiersGiveUpUnreachableTargetsAndReturn()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(4000, 4000), "t");
            var b = s.Place("barracks_cavalry", T.FindSpot(s, "barracks_cavalry", s.W.Castle.Center + new Vec2(7, 0)));
            Assert.IsNotNull(b, "cavalry barracks available");
            s.StartNight();
            b.RallyPost = null; // pursuit-cap mechanics around the barracks anchor (rally posts covered by RallyTests)
            var cav = (Unit)s.W.Get(b.Squad[0]);
            Assume.That(cav.Speed >= AiTuning.FastSpeed);
            var runner = T.SpawnEnemy(s, "wolf_rider", b.Center + new Vec2(cav.Leash - 1f, 0));
            runner.Attack.Damage = 0f;
            float r = cav.Leash - 1f, ang = 0f, angSpeed = cav.Speed * 1.6f / r; // circles inside the leash, faster than the cavalry
            bool gaveUp = false;
            for (int i = 0; i < 20 * 10 && !gaveUp; i++)
            {
                ang += angSpeed * World.Dt;
                runner.Position = b.Center + new Vec2((float)Math.Cos(ang), (float)Math.Sin(ang)) * r;
                s.W.RebuildSpatial();
                s.Defenders.TickSoldier(cav, World.Dt);
                gaveUp = cav.IgnoreTargetId == runner.Id;
            }
            Assert.IsTrue(gaveUp, "pursuit cap hit");
            Assert.AreEqual(UnitState.Returning, cav.State);
        }

        [Test]
        public void SoldiersPrioritiseEnemiesStandingInAFreshBreach()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(2000, 2000), "t");
            var b = s.Place("barracks_swordsman", T.FindSpot(s, "barracks_swordsman", s.W.Castle.Center + new Vec2(6, 0)));
            s.StartNight();
            var sw = (Unit)s.W.Get(b.Squad[0]);
            var near = T.SpawnEnemy(s, "goblin", sw.Position + new Vec2(1.6f, 0));
            var gap = T.SpawnEnemy(s, "goblin", sw.Position + new Vec2(0, 2.1f));
            near.Speed = gap.Speed = 0f;
            s.W.Analytics.Breaches.Add(new BreachRecord { Night = s.Night, X = gap.Position.X, Y = gap.Position.Y, LevelId = "wall_wood" });
            s.W.RebuildSpatial();
            sw.AiDecisionTimer = 0f;
            s.Defenders.TickSoldier(sw, World.Dt);
            Assert.AreEqual(gap.Id, sw.TargetId, "the enemy in the gap outranks a slightly closer one");
        }
    }
}
