using System;
using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    public class PhaseOneTests
    {
        [Test]
        public void RosterTargets()
        {
            Assert.AreEqual(16, T.Db.Generals.Count);
            Assert.AreEqual(12, T.Db.Units.Values.Count(u => u.Role == UnitRole.Soldier));
            Assert.AreEqual(24, T.Db.SoldierBranches.Count);
            foreach (var g in T.Db.Generals.Values)
            {
                Assert.AreEqual(2, g.Evolutions.Length, g.Id);
                Assert.AreNotEqual(g.Evolutions[0].Description, g.Evolutions[1].Description, g.Id);
            }
            foreach (var e in Elements.All) Assert.GreaterOrEqual(T.Db.Generals.Values.Count(g => g.Element == e), 1, e.ToString());
            foreach (GeneralGroup grp in Enum.GetValues(typeof(GeneralGroup))) Assert.GreaterOrEqual(T.Db.Generals.Values.Count(g => g.Group == grp), 2, grp.ToString());
            Assert.AreEqual(T.Db.Generals.Count, T.Db.Generals.Values.Select(g => g.Ultimate.Name).Distinct().Count(), "every ultimate is unique");
        }

        private static (GameSession s, Unit g) Arena(string generalId, GeneralEvolution evo = null)
        {
            var s = new GameSession(T.Db, new RunSetup { MapId = "moonstone_valley", GeneralId = generalId, Seed = 9 });
            s.StartNight();
            var g = s.W.PlayerGeneral;
            g.Position = s.W.Castle.Center + new Vec2(4, 0);
            g.Evolution = evo;
            for (int i = 0; i < 8; i++)
            {
                var e = T.SpawnEnemy(s, i % 2 == 0 ? "orc_brute" : "goblin", g.Position + new Vec2(1.2f + (i % 4) * 0.5f, (i / 4) - 0.5f));
                e.MaxHp = e.Hp = 5000;
            }
            s.W.RebuildSpatial();
            return (s, g);
        }

        private static float Snapshot(GameSession s, out int controlled)
        {
            float hp = 0;
            controlled = 0;
            foreach (var u in s.W.Units)
            {
                if (u.Team != Team.Enemy || u.Dead) continue;
                hp += u.Hp;
                if (u.Status.Frozen || u.Status.VulnAmount > 0 || u.Status.Stunned || u.Status.SlowAmount > 0) controlled++;
            }
            return hp;
        }

        [Test]
        public void EveryUltimateAndEvolutionHasAnEffect()
        {
            foreach (var gd in T.Db.Generals.Values)
            {
                foreach (var evo in new GeneralEvolution[] { null, gd.Evolutions[0], gd.Evolutions[1] })
                {
                    var (s, g) = Arena(gd.Id, evo);
                    s.W.Castle.Hp = s.W.Castle.MaxHp * 0.5f;
                    int buildings0 = s.W.Buildings.Count;
                    float before = Snapshot(s, out _);
                    float castleBefore = s.W.Castle.Hp;
                    Assert.DoesNotThrow(() => s.Generals.CastUltimate(g), gd.Id + "/" + evo?.Id);
                    for (int i = 0; i < 20; i++) s.Tick(); // zones/projectiles resolve
                    float after = Snapshot(s, out int controlled);
                    bool effect = after < before - 1f || controlled > 0 || s.W.Castle.Hp > castleBefore || s.W.CastleGuardTimer > 0 || s.W.Buildings.Count > buildings0;
                    Assert.IsTrue(effect, $"{gd.Id} {evo?.Id} ultimate had no measurable effect");
                }
            }
        }

        [Test]
        public void ShieldGuardiansInterceptProjectiles()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(1000, 1000), "t");
            var b = s.Place("barracks_shieldguardian", T.FindSpot(s, "barracks_shieldguardian", s.W.Castle.Center + new Vec2(6, 0)));
            s.StartNight();
            for (int i = 0; i < 6; i++) { var k = T.SpawnEnemy(s, "kobold_slinger", b.Center + new Vec2(3.5f, i * 0.3f - 1f)); k.MaxHp = k.Hp = 9999; }
            int blocks = 0;
            for (int t = 0; t < 400; t++) { s.Tick(); blocks += s.W.Events.Count(e => e.Text == "block"); s.W.Events.Clear(); }
            Assert.Greater(blocks, 0);
        }

        [Test]
        public void GriffinRidersHuntFlyers()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(1000, 1000), "t");
            var b = s.Place("barracks_griffinrider", T.FindSpot(s, "barracks_griffinrider", s.W.Castle.Center + new Vec2(6, 2)));
            Assert.IsTrue(s.W.Units.Any(u => u.Def.Class == SoldierClass.GriffinRider && u.IsFlying));
            s.StartNight();
            var h = T.SpawnEnemy(s, "harpy", b.Center + new Vec2(3, 0));
            h.MaxHp = h.Hp = 9999;
            for (int t = 0; t < 200; t++) s.Tick();
            Assert.Greater(s.W.Analytics.DamageBySource.Where(k => k.Key.Contains("Sư Ưng") || k.Key.Contains("Kỵ Sĩ Sư Ưng")).Sum(k => k.Value), 0f);
        }

        [Test]
        public void RuneGolemTauntsNearbyEnemies()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(1000, 1000), "t");
            var b = s.Place("barracks_runegolem", T.FindSpot(s, "barracks_runegolem", s.W.Castle.Center + new Vec2(6, -2)));
            var golem = s.W.Units.First(u => u.Def.Class == SoldierClass.RuneGolem);
            s.StartNight();
            var gob = T.SpawnEnemy(s, "goblin", golem.Position + (golem.Position - b.Center).Normalized * 1.2f);
            s.W.RebuildSpatial();
            for (int t = 0; t < 3; t++) s.Tick();
            Assert.AreEqual(golem.Id, gob.TargetId);
        }

        [Test]
        public void BattlemagesCastRangedSpell()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(1000, 1000), "t");
            var b = s.Place("barracks_battlemage", T.FindSpot(s, "barracks_battlemage", s.W.Castle.Center + new Vec2(6, 0)));
            var bm = s.W.Units.First(u => u.Def.Class == SoldierClass.Battlemage);
            Assert.NotNull(bm.SecondaryAttack);
            s.StartNight();
            var e = T.SpawnEnemy(s, "harpy", bm.Position + new Vec2(2.8f, 0)); // flying: melee cannot reach, spell can
            e.MaxHp = e.Hp = 9999;
            int fired = 0;
            for (int t = 0; t < 120; t++) { s.Tick(); fired += s.W.Events.Count(ev => ev.Type == SimEventType.ProjectileFired && ev.ActorId == bm.Id); s.W.Events.Clear(); }
            Assert.Greater(fired, 0);
        }

        [Test]
        public void TwoAiGeneralsPlusPlayerDefendWithoutFreezing()
        {
            var s = new GameSession(T.Db, new RunSetup { MapId = "sunken_ruins", GeneralId = "lyra", Seed = 21 });
            s.Setup.UnlockedGenerals.AddRange(T.Db.GeneralOrder);
            var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 21);
            for (int n = 0; n < 12 && s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory; n++)
            {
                while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat) bot.HandleChoices();
                if (s.Phase == RunPhase.Defeat) break;
                s.Economy.Add(new Cost(250, 120), "test");
                bot.PlayDay();
                s.StartNight();
                int guard = 0;
                while (s.Phase == RunPhase.Night && guard++ < 20 * 900) { bot.PlayNightTick(); s.Tick(); }
                Assert.Less(guard, 20 * 900, "night must end (no stall)");
            }
            Assert.AreEqual(2, s.W.AIGenerals.Count, "both AI slots commissioned by night 9+");
            foreach (var ai in s.W.AIGenerals)
                Assert.Greater(s.W.Analytics.DamageBySource.TryGetValue(ai.General.Name, out var d) ? d : 0f, 0f, ai.General.Id + " never fought");
            Assert.AreNotEqual(s.AiGeneralLanes[0], null);
        }

        [Test]
        public void EvolutionChoiceAndRosterSurviveSaveLoad()
        {
            var s = new GameSession(T.Db, new RunSetup { MapId = "moonstone_valley", GeneralId = "tharion", Seed = 5 });
            s.Setup.UnlockedGenerals.AddRange(T.Db.GeneralOrder);
            var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 5);
            bool sawEvolution = false;
            for (int n = 0; n < 8 && s.Phase != RunPhase.Defeat; n++)
            {
                while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat)
                {
                    if (s.Phase == RunPhase.GeneralEvolution) { sawEvolution = true; Assert.IsTrue(s.ChooseGeneralEvolution(1)); continue; }
                    bot.HandleChoices();
                }
                if (s.Phase == RunPhase.Defeat) break;
                s.Economy.Add(new Cost(300, 150), "test");
                bot.PlayDay();
                s.StartNight();
                while (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
            }
            if (s.Phase == RunPhase.Defeat) Assert.Inconclusive("bot lost");
            Assert.IsTrue(sawEvolution);
            Assert.AreEqual("tharion_breaker", s.GeneralEvolutionId);
            while (!RunSave.CanSave(s)) bot.HandleChoices();
            var r = RunSave.Deserialize(T.Db, RunSave.Serialize(s));
            Assert.AreEqual(s.GeneralEvolutionId, r.GeneralEvolutionId);
            Assert.AreEqual(r.W.PlayerGeneral.Evolution?.Id, "tharion_breaker");
            CollectionAssert.AreEqual(s.AiGeneralIds, r.AiGeneralIds);
            Assert.AreEqual(s.W.AIGenerals.Count, r.W.AIGenerals.Count);
        }
    }
}
