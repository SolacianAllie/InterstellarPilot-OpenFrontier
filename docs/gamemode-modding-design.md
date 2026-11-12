# Gamemode + Modding Design — future notes

*Companion to `docs/new-ui-guide.md` (the screens) and `AGENTS.md` (prefab
strategy, traps). Compiled 2026-11-11 from a design session. **Nothing here
is implemented** — these are the decisions the code must honour when it
lands, recorded so the conversation that produced them survives.*

## Why this exists (context, 2026-11-11)

The NewGame screen exists in `Main Menu.unity`: a `Gamemode Card`
(Title / Description / Image) plus `SelectionMenu → Scroll View → Content`
(stock uGUI spine already baked into the `Fullscreen Menu` scaffold). No
rows, no populator, no code yet. The need: gamemodes listed in that scroll,
addable by us AND by mods later. First real customer: a **"developer"
gamemode** used to build the rest of the game.

The modding vision that shaped every decision: mods add **ships, weapons,
components, commodities, stations, and gamemodes** — authored in Unity with
a future packing tool ("pack up mods, toss them where the game wants them,
and they seamlessly just work"). Mod management (import + delete) lives
in-game in the **Game Extras** menu, which makes PC and mobile one flow.

## Decision 1 — the registry seam (2026-11-11)

The scroll list renders **registrations**, never a fixed set. The UI never
names a gamemode in code or in the scene: it asks a registry for
definitions and instantiates one row per definition. "We add a gamemode"
and "a mod adds a gamemode" are the same act — something puts a
definition into the registry. The direction of knowledge is the
inviolable part: **nothing in a screen may enumerate or hardcode a mode.**

This also settles the populator doctrine (`new-ui-guide.md`: "real
auto-population when earned"): a mod-driven list cannot be hand-authored
block instances, so the gamemode list is a genuinely runtime-fed list and
earns its populator (the planned first user, alongside the future Load
Game save-slot list).

## Decision 2 — asset form: ScriptableObject (2026-11-11)

Mods are Unity-authored (Decision 5), so **AssetBundle + Unity-native
definition assets beat JSON** — you cannot put a texture in a text file
without rebuilding Unity's import pipeline by hand. A ScriptableObject
carries its object references (preview sprites, later meshes/prefabs)
inside the asset, which is exactly what makes it AssetBundle-portable
unchanged. SOs also tolerate added serialized fields (new fields default),
so a definition grows launch payload / tags / gating later without
breaking existing assets.

The legacy pattern (MonoBehaviour data prefabs + `UnityObjectHelper.LoadAll<T>`)
existed because content came out of a decompile; new authored content is SO.

## Decision 3 — string Ids, namespaced NOW

Everything downstream keys off `Id`, and the registry dedupes by it:
`core.universe`, `core.dev` for built-ins; `mod.<author>.<name>` for mods.
Renaming Ids after saves exist is the reserved-id-band problem all over
again — **this is the expensive-to-retrofit decision, made before any code.**

## The gamemode pieces (planned files — NOT yet written)

Six small files, one job each, one MonoBehaviour per file (filename ==
class name), all buttons self-wiring in Awake/Bind so **On Click lists stay
empty** (the double-fire trap). Definition + registry live in namespace
`OpenFrontier.Gamemodes` — destined for a content assembly, never the menu
one (Decision 8); the four UI files stay `OpenFrontier.MainMenu`.

| File | Kind | Job |
|---|---|---|
| `GamemodeDefinition.cs` | ScriptableObject | One playable mode as an asset: `Id`, `Title`, `Description`, `PreviewImage` (Sprite), `SortOrder`, `DeveloperOnly`. Presentation-only for now — launch config lands with the engine (SOs take new fields without breaking assets). |
| `GamemodeRegistry.cs` | static class | Built-ins via `Resources.LoadAll<GamemodeDefinition>("Gamemodes")`; public `Register()` reserved for the future ModLoader; dedupe **built-in wins + log a warning**; sort by `SortOrder` then `Title`; `DeveloperOnly` filter (`Application.isEditor || Debug.isDebugBuild`). |
| `GamemodeList.cs` | MonoBehaviour on the Scroll View's `Content` | Rebuilds rows **every OnEnable** (screens sleep hidden → free refresh; self-heals after mod toggles). Owns `Selected` + a `SelectedChanged` event. |
| `GamemodeRow.cs` | MonoBehaviour on the row prefab | Button + TMP labels (`SelectableTextColor`), `Bind(definition)`; click walks up to the owning list (`OpenScreenButton.FindOwnScreen` pattern). |
| `GamemodeCard.cs` | MonoBehaviour on the `Gamemode Card` | Listens to `SelectedChanged`; refreshes Title / Description / Image. |
| `StartGamemodeButton.cs` | MonoBehaviour on a Start button | The launch seam — stub today; the future port of legacy `ScenarioUI.PlayScenario`. |

Locations: definition assets under `Assets/OpenFrontier/Resources/Gamemodes/`;
the `Gamemode Row` prefab under `Resources/Prefabs/UIX/` with the other
blank-slate blocks. Legacy `ScrollList<T>`/`ScrollListItem<T>` are the old
navigator generation — **do not port**; rows are plain uGUI here.

Editor wiring checklist (user performs — headless Unity does not work here):
definition assets (`Create → OpenFrontier → Gamemode Definition`) with
`core.*` Ids; author the row prefab; `GamemodeList` on Content with the row
prefab dragged in; `GamemodeCard` on the card with Title/Description/Image +
the list reference; Start button + `StartGamemodeButton` → list; sidebar
New Game gets `OpenScreenButton` → `NewGameMenu` root. After the `.cs`
files land: `bash tools/harness/check.sh` (loud-idle until an asmdef exists)
then refocus the editor and `grep -c "error CS" Logs/Editor.log` — the real
gate for loose scripts.

## Decision 5 — one pattern, every content type (2026-11-11)

Ships / weapons / components / commodities / stations / gamemodes are one
problem with six definition types:

- **A definition asset per category** (`ShipDefinition`, `WeaponDefinition`,
  … `GamemodeDefinition`) — SOs whose serialized fields reference their
  textures, meshes, prefabs, stats.
- **A registry per category** — the same tiny static shape as
  `GamemodeRegistry`, keyed by string Id.
- **A loader that routes by TYPE, not by folder** — it opens a mod's
  bundle, loads all definition assets, hands each to the registry for its
  concrete type; the loader never learns what a "ship" is. Adding a new
  moddable category = author a definition type + a registry; the mod
  pipeline, loader, and every existing screen are untouched.

## Decision 6 — mod package anatomy

One folder/zip per mod: `manifest.json` + one AssetBundle.

```json
{ "id": "mod.author.packs", "name": "Author's Pack",
  "version": "1.0", "minGameVersion": "0.1",
  "dependencies": [] }
```

The manifest is metadata ONLY (identity, deps, compat) — never content.
All content lives in the bundle as definition assets with their references
intact. Duplicate Id across mods → **built-in wins, warning logged**;
override semantics ("rebalance a core mode") are reserved for later, not
designed now. Cross-mod needs are declared in `dependencies`.

## Decision 7 — saves record their mods (2026-11-11)

The save header lists `(mod Id, version)` pairs. Two load checks, because
they fail differently:

1. **Required mod set — hard blocker.** Any header entry missing from the
   active set refuses the load with an explicit screen ("Loaded with:
   Author's Pack 1.0 — enable it in Game Extras › Mods"); save slots show
   mod badges so the reason is visible before trying.
2. **Referenced-Id resolution — deep check.** Every content Id stored
   inside the save must resolve in the active registries. Catches the
   partial case (mod enabled but old version, or a deleted dependency)
   where the mod set nominally matches but a specific Id doesn't exist.
   Fail loudly per-Id, never half-load silently.

Versions drive **warnings, not blockers** (same mod, older version than the
save recorded → warn, allow). Deletion warns when saves reference the mod
(header scan — cheap); deleting a mod whose gamemode is selected in New
Game falls selection back to a built-in. Freebie: multiplayer's
client-mod-compat check is exactly this machinery, for nothing extra.

## Decision 8 — assembly implication

Definitions must eventually live in a **dependency-light assembly** the mod
SDK can reference (Unity + that one game DLL). Today this is only a
namespace decision (`OpenFrontier.Gamemodes`); when asmdefs land under
Assets, content definitions get their own small asmdef — never buried in
menu or engine code.

## Mod management: Game Extras (2026-11-11 decisions)

- **In-game import + delete only.** Canonical store:
  `Application.persistentDataPath/mods/<mod-id>/` (bundle + manifest per
  mod) — writable on both platforms, survives app updates. One flow,
  PC and mobile alike; the loader's API stays "ingest from source" with
  exactly one source.
- **REALTIME enable/disable** (user decision). Mods are managed only from
  the main menu, so there is no live gameplay session to corrupt — the
  whole hot-load corruption class is absent. The three hazards that remain,
  each solved by existing architecture:
  1. *Operation order:* disable/uninstall = eject from registries →
     `AssetBundle.Unload(true)` → delete files. Never delete with the
     handle open (PC file locks); if unload is somehow blocked, eject the
     content immediately and defer only the file deletion.
  2. *Screens sleep when hidden* (`MenuScreen` sibling pattern) → any
     screen showing mod-dependent data refreshes in `OnEnable` when
     navigated to; a mod toggled in Game Extras is by construction not
     visible on the screens it affects.
  3. *The menu backdrop is a live world:* mod change marks the menu world
     stale; `MainMenuWorldController` already re-seeds per visit, so it
     rebuilds against the new active set on next focus. No special
     machinery.
- **State model: installed / enabled (= instantly active) / failed-to-load.**
  Toggle = load + register immediately. No "restart to apply" state ever
  exists; broken mods surface inline as failure states on their row.
- **Import = validate before install.** Parse manifest + sanity-check the
  bundle in a quarantine temp location; only then copy into the store.
  Same-Id re-import: higher version = update; lower = explicit downgrade
  warning.
- **Screen shape.** Game Extras stays a hub; mods is one section among
  future ones. The mods list is the SECOND genuinely runtime-fed list,
  with richer rows (enable toggle, version, delete) — its own concrete
  list, and its data source is the mod **store** (installed list + enabled
  flags), NOT the registries (registries hold active content only; the
  manager reads installed content). The generic `ListPopulator` extraction
  question goes live only when a third list reveals the shared shape.

## Launch contract + the dev gamemode

Confirm hands over the definition (or its Id) to the launch stub — the
future port of legacy `ScenarioUI.PlayScenario`. Definitions stay
presentation-only until the engine's world-boot exists (launch payload
like legacy `WorldSeedSettings` — the ~28-layer seeding config a mod
gamemode would eventually tweak — is itself asset-referencing and the SO
pattern carries it fine).

The dev gamemode is an ordinary definition with `DeveloperOnly` ticked;
release players never see it, and its launch path stays a stub until the
engine seam exists — nothing dev-only leaks into the shipped UI.

**RESOLVED 2026-11-12** (see `docs/engine-plan.md`, M1): the dev gamemode
boots the bare M1 stub scene — empty skybox; the engine boots and proves
the chain (timeScale 0→1, log/unscaled marker); zero content authoring.

## Build order

1. **Now:** the six gamemode files — first registry customer, needed for
   the dev gamemode.
2. **When the first engine content lands** (ships/weapons in the rebuild):
   the second definition + registry, then extract whatever generalizes.
3. **ModLoader + the Game Extras mods screen: one milestone, together.**
4. **Packing editor tool: last** — a convenience over `BuildAssetBundles`
   once the package shape has been proven by hand.

## Legacy reference (PreRebuild — read-only contracts)

`GameModeScreen` (nav hub) → `UniverseSelectScreen` (`ScrollList<ScenarioInfo>`)
→ `UniverseGameTypeScreen` (`ScrollList<UniverseGameTypeInfo>`) →
`ScenarioUI.PlayScenario(loadData)` → `ScenarioLoaderUI.TryLoadScenario` →
scene load. Shapes worth reading: `ScenarioInfo` + `UniverseGameTypeInfo`
(what a mode record carried), `ScenarioLoadData`/`UniverseScenarioData`
(launch payload composed AT CONFIRM time, not stored in the menu record —
the reason `GamemodeDefinition` stays presentation-only), and
`GameController.LoadScenarios` + `UnityObjectHelper.LoadAll<T>` (the
Resources-discovery shape `GamemodeRegistry` follows).
