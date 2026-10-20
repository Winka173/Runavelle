# AI Behaviour & Priorities

No player commands to units. All weights in `Core/Systems/AiTuning.cs`; reasons are shown by the in-game
debug overlay (Settings → Điều khiển & trợ năng → Bảng gỡ lỗi AI).

## Soldiers (`DefenderAI`)
* **Deploy**: squads spawn on a free tile outside their Barracks (fixed a bug where they spawned inside the
  footprint and never left — the army previously never actually deployed). Units inside any blocked tile may walk out.
* **Region**: leash 5.5 tiles (7.5 cavalry/griffins) around the Barracks; targets further than leash + 1.5 are ignored.
* **Score** = 3/(1+distance) + 2·(1 − flow/60) + 1.2 if the enemy is hitting our structure (0.6 a unit) + 1.0 siege
  + 1.5 air-vs-air + 0.8 spear-vs-large + **0.8 enemy standing in a breach opened tonight** + 0.5 stickiness.
* **Ranged spacing**: soldiers with range > 2.5 step 1.4 tiles back (preferred: away + toward home, then ±45°/±90°)
  when a melee enemy is within 1.15 tiles and their attack is not ready. Reason: "Giữ khoảng cách".
* **Pursuit cap**: units with speed ≥ 3.2 give up a target not reached within 3.5 s, ignore it for 2 s and return
  ("Bỏ truy đuổi, quay về") — no runaway chases.
* Day: walk back to formation slots. Tactical Retreat decree: retreat below 30% HP.

## AI Generals (`GeneralSystem`)
* Hold an assigned sector post (40% from castle toward the lane); targets within post + 8.5 only, and a target
  that leaves that radius is dropped every tick (skirmishers can no longer bait them away).
* Retreat below 30% HP to the castle and regenerate; ultimates when ≥ 4 enemies (or an elite) are in radius
  (support styles: ≥ 3 wounded allies or castle < 40%).
* Barks (`BarkDirector`): night start, wall breach (nearest AI General within 14 tiles), timely save (killed an enemy
  that was hitting an ally under 35% HP), Ultimate, boss defeat, downed. Global cooldown 7 s, per-trigger 25 s, one
  line on screen, critical lines may replace minor ones. Subtitled; never the player's General.
* Measured contribution (`Tools/Sim` metrics): player General ≈ 50% of kills, AI Generals ≈ 12% — the AI does not
  overshadow the player.

## Cost
All AI decisions are throttled (soldiers 0.25–0.34 s, AI Generals 0.4 s, enemies 0.3 s), use spatial hashes, cached
flow fields (rebuilt only on topology change) and pooled lists. Simulation tick p99 < 0.6 ms on the measuring
container (see PerformanceBenchmarks.md).
