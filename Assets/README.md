# Assets — the blank-slate layout (2026-11-08)

`Assets/` now carries **raw content only**. All game code, scenes, prefabs,
materials, shaders, authored data, vendored DLLs and the derived extraction
dumps were moved to `PreRebuild/` at the project root — outside `Assets/`, so
Unity never imports or compiles them. `PreRebuild/` is tracked in git (not
ignored); its metas travelled with every file, so restoring is a move, not a
rebuild.

```
Assets/
  InterstellarPilot2/Extracted/   AudioClip/  Mesh/  Sprite/  Texture2D/   (raw)
  OpenFrontier/                   AudioClip/  Font/  Mesh/  Sprite/  Textures/
```

`InterstellarPilot2/Extracted/` keeps the extraction dump's raw substance:
56+10 audio clips, 215 textures, 219 mesh `.asset`s and 191 sprite `.asset`s.
`Mesh/`/`Sprite/` are Unity-native `.asset` data — geometry and sprite frames,
not prefabs — so they count as raw here. `OpenFrontier/` keeps the project's
own art: 1,020 textures, 7 source meshes (fbx), 10 ogg clips, the font, and
`README.md`. Two intentional strays stay with their kept folders (Font's
default font material; Mesh's material) — harmless, unused, wanted there for
coherence with the assets they belong to.

**Already-copied exceptions:** `OpenFrontier/Shaders/OpenFrontier_ShipSurface.shader`
and `OpenFrontier/Shaders/OpenFrontier_ShipCloak.shader` were copied back from
`PreRebuild/` on 2026-11-08 **with their original `.meta` files**, so the GUIDs every
ship-hull and cloaked-hull material references (`0c7559b00079d89961773603f874bf92`,
`c9f2d8847ba14e6d9b03a55f21c76e04`) are unchanged. While the originals stay outside
`Assets/` the duplicate GUIDs are invisible to Unity, but a wholesale restore of
`PreRebuild/OpenFrontier/Shaders/` must delete these copies first or the same-GUID
metas collide. Both are self-contained: every `#include` is a URP/Core package path;
no project-local dependencies, no CustomEditor. **Also restored 2026-11-08:**
`InterstellarPilot2/Settings/` wholesale — `OpenFrontier-URP.asset` +
`OpenFrontier-URP-Renderer.asset` (URP pipeline, referenced by GraphicsSettings and
every quality level) plus the three Build Profiles — original metas, so the previous
build's settings resolve again; a wholesale restore of that PreRebuild folder later
must delete these copies first (same-GUID rule). All renderer external references
resolve inside the URP package; the regenerated root
`UniversalRenderPipelineGlobalSettings.asset` stays the wired global settings.

Everything else from the old layout mirrors its old paths under `PreRebuild/`:
`PreRebuild/OpenFrontierLogic/` (all twelve assemblies), `PreRebuild/Editor/`,
`PreRebuild/OpenFrontier/{Editor,Discord,Material,Prefabs,Resources,Scenes,
Shaders,worlddata}/`, `PreRebuild/InterstellarPilot2/{Plugins,Resources,
Scripts,Settings,StreamingAssets,TextMesh Pro,UI Toolkit}/` plus the URP
global settings, and `PreRebuild/InterstellarPilot2/Extracted/{GameObject,
LightingSettings,Material,MonoBehaviour,PhysicMaterial,Shader,TextAsset}/`.

---

## The meta rule (timeless)

Assets are identified by the GUID inside their `.meta` file. Move an asset and
its `.meta` together and every reference survives — scene to script, prefab to
mesh, material to shader. Move an asset *without* its meta and it becomes a
new asset; every reference to it dangles. Folder rules follow the same rule at
folder depth (each folder's `.meta` sits beside the folder).

## Two by-path traps that matter on restore

- **`InterstellarPilot2/Resources/` is loaded by path, not GUID.** The old
  tree's `Resources.Load` call sites used names like `Textures/Backgrounds/<x>`,
  `Prefabs/WorldData/AsteroidTypes`, EngineResources, avatars. If it ever
  returns to `Assets/`, it must return to its original path — a Resources tree
  restored anywhere else loads empty.
- **`ProjectSettings/EditorBuildSettings.asset` listed scenes by path *and*
  guid.** Both dangled after the migration (expected). The new core re-adds its
  own scenes when it brings them; do not chase the old list.

## The harness in this state

Zero `.asmdef` / zero `.cs` under `Assets/` is a deliberate state: `check.sh`
purges any stale generated project set and reports green with a loud "build
gate idle" note instead of building. It returns to strict mode the moment the
first `.asmdef` lands under `Assets/`. See AGENTS.md ("The rebuild blank
slate") for the ProjectSettings consequences and the frozen
`Library/ScriptAssemblies`.
