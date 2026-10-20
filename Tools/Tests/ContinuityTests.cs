using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    /// <summary>P9 reliability: a run saved at a day boundary and loaded again must continue identically.</summary>
    public class ContinuityTests
    {
        private static string Night(GameSession s)
        {
            s.StartNight();
            int t = 0;
            while (s.Phase == RunPhase.Night && t++ < 20 * 400) { s.SetMoveInput(Vec2.Zero); s.Tick(); }
            return $"{s.Phase}/{s.Night}/{s.LastReport?.Kills}/{s.LastReport?.CastleDamage:0.0}/{s.Economy.Gold}/{s.Economy.Mana}/{s.W.Analytics.Breaches.Count}";
        }

        [Test]
        public void LoadedRunContinuesExactlyLikeTheOriginal()
        {
            int checkedNights = 0;
            foreach (var map in new[] { "moonstone_valley", "shadowfen", "stormcliff" })
            {
                var a = T.NewRun(map, "lyra", 23);
                var bot = new AutoPlayer(a, AutoPlayer.Strategy.Balanced, 23);
                for (int n = 0; n < 4; n++)
                {
                    while (a.Phase != RunPhase.Day && a.Phase != RunPhase.Defeat) bot.HandleChoices();
                    bot.PlayDay(); a.StartNight();
                    while (a.Phase == RunPhase.Night) { bot.PlayNightTick(); a.Tick(); }
                }
                while (a.Phase != RunPhase.Day && a.Phase != RunPhase.Defeat) bot.HandleChoices();
                Assume.That(a.Phase == RunPhase.Day);
                var b = RunSave.Deserialize(T.Db, RunSave.Serialize(a));
                Assert.IsTrue(b.Forecast.Matches(a.Plan, T.Db, out var d), "same wave after load: " + d);
                Assert.AreEqual(a.Economy.Gold, b.Economy.Gold);
                Assert.AreEqual(a.W.Buildings.Count(x => !x.Dead), b.W.Buildings.Count(x => !x.Dead));
                var ra = Night(a);
                var rb = Night(b);
                TestContext.WriteLine($"{map}: original {ra} | loaded {rb}");
                Assert.AreEqual(ra, rb, map + ": the loaded run diverged");
                checkedNights++;
            }
            Assert.AreEqual(3, checkedNights);
        }
    }
}
