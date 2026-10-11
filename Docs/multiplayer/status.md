# Open Frontier Multiplayer — Status

**Written:** 11 October 2026
**Branch state:** local commits only, nothing pushed. 541 commits.
**Build:** offline harness clean (one .NET project per asmdef, real Unity DLL
references, codec tests). The editor has not been recompiled since the last
change, and the last in-flight work is not yet verified in play.

---

## 1. The design, in one paragraph

Exactly one simulation runs: **the host's**. A client owns precisely one thing
— the ship it is currently flying — and streams where it got to up to the host
at 10 Hz. Everything else is the host's: every credit, every cargo item, every
station's stock, every order, every weapon discharge. A client asks; the host
decides; the host relays the result back to everybody.

This is not a limitation. It is the only arrangement in which one hull is in one
place and two copies of a transaction cannot disagree about what a player owns.

**The measure of success, unchanged:** *a player who never opens the multiplayer
menu cannot tell that any of this happened.*

**The replication rule, as decided:** *the host should tell clients everything
it can, so that remote players see the universe the same way the host does, but
interact with the universe they see, by asking the host.*

---

## 2. What works, verified by playing it

| | |
|---|---|
| Join by session code, no backend | working |
| World snapshot at join; final world on host departure | working |
| Flying your own ship — one ship simulated per copy, streamed both ways at 10 Hz | working |
| Ownership handover — claim on manual fly, host takes it for orders, returns on completion | working |
| **Sector travel** between sectors | working |
| **Fleet orders — 8 of 19 kinds** | working |
| **Docking and undocking**, host-authoritative, correct hangar *and* bay on every copy | working |
| Faction and ship id bands; "is this a player's?" decided with no message | working |
| Sensor/radar refresh runs before the authority gate, so contacts are right | working |
| Cancel an order, which is what returns the ship | working |

Fleet orders working: move-to-sector, move-to-sector-target, dock-at-target,
return-to-base, enter-wormhole, claim-unit, collect-cargo, scavenge,
explore-sector, stop.

---

## 3. How the pieces fit

**The relay pattern** — one shape, used for every action:

```
CLIENT                                  HOST
OrdersHelper.OrderDockAtTarget(fleet,…)  FleetOrderCommands.RunFor(player, body)
  └─ if (Authority.IsThisCopyAClient)      └─ validate ownership
       └─ TrySend(intent)                   └─ OrdersHelper.OrderDockAtTarget(…)
            └─ return; do nothing                ← the SAME helper, host's copy
                                               └─ AssignNextOrderIfNoCurrentOrder
                                            └─ broadcast the result
```

Three rules the shape encodes:

1. **The same helper runs on both sides.** The host does not reimplement what a
   click meant. The two copies cannot disagree about what "dock" means.
2. **Ships are named; fleet ids never travel.** A client may build a fleet in
   its own world and the host has never issued that id, so an action about a
   fleet names the ships in it.
3. **The host announces a handover after the order ran**, never before. A client
   that let go on *send* would be stranded if the order were refused.

**The command channel** — one envelope for the ~40 player actions still to come,
so adding an action stops being a protocol change. Type safety moves to a named
handler per command rather than living in a decode switch.

**The seams.** `Authority` (may this copy decide?), `AuthorityGate` (who may move
a ship?), `PlayerIdScheme` (reserved id bands), `Authority.Refused` (say no,
loudly, instead of diverging quietly). The game assembly cannot reference the
multiplayer one and that dependency must not exist, so every crossing is a seam
the multiplayer side fills.

**The protocol** — 17 message ids, 7 travelling client → host.

---

## 4. Progress

Measured against the 59-row backlog in `Docs/multiplayer/coverage-plan.md`:

| Status | Count |
|---|---|
| **WORKS** | 4 |
| **PARTIAL** | 3 |
| **BROKEN** | 45 |
| **NOT BUILT** | 1 |
| READ-ONLY (safe) | 5 |

**Roughly 12% of the game's multiplayer surface is working.** That is the honest
number, and it understates what has been built because several of the "works"
rows unblock many more.

**What has actually landed this session**, in order of how much it unblocks:

- **The log stopped lying.** The host reported every order as run, including the
  ones it had just refused — success printed directly under the warning saying
  it was refused. That is why a whole test session could report that exactly one
  order had ever been given when several had arrived and been turned away.
- **Three foundation bugs.** `FactionStateApplier` was written and never fitted
  to anything; the host has been pushing credits into a void. `MayBeClaimed`
  permitted claims on ships the host had never heard of. God mode was live on
  every joiner, because "not hosting" was being read as "alone".
- **Clients were simulating a galaxy economy.** `Authority.OwnsFaction` was
  written to answer exactly that and asked by nothing, so every client ran its
  own faction AI — expansion, station upgrades, spending credits nobody would
  ever see.
- **A refusal gate.** `Authority.Refused` says no in words instead of quietly
  changing a world nobody else can see. Four chokepoints: all 36 credit
  movements, all damage, docking, ship purchase.
- **Docking and undocking**, relayed properly — host-authoritative, with the
  specific hangar bay named so both copies agree.
- **A general command channel** with codec-tested messages.
- **Ship creation replication** — the first message that can bring something
  into existence, hooked at `EngineASX.RegisterUnit`, the one place every unit
  passes through.
- **Real credits shown for a player's faction**, so the sync can be seen rather
  than inferred.

---

## 5. In flight — compiles, does nothing yet

Two things are half-built and must not be mistaken for working:

**Ship removal replication.** `UnitGoneMessage` exists, `Unit.SafeDestroy`
raises an event, the broadcaster detects departures and broadcasts them — but
**the client applier does not handle the message yet**, so a client receiving
"gone" currently does nothing. No codec test either.

**Killed versus removed.** Not started. The question was whether a unit's
disappearance can be told apart, and the answer is that it cannot *from the
removal alone* — every path converges on `SafeDestroy()`, which takes no reason.
But each path has its own named entry point, so it can:

| what happened | path | animation wanted |
|---|---|---|
| death | `DestructableUnit.KillUnitAndChildren` | **yes** |
| ditching | `KillAnonymously` | **yes** |
| selling | `SellShipScreen` → `SafeDestroy` | no |
| dismantling | `CompleteDismantling` → `SafeDestroy` | no |
| cargo expiring | `Cargo.UpdateExpiry` → `SafeDestroy` | no |

So the message needs a kind byte, the death must be broadcast **at kill time**
rather than at removal (otherwise the client's explosion plays a full animation
length late), and a **sold ship must not explode**. The client should run the
game's own destruction on its copy — the same rule the fleet orders follow —
so the explosion, debris and sound come out of the game's own code.

---

## 6. What is left

### The empire chain — cut at the first link

You own a small empire and order it around. That chain is:

| link | state |
|---|---|
| dock at a station | **works** |
| **build a ship** | **refused on a client** — no message could create an object until this session |
| form a fleet | client-side view only; ~15 management actions do nothing |
| order them | 8 of 19 |
| fight | broken in both directions |

**Buying a ship is the next piece**, and it is nearly free now: the host already
has the create message and the client already knows how to build the ship. What
is missing is the *request* — a command the client sends instead of the local
purchase. Host charges the credits, constructs, broadcasts; every copy builds
the same ship from the same prefab.

### Combat — broken in both directions

`UnitState` carries position, rotation, velocity, turn and throttle. **No hull,
no shield, no capacitor** — grep across the whole multiplayer assembly for those
words returns nothing. So:

- A client fires. Its own copy shows damage. The host never hears, streams the
  target back at full health ten times a second, and the damage un-does itself.
- The host's broadcaster **deliberately skips the ship you are flying** — correct
  for position, wrong for condition. So when host AI kills your ship, you are
  never told.

Kill credit, bounties and kill messages all diverge. `TopPilots` ranks by kills
that were never replicated.

### Everything else

- **The eleven unwired orders** — Protect, Repair ×2, Rearm ×2, Transport
  Passengers, Merge, Manual Trade, Patrol. Plus seven orders with no
  `FleetOrderKind` at all: Mine, Build Station, Attack, Sell Cargo, Wait For
  Auto Repair, Autonomous Trade, Undock.
- **Cargo and economy** — transfer, trade, eject, the factory, refit. (Refit is
  the hard one: a component is a `GameObject`, which no state push can express.)
- **Fleet management** — create, split, disband, rename, settings, formation,
  home base, add/remove ship.
- **Reproduction gaps** — AI ships appearing and disappearing; faction credits
  for *all* factions rather than only your own; attitudes and opinions; intel.
- **Missions, mail, bounties, intel purchases, taxi.**
- **Comms** — the lines are random (`Messages.GetRandom()`), so they cannot be
  generated on a client at all. One choke point, four callers. Fixes a live bug:
  a client pressing Comms writes attitudes into its own world today.
- **Player-to-player diplomacy** — alliance, war, intel sharing. The
  auto-war rule is already enforced (`Faction.cs`): between two player factions,
  enough damage does **not** declare war. Hostily says so.
- **Public release viability** — a fixed 24890–24897 range works on a LAN and
  behind manual port forwarding. It does not work behind CGNAT, which is not a
  rare case in a community this size. UPnP/NAT-PMP needs no backend, and the
  current failure message (`connection refused - nothing is listening there`) is
  a lie on most home networks, where it means a firewall.
- **"We are not changing the game" audit.** The auto-war suppression is the first
  change outside the multiplayer assembly. It is guarded and inert singleplayer,
  but it should be the first of several, not the last.

---

## 7. How it gets verified

The harness (`/tmp/opencode/harness`) is the fast check: one project per asmdef
against the editor's own DLLs, monoscript and prefab checks, and codec tests
that link the **real** source files — so a test cannot pass against a stale copy.
It is in `/tmp`, and a power cut wipes it.

A clean build is necessary and not sufficient. The editor's compile differs,
because the harness does not define every symbol the editor defines.

**In-game verification is manual, and at this scale it does not scale.** Two
discipline rules that have actually caught things:

1. **Two comparable things on adjacent log lines.** A counter nobody can check
   is not a measurement. The never-activated-order bug was found by one line
   reading `fleet order NONE ACTIVE, queued 1`.
2. **Editor and build must both be rebuilt for any protocol change.** They are
   two players.

There is also a limit worth recording: the harness **cannot** catch a loop that
never terminates, and it cannot see a script reaching for a type from an assembly
it does not reference.

---

## 8. Suggested order

```
0  honest failure          DONE
1  infrastructure (5 of 8) DONE
2  command channel         STARTED
3  finish ship removal + killed-vs-removed
4  buy a ship              ← your empire chain, link 1
5  combat, both directions
6  the eleven orders, three tranches
7  faction credits for all factions; then attitudes
8  client legibility
9  comms + player diplomacy
10 public release (NAT, diagnostics, help)
11 "we are not changing the game" audit
```

Steps 3–5 are the shortest path to the thing the game *is*: an empire you can
build and order. Steps 7–9 are what make it a shared universe rather than two
copies.

---

## 9. Mistakes worth recording

Three this session, all the same shape — **a reason generated and then dropped
before it reaches a log**, or a fix put in the wrong layer:

1. **Refusals went into primitives.** `TryDockInHangar` is also called by
   `NpcPilot`, `Fleet`, `FleetSpawn` and the world seeders, so a client could not
   load a world whose ships were saved docked. The game failed to load.
2. **The undock relay went on one of two buttons.** `DockUI` has `UndockShip`
   and `UndockCurrentShip`; the seam was on one.
3. **`FactionStateApplier` was marked done when it was still dead.** Installing
   it fixed nothing until the subscription was moved from the session to the
   service — because an applier added at `BeforeSceneLoad` is added before any
   session exists.

The lesson now written into the work: **find every caller before touching a
path, not the one that seems obvious.**

---

*Plans and the full 59-row backlog: `Docs/multiplayer/` in the project, committed
so a power cut cannot take them.*