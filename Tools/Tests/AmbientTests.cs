using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    public class AmbientTests
    {
        [Test]
        public void AmbientLifeFollowsTheKingdomAndStaysInBudget()
        {
            var s = T.NewRun();
            s.Economy.Add(new Cost(9000, 9000), "t");
            var c = s.W.Castle.Center;
            for (int i = 0; i < 14; i++) s.Place(i % 2 == 0 ? "house_1" : "well_1", T.FindSpot(s, i % 2 == 0 ? "house_1" : "well_1", c + new Vec2(4 + i, (i % 3) * 3 - 3)));
            foreach (var h in s.W.Buildings.Where(b => b.Def.Family == BuildingFamily.House).ToList())
                if (h.Level.Next.Length > 0) s.Upgrade(h, h.Level.Next[0]);
            Assert.IsEmpty(AmbientDirector.Emitters(s.W, 0), "level 0 = off");
            var day = AmbientDirector.Emitters(s.W, 2);
            Assert.IsTrue(day.Any(e => e.Kind == AmbientKind.ChimneySmoke) && day.Any(e => e.Kind == AmbientKind.WellShimmer));
            Assert.IsFalse(day.Any(e => e.Kind == AmbientKind.Torch), "torches only at night");
            Assert.LessOrEqual(day.Count, AmbientDirector.MaxEmitters);
            Assert.LessOrEqual(AmbientDirector.Emitters(s.W, 1).Count, AmbientDirector.MaxEmitters / 2);
            s.StartNight();
            var night = AmbientDirector.Emitters(s.W, 2);
            Assert.IsTrue(night.Any(e => e.Kind == AmbientKind.Torch));
            foreach (var b in s.W.Buildings.Where(b => b.Def.Family == BuildingFamily.House)) b.Ruined = true;
            Assert.IsFalse(AmbientDirector.Emitters(s.W, 2).Any(e => e.Kind == AmbientKind.ChimneySmoke), "ruins do not smoke");
        }
    }
}
