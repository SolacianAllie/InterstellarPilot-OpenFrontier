# Multiplayer: how authority works, and what is left to build

Written at the end of the first working session, while the design was fresh
and the remaining work was still honestly sized.

## The rule

**The host owns the world. A client owns exactly one thing: the ship it is
flying.**

Everything else — every credit, every cargo item, every station's stock,
every order, every weapon discharge — is the host's, and a client is told
about it rather than deciding it.

This is not a limitation. It is the only arrangement in which one hull is in
one place and two copies of a transaction cannot disagree about what a
player owns.

## What works today

| | |
|---|---|
| Join by code, no backend | ✅ |
| Session secret, exact version gate | ✅ |
| Host exports the world; client loads it as a scenario | ✅ |
| A faction and a shuttle made for each joiner, on the host | ✅ |
| Joiner adopts them and is placed in the shuttle | ✅ |
| Unit transforms host → client at 10 Hz, interpolated between updates | ✅ |
| Host's clock on every batch, so clients draw on its timeline | ✅ |
| The joiner's ship state client → host, accepted as authority | ✅ |
| Ownership arbitration: one ship, one player | ✅ |
| Credits and the ship's hold, host → client on change | ✅ |
| Quit, host-left and lost-connection all reported distinctly | ✅ |
| Saved-host slots showing latency, scenario, players, uptime | ✅ |

## How the pieces fit

```
        HOST                                    CLIENT
  ┌───────────────┐                       ┌────────────────┐
  │ simulates all │                       │ simulates one  │
  │ ships         │◄── ship state (20Hz)──│ ship           │
  │               │                       │                │
  │ owns all      │──── world snapshot ──►│ holds every    │
  │ faction state │                       │ other ship     │
  │               │──── unit transforms ──► still           │
  │               │──── faction state ────► (NOT BUILT)    │
  │ executes      │◄── commands ──────────│ (NOT BUILT)    │
  │ commands      │                       │                │
  └───────────────┘                       └────────────────┘
```

The two boxes marked NOT BUILT are the whole of the remaining work, and
they are the same problem seen from two directions.

## What is left, and how to build it

### 1. Faction state, pushed host → client — DONE

`FactionStateMessage` carries the faction id, its credits, and the contents
of the player's current ship's hold. `FactionStatePusher` sends it per player
whenever any of those differ from what that player was last told;
`FactionStateApplier` puts it on the client.

Two decisions worth keeping:

- **Compared, not event-driven.** A few integer comparisons per player per
  frame, rather than subscribing to change events that one code path will
  eventually not raise. A missed event is silent: the client is wrong and
  nothing says why.
- **Rebuilt, not adjusted.** Cargo is emptied and refilled rather than moved
  toward the target. A delta compounds - one missed message leaves the client
  permanently out by one purchase, and no later message can say which one.

The faction id is derived from the player's token rather than stored, which is
the reason a player's faction number is a hash of who they are. The ship is
found through the ownership registry.

### 2. Commands, client → host

The client never mutates anything. It asks.

```
Command { id, args... }
   → host executes the SAME method the local UI would have called
   → the resulting faction-state push is the client's confirmation
```

That last part is the design that makes this tractable: **the client does
not need its own result messages.** If the client sent a buy and the host
deducted the credits and changed the cargo, the push *is* the answer. One
channel instead of a request/response pair per action.

**The trap to avoid:** patching each screen individually. Buying, selling,
refuelling, repairing, shipyard, refinery, bounty board, comms, cargo
operations, station services — each one becomes a place to get authority
wrong, and every one of them has to be re-audited whenever the next is
added. Route them all through one channel and a new action costs an adapter
rather than an audit.

### 3. Events, host → client

We replicate *state*. Combat is *events*: a shot was fired, a hit landed, a
countermeasure popped.

This is the hardest of the three and it is a different shape of problem.
An event that fires at a slightly different moment on each machine produces
two players watching the same fight differently, which is worse than no
combat at all.

The infrastructure it needs already exists — ids that agree, ordering on a
channel, and a host that owns the truth. What it does not yet have is a
clock the two machines agree on, which is the thing that makes events
either fire together or not fire.

Size: large. Do it after 1 and 2, and do it when there is time to think
rather than when there is time left.

## Known gaps

- **The joiner's shuttle may be incomplete.** It is created outside the
  normal unit flow, so it may not get the component setup a normally-spawned
  ship gets. `UnitDiagnostics.Describe` prints the joiner's ship next to a
  known-good one — the host's own ship, or the ship the joiner was already
  flying — under the `DiagS` marker. Read against each other, a difference
  names itself.
- **Ownership is keyed on player token, not faction**, which is what makes
  two players sharing one faction a change to one test rather than a
  rewrite. The host-ships-are-not-claimable check is the one place that
  assumes otherwise.
- **A player owning more than one ship** is answered by lowest-id-wins in
  `FactionStatePusher`. A client flies one ship at a time; the rest are
  scenery to that message.
- **Sector changes are not synchronised.** Every sector's ships are
  broadcast, but *which* sector a player is in is not sent — the client
  arrives in the host's active sector and stays there.
- **No interpolation for the joiner's own ship**, only for everyone else's —
  it is simulated here instead, because a control that waits for a round trip
  is not a control. Its own input is uploaded to the host.
- **Interpolation draws a fixed 0.22 s behind the host.** Nothing corrects for
  a stalled connection, so a client that stops receiving holds its ships
  where they were rather than letting them run on. Extrapolating would look
  better and be wrong within a second whenever a packet was merely late.