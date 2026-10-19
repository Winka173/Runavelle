using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    public class PhaseThreeTests
    {
        private static string CoreDir => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Assets", "Runavelle", "Scripts", "Core"));

        [Test]
        public void DecreePoolSizeAndIntegrity()
        {
            Assert.That(T.Db.Decrees.Count, Is.InRange(80, 100));
            Assert.AreEqual(T.Db.Decrees.Count, T.Db.Decrees.Values.Select(d => d.Name).Distinct().Count(), "unique names");
            foreach (DecreeCategory c in Enum.GetValues(typeof(DecreeCategory))) Assert.GreaterOrEqual(T.Db.Decrees.Values.Count(d => d.Category == c), 6, c.ToString());
            foreach (var d in T.Db.Decrees.Values)
            {
                Assert.IsTrue(d.Modifiers.Count > 0 || d.Hooks.Count > 0 || !d.Grant.IsFree, d.Id + " has no effect");
                if (d.RequireClass != SoldierClass.None) Assert.IsTrue(T.Db.SoldierOrder.Contains(d.RequireClass), d.Id);
                foreach (var x in d.Excludes) Assert.IsTrue(T.Db.Decrees[x].Excludes.Contains(d.Id), d.Id + " exclusion must be mutual");
            }
        }

        [Test]
        public void EveryHookUsedByContentIsImplementedBySomeSystem()
        {
            var used = T.Db.Decrees.Values.SelectMany(d => d.Hooks).Concat(T.Db.Equipment.Values.SelectMany(e => e.Hooks)).Concat(T.Db.Kingdoms.SelectMany(k => k.Hooks)).Select(h => h.Hook).Distinct().ToList();
            var systemSource = string.Join("\n", Directory.GetFiles(CoreDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Replace('\\', '/').Contains("/Content/")).Select(File.ReadAllText));
            foreach (var h in used) StringAssert.Contains("Hook." + h, systemSource, $"Hook {h} is used by content but never read by a system");
        }

        private static GameSession Day(string general = "roland", ulong seed = 3)
        {
            var s = new GameSession(T.Db, new RunSetup { MapId = "moonstone_valley", GeneralId = general, Seed = seed });
            s.Economy.Add(new Cost(2000, 2000), "t");
            return s;
        }

        [Test]
        public void RoyalArchitectMakesFirstBuildingFree()
        {
            var s = Day();
            s.ApplyDecree(T.Db.Decrees["royal_architect"]);
            int g0 = s.Economy.Gold;
            s.Place("archer_1", T.FindSpot(s, "archer_1", s.W.Castle.Center + new Vec2(5, 0)));
            Assert.AreEqual(g0, s.Economy.Gold, "first building free");
            s.Place("archer_1", T.FindSpot(s, "archer_1", s.W.Castle.Center + new Vec2(-5, 3)));
            Assert.Less(s.Economy.Gold, g0, "second costs gold");
        }

        [Test]
        public void ArcaneRebirthRevivesOneSoldierPerNight()
        {
            var s = Day();
            s.ApplyDecree(T.Db.Decrees["arcane_rebirth"]);
            var b = s.Place("barracks_swordsman", T.FindSpot(s, "barracks_swordsman", s.W.Castle.Center + new Vec2(6, 0)));
            s.StartNight();
            var soldiers = s.W.Units.Where(u => u.Role == UnitRole.Soldier).ToList();
            s.Combat.Deal(soldiers[0], DamageInfo.Simple(99999, ElementId.None, 0, SourceKind.Enemy, -1, "t"));
            s.Combat.Deal(soldiers[1], DamageInfo.Simple(99999, ElementId.None, 0, SourceKind.Enemy, -1, "t"));
            Assert.IsFalse(soldiers[0].Dead, "first death revived");
            Assert.IsTrue(soldiers[1].Dead, "only once per night");
        }

        [Test]
        public void FirstStrikeSiegeBreakersSkyWatchScaleDamage()
        {
            var s = Day();
            s.StartNight();
            float Hit(string enemy)
            {
                var e = T.SpawnEnemy(s, enemy, s.W.Castle.Center + new Vec2(9, 0));
                e.MaxHp = e.Hp = 10000;
                return s.Combat.Deal(e, DamageInfo.Simple(100, ElementId.Fire, 0, SourceKind.MagicTower, -1, "t"));
            }
            float baseRam = Hit("ram_crew"), baseHarpy = Hit("harpy");
            s.ApplyDecree(T.Db.Decrees["siege_breakers"]);
            s.ApplyDecree(T.Db.Decrees["sky_watch"]);
            s.ApplyDecree(T.Db.Decrees["first_strike"]);
            Assert.Greater(Hit("ram_crew"), baseRam * 1.9f);
            Assert.Greater(Hit("harpy"), baseHarpy * 2.0f);
        }

        [Test]
        public void EmergencyMasonsAndManaShieldAtDawn()
        {
            var s = Day();
            s.ApplyDecree(T.Db.Decrees["emergency_masons"]);
            s.ApplyDecree(T.Db.Decrees["mana_shield_castle"]);
            var w = s.Place("wall_wood", T.FindSpot(s, "wall_wood", s.W.Castle.Center + new Vec2(4, 4)));
            int mana0 = s.Economy.Mana;
            s.StartNight();
            s.Combat.Deal(w, DamageInfo.Simple(99999, ElementId.None, 0, SourceKind.Enemy, -1, "t"));
            Assert.IsTrue(w.Ruined);
            float castleDmg = s.Combat.Deal(s.W.Castle, DamageInfo.Simple(100, ElementId.Fire, 0, SourceKind.Enemy, -1, "t"));
            Assert.Less(castleDmg, 80f, "mana shield reduces castle damage");
            int guard = 0;
            while (s.Phase == RunPhase.Night && guard++ < 20 * 600) s.Tick();
            Assert.IsFalse(w.Ruined, "rebuilt at dawn");
            Assert.IsTrue(s.LastReport.Notes.Any(n => n.Contains("dựng lại")));
        }

        [Test]
        public void InsomniaRaisesThreatBudget()
        {
            var a = Day(seed: 4); var b = Day(seed: 4);
            b.ApplyDecree(T.Db.Decrees["insomnia"]);
            var pa = a.Waves.Plan(6, 15);
            b.RebuildModifiers();
            var field = typeof(GameSession).GetMethod("ConfigureWaves", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.Invoke(b, null);
            var pb = b.Waves.Plan(6, 15);
            Assert.Greater(pb.Budget, pa.Budget);
        }

        [Test]
        public void KingdomEvolutionsChangeMechanicsAndPersist()
        {
            Assert.AreEqual(3, T.Db.Kingdoms.Count);
            CollectionAssert.AreEquivalent(new[] { "fortress", "legion", "arcanum" }, T.Db.Kingdoms.Select(k => k.Id));
            foreach (var k in T.Db.Kingdoms) Assert.IsTrue(k.Hooks.Count > 0, k.Id + " must change mechanics, not only stats");
            // Arcanum unlocks secondary element slot on tier-2 magic towers
            var s = Day();
            var t = s.Place("magic_fire_1", T.FindSpot(s, "magic_fire_1", s.W.Castle.Center + new Vec2(5, 0)));
            s.Upgrade(t, "magic_fire_2");
            Assert.IsFalse(s.CanImbue(t, ElementId.Water, true).Ok);
            s.KingdomEvolution = "arcanum";
            s.RebuildModifiers();
            Assert.IsTrue(s.CanImbue(t, ElementId.Water, true).Ok);
            Assert.IsTrue(s.Imbue(t, ElementId.Water, true));
            var r = RunSave.Deserialize(T.Db, RunSave.Serialize(s));
            Assert.AreEqual("arcanum", r.KingdomEvolution);
            Assert.IsTrue(r.W.Mods.Has(Hook.WildMagic));
            Assert.AreEqual(ElementId.Water, r.W.Buildings.First(b => b.Def.Family == BuildingFamily.MagicTower).Secondary);
        }

        [Test]
        public void LegionReinforcesDuringNight()
        {
            var s = Day();
            s.KingdomEvolution = "legion";
            s.RebuildModifiers();
            var b = s.Place("barracks_swordsman", T.FindSpot(s, "barracks_swordsman", s.W.Castle.Center + new Vec2(6, 0)));
            int full = s.Spawner.SquadSize(b);
            Assert.AreEqual(full, b.Squad.Count);
            s.StartNight();
            foreach (var id in b.Squad.ToList()) s.Combat.Deal(s.W.Get(id), DamageInfo.Simple(99999, ElementId.None, 0, SourceKind.Enemy, -1, "t"));
            T.SpawnEnemy(s, "orc_brute", new Vec2(2, 2)).MaxHp = 999999; // keep night going
            for (int i = 0; i < 20 * 30; i++) s.Tick();
            Assert.Greater(b.Squad.Count, 0, "legion reinforced during the night");
        }

        [Test]
        public void EquipmentWeaponAddsSecondaryElementAndRelicsWork()
        {
            Assert.AreEqual(8, T.Db.Equipment.Values.Count(e => e.Slot == EquipmentSlot.Weapon));
            Assert.AreEqual(8, T.Db.Equipment.Values.Count(e => e.Slot == EquipmentSlot.Relic));
            var s = Day("kaelen");
            Assert.IsTrue(s.SetEquipment(EquipmentSlot.Weapon, "frostbite"));
            Assert.IsTrue(s.SetEquipment(EquipmentSlot.Relic, "phoenix_charm"));
            Assert.IsFalse(s.SetEquipment(EquipmentSlot.Relic, "frostbite"), "slot mismatch rejected");
            Assert.AreEqual(ElementId.Ice, s.W.PlayerGeneral.SecondaryElement);
            s.StartNight();
            var g = s.W.PlayerGeneral;
            var e = T.SpawnEnemy(s, "orc_brute", g.Position + new Vec2(1, 0));
            e.MaxHp = e.Hp = 99999;
            for (int i = 0; i < 40; i++) s.Tick();
            bool iceUsed = e.Status.Gauge[(int)ElementId.Ice] + e.Status.FreezeTimer > 0f || s.W.Analytics.Reactions.ContainsKey(ReactionId.Melt);
            Assert.IsTrue(iceUsed, "weapon secondary element applied (Fire primary + Ice secondary melts)");
            s.Combat.Deal(g, DamageInfo.Simple(99999, ElementId.None, 0, SourceKind.Enemy, -1, "t"));
            Assert.AreEqual(UnitState.Downed, g.State);
            Assert.LessOrEqual(g.DownedTimer, 5f, "phoenix charm revive time");
            // only one secondary element: the weapon never adds a duplicate of the primary
            var s2 = Day("kaelen");
            s2.SetEquipment(EquipmentSlot.Weapon, "emberheart");
            Assert.AreEqual(ElementId.None, s2.W.PlayerGeneral.SecondaryElement);
        }

        [Test]
        public void DraftsAcrossAFullRunNeverOfferIneligibleOrDuplicates()
        {
            for (ulong seed = 1; seed <= 4; seed++)
            {
                var s = new GameSession(T.Db, new RunSetup { MapId = "moonstone_valley", GeneralId = "seraphine", Seed = seed });
                var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, seed);
                int guard = 0;
                while (s.Phase != RunPhase.Victory && s.Phase != RunPhase.Defeat && guard++ < 20 * 60 * 60 * 3)
                {
                    if (s.Phase == RunPhase.DecreeDraft)
                    {
                        Assert.AreEqual(s.DecreeOffer.Count, s.DecreeOffer.Distinct().Count());
                        foreach (var id in s.DecreeOffer) Assert.IsTrue(s.DecreeEligible(T.Db.Decrees[id]), id);
                    }
                    if (s.Phase == RunPhase.Day) { s.Economy.Add(new Cost(80, 40), "t"); bot.PlayDay(); s.StartNight(); }
                    else if (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
                    else bot.HandleChoices();
                }
                Assert.AreEqual(s.TakenDecrees.Count, s.TakenDecrees.Distinct().Count(), "MaxStacks=1 decrees never repeat");
            }
        }
    }
}
