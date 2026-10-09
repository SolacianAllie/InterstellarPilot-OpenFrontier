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

"Connected" in the status column means a session with that host is live *now*.
It is not a ping: with no server there is nothing to ask, and reporting a host
as online without having checked would be worse than saying nothing.