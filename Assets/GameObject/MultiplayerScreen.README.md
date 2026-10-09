# Wiring MultiplayerScreen.prefab

`Assets/GameObject/MultiplayerScreen.prefab` is a clone of `LoadGameScreen.prefab`,
adapted to list saved multiplayer hosts instead of saved games. The list, the
selection handling, the scroll behaviour, the rename flow and the delete
confirmation all come from `LoadGameScreen`'s own machinery — only the item
type and what the primary button does are different.

## What still needs doing in the editor

One thing: **run `Tools > Multiplayer > Transfer Screen References`.**

It moves every reference from the `LoadGameScreen` component (which the clone
still carries, holding correct references under the old field names) across to
`MultiplayerScreen`, retitles the screen, hides the save-only detail rows, and
deletes the import button. It reports what it did, refuses to save if anything
is missing, and is safe to re-run.

It needs **both** components on the prefab: `LoadGameScreen` as the source of
the references, and `MultiplayerScreen` as the destination. If `LoadGameScreen`
has already been removed, restore the prefab from git and add
`MultiplayerScreen` alongside it.

### The mapping it applies

| `MultiplayerScreen` | From | Clone object |
|---|---|---|
| `JoinButton` | `LoadGameButton` | `ButtonIconLoad` |
| `AddHostButton` | `ExportFileButton` | `ButtonIconExport` |
| `RenameHostButton` | `RenameFileButton` | `ButtonIconRename` |
| `DeleteHostButton` | `DeleteGameConfirmButton` | `ButtonIconDelete` |
| `HostInfoRoot` | `GameInfoRoot` | - |
| `MultipleItemsSelectedRoot` | `MultipleItemsSelectedRoot` | `MultiSelectRoot` |
| `ItemsList` | `ItemsList` | `ScrollRectGameList` |
| `HostNameLabel` | `SaveGameNameLabel` | `LegacyTextSubTitle` |
| `HostCodeLabel` | `SaveGameFilename` | `TextFilenameValue` |
| `HostAddedLabel` | `SaveGameDateLabel` | `TextDate` |
| `HostStatusLabel` | `SaveGameFilesize` | `TextFileSizeValue` |

The import button is deleted rather than repurposed: there is no file to
import, since a host is reached over the network.

`HostStatusLabel` has no natural counterpart - the save panel has no "is this
reachable" row - so it borrows `TextFileSizeValue`, and that row's own "File
size" caption is hidden so the status is not mislabelled.

### Still manual

**The list scripts.** `ScrollRectGameList` carries `LoadGameItemsUI`; it needs
`SavedHostItemsUI` from `SavedHostItemsUI.cs`. The row prefab inside it carries
`LoadGameItemUI`; it needs `SavedHostItemUI`, with three labels wired:

- `NameLabel` - the host's name (was `ScenarioNameLabel`)
- `StatusLabel` - online / offline (new; reuse the old date or scenario label)
- `OfflineIndicator` - dimmed when unreachable (was `DisabledIndicator`)

**The main menu's screen reference.** `MainMenuScreen.MultiplayerScreenPrefab`
in `MenuScreenScene.unity` points at a prefab that was deleted when this screen
was rebuilt, so the reference is dead. Select `MainMenuScreen` and drag
`MultiplayerScreen.prefab` into it.

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