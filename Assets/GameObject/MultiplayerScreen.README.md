# Wiring MultiplayerScreen.prefab

`Assets/GameObject/MultiplayerScreen.prefab` is a clone of `LoadGameScreen.prefab`,
adapted to list saved multiplayer hosts instead of saved games. The list, the
selection handling, the scroll behaviour, the rename flow and the delete
confirmation all come from `LoadGameScreen`'s own machinery — only the item
type and what the primary button does are different.

## What still needs doing in the editor

**Run `Tools > Multiplayer > Transfer Screen References`** if you have not
since the list component was swapped.

It assigns the host list, puts `SavedHostItemUI` on the row prefab, wires its
labels and points the list at it. It reports what it did, refuses to save if
anything is missing, and is safe to re-run.

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

The references came from the clone's own `LoadGameScreen` component, which held
everything correctly wired under the old names. It is now removed, so the table
is a record rather than a set of instructions:

| `MultiplayerScreen` | Was | Clone object |
|---|---|---|
| `JoinButton` | `LoadGameButton` | `ButtonIconLoad` |
| `AddHostButton` | `ExportFileButton` | `ButtonIconAddHost` |
| `RenameHostButton` | `RenameFileButton` | `ButtonIconRename` |
| `DeleteHostButton` | `DeleteGameConfirmButton` | `ButtonIconDelete` |
| `SessionPanelRoot` | `GameInfoRoot` | `SelectedGameRoot` |
| `MultipleItemsSelectedRoot` | `MultipleItemsSelectedRoot` | `MultiSelectRoot` |
| `ItemsList` | `ItemsList` | `ScrollRectGameList` |
| `SessionTitleLabel` | `SaveGameNameLabel` | `LegacyTextSubTitle` |
| `SessionProblemLabel` | `SaveGameDateLabel` | `TextDate` |

### The session panel

The right-hand panel reports **this player's own session**, not the selected
host. It exists because "is multiplayer even working?" previously had its
answer only in a log file on a developer's machine, and every line on it is
something a failed join traces back to.

The save panel's spare rows are reused rather than new objects being added, so
each is relabelled to say what it now holds. The tool does this after hiding
the save-only rows, which is why the captions here differ from the clone's.

| `MultiplayerScreen` | Row reused | Caption |
|---|---|---|
| `SessionCodeLabel` | `TextFileName` | `Join code:` |
| `SessionHostingLabel` | `TextFileSize` | `Hosting:` |
| `SessionAddressLabel` | `TextScenarioAuthor` | `Listening on:` |
| `SessionModeLabel` | `TextScenarioAuthoringTool` | `Mode:` |
| `SessionVersionLabel` | `TextCreatedVersion` | `Game version:` |
| `SessionProtocolLabel` | `TextSaveVersion` | `Protocol:` |
| `SessionPlayersLabel` | `TextNetWorth` | `Players:` |
| `SessionIdentityLabel` | `TextSector` | `Player:` |
| `SessionRouterLabel` | `TextPilot` | `Router:` |

`SessionCodeLabel` and `SessionHostingLabel` keep the two references the clone
already carried (`TextFilenameValue` and `TextFileSizeValue`), so only their
captions changed.

The panel gives way to `MultiSelectRoot` when more than one row is selected -
the two share the same space and would otherwise be drawn on top of each other.

`Router:` reports the UPnP port forward. It is the one row here that takes a
few seconds to settle - the gateway is asked on a background thread after
hosting starts - so it moves through "Asking the router...", "Forwarded: 24890",
or "No" depending on what the router had to say.

The screen heading lives in `TextScreenTitle`, not on the button: the button row
is icon-only throughout, like the rest.

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

Nothing, as of the last run. What was left by hand:

- **`ScrollRectGameList`: `LoadGameItemsUI` → `SavedHostItemsUI`.** Unrelated
  classes — both derive from `ScrollList`, but of different item types — so no
  reference can cross and a component swap drops its serialized data. The tool
  assigns `ItemsList` and restores `NoItemsLabel` afterwards, so it has to run
  *after* this step.
- **`MainMenuScreen.MultiplayerScreenPrefab`** in `MenuScreenScene.unity` —
  drag `MultiplayerScreen.prefab` into it.

### Re-running after LoadGameScreen is gone

The tool inherited its references from the `LoadGameScreen` component sitting
on the clone, and removed that component once they were across. A run with it
already gone is normal and not a fault: the tool reports that there was nothing
left to inherit and goes straight to the work that could not be inherited
either way — assigning the list, building the row prefab, and pointing the list
at it. It is safe to run repeatedly.

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