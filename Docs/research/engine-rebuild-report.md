# Open Frontier — Engine Rebuild Report

**Scope:** deep research into how to rebuild/optimise the engine of
*InterstellarPilot: Open Frontier* — with focus on **factions, AI and system
generation**, including what can safely be multithreaded.
**Date:** 2026-11-08 · **Status:** research complete; no code was modified.
**Author:** research agent session (findings file also saved at
`~/.opencode/plan/engine-rebuild-research.md`).

Companion reading: [`AGENTS.md`](../../AGENTS.md) (non-obvious traps),
`Docs/multiplayer/*` (authority model), `README.md` (platform floor).

---

## Contents

1. Executive summary
2. Method and evidence
3. Ground truth about the project
4. Where the code actually is (map)
5. How the simulation runs today (the real architecture)
6. Faction system — findings
7. AI system — findings
8. System generation — findings
9. Peripherals that shape the plan (save, multiplayer, assets, rendering)
10. Cross-cutting risk register (R1–R9)
11. What "rebuild from the ground up" should mean here
12. Phased roadmap (0–7) with acceptance criteria
13. Multithreading strategy and the threading contract
14. Measurement plan (Phase 0 detail)
15. Open questions that change the plan
16. Appendix A — hot file (LOC) table
17. Appendix B — PerformanceSettings budget inventory
18. Appendix C — resume recipes for future sessions

---

## 1. Executive summary

The headline instinct — "the majority of its code is dated and needs hard
optimising" — is **half true**, and the half that is false matters more.

Decompiled-era code that has already survived one serious, well-documented
optimisation campaign:

- The simulation is on a single heartbeat: `EngineASX.FixedUpdate` at a fixed
  0.0333 step (30 Hz), display-rate independent, with pause semantics preserved
  and per-system Profiler markers already installed.
- Per-frame Unity dispatch fan-out was killed twice: the per-unit
  `UnitComponentHolder.Update()` (≈6,308 dispatched callbacks/frame at 3068
  units, ~2.1 ms) and the per-faction `Faction`/`FactionAIBase` updates
  (≈3,044/frame at 1024 sectors, ~1.1 ms) were moved into centralised tick
  loops, with commentary proving identity of behaviour.
- Nearly every AI/economy cost is a named, serialised budget in
  `PerformanceSettings` — trade searches, NPC target scans, turret scoring,
  heatmap cells, expansion checks — including separate budgets "when the player
  is involved".
- The data the sim produces is already id-keyed and bucketed by sector.

So the *skeleton* is functional and modern. The problems are structural, and
they sit exactly in the three areas the project owner flagged:

| Area | Verdict | Structural problem |
|---|---|---|
| Factions | sound cadence (`UpdateLight` every tick / `UpdateHeavy` every 3 s) | heavy pass walks *all* fleets × sectors per faction; world-scale scans inside passes; faction data is scattered through the god-object |
| AI | exemplary budget architecture | AI is **latency-bound by design** — the data structures under the budgets (intel DBs, candidate lists, physics-Query broad phase) are the upgrade surface |
| System generation | layered pipeline is a good design | **entirely synchronous on the main thread**; MonoBehaviour-based layer tree; blueprint generator still allocates; story scenarios bypass it entirely (baked scenes) |

Plus one engine-wide finding worth flagging above the others: **intel scans
query the whole physics world** (`Physics.OverlapSphereNonAlloc` in world
space) — every sector's colliders participate at 1024 sectors — while the sim
already maintains per-sector unit buckets that could replace the physics query
for almost all cases (R1).

The recommendation is **strangler, not rewrite**: keep Unity, the prefab
pipeline, the multiplayer protocol and the save format; decompose `EngineASX`
into plain-C# modules with owned state; label generation off-thread first,
factions/AI restructure second, and reach for Jobs/Burst only where measured and
only for stranded or array-parallel epics — never per-frame, because this is a
30 Hz mobile sim first.

---

## 2. Method and evidence

Read-only research session, no repo modifications. What was done:

- Full project census: 2,526 `.cs` files, ~349,921 LOC (scripts), 1,293
  worlddata prefabs, 21 scenes, ~5 GB LFS-managed assets.
- LOC tables per file and per directory to find the hot half of the code.
- Structure extraction (method-signature maps) of the five hottest engine files:
  `EngineASX`, `UnitComponentHolder`, `Faction`, `FactionAIBase`, `NpcPilot`.
- Close reads of: the simulator core tick graph (`EngineASX.cs:3925–4510`),
  the per-unit tick (`UnitComponentHolder.cs:1210–1556`), the faction tick
  split (`FactionAIBase.cs:1088–1182`), the NPC tick cascade
  (`NpcPilot.cs:1159–1368`), target scanning (`NpcTargetScanner.cs`, 205 LOC),
  faction intel scanning & pathfinding (`FactionIntel.cs:830–960`), world
  generation (`WorldSeeder.cs` whole, `WorldBlueprintSectorGenerator.cs` whole,
  `WorldBase.cs:1500–1665`), and `PerformanceSettings.cs` (whole file).
- Project docs read: `AGENTS.md` (591 lines, primary source of engineering
  constraints), `Docs/multiplayer/status.md` (authority model and verified
  feature list), `README.md` (platforms, Unity version, licensing).
- Cross-checks: assembly/asmdef layout, harness tooling inventory, count of
  `Update()` callbacks (223), LINQ density in engine code (164), physics query
  call sites (31), Instantiate call sites (70).

Anything in this report that has **not** been measured by profiling is marked
*hypothesis*. The Next-Step section (Phase 0) exists precisely to convert those
into numbers.

---

## 3. Ground truth about the project

| Fact | Value / implication |
|---|---|
| Engine | Unity **6000.6.4f1**, URP 17.6.0, Forward |
| Priority | **Mobile first, PC second** — every decision judged on a mid-range phone |
| License | AGPL-3.0; name/branding trademarked separately (rebrand checklist in `TRADEMARK_POLICY.md`) |
| Scale of codebase | ~2,500 `.cs` files / ~350k LOC scripts; UI half is most LOC but not most CPU |
| Scale of assets | ~5 GB, everything in Git LFS (recovery clones must `git lfs fetch` or ~1890 assets break) |
| World size | sector-graph generator historically ceiling 64, tested to **1024 sectors**; ~6,300+ units in a full galaxy |
| Sim target | 30 Hz fixed FixedUpdate; presentation per-frame |
| Multiplayer | host-authoritative, session-code relay; 8/19 fleet orders verified working; clients *must not* simulate faction AI (authority gate) |
| Verification | offline harness `tools/harness/check.sh` (14 per-asmdef .NET projects, 4.6 s warm) — **headless Unity does not work** on this machine; the user runs the editor / device checks |
| Branch discipline | everything lands on `Dev`; `Legacy` frozen; never bare `git push` (origin/HEAD is a trap) |

---

## 4. Where the code actually is (map)

Two asset roots:

- `Assets/OpenFrontierLogic/` — **code**, one folder per asmdef. References are
  *not* transitive (AGENTS.md trap), so cross-module reads are always explicit.
- `Assets/InterstellarPilot2/` + loose roots — content: prefabs, scenes,
  `Resources/prefabs/worlddata/sectors/` (sector content prefabs), plugin DLLs.

Full hot-file table is Appendix A. The load-bearing trio for this report:

```
EngineASX.cs              5,630 LOC   every registry + the tick graph
UnitComponentHolder.cs    2,782 LOC   per-unit state + Tick()
FactionAIBase.cs          3,782 LOC   the faction brain
Faction.cs                2,818 LOC   faction state + intel wiring
NpcPilot.cs               2,922 LOC   per-pilot AI (plain class!)
WorldBase.cs              2,006 LOC   world lifecycle, seeding entrypoint
Engine.SaveGame/*        ~5,900 LOC   current importer/exporter
V2.BinarySerialization ~27,000 LOC   nine historical readers + writer
```

A module pattern **already exists** inside EngineASX (`traderHeatmapModule`,
`asteroidRespawner`, `wormholeUpdater`, `cargoUpdater`, …) and `NpcPilot`,
`NpcTargetScanner`, `FactionIntel` are plain C# classes — the codebase has been
drifting in the right architectural direction already. Extraction is finishing
a migration that is in flight, not reversing a design.

---

## 5. How the simulation runs today (the real architecture)

### 5.1 The heartbeat

`EngineASX.FixedUpdate()` (commentary at `EngineASX.cs:3939`) runs at
fixed timestep 0.0333 → **30 Hz everywhere**. Rationale is written in the file:
Update() made the simulation rate depend on the display (30 fps phone vs 144 Hz
monitor); timescale-0 pause screens stop the sim properly; `fixedDeltaTime`
reads everywhere becauseinic FixedUpdate can run 0..n times per frame with
identical accumulated deltas otherwise. Presentation (ActiveUnitShip movement)
stays per-frame.

### 5.2 The tick graph (memory model)

```
EngineASX.FixedUpdate  (30 Hz)
│
├─ UpdateActiveSectorUnits()      per frame — active sector ONLY
│    └─ walks sector.GetUnitsByType(...) lists; UnitComponentHolder.Tick()
│       gated by ShouldTickNow (hasInit, Engine set, cached IsInActiveSector)
│
├─ UpdateFactions()               per frame
│    └─ Faction.Tick() + FactionAIBase.Tick() per faction
│       (authority gate → UpdateLight every tick; UpdateHeavy every 3 s)
│
├─ module updates                 per frame, each opt-out cheap when idle
│    traderHeatmap, asteroidRespawner, wormholes, unit capture, ditch/despawn,
│    cargo
│
├─ UpdateWatchedSectorUnits()     multiplayer: sectors being watched remotely
│    tick at full rate (per-player reference counting; SetSectorWatched)
│
├─ UpdateInactiveUnits()          round-robin over updatableUnits (cap 2000+)
│    budget N per tick from PreferredTimeBetweenUnitUpdates (3 s) & fsm dt
│    skipped if sector IsActive or SimulatesEveryFrame
│
├─ UpdateInactiveFleets() / UpdateInactiveGroupSpawners()   round-robin, ditto
│
└─ CleanupEmptyFleets() + destroyedUnitsToCleanup deferred destruction
```

The comment at `:4290–4323` is the best single document in the engine: it
explains *why* the active-sector tick reads the sector's by-type lists rather
than scanning the world list (`IsInActiveSector ⇒ unit ∈ sector.unitsByType` by
construction, symmetric add/remove), and how both priorities have already been
paid.

### 5.3 The per-unit tick

`UnitComponentHolder.Tick()` (`:1210`) is a state machine:

- not flying this copy → refresh detection radius (arith derived, no host
  data needed) and return;
- Constructing/Dismantling have their own ticks;
- Constructed → `UpdateComponents(num)` (cloak + recharge via priority-ordered
  list guarded by `anyComponentRequiresRecharge`; auto-fire turrets guarded by
  `hasAutoFireTurrets` — idle ships cost ~nothing) → `TickHeat` →
  `NpcPilot.Tick` → periodic `PerformScan`
  (`UnitScanWhenActiveFrequency` / `...InactiveFrequency`) → detection radius →
  auto-turret + point-defence modules when decloaked → autorepair → factory.

Everything below this line is procedural C# — no Unity dispatch, no
`GetComponent` chains except in scan paths flagged below.

---

## 6. Faction system — findings

### 6.1 Structure

- `Faction` (2,818): id-keyed (`int`), with **reserved bands** for player-made
  factions (800,000,000) and player-made units (900,000,000) and
  `IsPlayerMade(id)` as the test; `ReassignFactionId` exists because
  `factionIdMap` is private. Wealth/networth, hostility matrix via attitude
  maps, leader/Person ties, `FactionIntel` and bounty linkage. `Faction.Tick()`
  per frame at `:1877` (next-AI timer, recent attacks, intel upkeep).
- `FactionAIBase` (3,782, MonoBehaviour, created per AI faction via
  `CreateFactionAIType` on the faction's transform): the brain.
- Strategy layer: `shipStrategyFlags` Dictionary + `FactionStrategy` records
  (fleet stance / aggression baseline).
- `FactionHeatmap` + `FactionHeatmapDissipator`: grid-based territory pressure
  with own budgets (`MaxHeatmapCellsUpdatedPerSecond`=10 cells/s,
  `HeatmapDissipationUpdateFrequency`=10 s pass).

### 6.2 Tick decision (already good)

`FactionAIBase.Tick()` (`:1088`):

1. `Authority.OwnsFaction(faction)` gate — clients return false; *this exact
   gate once caught a full second economy running on every client* (the
   comment at `:1095–1127` details why UpdateHeavy must not run client-side).
2. `UpdateLight()` **every tick** (`:1138`): repair fleets, rearm fleets,
   under-attack response, attack module, expansion-search move, station build
   request processing. Reactive — deliberately not throttled (design, not bug).
3. `UpdateHeavy()` every `FactionAIUpdateInterval` = **3 s** (`:1131`):
   building (ships/stations/idle NPCs), pilots & surplus ships, fleet
   formation, expansion, upgrade stations, bounty/revenge responses.

### 6.3 Costs & weak points (hypotheses until Phase 0)

- UpdateHeavy iterates faction fleets × subpasses (economy validation, build
  decisions, idle NPC recycling). Complexity ≈ O(factions × fleetships ×
  passes) per 3 s interval galaxy-wide. With 20 factions × hundreds of ships
  this is a periodic spike, not a flat load — **spread heavy passes over the
  interval instead of aligning all factions on one gate** (they already have
  independent `NextFactionAIUpdate` seeds — verify they are desynchronised at
  world load).
- `UpdateExpansionSearch` candidates are scored against "a long list" — budget
  `EmpireAIExpansionSearchItemsPerSecond`=4 candidates/s confirms the search is
  expected to be expensive. Good candidate for a cooperative background job
  (pure data after Phase 1).
- Faction networth & valid-trader recache (`CacheAllFactionValidTraders`,
  `RecacheValidTraderTargets` at seed/refresh storms) is O(units × factions)
  at load — which is expected to show up as the "world load spike" in Phase 0
  numbers.
- Faction intel DB is a dictionary per faction of discovered units keyed by
  unit id with per-entry time stamps; pruning runs in throughput loops with
  EngineASX lookups (`GetUnitByid` per entry) — dense-struct candidate (R5).

### 6.4 What a rebuild must preserve

- Authority gating semantics *everywhere* (it encodes multiplayer law).
- Reserved id bands + verbatim id storage (saves from before the band move were
  deliberately allowed to half-load; that decision is recorded and static).
- The light/heavy cadence and the "reactive is not throttled" rule.
- Trader-target cache TTL (120 s) and heatmap budgets as tuning surfaces.

---

## 7. AI system — findings

### 7.1 Who the AI actually is

**`NpcPilot` (2,922 LOC) is *not* a MonoBehaviour.** It is a plain class driven
from `UnitComponentHolder.Tick()` — the codebase's own proof that the
sim/presentation split is viable and already partially in place.

Per-pilot flow (`NpcPilot.Tick` → `UpdateWhenInActiveSector` / `tickInactive`
→ `UpdateCommon` at `:1202`, in order):

1. regroup order when separated from fleet (`CreateRegroupOrderWhenSeparated`);
2. stand-down-from-combat checks;
3. fleetless → `NpcTargetScanner.Update` (claimed candidate flow, below);
4. combat targetting (guarded by `NpcTargettingEnabled`);
5. mining — *two implementations*: active lasers (per-turret fire) or
   abstracted off-screen mining with **fleet-wide cargo balancing** across the
   whole fleet's bays (`:1281–1368`) — a tell-tale that inactive-sector AI was
   designed to math instead of simulate;
6. cargo collector checks (1–6 s randomised interval);
7. turret autoload when a combat target exists;
8. combat update + cloaking update.

### 7.2 Target scanning (the exemplary subsystem)

`NpcTargetScanner` (205 LOC):

- candidates come **from faction intel**, not from the scene —
  `FactionIntel.GetDiscoveredUnitIdsInSectorNonAlloc` into a reusable list;
- processing is a **credit budget**: `NpcTargetScanProcessedUnitsPerSecond`
  (20/s) ÷ by frame delta → per-tick allotment; a busy sector resolves its
  targets over more frames instead of spiking;
- hostile target-list pruning every 10 s (`timeBetweenTargetListUpdates`);
- inactive vs active sector cadences 6 s / 2 s;
- scanning "in progress" is resumable state (`scanProgressIndex`), not a
  cancelled-then-restarted pass every frame;
- same pattern for turret target scoring: `NpcPilotTargetScoresPerSecond`=20
  per pilot, `AutoTurretTargetScoresPerSecond`=20 **shared across all turrets
  in the sector**.

In short: the engine's AI *cannot* spike; the cost is bounded, and under load
it degrades to slower decisions. That is the correct mobile-first trade —
preserve it.

### 7.3 Weak points (hypotheses until Phase 0)

- The candidate *source* (`FactionIntel` dictionaries + EngineASX id lookups)
  and the per-target test chain (`IsTargettable` → `IsHostileTo` → attitudes)
  allocate and chase pointers; the budget architecture hides rather than
  removes the cost. Dense data (arrays of unit records per sector, bitsets for
  hostility) makes the same budgets dramatically cheaper.
- Turret auto-fire path (`TurretComponent.UpdateAutoFire`) runs per-turret
  every ship tick with weapon lead/fire logic; at battle scale the *scoring*
  is budgeted but the per-shot math and projectile spawn are not. Verify with
  a 500-ship battle capture (Phase 0 timeline).
- `MissileSeeker` (1,271 LOC) / `Missile` (907): projectile acquisition logic —
  not yet measured; likely the largest burst-mode consumer in a missile-heavy
  battle burst. See Phase 6 candidates.
- Comms/dialog AI (`Engine.Comms.FactionDynamicComms*`) is dialog-driven and
  event-time-gated; likely cheap. Deprioritised.

---

## 8. System generation — findings

Generation is **three tiers**, and *all of them are synchronous on the main
thread today*.

### 8.1 Tier 1 — Blueprint generation (pure data, already isolated)

- `WorldBlueprintGenerator` (23 LOC) → `WorldBlueprintSectorGenerator` (296) →
  `WorldBlueprintSectorTypeGenerator` (123) + sector-asteroid type generator
  (91) + `FindNearestSectorBlueprint` (60).
- Seeded by `System.Random` (world-level `SeederRandom`), data-only — Vector3
  geometry, graph growth with **snakiness weighting**, node snapping, and
  connection-crossing checks. Historically ceiling 64 sectors, tested at 1024.
- Already has *one* O(n³)→hoisted fix documented in-source
  (`GetNextNode`, `:39–59`); remaining allocations are LINQ (`Select/ToList` in
  the growth loop, `Where/GetRandom` in snapping) and per-attempt recompute.
  Fine at 64; a GC tax at 1024.
- Sector naming goes through `GameController.Instance.SectorNamer` — the one
  Unity-coupled call in the otherwise pure-data path. Trivially relocatable.

### 8.2 Tier 2 — Seeding layers (the ~120-class pipeline)

`WorldSeeder` (91 LOC) is the orchestrator on a MonoBehaviour:

```
SeedWorld(world)
  SeedSpecialLayers()          ← GetComponentsInChildren<WorldSeederLayer>()
                                 — declarative layer tree, ~120 classes
  CacheAllFactionValidTraders()
  SetupFactions()              ← CreatePilots(true) + CreateFleets() per
                                 faction + networth bookkeeping
  PopulateFactionValidTraderTargets()
  SeperateOverlappingUnitsInAllSectors()
```

Layer inventory (each class + its Settings pair): asteroids
(sprinkler/depleter/yield/clusters), stations, ships, equipment, fleets
(created + randomized + ordered), planets, gas clouds, empire factions,
faction spawning/relations/leaders, intel seeders (static/full/on-faction/
all-factions-discover-everything), bandits (+supercharged variant), trader
cargo (world + per-ship), bars, passengers, pilots ranking, wormholes
(+unstable), security levels, blueprint sectors, modded units, custom scenario,
skirmish universe generator, scenario options, sector content refresh,
rename/validate/out-of-bounds/reachability layers, tools utilities.

Design verdict: **the layer pipeline is a genuinely good architecture** —
composable, settings-driven, scenario-tweakable. Its problems are *hosting*:
layers are MonoBehaviours parented under a scene object (so layer composition
is authored in scenes/prefabs, not code), each `Seed()` runs immediately and
serially, and nothing records per-layer cost.

### 8.3 Tier 3 — The new-game pipeline (the blocker)

`WorldBase.OnNewGameStarting` (`:1521`) runs the whole world birth
synchronously on the main thread:

```
Random.InitState(customSeed) + SeederRandom = new System.Random(seed)
ship/faction name randomisation
InitNewGameObjects()
ApplyFactionSetupOnNewGame()          (economy population entry)
SeedWorldOnNewGame()                  ← Seeder.SeedWorld (tier 2)
SetInitialScene()                     ← first scene materialise
PowerupFactionStations()
RandomizeEmptyPilotNames()
CallFactionAIOnNewGame()
autosave                              ← first save written HERE
```

At 1024 sectors this is expected to be a multi-second, frame-frozen loading
block — *hypothesis, to be measured in Phase 0*, but nothing in the pipeline
produces a frame between `InitNewGameObjects` and the autosave.

### 8.4 The story-scenario trap

`AGENTS.md` documents it and it is the largest single constraint on "just
regenerate everything": **story universes are baked into their scene files**.
`unchartered_space.unity` and `a_wormhole_too_far.unity` contain fully-expanded
runtime-named copies of every sector's content. Editing the seeder layers or
sector prefabs does **not** change the story scenarios; the prefabs only serve
battle scenarios and `EngineASX.LoadSectorPrefabs` by name. Any generation
overhaul therefore needs either (a) a re-bake tool that regenerates those
scene files, or (b) an explicit decision that story worlds remain frozen assets.

### 8.5 Randomness inventory

- `UnityEngine.Random.InitState(customSeed)` once at new game (gameplay RNG,
  shared) — order-dependent across platforms/threads;
- separate `SeederRandom` (`System.Random`) for seeding (tread-safe, reproducible);
- `ScenarioLoadData.SeedSettings` may replace the default seeder settings
  (custom scenario path). SkirmishWorld adds +8000 to intel ScanRange.
- Rework must define which systems must be reproducible from seed (R7).

---

## 9. Peripherals that shape the plan

### 9.1 Save system — the 27k-LOC version zoo

`SavedGames.V2.BinarySerialization.Readers` contains **nine historical
importers** (`SaveGameReader2000…2019`) of ~2,600–2,730 LOC each, plus the
live writer (1,724) and current importer (`SaveGameModelImporter170`, 3,434) —
in total ≈27k LOC whose entire job is backward compatibility. Every engine
model change ripples into model + exporter + importer (+ next year's new
reader). This is the classic "maintenance tax grows linearly with features."
The fix is an upgrade-chain architecture: current reader only; old saves flow
through an ordered upgrade pipeline. Requires the save-compat decision (§15).

### 9.2 Multiplayer — the load-bearing law

`Docs/multiplayer/status.md`: exactly one simulation (the host); the client
owns only its flown ship (10 Hz stream); everything is ask-host-decide-relay;
8/19 fleet orders live; watched-sector tick-rate semantics (a player watching a
distant sector gets full-rate simulation for it — paid on the host frame).
The `Authority` provider is the encoding of all of this and must be designed
*around*, never refactored away. Any new parallel phase must keep authority
checks in the tick, host-only writes, and worker threads producing **data, not
decisions**.

### 9.3 Assets / rendering context

- 1,293 worlddata prefabs; 21 scenes; story scenes baked (see 8.4).
- Sector colliders in the active sector are already distance-culled
  (`DisableCollidersInActiveSector`, 2000–2200 m fade) — the engine knows
  physics overdraw is a cost.
- 223 `Update()` calls remain in the codebase overall; core engine has just
  one (`ActiveUnit.Update`). The rest is UI/audio/FX — a later sweep, not the
  hot path. Diagnostic-only work must compile out (`DEVELOPMENT_BUILD ||
  UNITY_EDITOR`).

---

## 10. Cross-cutting risk register

Ranked by (expected impact × confidence). All are hypotheses until Phase 0
measures them — that is what Phase 0 is for.

| # | Finding | Why it matters | First action |
|---|---|---|---|
| **R1** | Intel scans `Physics.OverlapSphereNonAlloc` in **world space** — the sphere touches every sector's colliders | likely the single biggest galaxy-scale CPU sink in warm play; grows with total unit count regardless of which sector player is in | replace broad phase with per-sector candidate lists (`sector.unitsByType`) + per-faction sector caches; keep `FactionIntelProcessor` budget; golden-test detection parity (cloak, gas clouds, wormhole cooldown) |
| **R2** | `EngineASX` god-object: ~40 dictionaries; every system reaches `Instance.*` | blocks modularity, parallelism, testability; every change risks other systems | Phase 1 registry extraction; module pattern already in-tree to follow |
| **R3** | Generation pipeline synchronous, MonoBehaviour-hosted layers | multi-second frozen load screen at scale; blocks "raise sector count" | Phase 2: blueprint gen → background thread; then data-driven layer list |
| **R4** | Faction `UpdateHeavy` full-world passes per faction every 3 s regular | periodic frame spikes scale with faction × fleet count | Phase 3: per-pass budgeting + desynchronised gates + cooperative jobs |
| **R5** | Pointer-chasing data structures under budgeted AI (intel DBs, dictionaries, LINQ) | budgets hide cost instead of removing it; GC pressure shows as "random" drops | Phase 3/4: dense structs per sector, bitset hostility, array-backed intel |
| **R6** | Save system: 27k LOC legacy zoo + duplicated import/export paths | maintenance tax per feature; potential load-time cost | Phase 5 (after compat decision); also enables faster autosave |
| **R7** | Mixed RNG (`UnityEngine.Random` shared everywhere + `SeederRandom`) | reproducibility unprovable; multiplayer/parallelism hazards | define reproducibility contract per system in Phase 1 docs |
| **R8** | No general pooling; FX allocations at battle scale (`Instantiate` × 70 sites) | GC hitches "sometimes" = random-feeling frame drops on phones | Phase 7 (only if profile says battle FX matter) |
| **R9** | 223 `Update()` MonoBehaviours across UI/audio/FX + hand-edited prefab risks | per-frame UI dispatch tax; silent reference-loss bugs (documented) | Phase 7 sweep + existing harness checks (`prefab-null-check.py`) |

---

## 11. What "rebuild from the ground up" should mean here

Three honest options, and why the obvious one is the right one:

1. **Full rewrite / DOTS-ECS conversion** — discards 541 commits of verified
   fixes, multiplayer protocol + authority law, working prefab pipelines,
   and the community content that the AGPL project accumulated. On a mobile
   first game with ~6k live units, full ECS is not the bottleneck anyway.
   **Rejected.**
2. **New engine alongside** — doubles content pipeline cost forever, diverges
   immediately. **Rejected.**
3. **Strangler rebuild (recommended)** — keep Unity, prefabs, scenes, save
   format, multiplayer protocol; **replace the simulation core's
   implementation behind stable seams**. The sim seam already exists: ids are
   stable ints; sectors bucket their own units; `NpcPilot`,
   `NpcTargetScanner`, `FactionIntel`, blueprint generators are already plain
   C#; the module pattern is in-tree. Phase 1 *completes a migration that is
   already half-done*, then Phase 2+ can move genuine compute off the main
   thread on real data.

What "from the ground up" then means concretely: **a rebuilt engine core whose
rules are (a) simulation in plain C#, (b) Unity only for presentation, (c) main
thread owns timing, workers own epics, (d) everything measurable.** The game
itself keeps its face.

---

## 12. Phased roadmap

Order is deliberate: nothing parallel-adjacent happens before the seams that
make it safe exist. Each phase lists acceptance criteria so progress is
judged, not felt.

### Phase 0 — Instrument & baseline (no behaviour change)

- Extend the existing `ProfilerRecorder` markers (`s_markerActiveUnits`,
  `s_markerFactions`) with: per-seeder-layer timing, save load/export timers,
  faction heavy pass timer, scan/turret timers.
- Scripted timelines: 64 / 256 / 1024-sector worlds; 30 / 300 / 1000-ship
  battles. Capture on PC editor AND the reference weakest phone.
- Deliverable: **budget document** — frame ms, GC ms/30 s, load time, memory
  — with every R# above either confirmed, refuted, or quantified as "not hot".
- Acceptance: every subsequent phase has before/after numbers against it.

### Phase 1 — Decompose EngineASX into modules with owned state

- Extract registries: `UnitRegistry`, `FactionRegistry`, `SectorMap`,
  `FleetRegistry`, `MissionRegistry`, `PersonRegistry` (ids already stable).
- Lookups go behind explicit module interfaces; `Instance.*` reach shrinks per
  module as callers migrate.
- Engine-internal synchronous events → per-tick notification queues (kills
  reentrancy hazards between systems mid-tick). Unity-side events remain for
  presentation wiring.
- Randomness contract defined per system (R7).
- Acceptance: harness clean (including `codec-test`); world golden-loads
  byte-comparable; play session unchanged; `LogWrapper` unchanged.

### Phase 2 — System generation off the main thread

- (a) Blueprint generation → background worker (pure data; move sector name
  generation off `GameController.Instance.SectorNamer` to data first).
- (b) Layers: convert `WorldSeederLayer` MonoBehaviour tree → data-driven,
  ordered, chunked `IWorldSeedLayer` list + settings structs; per-layer cost
  counters (dev build only).
- (c) Materialisation (prefab `Instantiate`) stays main-thread, **batched per
  frame during load with progress UI**; validation layers run post-materialise.
- (d) Loading screen reflect progress; autosave still written only after
  everything is deterministic.
- **Acceptance: same seed → bit-identical world state** (golden-seed test in
  harness), load time on 1024 sectors ≈ O(screen progress, not frozen frames).
- Note: story scenarios are untouched by this phase (baked scenes, §8.4).

### Phase 3 — Faction AI restructure

- Keep UpdateLight/UpdateHeavy cadence and the authority gate byte-for-byte.
- UpdateHeavy passes → internal planner with per-pass budgets; faction gates
  desynchronised across the 3 s interval (verify seeds at load);
- Expansion search + trader recache → cooperative background jobs producing
  data plans the tick consumes;
- Hot data → dense structures (arrays per faction, bitset hostility/attitude
  matrix, array-backed intel queue).
- Acceptance: same decisions for same inputs (bounded-span golden tests), frame
  spike at heavy pass ≤ budget from Phase 0 doc, GC/30 s reduced on busy worlds.

### Phase 4 — Intel & scanning de-physicsation (R1)

- Broad phase: per-sector candidate lists + per-faction sector caches instead
  of world-space overlap queries; physics queries only for the specific trigger
  semantics that cannot be derived data-locally (to be enumerated in-bound).
- Keep `FactionIntelProcessor` budget mechanics; keep radar-cross-section
  radius semantics exactly.
- Acceptance: detection parity suite (cloak / gas cloud / wormhole cooldown /
  static targets) green; OverlapSphere calls in warm play ≈ 0 outside active
  sector.

### Phase 5 — Save system consolidation (R6)

- Single current importer/exporter + ordered legacy-save upgrade chain;
  legacy readers compile-out (feature-flag per decision in §15).
- Upgrade chain is data-described (per-feature records), so the *next* model
  change stops costing reader-rewrites.
- Acceptance: golden saves from every supported version load; load times
  improved vs Phase 0 baseline; harness codec tests extended to the chain.

### Phase 6 — Targeted jobification (only where measured, only where N is large)

Candidate list (each must prove itself in the Phase 0 numbers first):

| Candidate | Work type | Parallel shape |
|---|---|---|
| Heatmap dissipation cells | grid math, float ops | embarrassingly parallel |
| Universe path recomputation (Dijkstra batches after sector changes) | graph | batch-parallel per query |
| Save export/import (compression + serialise) | throughput | staged pipelines |
| Missile/projectile seek (`MissileSeeker`) if battle spikes confirm | per-projectile math | Burst-friendly |
| Seeding layer epics that stay costly after Phase 2 | data generation | stage-parallel |

Rule of thumb: **Burst + NativeArrays with main-thread apply-back**, mobile
ARM64-first, only for work with hundreds+ elements, never on the 30 Hz tick
path of the active sector. If a job can't amortise on a phone, it doesn't ship.

### Phase 7 — Broad sweeps (tie-breakers, only if profile says so)

- FX/pooling battle sweep (R8), lingering `Update()` dispatch sweep (R9),
  UI hot-path tuning (HUD screen is 1,525 LOC), compile-out verification for
  all diagnostic-only code (AGENTS.md rule).

Sequencing note: multiplayer feature parity work (remaining fleet orders)
interleaves safely with Phases 0–2; Phases 3–4 must be merged with current
authority rules reviewed together with any order changes; Phase 5 is
independent and can run any time after the compat decision.

---

## 13. Multithreading strategy and the threading contract

The deciding fact: this is a **30 Hz mobile sim**. A phone gives cores but
starves memory bandwidth, and per-frame jobs that cannot amortise are a loss.
Therefore:

- **Per-frame** (active/ watched-sector simulation, player movement, turret
  fires): stays main-thread. Budget architectures already bound it.
- **Episodic/stranded CPU** (world generation, load, AI heavy searches,
  expansion search, trader recache, save export, path recompute): workers +
  line-of-sight queues. These are the multi-second or spike-shaped costs.
- **Burst-flavoured array math** (heatmaps, Dijkstra batches, projectile swarms
  if confirmed): Jobs + NativeArrays, apply-back on the main thread.

**Threading contract (to be codified in-repo during Phase 1):**

1. The main thread owns the simulation and its 30 Hz clock. Authority checks
   (`Authority.*`) never move off-thread.
2. Workers produce **data, never decisions** beyond inputs the host already
   owns; consuming code stays in the tick.
3. No Unity API off the main thread — not even `Debug.Log`-adjacent
   Unity-only calls; workers speak plain C#.
4. Shared state crosses threads by snapshot (immutable structs) or queue
   (command buffers). Direct field access across threads is a review veto.
5. Determinism: anything that must reproduce from seed uses an injected
   `System.Random`; `UnityEngine.Random` is gameplay-only (R7).
6. Every worker work item prints/records its cost histogram in dev builds.

This contract keeps the multiplayer authority model intact by construction —
the host's tick remains the single writer of world state; workers feed it.

---

## 14. Measurement plan (Phase 0 detail)

Sim timelines to script (player-free, deterministic via standoff inputs):

1. Galaxy load: new-game to first autosave at 64/256/1024 sectors.
2. Warm play: 60 s orbit in an active sector each scale, camera fixed and
   camera panning (AGENTS.md: CPU-vs-render competition shows as
   "moving camera halves FPS").
3. Battle: staged 30/300/1000-ship fight with autoturrets and missiles.
4. Economy churn: force faction expansion + trader recache storms.
5. Save/load: 1024-sector world export/import round trip.

Captures: Unity Profiler on editor, on-device profiler tooling if available.
The dev-only markers from AGENTS.md style (compile-out diagnostics) go in
during Phase 0; the deliverable budget doc lives in `Docs/research/`.

Success bar for the rebuild overall (draft, to be ratified with Phase 0
numbers): 1024-sector world load < 5 s wall with visible progress; steady-state
GC < 1 MB/30 s in active play; heavy-pass spike ≤ 3 ms on reference phone;
all diagnostic overlays dev-only.

---

## 15. Open questions that change the plan

| # | Question | Why it changes the work |
|---|---|---|
| 1 | Universe scale goal: 1024 sectors is the tested ceiling — raise higher? | R1/R4/R5 priorities scale directly with unit count |
| 2 | Save compatibility: freeze legacy readers? v2 the last floor? | gates Phase 5 entirely |
| 3 | Story scenarios: re-bake tool acceptable, or story worlds frozen? | gates how far Phase 2 can automate |
| 4 | Reference weakest device(s) for the budget doc | without a device list, Phase 0 has no denominator |
| 5 | Interim multiplayer feature work: must remaining 11 orders land first? | affects merge order of Phases 3–4 with order work |
| 6 | Appetite for a pure-C# sim assembly (new asmdef `OpenFrontier.Sim`)? | Phase 1's biggest single convenience win |

---

## 16. Appendix A — hot file (LOC) table

| File | LOC | Role |
|---|---|---|
| `OpenFrontier.IP.Engine/EngineASX.cs` | 5,630 | engine core: registries, events, tick graph |
| `OpenFrontier.IP.Engine.Factions/FactionAIBase.cs` | 3,782 | faction AI brain |
| `OpenFrontier.IP.Engine.SaveGame/SaveGameModelImporter170.cs` | 3,434 | current save importer |
| `OpenFrontier.IP.Engine/Fleet.cs` | 3,225 | fleet logic + orders plumbing |
| `OpenFrontier.IP.Engine/NpcPilot.cs` | 2,922 | per-pilot AI (plain class) |
| `OpenFrontier.IP.Engine.Factions/Faction.cs` | 2,818 | faction state/wealth/intel |
| `OpenFrontier.IP.Engine/UnitComponentHolder.cs` | 2,782 | per-unit state + `Tick()` |
| `OpenFrontier.IP.SavedGames.V2.BinarySerialization/…/SaveGameReader.cs` (×9 historical) | ~2,600–2,730 each | legacy save importers |
| `OpenFrontier.IP.Engine/Unit.cs` | 2,350 | unit state |
| `OpenFrontier.IP.Engine/WorldBase.cs` | 2,006 | world lifecycle + seeding entry |
| `OpenFrontier.IP.SavedGames.V2.Model/…/ComponentBayMap.cs` | 3,293 | save model |
| `OpenFrontier.IP/UI.Screens.Hud/HudScreen.cs` | 1,525 | HUD |
| `OpenFrontier.IP.UI.Screens/OrdersHelper.cs` | 1,444 | order relay helpers |
| `OpenFrontier.IP.Engine.EngineMusic/MusicDirector.cs` | 1,367 | music (diagnostics lesson lives here) |
| `OpenFrontier.IP.Engine/MissileSeeker.cs` | 1,271 | projectile targeting |
| `OpenFrontier.IP.Multiplayer/Session/FleetOrderCommands.cs` | 1,042 | multiplayer order validation |
| `OpenFrontier.IP.Engine/GameController.cs` | 1,148 | settings root |
| `OpenFrontier.IP.Engine.Factions/FactionIntel.cs` | 998 | discovery DB + path cache |
| `OpenFrontier.IP.Engine/Sector.cs` | 969 | sector buckets |
| `OpenFrontier.IP.Engine/Missile.cs` | 907 | projectile |
| `OpenFrontier.IP.Multiplayer/Session/HostSession.cs` | 875 | host |
| `OpenFrontier.IP.Engine/Person.cs` | 867 | pilot/person |

---

## 17. Appendix B — PerformanceSettings budget inventory

The engine's master cost ledger (`OpenFrontier.IP/Engine/PerformanceSettings.cs`),
grouped. These are the numbers a rebuild must preserve as *tuning surfaces*:

**Per-server-wide sim cadence**
- `PreferredTimeBetweenUnitUpdates` = 3 s, `PreferredTimeBetweenGroupUpdates` = 3 s
  — "the single biggest lever on how heavy the simulation is".
- `FactionAIUpdateInterval` = 3 s — the cost of the whole background sim.

**NPC/perception budgets**
- `NpcTargetScanActiveFrequency` 2 s / `NpcTargetScanInactiveFrequency` 6 s;
- `NpcTargetScanProcessedUnitsPerSecond` = 20 (total cap);
- `NpcPilotTargetScoresPerSecond` = 20; `AutoTurretTargetScoresPerSecond` = 20
  (sector-shared across turrets);
- `AIActivePilotMineCalcInterval` = 0.2 s.

**Faction intel/economy**
- `FactionIntelMaxScansProcessedPerSecond` = 120; `FactionIntelScanInterval` = 2 s;
- `FactionTraderTargetCacheMaxAge` = 120 s;
- `AITradeSearchSearchesPerSecond` = 100 (player-involved: 300);
- `AICargoSearchSearchesPerSecond` = 60 (player-involved: 120);
- `AIBountyHunterSearchSearchesPerSecond` = 100; `AIMineSearchSearchesPerSecond` = 30;
- `EmpireAIExpansionSearchItemsPerSecond` = 4;
- `MaxHeatmapCellsUpdatedPerSecond` = 10; `HeatmapDissipationUpdateFrequency` = 10 s.

**Distance culls** (active-sector colliders 2000–2200 m fade; shield FX 200 m;
ejected cargo physics 1500 m; countermeasures/units/trails/lighting/roll
distances; `CreateImpactParticles`/`ShowDamageEffects`/`ShowShieldHitEffects`
toggles — "the first thing to switch off on low-end hardware").

**Other** — cargo-collector intervals 1–6 s randomised; passenger morale 0.1 s;
`EjectedCargoPhysicsMaxRangeFromCamera` 1500 m; thruster draw distance group.

---

## 18. Appendix C — resume recipes for future sessions

```bash
REPO=/home/deck/Documents/InterstellarPilot-OpenFrontier

# simulation tick graph + budget system
sed -n '3925,4050p;4198,4250p;4325,4470p' "$REPO/Assets/OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.Engine/EngineASX.cs"

# cadence + culls: the ledger
cat "$REPO/Assets/OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.Engine/PerformanceSettings.cs"

# faction AI: light/heavy split + authority gate
sed -n '1088,1180p' "$REPO/Assets/OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.Engine.Factions/FactionAIBase.cs"

# per-unit tick + component internals
sed -n '1210,1360p;1447,1560p' "$REPO/Assets/OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.Engine/UnitComponentHolder.cs"

# NPC AI cascade + target scanner
sed -n '1159,1370p' "$REPO/Assets/OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.Engine/NpcPilot.cs"
cat "$REPO/Assets/OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.Engine.Npcs/NpcTargetScanner.cs"

# intel scans (the R1 physics call) + path cache
sed -n '830,960p' "$REPO/Assets/OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.Engine.Factions/FactionIntel.cs"

# generation: blueprint + layers + new game pipeline
cat "$REPO/Assets/OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.Engine.WorldSeeding/WorldSeeder.cs"
cat "$REPO/Assets/OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.Engine.WorldGeneration/WorldBlueprintSectorGenerator.cs"
sed -n '1500,1665p' "$REPO/Assets/OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.Engine/WorldBase.cs"

# census helpers
grep -rln "void Update()" "$REPO/Assets/OpenFrontierLogic" --include=*.cs | wc -l
ls "$REPO/Assets/OpenFrontierLogic/OpenFrontier.IP/" | grep WorldSeeding
```

Still-unexplored areas for the next research pass: `NpcPathfinding/`,
`Pathfinding/`, `Comms*` (dynamic comms AI), `AutoTurrets/` internals,
`FactionIntelProcessor.cs` queue mechanics, `Fleets.ActiveOrders/*`,
`Heatmaps/FactionHeatmap.cs` math, `Hypersleep/`, `Scratchcard/`,
ActiveUnit movement ↔ Rigidbody integration, `OpenFrontier.IP.SpaceUnity`
skybox pipeline.

Research notes (session recovery): `~/.opencode/plan/engine-rebuild-research.md`.
