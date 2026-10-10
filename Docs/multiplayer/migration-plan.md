# Open Frontier — Multiplayer Migration Plan

**Status:** step 1 done (the two live bugs + tests). M1 not started.
**Written:** 2026-10-10
**Scope:** make InterstellarPilot 2 work identically in singleplayer and
additionally as host-authoritative co-op multiplayer, without breaking
singleplayer.

---

## 1. The decision that everything else follows from

### 1.0 The guiding constraint *(above all else)*

> **We are not changing the game. We are adding the ability for a client to
> receive its information from the host instead of computing it locally — and
> for the host to send that information out.**

That is the entire scope of this migration. Everything in this document is
either:

- **sending** — the host tells a client what it decided, or
- **not deciding** — a client stops deciding something and waits to be told.

Nothing else. No new gameplay, no rebalanced systems, no redesigned UI, no
altered singleplayer behaviour. When in doubt, the change is wrong, and the
right answer is to send more data rather than to change what is computed.

*"No redesigned UI" applies to the game's own UI.* Multiplayer-specific UI
(`MultiplayerScreen`, `JoinCodeScreen`, the saved-host list) is new surface
added for this feature and may be changed freely as the feature needs.

**The measure of success is not "multiplayer works".** It is:

> **A player who never opens the multiplayer menu cannot tell that any of
> this happened.**

The original game is the product. Multiplayer is an addition to it.

---

### 1.1 The authority ruleset

The original plan (see `Assets/Scripts/OpenFrontier.IP.Multiplayer/README.md`)
had the client own "the ship it is flying, always". That has been revised.
Authority is **per-unit and time-varying**:

```
manually flying your own ship  →  client simulates it, streams state UP
you place an autopilot order   →  host simulates it, streams state DOWN
you cancel the order           →  client resumes from the HOST's last position
everything else, always        →  host
```

Client actions never mutate state. They send **intent**; the host executes it
through the same code a local click would call — "as if they were just an AI
faction".

**Co-op only. No anti-cheat, no authority validation of a client's own ship.**
A client can teleport its own ship and the host will believe it. This is
accepted: it is a P2P game with no server to enforce anything, and it is
built for friends playing together.

> ⚠️ **This is not compatible with a dedicated authoritative server later.**
> That would require rejecting client-reported positions. It is a design
> decision that cannot be reversed cheaply, so it is recorded here
> deliberately.

---

## 2. Why the existing approach kept failing

`AuthorityGate` was written as a *brake*: it walks `engine.UpdatableUnits`
and sleeps the rigidbody of any unit not locally owned. That only stops
**physics**.

On a client today, all of this still runs:

| Still simulating on a client | Where |
|---|---|
| Every NPC pilot's AI | `UnitComponentHolder.cs:1021` → `NpcPilot.Tick()` |
| Fleet orders incl. trade/build/claim | `Fleet.cs:874` → `ActiveFleetOrder.Tick()` |
| Faction AI: spawn fleets, build stations, spend credits, change opinions | `FactionAIBase.cs:1073` |
| Continuous faction-opinion drift | `FactionOpinionNeutralizerComponent.cs:17` |
| Missions completing on their own | `MissionManager.cs:165`, `Mission.cs:456` |
| Auto-turrets and point defence | `UnitComponentHolder.cs:1037`, `:1041` |
| Cargo factories, expiry, heatmaps, security levels | multiple |

So the client was **partially** frozen. A partially-frozen simulation is the
worst case: the parts we braked agreed, and everything else drifted. That is
the signature of every bug in this project — the drift-and-snap-back, ships
that looked alive but weren't, jank that moved around as different pieces
were fixed.

Worse, the brake does not even work completely:

- `AuthorityGate.Hold()` sleeps the rigidbody in `LateUpdate`, **after** the
  simulation has already run for the frame.
- Several paths write `transform.localPosition` **directly**, bypassing physics
  entirely — `Fleet.cs:968`, `PlayerWaypointUnitSyncer.cs:135`, and rotation
  via `ActiveUnitShip.ApplyTurn`. A sleeping rigidbody cannot stop those.
- `AuthorityGate.IsLocallyOwned` (`AuthorityGate.cs:140-148`) requires
  `IsActiveInEngine`, which is **false for anything outside the active
  sector** — including the player's own parked ship.

**Conclusion: the brake must be replaced, not extended.** Suppress the
simulation itself; let the brake become a safety net rather than the
mechanism.

---

## 3. Two live bugs found during research

These are **already shipping**, today, in ordinary singleplayer. They are
not multiplayer work — they are damage already done.

### 🔴 BUG 1 — `ReassignFactionId` corrupts the faction id counter

`EngineASX.cs:1743-1746`:

```csharp
if (newId >= FactionIdCounter)
{
    FactionIdCounter = newId + 1;   // jumps to 900,000,001
}
```

`PlayerFactionAdopter` runs unconditionally at startup in **every singleplayer
game**, so this fires constantly. Afterwards, **every AI faction the world
generator creates is numbered inside the reserved player band.** `IsPlayerMade`
returns true for ordinary NPCs, the band stops meaning anything, and the
corruption is written into every save and reloaded.

**Fix:** delete the counter bump. `GetUniqueFactionId()` already scans
`ContainsFactionById` (`EngineASX.cs:5226`), so the bump buys nothing and
costs everything. One line.

> The existing codec tests **cannot see this** — `SeededFactionIdTests` runs
> `PlayerIdScheme` against stubs, never against a real `EngineASX`.

### 🔴 BUG 2 — god mode and hypersleep disabled by the options toggle

`WorldPlayerPermissions.SharedSession` is set from `MultiplayerSettings.Enabled`
— that is, the **options toggle**, not "is anyone connected"
(`MultiplayerHostService.cs:212-216`).

A solo player who ticks "Enable Multiplayer" so friends can join later
**silently loses god mode and hypersleep in their own single-player game.**
Zero players required.

**Fix:** derive it from the session, not the toggle:
`SharedSession = MultiplayerHostService.Session != null && Session.Players.Count > 0`.

---

## 4. Design: authority as a first-class concept

### 4.0 The non-invasiveness principle *(hard constraint)*

> **You are either a client asking the host, or you are not, in which case
> you simulate it yourself.**

Every consult point is written so that **the non-client path is byte-for-byte
today's behaviour**. This is not a preference — it is the safety property
that makes the whole migration survivable. It means:

- A singleplayer player never reaches a multiplayer branch. Not "the branch
  is cheap", not "the branch returns the same value" — the branch is not
  reached.
- No existing call site changes meaning. Only the *decision* about whether to
  simulate moves, and only on a copy that is a client.
- Every addition is guarded by one check of the form
  `if (!Authority.IsThisCopyAClient) { /* exactly today's code */ }`.

The test for "non-invasive" is: **can a singleplayer player reach any changed
line?** If yes, the design is wrong.

**Correction to the two-way simplification** — it is three states, because the
player's own ship is the exception in *both* directions:

| State | The ship it flies | Everything else |
|---|---|---|
| Host / singleplayer | simulates | simulates |
| Client, **manually** flying | **simulates** | asks the host |
| Client's ship **on autopilot** | asks the host | asks the host |

### 4.1 The seam

The engine must be able to ask "am I authoritative for this?" before
simulating. The engine **cannot** reference the multiplayer assembly, so the
seam is a provider the engine owns and multiplayer fills in:

```csharp
// engine side
public interface IAuthorityProvider
{
    bool IsThisCopyAClient { get; }
    bool OwnsUnit(Unit unit);   // true when this copy simulates it
    bool OwnsFaction(Faction faction);
    bool OwnsWorld();           // faction AI, missions, economy
}

public static class Authority
{
    public static IAuthorityProvider Provider;   // null ⇒ authoritative for all
}
```

**`Provider == null` means authoritative for everything.** That is the
singleplayer guarantee, and it is structural rather than conditional — there
is no path by which a null provider can answer "no".

Rules:
- Plain C#, no MonoBehaviour required, never throws on a null engine/world.
- Resolve `EngineASX.Instance.LocalFaction` on **every** call, never cache it
  (god mode swaps the player's faction at runtime —
  `GodModeGeneralOptions.cs:242`).
- Reach it from editor tooling without a live scene (see §7).

---

## 5. Milestones

Each is independently testable and independently committable.

### M1 — An inert client *(the big one)*

Everything except the player's manually-flown ship stops running on a client.
Not frozen — **not run.**

Consult points, highest value first:

| # | Path | What it stops |
|---|---|---|
| 1 | `UnitComponentHolder.Tick()` `:989` | ship AI, weapons, shields, sensors, shipyard, auto-turrets |
| 2 | `Fleet.Update()` `:849` → `Fleet.Tick()` `:857` | fleet orders incl. trade, build, claim |
| 3 | `FactionAIBase.Update()` `:1073` | faction AI, credits, opinions |
| 4 | `MissionManager.Update()` `:165`, `Mission.Update()` `:456` | missions self-completing |
| 5 | `FactionOpinionNeutralizerComponent.Update()` `:17` | opinion drift |
| 6 | `FactionSpawner.Update()`, `RandomUniverseEventsController`, `TriggerGroup`, `WorldBase.Update()` | world spawning, daily income |
| 7 | `ActiveUnitShip.Update/FixedUpdate` `:112`/`:120` | the integrator (replaces the brake) |
| 8 | Sector security, heatmaps, cargo expiry, asteroid respawn, passengers | lower-frequency world state |

`AuthorityGate` is folded in, not left alongside.

**Success test:** host flies; on a client, **nothing moves that the host did
not move**. Make it mechanical, not visual — count mutations a client attempts
that authority refused. That is how the original drift would have been caught.

**Expected and not a regression:** movement becomes correct while other UI
stays wrong — credits, market prices, mission log. Those are faction-level
and come later.

**Order of work:** one path at a time, test, commit. Do **not** do all eight at
once; this session's evidence is that a multi-edit change hides which edit
caused a regression.

### M2 — The command channel

Client sends intent; host executes via the existing local-click path.

Research found this is **not** one seam but **~20 channels**, which collapse
onto two methods:

- **`OrdersHelper.SetFleetOrder`** (`OrdersHelper.cs:22`) — covers **~50 UI
  call sites**, all 29 order types. The single highest-value edit in the
  codebase. Fleet orders are already fully data-driven
  (`(orderTypeId, targetUnitIds, targetSectorId, stack)`), so a client can
  serialise an intent and the host rebuilds it with identical code. This is
  literally "as if they were an AI faction", and it is already ~80% there.
- **`EngineASX.RegisterTaxedPlayerTrade`** (`EngineASX.cs:3410`) — the entire
  player credit ledger, 5 call sites. An **accounting** hook, not a decision
  hook: it fires after cargo has already moved. Treat as a divergence
  detector, not the channel.

Then ~18 smaller channels (cargo eject/transfer, repairs, component trade,
ship buy/sell, station build, comms, dock/undock, renames, bounty, self-destruct,
passengers, faction settings, scans). Each is 1-4 lines in one already-small
method.

### M3 — Handoff

- **Order placed** → client stops predicting *that frame*. No grace period, or
  the two integrators fight and the ship jolts.
- **Order cancelled** → resume from the **host's** last position, not the
  client's own prediction. Resuming from your own is the classic
  teleport-on-cancel bug.

### M4 — Events

Combat, destruction, weapon fire. **Mandatory once M1 lands**, because with
weapons dead on the client a hit lands and nothing happens. The README's
"large" item.

---

## 6. Bandwidth (measured, not guessed)

52 bytes per moving ship per update, at 10 Hz, per client. Interpolation draws
0.22 s behind the host, so it *looks* like 60 fps and costs a sixth as much.

| Moving ships | @10 Hz | @15 Hz | @30 Hz |
|---|---|---|---|
| 30 | 15.4 KB/s | 23.0 | 46.1 |
| 100 | 51.0 KB/s | 76.5 | 153.0 |
| 300 | 152.9 KB/s | 229.4 | 458.8 |

Framing overhead is 0.4% at 64-ship batching.

**Staying at 10 Hz.** 30 Hz triples cost and buys nothing — interpolation
fills the gaps. The rate's real purpose is latency, not smoothness, and 10 Hz
→ 220 ms lag on *other people's* ships is invisible because nobody steers them.

Two levers if it ever matters: drop velocity (12 of 48 bytes, unused by
interpolation) for a 25% cut; and distance-weighting so distant ships drop to
a slow rate.

Two real limits: a 300-ship battle is ~153 KB/s per client, and four players in
one sector send the same data four times (the only case that would need
deduping).

---

## 7. Singleplayer safety — ranked risks

| | Risk | Mitigation |
|---|---|---|
| 🔴 | BUG 1: faction id counter corruption (§3) | delete the counter bump |
| 🔴 | BUG 2: god mode/hypersleep from the toggle (§3) | derive from session, not toggle |
| 🔴 | An authority gate would freeze the player's **parked** ship — `IsActiveInEngine` is false outside the active sector | check a singleplayer default **before** any per-unit predicate |
| 🟠 | Stations and all non-ship units are **not** in `UpdatableUnits` (`EngineASX.cs:1907-1910`), so any gate walking that list never covers them — the player's station keeps simulating on a client forever | gate on sectors / `IsActiveInEngine`, or add an explicit "does this unit simulate at all" predicate |
| 🟠 | Old saves **half-load silently** — `PlayerFactionAdopter` gives up quietly when the derived id is occupied; nothing validates ids on import | bump `EngineIO.SaveVersion` and refuse to continue with a clear message |
| 🟠 | God mode swaps `LocalFaction` at runtime | resolve on every call, never cache |
| 🟡 | `SaveGameModelExporter` uses `Dictionary.Add` → throws on duplicate id. Symptom: host cannot export, joiner waits forever — indistinguishable from "host unreachable" | indexer + logged warning naming the id |
| 🟡 | `PlayerIdScheme.Reserved` is process-wide static; `Forget()` is called from nowhere, so ids leak across world loads | clear on world teardown, or make it world-owned |
| 🟡 | Story triggers `Trigger_Player_*` require `Hud.IsCurrentScreen` — **"the player" is a UI-relative concept** | resolve "the player" only via `EngineASX.Instance.LocalPlayer`, never via HUD |
| 🟡 | `AuthorityGate` + `UnitInterpolator` are already in the singleplayer frame; anything making their early-outs fail **freezes the world silently** | keep early-outs first; assert zero suppressions in singleplayer |
| 🟡 | Editor tooling and prefabs fail *silently* (three documented traps in AGENTS.md) | after any new serialized field, run `check.sh` **and** confirm zero `error CS` in `Logs/Editor.log` |

**Story scenarios are safe.** `unchartered_space.unity` and
`a_wormhole_too_far.unity` reference the player faction by **Unity object
reference** (scene pointer at line 82841), not by id. Renumbering keeps the
same object, so every reference keeps working. `Faction.IsPlayerFaction` is
object identity. The one id comparison on the story path is
`FactionSetup.cs:15`, and it compares two *different* objects, so renumbering
stays correct.

**No automated engine tests exist.** The only harness is outside the repo at
`/tmp/opencode/harness2/`. It compiles `PlayerIdScheme` against stubs and
**structurally cannot see BUG 1**. Two gaps to close before M1: a test
asserting `FactionIdCounter` is unaffected by `ReassignFactionId`, and one
asserting a singleplayer frame performs zero suppressions.

---

## 8. Dedicated servers — not now, and not cheap

Currently impossible. Blockers in order of severity:

1. **`UnityEngine.Physics` is load-bearing in the simulation** —
   `EngineASX.cs:3967-3975` does a `Physics.Raycast` inside `Update`, and the
   entire sync mechanism rests on PhysX, not a deterministic sim.
2. **Tick rate is `Time.deltaTime`-derived** — a server on a different frame
   rate produces a *different simulation*. No fixed-timestep accumulator.
3. **The active sector is a rendering concept** — its setter pools objects,
   clears audio and gas clouds, repositions sectors.
4. **UI gates gameplay triggers** — `Trigger_Player_*` need a HUD.
5. No `UNITY_SERVER` / `-batchmode` / `-nographics` define anywhere.

Record the intent; do not design for it now.

---

## 9. Already built (do not rebuild)

Transport and session: handshake, exact-version gate, session secret, TCP,
`MainThreadDispatcher`, `MessageChannel`, `NetListener`/`NetDialer`,
`PlayerIdentity`, `MultiplayerSettings`, `HostProbe`, UPnP, fixed port range
24890–24897, player profile slots.

Menu: `MultiplayerScreen`, `JoinCodeScreen`, saved-host list with
latency/scenario/players/uptime.

World transfer: `WorldSnapshotChunkMessage`, `SnapshotReassembler` (bitmap
coverage), `SnapshotChunker`, `WorldSnapshotBuilder`, `WorldSnapshotSender`,
`ReceivedWorldLoader`, `WorldDescriptorMessage`, `JoinerPreferencesMessage`,
`HostFarewellMessage`, `UnitOwnershipMessage`, `UnitStateMessage`,
`FactionStateMessage`, `JoinerReadyMessage`.

Session: `JoinerFactionFactory`, `JoinerPilotAdoption`, `JoinerIdentityAdopter`,
`ClientOwnershipView`, `UnitOwnershipRegistry`, `HostShipUpload`,
`AuthorityGate`, `ClientShipUploader`, `UnitStateBroadcaster`,
`UnitStateApplier`, `UnitInterpolator`, `FactionStatePusher`,
`FactionStateApplier`, `WatchedSectorWatcher`, `PlayerUnitIdSeeder`,
`PlayerFactionAdopter`, `PlayerIdScheme`, `EngineASX.ReassignFactionId`,
`Sector.SimulatesEveryFrame`.

Bugs already fixed this session: interpolation was entirely absent (README
claimed otherwise); `num--` deleted from `EngineASX.UpdateInactiveUnits`,
freezing every launch; the join failing because a saved shuttle's id was
handed out twice; nothing streamed until a joiner announced readiness.

---

## 10. Immediate next actions

**Done** (commit `70045bb0`):
- BUG 1 — the counter bump deleted, *and* the engine now asks an
  `IReservedIdPolicy` before handing out any id, so the corruption is
  structurally impossible rather than merely absent. All ten id allocators
  consult it; a throwing policy fails open rather than closed.
- BUG 2 — `SharedSession` derived from the session's player count, through a
  method that takes a count so the rule can be asked about without a session.
- Tests for both. BUG 1's needed somewhere for the engine to put the
  question, which is what `ReservedIds` is.

**Next:**
1. **Play-test in singleplayer.** Both fixes change ordinary play: faction ids
   now stay in the engine's own range instead of inside the player band, and
   god mode/hypersleep now work with the multiplayer option ticked on and
   nobody connected. Confirm nothing regressed before anything else is touched.
2. **Then M1**, one consult point at a time, each committed separately.

**Note on old saves:** a save written before this commit may carry a
`FactionIdCounter` inside the player band. Loading it still works — the
allocators skip the band now — but the counter will take a long walk back down
to the engine's range before it starts producing ordinary ids again. A fresh
save avoids that. This is the save-breakage the id bands were always going to
cause, arriving a little earlier than planned.

---

## 11. Resolved questions

1. **Host copy or joiner copy first?** → **Host.** The host is authoritative
   for everything, so it is the least surprising thing to be testing and the
   failures are the most legible. Downside: it is the one place you cannot be
   a client. Accepted.
2. **Do we visualise refused mutations?** → **Yes.** A counter of authority
   rejections makes M1's success test *mechanical* rather than visual. It is
   also the only way to catch a client mutating something silently again —
   which is the exact failure this whole migration exists to prevent, and the
   one that has now happened twice.
3. **Should god mode stay enabled in a shared session?** → **Leave it
   disabled** (current behaviour) and do cheats from the host. Re-enabling it
   on a client would mean a client mutating state the host does not own —
   directly against the principle in §4.0.

## 12. Execution order

Nothing below depends on a question still being open.

1. **BUG 1** — delete the counter bump in `ReassignFactionId`
   (`EngineASX.cs:1743-1746`). One line. Fixes a bug that is shipping today.
2. **BUG 2** — derive `SharedSession` from the session, not the options
   toggle (`MultiplayerHostService.cs:212-216`). One line. Fixes a second
   bug that is shipping today.
3. **Test for 1 and 2** — `FactionIdCounter` unaffected by `ReassignFactionId`;
   `SharedSession` false when the toggle is on but nobody is connected. Both
   are invisible to the current harness.
4. **M1**, one consult point at a time, each committed separately, each with a
   mechanical success test.

Steps 1 and 2 are ordinary bug fixes, not multiplayer work, and would be worth
doing whether or not multiplayer ever ships.