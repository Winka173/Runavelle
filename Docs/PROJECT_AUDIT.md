# Runavelle — Project Audit (Phase 0)

Audit date: 2026-10-19. Performed by Claude Code in a Linux cloud container.

## Repository state at start
- Git repository `Winka173/Runavelle` was **empty** (no commits, no files). This is a **greenfield** project;
  nothing existing was replaced or deleted.

## Tooling actually available in this environment
| Tool | Status | Notes |
|---|---|---|
| Unity Editor | **Not available** | Linux container, no Unity install, no license. Cannot open scenes, enter Play Mode, run Unity Test Runner or build players. |
| Blender | **Not available** | Not installed. Asset pipeline uses procedural runtime sprite generation in C# instead (see ARCHITECTURE.md). |
| Unity / Blender MCP servers | **Not available** | Only GitHub + Claude Docs MCP servers are connected. No Unity/Blender MCP was used. |
| .NET 8 SDK | Installed (`apt install dotnet-sdk-8.0`, 8.0.132) | Used to compile the engine-free simulation core, run NUnit tests and a headless 15-night simulation harness. |
| UnityEngine reference assemblies | NuGet `UnityEngine.Modules 2021.3.33` (dev-only, not committed) | Used **only** to compile-check the Unity presentation layer. Unity 6 API compatibility of the subset used is assumed, not proven. |
| UnityEditor reference assembly | NuGet `UnityEditor 2019.3.2.1` (dev-only, not committed) | Compile-check of editor scripts only. |
| Android SDK/NDK, Xcode | **Not available** | No player builds were produced. |

## Consequences / honest limits
- The project targets **Unity 6 (6000.0.23f1 pinned in `ProjectSettings/ProjectVersion.txt`)**. This exact version was
  never opened here; any Unity 6000.x LTS should import it. First open in Unity is the first real Unity validation.
- No `.meta` files are committed: Unity generates them on first import. Nothing references assets by GUID — the game
  bootstraps from code (`RuntimeInitializeOnLoadMethod`) and builds all visuals procedurally, so missing-reference
  failures from hand-written GUIDs are impossible by construction.
- Gameplay simulation (economy, map, navigation, combat, elements, AI, waves, decrees, save) is pure C#
  (`Assets/Runavelle/Scripts/Core`, asmdef with `noEngineReferences`) and **is** compiled and tested here.

## Architecture decision summary
See `Docs/ARCHITECTURE.md`. Key choice: deterministic fixed-tick simulation in an engine-free assembly; Unity is a
thin presentation/input layer. This maximises what can be verified without the Unity Editor.
