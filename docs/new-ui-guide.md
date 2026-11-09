# New UI Guide — building screens under `Assets/OpenFrontier/`

*Companion to `docs/legacy-ui-inventory.md` (what the OLD UI did) and AGENTS.md
(prefab strategy, 2026-11-09). This file is the HOW-TO for the NEW UI.*
*Compiled 2026-11-09 after the harness went strict again (first MainMenu code).*

## State of the toolbox (what exists today)

Scripts live loose under `Assets/OpenFrontier/Scripts/MainMenu/` (editor-only,
Assembly-CSharp — `Logs/Editor.log` is its gate; the offline harness now
compiles them as the `Assembly-CSharp` project too):

| Script | Job | Wired today? |
|---|---|---|
| `MenuScreen` | screen marker + open state + sibling close + optional fade | **no users yet** |
| `OpenScreenButton` | one-drag screen switch on a Button | **no users yet** |
| `QuitGameButton` | Application.Quit on Button click | **no users yet** |
| `SafeAreaFitter` | clamps safe roots into device insets at play | scene + scaffold |
| `SelectableTextColor` | label tint follows Button states (+ `ExtraTargets` for multi-label buttons) | scene + scaffold |
| `SelectableImageColor` | same, for embedded icon Graphics with their own ColorBlock | scene only |
| `VersionLabel` | fills `Versioning.Version` (+ StageTag) | scene + scaffold |
| `OpenFrontier_UISaturation.shader` | greyscale-on-unfocused image tint | scene only |

Shared art + authoring blocks live under `Assets/OpenFrontier/Resources/Prefabs/UIX/`
(`Fullscreen Menu.prefab` = blank screen scaffold: Backdrop/Background/Title/
Subtitle/Separator/SafeAreaRoot/Version chrome) and `Assets/OpenFrontier/Textures/UIX/`.

The Main Menu scene is currently a **single screen** (SidebarMenu with
New Game / Load Game / Multiplayer / Tutorials / News / Options / Quit).
Nothing switches yet — the two navigation scripts were built to make the
first switch the cheapest possible editor operation.

## The three concepts (the whole navigation system)

1. **`MenuScreen` marker** on a screen's root. It is the screen's identity:
   `Show()` opens it and closes every *sibling* that also carries a marker;
   `Hide()` closes it; `EntryScreen` says "at play start every sibling menu
   screen is closed, this one stays"; `FadeDuration` fades in (needs a
   CanvasGroup on the same object). That is the entire component.
2. **`OpenScreenButton`** on a Button, with `Target` dragged to the other
   screen's root. Click = show Target + hide the button's own screen (found
   by walking up to the nearest ancestor with a `MenuScreen`). A back button
   is the same component pointing back at the parent.
3. **The sibling rule.** Screens that swap are siblings under one parent
   (an empty organizer object, e.g. `Screens`). The saved-visible screen is
   the entry screen; `MenuScreen`'s sibling sweep makes a forgotten extra
   visible screen self-heal at play start.

There is **no** navigator object, screen registry, ShowXxxScreen extension
method, preload list, or navigation stack to fill in. That was the legacy
setup cost (78-field `SceneNameMapping` + 60 `ShowXxxScreen` methods +
`AllScreensLoader` preloading every screen at boot); here a pure navigation
screen costs one prefab and zero code.

## Recipe: add a screen (all editor, no code)

1. **Build the screen.** Drop/instance the `Fullscreen Menu` scaffold under
   the `Screens` organizer, rename it (e.g. `Options Screen`), make it
   self-contained, then break nothing that points at the scaffold — author
   content *inside the copy*, never edit the scaffold to special-case one
   screen (it is a template by agreement).
2. **Mark it.** `Add Component → MenuScreen` on the screen root (CanvasGroup
   needed only if you want fade).
3. **Deactivate it.** Save the scene with exactly the entry screen visible.
4. **Wire in.** On the opener's button: `Add Component → OpenScreenButton`,
   drag the new screen root into `Target`. On the new screen's Back button:
   same component, `Target` = the opener's root.
5. **Buttons inside the screen** come from the MenuButton block (when it is
   extracted into a prefab — per AGENTS.md, block-first authoring):
   `SelectableTextColor` tints its labels, `SelectableImageColor` tints an
   embedded icon independently.
6. **Fresh scripts** (if a screen needs a behaviour stock uGUI cannot do):
   one MonoBehaviour per file, filename == class name; then
   `bash tools/harness/check.sh --regen` (a new `.cs` file is orphaned from
   the generated csproj until regen — that stale-guard FAIL is the entire
   reason a plain `check.sh` once passed a stale build), and the
   Editor.log compile check after the user's refocus.

### Editor traps to respect while wiring

- Leave the **On Click list empty** on any button carrying an OpenScreen/
  Quit-script: the listener is added in `Awake`, and a manual On Click on
  top fires the action twice per press (the nested-selectables trap shape).
- **One behaviour per button** — a button that quits gets `QuitGameButton`,
  one that opens `OpenScreenButton`, not both.
- Never hand-edit prefab YAML (AGENTS.md): author in the editor, and read
  the diff on the prefab after commits.
- Safe-area / aspect verification happens in **PLAY mode on the Device
  Simulator** — the simulator's fold-notch overlay is an editor-view
  visualization only, and `SafeAreaFitter` clamps at runtime by design.

## Auto-laid fullscreen screens: scaffold spine + blocks (2026-11-09)

The standing question: *can the fullscreen GUIs populate themselves, so a
change to one baseline prefab re-lays every screen without touching each
screen?* The answer, in this build's vocabulary, has three layers — all of it
stock uGUI, zero new scripts, mobile-first by construction.

### The two-layer split every screen inherits

Every screen is an instance of the `Fullscreen Menu` scaffold, and the
scaffold splits into two ownership zones:

- **Chrome — scaffold-owned.** Backdrop, Background, Title, Subtitle,
  Separator, Version, Quit chrome. Edited ONCE on the baseline; every screen
  inherits the edit automatically. This is the user's requirement, and prefab
  inheritance is the entire mechanism.
- **Body — screen-owned.** One `Body` container under `SafeAreaRoot` is the
  ONLY place a screen author lays content. Handwritten twists live here and
  nowhere else.

The rule that keeps inheritance alive: **an instance override on a
scaffold-owned child freezes that child on that screen forever** — the next
baseline edit silently skips it, and the screen drifts from the template
without any error. So: never move/unlink scaffold chrome inside a screen
instance; if a screen truly needs a different title, that is a logged
exception, because propagation to that screen silently stops for that node.
(Same doctrine as "never edit the scaffold to special-case one screen", from
the other side.)

### The auto-layout spine (stock uGUI; bakes into the scaffold body)

Under `SafeAreaRoot`, along the vertical the screens actually read:

```
SafeAreaRoot            SafeAreaFitter (already there)
 └─ Scroll              ScrollRect + Mask (viewport clips; scrolled on phones)
     └─ Content         VerticalLayoutGroup (child force expand) + ContentSizeFitter
         ├─ MenuButton  (block instance)
         ├─ MenuButton  ...
         └─ header rows / toggles / sliders — same spine, any order
```

Drop blocks in → they stack themselves; content taller than the phone
scrolls; rotate/fold/aspect changes reflow through the anchors with no code.
No per-frame cost, and inactive siblings build nothing (same steady-state as
the nav system: zero). Baking the spine INTO the scaffold means every future
blank-slate screen is born with it — the emptier screen authoring becomes
"drop blocks in", the less work left overall. Non-scrolled exceptions
(confirm dialogs) strip the Scroll off their own instance — a body-layer
override is legitimately theirs.

### Blocks are the reuse vocabulary

Screen bodies are stacks of block instances, same ownership logic one level
down: the MenuButton prefab first (agreed), then each shape when the game
first needs it — two-line card row (News), toggle row (Options), slider row,
list row with value + arrow. Editing a block prefab re-lays every screen and
instance that uses it. The whole visual language ends up living in a dozen
block prefabs instead of sixty screens × their elements — that delta IS the
"less work overall".

### Real auto-population (data → rows) when earned

The first list screen (Options is the likely first — a save list is close
behind) earns a small ListPopulator: row prefab + data hook, instantiating
rows into the Content spine at enable; the layout spine means even
runtime-spawned rows need zero layout
code. Until then, placeholder rows in the scene are the workflow — WYSIWYG,
no authoring pass through logs.

### What NOT to build

- **No data-driven screen-builder framework** (SO manifests describing
  sections + a generic runtime renderer). It converts every design tweak into
  code work, hides authoring behind logs, and abandons the GUID-stable
  editor-authored hierarchy this build has standardized on.
- **No UI Toolkit rewrite.** uGUI owns this build's chrome, the
  saturation-tint shader, TMP labels, SafeAreaFitter anchors, and the touch
  input path — and UI Toolkit's (real) auto-layout wins don't outweigh having
  two styling vocabularies forever, on a one-person project.
- **No per-screen ScreenBase code.** Pure navigation stays the zero-code
  sibling-sweep + OpenScreenButton shape already landed. Sections that need
  the engine get a small PUBLIC method on that section's block, called by a
  screen-scope class, not a screen-complete subclass.

## Setup cost, then vs now

| Legacy step | New equivalent |
|---|---|
| `ScreenBase` subclass | none (pure nav screens need no code at all) |
| author prefabs via `ScreenNameMapping`-style registry field | scene placement (sibling under the organizer) |
| `ShowXxxScreen` extension in ScreenNavigatorExtensions | one drag (`OpenScreenButton.Target`) |
| `AllScreensLoader` preloads all screens | none — deactivated siblings cost no build |
| `ScreenNavigationRequest<T>` result plumbing | none for menus; context = public fields when a screen truly needs one |

## Growth paths — earned, not pre-built

Add these the first time a need appears, in this order; not before.

1. **Back to whoever opened me.** Only needed once a screen is reachable
   from *two* parents (Load Game from Main Menu and from Pause). Shape:
   `OpenScreenButton` records its own screen as `Target`'s opener before
   showing it; a `BackScreenButton` shows `opener ?? fallback` and falls
   back to nothing (menu top-lvl screens just don't record openers).
   Until then, screens have a fixed parent and back is a plain drag.
2. **Message box.** The legacy game's single most-used UI entry point
   (`UIController.ShowMessageBox`). First real confirm dialog (overwrite
   save / quit-without-saving) earns a `MessageBox` prefab + one tiny
   static helper; keep the callback pattern = *stash result, act on
   dismissed*, mirroring the legacy pitfalls documented in AGENTS.md.
3. **Escape / hardware back.** Android's back gesture arrives as Escape.
   Menu-top-level can ignore it for now (legacy menu "back" = quit);
   give any deep screen a `MenuScreen`-level optional "Escape closes me"
   toggle if playtesting shows the need.
4. **In-game overlay screens are a different home.** Docking hub, pause
   overlay, HUD-context panels: they must freeze/coexist with the engine
   (legacy `PauseWhenShown` froze `timeScale`; HUD is the nav root while
   flying and the dialogue controller pauses the world). The menu sibling
   system is deliberately **menu-scoped**; engine screens arrive with the
   engine work and use the prefab-per-screen under `Resources/Prefabs/UIX/`
   home, not the menu organizer.

## Non-negotiables carried over from AGENTS.md

- Everything lands on `Dev`; commits sweep the user's working tree.
- Editor log is the real gate for loose scripts; harness is necessary-not-sufficient.
- Mobile-first: nothing per-frame that a deactivated sibling sweep does;
  the whole system's steady-state cost is zero (inactive GOs build nothing).
