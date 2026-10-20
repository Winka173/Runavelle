# Quality Enhancement — Final Report

## 1. Build / version / platforms
Unity project `6000.4.8f1` (ProjectSettings). Verified here: Core build, NUnit 105/105, Unity-reference compile of
Game+Editor, headless harness. **Not verified:** Unity import/PlayMode, Windows/Android/iOS builds, device FPS.
Commands: `cd Tools/Tests && dotnet test`; `cd Tools/UnityCheck && dotnet build`;
`cd Tools/Sim && dotnet run -c Release -- seeds=2 maps=all generals=all`; `... perf <map> <general> <seed> 15`.

## 2. Acceptance matrix
See `QualityEnhancementProgress.md` (phase table with evidence paths).

## 3. Before / after (measured)
| Metric | Before | After |
|---|---|---|
| Managed alloc / sim tick | 2.0–7.6 KB | 0.45–0.53 KB |
| Squads leave their Barracks | no (bug) | yes (test `SquadsDeployOutsideTheirBarracks`) |
| Bot win rate A0 (256 runs) | 74% (Phase IV/V) | 77% |
| Settings rows visible | volumes cut off | 3 tabs, all visible |
| UI glyphs missing in font | ★ ◄ ► ∞ → ✔ ✘ ⚠ | replaced with text |

## 4. Issues / trade-offs
* All visual/audio work is compile-checked and logic-tested only; it was never seen or heard in Unity.
* Hit-stop is per-sprite (never global) by design; boss "slow-motion" is a camera punch, not time scaling.
* Highlights are an event summary with camera jump, not a replay (the simulation is deterministic, but no
  recorder/replayer of inputs exists yet).

## 5. Remaining limitations
Sprite atlas packing, draw-call/overdraw profiling, audio mix on real speakers, safe-area tests on notched phones.

## 6. 10-minute manual QA script (Unity)
1. Title → Chơi mới → Roland → Thung Lũng Đá Trăng → Bắt đầu. Check bird flocks and the day HUD.
2. Build 3 houses, a well, an archer tower (ghost shows reason/warning, range ring), a barracks; watch the rise animation and that soldiers walk **out** of the barracks.
3. Drag a wall across the east road with a gate; select the tower → range ring; upgrade → delta lines on the button.
4. Press BẮT ĐẦU ĐÊM → dusk bell + orange sky (2 s), horn caption, night music.
5. Fight: General swings (lean-back windup), element impacts per grammar, aggregated numbers; press Ultimate → anticipation ring, troop hop.
6. Let a wall break: rubble decal, "!" marker, danger music, nearby AI General bark (night ≥ 4 with an AI General).
7. Settings → Hình ảnh: shake Tắt/Nhẹ/Đầy đủ, damage text modes, ambient off; Điều khiển: reduced motion, AI debug overlay (reasons above soldiers).
8. Win the night → dawn chime, gold burst, report with side objective and "Điểm nhấn đêm" (Xem jumps the camera).
9. Play to night 5 boss → boss defeat banner, relief music.
10. Quit to title mid-day, Tiếp tục → state restored (rubble under ruined walls).
