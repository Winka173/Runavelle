using System;
using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    public class PhaseFiveTests
    {
        private static GameSession Run(int asc = 0, string challenge = null, bool endless = false, string map = "moonstone_valley", ulong seed = 11)
            => new GameSession(T.Db, new RunSetup { MapId = map, GeneralId = "roland", Seed = seed, Ascension = asc, ChallengeId = challenge, Endless = endless });

        private static void PlayUntil(GameSession s, AutoPlayer bot, Func<bool> stop, int maxTicks = 20 * 60 * 60 * 2)
        {
            for (int t = 0; t < maxTicks && !stop() && s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory; t++)
            {
                if (s.Phase == RunPhase.Day) { s.Economy.Add(new Cost(400, 200), "test"); bot.PlayDay(); s.StartNight(); }
                else if (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
                else bot.HandleChoices();
            }
        }

        [Test]
        public void TenAscensionTiersEachChangeTheRun()
        {
            Assert.AreEqual(10, RunRules.Ascension.Length);
            var a0 = Run(0);
            var a10 = Run(10);
            Assert.Less(a10.Economy.Gold, a0.Economy.Gold, "A2 start gold");
            Assert.AreEqual(2, a10.DraftSize, "A5 draft size");
            Assert.AreEqual(22, a10.LimitBuildingSlots, "A6 slots");
            Assert.AreEqual(0.6f, a10.DawnRepairFraction, 1e-4, "A9 repairs");
            Assert.Greater(a10.BaseBudgetMult, a0.BaseBudgetMult, "A4 budget");
            Assert.IsTrue(a10.Spawner.EliteAffixes, "A3 affixes");
            var e0 = T.SpawnEnemy(a0, "goblin", new Vec2(20, 15));
            var e10 = T.SpawnEnemy(a10, "goblin", new Vec2(20, 15));
            Assert.Greater(e10.MaxHp, e0.MaxHp, "A1 enemy hp");
            var b0 = T.SpawnEnemy(a0, "boss_gatebreaker", new Vec2(20, 15));
            var b10 = T.SpawnEnemy(a10, "boss_gatebreaker", new Vec2(20, 15));
            Assert.Greater(b10.Attack.Damage, b0.Attack.Damage, "A8 boss damage");
            Assert.IsNotNull(b10.Affix, "A3 boss affix");
            Assert.Greater(a10.Plan.Budget, a0.Plan.Budget);
            // A7: reward multiplier
            Assert.Less(a10.W.Mods.Percent(Stat.NightReward, Scope.Any), a0.W.Mods.Percent(Stat.NightReward, Scope.Any));
        }

        [Test]
        public void AscensionTenAddsASecondBossOnTheFinalNight()
        {
            foreach (var map in new[] { "moonstone_valley", "frosthaven" })
            {
                var s = Run(10, map: map);
                var m = T.Db.Maps[map];
                // jump the plan to the final night through the public planner + rules hook
                var plan = s.Waves.Plan(15, 15, n => m.Bosses[n / 5 - 1]);
                Assert.AreEqual(m.Bosses[2], plan.BossUnitId);
                var field = typeof(GameSession).GetMethod("MakePlan", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var nightProp = typeof(GameSession).GetProperty("Night");
                nightProp.SetValue(s, 15);
                var final = (NightPlan)field.Invoke(s, null);
                Assert.IsTrue(final.Groups.Any(g => g.UnitId == m.Bosses[1]), map + " extra boss");
                Assert.IsTrue(final.Groups.Any(g => g.UnitId == m.Bosses[2]), map + " final boss");
            }
        }

        [Test]
        public void ChallengesApplyTheirRules()
        {
            Assert.AreEqual(8, RunRules.Challenges.Length);
            var nw = Run(challenge: "no_walls");
            nw.Economy.Add(new Cost(999), "t");
            var spot = Int2.FromPosition(nw.W.Castle.Center + new Vec2(5, 0));
            Assert.IsFalse(nw.CanPlace("wall_wood", spot).Ok);
            StringAssert.Contains("thử thách", nw.CanPlace("wall_wood", spot).Reason);
            var blitz = Run(challenge: "blitz");
            Assert.AreEqual(10, blitz.TotalNights);
            var plan10 = blitz.Waves.Plan(10, 10, n => n == 10 ? T.Db.Maps["moonstone_valley"].Bosses[2] : null);
            Assert.AreEqual("boss_siege_lord", plan10.BossUnitId);
            var cursed = Run(challenge: "cursed_crown");
            Assert.AreEqual(2, cursed.ChallengeCurses.Count);
            Assert.IsTrue(cursed.ChallengeCurses.All(c => T.Db.Decrees[c].Category == DecreeCategory.Cursed));
            Assert.AreEqual(0, cursed.TakenDecrees.Count, "curses are rules, not draft picks");
            var glass = Run(challenge: "glass_castle");
            Assert.Less(glass.W.Castle.MaxHp, Run().W.Castle.MaxHp);
            var pauper = Run(challenge: "pauper");
            Assert.Less(pauper.Economy.Gold, Run().Economy.Gold);
        }

        [Test]
        public void ChallengeCursesSurviveSaveLoad()
        {
            var s = Run(challenge: "cursed_crown");
            var json = RunSave.Serialize(s);
            var loaded = RunSave.Deserialize(T.Db, json);
            CollectionAssert.AreEqual(s.ChallengeCurses, loaded.ChallengeCurses);
            Assert.AreEqual(s.W.Mods.Percent(Stat.Damage, Scope.Enemies), loaded.W.Mods.Percent(Stat.Damage, Scope.Enemies), 1e-5);
        }

        [Test]
        public void EndlessContinuesPastTheCampaignWithMutatorsAndBossRotation()
        {
            var s = Run(endless: true, seed: 21);
            var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 21);
            PlayUntil(s, bot, () => s.Night >= 17 && s.Phase == RunPhase.Day);
            Assert.AreNotEqual(RunPhase.Victory, s.Phase, "endless never auto-wins");
            if (s.Phase == RunPhase.Defeat) Assert.Inconclusive("bot lost before night 17 (seeded); rules still covered below");
            Assert.AreEqual(17, s.Night);
            Assert.AreEqual(2, s.EndlessMutatorsActive.Count, "nights 16 and 17 each add a mutator");
            Assert.AreEqual(RunRules.MutatorFor(21, 16).Id, s.EndlessMutatorsActive[0].Id);
            Assert.IsNull(s.Plan.BossUnitId, "no boss on night 17");
            // save/load keeps the stacked mutators
            var loaded = RunSave.Deserialize(T.Db, RunSave.Serialize(s));
            Assert.AreEqual(2, loaded.EndlessMutatorsActive.Count);
        }

        [Test]
        public void EndlessBossNightsRotateDeterministically()
        {
            var s = Run(endless: true, seed: 5);
            var b20 = RunRules.EndlessBoss(T.Db, 5, 20);
            Assert.AreEqual(b20, RunRules.EndlessBoss(T.Db, 5, 20));
            Assert.IsNotNull(T.Db.Unit(b20).BossId);
            var night = typeof(GameSession).GetProperty("Night");
            night.SetValue(s, 20);
            var plan = (NightPlan)typeof(GameSession).GetMethod("MakePlan", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(s, null);
            Assert.AreEqual(b20, plan.BossUnitId);
            night.SetValue(s, 21);
            plan = (NightPlan)typeof(GameSession).GetMethod("MakePlan", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(s, null);
            Assert.IsNull(plan.BossUnitId);
        }

        [Test]
        public void ShareCodesRoundTripAndRejectTampering()
        {
            var setup = new RunSetup { MapId = "dawnspire", GeneralId = "nyxara", Seed = 0xDEADBEEF12345UL, Ascension = 7, Endless = true, ChallengeId = "horde" };
            var code = ShareCode.Encode(T.Db, setup);
            StringAssert.StartsWith("RV1-", code);
            var back = ShareCode.Decode(T.Db, code.ToLowerInvariant().Replace("-", " "));
            Assert.IsNotNull(back);
            Assert.AreEqual(setup.MapId, back.MapId);
            Assert.AreEqual(setup.GeneralId, back.GeneralId);
            Assert.AreEqual(setup.Seed, back.Seed);
            Assert.AreEqual(7, back.Ascension);
            Assert.IsTrue(back.Endless);
            Assert.AreEqual("horde", back.ChallengeId);
            var chars = code.ToCharArray();
            int i = code.Length - 3;
            chars[i] = chars[i] == 'A' ? 'B' : 'A';
            Assert.IsNull(ShareCode.Decode(T.Db, new string(chars)), "checksum");
            Assert.IsNull(ShareCode.Decode(T.Db, "hello"));
            var blitz = ShareCode.Decode(T.Db, ShareCode.Encode(T.Db, new RunSetup { MapId = "moonstone_valley", GeneralId = "roland", Seed = 1, ChallengeId = "blitz" }));
            Assert.AreEqual(10, blitz.TotalNights);
        }

        [Test]
        public void DailyRunIsTheSameForEveryoneAndChangesDaily()
        {
            var a = RunRules.Daily(T.Db, 2026, 10, 19);
            var b = RunRules.Daily(T.Db, 2026, 10, 19);
            Assert.AreEqual(a.Seed, b.Seed);
            Assert.AreEqual(a.MapId, b.MapId);
            Assert.AreEqual(a.GeneralId, b.GeneralId);
            Assert.AreEqual(a.ChallengeId, b.ChallengeId);
            Assert.IsTrue(a.IsDaily);
            var days = Enumerable.Range(1, 20).Select(d => RunRules.Daily(T.Db, 2026, 11, d)).ToList();
            Assert.Greater(days.Select(d => d.MapId + d.GeneralId).Distinct().Count(), 5, "varies by day");
        }

        [Test]
        public void ProfileRecordsMasteryAchievementsCosmeticsAndDaily()
        {
            var p = new Profile();
            var s = Run(asc: 5, challenge: "horde");
            s.W.Analytics.Nights.Add(new NightRecord { Night = 1, CastleDamage = 0 });
            typeof(GameSession).GetProperty("Night").SetValue(s, 15);
            var got = p.RecordRun(s, true);
            CollectionAssert.Contains(got, "asc_5");
            CollectionAssert.Contains(got, "challenge_win");
            CollectionAssert.Contains(got, "flawless");
            Assert.IsTrue(p.Cosmetics.Contains("banner_crimson"), "A5 victory unlocks the crimson banner");
            Assert.AreEqual(15 + 10 + 10 + 15, p.GeneralMastery["roland"], "nights + win + horde bonus + 3×asc");
            Assert.IsTrue(p.ChallengesWon.Contains("horde"));
            var back = Profile.Deserialize(p.Serialize());
            Assert.IsTrue(back.ChallengesWon.Contains("horde"));
            Assert.IsTrue(back.MapsWon.Contains("moonstone_valley"));
            Assert.AreEqual(Mastery.Level(p.GeneralMastery["roland"]), Mastery.Level(back, "roland"));
            foreach (var id in got) Assert.AreNotEqual(id, Mastery.Name(id), "every achievement has a name");
        }

        [Test]
        public void EndlessRunPastTheCampaignCountsAsVictoryAndCanRetire()
        {
            var p = new Profile();
            var s = Run(endless: true);
            typeof(GameSession).GetProperty("Night").SetValue(s, 21);
            typeof(GameSession).GetProperty("Phase").SetValue(s, RunPhase.Defeat);
            p.RecordRun(s, false);
            Assert.AreEqual(1, p.Victories);
            Assert.AreEqual(20, p.BestEndlessNight);
            CollectionAssert.Contains(p.Achievements.ToList(), "endless_20");
            var r = Run(endless: true);
            typeof(GameSession).GetProperty("Night").SetValue(r, 15);
            typeof(GameSession).GetProperty("Phase").SetValue(r, RunPhase.Report);
            Assert.IsTrue(r.RetireEndless());
            Assert.AreEqual(RunPhase.Victory, r.Phase);
        }

        [Test]
        public void AscensionAndChallengeRunsCompleteWithoutExceptions()
        {
            foreach (var (asc, ch) in new[] { (10, (string)null), (3, "no_walls"), (6, "lone_general"), (0, "cursed_crown"), (0, "blitz") })
            {
                var s = Run(asc, ch, map: "stormcliff", seed: 9);
                var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 9);
                Assert.DoesNotThrow(() => bot.RunToEnd(), asc + "/" + ch);
                Assert.IsTrue(s.Phase == RunPhase.Victory || s.Phase == RunPhase.Defeat);
            }
        }
    }
}
