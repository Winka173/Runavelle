using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    /// <summary>P6: readable costs / ROI from content data, and post-night insights grounded in measured numbers.</summary>
    public class InsightTests
    {
        [Test]
        public void RoiNumbersComeFromContent()
        {
            var house = T.Db.Levels["house_1"];
            int expected = (int)Math.Ceiling(BuildInsight.GoldValue(house.Cost) / BuildInsight.GoldValue(house.IncomePerDawn));
            Assert.AreEqual(expected, BuildInsight.PaybackNights(house, house.Cost));
            StringAssert.Contains($"~{expected} đêm", string.Join("|", BuildInsight.Lines(house, house.Cost)));
            var archer = T.Db.Levels["archer_1"];
            Assert.AreEqual(archer.Attack.Damage / archer.Attack.Interval, BuildInsight.Dps(archer), 1e-4);
            Assert.IsNull(BuildInsight.PaybackNights(archer, archer.Cost));
            StringAssert.Contains("máu mỗi vàng", string.Join("|", BuildInsight.Lines(T.Db.Levels["wall_stone"], T.Db.Levels["wall_stone"].Cost)));
            foreach (var l in T.Db.Levels.Values)
                Assert.DoesNotThrow(() => BuildInsight.Lines(l, l.Cost), l.Id);
        }

        [Test]
        public void NightInsightsCiteMeasuredNumbers()
        {
            int reports = 0, castleTips = 0, breachTips = 0;
            foreach (var map in new[] { "dawnspire", "ember_highlands", "shattered_crown" })
            {
                var s = T.NewRun(map, "roland", 11);
                var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 11);
                for (int n = 0; n < 12 && s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory; n++)
                {
                    while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory) bot.HandleChoices();
                    if (s.Phase != RunPhase.Day) break;
                    bot.PlayDay(); s.StartNight();
                    while (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
                    var rep = s.LastReport;
                    reports++;
                    Assert.IsNotEmpty(rep.Insights);
                    Assert.LessOrEqual(rep.Insights.Count, 3);
                    foreach (var tip in rep.Insights) Assert.IsTrue(Regex.IsMatch(tip, "\\d"), "every insight cites a number: " + tip);
                    Assert.AreEqual(rep.CastleDamage, s.CastleDamageThisNight.Values.Sum(), Math.Max(1f, rep.CastleDamage * 0.01f), "per-attacker split adds up to the night's castle damage");
                    bool breached = s.W.Analytics.Breaches.Any(b => b.Night == rep.Night);
                    bool breachTip = rep.Insights.Any(t => t.StartsWith("Tường thủng"));
                    if (breachTip) { breachTips++; Assert.IsTrue(breached); }
                    if (rep.Insights.Any(t => t.StartsWith("Lâu Đài mất"))) { castleTips++; Assert.GreaterOrEqual(rep.CastleDamage, s.W.Castle.MaxHp * 0.05f); }
                }
            }
            TestContext.WriteLine($"reports {reports}, castle tips {castleTips}, breach tips {breachTips}");
            Assert.Greater(reports, 20);
        }
    }
}
