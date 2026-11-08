# Assets — blank-slate layout, second pass (2026-11-08)

`Assets/` carries raw content and the first rebuilt pieces only. The full
legacy game — code, scenes, prefabs, materials, shaders, authored data,
vendored DLLs, extraction dumps — still mirrors its old paths under
`PreRebuild/` at the project root, outside `Assets/`: Unity never imports or
compiles it. `PreRebuild/` is tracked in git (not ignored) and every file's
`.meta` travelled with it, so restoring from there is a move, not a rebuild.

## Where things stand now

```
Assets/
  OpenFrontier/
    AudioClip/ Font/ Mesh/ Sprite/ Textures/    raw project content (first pass)
    Textures/FX/                                + particle_128, smoke — the two
                                                particle maps the FX closure uses
    Shaders/                                    OpenFrontier_ShipSurface,
                                                OpenFrontier_ShipCloak,
                                                OpenFrontier_LegacyParticle_{
                                                Additive,AlphaBlended},
                                                OpenFrontierLegacyParticles.cginc
    Settings/                                   OpenFrontier-URP + renderer asset
                                                + the three Build Profiles
    Effects/                                    Blue/Orange/Purple Engine Flare
                                                (user-made LensFlareDataSRP data)
    Resources/{Materials,Prefabs}               working session (uncommitted):
                                                Ship Standard/Cloak + Glass, the
                                                grabbed thruster/smoke prefab copies
    Ships/                                      working session (uncommitted): ship
                                                art and materials, and
                                                ActiveUnitRanger.prefab — a
                                                grab-and-strip reference (missing
                                                mesh/material/script refs by design)
  UniversalRenderPipelineGlobalSettings.asset   regenerated in place; stays the
                                                wired global settings
```

**`Assets/InterstellarPilot2/` is gone** — deleted in the second pass after
the user grabbed the thruster FX they wanted into
`OpenFrontier/Resources/Prefabs/`. Its former content lives on only in
`PreRebuild/InterstellarPilot2/` at the old paths.

## Restored exceptions (all with original metas, GUIDs unchanged)

- `OpenFrontier/Shaders/OpenFrontier_ShipSurface.shader` and
  `OpenFrontier_ShipCloak.shader` — copied back from PreRebuild in the first
  pass. URP-only (`ShaderLab` passes `LightMode = UniversalForward`); pink is
  the Built-in fallback signature, not an error.
- `OpenFrontier/Settings/` — moved out of `InterstellarPilot2/Settings`
  immediately before that tree was deleted. `GraphicsSettings` and every
  quality level reference its pipeline asset by GUID (`dc538db0…`), which the
  move preserved — `ProjectSettings/` needed no edits at all.
- The legacy-particle closure of the grabbed FX prefabs, moved as a set:
  `Shaders/OpenFrontier_LegacyParticle_{Additive,AlphaBlended}.shader` +
  `OpenFrontierLegacyParticles.cginc` (both shaders `#include` it by name,
  so shader and include stay together),
  `Resources/Materials/m_additive_128.mat` and `m_smoke_alphablend.mat`
  (referenced by every grabbed thruster/smoke prefab), and their maps
  `Textures/FX/particle_128.png` + `smoke.png`. The retired FX-driving
  scripts stay retired: grabbed prefabs import them as missing components —
  visuals intact, the new core will drive them.
- `OpenFrontier/Effects/* Engine Flare.asset` — the three user-made engine
  flare data assets, copied in from PreRebuild. Self-contained: their only
  reference is the URP core package's `LensFlareDataSRP` script. The
  component-side flare objects still live on the retired ship prefabs in
  PreRebuild (see the `ActiveUnitRanger.prefab` reference copy under
  `Ships/Ranger/`).

## Same-GUID caveat (unchanged rule)

Every restored/moved copy shares its GUID with the un-relocated original in
`PreRebuild/`. Invisible while the original sits outside `Assets/` — but a
wholesale restore of any PreRebuild folder must first delete the matching
in-Assets copy, or the duplicate metas collide.

## The meta rule (timeless)

Assets are identified by the GUID inside their `.meta` file. Move an asset
and its `.meta` together and every reference survives — move it without the
meta and it becomes a new asset with every reference dangling. Folder rules
are the same rule at folder depth.

## By-path traps on restore

- The legacy `Resources.Load` tree loads BY PATH. If
  `PreRebuild/InterstellarPilot2/Resources/` ever returns, it returns to
  `Assets/InterstellarPilot2/Resources/` — same-path restore only.
- The re-homed pieces under `OpenFrontier/` (Settings, particle shaders/
  materials/textures, flares) are now their GUIDs' live home. A wholesale
  PreRebuild restore must not recreate their old-path copies alongside them.
- `ProjectSettings/EditorBuildSettings.asset` still lists retired scenes by
  path+guid; they stay retired — the new core re-adds its own scenes when it
  brings them.

## The harness in this state

Zero `.asmdef` under `Assets/` keeps the harness loud-idle: `check.sh`
purges stale generated projects and reports green with a loud "build gate
idle" note, and re-arms strict the moment the first `.asmdef` lands under
`Assets/`. Loose `.cs` files (first: `OpenFrontier/Scripts/NavLight.cs`,
2026-11-08) compile into `Assembly-CSharp` editor-side only — the harness
cannot compile them, so the editor plus
`grep -c "error CS" Logs/Editor.log` is their real gate.
See AGENTS.md ("The rebuild blank slate") for the session law.
