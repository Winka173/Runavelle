using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Runavelle.Core;

// Usage: dotnet run -c Release -- [seeds=8] [maps=all] [generals=roland,lyra,kaelen] [strategies=Balanced] [verbose]
public static class Program
{
    public static int Main(string[] args)
    {
        var opts = args.Select(a => a.Split('=')).ToDictionary(p => p[0], p => p.Length > 1 ? p[1] : "1");
        if (args.Length > 0 && args[0] == "catalog") { Catalog.Write(System.IO.Path.Combine("..", "..", "Docs", "Content_Catalog.md")); Console.WriteLine("catalog written"); return 0; }
        if (args.Length > 0 && args[0] == "snapshot")
        {
            // snapshot <map> <general> <night> <seconds> <out.json> [strategy]
            Snapshot.Write(args[1], args[2], int.Parse(args[3]), float.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture), args[5], args.Length > 6 ? args[6] : "Balanced");
            Console.WriteLine("snapshot written " + args[5]); return 0;
        }
        if (args.Length > 0 && args[0] == "plan")
        {
            // plan <map> [seed]: wave composition per night (balance debugging)
            var pdb = ContentDatabase.Create();
            var ps = new GameSession(pdb, new RunSetup { MapId = args[1], GeneralId = "roland", Seed = args.Length > 2 ? ulong.Parse(args[2]) : 1000 });
            for (int n = 1; n <= 15; n++)
            {
                var plan = ps.Waves.Plan(n, 15, x => { string bo = null; return pdb.Maps[args[1]].Bosses != null && (x == 5 || x == 10 || x == 15) ? pdb.Maps[args[1]].Bosses[x / 5 - 1] : bo; });
                Console.WriteLine($"N{n} budget={plan.Budget} boss={plan.BossUnitId} lanes={string.Join("/", plan.Lanes)} :: " + string.Join(", ", plan.Groups.Select(gr => gr.Count + "x" + gr.UnitId)));
            }
            return 0;
        }
        var db = ContentDatabase.Create();
        int seeds = opts.TryGetValue("seeds", out var s) ? int.Parse(s) : 8;
        var maps = opts.TryGetValue("maps", out var m) && m != "all" ? m.Split(',').ToList() : db.MapOrder.ToList();
        var generals = opts.TryGetValue("generals", out var g) ? (g == "all" ? db.GeneralOrder.ToList() : g.Split(',').ToList()) : new List<string> { "roland", "lyra", "kaelen" };
        var strategies = opts.TryGetValue("strategies", out var st) ? st.Split(',').Select(x => Enum.Parse<AutoPlayer.Strategy>(x)).ToList() : new List<AutoPlayer.Strategy> { AutoPlayer.Strategy.Balanced };
        int asc = opts.TryGetValue("ascension", out var a) ? int.Parse(a) : 0;
        bool verbose = opts.ContainsKey("verbose");
        string challenge = opts.TryGetValue("challenge", out var chl) ? chl : null;
        bool endless = opts.ContainsKey("endless");
        Console.WriteLine($"Runavelle headless harness | seeds={seeds} maps={string.Join(",", maps)} generals={string.Join(",", generals)} strategies={string.Join(",", strategies)} asc={asc}");
        Console.WriteLine("map,general,strategy,seed,result,nights,kills,castle_dmg_total,reactions,peak_enemies,max_rx_tick,gold_end,mana_end,seconds,exception");
        int runs = 0, wins = 0, exceptions = 0;
        var total = Stopwatch.StartNew();
        var byKey = new Dictionary<string, (int runs, int wins, double nights)>();
        foreach (var map in maps)
        foreach (var gen in generals)
        foreach (var strat in strategies)
        for (int i = 0; i < seeds; i++)
        {
            ulong seed = (ulong)(1000 + i * 7919);
            var setup = new RunSetup { MapId = map, GeneralId = gen, Seed = seed, Ascension = asc, ChallengeId = challenge, Endless = endless };
            setup.UnlockedGenerals.AddRange(db.GeneralOrder);
            var sw = Stopwatch.StartNew();
            string ex = "";
            int nights = 0;
            GameSession sess = null;
            try
            {
                sess = new GameSession(db, setup);
                var bot = new AutoPlayer(sess, strat, seed);
                bot.SoldierPreference = db.SoldierOrder[i % db.SoldierOrder.Count].ToString().ToLowerInvariant();
                bot.MagicSchool = "magic_" + Elements.Info(Elements.All[(i * 3) % 8]).Key + "_1";
                nights = bot.RunToEnd();
            }
            catch (Exception e) { ex = e.GetType().Name + ": " + e.Message.Replace(',', ';') + " @ " + (e.StackTrace ?? "").Split('\n').FirstOrDefault()?.Trim().Replace(',', ';'); exceptions++; }
            bool win = sess != null && sess.Phase == RunPhase.Victory;
            runs++; if (win) wins++;
            string key = $"{map}/{gen}/{strat}";
            byKey.TryGetValue(key, out var acc);
            byKey[key] = (acc.runs + 1, acc.wins + (win ? 1 : 0), acc.nights + nights);
            var an = sess?.W.Analytics;
            int rx = an?.Reactions.Values.Sum() ?? 0;
            float cd = an?.Nights.Sum(n => n.CastleDamage) ?? 0;
            Console.WriteLine($"{map},{gen},{strat},{seed},{(win ? "WIN" : "LOSS")},{nights},{an?.TotalKills},{cd:0},{rx},{an?.PeakEnemies},{an?.MaxReactionsInTick},{sess?.Economy.Gold},{sess?.Economy.Mana},{sw.Elapsed.TotalSeconds:0.0},{ex}");
            if (verbose && sess != null)
            {
                foreach (var n in an.Nights) Console.WriteLine($"   night {n.Night}: {n.Duration:0}s kills={n.Kills} castleDmg={n.CastleDamage:0} lost={n.SoldiersLost}/{n.BuildingsLost} gold={n.Gold} mana={n.Mana} lanes={n.Lanes}");
                if (!win) Console.WriteLine($"   defeat cause: {sess.DefeatCause}; castle damage by: " + string.Join("; ", RunAnalytics.Top(an.CastleDamageBy, 4).Select(k => $"{k.Key}={k.Value:0}")));
                Console.WriteLine("   reactions: " + string.Join("; ", an.Reactions.OrderByDescending(k => k.Value).Take(8).Select(k => $"{k.Key}={k.Value}")));
                Console.WriteLine("   damage: " + string.Join("; ", RunAnalytics.Top(an.DamageBySource, 6).Select(k => $"{k.Key}={k.Value:0}")));
                Console.WriteLine("   decrees: " + string.Join(",", sess.TakenDecrees));
                if (opts.ContainsKey("builds")) Console.WriteLine("   builds: " + string.Join(" | ", an.BuildLog));
            }
        }
        Console.WriteLine();
        Console.WriteLine("summary,key,runs,wins,win_rate,avg_nights");
        foreach (var kv in byKey.OrderBy(k => k.Key)) Console.WriteLine($"summary,{kv.Key},{kv.Value.runs},{kv.Value.wins},{100.0 * kv.Value.wins / kv.Value.runs:0}%,{kv.Value.nights / kv.Value.runs:0.0}");
        Console.WriteLine($"TOTAL runs={runs} wins={wins} ({100.0 * wins / Math.Max(1, runs):0}%) exceptions={exceptions} wall={total.Elapsed.TotalSeconds:0.0}s");
        return exceptions > 0 ? 2 : 0;
    }
}
