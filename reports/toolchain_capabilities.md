# Toolchain capabilities (measured)

| Tool | Result |
|---|---|
| python | 3.13.16 |
| blender | Blender 4.0.2 |
| unity_editor | **not available** |
| dotnet | 8.0.132 |
| git | git version 2.43.0 |
| unity_project_version | 6000.4.8f1 |
| blender_render | Cycles CPU (denoiser unavailable in this build); EEVEE fails headless without EGL |

Python modules: PIL 12.3.0, numpy 2.5.3, skia 144.0.post2

Unity packages (Packages/manifest.json): `com.unity.2d.sprite` 1.0.0, `com.unity.modules.animation` 1.0.0, `com.unity.modules.audio` 1.0.0, `com.unity.modules.imageconversion` 1.0.0, `com.unity.modules.imgui` 1.0.0, `com.unity.modules.jsonserialize` 1.0.0, `com.unity.modules.screencapture` 1.0.0, `com.unity.modules.ui` 1.0.0

Consequences: Unity import/compile in the Editor, play mode, in-engine screenshots and player builds are **BLOCKED** here (no Unity Editor). The Unity layer is compile-checked against reference assemblies (`Tools/UnityCheck`). Scene previews use `Tools/Art/render_snapshot.py`, which draws real simulation snapshots with the shipped sprites and manifest pivots — a faithful layout preview, not a Unity capture. MCP (Unity/Blender) is not connected; CLI is used.
