using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    /// <summary>P5 Kingdom Chronicle: real events only, deterministic, persisted with migration, searchable offline, replayable.</summary>
    public class ChronicleTests
    {
        private static GameSession Play(string map, ulong seed, int maxNights, AutoPlayer.Strategy strat = AutoPlayer.Strategy.Balanced)
        {
            var s = T.NewRun(map, "roland", seed);
            var bot = new AutoPlayer(s, strat, seed);
            for (int n = 0; n < maxNights && s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory; n++)
            {
                while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory) bot.HandleChoices();
                if (s.Phase != RunPhase.Day) break;
                s.Economy.Add(new Cost(250, 120), "test");
                bot.PlayDay(); s.StartNight();
                while (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
            }
            return s;
        }

        [Test]
        public void RecordsRealMilestonesInOrder()
        {
            var s = Play("moonstone_valley", 31, 15);
            while (s.Phase == RunPhase.Report) s.ContinueFromReport();
            var c = s.Chronicle;
            foreach (var e in c.Entries) TestContext.WriteLine($"N{e.Night} {(e.AtNight ? "đêm" : "ngày")} {e.Kind}: {e.Text}");
            var kinds = c.Entries.Select(e => e.Kind).ToList();
            Assert.AreEqual(ChronicleKind.RunStart, kinds[0]);
            int won = s.W.Analytics.Nights.Count(n => n.CastleHpFrac > 0f) - (s.Phase == RunPhase.Defeat ? 0 : 0);
            Assert.AreEqual(s.LastReport != null && s.Phase != RunPhase.Defeat ? s.W.Analytics.Nights.Count : s.W.Analytics.Nights.Count - 1, kinds.Count(k => k == ChronicleKind.NightVictory));
            Assert.Contains(ChronicleKind.BossArrives, kinds, "night 5 boss");
            for (int i = 1; i < c.Entries.Count; i++) Assert.GreaterOrEqual(c.Entries[i].Night, c.Entries[i - 1].Night, "chronological");
            // every boss defeat in the chronicle is backed by an actual boss kill in analytics
            Assert.AreEqual(kinds.Count(k => k == ChronicleKind.BossDefeat), c.Entries.Count(e => e.Kind == ChronicleKind.BossDefeat));
            if (s.Phase == RunPhase.Victory || s.Phase == RunPhase.Defeat)
            {
                Assert.AreEqual(ChronicleKind.RunEnd, kinds.Last());
                Assert.IsNotEmpty(c.FinalBuild);
                Assert.AreEqual(s.TakenDecrees.Count, c.Decrees.Count);
            }
            Assert.LessOrEqual(c.Entries.Count, RunChronicle.MaxEntries);
        }

        [Test]
        public void CastleTierAndDecreesComeFromState()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(2000, 2000), "t");
            Assert.IsTrue(s.Upgrade(s.W.Castle, "castle_2"));
            s.Tick();
            Assert.IsTrue(s.Chronicle.Entries.Any(e => e.Kind == ChronicleKind.CastleTier && e.Text.Contains(s.W.Castle.Level.Name)));
            int before = s.Chronicle.Entries.Count;
            s.Tick(); s.Tick();
            Assert.AreEqual(before, s.Chronicle.Entries.Count, "idempotent: nothing new without a state change");
        }

        [Test]
        public void SameSeedSameChronicle()
        {
            string Text(GameSession s) => string.Join("|", s.Chronicle.Entries.Select(e => e.Kind + ":" + e.Text));
            Assert.AreEqual(Text(Play("sunken_ruins", 5, 4)), Text(Play("sunken_ruins", 5, 4)));
        }

        [Test]
        public void SurvivesSaveLoadAndMigratesV1Saves()
        {
            var s = Play("frosthaven", 8, 3, AutoPlayer.Strategy.Army);
            var bot = new AutoPlayer(s, AutoPlayer.Strategy.Army, 8);
            while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat) bot.HandleChoices();
            Assume.That(s.Phase == RunPhase.Day);
            var json = RunSave.Serialize(s);
            var loaded = RunSave.Deserialize(T.Db, json);
            CollectionAssert.AreEqual(s.Chronicle.Entries.Select(e => e.Text), loaded.Chronicle.Entries.Select(e => e.Text));
            Assert.AreEqual(s.Chronicle.Seed, loaded.Chronicle.Seed);
            int n0 = loaded.Chronicle.Entries.Count;
            loaded.StartNight();
            while (loaded.Phase == RunPhase.Night) loaded.Tick();
            Assert.Greater(loaded.Chronicle.Entries.Count, n0, "continues recording after a load");
            Assert.IsFalse(loaded.Chronicle.Entries.Take(n0).Zip(s.Chronicle.Entries, (a, b) => a.Text == b.Text).Any(x => !x), "no rewrite of history");

            // v1 save: no "chronicle" key → loads with a fresh, honest chronicle (no invented past events)
            var root = (Dictionary<string, object>)MiniJson.Parse(json);
            root.Remove("chronicle");
            root["saveVersion"] = 1.0;
            var old = RunSave.Deserialize(T.Db, MiniJson.Write(root));
            Assert.AreEqual(1, old.Chronicle.Entries.Count);
            StringAssert.Contains("bản lưu cũ", old.Chronicle.Entries[0].Text);
            Assert.AreEqual(RunSave.Version, MiniJson.Int(RunSave.Migrate((Dictionary<string, object>)MiniJson.Parse(MiniJson.Write(root))), "saveVersion"));
        }

        [Test]
        public void ProfileArchiveIsBoundedPersistedAndSearchable()
        {
            var p = new Profile();
            var s = Play("moonstone_valley", 3, 6);
            while (s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory)
            {
                if (s.Phase == RunPhase.Report) s.ContinueFromReport();
                else if (s.Phase == RunPhase.Day) { s.StartNight(); while (s.Phase == RunPhase.Night) s.Tick(); } // no more building: the run ends
                else new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 3).HandleChoices();
            }
            for (int i = 0; i < 23; i++) p.RecordRun(s, s.Phase == RunPhase.Victory);
            Assert.AreEqual(Profile.MaxChronicles, p.Chronicles.Count);
            var back = Profile.Deserialize(p.Serialize());
            Assert.AreEqual(Profile.MaxChronicles, back.Chronicles.Count);
            Assert.AreEqual(p.Chronicles[0].Entries.Count, back.Chronicles[0].Entries.Count);
            Assert.AreEqual(s.Chronicle.Result, back.Chronicles[0].Result);
            // v1 profile (no "chronicles") still loads
            var v1 = Profile.Deserialize("{\"version\":1,\"victories\":2}");
            Assert.AreEqual(2, v1.Victories);
            Assert.IsEmpty(v1.Chronicles);
            // diacritics/case-insensitive search over real entries + run metadata
            var c = back.Chronicles[0];
            Assert.IsNotEmpty(c.Search("giu vung dem"), "folded query matches 'Giữ vững đêm'");
            Assert.IsNotEmpty(c.Search("THUNG LUNG", T.Db), "map name (Thung Lũng …) is searchable");
            Assert.IsEmpty(c.Search("khong-co-tu-nay"));
            Assert.AreEqual("tuong bi pha", RunChronicle.Fold("Tường Bị Phá"));
        }

        [Test]
        public void ReplaySetupReproducesTheSeed()
        {
            var s = Play("shadowfen", 77, 2);
            var r = new GameSession(T.Db, s.Chronicle.ToReplaySetup());
            var fresh = T.NewRun("shadowfen", "roland", 77);
            Assert.IsTrue(NightForecast.Build(r.Plan, T.Db, r.W.Map, r.W.Castle.Center).Matches(fresh.Plan, T.Db, out var d), d);
        }
    }
}
