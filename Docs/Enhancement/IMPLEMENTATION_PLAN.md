# Implementation plan (final quality / art / balance)

Order and rationale (recorded because it deviates from strict P1→P9):
0. **Quality gates first** (prompt: "establish measurable gates before broad asset regeneration"): Art Quality
   Validator + self-tests + waivers + CI, art pipeline CLI with staging/import/rollback, Blender availability.
1. P1 Castle growth & living world — Blender 3D→2D benchmark for castle tiers and kingdom forms, wired into ArtKeys.
2. P2 Army rally & night preview (confidence, bypass routes, threat lines).
3. P3 Breach crisis (fallback to inner defences, bounded re-path, crack stages from the pipeline).
4. P4 General–army synergy made visible (synergy links/projectiles/status), barks already exist.
5. P5 Kingdom Chronicle (deterministic event log, searchable, save migration).
6. P6 Loop polish (smart camera hints, quick retry, actionable insights).
7. P7 Art pipeline batch: character benchmark (idle/walk/attack/hit/death), REFINE fixes, before/after.
8. P8 Balance lab: 7 policies, paired-seed A/B, confidence intervals, preview-accuracy & exploit tests.
9. P9 Audio/accessibility/reliability: input remapping, profiling (path spikes after wall destruction), docs.

Every phase: code + tests + validator/harness evidence + STATUS update; no DONE without evidence.
