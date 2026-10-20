# Test report (updated per phase)
| Suite | Command | Result |
|---|---|---|
| NUnit (core/rules/presentation logic) | `cd Tools/Tests && dotnet test` | 106/106 PASS (baseline) |
| Unity compile check | `cd Tools/UnityCheck && dotnet build` | PASS |
| Art validator self-tests | `python3 -m unittest discover -s Tools/ArtPipeline/tests` | 9/9 PASS |
| Art validator on game art | `python3 Tools/ArtPipeline/artpipe.py validate` | BLOCK 0 / WARN 90 |
| PlayMode / device | — | NOT RUN (no Unity) |
