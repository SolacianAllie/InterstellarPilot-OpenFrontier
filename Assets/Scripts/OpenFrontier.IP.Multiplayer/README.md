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
| The joiner's ship state client → host, accepted as authority | ✅ |
| Ownership arbitration: one ship, one player | ✅ |
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

### 1. Faction state, pushed host → client

Credits (`Faction.Credits`, an int) and cargo (the `Cargo` list on a unit's
cargo bay) are the two that matter first. Without these a client sees a
price it already paid, and a balance it has already spent.

Push them whenever they change, not on a timer: a host that tells a client
its balance changed is correct immediately, and one that polls is wrong for
up to the poll interval after every purchase.

Size: small. One message, one applier, one trigger.

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

- **The joiner's shuttle has no components.** It is created outside the
  normal unit flow, so the component setup that a normally-spawned ship gets
  is not reached.
- **Ownership is keyed on player token, not faction**, which is what makes
  two players sharing one faction a change to one test rather than a
  rewrite. The host-ships-are-not-claimable check is the one place that
  assumes otherwise.
- **Only the host's active sector is broadcast.** Sector changes are not
  synchronised at all.
- **No interpolation for the joiner's own ship**, only for everyone else's;
  it is extrapolated from the host instead.