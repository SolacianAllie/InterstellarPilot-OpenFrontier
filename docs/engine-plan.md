# Engine Plan — Open Frontier rebuild

*Written 2026-11-12 so it survives context compression. This is the agreed
architecture for "the engine" milestone set. Companion docs:
`docs/gamemode-modding-design.md` (registry/modding decisions — still
authoritative), `docs/new-ui-guide.md`, `AGENTS.md` (traps — still
authoritative). Nothing here is implemented yet. Amended 2026-11-12,
second pass (design session — decisions pinned with the user): the tick
machine is pinned in detail (30/6/1 Hz tiers + resolver), unloaded space is
RESOLVED not ticked (orders-as-itineraries, event layer, escalation AI,
world journal, delta records), entity IDs are a fresh 64-bit kind-tagged
scheme, parallelism is budget-gated per platform, and the M0 assembly map,
scene-reference decision and EngineCore-at-boot are recorded. New section:
"The tick machine + world sim". Changed bullets are tagged [2026-11-12]
inline. Amended 2026-11-12, third pass (execution session, M0 landed
green-strict as `bb5607fb`): from-scratch content doctrine pinned (standing
rules) and the M1 PreRebuild extraction landed contract corrections
(Porting contracts section).*

## Status when this plan was written

- Menu UI is done: New Game screen fully wired (GamemodeList/Row/Card,
  `StartGamemodeButton` → logged stub, `GamemodeCard`, Create process).
  The UI-scale nesting bug was diagnosed (screens parented under the nested
  `Main` ConstantPixelSize canvas made their own 1920×1080 scalers inert —
  `CanvasScaler.Handle()` early-returns when `!m_Canvas.isRootCanvas`).
- Engine side is at **zero** in `Assets/`: no GameController/EngineASX/Unit/
  Faction/sectors/seeding/ShipController/cameras/docking. All live code is
  menu/UI/input/visual (30 loose `.cs`, no asmdef, harness LOUD-IDLE).
- Two inventory sweeps completed (see "Porting contracts" + "Live build state"
  below) — the raw reports predate this file; their essence is recorded here.

## The space model (agreed design)

Three layers. The old flat "sectors" concept is replaced.

### 1. Galaxy stage — exists ONLY to be photographed

- The user hand-crafts a galaxy scene. Voided skybox; stars are real objects
  in it: visibly brighter stars = visitable systems, dim ones = distant
  systems or padding. Nebulae (SpaceUnity content) become hand-placed
  dressing — the runtime-generated backdrop pipeline is dead.
- The scene carries **system anchors** (named objects) and **trigger boxes**
  the user authors as legal placement volumes where gamemode-placed or
  generated systems land.
- A gamemode's universe is a *data overlay* onto anchors/boxes.
- Load-into-system (loading screen or jump animation): camera setup at the
  system's galaxy position grabs a **full cubemap** → that becomes the
  system's skybox → galaxy scene unloads → loading mask released.
- Capture uses the same pinned-reference pattern as systems (below): shift
  the galaxy root so the destination anchor sits at exact zero, camera at
  origin. One pattern, two applications (system stage, galaxy stage).
- The grab may span many frames (one face per frame is fine); **the system
  load completes only when BOTH the sky generation is done AND the system
  scene has loaded**. Cubemap resolution follows graphics settings: 4K on
  highest (the one time mobile ships a 4K texture), scaled down on lower
  sliders.
- Because anchors are positioned in the galaxy, the player's **arrival
  position around the destination's central body is computed from the
  departure location** (arrival vector, scale-invariant direction).

### 2. Star system stage — systems are the scenes

- A system = one Unity scene: scaled-down space with "realistic" compressed
  distances. Contains the star(s), planets, moons, binaries, POIs,
  superliminal space, SafeZone/mass-lock volumes.
- Systems generate with a proper solar-system look: star, planets orbiting
  the star, moons orbiting planets, binaries (stars orbiting stars), other
  celestial configurations. **Orbits are analytic**: position = `f(gameTime)`
  from orbital elements — no per-frame integration drift, deterministic
  across MP, resumes from save by storing the time.
- Systems NOT currently in: visually unloaded and **never ticked** — their
  coarse state is RESOLVED on demand (see "The tick machine + world sim"
  below). Current system full-ticks. **[2026-11-12: replaces "slowly
  ticked"]**
- Systems are either placed BY a gamemode (referencing anchors/boxes) or
  generated for sandbox/custom gamemodes (into trigger boxes).

### 3. POIs — places within the system

- Stations, capital ships, asteroid clusters, random events (e.g. a trader
  who dropped out of superliminal), asteroid belts, stellar phenomena.
- POI arrival has its **own loading screen, masked as the drop-out wait** —
  POI content builds behind the mask before the player is placed in it.
  Arrival-triggered content unit, not background streaming.
- Player drops out **~10k from the POI centre**.
- Ticking: fully ticked while the player is there, occasionally ticked
  otherwise.

### Pinned origin — star system layer ONLY (critical, load-bearing)

- **The player's position in a system is exactly (0,0,0) at all times.**
  To move is to move the entire system around the player. This applies to
  the star-system layer only, NOT the rest of the game (galaxy stage, menus,
  other spaces are ordinary Unity space).
- Consequences (agreed explicitly):
  - **Simulation coordinates live in doubles** (player world position,
    velocities, orbital elements, POI/NPC positions). Floats only hold
    *render* offsets computed per tick as `simPos − playerSimPos` — bounded
    by the scaled system radius, i.e. permanently best-case float precision
    with zero mitigation machinery. No floating-origin recentering needed:
    there is nothing to recenter from.
  - **Player Unity transform locked at origin; rotation free.** Input feeds
    velocity into the sim; the world counter-moves.
  - **No Rigidbody flight.** Ship motion is script-driven (kinematic);
    trigger volumes/queries handle mass lock, SafeZone contact, POI grabs.
    (Agreed — also the right call for mobile.)
  - Galaxy-stage data (overlay placements, generation inside trigger boxes,
    anchor positions, arrival-direction math) are **doubles in content
    data**; floats decide only how things are drawn.
  - No snap artifacts ever (no discrete recenter events) — the earlier
    "POI centre = 0 position / origin snaps to reference" framing is
    **superseded** by continuous player-pinning; recorded to avoid
    confusion. The tight-orbit-station-arrival case stays on the
    verification list for celestial-rendering consistency.

### Local frames (frame grabs)

- Ships' movement is reparented to POIs on arrival/nearness — **configurable
  per POI type**: station POIs grab you fully on arrival (you ride rotation/
  orbit); planet POIs grab you based on distance from them; **arrival grabs
  override proximity grabs**.
- Frame switch preserves world transform + velocity (pure bookkeeping: whose
  motion carries you). Docked at a rotating station, the station spins around
  you while the outside sky sweeps past.
- **Planets are orbit-only frames. Landing is not possible** (mobile-first).
  No surface-relative frames, no spherical terrain feature.

### Rendering split

- **Celestial camera** (dedicated): renders the planet/bodies layer so
  planets are **always visible**. Elite-Dangerous-style **scale-by-distance**:
  distant bodies sit small on the sky layer; approaching, they grow smoothly
  to full apparent size. URP camera stack / layer separation.
- **The only ship visible at all times is your own.** Other ships render only
  in their POI-local context; superliminal ships render as comet trails only.

### Superliminal (intra-system FTL)

- Faster-than-light traversal of the system. Ships in superliminal tick fully
  when you share the system. They have **no visual model — a comet-looking
  trail effect** — and no collision with each other or the player.
- They CAN collide with **SafeZones**: sphere triggers placed around planets,
  stars, and other bodies. Touching one forces removal from superliminal
  back into regular space, **repositioned just past the collider** so the
  player is never stuck; they can turn away and jump out again.
- NPCs and player use the same layer/mechanics.

### Tearspace (inter-system FTL) + wormholes

- **Tearspace = this universe's hyperspace.** How you travel between systems.
  Accessible from **ship-based drives** (anywhere outside mass lock) or
  **jumpgates** (mass-lock-exempt structures).
- **Wormholes** work the same way but are **stellar phenomena**: they
  randomly form and dissipate across the galaxy, **always generating in
  pairs** so they link — and can connect **systems OR POIs** together.
  Generated content in the system-seed/galaxy phenomena tables; each pair is
  a linked entity pair; traversal reuses the jump machinery.
- The **jump animation masks** the destination system's load + the galaxy
  cubemap re-grab (the M2 pipeline is literally the underside of a Tearspace
  jump).
- Mass lock volumes gate engagement of BOTH superliminal and Tearspace,
  except jumpgates/wormholes.

### Mass lock (unified trigger volume)

- Sphere triggers placed by POIs **and** planets. Two behaviours, one
  component (`MassLockVolume`), per-source radius:
  1. **Block engagement** — cannot start superliminal/Tearspace inside one.
  2. **Force drop-out** — entering one while in superliminal removes you from
     it (this is the SafeZone behaviour, unified into the same component).
- Proposal accepted in discussion; if the user later wants them as separate
  spheres, split then.

### Ship flight (Everspace-2 / GoF2-remake style)

- **Full 6DOF**: forward/backward/up/down/left/right translation without
  rotating; rotation fully decoupled and optional.
- **Two thruster sets**: **Normal** (forward/backward, proper flight) and
  **RCS** — RCS activates with movement in their respective directions OR
  rotation, like real craft. Thruster VFX keyed to the same inputs.

### Multiplayer — day-one design constraint

- **Host/server authoritative.** Clients receive all their info from the
  host/server and **relay whatever they can to the host to minimise desync**.
- Implications baked in from M1: network-stable entity IDs, snapshot/relay
  state model, host owns ALL tick tiers; clients own only their local frame
  prediction. Netcode implementation itself comes later.

### Universe generation (same as original, 3D)

- Generates a **"spiderweb" universe**: systems as a connected graph of
  jump lanes, in 3D not 2.
- Each system gets a **seed that dictates everything**: star class, **star
  count** (alone / binary / trinary — **research task: real stellar system
  architecture** so it feels realistic), planets, moons, smaller POIs
  (asteroid belts, stellar phenomena the user will add).
- Seeded → deterministic/shareable systems.
- Gamemode-placed systems skip generation (anchor/trigger-box references).

### Dev gamemode (`core.devtest`)

- Ordinary `GamemodeDefinition` with `DeveloperOnly`; release players never
  see it. Resolves the gamemode doc's OPEN QUESTION **[2026-11-12]**: it
  boots the M1 bare stub scene — empty skybox; the engine boots; timeScale
  0→1; the chain proven by log/unscaled marker — and becomes the real dev
  sandbox by M6.

### Replacements

- **Imphenzia StaticStars: replaced** — stars come from the galaxy scene; the
  DLL is NOT re-homed. SpaceUnity nebula/star content survives as dressing
  content only. Nebula quad-generation pipeline not ported.

## The tick machine + world sim — pinned 2026-11-12

Everything below was designed and agreed in one session; it is the engine's
core contract and supersedes looser wording elsewhere in this file.

### Heartbeat

- ONE heartbeat: `EngineCore` — a persistent MonoBehaviour singleton,
  spawned AT BOOT through the LandingBoot seam ("grow this block as
  boot-time services arrive"). Its `FixedUpdate` drives every tier; Unity's
  Fixed Timestep is set to 1/30 s (user editor step: Project Settings →
  Time → Fixed Timestep 0.0333333). `timeScale == 0` stops it natively —
  the load/pause protocol needs no extra machinery.
- Why at boot and not lazily on first launch: the menu is intended to
  become an active engine world like the old game's (user decision). The
  menu-world design pass itself is DEFERRED — its own later conversation;
  the boot-time spawn is what is decided now. The menu scene stays
  standalone-playable in the editor regardless.
- timeScale protocol unchanged (0 = loading/paused, 1 = ready).

### Tiers — where the consumer sits relative to the player

Tier assignment changes ONLY on explicit events (POI enter/exit, system
load/unload). Never a per-tick reclassification scan.

- **FullFrame — 30 Hz**: the current system's live context: player
  kinematics, in-system NPCs, projectiles, active-POI logic, mass-lock/
  SafeZone bookkeeping.
- **SystemTraffic — 6 Hz** (every 5th tick): current-system off-POI
  bookkeeping — traffic director spawn/despawn decisions, in-system economy
  ticks, ambient events.
- **PoiAmbient — 1 Hz**: POIs in loaded contexts that are not the player's
  (a visited station's docking queue keeps breathing).
- **There is NO SystemSlow ticking tier** — unloaded systems are RESOLVED,
  not ticked (below). **[2026-11-12]**

### Phases + render sync

- Fixed order inside a tick: **Commands → Simulate → AI → Housekeeping**.
- Then, per RENDER FRAME (Update/LateUpdate, not FixedUpdate):
  **RenderSync** — the only gameplay transform writer, computing
  `simPos − playerSimPos` (the pinned-origin render offset) and driving
  transforms. Sim never reads render state.
- Sim/render decoupling lands at M1: sim state lives in plain structs
  (doubles); render reads a snapshot (the last two sim states are kept, so
  interpolation can be added). Whether we interpolate (smooth 60 fps for
  fast movers) is an M3 decision — the decoupling is what must exist at M1.
- MP shape: host runs every tier, clients run NO sim; the tick counter
  doubles as the sync frame index later. Per-tier budget diagnostics behind
  `DEVELOPMENT_BUILD || UNITY_EDITOR`, compiled out of release.

### Unloaded world — the resolver

Unloaded systems store nothing and tick never. Coarse state at sim-time t
is COMPUTED from three inputs:

1. **Seeds** — galaxy/system seed: everything procedural.
2. **Schedules** — seed-derived timelines of discrete events the resolver
   walks from last-observed time to now: wormhole form/dissipate windows,
   stellar phenomena spawns, scheduled faction conflict rolls.
3. **Player delta records — the only mutable memory.** Departure snapshots
   (written when leaving a system: station stock, standing) integrated
   forward (`stock = min(cap, snapshot + rate·Δt)`), plus player actions
   (kill, rescue, capture) timestamped at `t_interaction`. A delta
   OVERRIDES the schedule for the thing it touches.

- Resolution is pure math (microseconds), triggered on system load and
  galaxy-map refresh. A 30-system galaxy and a 1000-system galaxy cost the
  same while unloaded.
- The resolver's registry/entry-point seam exists at M1; it is exercised
  by M6/M7 content.

### Orders as itineraries (the plan layer)

Every NPC ship — loaded or not — carries an ORDER RECORD: an itinerary of
deterministic segments, e.g. `Buy(Station1, X, qty) → Fly(dest, topSpeed) →
Sell(dest)`. Travel time computed at order time from distance ÷ top speed.

- **Position at any t** = progress along the current segment, pure math.
  Arrival fires the segment's effect (the sell applies; stock/wealth
  update).
- **Continuation is generative**: when an itinerary ends, the next order is
  `f(hull seed, time, state)` — histories extend indefinitely, nothing
  ticking.
- **The simplification is UNIFORM**: loaded ships fly the same simplified
  itineraries (top speed, no exotic profiles). Watched and unwatched
  behavior can never diverge — the consistency tax is eliminated by
  construction. Loaded-only extras (collision avoidance, reaction to the
  player, combat) ride ON TOP of the itinerary, never instead of it.
- **Player interaction = delta record**: a fork at `t_interaction` (the
  attacker's itinerary terminates; the target's journal gains "rescued by
  player"; reputation/cargo deltas write). Every later query honors the
  fork.

### Event layer (encounters)

- Seed-scheduled encounter windows per system/lane; probability and
  outcome derived from factors: lane traffic density, faction relations,
  security presence, hull/escort strength.
- **While unobserved**: ONE abstract roll — attacker vs target (+escort) →
  outcome — applied and journaled ("freighter UH-4 attacked 14:02, lost 30
  rations, escaped"). Cost: one roll per window, not a physics sim.
- **OBSERVATION UPGRADES RESOLUTION — the rule that makes it honest**: a
  pre-rolled outcome stands only until someone can see it. Arriving
  mid-window materializes the encounter LIVE — the pirate is physically
  there closing on the trader — the real fight plays out and its real
  outcome supersedes the pre-roll; the journal records what actually
  happened. Present at the start: live from the beginning, never
  pre-rolled.
- First beneficiary: the player's OWN fleet (the Interstellar Pilot
  staple) — your freighters trade while you fly; the event layer can hit
  them; the journal alert arrives; jump in during the window and the
  rescue is a real fight.

### Reaction layer (escalation)

- A loaded ship runs cheap "am I on plan?" checks. Only a DISTURBANCE
  escalates it to the legacy-style state machine at FullFrame/
  SystemTraffic: player proximity, hostile contact, collision risk,
  interrupted order.
- Resolving the disturbance re-plans the itinerary; the ship de-escalates.
- CPU scales with disturbances and live encounters, NEVER ship count.
- Adaptive, reactive AI exists ONLY in loaded contexts. A context
  unloading mid-engagement freezes the AI into a pending order that
  resumes or resolves deterministically on next entry. "What ships did
  while you were away" is authored (itineraries + schedules), never
  emergent. Boundary chosen deliberately.

### World journal

- Append-only: `(hull id, order, action, sim-time, place, outcome)`.
- Written LIVE in loaded contexts; DERIVED from itineraries in unloaded
  ones. Persisted in saves (deltas + recent entries); MP-synced later.
- String formatting stays OUTSIDE the hot path: workers emit struct
  events; the main thread renders text, merged in sim-time order.

### Entity IDs — the fresh scheme

- **64-bit (`long`), `0 = invalid/none`.** Top byte = KIND (Unit, Faction,
  Fleet, POI, System, JumpLane, Wormhole, Effect, …) — an ID self-describes
  in a debugger; save validation routes by kind. Base = kind << 56.
- **Runtime-spawned** (NPC ships, projectiles): authority counter per kind
  from `base + 1`. The projectile/ephemeral range RECYCLES and is never
  saved.
- **Seed-derived** (generated systems, their planets/POIs/lanes/wormhole
  pairs): a function of (system seed, local index) — INTEGER-only math —
  identical on every machine, nothing to sync for MP, nothing stored.
- **Player things**: fixed constants/band near the kind's top (the "player
  faction lives at a known address" idea; no legacy numbers to honor — no
  saves exist).
- **Mods**: instances share the runtime counters — mod CONTENT is
  string-Id already (gamemode doc); instances are session-local host
  allocations.
- Registries key by ID, the legacy `Register/Deregister*` shape.

### Parallelism policy (PC threads; mobile stays its own optimum)

ONE mechanism, not two codepaths: a `ParallelismPolicy` object on
EngineCore; every parallel call site consults it.

- `MaxWorkers`: PC 8–12, mobile 1–2. `MinWorkToParallelize`: PC ~64 items,
  mobile ~1024. Boot-time settings, runtime-configurable
  (`JobsUtility.JobWorkerCount`) — a beefy tablet earns PC-ish behavior;
  NO `#if` forks at call sites.
- **Below threshold → run inline, serially** (on mobile with small counts,
  scheduling overhead can exceed the work — the threshold means mobile
  never pays that tax). Above it → ParallelFor.
- Mobile: Burst (ARM, 5–50× single-core, free) + zero-alloc steady state +
  serial-by-default. Parallelism reserved for SHORT UX bursts (system-load
  resolution, galaxy-map refresh).
- PC earns: resolver bursts, event-roll batches, per-entity integration at
  scale, TransformAccessArray transform writes, journal formatting,
  generation tooling.
- Thermal seam: the budget is the ONE knob that shrinks (workers,
  escalation caps, render resolution). Unity's Adaptive Performance
  package gets wired later only if a real device demands it.
- Structural rule: **parallelize WITHIN phases, never across tiers** — the
  tier machine's value is its sequencing, and ambient work is ~zero by
  design (the resolver's whole point).
- MP-safe by construction: clients don't simulate (host-authoritative), so
  hardware asymmetry cannot desync anything; the budget split is
  client-local.

### Two standing threading rules

1. **Hot sim/resolver paths are blittable structs only** — no strings, no
   managed objects, no registry access inside per-tick/per-resolve math.
   Journal text, names, UI queries stay outside. This one rule keeps
   everything above Burst-eligible for the life of the project.
2. **Seed-derived content and ID derivation use INTEGER-ONLY math.** Burst
   floats are IEEE per-operation but not guaranteed bitwise-identical
   across CPU architectures; hashes must match everywhere. Sim floats are
   fine (host simulates; clients receive).

### Optimization ladder (mobile-first order)

1. Burst the hot math — single-core native speed, the big one.
2. Zero steady-state allocations — GC hitches are the mobile killer.
3. Parallelize the BURSTS (resolver bursts, event-roll batches).
4. Parallel transform writes when entity counts earn it.
5. AI stays bounded by escalation; thread it last, only if profiled.

## Module structure (asmdefs) — the "don't tear the game down" answer

- `OpenFrontier.Engine` — sim core, units, space pipeline (stages, frames,
  celestial renderer, tick scheduler, superliminal).
- `OpenFrontier.Content` — `GamemodeDefinition` + future definition types and
  registries. **Dependency-light** — what the mod SDK will reference
  (Decision 8 in the gamemode doc).
- `OpenFrontier.Menu` — existing 30 menu scripts under asmdef.
- Editor tools stay loose under `Assets/Editor/` (platform-callback trap).

Re-homing map **[2026-11-12]**: Engine ← `Scripts/Input/` (ActiveInput,
InputSchemeTracker — game-wide input; the menu needs it today and
`StartGamemodeButton` calls the engine seam). Content ← `Scripts/Gamemodes/`
(Definition + Registry), `Scripts/SpaceUnity/` (NebulaAsset/StarsAsset) +
`Scripts/Common/` flag enums + `NavLight.cs` (a content component on ship
prefabs, driven later via `SetVisible`). Menu ← the 20 `Scripts/MainMenu/`
scripts. Dependency direction: Content → nothing; Engine → Content; Menu →
Engine + Content. Script bindings survive the move — GUIDs ride in the
`.meta` files, moved together. Namespaces untouched in M0 (zero-churn;
normalize opportunistically later). asmdef files are authored as JSON
directly (plain config, not prefab YAML).

Process consequences to execute deliberately:
- The **first `.asmdef` flips the harness loud-idle → strict**: run
  `bash tools/harness/check.sh --regen`, regenerate `codec-test`'s `sync.py`
  BEFORE trusting any green.
- `Library/ScriptAssemblies` staleness rules start applying cross-assembly
  (CS0103/CS0234 false-misses clear on editor refocus).
- New scripts get **fresh UUID hex GUIDs** (the deterministic
  `Guid(MD5(assembly+ns+class))` scheme is dead — verified unreproducible).
- Vendored DLLs re-home under `Assets/` only when their consumers land
  (Newtonsoft for saves, file pickers for the mod importer; **not
  Imphenzia**). All currently live in `PreRebuild/InterstellarPilot2/Plugins/`.

## Milestones

- **M0 — Assembly skeleton.** Four asmdefs (Engine/Content/Menu + nothing new
  for Editor), scripts re-homed per the map above, harness regenerated and
  green-strict. `check.sh --regen` flips loud-idle → strict; regenerate
  `codec-test`'s `sync.py` BEFORE trusting any green. No gameplay-visible
  change; everything later compiles against real boundaries.
- **M1 — EngineCore.** Persistent singleton spawned AT BOOT via the
  LandingBoot seam (port shape of GameController + EngineASX minus
  2.5D/sector specifics): registries, net-stable ID allocation (the fresh
  64-bit kind-tagged scheme), event bus, pools, fixed-step scheduler at
  1/30 s with the tick-tier machine (FullFrame / SystemTraffic /
  PoiAmbient + resolver seam; NO SystemSlow tier), four tick phases,
  per-frame RenderSync as the only gameplay transform writer, timeScale
  protocol (0=loading/paused, 1=ready), double-precision sim state +
  pinned-player architecture, sim/render snapshot decoupling, launch
  payload (`GamemodeDefinition` → payload → scene boot),
  `StartGamemodeButton` wired to it. Dev gamemode boots the BARE stub
  scene (empty skybox; engine boots; timeScale 0→1; chain proven by
  log/unscaled marker — zero content authoring).
- **M2 — GalaxyStage.** Galaxy scene contract (anchors + trigger boxes),
  cubemap capture rig (root-shift pinning, multi-frame capture, resolution
  from graphics settings), arrival-vector math, loading/jump integration.
  Load completes only when sky AND system are both ready.
- **M3 — SystemStage.** Scaled space; analytic orbits (star/planets/moons,
  binaries); SafeZone/mass-lock volumes; Frames (arrival > proximity);
  celestial camera with scale-by-distance; masked POI arrival (~10k drop-out,
  content build behind drop-out wait); sim/render interpolation on-off
  decided here (the M1 snapshot decoupling is its prerequisite);
  **one hand-authored dev system with a station POI** the game boots into.
- **M4 — Player ship.** 6DOF controller, Normal + RCS thruster logic and VFX,
  chase camera; **Ranger made playable** — fixes the dangling hull-mesh guids
  in passing (Ranger `3c92e89f6ee555a1e98457edce589bc3`, Overlord
  `4be487fa4407ab03389e8a38408dee49` — resolve nowhere; geometry exists as
  `Assets/OpenFrontierShips.fbx`, different guids; also explains a
  hull-less menu hologram).
- **M5 — Superliminal + Tearspace.** Point-mass layer, comet trails,
  SafeZone forced drop-out + just-past reposition, POI drop-out spawning from
  traversal; Tearspace engagement + jump sequence (galaxy re-grab under the
  mask); wormhole traversal basics.
- **M6 — Living world.** Unit/faction/AI port onto the tick tiers with the
  THREE-LAYER AI from day one — itineraries (plan), event layer
  (encounters: abstract while away, live when observed), escalation
  (legacy state machines as the reaction vocabulary); world journal +
  delta records; NPC superliminal traffic materializes on-itinerary;
  `core.devtest` becomes the real sandbox.
- **M7 — Universe generation.** 3D spiderweb graph, per-system seeds,
  seed-derived INTEGER entity IDs (identical on every machine), schedule
  tables (phenomena, conflict rolls, encounter windows), stellar research
  tables (binary/trinary configs, belts, wormhole spawn rules).
- **Later, on day-one foundations:** netcode implementation, saves (world
  journal + delta records persist; itineraries re-derive — no live world
  state needed), mod loader (ModLoader + Game Extras mods screen = one
  milestone together, per the gamemode doc's build order).

## Porting contracts (from the PreRebuild inventory — read-only reference)

All in `PreRebuild/OpenFrontierLogic/OpenFrontier.IP/...`, assembly
`OpenFrontier.IP`:

- **Launch chain**: `ScenarioUI.PlayScenario(loadData, launchOrigin)` (STATIC,
  mutates GameController singleton) → `ScenarioLoader.TryLoadScenario` on
  GameController → `Time.timeScale = 0`, music seam, menu screens destroyed,
  LoadingScene loaded ADDITIVE, 1s delay → **`WorldBase.LoadData = payload`
  (STATIC field is the scene-boundary carrier)** →
  `LoadSceneAsync(ScenarioInfo.TargetSceneName, Single)`.
- **`ScenarioLoadData`**: `FullSaveGamePath` (null = new game), `ScenarioInfo`
  (has `TargetSceneName` string), `SaveVersion`, `SeedSettings` (optional
  override), `CustomSeed` (−1 = random-assigned). `UniverseScenarioData`
  adds `GameTypeInfo` + `PilotName`. Payload composed AT CONFIRM TIME,
  incrementally across three screens (Select → SeedSettings → GameType/
  PilotName) — carried as a serialized field on a persistent screen; a
  spawn-per-navigation port must pass it explicitly.
- **WorldBase** (2006 lines): world scenes carry it; `Awake` loads
  `EngineScene` ADDITIVE (the world boots the engine, never the reverse);
  `Update` polls engine readiness then `Init()`; adopts static `LoadData`,
  branches save-load vs new-game; `NewGameRoot.transform.DetachChildren()`
  after seeding (authored starter kit becomes engine-owned).
- **EngineASX** (5630 lines, additive EngineScene, outlives worlds —
  menu world and galaxy world share ONE instance): registries
  (`Register/Deregister{Unit,Sector,Faction,Fleet,...}`), `FixedUpdate`
  30 Hz (stops under timeScale 0), pools from
  `Prefabs/WorldData/{Projectiles,Lasers,Particles,AudioSources,...}`,
  ID counters start at 100000, `ActiveSector` setter is a monster
  (sector activation = backdrop, HUD caches, camera particles, event),
  `NotifyWorldLoadedAndReady()` → timeScale=1, `LoadedAndReady=true`,
  clears `WorldBase.LoadData`.
- **GameController** (1148 lines, DontDestroyOnLoad, booted by
  `GameControllerInstantiator`): Awake loads UIControllerScene additive,
  pre-loads sector templates/unit classes/ScenarioInfos from
  `Resources/Prefabs/WorldData/*`, creates the camera, owns
  `GameSettings.DefaultWorldSeedSettings`; LateUpdate pins the skybox camera
  at the active sector (world-pinned starfield discipline).
- **`WorldSeedSettings`**: ~28 seeder-layer settings (one MonoBehaviour field
  each; ~45 `WorldSeederLayer` components seed the world). The eventual
  mod-tweak surface; `GamemodeDefinition` SO pattern carries it as an asset
  reference. Fallback default lives on GameSettings.
- **`ScenarioInfo`**: MonoBehaviour asset (not SO): `TargetSceneName`,
  `InitialScene`, `PilotFaction`, `Prereqs` (direct refs), `UniqueId`,
  completion in PlayerPrefs (`scenario_<id>`).
- **`UniverseGameTypeInfo`**: spawn/credits/security preferences consumed by
  `SpawnPointSeeder` during seeding.
- **timeScale is the whole load/pause protocol**: loader zeroes it,
  LoadingScreenUI re-zeroes, WorldBase.Init sets 1,
  `NotifyWorldLoadedAndReady` sets 1; every pause screen zeroes it;
  `EngineASX.FixedUpdate` silently stops at 0. New LandingBoot/SceneFade
  already follow the unscaled half.
- **Scene names are serialized strings** (`TargetSceneName`, ScreenNames
  constants). DECIDED [2026-11-12]: keep strings; a static constants class
  is the single source of spellings (the ScreenNames shape). No enums/GUID
  lookups.
- **Menu IS a world**: `MainMenuScenario` → `MainMenuWorldController`
  re-seeds per visit; `AutoSpectator` orbits a ship. NOTE: this does NOT
  exist live (the gamemode doc's "already re-seeds" refers to the
  reference). Current menu backdrop is statically authored. DECIDED
  [2026-11-12]: the rebuild's menu WILL become an engine-world customer
  (user wants the menu active like the old game) — EngineCore therefore
  spawns at boot; the menu-world design pass itself is deferred, not
  designed yet.
- **`UIWinScreen`** does a naked `LoadScene(MenuSceneName, Single)` — the one
  transition without loader/fade (note, don't copy).
- Port doctrine: read old classes for (1) features to carry, (2) engine entry
  points called — the real contract, (3) engine-vs-layout split; port only
  (2). Old layout/flow are suggestions. Never wholesale-copy EngineASX;
  extract registry/sim-loop concepts, rewrite space-facing parts.

**Contract corrections from the M1 extraction (2026-11-12 — full report in
the session; facts, not opinions):**

- **The decompiled engine is NOT persistent.** EngineASX has no
  DontDestroyOnLoad, EngineScene loads additively per world, and the
  Single-mode launch load unloads it — every world (menu included;
  MenuScreenScene carries a WorldBase) built a FRESH engine; only the
  statics (`EngineASX.Instance`, `LoadedAndReady`, `WorldBase.LoadData`)
  survived scene boundaries. The old "menu world and galaxy world share ONE
  instance" line above was belief, not fact. EngineCore's DDOL-at-boot
  design is the deliberate improvement and stands.
- Legacy budgeting read `Time.deltaTime` inside FixedUpdate — a frame delta
  in the fixed step. Budget in TICKS (integer cadence), never frame deltas.
- timeScale had FOUR unrelated writers (loader, LoadingScreenUI.Start,
  WorldBase.Init, NotifyWorldLoadedAndReady). EngineCore is the single
  authority for the load/pause protocol in M1.
- WorldBase's init poll swallowed exceptions via try/finally — a failed
  init read as a ready world. M1 fails loudly.
- Watched-sector ticking had a gap (ships ticked in watched sectors while
  inactive updates skipped them — stations/cargo never did). The
  kind-tagged tier system decides this explicitly; do not inherit it.
- `ActiveSector`'s setter fused sim state with presentation (pool returns,
  HUD caches, backdrop, camera particles) — decompose into a sim event +
  render reaction under the RenderSync doctrine when sectors arrive (M3).
- Legacy engine→UI coupling: the engine loaded its own screens and pushed
  toasts through UIController. The event bus carries notifications instead.
- Bugs to NOT reproduce: mission data loaded twice; two off-by-one count
  getters; `LoadingScenario` event fired before the payload was assigned.
- 2.5D assumptions are baked into otherwise-portable primitives
  (Y-flattened inactive-player motion, 2D map positions, radial universe
  bound). The rebuild's sim is full 3D doubles — nothing of that shape is
  carried.

## Live build state (Assets/) — pointers

- `Scripts/MainMenu/` (20 files), `Scripts/Gamemodes/` (GamemodeDefinition,
  GamemodeRegistry — lazy Resources load of `Gamemodes`, `Warmup()` called by
  LandingBoot), `Scripts/Input/` (ActiveInput, InputSchemeTracker),
  `Scripts/SpaceUnity/` (NebulaAsset/StarsAsset data components + flag enums),
  `Scripts/Common/`, root `NavLight.cs` (engine-decoupled chip awaiting a
  `SetVisible` driver), `Assets/Editor/` (GamemodeScaffoldInstaller,
  HoloOutlineNormalsBaker, HoloPostFXInstaller).
- Scenes: `Landing Scene.unity` (LandingBoot + Loading Screen prefab: camera,
  2 canvases, tips/ship-picker, HOLO instances), `Main Menu.unity` (camera,
  AudioListener, 6 canvases, screens incl. NewGameMenu/SPOptions/MP Options
  as Fullscreen Menu.prefab instances, Ranger/Overlord/PlanetAlpine2
  instances, hologram rig, EventSystem, SafeAreaRoot).
- Content: ship FBX/textures (no scripted ship prefabs — Ranger/Overlord unit
  shells are scriptless strips with dangling nested prefab refs), 13 Planet*
  shells (Sphere ×900 + Atmosphere ×910, visual-fold complete, 0 scripts),
  SpaceUnity 1040 files (338 NebulaAssets + materials, 10 StarsAssets),
  hologram pipeline (HologramShip clock + HoloSpinner + outline baker),
  109 ogg audio, 18 shaders, URP settings + Build Profiles
  (Win/Linux/Android).
- `GamemodeRegistry` consumers: GamemodeList/Row/Card read it; registry is
  single source of definitions; `Register()` reserved for ModLoader.
- LandingBoot (build index 0) → `GamemodeRegistry.Warmup()` → cross-fade
  load of "Main Menu"; "grow this block as boot-time services arrive" is the
  sanctioned boot seam. Menu scene must stay standalone-playable.
- Harness is LOUD-IDLE (0 asmdef under Assets) until M0 flips it strict.

## Process / verification rules (standing)

- **From-scratch content doctrine [2026-11-12, execution session]:** game
  content is authored fresh — nothing NEW consumes legacy content assets.
  Already-integrated legacy assets (menu holograms + Ranger/Overlord, the 13
  `Planet*` shells, SpaceUnity nebula/star dressing, ship FBXs/textures,
  audio, the two legacy particle shaders) stay load-bearing until their
  authored replacements land — no removal schedule; they are simply never
  built upon. PreRebuild's decompiled code stays READ-ONLY reference for
  entry-point contracts (port doctrine unchanged: port the contract, never
  the code — and none of it is copied).
- Headless Unity does not work here (licensing, exit 198) — the USER performs
  all editor actions; verify via `tools/harness/check.sh` +
  `grep -c "error CS" Logs/Editor.log` after editor refocus.
- Mobile-first cost rules (AGENTS.md) apply to every engine decision:
  diagnostic work compiled out, no scene-wide searches on render paths,
  per-frame work is a per-frame tax, allocations vs GC hitches.
- Branches: everything lands on `Dev`; `git push origin Dev`, never bare
  `git push`. Agent commits sweep the user's uncommitted working tree
  (name what was theirs vs mine; skip scratch, list it).
- Verify, don't assume: grep every caller, read whole methods, check both
  sides of two-system contracts. Cheap reads beat wrong assumptions.
- One MonoBehaviour per file, filename == class name. Editor-authored
  prefabs only — never hand-edited YAML. After touching prefabs run
  `check.sh`, not just the compile.
- Verification list item carried from design discussion: tight-orbit station
  arrival (celestial rendering consistency under pinned origin).
