using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Runavelle.Core;

/// <summary>Headless performance probe: per-tick CPU time + managed allocations of the shared simulation (no rendering).</summary>
public static class Perf
{
    public static void Run(string map, string general, ulong seed, int nights)
    {
        var db = ContentDatabase.Create();
        var s = new GameSession(db, new RunSetup { MapId = map, GeneralId = general, Seed = seed });
        var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, seed);
        var ms = new List<double>();
        var spikes = new List<(double ms, int tick, int night, bool topo)>();
        var bytes = new List<long>();
        int peakUnits = 0, peakProjectiles = 0, peakZones = 0;
        long events = 0;
        var sw = new Stopwatch();
        for (int t = 0; t < 20 * 60 * 60 && s.Night <= nights && s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory; t++)
        {
            if (s.Phase == RunPhase.Day) { bot.PlayDay(); s.StartNight(); continue; }
            if (s.Phase != RunPhase.Night) { bot.HandleChoices(); continue; }
            bot.PlayNightTick();
            int topo0 = s.W.TopologyVersion;
            long b0 = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();
            s.Tick();
            sw.Stop();
            long b1 = GC.GetAllocatedBytesForCurrentThread();
            ms.Add(sw.Elapsed.TotalMilliseconds);
            if (sw.Elapsed.TotalMilliseconds > 2.0) spikes.Add((sw.Elapsed.TotalMilliseconds, ms.Count, s.Night, s.W.TopologyVersion != topo0));
            bytes.Add(b1 - b0);
            peakUnits = Math.Max(peakUnits, s.W.Units.Count);
            peakProjectiles = Math.Max(peakProjectiles, s.W.Projectiles.Count);
            peakZones = Math.Max(peakZones, s.W.Zones.Count);
            events += s.W.Events.Count;
            s.W.Events.Clear(); // the Unity layer drains events every frame; do the same here
        }
        ms.Sort();
        var sb = bytes.OrderBy(x => x).ToList();
        double P(List<double> l, double q) => l.Count == 0 ? 0 : l[Math.Min(l.Count - 1, (int)(l.Count * q))];
        Console.WriteLine($"perf map={map} general={general} seed={seed} nightsPlayed={s.Night - 1} ticks={ms.Count} phase={s.Phase}");
        Console.WriteLine($"tick_ms p50={P(ms, 0.5):0.000} p95={P(ms, 0.95):0.000} p99={P(ms, 0.99):0.000} max={ms.LastOrDefault():0.000}  (budget at 20 Hz = 50 ms; Unity frame budget 16.7 ms)");
        Console.WriteLine($"alloc_bytes_per_tick mean={(sb.Count == 0 ? 0 : sb.Average()):0} p95={(sb.Count == 0 ? 0 : sb[(int)(sb.Count * 0.95)])} total_MB={sb.Sum() / 1048576.0:0.0}");
        Console.WriteLine($"friendly_astar calls/tick={(ms.Count == 0 ? 0 : (double)s.Nav.PathCalls / ms.Count):0.00} expanded/tick={(ms.Count == 0 ? 0 : (double)s.Nav.PathExpanded / ms.Count):0.0} (includes day ticks run by the bot)");
        var topo = spikes.Where(x => x.topo).ToList();
        Console.WriteLine($"topology-change ticks over 2 ms: {topo.Count}" + (topo.Count > 0 ? $", worst {topo.Max(x => x.ms):0.0} ms" : ""));
        Console.WriteLine($"peak units={peakUnits} projectiles={peakProjectiles} zones={peakZones} events/tick={(ms.Count == 0 ? 0 : (double)events / ms.Count):0.0}");
        Console.WriteLine("spikes>2ms: " + spikes.Count + "  " + string.Join("; ", spikes.OrderByDescending(x => x.ms).Take(5).Select(x => $"{x.ms:0.0}ms tick{x.tick} n{x.night}{(x.topo ? " topo" : "")}")));
        Console.WriteLine($"hw: {Environment.ProcessorCount} logical CPUs, {System.Runtime.InteropServices.RuntimeInformation.OSDescription}, .NET {Environment.Version} (container, not a target device)");
    }
}

/// <summary>
/// High-density siege stress (P9): play to night 9 (two AI General slots), commission both, then add `extra` enemies on the
/// fronts and break a wall every 3 s. Measures per-tick CPU, ticks that changed topology (flow-field rebuild) and managed memory.
/// </summary>
public static class Stress
{
    public static void Run(string map, int extra, ulong seed)
    {
        var db = ContentDatabase.Create();
        var setup = new RunSetup { MapId = map, GeneralId = "roland", Seed = seed };
        setup.UnlockedGenerals.AddRange(db.GeneralOrder);
        var s = new GameSession(db, setup);
        var bot = new AutoPlayer(s, AutoPlayer.Strategy.WallFirst, seed);
        while (s.Night < 9 && s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory)
        {
            if (s.Phase == RunPhase.Day) { s.Economy.Add(new Cost(400, 200), "stress"); bot.PlayDay(); s.StartNight(); }
            else if (s.Phase == RunPhase.Night) { bot.PlayNightTick(); s.Tick(); }
            else bot.HandleChoices();
        }
        while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat) bot.HandleChoices();
        s.Economy.Add(new Cost(2000, 1000), "stress");
        for (int slot = 0; slot < 2; slot++) if (!s.AiSlotCommissioned[slot] && s.AiSlotsAvailable > slot) s.CommissionAiGeneral(slot, s.AvailableAiGenerals()[0]);
        bot.PlayDay();
        s.StartNight();
        var rng = Rng.Derive(seed, 5);
        var pool = db.Units.Values.Where(u => u.Role == UnitRole.Enemy && u.Behavior != EnemyBehavior.Boss && !u.Flying).OrderBy(u => u.Id).ToList();
        var lanes = s.W.Map.Lanes.Where(l => !l.FlyerOnly).ToList();
        for (int i = 0; i < extra; i++)
        {
            var lane = lanes[i % lanes.Count];
            var t = lane.Tiles[rng.Range(0, lane.Tiles.Count)];
            s.Spawner.SpawnEnemy(pool[rng.Range(0, pool.Count)], t.Center, lane.Id, 1f);
        }
        var ms = new List<double>(); var topo = new List<double>();
        var sw = new Stopwatch();
        int broken = 0, peakUnits = 0, peakEnemies = 0;
        long mem0 = GC.GetTotalMemory(true);
        float nextBreak = 3f;
        for (int t = 0; t < 20 * 90 && s.Phase == RunPhase.Night; t++)
        {
            int topo0 = s.W.TopologyVersion; // before the scripted break, so its rebuild tick is counted
            if (s.NightTime >= nextBreak)
            {
                nextBreak += 3f;
                var walls = s.W.Buildings.Where(b => !b.Dead && !b.Ruined && b.IsWallLike).ToList();
                if (walls.Count > 0) { s.Combat.Kill(walls[rng.Range(0, walls.Count)], DamageInfo.Simple(1e6f, ElementId.None, 0, SourceKind.Enemy, -1, "stress")); broken++; }
            }
            bot.PlayNightTick();
            sw.Restart(); s.Tick(); sw.Stop();
            double v = sw.Elapsed.TotalMilliseconds;
            ms.Add(v);
            if (s.W.TopologyVersion != topo0) topo.Add(v);
            peakUnits = Math.Max(peakUnits, s.W.Units.Count(u => !u.Dead));
            peakEnemies = Math.Max(peakEnemies, s.W.Units.Count(u => !u.Dead && u.Team == Team.Enemy));
            s.W.Events.Clear();
        }
        long mem1 = GC.GetTotalMemory(false);
        ms.Sort(); topo.Sort();
        double P(List<double> l, double q) => l.Count == 0 ? 0 : l[Math.Min(l.Count - 1, (int)(l.Count * q))];
        Console.WriteLine($"stress map={map} extra={extra} AIgenerals={s.W.AIGenerals.Count} ticks={ms.Count} walls_broken={broken} peak_units={peakUnits} peak_enemies={peakEnemies}");
        Console.WriteLine($"tick_ms p50={P(ms, 0.5):0.000} p95={P(ms, 0.95):0.000} p99={P(ms, 0.99):0.000} max={ms.LastOrDefault():0.000}");
        Console.WriteLine($"topology-change ticks={topo.Count} p50={P(topo, 0.5):0.000} max={topo.LastOrDefault():0.000} ms (flow-field rebuild after a wall breaks)");
        Console.WriteLine($"friendly_astar calls={s.Nav.PathCalls} expanded={s.Nav.PathExpanded}; managed heap {mem0 / 1048576.0:0.0} -> {mem1 / 1048576.0:0.0} MB");
        Console.WriteLine($"hw: {Environment.ProcessorCount} logical CPUs, .NET {Environment.Version} (container, not a target device)");
    }
}
