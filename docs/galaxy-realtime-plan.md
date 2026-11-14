# Realtime Galaxy Renderer — plan

*Written 2026-11-14 so it survives context compression. This is the agreed
plan for the realtime galaxy system that replaces the scrapped bake-and-place
galaxy (revert of the removal commit restores every byte of that system —
it is retired, not deleted). Companion docs: `docs/engine-plan.md` (stage
contract + milestones), `AGENTS.md` (traps). Evidence for every ED claim
lives in `/home/deck/ed-assets/docs/FINDINGS.md` (read-only carve) and the
game's own `GraphicsConfiguration.xml`.*

## Why this shape

The user's goal, verbatim: **"i want a realtime renderable version so i can
build it in unity, or have others make their own galaxies. it will also be
useful for building a procedural galaxy builder."**

So the galaxy is NOT a baked artifact. It is one authored resource plus a
seed, rendered live:

```
GalaxyBlueprint  (ScriptableObject — the "one non-programmatic resource")
  ├─ optional painted 256×256 top-down density map (brightest channel)
  ├─ else: analytic spiral (arms/pitch/bar/bulge/disc falloff) — ProceduralDensity()
  ├─ shape / palette / point-count / haze / twinkle params
  └─ seed  →  deterministic in (seed, params), System.Random
        ↓
CDF sampling (CPU, once per reseed, few ms for 12k points)
        ↓
GalaxyInstanceCloud — one Graphics.DrawMeshInstancedIndirect per frame,
  camera-facing clip-space billboards, additive, queue 2990
        ↓
ONE draw seen by EVERY camera: ortho MAP view and perspective Galaxy Camera
  (billboard math is per-camera in the shader, so no second draw)
```

Frontier's own renderer follows this shape (seeded instance sets rebuilt per
view; the density data is theirs to author) — structure borrowed, nothing of
theirs shipped. ED numbers referenced for budgets only: MW instances
8000/16000 (low/high), star instances 2000/4000, nebulas 10/30 + 40
background, LocalDustBrightness 0.13/0, 64³ vs 256³ BC7 nebula volumes.

**Hard budgets (mobile-first):** 48k instances hard ceiling
(`GalaxyBlueprint.MaxStarCount`), default 12k = 24k tris + ~576 KB instance
data; per-frame CPU is zero (reseed only); one cloud draw (+ one future dust
draw). No URP C# types in Content. No `_Time` in the shader — the stage
boots at timeScale 0, so the sky animates on the `_OpenFrontierGalaxyTime`
global pushed from `Time.unscaledTime` (the `_OpenFrontierHoloTime` pattern).

## Current position — update at every landing (cross-chat recall anchor)

- **Phase 1 LANDED `9126d385`** (Dev): `GalaxyBlueprint.cs` +
  `GalaxyInstanceCloud.cs` (Content), `OpenFrontier_GalaxyCloud.shader`,
  `GalaxyCloudInstaller.cs` (Assets/Editor, idempotent — authors
  `Assets/OpenFrontier/Galaxy/{DefaultGalaxy.asset,GalaxyCloud.mat}`, drops
  `hand/Galaxy Cloud` under the scene's `hand/` container, default tilt
  24/0/12). Proven in the editor: cloud renders. Harness red afterwards is
  ONLY ScriptAssemblies staleness (CS0246 GalaxyBlueprint in
  Assembly-CSharp-Editor; defining assembly compiles clean, Content.dll
  predates the file) — clears on editor refocus.
- **Phase 2 LANDED (`f26cd2f2` haze/temperature/twinkle + `3393d924` scene
  wiring), phase-2.5 landed same session (ED textures + real density map):**
  - **ED evidence update:** the carve contains NO flat galaxy image and no
    skybox/backdrop cubemaps (grep-verified: `galaxymapui*` is UI chrome —
    codex icons/badges/panels; skybox/backdrop/cubemap = zero matches).
    ED's map galaxy is GENERATED live from catalog point data — the same
    architecture as ours. What ED ships and we now USE: the `stars` sprite
    family (the carve keeps 128² LOD copies; the full 512²
    `systemmap_stars.pdiffuse_alphatexture` re-packs rows 0–3 into
    `Assets/OpenFrontier/Galaxy/GalaxyStarSprites.png`, 8×4 cells, alpha =
    sprite shape) — decoded offline via BC3/BC7 (ffmpeg for BC1, python
    `texture2ddecoder` for BC7; tooling under /tmp/opencode/edgalaxy).
  - **Galaxy shape** comes from `GalaxyDensityMap.png` (512², authored by
    us: bar+bulge+hotspot, 2 strong + 2 weak log-spiral arms at 22° pitch,
    dust rifts along arm inner edges, the great rift, clump noise) —
    assigned to `DefaultGalaxy.asset`'s densityMap; `GridResolution` 512 so
    the GetPixels32 fast path hits. The procedural spiral stays as fallback
    (contrast boosted in look v3); the map is the Phase-4 painter's surface.
  - Shader draws stars from the sheet (`fx.w` 0..31 = cell; 241 = haze
    profile; 242 = procedural diffraction cross for the brightest);
    `GalaxyCloud.mat` carries `_SpriteTex`. lookVersion 3 = procedural
    fallback retune only (the user's tuned asset values are preserved:
    armContrast 3, starCount 48000, sizeMin 1).
- **Traps a fresh session must not re-learn:** shader compile failures are
  SILENT (last good version keeps rendering — grep
  `Logs/Editor.log` for `shader error` after every shader edit);
  `check.sh --regen` whenever .cs files arrive/depart; the user performs all
  editor actions; everything lands on `Dev`, never a bare push; one
  MonoBehaviour per file; no native calls in field initializers/static ctors
  (`Shader.PropertyToID` in static init is fine — URP itself does it).

## Phases

### Phase 1 — pipeline (DONE)
Blueprint + CDF sampler + instanced cloud + shader + installer, wired clean
into the scene contract. Delivered: `9126d385`.

### Phase 2 — the band reads as a galaxy (this session)
- **Haze layer:** `hazeFraction` of points become large (25–90 u), very dim
  (peak alpha 0.05), steady sprites sampled from the SAME density field —
  additive stacking turns confetti into a continuous glowing band.
- **Star temperature re-tune:** correlated warm↔cool jitter (jitter slides
  r up / b down around white), `colorJitter` 0.18 → 0.09, tighter density
  boost. Old assets migrate once via `lookVersion` (OnValidate + Generate
  guard) and a "Reset To Default Look" context menu.
- **Twinkle + variants:** per-star amp 0.06–0.22, speed 1.2–3.8 rad/s on
  unscaled time; brightest stars (dens > 0.85, 40%) get the
  diffraction-cross sprite; haze never twinkles.
- **Reseed hook:** `ReseedAllRandom()` static (event-time scene scan — never
  on the render path) + per-instance context menu. M7 wires it.

### Phase 3 — dust, nebulae, depth (next session(s))
- **Dark dust lanes:** SECOND small buffer + draw, alpha-blended
  (`Blend SrcAlpha OneMinusSrcAlpha`) ABOVE the cloud draw (queue ~2991),
  sampled from a lane field along the arms' inner edges — additive sprites
  can't darken, so dust must occlude. This is ED's dark-rift read.
- **Nebula cards:** world-pinned large dim textured quads at queue 2999
  (house backdrop tier), palette-matched; the SpaceUnity nebula materials
  already live in the repo at that exact queue.
- **Optional hero volume:** one 64³ BC7-ish volume texture + 256×1 color
  ramp raymarched (ED's low-res shape) — only if the cards aren't enough;
  mobile budget decides.
- **MAP camera tuning:** inside-the-bulge brightness (ED answers with
  LocalDustBrightness 0.13 at Low + an offscreen-RT capture pass — our
  cheaper knobs: fade-by-bulge-radius, or distance-based dimming baked into
  per-instance data at reseed since the capture camera position is known).

### Phase 4 — the builder (last)
- Seed→spiral generator (writes a `GalaxyBlueprint` from a few dials),
- density painter (paints the 256×256 map; `ProceduralDensity()` is public
  exactly so the painter can render it as a starting image),
- importer for hand-painted maps (sets Read/Write + linear import),
- maybe: ED reference PNGs under gitignored `reference/` for palette
  calibration only.

## Verification workflow (every landing)

1. `bash tools/harness/check.sh` (green; `--regen` if files moved in/out).
2. User refocuses Unity → let import FINISH before Play (mid-play reload
   hollows runtime singletons).
3. `grep -in "shader error" Logs/Editor.log` + `grep -c "error CS"
   Logs/Editor.log` (want 0) — the shader silent-failure gate.
4. Visual pass in edit mode (the cloud renders without Play) then Play.

## Open decisions (park, revisit with eyes on screen)

- Dust lane shape: procedural lanes vs painting them into the density map
  (the map already fully replaces the spiral when present).
- Per-jump reseed intensity: full reseed (ED) vs partial jitter (cheaper
  read as "the same sky, shifted").
- Whether the MAP capture view needs the offscreen-RT trick at all before
  the hero-volume question is even live.
