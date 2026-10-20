using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    /// <summary>P2 Army Rally: autonomous, deterministic, skippable, and squads never muster outside their walls.</summary>
    public class RallyTests
    {
        private static GameSession DayWithBarracks(string map, ulong seed, int nights = 2)
        {
            var s = T.NewRun(map, "roland", seed);
            var bot = new AutoPlayer(s, AutoPlayer.Strategy.Army, seed);
            for (int n = 0; n < nights; n++)
            {
                while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat) bot.HandleChoices();
                s.Economy.Add(new Cost(400, 150), "test");
                bot.PlayDay();
                if (n == nights - 1) break;
                s.StartNight();
                while (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
            }
            return s;
        }

        [Test]
        public void PostsStayInsideTheWallLineAndNearTheirBarracks()
        {
            int posts = 0;
            foreach (var map in T.Db.MapOrder)
            {
                var s = DayWithBarracks(map, 4);
                s.AssignRallyPosts();
                foreach (var b in s.W.Buildings.Where(b => !b.Dead && b.Family == BuildingFamily.Barracks && b.RallyPost.HasValue))
                {
                    posts++;
                    var p = b.RallyPost.Value;
                    var t = Int2.FromPosition(p);
                    Assert.IsTrue(s.W.FriendlyPassable(t.X, t.Y), $"{map}: post on blocked tile");
                    Assert.LessOrEqual(Vec2.Distance(p, b.Center), b.Size * 0.5f + 0.9f + AiTuning.RallyDistance + 0.01f);
                    // no intact wall/gate between the barracks door and the post
                    var door = b.Center + b.RallyDir * (b.Size * 0.5f + 0.9f);
                    for (float k = 0f; k <= 1f; k += 0.05f)
                    {
                        var q = Int2.FromPosition(door + (p - door) * k);
                        var occ = s.W.BuildingAt(q.X, q.Y);
                        Assert.IsFalse(occ != null && occ.IsWallLike && !occ.Ruined, $"{map}: wall between barracks and post");
                    }
                }
            }
            TestContext.WriteLine("posts checked: " + posts);
            Assert.Greater(posts, 4);
        }

        [Test]
        public void SkippingTheDuskRallyGivesTheSamePosts()
        {
            var a = DayWithBarracks("moonstone_valley", 8);
            var b = DayWithBarracks("moonstone_valley", 8);
            a.BeginRally();
            Assert.IsTrue(a.W.Rally);
            a.StartNight();
            b.StartNight(); // dusk skipped (headless)
            var pa = a.W.Buildings.Where(x => x.Family == BuildingFamily.Barracks).Select(x => x.RallyPost).ToList();
            var pb = b.W.Buildings.Where(x => x.Family == BuildingFamily.Barracks).Select(x => x.RallyPost).ToList();
            CollectionAssert.AreEqual(pa, pb);
            Assert.IsFalse(a.W.Rally, "rally flag ends when the night starts");
            Assert.IsTrue(pa.Any(p => p.HasValue));
        }

        [Test]
        public void SquadsMusterAtTheirPostAndGoHomeByDay()
        {
            var s = DayWithBarracks("sunken_ruins", 12);
            var bot = new AutoPlayer(s, AutoPlayer.Strategy.Army, 12);
            s.BeginRally();
            for (int i = 0; i < 20 * 3; i++) s.Tick(); // 3 s of dusk march (day sim keeps running)
            s.StartNight();
            for (int i = 0; i < 20 * 3; i++) { bot.PlayNightTick(); s.Tick(); }
            int near = 0, total = 0;
            foreach (var b in s.W.Buildings.Where(b => !b.Dead && b.Family == BuildingFamily.Barracks && b.RallyPost.HasValue))
                foreach (var id in b.Squad)
                    if (s.W.Get(id) is Unit u && !u.Dead)
                    {
                        total++;
                        if (Vec2.Distance(u.Position, b.RallyPost.Value) < 3.2f) near++;
                    }
            TestContext.WriteLine($"{near}/{total} soldiers at their post after 3 s dusk + 3 s night");
            Assert.Greater(total, 0);
            Assert.GreaterOrEqual(near, total * 0.7);
            while (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
            Assert.IsTrue(s.W.Buildings.All(b => !b.RallyPost.HasValue), "posts cleared after the night");
        }
    }
}
