# OpenFrontier foundation rebuild — plan & decisions (planning doc)

Written 2026-11-08. Status: PLANNING, not yet green-lit for implementation.
Untracked by design, like `engine-rebuild-report.md`. The measurement findings in that
report all still stand (tick graph, scan costs, save zoo budgets); **but the strategy has
changed** per the user: this plan supersedes the strangler sequencing in the report.

**Blank-slate move executed later the same day (2026-11-08):** all legacy code, scenes,
prefabs, materials, shaders, authored data and vendored DLLs now live in `/PreRebuild/`
(project root, tracked, outside Assets — never imported/compiled). `Assets/` holds raw
content only (art/audio/fonts/geometry at original paths). Harness runs "loud-idle"
(0 asmdefs → green with an explicit note, not a lying pass). Full law: `AGENTS.md`
section "The rebuild blank slate"; restore procedure in `Assets/README.md`. The M0
slice now drops into an empty `Assets/` with zero legacy interference.

## 1. The pivot the user chose

- Old quote: "we are going to pull out as much as possible, start again so to speak."
  "id rather shatter the foundation early, so we don't have to redo this step later."
- Goal: the dated codebase re-architected from its core so a **couple-thousand-sector**
  galaxy runs well on a mid-range phone (mobile-first rule unchanged), and *everything*
  above the core (flying, weapons, loadouts) becomes reworkable afterwards.
- This is a clean-slate rebuild of the simulation core, not a refactor of `EngineASX`.
  The old engine stays in-tree as the shipping game during the work and as a reference
  for behaviours, but nothing new ports onto it. Port happens *onto the new core*.

## 2. Locked decisions (2026-11-08 session)

1. **Scale goal:** a couple thousand sectors (2,000 as the working number) instead of
   the current 1024 ceiling.
2. **Flight:** full 3D, EVERSPACE 2-style, with reverse thrust. Reference implementation:
   <https://github.com/JoppieToppie/Galaxy-on-Fire-2-Unity-Remake> (see §7).
3. **Weapons / loadouts:** deferred. Foundation first; their data format gets designed
   when the unit model stabilises.
4. **Approach:** strip-first. The new core contains none of the dated patterns (§6);
   consumers port onto it later, one at a time.

## 3. What the foundation actually is (deepest core, three layers)

Everything in the old game keys through these; everything else is a consumer:

1. **Identity + addressing.** Stable int ids for unit / sector / faction / fleet /
   person / mission + registry lookups (old: `unitIdMap`, `factionIdMap`,
   `sector.unitsByType`). Save format and multiplayer wire both encode ids verbatim;
   the player's id bands (800M/900M) are load-bearing. If this layer is wrong, every
   later system re-forks against it. This gets built first and exactly right.
2. **Unit state model + flight.** What a ship/station *is* (position, orientation,
   velocity, hull state, mount points later) and how it moves (§5). In the new core
   this is a plain C# record, not a GameObject with component MonoBehaviours.
   Inactive units are pure data; only watched/active units get presentation.
3. **Tick kernel + sector graph + spatial addressing.** Fixed 30 Hz deterministic
   heartbeat with per-system cadence budgets (the PerformanceSettings philosophy
   survives as the new core's tuning surface), and the sector graph as the spatial
   partition. Critically: **one physics world per active sector**, not all sectors
   sharing one world at spatial offsets (the current design forces galaxy-wide
   OverlapSphere scans — the report's R1). Neighbouring-sector queries use the graph
   + per-sector data, not a shared physics broadphase.

## 4. New core architecture (patterns proven by the GOF2 remake)

Their runtime is the closest working analogue to what we want for OpenFrontier:
pure-C# game logic, Unity as an adapter, mobile-tested, multiplayer, moddable.

- **Layered assemblies that only reference downwards.** Their stack:
  `Core <- Game <- Space <- Net <- App`; the folder decides the assembly, namespaces
  don't have to mirror it. A layer that needs something from above declares a **hook
  class** (falling back to single-player/neutral behaviour while unset) that the
  upper layer's installer fills at startup — with tests that fail if a hook is left
  unfilled. This is a cleaner answer to the god-object problem than interfaces
  everywhere, and it keeps heavy packages (e.g. multiplayer transport) out of the
  layers that don't need them.
- **Plain C# game rules with Unity adapters.** Their `FlightModel` is plain C# (in
  Game layer), unit-tested headless (`~3000` edit-mode tests); `ShipController`
  (MonoBehaviour) only wires input/presentation. Our equivalent split:
  simulation assemblies with zero `UnityEngine` references; a thin presentation
  assembly maps sim state to prefabs/FX/vignettes/camera.
- **Deterministic time.** They run game time in integer milliseconds. Our new core:
  fixed 33.33 ms ticks (30 Hz), integer ms timestamps inside the sim, every
  randomness from per-system seeded streams (required for replay + netcode).
- **Naming proposal (open):** `Assets/OpenFrontier.Sim/` with sub-folders
  `Core/`, `World/`, `Unit/`, `Flight/`, and `OpenFrontier.Present/` for adapters.
  asmdef graph mirrors the layer table in §3. Final names TBD when code starts.

## 5. Flight model v1 (EVERSPACE 2-style + reverse)

Port of the GOF2 remake's `FlightModel.StepFree` (their free-flight option), which is
itself a measurement-backed port of EVERSPACE 2's `UShipMovementComponent::ApplyControlInputToVelocity`.
Their code is Apache 2.0, so porting formulas + constants with attribution is legal;
we copy *numbers and math*, not their assets (proprietary originals).

### State per ship (sim-owned, plain C#)

| Field | Notes |
| --- | --- |
| Position | world space, sector-local addressing on top |
| Orientation | quaternion; models face +Z |
| Velocity | world space; the fundamental quantity (not per-axis speeds) |
| Turn-rate state | per axis current rate toward target (ramped) |
| Boost pool | 0..1, drains while boosting, recharges after |

### Input axes (player or NPC pilot — same struct for both)

thrust fwd/back, strafe L/R, up/down, roll, boost (hold), dampeners toggle.
No throttle in this style; held keys are the throttle.

### Velocity step (per tick, deterministic)

- Top speeds: forward = 1.0 (old top speed, e.g. 100 m/s), **reverse = 0.5×**, strafe
  and up/down = 0.5× — they follow the forward one (boost raises them proportionally).
  Reverse is built into the style at half rate (the user's "added reverse" ✓).
- Input vector clamped to length 1, so thrust+strafe share one top speed in their
  combined direction.
- Acceleration per direction: `A * (1 - p^3)` where p = current speed / top speed of
  that direction (cubic ease, `EaseAccelerationExponent = 3`). Constants measured
  from EVERSPACE 2: forward A ≈ 132 m/s², lateral A ≈ 90 m/s² (over their half top
  speed). Scaled by handling (√-scaled at H=100 → 1.0) — a tunable per ship later.
- Speed cap: capped to what was asked; if already faster (post-boost), not capped below.
- **Inertial dampeners** (on by default, toggle key):
  - velocity steered toward the asked direction; turning boost strength = forward
    accel / 6000 (≈ 2.3 s to follow the nose when cos-weighted `(1+cos)/2`);
  - overspeed brakes *along velocity* at ¼ of forward accel (≈ 34.6 m/s²);
  - braking is reduced when thrusting the way the ship already moves
    (`InputInheritInertia`: × up to 0.9 less, ∝ (1 − 0.9·cos²)) and stronger the
    further above top speed (∝ (speed/top)^1.67) — a boost burns down in ~1 s;
  - dampeners OFF = drift: no braking, no ease; thrust sums onto momentum up to top
    speed and only steers above it. (Drift mode falls out of this for free.)
- **Boost:** top speed ×3.2, forward accel ×1.63 (≈ 215 m/s²), drains ship's boost
  pool over its duration (default ≈ 3.5 s without equipment) and recharges;
  only restarts past 20 % on a fresh press.
- **Engines off** when no thrust held and not boosting (presentation flag:
  exhaust FX, nozzle glow, audio loop stop; coasting burns nothing).

### Turning

- All axes including roll: base 125 deg/s at handling 100, scaled `pow(H/20, 0.75)`
  with a soft cap beyond (only a tenth of further handling counts — avoids
  "nimble ship whips around" feel).
- Reached by a linear ~400 ms ramp per axis; input sensitivity option ×0.6..1.6;
  ×0.85 while boosting; turn rate independent of speed.
- No auto-level in this style (level-out stays an explicit binding).

### Mouse steering (optional input mode)

Virtual stick: mouse moves a reticle inside a circle of ~0.5× screen height; turn
rate grows linearly with offset (0.229 deg/s per px measured), hard dead zone ~8 %
of screen height. Reticle stays where left. Touch/tilt can map onto the same
reticle concept for mobile — to evaluate in the Unity slice (M2), since our sim must
also drive well with touch + controller.

### Adaptation to our sim

- Integrate at fixed 33.33 ms sim ticks, plain math only — **no UnityEngine physics
  in kinematics**. The old model mixed Rigidbody (`MoveRotation`/position snaps) at
  up to 30 Hz with per-component logic; the new one owns integration and hands a
  transform to presentation each frame (presentation interpolates between ticks).
- One model for player and NPC pilots: NPCs produce the same input struct
  (an AI layer decides thrust/turn/boost). This replaces the old separate
  player-vs-NPC movement paths and makes flight reworkable for everything at once.
- Collision + feel: active sector may use Unity physics for visuals/contact only;
  sim speed/position never reads it back (top-level decision: sim is authoritative).

### Acceptance checks for the port (from their measured values)

Forward 100 m/s reached ≈ 90 % at 0.77 s, 99 % at 1.12 s; full stop from top
≈ 2.96–3.0 s; strafe stop ≈ 1.48 s; heading turns 125 deg/s at H=100 in ~0.23 s
from neutral; reverse caps at exactly half forward. Encode as headless tests (M0).

## 6. Strip list — what the new core deliberately does NOT carry

1. `EngineASX` god-object (singletons, FindObjects, 3000-line tick); replaced by
   layered assemblies + hooks.
2. Per-component MonoBehaviours on every unit; units become data records +
   watched/active presentation.
3. One shared physics world for the whole galaxy → per-active-sector physics +
   graph queries elsewhere (kills the R1 galaxy-wide OverlapSphere scans).
4. Class/definitions serialized as prefabs → plain data (JSON or ScriptableObjects
   generated from data), so loadouts/weapon rework is a data problem later.
5. The nine legacy save-reader families (~27k LOC) — the new core's save is a
   projection of the new model (old saves: only via an optional importer, §8).
6. Unity APIs off the main thread; anything allocation-per-tick in hot loops.
7. SendMessage/UnityEvent-style fan-out in the sim; events become sim-side queues.

Presentation keeps: existing ship prefabs/models, FX, UI shell — adapters map ids →
prefabs exactly like the old `UnitView`/component-holder mapping already does.

## 7. Reference project facts (for later porting sessions)

- Repo: <https://github.com/JoppieToppie/Galaxy-on-Fire-2-Unity-Remake> (their code:
  **Apache License 2.0** — LICENSE.md; original GOF2 assets are proprietary and we
  take none of them). Fetched/read 2026-11-08.
- Files that matter to us:
  - `docs/notes/free-flight.md` — the ever-flight spec this §5 ports; every constant
    has provenance (measured from ES2 by their `Reference/tools/es2flight/es2measure.py`
    + decomp `Reference/research/es2_flight_measurements.md`).
  - `docs/notes/flight-model.md` — the *original* GoF2 linear flight constants
    (baseline speeds, handling H formula, ramp/decay math) — useful for NPC-style
    simpler flight and for the authentic mode if ever wanted.
  - `Assets/Scripts/Runtime/README.md` — the layered assembly + hook class + installer
    pattern (this is the architectural template for §4).
  - `CLAUDE.md` — their project-wide rules: plain-C#-logic-with-adapters, one
    MonoBehaviour per file, big-class partial-split discipline (~60 KB ceiling),
    per-area docs/notes files, ~3000 headless edit-mode tests, ms-based game time.
- What we adopt vs. not: adopt layer/hooks, plain-C# sim, ms clock, test harness
  discipline, flight math; NOT their netcode stack (we keep our host-authoritative
  protocol) and none of their content/assets.

## 8. Contracts kept from the old game (so the game stays "OpenFrontier")

1. Id scheme + bands (player 800M/900M, faction/sector/fleet/person/mission ids).
2. 30 Hz fixed cadence + budget philosophy (PerformanceSettings surfaces re-derived
   for the new kernel).
3. Host-authoritative multiplayer law and the wire protocol's shape (orders per id;
   8/19 fleet orders done, remainder ports onto the new sim once it exists).
4. Save story: short-term the old build keeps shipping saves; the new core adds its
   own projection later. **Open question:** optional old-save importer, or clean break?
5. Content continuity: prefabs/FX/UI keep working against new sim ids (adapters).

## 9. Milestones (planning-level, descope before green-light)

- **M0 — headless core slice (first code written):** identity registry with id bands;
  `FlightModel` port (§5) with the measured-value tests; tick kernel at 30 Hz
  (integer ms); tiny sector graph (2–4 sectors). Runs entirely offline in
  `tools/harness/check.sh`-style edit-mode tests; no Unity scene needed.
- **M1 — scale probe:** 2,000 sectors × N ships as pure data, no rendering; assert
  per-tick budget on reference mid-range phone numbers. Gate for the scale goal.
- **M2 — Unity slice:** one sector, one player ship flying with ES2 feel using
  touch + controller + optional mouse; chase camera; engines-off FX; presentation
  interpolating sim ticks. No enemies, no weapons. This proves the feel on device.
- **M3+ — port consumers onto the core, one subsystem at a time** (factions, intel,
  NPC AI, economy, save projection, multiplayer). Each port retires the matching old
  engine path.

## 10. Open questions (small, blocking M0–M2 not at all, or barely)

1. Old-save import: clean break vs. optional translator later (recommend clean break).
2. Final assembly/namespace names + placement under `Assets/` (§4 proposal stands
   until code starts).
3. Reference phone model for the budget doc (still unanswered from the report).
4. Touch-first ES2-style ergonomics: virtual-stick steering on touch HUD — validate
   in M2; fallback = original-style touch stick driving the same input struct.
5. Weapon/loadout data format: deferred with the subsystem.
