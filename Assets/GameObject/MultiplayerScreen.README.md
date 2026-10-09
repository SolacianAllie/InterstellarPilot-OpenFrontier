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
`MultiplayerScreen`, retitles the screen, relabels the visible text, hides the
save-only detail rows, and wires the row prefab. It reports what it did,
refuses to save if anything is missing, and is safe to re-run.

The buttons are already in the prefab and already wired - it is a copy of
LoadGameScreen, so nothing needs adding, and `ButtonIconLoad` plus the
hand-duplicated `ButtonIconAddHost` both have their references set.

`ButtonIconAddHost` was duplicated from the load button in the editor rather
than cloned by the tool, so it arrived with nothing pointing at it. It is wired
now. It carries no children - neither does any other button in the row, which
is icon-only throughout - and it has no inherited click listeners.

`ButtonIconExport` was deleted: hosts are reached over the network, so there is
nothing to export, and an import has the same problem.

### The mapping it applies

| `MultiplayerScreen` | From | Clone object |
|---|---|---|
| `JoinButton` | `LoadGameButton` | `ButtonIconLoad` |
| `AddHostButton` | `ExportFileButton` | `ButtonIconAddHost` |
| `RenameHostButton` | `RenameFileButton` | `ButtonIconRename` |
| `DeleteHostButton` | `DeleteGameConfirmButton` | `ButtonIconDelete` |
| `HostInfoRoot` | `GameInfoRoot` | - |
| `MultipleItemsSelectedRoot` | `MultipleItemsSelectedRoot` | `MultiSelectRoot` |
| `ItemsList` | `ItemsList` | `ScrollRectGameList` |
| `HostNameLabel` | `SaveGameNameLabel` | `LegacyTextSubTitle` |
| `HostCodeLabel` | `SaveGameFilename` | `TextFilenameValue` |
| `HostAddedLabel` | `SaveGameDateLabel` | `TextDate` |
| `HostStatusLabel` | `SaveGameFilesize` | `TextFileSizeValue` |

`HostStatusLabel` has no natural counterpart - the save panel has no "is this
reachable" row - so it borrows `TextFileSizeValue`, and that row's own "File
size" caption is hidden so the status is not mislabelled.

### The row prefab

`ScrollList` instantiates its `ItemPrefab` once per row, so the list needs a
row prefab carrying `SavedHostItemUI` - there is no multiplayer equivalent to
`LoadGameItem.prefab`, which is the template the save-game list clones.

`Assets/GameObject/MultiplayerGameItem.prefab` is that prefab. The tool swaps
`LoadGameItemUI` for `SavedHostItemUI` on it and wires the labels by **object
name**, so the mapping survives a re-layout:

| `SavedHostItemUI` | Object |
|---|---|
| `NameLabel` | `TextHostSenario` |
| `StatusLabel` | `TextConnection` |
| `VersionLabel` | `TextHostVersion` |
| `PlayerCountLabel` | `TextHostPlayerCount` |

`PermadeathImage` is switched off. It came over from the save-game row and is a
plain white box, which means nothing to a host; unreachable rows are dimmed
instead. `Selected` is left alone - the list owns that one.

### Still manual

**The list script.** `ScrollRectGameList` carries `LoadGameItemsUI`; it needs
`SavedHostItemsUI`. This one cannot be automated: the two are unrelated classes,
so no reference can cross between them and a component swap drops its
serialized data. Add `SavedHostItemsUI` in the inspector, remove
`LoadGameItemsUI`, then re-run the tool - it sets `ItemPrefab` only once
`ItemsList` is assigned, so running it before the swap reports the remaining
step rather than appearing to succeed.

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

The result splits across four labels rather than piling into one string, so
each gets its own column:

| Label | Shows |
|---|---|
| `StatusLabel` | `Checking...` / `Online` / `Not accepting players` / `Different version` / `Offline` / `Connected` |
| `PlayerCountLabel` | `N` - hidden when the host is up but empty |
| `VersionLabel` | `v1.2.3` - hidden unless the version is the reason it cannot be joined |
| whole row, dimmed | Confirmed unreachable |

`Checking...` rather than `Offline`: a claim not yet checked is a lie the
player acts on. For the same reason only a *completed* probe dims a row - a
host being probed right now is not dimmed.

A matching version is never shown. Every healthy row displaying `0.1.1` would
be noise; the version appears only when it is the reason the join will be
refused, and then it is the one thing worth telling the player.

`Offline` deliberately does not distinguish "host closed the game" from "a
router blocked it". With no server there is nothing to ask, and a more
specific-sounding guess would send players after the wrong problem.

Probes re-run every 15 seconds while the screen is open, in parallel so a list
of unreachable hosts still settles within one timeout rather than one per host.