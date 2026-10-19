using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    public class PhaseTwoTests
    {
        private static (GameSession s, Unit e) Night()
        {
            var s = T.NewRun();
            s.StartNight();
            var e = T.SpawnEnemy(s, "orc_brute", s.W.Castle.Center + new Vec2(8, 0));
            e.MaxHp = e.Hp = 100000;
            s.W.RebuildSpatial();
            return (s, e);
        }

        private static DamageInfo Hit(ElementId el, float status, SourceKind src = SourceKind.MagicTower) => DamageInfo.Simple(10, el, status, src, -1, "t");

        [Test]
        public void NinthElementIsImpossible()
        {
            Assert.AreEqual(9, System.Enum.GetValues(typeof(ElementId)).Length);
            Assert.AreEqual(Elements.ElementCap, Elements.All.Length);
            Assert.IsFalse(Elements.IsValid(9));
            Assert.IsFalse(Elements.IsValid(-1));
            // a save carrying an unknown element id loads as None instead of corrupting state
            var s = T.NewRun();
            s.Economy.Add(new Cost(500, 500), "t");
            var b = s.Place("archer_1", T.FindSpot(s, "archer_1", s.W.Castle.Center + new Vec2(6, 0)));
            var json = RunSave.Serialize(s).Replace("\"primary\":0", "\"primary\":9");
            var r = RunSave.Deserialize(T.Db, json);
            Assert.IsTrue(r.W.Buildings.All(x => Elements.IsValid(x.Primary)));
        }

        [Test]
        public void LightAndShadowAreUsedEverywhere()
        {
            foreach (var e in new[] { ElementId.Light, ElementId.Shadow })
            {
                Assert.GreaterOrEqual(T.Db.Generals.Values.Count(g => g.Element == e), 2, e + " generals");
                Assert.GreaterOrEqual(T.Db.SoldierBranches.Values.Count(b => b.Element == e), 1, e + " soldier branch");
                Assert.IsTrue(T.Db.Levels.ContainsKey("magic_" + Elements.Info(e).Key + "_1"), e + " magic tower");
                Assert.GreaterOrEqual(T.Db.Units.Values.Count(u => u.Role == UnitRole.Enemy && u.Behavior != EnemyBehavior.Boss && u.Attack.Element == e), 1, e + " enemy");
                Assert.GreaterOrEqual(T.Db.Decrees.Values.Count(d => d.RequireElement == e || d.Modifiers.Any(m => m.Element == e)), 1, e + " decree");
                Assert.GreaterOrEqual(ReactionTable.AllBespoke.Count(r => r.A == e || r.B == e), 3, e + " reactions");
            }
            Assert.IsTrue(T.Db.Bosses.Values.Any(b => b.Phases.Any(p => p.Abilities.Any(a => a.Element == ElementId.Shadow))), "a boss uses Shadow");
        }

        [Test]
        public void MixedSourceEclipseFromTowerAndGeneral()
        {
            var (s, e) = Night();
            s.Combat.Deal(e, Hit(ElementId.Light, 40, SourceKind.MagicTower));
            s.Combat.Deal(e, Hit(ElementId.Shadow, 40, SourceKind.PlayerGeneral));
            Assert.AreEqual(1, s.W.Analytics.Reactions[ReactionId.Eclipse]);
        }

        [Test]
        public void PurifyHealsFortifyShieldsHauntedArcSoulFrostExecutes()
        {
            var (s, e) = Night();
            s.Economy.Add(new Cost(500, 500), "t");
            // Purify heals a damaged nearby wall
            var wall = s.Spawner.CreateBuilding("wall_stone", Int2.FromPosition(e.Position + new Vec2(0, 1.5f)));
            wall.Hp = wall.MaxHp * 0.5f;
            s.Combat.Deal(e, Hit(ElementId.Water, 40));
            s.Combat.Deal(e, Hit(ElementId.Light, 40));
            Assert.Greater(wall.Hp, wall.MaxHp * 0.5f, "Purify heals structures");
            // Fortify shields the wall
            var e2 = T.SpawnEnemy(s, "orc_brute", wall.Center + new Vec2(1.2f, 0)); e2.MaxHp = e2.Hp = 100000;
            s.W.RebuildSpatial();
            s.Combat.Deal(e2, Hit(ElementId.Earth, 40));
            s.Combat.Deal(e2, Hit(ElementId.Light, 40));
            Assert.Greater(wall.ShieldHp, 0f, "Fortify shields walls");
            // Haunted Arc jumps to marked enemies
            var marked = T.SpawnEnemy(s, "goblin", e.Position + new Vec2(2.5f, 1)); marked.MaxHp = marked.Hp = 5000;
            marked.Status.Gauge[(int)ElementId.Shadow] = 50;
            var e3 = T.SpawnEnemy(s, "orc_brute", e.Position + new Vec2(1, -2)); e3.MaxHp = e3.Hp = 100000;
            s.W.RebuildSpatial();
            float before = marked.Hp;
            s.Combat.Deal(e3, Hit(ElementId.Shadow, 40));
            s.Combat.Deal(e3, Hit(ElementId.Lightning, 40));
            Assert.Less(marked.Hp, before, "Haunted Arc hits marked enemies");
            // Soul Frost executes a weakened normal enemy but not an elite
            var weak = T.SpawnEnemy(s, "goblin", e.Position + new Vec2(-3, 3));
            weak.Hp = weak.MaxHp * 0.15f;
            s.Combat.Deal(weak, DamageInfo.Simple(0.01f, ElementId.Ice, 40, SourceKind.MagicTower, -1, "t"));
            s.Combat.Deal(weak, DamageInfo.Simple(0.01f, ElementId.Shadow, 40, SourceKind.MagicTower, -1, "t"));
            Assert.IsTrue(weak.Dead, "Soul Frost executes");
            var elite = T.SpawnEnemy(s, "dark_champion", e.Position + new Vec2(-3, -3));
            elite.Hp = elite.MaxHp * 0.15f;
            s.Combat.Deal(elite, DamageInfo.Simple(0.01f, ElementId.Ice, 40, SourceKind.MagicTower, -1, "t"));
            s.Combat.Deal(elite, DamageInfo.Simple(0.01f, ElementId.Shadow, 40, SourceKind.MagicTower, -1, "t"));
            Assert.IsFalse(elite.Dead && elite.Hp > 0, "elite is not instantly executed");
        }

        [Test]
        public void StealthAssassinIsRevealedByProximityOrLight()
        {
            var (s, _) = Night();
            var a = T.SpawnEnemy(s, "shade_stalker", new Vec2(40, 14));
            Assert.IsTrue(a.Stealthed);
            Assert.IsFalse(a.Targetable);
            s.Combat.ApplyElement(a, ElementId.Light, 20, Hit(ElementId.Light, 20));
            s.Tick();
            Assert.IsFalse(a.Stealthed, "Light reveals stealth");
        }

        [Test]
        public void TemplarGuardsNearbyEnemiesAndCultistCursesDefenders()
        {
            var (s, e) = Night();
            var tpl = T.SpawnEnemy(s, "fallen_templar", e.Position + new Vec2(1, 0));
            for (int i = 0; i < 3; i++) s.Tick();
            Assert.Greater(e.Status.GuardAmount, 0.2f, "templar shield reduces damage to allies");
            var g = s.W.PlayerGeneral;
            var cult = T.SpawnEnemy(s, "eclipse_cultist", g.Position + new Vec2(3, 0));
            cult.MaxHp = cult.Hp = 9999;
            float shadow = 0;
            for (int i = 0; i < 80; i++) { s.SetMoveInput(Vec2.Zero); s.Tick(); shadow = System.Math.Max(shadow, g.Status.Gauge[(int)ElementId.Shadow]); }
            Assert.Greater(shadow, 10f, "cultist marks defenders with Shadow");
        }

        [Test]
        public void RiftWarlockCastsCurseZoneInPhaseTwo()
        {
            var (s, _) = Night();
            s.Spawner.CreateBuilding("house_1", Int2.FromPosition(s.W.Castle.Center + new Vec2(4, 2)));
            var boss = T.SpawnEnemy(s, "boss_rift_warlock", s.W.Castle.Center + new Vec2(6, 0));
            boss.Hp = boss.MaxHp * 0.45f;
            bool gloom = false;
            for (int i = 0; i < 20 * 20 && !gloom; i++) { s.Tick(); gloom = s.W.Zones.Any(z => z.Kind == ZoneKind.Gloom && z.Team == Team.Enemy); }
            Assert.IsTrue(gloom);
        }

        [Test]
        public void LightShadowDecreeHooksWork()
        {
            var (s, e) = Night();
            s.ApplyDecree(T.Db.Decrees["shadow_harvest"]);
            var g = T.SpawnEnemy(s, "goblin", e.Position + new Vec2(2, 0));
            g.Status.Gauge[(int)ElementId.Shadow] = 50;
            s.Combat.Deal(g, DamageInfo.Simple(9999, ElementId.None, 0, SourceKind.ArcherTower, -1, "t"));
            Assert.AreEqual(3, s.W.PendingMana);
            s.ApplyDecree(T.Db.Decrees["radiant_walls"]);
            var wall = s.Spawner.CreateBuilding("wall_wood", Int2.FromPosition(e.Position + new Vec2(-1.5f, 0)));
            s.Combat.Deal(wall, DamageInfo.Simple(5, ElementId.None, 0, SourceKind.Enemy, e.Id, e.Def.Name));
            Assert.Greater(e.Status.Gauge[(int)ElementId.Light], 0f, "radiant walls mark attackers");
        }
    }
}
