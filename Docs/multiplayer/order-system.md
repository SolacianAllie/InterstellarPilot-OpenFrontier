# The order system: what it is, and how to make it work for a client

## The headline

`OrdersHelper.CreateAndInitPlayerFleet` returns `null` **unconditionally** — the
`return null;` sits outside the `if (Authority.IsThisCopyAClient)`. The game's
only fleet-creation path is broken for every machine.

The host does not notice because their own fleet is already in the save, so
they never call it. A joining player has no fleet, calls it, gets null, and
every screen built around a fleet — orders, cargo, settings — is dead. That is
why this presented as a remote-client bug and not a universal one.

This is my regression, from the commit that stopped clients building fleets.
It is also the whole reason orders do not work.

---

## What the system actually is

Three layers. The failure is spread across all three.

### 1. Selection — `NewOrderTarget`

`NewOrderTarget` is a plain class holding a UI selection in four buckets:
`orderedUnits`, `orderedFleets`, `unitsWithoutFleet`, plus derived flags
(`IsValid`, `AllUnitsMobile`, `AnyUnitArmed`, …) computed by
`CompileAllOrderedUnits()`.

Built by three factories: `CreateFromUnits`, `CreateFromFleets(Fleet)`,
`CreateFromFleets(IEnumerable<Fleet>)`. The ship-menu path uses
`CreateFromFleets`, so **`orderedUnits` is empty** on that path — which
matters, because the fleet-less bucket is empty too.

Every order is then:

```csharp
foreach (Fleet item in orderTarget.CreateAndReturnFleets())
    OrderX(item, ...);
```

`CreateAndReturnFleets()` (`NewOrderTarget.cs:290`) returns `orderedFleets`,
and for fleet-less ships calls `FindOrCreateNpcAndFleetAndPilotShip` — which
is the null-returning path.

### 2. Request — `FleetOrderIntent`

The eight wired orders call, in their `Fleet` overload:

```csharp
if (FleetOrderIntents.TrySend(Intent(FleetOrderKind.DockAtTarget, orderedFleet,
    targetUnit: Id(targetUnit), stack: stack)))
    return;
```

`Intent()` carries `FleetIds = OneFleet(fleet)` and
`LooseUnitIds = FleetOrderIntents.TakeLooseUnits()`. `OneFleet` returns empty
on a client, so the wire carries ships, never fleet ids. That rule is right
and should stay.

`FleetOrderIntents.TrySend` returns **false** when no interceptor is installed,
which is what leaves singleplayer byte-for-byte unchanged.

### 3. Execution — `Fleet`

`Fleet` is a `SectorObject` (a MonoBehaviour). Its own `Update()`:

```csharp
private void Update()
{
    if (IsInActiveSector)
        Tick();                      // → activeOrder.Tick(elapsedTime)
}
```

**This path has no authority gate.** The gate that stops ships simulating on a
client lives in `UnitComponentHolder.Tick()`. Fleet orders tick somewhere else
entirely. A client that builds an order would *run* it.

An order becomes active by exactly three routes:
1. `SetFleetOrder(..., stack: false)` → `AssignNextOrder()` immediately
2. `NewFleetOrderScreen.OrderButton_Ordered` → `AssignNextOrderIfNoCurrentOrder()`
3. `Fleet.UpdateIdleFleet()` → `AssignNextOrder()` (the backstop)

`ActiveFleetOrder` is a **component added to the same GameObject** as its
`FleetOrder`, created by `createActiveFleetOrder()`.

---

## The gaps, in the order they bite

### A. `CreateAndInitPlayerFleet` returns null for everyone — **the blocker**

Restoring the original body is the fix. The "clients must not build fleets"
rule was the wrong cure for a right disease: it should have been a rule about
what goes **on the wire**, not about what may be built locally. `OneFleet`
already enforces that. Keeping both meant the world stopped disagreeing at the
price of the game not working.

*Verify while there:* the original body calls
`EngineASX.OnPlayerFleetCreated(fleet)` and `ApplyDefaultSettingsToFleet`
when `faction.IsPlayerFaction`. True on a client. Need to confirm neither
saves, charges, or notifies — or that doing so locally is harmless because
the client never saves. Check before restoring.

### B. `Fleet.Update()` is ungated — **the safety blocker**

Without this, nothing below is safe. Gate `Fleet.Tick()` on
`!Authority.IsThisCopyAClient`, alongside the other guards, feeding
`Authority.Declined()` so it shows in the existing per-world report.

Gate on *is-a-client*, not on per-ship ownership. The rule is "no fleet
simulates on a client", which is the stated model: the host manages
everything simulation-wise. Per-ship ownership would let the player's own
fleet run orders locally and fight the stick.

This does not touch the autopilot handoff, which is a different path: claim
while flying manually, release on autopilot, host simulates, re-claim on
manual resumption.

### C. Only 8 of 19 order kinds are wired — **the big gap**

`TrySend` exists in eight `Fleet` overloads. The other eleven apply **locally
on the client and are never sent**, with no log line. They are:

> Protect · Repair At · Rearm · Repair At Nearest · Rearm At Nearest ·
> Merge · Manual Trade · Patrol · Undock · Explore · Attack

`OrderStop(Unit)` also never calls `TrySend`, though `FleetOrderKind.Stop`
exists and the host already handles it.

Six more `OrderX` methods have **no enum value at all** and need one appended
(new values only — the kind is a wire byte, so existing numbers must not
move): Mine, Attack, Sell Cargo, Wait For Auto Repair, Autonomous Trade,
and Follow Target (which can map onto `MoveToSectorTarget`).

Fixing this is mechanical and mostly one shape per method:

```csharp
if (FleetOrderIntents.TrySend(Intent(FleetOrderKind.X, orderedFleet, ...)))
    return;
```

The host already has `case` arms for most of the nineteen.

### D. Three specific bugs

**`OrderReturnToBase(NewOrderTarget, bool)`** loops `newOrderTarget.OrderedFleets`
directly, bypassing `CreateAndReturnFleets()`. On a client those ids are
dropped by `OneFleet`, so it sends an order naming no fleet — and
`SendIfClient` reads that as *"the player selected nothing"* and discards it
**in silence**. This one is worth calling out: a reason generated and then
dropped before it reaches a log.

**`CreateAndReturnFleets` can put `null` in the returned list.** When
`FindOrCreateNpcAndFleetAndPilotShip` returns null (today: always), the
unwired orders dereference it before `TrySend` — `OrderAttackUnit` at
`fleet.Faction.IsHostileTo`, and `SetFleetOrder` at `fleet.ClearOrders()`.
NRE inside a button press, swallowed by Unity, indistinguishable from a dead
button. Fixed by (A); worth a filter anyway.

**`NewOrderTarget.FleetContaining`** is dead code. Delete it.

### E. The client cannot see its own orders — the relay

The host applies the order. Nothing comes back. The client's orders screen
shows an empty queue even when orders work.

Two options, and this is the one real fork in the design:

**E1 — Optimistic local order.** The client builds the same `FleetOrder` and
enqueues it locally. It will never execute once (B) lands.

The hazard: promoting it to `ActiveOrder` locally fires `ActiveOrder`'s
setter, which calls `NpcPilotsControlShips()` and rewrites
`pilot.CurrentUnit.Components.PilotPerson` — mutating a component on the
player's own ship, underneath the flight model. So: **enqueue locally, never
`AssignNextOrder`**. The queue shows the pending order; the host's relay
promotes it. Gets the UX for free and converges because both sides run the
same code on the same ships.

**E2 — Host relay only.** Host streams the player's fleet order state back;
client renders it. No divergence, no local mutation at all, but every order
costs a round trip and needs a serialisable DTO per order type
(orderId, orderType, targetUnitId/targetSectorId, completionMode, queue
position).

E1-then-E2 is the natural end state: instant feedback, then truth. E1 alone
is shippable; E2 alone is correct but laggy.

---

## Execution order

| # | Stage | Why here |
|---|-------|----------|
| 0 | Restore `CreateAndInitPlayerFleet` | Un-blocks the four dead buttons. Nothing else can be tested until it lands. |
| 1 | Gate `Fleet.Update()` on clients | Safety. Makes every later stage inert-by-construction. |
| 2 | Wire the 11 missing `TrySend` calls; append missing enum values | Turns "most orders do nothing" into "all orders are requests". |
| 3 | Fix RTB bypass, null-fleet-in-list, dead code | Small, and each one is a silent failure. |
| 4 | E1 optimistic enqueue (never `AssignNextOrder`) | Makes orders *visible*. |
| 5 | E2 host relay of order state | Makes them *true*. Handles refusal. |
| 6 | Order refused/accepted feedback to the client | Closes the loop; today the client cannot tell a refusal from a dropped connection. |

Stage 0 and 1 together are small and unblock everything. 2 is the bulk and is
mechanical. 4–6 are the design work.

## What to watch on the first run after stage 1

```
Multiplayer: <world> is CLIENT, N ship update(s) declined
```
`N` should rise, because fleets are now declining too. That is the check that
(B) took effect — a counter that does not move is not a measurement.

## Not in scope here

Weapons and combat events. They are event-shaped rather than state-shaped,
they need a clock the two machines agree on, and they should be built after
this settles. The same gate that stops fleets ticking is the gate a weapon
needs, so stage 1 is not wasted work.