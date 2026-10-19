# Known issues / limits (honest list)

- **Never opened in Unity**: the project has only been compile-checked against Unity *reference* assemblies.
  First open may surface Unity-6-specific warnings/errors (e.g. obsolete APIs). Fix list should be short (pure C#, IMGUI, SpriteRenderer).
- No `.meta` files are committed; Unity generates them. Import settings are enforced by `RunavelleAssetImport`.
- Art is procedural (user's choice): coherent storybook vector style, but below hand-painted quality. Shorelines use
  circular blobs (scalloped look). Characters have 2 frames (idle/attack) + procedural bob/squash, no walk cycles.
- Audio is synthesized and was **not listened to** (no audio device). Music loops are short (15–27 s).
- AutoPlayer bot is weak; balance numbers reflect the bot, not humans. Roland underperforms with the bot.
- IMGUI layout tuned for 16:9; ultra-wide/4:3 not visually verified.
- Unity MCP / Blender MCP were not available in this environment; Blender was not used.
- Phase IV: new enemies/bosses use the shared 2-frame humanoid rig (beasts/machines bespoke); flying bosses use the
  standard flyer goal (dive at towers). Hazard overlays (tide, cracked ice, mist) and hazard VFX are only
  compile-checked; the snapshot renderer shows base terrain, not these overlays.
- Bot win rates rose sharply in Phase IV mainly because of the General-trap fix; difficulty for skilled players is
  expected to come from Ascension (Phase V).
- Phase V: the daily run is offline-only (date-seeded; no leaderboard). Share codes encode map/General order indexes,
  so they stay valid only while content order is append-only. Cosmetics are simple (badge ring, flag) and were only
  compile-checked, never seen in Unity. The share-code text field uses Unity's default IMGUI skin.
