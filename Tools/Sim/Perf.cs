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
        Console.WriteLine($"peak units={peakUnits} projectiles={peakProjectiles} zones={peakZones} events/tick={(ms.Count == 0 ? 0 : (double)events / ms.Count):0.0}");
        Console.WriteLine("spikes>2ms: " + spikes.Count + "  " + string.Join("; ", spikes.OrderByDescending(x => x.ms).Take(5).Select(x => $"{x.ms:0.0}ms tick{x.tick} n{x.night}{(x.topo ? " topo" : "")}")));
        Console.WriteLine($"hw: {Environment.ProcessorCount} logical CPUs, {System.Runtime.InteropServices.RuntimeInformation.OSDescription}, .NET {Environment.Version} (container, not a target device)");
    }
}
