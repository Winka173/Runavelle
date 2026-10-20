using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    /// <summary>P4: General–army synergy is a real behaviour (element statuses, coordinated attacks), bounded and switchable.</summary>
    public class SynergyTests
    {
        private static (GameSession s, Building b, Unit soldier) Squad(string general)
        {
            var s = T.NewRun("moonstone_valley", general, 5);
            s.Economy.Add(new Cost(2000, 2000), "t");
            var b = s.Place("barracks_swordsman", T.FindSpot(s, "barracks_swordsman", s.W.Castle.Center + new Vec2(6, 0)));
            s.StartNight();
            var soldier = (Unit)s.W.Get(b.Squad[0]);
            return (s, b, soldier);
        }

        [Test]
        public void SoldiersBesideTheGeneralCarryItsElementAndApplyIt()
        {
            var (s, b, soldier) = Squad("roland"); // Earth, no dedicated element aura
            Assume.That(soldier.Attack.Element == ElementId.None);
            var g = s.W.PlayerGeneral;
            g.Position = soldier.Position + new Vec2(1f, 0);
            var e = T.SpawnEnemy(s, "goblin", soldier.Position + new Vec2(0.8f, 0));
            e.Attack.Damage = 0f;
            float earth = 0f;
            for (int i = 0; i < 20 * 4 && !e.Dead; i++)
            {
                g.Position = soldier.Position + new Vec2(1f, 0);
                s.Tick();
                earth = System.Math.Max(earth, e.Status.Gauge[(int)ElementId.Earth]);
            }
            Assert.AreEqual(ElementId.Earth, soldier.Buffs.GrantElement == ElementId.None && e.Dead ? ElementId.Earth : soldier.Buffs.GrantElement, "linked while beside the General");
            Assert.Greater(earth, 0f, "soldier hits applied the General's element status");
        }

        [Test]
        public void LinkEndsAwayFromTheGeneralAndCanBeSwitchedOff()
        {
            var (s, b, soldier) = Squad("lyra");
            var g = s.W.PlayerGeneral;
            void Settle() { for (int i = 0; i < 12; i++) s.Tick(); } // auras refresh on a fixed cadence, not every tick
            g.Position = soldier.Position + new Vec2(20f, 0);
            Settle();
            Assert.AreEqual(ElementId.None, soldier.Buffs.GrantElement, "no link outside the aura radius");
            g.Position = soldier.Position + new Vec2(1f, 0);
            Settle();
            Assert.AreEqual(ElementId.Lightning, soldier.Buffs.GrantElement);
            float old = AiTuning.GeneralLinkStatus;
            try
            {
                AiTuning.GeneralLinkStatus = 0f;
                Settle();
                Assert.AreEqual(ElementId.None, soldier.Buffs.GrantElement, "knob off = no link");
            }
            finally { AiTuning.GeneralLinkStatus = old; }
        }

        [Test]
        public void DedicatedElementAurasAreNotOverridden()
        {
            var (s, b, soldier) = Squad("maren"); // Water aura grants 12 status explicitly
            var g = s.W.PlayerGeneral;
            for (int i = 0; i < 12; i++) { g.Position = soldier.Position + new Vec2(1f, 0); s.Tick(); }
            Assert.AreEqual(ElementId.Water, soldier.Buffs.GrantElement);
            Assert.Greater(soldier.Buffs.GrantStatus, AiTuning.GeneralLinkStatus, "the stronger dedicated aura wins");
        }

        [Test]
        public void UltimateTriggersOneCoordinatedStrikeInsideItsRadiusOnly()
        {
            var (s, b, soldier) = Squad("roland");
            var g = s.W.PlayerGeneral;
            var far = (Unit)s.W.Get(b.Squad.Last());
            g.Position = soldier.Position + new Vec2(1f, 0);
            far.Position = g.Position + new Vec2(g.General.Ultimate.CastRadius + 3f, 0);
            soldier.AttackCooldown = 2f;
            far.AttackCooldown = 2f;
            s.W.RebuildSpatial();
            s.W.Events.Clear();
            s.Generals.CastUltimate(g);
            Assert.AreEqual(0f, soldier.AttackCooldown, "inside the radius: strikes now");
            Assert.AreEqual(2f, far.AttackCooldown, "outside: unaffected");
            var ev = s.W.Events.Single(x => x.Type == SimEventType.SynergyStrike);
            Assert.GreaterOrEqual(ev.Value, 1f);
        }
    }
}
