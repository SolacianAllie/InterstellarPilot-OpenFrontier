# Open Frontier — Full Multiplayer Coverage

**Goal:** the entire game works in multiplayer, except the main menu.

**Success measure (unchanged, and it is the one that matters):** *a player who
never opens the multiplayer menu cannot tell that any of this happened.*

**Audience:** a public release for a small but sizable community. Peer-to-peer
and friend session codes are **mandatory** — there is no backend and none will
be added. So every design choice below has to survive contact with real home
networks, and "it works on my machine" is not evidence.

---

## Progress

Updated as work lands. "Verified" means read in a log on both sides, not
compiled.

### Stream A — honest failure: **DONE, awaiting verification**

| | |
|---|---|
| A1 | `ExecuteOnHost` counts refusals and says ran / refused / ran-some-and-refused-some. `refused++` in all three refusal arms. |
| A2 | `SendIfClient` says so when the player selected nothing, and names the likely cause (a fleet that exists on this copy only). |
| A3 | `Authority.Refused(what, whatItWouldHaveChanged)` — counts the decline and names the action, once per action. Four chokepoint gates: `Faction.ApplyTransaction`, `DestructableUnit.ApplyDamage`, `UnitComponentHolder.TryDockInUnit` / `UndockIfDocked`, `ShipBuyScreen.Buy`. |

### Stream B — infrastructure: **5 of 8 done**

| | |
|---|---|
| B1 | `FactionStateApplier` installed in `MultiplayerClientService.Install`. It had never been fitted to anything. |
| B2 | `FactionAIBase.Update` gated on `Authority.OwnsFaction`. Clients were simulating a galaxy economy. |
| B3 | `MayBeClaimed` refuses units the host has never heard of, and says so in words that are not "it belongs to the host". |
| B4 | **Folds into D1** — a purchase is now refused on a client, so no out-of-band ship can be created until the relay exists. |
| B5 | `ApplyWorldRestrictions` distinguishes "not hosting" from "alone". God mode was live on every joiner. |
| B6 | Not done — dead comments in `PropertyScreen` / `OrdersHelper`. Cosmetic. |
| B7 | Not done — needs E3. Hull/shield/capacitor are not on the wire. |
| B8 | Partly done — the README's DONE claim is corrected and points at the coverage plan. |

### Stream C — command channel: **STARTED**

| | |
|---|---|
| C1 | `PlayerCommandMessage` (id 14) — one envelope, `int[]` + `string[]`. Codec tested against lying headers. |
| C2 | `PlayerCommands` — registry, host dispatch via `HostSession.PlayerCommandRequested`, handlers validate through the ownership registry before mutating. |
| C3 | `Authority.DockRequest` / `Authority.UndockRequest` — the engine's door. Return false when nobody is listening, so host and singleplayer are untouched. |

### Stream D — empire: **1 of 12, and it unblocks the station**

| | |
|---|---|
| D2 | **Docking and undocking — DONE.** Host-authoritative, `DockedStateMessage` (id 15) broadcasts hangar *and* bay, applied on every copy by `DockedStateApplier`. Un-gates the whole station interface. |

### Not started

D except docking · E combat · F remaining orders · G legibility · H comms ·
I public release · J audit

### What to check in the next session

On the **client**, at startup:

```
Authority: CLIENT - following another player's game; would simulate 1 of N ship(s) and 0 of M faction(s)
```

`0 of M factions` is B2 landing. Before this it always said `M of M`.

Then, on the **client**, press Buy on a docked ship, and fire at anything:

```
Multiplayer: refused - buying a ship. Nothing changed. It would have spent 45000 credits...
Multiplayer: refused - firing weapons. Nothing changed. It would have damaged ship 100812 for 34...
```

Both are new. Neither was possible before — the log could not distinguish a
refusal from a success.

On the **client**, dock at a station and undock:

```
Multiplayer: asked the host to dock ship 800000000 at 100506
Multiplayer: ship 800000000 is docked at station 100506, bay 2
```

On the **host**, the same moment:

```
Multiplayer: docked ship 800000000 at station 100506, bay 2
```

And on **every** copy, including other players, the applied line — that it
goes to everyone rather than only the asker is the thing to confirm, since two
players watching the same ship must see it in the same hangar.

If a dock fails on the host, every copy is told the ship is *not* docked, and
the host says why:

```
Multiplayer: could not dock ship 800000000 at 100506 - it does not fit, or the hangar is full.
Multiplayer: ship 800000000 is out of its hangar
```

On the **host**, after any client order:

```
Multiplayer: ran DockAtTarget for 1 fleet(s)          <- success
Multiplayer: REFUSED Patrol for all 1 fleet(s) - none of them were carried out.   <- refusal
```

A host log containing "ran X" with no preceding refusal warning for the same X
is now trustworthy, which it was not before.

---

## How to read this

| Marker | Meaning |
|---|---|
| **WORKS** | Relayed; host mutates, client sees the result. |
| **PARTIAL** | Some of it is relayed; the rest silently diverges. |
| **BROKEN** | Client mutates its own copy. Host never told. Nothing corrects it. |
| **READ-ONLY** | Displays only. Safe, *provided* what it displays is correct. |
| **UNVERIFIED** | Not yet checked. Assume broken until proven otherwise. |

Status legend applies to the **backlog table** in §6.

**The one rule, and every stream obeys it:**

> The client never decides. It sends a request, the host decides, the host
> relays the result back to everybody.

There are no exceptions and no "but it's only a rename" exemptions. A rename
that happens on the client's copy only is a bug of exactly the same species as
a ship purchase that happens on the client's copy only, and both are on this
list.

**The recurring lesson of the last several sessions**, recorded here so it is
not re-learned: *a reason is generated and then dropped before it reaches a
log.* It has happened at least five times, in both directions — reasons dropped
(`SendIfClient`'s silent-discard path) and reasons buried (`ExecuteOnHost`
printing `ran Patrol for 1 fleet(s)` immediately after saying it refused
Patrol). Anything in this plan that cannot be seen in a log cannot be verified.

---

## 1. What actually works today

Eight things. This is a real foundation and it should not be thrown away.

| | Evidence |
|---|---|
| **Join / leave** | `HostSession`, session codes, `WorldSnapshotSender` sends a full save at join |
| **Host departure** | `HostFarewellMessage` carries the final world; `WorldSnapshotSender.cs:213` |
| **Flying your own ship** | `AuthorityGate` + `ClientAuthorityProvider.OwnsUnit` — one ship simulated per copy |
| **Ship streaming, both ways** | `UnitStateBroadcaster` down at 10 Hz; `ClientShipUploader` up |
| **Ownership handover** | Claim on manual fly, host takes it for orders, returns on `AllObjectivesRemoved` |
| **Fleet orders** | **8 of 19** `FleetOrderKind` values |
| **Sector travel** | The wormhole move |
| **Faction identity** | `PlayerIdScheme` reserved bands, `PlayerFactions.IsPlayerFaction` |
| **Sensors** | `RefreshDetectionRadiusIfDue` runs *before* the authority gate — contacts are correct |
| **Kill credit (own ship)** | `Person.Kills` tallies locally on the ship you actually fly |

**Scale of the codebase:** 369 screen files, 78 player-facing feature areas,
one game assembly (`OpenFrontier.IP`) with sub-namespaces, 12 protocol message
types of which **7 travel client → host**.

**Multiplayer awareness in game code:** `IsThisCopyAClient` appears **17 times
in 9 files**. Six of the 78 feature areas contain any multiplayer awareness at
all.

---

## 2. The failure mode, which is uniform

Every broken thing in §6 has the same shape:

```
player clicks
  → their own copy of the world changes
  → the host is never told
  → nothing ever corrects it
```

Not *refused*. Not *queued*. **Silently divergent**, and usually visibly so
within seconds — a ship that everyone else cannot see, credits that disagree
between machines, damage that appears and then un-does itself as the host
streams the target back at full health.

This matters for prioritisation: there is one bug class, forty instances, and
a known fix for it. Fleet orders are the worked example — a UI helper that
either mutates locally or sends an intent, with a host handler that calls the
*same* helper. We did that eight times already. The other forty are tedious,
not hard.

**But**: *silent divergence is worse than refusal.* A player who presses Buy
and sees a ship appear is in a worse position than one who is told "not
available in multiplayer yet", because the first has no way to know something
is wrong. See Stream A.

---

## 3. Infrastructure integrity — eight bugs that poison everything else

These are ahead of every feature because until they are fixed, each feature is
built on a foundation that silently corrupts itself. They are cheap.

### B1. `FactionStateApplier` is dead code — the receipt half of faction state never runs

**Verified.** `grep -rn FactionStateApplier Assets/Scripts --include=*.cs`
outside its own file returns **nothing**. Its GUID appears in no prefab and no
scene. `MultiplayerClientService.Install()` (`MultiplayerClientService.cs:106-114`)
adds `UnitStateApplier` but **not** `FactionStateApplier` — while
`WorldSnapshotSender.cs:127-132` deliberately adds *both* on the host side,
with a comment about exactly this class of bug ("the broadcaster … used to be
nobody's job").

Consequence: the host pushes credits and cargo hold (`FactionStatePusher.cs:160`),
`ClientSession.cs:290` decodes it and raises `FactionStateReceived`, **and there
is no subscriber**. `FactionStateApplier.Apply:135` (`faction.Credits =
message.Credits`) never executes. A client shows its join-time credits forever,
and no client-side economy edit is ever corrected.

`README.md:63` calls this DONE. `README.md:52` draws the same arrow and labels
it `NOT BUILT`. **The diagram is the accurate one.**

**Fix:** install it, and add a log line on apply so we can see it firing. Then
delete the DONE claim from the README.

### B2. `Authority.OwnsFaction` and `OwnsWorld` are never consulted

**Verified.** The only caller of `OwnsFaction` in the entire repository is
`AuthorityModeReport.cs:116`, a diagnostic. `OwnsWorld` has **zero** callers.

`ClientAuthorityProvider.cs:102-107` promises "a client does not run an economy,
a reputation drift, or an expansion search". Nothing enforces it.
`FactionAIBase.cs:1073` is a plain `Update()` with no gate.

Consequence: **every client runs a full second faction-AI and economy
simulation of the entire galaxy**, diverging from the host's, silently.

**Fix:** gate the economy/AI tick on `Authority.OwnsFaction(faction)`, and log
the decline count through the existing `AuthorityModeReport`.

### B3. `MayBeClaimed` grants ownership of ships the host does not have

`HostShipUpload.cs:253-257` returns `true` when `engine.GetUnitByid(unitId) == null`.

Combined with buy-and-enter, a client that buys a ship requests ownership of a
brand-new engine-numbered id; the host's `TryClaim` succeeds because nobody
holds it; the host then **broadcasts to everyone** that this player owns it
(`HostSession.cs:485`). The registry holds a phantom forever, and
`HostShipUpload.Apply` counts it `unknown` and drops it.

**Fix:** an unknown unit must be *refused*, not claimable. Ownership is a claim
about an object both copies have.

### B4. Bought ships get engine-counter ids, not player-band ids

**Verified.** `ShipBuyScreen.SpawnUnit` (`ShipBuyScreen.cs:357-372`) calls
`component.Init()` and never touches `PlayerIdScheme` (zero references in that
file). `Init()` → `Unit.SetEngine` → `EngineASX.GetUniqueUnitId` — a monotonic
counter starting at 100000. So a purchased ship is ~100500, **outside the
800,000,000 player band**, and every `IsPlayerMadeUnit` test disagrees with
itself depending on how the ship came to exist.

`JoinerFactionFactory.cs:226-228` already documents this.

**Fix:** once D1 exists, the host assigns ids from `PlayerIdScheme.NextUnitId()`.
The primitive already exists and is proven (`JoinerFactionFactory.cs:442-457`,
`PlayerIdScheme.NextUnitId()` + `EngineASX.ReassignUnitId`).

### B5. God mode is not disabled on a client

`WorldPlayerPermissions.SharedSession` is set only from
`MultiplayerHostService.cs:226` via
`SetSharedSessionFor(session == null ? 0 : session.Players.Count)`.
`MultiplayerHostService` self-installs on every process (`:107`), but a pure
joiner has `Session == null` ⇒ count 0 ⇒ `SharedSession = false` ⇒ the
restrictions at `WorldPlayerPermissions.cs:96` and `:117` read false and the
buttons stay visible, contradicting the doc comment at `:50-58`.

**Fix:** derive it from *this process's role*, not the host's player count.

### B6. Three dead null-branches from the fleet-creation change

`CreateAndInitPlayerFleet` (`OrdersHelper.cs:1078-1124`) now returns
unconditionally and documents "any copy may build a fleet". The
`if (fleet == null)` handlers at `PropertyScreen.cs:192`, `OrdersHelper.cs:1395`
and the comment at `OrdersHelper.cs:563-575` ("null means this copy may not
build fleets, which is a client") are unreachable.

Not a live bug — dead code that will mislead the next reader. **Fix:** delete
or update the comments.

### B7. No hull, shield or capacitor on the wire

**Verified:** `grep -rE "Hull|Shield|Capacitor|CurrentHealth"` across
`OpenFrontier.IP.Multiplayer` returns **zero matches**. `UnitState` carries
`Id, Position×3, Rotation×4, Velocity×3, CurrentTurn, Throttle, SectorId, Time`.

This is why Stream E is a design problem and not a plumbing problem: a ship
that is being shot at has state that must be *authoritative on the host* and
*pushed to the client*, in both directions. See E3.

### B8. The README contradicts itself

`README.md:52` (`NOT BUILT`) vs `README.md:63` (`DONE`) about faction state.
The diagram is right. Fix as part of B1, and keep the diagram as the source of
truth from then on.

---

## 4. Architecture

### 4.1 The relay pattern (proven, reuse everywhere)

```
CLIENT                                  HOST
──────                                  ────
OrdersHelper.OrderDockAtTarget(fleet,…)  FleetOrderCommands.RunFor(player, body)
  └─ if (Authority.IsThisCopyAClient)      └─ NamesOnlyTheirOwn  (validate)
       └─ FleetOrderIntents.TrySend(…)       └─ WithFleetsForLooseShips
            └─ return true; do nothing       └─ ExecuteOnHost
                                               OrdersHelper.OrderDockAtTarget(…)
                                               ← the SAME helper, host's copy
                                               └─ AssignNextOrderIfNoCurrentOrder
                                            └─ SendToEveryone (the result)
```

Three rules this shape encodes, worth keeping:

1. **The same helper runs on both sides.** The host does not reimplement an
   order; it calls the identical method the UI called. So the two copies cannot
   disagree about what "dock at target" means.
2. **Ships are named, never fleet ids.** `OneFleet` (`OrdersHelper.cs:75-78`)
   returns empty on a client, so no fleet id a client invents ever reaches the
   host. The order names *ships*, which both copies already agree on.
3. **The host announces the takeover after the order ran**, never before — a
   client that let go on *send* would be stranded if the order were refused.

### 4.2 The seams already in place

| Seam | File | Purpose |
|---|---|---|
| `Authority` | `Engine/Authority.cs` | per-unit, time-varying authority; every answer fails toward **yes** |
| `IAuthorityProvider` | — | the interface multiplayer fills in |
| `AuthorityGate` | `Multiplayer/Session/AuthorityGate.cs` | who may move a ship on this copy |
| `PlayerIdScheme` | `Multiplayer/Session/` | reserved id bands (faction 900,000,000+; unit 800,000,000+) |
| `PlayerFactions` | `Engine/PlayerFactions.cs` | is this id a player's? (band test, no message needed) |
| `ReservedIds.Policy` | `Engine/ReservedIds.cs` | band policy |
| `FleetOrderIntents` | `Engine.FleetOrders/FleetOrderIntent.cs` | the intent/trySend plumbing |

**Unused and load-bearing:** `Authority.OwnsFaction` and `Authority.OwnsWorld`
exist and are consulted by nothing (B2). They are the correct gate for the
entire economy cluster. Wiring them is most of Stream D's safety net.

### 4.3 Protocol

- 12 message types; ids 1–13 allocated. **Next free id: 14.**
- Client → host: 7 (Hello 1, Ping 3, UnitState 5, UnitOwnership 8,
  JoinerPreferences 9, JoinerReady 12, FleetOrderCommand 13).
- Host → client: UnitState, UnitOwnership, FactionState 11, WorldDescriptor,
  WorldSnapshotChunk, HostFarewell, Ping.
- **Both editor and build must be rebuilt for any protocol change.**
- Stream C turns this into a general command channel. Do not skip to C before B.

---

## 5. Work streams

Ordered by dependency, not by fun. Each stream ends with something checkable in
a log.

### Stream A — Honest failure *(do first, it is small)*

The whole map is being diagnosed through a log that lies. Fix that before
anything else.

**A1.** `FleetOrderCommands.ExecuteOnHost` prints
`"Multiplayer: ran " + kind + " for " + fleets.Count + " fleet(s)"`
unconditionally, after the switch. Every refusal arm `break`s into it. So the
host logs, on adjacent lines:

```
Multiplayer: order Patrol cannot yet be given by a client, and was not carried out.
Multiplayer: ran Patrol for 1 fleet(s)
```

**This is why "only one order fired in the whole test session" was
undiagnosable.** The other orders may have arrived and been refused, with the
log saying otherwise. Fix: track whether any arm actually did work; if none
did, say *that*, and do not say "ran".

**A2.** `FleetOrderCommands.SendIfClient` returns `true` having logged nothing
on the path where `fleetCount == 0 && looseCount == 0`. A refused send must
say so.

**A3.** *(needs your decision — see §8)* Should every unwired action **visibly
refuse** until it is wired? My recommendation is yes: one gate, one honest
message, and a list that only ever shrinks. Silence is what made this map take
this long to produce.

**A4.** Once A3 is decided, put a `Authority.Declined()` gate at each of the
~40 mutation sites in §6, so an unwired action fails *visibly* rather than
silently. This is the safety net that makes the rest of the work safe to do in
waves — a missed site stops being a silent corruption and becomes a log line.

### Stream B — Infrastructure integrity *(§3, all eight)*

Do all of B before C. B1, B3, B4 are each a handful of lines. B2 is the one
with real thought in it.

### Stream C — The command channel *(the general answer)*

~40 player actions each want a message. Written individually that is 40 message
types, 40 ids, 40 codecs, 40 host branches, 40 client branches, and 40 rounds
of codec-test updates. The repo's own `README.md:84-104` sketches the right
shape: one command channel, with the faction-state push acting as the receipt.

**Proposed:** one envelope, typed handlers.

```
CommandMessage (id 14)
  byte   CommandId      // which player action
  int[]  IntArgs
  string[] StringArgs
```

- One wire format, one dispatch site in `HostSession.OnPlayerMessage`, one
  codec, one round of codec tests.
- Each action registers a **typed handler** in a registry, so type safety lives
  at the handler layer where the compiler can see it, not in a 40-case switch.
- New actions stop being protocol changes. (Version skew is then a *registry*
  concern — a client that sends an id the host doesn't know must be refused
  loudly, which is the same honest-refusal rule as A3.)

**Precondition:** B1. The receipt half has to work before the command half
means anything.

### Stream D — The empire *(your framing: own a small empire and order it)*

This is the chain you said matters, and it is cut at the first link.

**D1. Buy / sell a ship** — the deepest hole. There is **no message in the
protocol that can create an object**. `ShipBuyScreen.cs:289`/`:359` spawns a
real `Unit` locally, debits local credits, and the host never hears.

```
client → ShipBuildRequest(unitClassId, dockUnitId)
host   →  charges credits (Faction.ApplyTransaction — already authoritative)
          constructs via WorldHelper.SpawnUnitAndInstallComponents
          assigns id via PlayerIdScheme.NextUnitId()   ← fixes B4 too
host   →  NewUnit(id, unitClassId, factionId, sectorId)
client →  constructs the same ship locally from unitClassId
```

**Host-assigned ids, not client-allocated.** The 800,000,000 band would permit
client allocation with no round trip, but a purchase is the one place a 100 ms
round trip is invisible, and host-assigned cannot collide. Take the round trip.

**A position message cannot express a ship.** `UnitState` is kinematic. A new
unit needs its class and its construction — so `NewUnit` carries `unitClassId`,
and the client builds with the same factory the host used, because both run the
same build and the same unit-class tables. A freshly bought ship has stock
modules, so `unitClassId` alone probably carries it; **custom variants and
upgraded ships are the known boundary** and need a decision (§8).

**Sell ship** (`SellShipScreen.cs:50`) is the same message with a sign.

**D2. Dock / undock** — **not on the wire at all.** No hangar/parent/docked
field in `UnitState`, so `UnitHangar.Items` diverges permanently and visibly
(`SectorMap.cs:404` filters on `unit.IsDocked`; `JoinerFactionFactory.cs:277`
skips docked ships when snapshotting). Undock is also not a fleet order — there
is no `Undock` in `FleetOrderKind` — and `DockedShipsScreen.cs:190` is four
lines with zero multiplayer awareness.

Needs either a `DockedIn` field on `UnitState` or its own message. **Recommend a
field**: docked-ness is ship state that must be host-authoritative and pushed
every tick, not a one-shot event.

**D3. Fleet management** — ~15 actions, all currently inert on a client
because `Fleet.Tick` returns immediately (`Fleet.cs:882-887`) and the UI
reports success regardless. Create, split, disband, rename, stance settings,
advanced settings, formation style, home base, add/remove ship, apply-to-all.

These are **not** orders — they are edits to an object the host owns. They want
Stream C's command channel, not `FleetOrderCommandMessage`. Fleet *ids* still
never go on the wire (rule 4.1.2), so "create a fleet" must be expressed as
"these ships, in this formation" — the host builds the fleet.

**D4. Cargo** — transfer (`CargoTransferUnitController.cs:79`), trade
(`CargoTradeHelper.cs:130`), eject (`FleetCargoScreen.cs:93`,
`UnitCargoScreen.cs:75`), and the factory (`FactoryComponent.Tick`, inside
`UnitComponentHolder.Tick` behind `ShouldSimulateThisShip`, so a client-owned
factory produces locally and is never corrected).

Note the awkward one: `CollectCargo` **is** properly relayed, but the handover
then hands the ship to the host to fly, so the tractor runs on whichever copy
is simulating and there is no cargo in `UnitState` to reconcile them.

**D5. Refit** — `ComponentTradeScreen.cs:184`/`:196`. Structurally the hardest
non-combat item: a component is a `GameObject` created under the ship
(`ComponentBay.cs:201-208`), which **no state push can express**. Needs its own
message carrying the component list, or a component-state extension to
`FactionStateMessage`. (§8)

**D6. Build station** — `OrdersHelper.OrderBuildStation:192` is the one
`Order*` method of ten missing its `TrySend` guard, **and** `BuildStation` is
not in `FleetOrderKind`, so it cannot be relayed at all today. Also reachable via
`BuildModePlacementScreen.cs:202-216`.

**D7. Credits** — one-way push only. With B1 fixed and C in place, credits
become a *consequence* of relayed actions rather than something relayed itself.
`FactionStateMessage` is the receipt.

### Stream E — Combat *(broken in both directions)*

**E1. Client → host.** The client's ship ticks in full locally
(`Authority.OwnsUnit` is true for `IsPlayerCurrentUnit`), so it fires, and
`TurretComponent.Fire:148` has no `Authority` check anywhere in its path:

```
TurretGridUI.cs:36 → TurretComponent.cs:148 → ActiveTurret.cs:287
  → ActiveProjectileTurret.cs:48 → Projectile.OnHitTarget:234
  → DestructableUnit.ApplyDamage:134        ← the sink
```

The host *does* run `ApplyDamage` for every unit (`Authority.OwnsUnit`
short-circuits true on the host). **The host simply never receives the trigger.**

**Design decision, and it is a real one:** the client reports *damage it
applied* (`targetId, amount, attackerId`) and the host applies it
authoritatively; or the client reports *shots fired* and the host decides what
was hit. Damage-report is simpler, matches the co-op/no-anti-cheat rule, and is
consistent with everything else here — the client reports, the host decides. But
it means the host trusts the client's arithmetic, which is acceptable here and
worth writing down explicitly as accepted.

**E2. Host → client is also broken.** `UnitStateBroadcaster.cs:424-427`
explicitly *skips* the ship a player is flying ("They are simulating it;
sending them the host's copy would fight their own stick"). So when host AI
destroys the client's ship, the client is never told. Hull, shields, kills,
bounty payouts and kill messages diverge in **both** directions.

This is the subtlest item in the plan: the skip is *correct for position* and
*wrong for condition*. Splitting them is the fix.

**E3.** Needs B7: hull, shield, capacitor on the wire, host-authoritative, in
both directions.

**E4.** Knock-on world writes all follow from E1: `EngineASX.HandleUnitAttacked`
(`:1212`), `NotifyUnitKilled` → `Person.Kills++` (`:3085`), `ProcessBounties`
(`:3107`), `LocalPlayer.AddMessage` (`:3127`). And `TopPilots` ranks by kills
that were never relayed.

### Stream F — The remaining fleet orders

19 `FleetOrderKind` values, ids 1–19. **9 have no `TrySend` guard:**

`ProtectTarget`(10) `RepairAtNearest`(11) `RepairAt`(12) `RearmAtNearest`(13)
`RearmAt`(14) `TransportPassengers`(15) `MergeWithFleet`(16) `ManualTrade`(17)
`Patrol`(18)

**7 orders have no kind at all**, so cannot be relayed in principle:
Mine, BuildStation, Attack, SellCargo, WaitForAutoRepair, AutonomousTrade,
Undock.

`FollowTarget` maps to `MoveToSectorTarget`; no new kind needed.

**F3 — stage 3 cleanups** (from `order-system.md`):
- `OrderReturnToBase(NewOrderTarget, bool)` bypasses `CreateAndReturnFleets()`,
  sends nothing, and is discarded silently
- filter `null` out of `CreateAndReturnFleets`'s returned list
- delete the dead `NewOrderTarget.FleetContaining`

### Stream G — Client legibility

The client can give an order and watch its ship leave, with nothing in its
orders screen. It cannot see what the host is doing for it. Until it can, the
whole thing feels like the game ate their ship. This is the "doesn't display
the order" symptom, and it is what makes everything above *legible* rather than
merely functional.

### Stream H — Comms and diplomacy

**The shape is settled** — same as everything else:

```
client → CommsRequest(handlerUnitId, kind, optionIndex)   [id 15]
host   →  runs the real CommsHelper.OpenCommsWithHandler, generates the stage
host   →  CommsStage(handlerUnitId, messageText, optionLabels[])
client →  renders it; picking option N sends a request back
```

`CommsHelper.OpenCommsWithHandler` is **one choke point with four callers**
(`UnitPopupMenu:237`, `FactionsScreen:148`, `HudScreen:420`,
`UnitContextMenuScreen:395`) — a clean insertion place, no call-site changes.

**Why it must be relayed:** the lines are random.
`DynamicCommsProfileFinder.GetProfile(...).Messages.GetRandom()`. Two machines
talking to the same AI would say different things.

**It fixes a live bug:** `OpenCommsWithHandler` calls `GetOrCreateAttitude` and
`SyncNeutrality` today, so **a client pressing Comms writes attitudes into its
own world**, silently, right now.

**Player-to-player diplomacy** rides the same channel — alliance, war, intel
sharing are just options on the stage. The client sends an option index, so a
client **structurally cannot declare war**, which is your rule enforced a second
time. The rule's first enforcement is already done: `Faction.cs:2020` withholds
the automatic `UnderAttack` declaration between two player factions
(`TwoPlayersAreShootingAtEachOther`, committed `2ed2b55c`).

### Stream I — Surviving contact with a real community

P2P is mandatory, so this is about making the mandatory design work for people
who are not sitting next to the host.

- **NAT.** A fixed 24890–24897 range works on a LAN and behind manual port
  forwarding. It does **not** work behind CGNAT — not a rare edge case in a
  community this size. Without a backend: UPnP / NAT-PMP port mapping is
  entirely local and legitimate, and IPv6 has no NAT at all.
- **Failure diagnostics.** Today the only signal is
  `host 192.168.1.106:24890 is unreachable - connection refused - nothing is
  listening there (is the code out of date?)` — which is **a lie on most home
  networks**, where the refusal means a firewall, not an old build. Distinguish
  *nothing listening* / *firewall* / *wrong IP* / *unreachable carrier*.
- **Setup help** for a public release: what to forward, and to whom.

### Stream J — "We are not changing the game" audit

The auto-war suppression in `Faction.cs` (`2ed2b55c`) is the **first change
outside the multiplayer assembly**. It is guarded and inert singleplayer — the
band test is false for both sides when there is nobody else — but it is the
first of what will be several.

Standing check: inventory every seam referenced outside
`OpenFrontier.IP.Multiplayer` (`Authority.`, `PlayerFactions.`,
`ReservedIds.`, `IsPlayerMade`, `IsThisCopyAClient`) and confirm each is
**provably inert with no provider installed**. Currently 17 call sites across 9
files; the list grows with every stream.

---

## 6. The backlog

Every row is a player action that mutates the world. **Nothing here is
singleplayer-only by design except where marked.**

### Empire

| Action | Mutation | Relay | Status |
|---|---|---|---|
| Buy ship | `ShipBuyScreen.cs:289,359` | none | **BROKEN** |
| Sell ship | `SellShipScreen.cs:50` | none | **BROKEN** |
| Buy equipment / refit | `ComponentTradeScreen.cs:184,196` | none | **BROKEN** |
| Dock at station | `UnitComponentHolder.cs:1761` | none | **BROKEN** (not on wire) |
| Undock | `UnitComponentHolder.cs:1808` | none | **BROKEN** |
| Buy/sell cargo | `CargoTradeHelper.cs:130` | none | **BROKEN** |
| Cargo transfer ship↔ship | `CargoTransferUnitController.cs:79` | none | **BROKEN** |
| Cargo eject (fleet/unit) | `FleetCargoScreen.cs:93`, `UnitCargoScreen.cs:75` | none | **BROKEN** |
| Cargo factory | `FactoryComponent.Tick` | partial | **BROKEN** |
| Collect loose cargo | `Cargo.cs:143` | order only | **PARTIAL** |
| Build station (order) | `OrdersHelper.cs:192` | none | **BROKEN** (no kind) |
| Build station (placement) | `BuildModePlacementScreen.cs:202` | none | **BROKEN** |

### Fleet management

| Action | Mutation | Relay | Status |
|---|---|---|---|
| Create fleet | `PropertyScreen.cs:177` → `OrdersHelper.cs:190` | none | **BROKEN** (local object only) |
| Split fleet | `FleetUtils.cs:19` | none | **BROKEN** |
| Disband fleet | `OrdersHelper.cs:1281` | none | **BROKEN** |
| Add ship to fleet | `PropertyScreen.cs:229` | none | **BROKEN** |
| Remove ship from fleet | `PropertyUnitPopupMenu.cs:316` | none | **BROKEN** |
| Rename fleet | `FleetPopupMenu.cs:179` | none | **BROKEN** |
| Clear fleet orders | `FleetPopupMenu.cs:139` | none | **BROKEN** |
| Fleet stance settings | `StanceSettingsController.cs:130` | none | **BROKEN** |
| Fleet advanced settings | `AdvancedOrdersController.cs:184-373` | none | **BROKEN** |
| Formation style | `AdvancedOrdersController.cs:288` | none | **BROKEN** |
| Home base | `Fleet.cs:3166` | none | **BROKEN** |
| Request NPC crew | `OrdersHelper.cs:506` | order side-effect only | **PARTIAL** |
| Fleet orders (8 of 19) | `FleetOrderCommands.cs:806` | yes | **WORKS** |
| Fleet orders (9 of 19) | — | no `TrySend` | **BROKEN** |
| Fleet orders (no kind) | Mine, Attack, SellCargo, WaitForAutoRepair, AutonomousTrade, Undock | n/a | **BROKEN** |

### Combat

| Action | Mutation | Relay | Status |
|---|---|---|---|
| Fire weapons → damage | `Projectile.cs:240` → `DestructableUnit.cs:134` | none | **BROKEN** |
| Laser / point defence | `ActiveLaserTurret.cs:411` | none | **BROKEN** |
| Off-screen fire | `ProjectileTurretComponent.cs:353` | none | **BROKEN** |
| Damage *to* your ship | `UnitStateBroadcaster.cs:424` skips it | none | **BROKEN** |
| Kill credit / bounties / messages | `EngineASX.cs:3085,3107,3127` | none | **BROKEN** |
| Hull / shield / capacitor | — | not on wire | **BROKEN** |

### Faction and diplomacy

| Action | Mutation | Relay | Status |
|---|---|---|---|
| Rename faction | `FactionSettingsScreen.cs:168` | none | **BROKEN** |
| Faction policies/settings | `FactionSettingsScreen.cs:75,117,129,141` | none | **BROKEN** |
| Faction transactions screen | `FactionTransactionsScreen.cs:61` | read-only | **READ-ONLY** |
| Buy sector intel | `BuySectorIntelConfirmStage.cs:68` | none | **BROKEN** |
| Sector control (via station) | `Faction.OnSectorControlled:670` | none | **BROKEN** |
| Comms (all forms) | `CommsHelper.OpenCommsWithHandler` | none; **writes attitudes locally** | **BROKEN** |
| Alliance / declare war (players) | — | does not exist | **NOT BUILT** |
| Manual player comms | `TryGetManualCommsHandlerFromPerson` | none | **BROKEN** |

*There is no sector purchase or territory-claiming mechanic in this game.
`FactionTransactionType` has only `SectorIncome` and `StationBuild`, and
`Sector.ControllingFaction` is derived from built `SectorControl` stations with
no public setter.*

### Exploration, missions, intel

| Action | Mutation | Relay | Status |
|---|---|---|---|
| Sector travel | — | yes | **WORKS** |
| Universe map → fly to sector | `UniverseMapScreen.cs:434` | yes | **WORKS** |
| Autopilot exclusions | `UniverseMapScreen.cs:362,371` | none | **BROKEN** |
| Universe map waypoint | `UniverseMapScreen.cs:413` | none | **BROKEN** |
| Request taxi | `RequestTaxiScreen.cs:31` | none | **BROKEN** |
| Accept mission | `JobBoardScreen.cs:103` | none | **BROKEN** |
| Mission abort / complete / reward | `MissionScreen.cs:99,122` | none | **BROKEN** |
| In-game mail | `GamePlayer.cs:347,388` | none | **BROKEN** |
| Bounty screen | `BountyScreen.cs:85` **writes intel on open** | none | **BROKEN** |
| Passive scanning / intel | `FactionIntel.cs:840` | none | **BROKEN** |
| Ship scan | `ShipScanScreen.cs:84` | n/a | **READ-ONLY** |
| Sector map | `SectorMap.cs:422` | n/a | **READ-ONLY** |
| Top pilots | `TopPilotsScreen.cs:53` | n/a | **READ-ONLY** (over un-relayed data) |
| Hypersleep | `Time.timeScale` | client-local | harmless |

### Deliberately singleplayer

`MainMenu`, `LoadGame`, `GameMode`, `Options`, `HelpSystem`, `UIScale`,
`AdjustSafeArea`, `PurchasesStore`, `Skirmish` + `Battles`, `UniverseSandboxSettings`,
`UniverseGameType`, `UniverseSelect`, `Test*`.

Hypersleep is a `Time.timeScale` change and is harmless client-local — but
**god mode is not** (B5).

---

## 7. Verification

The harness at `/tmp/opencode/harness` is the fast check. It is in `/tmp` and
**a power cut wipes it** (`gen.py` and `check.sh` have to be rewritten; the
project is safe). It gives: per-asmdef compile against the editor's real DLLs,
monoscript-check, prefab-null-check, `asmdef-refs.py`, and codec tests linking
18 real source files via `sync.py`.

**A clean build is necessary and not sufficient.** The harness does not define
every symbol the editor defines. After the editor refocuses, check
`grep -c "error CS" Logs/Editor.log` — should be 0.

**In-game verification is manual and slow, and at this scale it does not
scale.** Two rules to keep it honest:

1. **Two comparable things on adjacent log lines.** A counter nobody can check
   is not a measurement. `DiagShip` found the never-activated-order bug precisely
   because it put the host's view of the fleet next to the player's view.
2. **Editor and build must both be rebuilt for any protocol change.** They are
   two players. Editor prefs and build prefs are separate slots in one shared
   prefs file; the tokens genuinely differ.

**What should become mechanical.** Every row in §6 should eventually be
answerable by a checker, not by reading logs: enumerate the registered command
handlers and assert every mutating UI action has an entry. Stream A3's
visible-refusal gate is what makes such a check possible — if an action cannot
run un-relayed, the list of unwired actions is the list of refusals, and it can
be printed on demand.

**Known harness blind spot:** it cannot catch a loop that never terminates, and
Unity asmdef references are not transitive — `asmdef-refs.py` covers the latter,
but a clean harness build does **not** prove the editor will compile. Both have
bitten before.

---

## 8. Open questions

These need your call; several change the shape of a stream.

1. **A3 — silent divergence vs visible refusal.** Should every unwired action
   visibly refuse until wired? *Recommendation: yes.* It is the difference
   between a player who knows and a player who is quietly wrong, and it is
   what makes the rest of this plan verifiable in waves.
2. **D1 — custom variants and upgraded ships.** A freshly bought ship is
   reproducible from `unitClassId` alone because its modules are stock. A
   *refitted* ship is not, and a custom variant is not. Do we accept a first
   cut that relays purchases of stock ships only and refuses the rest, or does
   `NewUnit` need to carry a full component list from the start?
3. **D5 — refit transport.** A component is a `GameObject`. Either `NewUnit` and
   a component message carry an explicit component list, or refit needs a
   general "ship definition" message. Related to (2).
4. **E1 — combat trust.** Damage-report (client says what it hit; host applies)
   is simpler and matches the co-op rule, but the host trusts the client's
   arithmetic. Shot-report (host decides what was hit) is authoritative and more
   work. *Recommendation: damage-report, and write down that it is accepted, so
   nobody re-litigates it later.*
5. **Stream I — NAT.** Is UPnP/NAT-PMP acceptable? It is entirely local, needs
   no backend, and is the difference between "works for people who can forward
   a port" and "works for most of the community". CGNAT users are unreachable
   without it or IPv6.
6. **H — player comms scope.** May a client open comms with another *player's*
   faction, or only with AI? The reserved-band test already answers "which is
   which" for free, so this is cheap either way — but it is your call.

---

## 9. Suggested execution order

```
A  honest failure          (small; unblocks all diagnosis)
B  infrastructure (8)      (cheap; makes every later feature safe)
C  command channel         (the general answer; unblocks D/E)
D  empire                  (the chain you said matters — build, dock, fleets, cargo)
E  combat                  (in both directions)
F  remaining fleet orders  (9 guards + 7 new kinds)
G  client legibility       (makes it legible)
H  comms + diplomacy
I  public release (NAT, diagnostics, help)
J  "not changing the game" audit (continuous)
```

A, B and C are prerequisites for doing D–H safely in waves rather than one at
a time. Each stream should end with something checkable in a log.

**Next concrete step:** Stream A (A1 + A2 + a decision on A3), then a rebuild on
both sides to confirm the log stopped lying before anything else is built on it.