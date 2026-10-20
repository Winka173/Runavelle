using System;
using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    /// <summary>P8: economy exploit fuzzing and policy regression (counterexample guard for dominant/unwinnable builds).</summary>
    public class PolicyTests
    {
        [Test]
        public void NoDaytimeActionSequenceCreatesGoldOrMana()
        {
            int ops = 0;
            foreach (ulong seed in new ulong[] { 1, 2, 3 })
            {
                var s = T.NewRun("moonstone_valley", "roland", seed);
                var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, seed);
                var rng = Rng.Derive(seed, 7);
                var levels = T.Db.Levels.Values.Where(l => T.Db.BuildingOfLevel(l.Id).PlayerBuildable && l.Tier == 1).Select(l => l.Id).ToList();
                for (int day = 0; day < 4 && s.Phase != RunPhase.Defeat; day++)
                {
                    while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat) bot.HandleChoices();
                    if (s.Phase != RunPhase.Day) break;
                    s.Economy.Add(new Cost(600, 300), "test");
                    // wealth = cash + what every building would sell for right now; no action may raise it
                    (int g, int m) Wealth()
                    {
                        int gg = s.Economy.Gold, mm = s.Economy.Mana;
                        foreach (var b in s.W.Buildings) if (!b.Dead && b.Lifetime < 0f) { var v = s.SellValue(b); gg += v.Gold; mm += v.Mana; }
                        return (gg, mm);
                    }
                    var trace = new System.Collections.Generic.List<string>();
                    for (int i = 0; i < 250; i++)
                    {
                        var (gb, mb) = Wealth();
                        var mine = s.W.Buildings.Where(b => !b.Dead && b.Def.Family != BuildingFamily.Castle && b.Lifetime < 0f).ToList();
                        switch (rng.Range(0, 6))
                        {
                            case 0:
                            {
                                var lvl = levels[rng.Range(0, levels.Count)];
                                var c = s.W.Castle.Center;
                                s.Place(lvl, new Int2((int)c.X + rng.Range(-9, 10), (int)c.Y + rng.Range(-9, 10)));
                                break;
                            }
                            case 1:
                                if (mine.Count > 0) { var b = mine[rng.Range(0, mine.Count)]; if (b.Level.Next.Length > 0) s.Upgrade(b, b.Level.Next[rng.Range(0, b.Level.Next.Length)]); }
                                break;
                            case 2:
                                if (mine.Count > 0) s.Sell(mine[rng.Range(0, mine.Count)]);
                                break;
                            case 3:
                                s.UndoLast();
                                break;
                            case 4:
                                if (mine.Count > 0) s.Imbue(mine[rng.Range(0, mine.Count)], Elements.All[rng.Range(0, 8)], rng.Chance(0.5f));
                                break;
                            case 5:
                                var ruined = s.W.Buildings.Where(b => !b.Dead && b.Ruined).ToList();
                                if (ruined.Count > 0) s.Rebuild(ruined[rng.Range(0, ruined.Count)]);
                                break;
                        }
                        ops++;
                        var (ga, ma) = Wealth();
                        trace.Add($"op{i} wealth gold {gb}->{ga} mana {mb}->{ma} last={s.W.Analytics.BuildLog.LastOrDefault()}");
                        if (ga > gb || ma > mb) TestContext.WriteLine(string.Join("\n", trace.Skip(Math.Max(0, trace.Count - 6))));
                        Assert.LessOrEqual(ga, gb, $"gold wealth created (seed {seed}, day {day}, op {i})");
                        Assert.LessOrEqual(ma, mb, $"mana wealth created (seed {seed}, day {day}, op {i})");
                        Assert.GreaterOrEqual(s.Economy.Gold, 0);
                        Assert.GreaterOrEqual(s.Economy.Mana, 0);
                    }
                    s.StartNight();
                    while (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
                }
            }
            TestContext.WriteLine("random economy operations checked: " + ops);
            Assert.Greater(ops, 2000);
        }

        [Test]
        public void EveryPolicyCanWinAndNoneCrashes()
        {
            // regression guard behind the balance lab: each policy must stay winnable on the tutorial map and never throw
            foreach (AutoPlayer.Strategy p in Enum.GetValues(typeof(AutoPlayer.Strategy)))
            {
                int wins = 0;
                for (int i = 0; i < 2; i++)
                {
                    ulong seed = (ulong)(1000 + i * 7919); // same sampling as the Tools/Sim balance lab
                    var s = new GameSession(T.Db, new RunSetup { MapId = "moonstone_valley", GeneralId = "roland", Seed = seed });
                    var bot = new AutoPlayer(s, p, seed);
                    if (p != AutoPlayer.Strategy.MageHeavy)
                    {
                        bot.SoldierPreference = T.Db.SoldierOrder[i % T.Db.SoldierOrder.Count].ToString().ToLowerInvariant();
                        bot.MagicSchool = "magic_" + Elements.Info(Elements.All[(i * 3) % 8]).Key + "_1";
                    }
                    Assert.DoesNotThrow(() => bot.RunToEnd(), p.ToString());
                    if (s.Phase == RunPhase.Victory) wins++;
                }
                Assert.Greater(wins, 0, p + " cannot win the tutorial map");
            }
        }
    }
}
