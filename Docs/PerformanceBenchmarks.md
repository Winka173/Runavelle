# Performance benchmarks

Only numbers actually measured are listed. Hardware: cloud container, 4 logical CPUs, Ubuntu 24.04, .NET 8
(Release). This is **not** a target device; Unity rendering cost, GPU, draw calls and device thermals are unmeasured.

Command: `cd Tools/Sim && dotnet bin/Release/net8.0/Runavelle.Sim.dll perf <map> <general> <seed> 15`
(measures `GameSession.Tick()` only — the shared simulation at 20 Hz — with events drained like the Unity layer).

## Baseline (before Quality Enhancement, commit 9cb971a)
```
perf map=moonstone_valley general=lyra seed=1000 nightsPlayed=14 ticks=19001 phase=Victory
tick_ms p50=0.051 p95=0.155 p99=0.489 max=9.992  (budget at 20 Hz = 50 ms; Unity frame budget 16.7 ms)
alloc_bytes_per_tick mean=1974 p95=7656 total_MB=35.8
peak units=30 projectiles=10 zones=17 events/tick=0.6
hw: 4 logical CPUs, Ubuntu 24.04.5 LTS, .NET 8.0.32 (container, not a target device)
perf map=shattered_crown general=kaelen seed=1000 nightsPlayed=14 ticks=21338 phase=Victory
tick_ms p50=0.030 p95=0.176 p99=0.288 max=11.033  (budget at 20 Hz = 50 ms; Unity frame budget 16.7 ms)
alloc_bytes_per_tick mean=7557 p95=28768 total_MB=153.8
peak units=54 projectiles=10 zones=4 events/tick=0.8
hw: 4 logical CPUs, Ubuntu 24.04.5 LTS, .NET 8.0.32 (container, not a target device)
```
Reading: simulation CPU is far below budget (p99 < 0.5 ms vs 50 ms per tick); the concern for mobile is managed
allocation (≈2–7.5 KB per tick → up to ~150 KB/s, i.e. regular GC) and rare 10–11 ms spikes.
