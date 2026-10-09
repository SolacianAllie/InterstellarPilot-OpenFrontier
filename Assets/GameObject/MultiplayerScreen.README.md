# Wiring MultiplayerScreen.prefab

`Assets/GameObject/MultiplayerScreen.prefab` is a clone of `LoadGameScreen.prefab`,
adapted to list saved multiplayer hosts instead of saved games. The list, the
selection handling, the scroll behaviour, the rename flow and the delete
confirmation all come from `LoadGameScreen`'s own machinery — only the item
type and what the primary button does are different.

## What still needs doing in the editor

The prefab currently carries `LoadGameScreen`, not `MultiplayerScreen`. Swap the
component, then wire the fields.

### 1. Replace the script

On the prefab root, remove `LoadGameScreen` and add
`OpenFrontier.IP.Multiplayer.UI.MultiplayerScreen` (`MultiplayerScreen.cs`).

### 2. Swap the list scripts

| Object | From | To |
|---|---|---|
| `LoadGameList` | `LoadGameItemsUI` | `SavedHostItemsUI` |
| the row prefab inside `ScrollRectGameList` | `LoadGameItemUI` | `SavedHostItemUI` |

`SavedHostItemUI` needs three labels wired, matching the fields on
`LoadGameItemUI`:

- `NameLabel` — the host's name (was `ScenarioNameLabel`)
- `StatusLabel` — "Connected" / "Not connected" (new)
- `OfflineIndicator` — dimmed when not connected (was `DisabledIndicator`)

### 3. Wire `MultiplayerScreen`'s fields

The clone's old `LoadGameScreen` references map across like this. The file IDs
are from the current clone, so these can be checked against the inspector:

| `MultiplayerScreen` field | Take from | Current fileID |
|---|---|---|
| `ItemsList` | old `ItemsList` | `114633296557590524` |
| `JoinButton` | old `LoadGameButton` | `114426252596376205` |
| `AddHostButton` | old `ExportFileButton` (reuse the object) | `114734143864522482` |
| `RenameHostButton` | old `RenameFileButton` | `114536148047902537` |
| `DeleteHostButton` | old `DeleteGameConfirmButton` | `114667785438668446` |
| `HostInfoRoot` | old `GameInfoRoot` | `1371939378208658` |
| `MultipleItemsSelectedRoot` | old `MultipleItemsSelectedRoot` | `224739268104783211` |
| `HostNameLabel` | old `SaveGameNameLabel` | `114965908813272002` |
| `HostCodeLabel` | old `SaveGameFilename` | `114869737159367836` |
| `HostAddedLabel` | old `SaveGameDateLabel` | `114232415838589296` |
| `HostStatusLabel` | new — any spare text label in the info panel | — |
| `TitleLabel` | the screen's own title text | — |

### 4. Delete the import button

There is no file to import — a host is reached over the network, not from
disk. Remove the old `ImportFileButton` object (`114299931057912216`) from the
hierarchy and the field goes with it.

Import and export are gone; rename and delete stay, because a list of hosts
the player has joined before needs both.

### 5. Hide the save-specific detail labels

`ScenarioAuthorLabel`, `ScenarioAuthoringToolLabel`, `SaveGameCreditsLabel`,
`SaveGameNetWorthLabel`, `SaveGamePilotLabel`, `SaveGameFactionLabel`,
`SaveGameVersion`, `SaveGameCreatedVersion`, `SaveGamePermadeathLabel`,
`SaveGameScenarioDateLabel`, `SaveGameSectorLabel` and
`IncompatibleSaveIndicator` all describe a save file and have no meaning for a
host. Deactivate them (or delete them) inside `HostInfoRoot` so the panel shows
only name, code, when it was added, and connection state.

## Why the screen looks the way it does

The keyboard entry is `RenameUnitScreen`, the same screen used for renaming a
save file or a sector. That means the Add Host prompt behaves exactly like
every other text entry in the game — same validation, same OK/Cancel, same
mobile keyboard handling — instead of being a one-off prompt.

The version check happens when a host is joined, not when it is added. A host
on a different build is saved like any other and refused with an explanation
at join time, so a stale entry stays visible and explains itself rather than
being silently discarded.

## Online status

Each saved host is probed on the same port a real join would use. A probe is a
single short connection carrying one small message; the host answers it without
a handshake and hangs up. It creates no session and consumes no player slot, so
refreshing the list is harmless.

A reply proves something specific: the host process is running, is in a game,
and has multiplayer listening right now. That is exactly the question worth
asking before a player tries to join.

The status column reads:

| Shown | Meaning |
|---|---|
| `Checking...` | A probe is in flight. Shown rather than `Offline`, which would be a claim not yet checked. |
| `Online` | Reached, and accepting players. Appends `- N connected` when others are in. |
| `Online - not accepting players` | Reached, but the host has stopped admitting players. |
| `Different version (x.y.z)` | Reached, but running another build. Reports their version. |
| `Offline` | Nothing answered. Either not hosting, offline, or a network in between blocked it — indistinguishable from here. |
| `Connected` | This player is in that session right now. Takes precedence over the above. |

Probes re-run every 15 seconds while the screen is open, in parallel so a list
of unreachable hosts still settles within one timeout rather than one per host.

`Offline` deliberately does not distinguish "host closed the game" from "a
router blocked it". With no server there is nothing to ask, and a more
specific-sounding guess would send players after the wrong problem.