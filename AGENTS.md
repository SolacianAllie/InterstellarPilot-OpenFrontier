# Notes for Coding Agents

## This is a MOBILE game first, PC second

Decide everything against the weakest target, not the one in front of you.
A PC has cores to spare and desktop memory; a phone does not, and a foldable
is worse than a desktop again. Something that is free on a PC can be the
single largest cost in a mobile build, and the PC is not where anyone plays.

Practical form of it, in the order it actually bites:

- **Diagnostic work does not ship.** Anything only a developer reads - an
  overlay, a voice report, a scan that feeds one - belongs behind
  `DEVELOPMENT_BUILD || UNITY_EDITOR`, compiled out rather than runtime-guarded
  so a release build does not contain it. `MusicDirector.RefreshVoiceReport`
  ran two `FindObjectsOfType` scans every quarter second in release to fill a
  cache that only the overlay read, and the overlay is compiled out. Removed.
  **The rule is "only a developer reads it", not "it measures something".** The
  dock header's framerate and V-Sync readout
  (`DockUIHeader.GetLocationText`) is a deliberate player-facing feature - it is
  how performance gets checked without entering dev menus - so it is NOT to be
  gated. It accumulates `Time.unscaledDeltaTime` on purpose: `timeScale` is 0
  behind any `PauseWhenShown` screen, and a scaled reading would report 0fps
  exactly when something is holding the framerate up.
- **Never put a scene-wide search on the render path.** `FindObjectsOfType`
  walks every GameObject and allocates an array. In `OnGUI` it runs at least
  twice per frame. If something must be watched, watch it on an event
  (`sceneLoaded`, `sceneUnloaded`) and use a poll only as a backstop, at a
  rate the weakest device would not notice.
- **Per-frame work is a per-frame tax on a phone.** If it can be done at
  2-4 Hz because nothing changes faster, it is at 2-4 Hz. "Cheap enough" is
  judged against a budget, not against the desktop it was tested on.
- **Look for the symptom that lies.** A CPU cost competing with rendering only
  shows when there is rendering to compete with, so it appears as "moving the
  camera halves the FPS" and looks like a graphics problem. Measure on the
  target before believing what the symptom points at.
- **Memory is budget starvation, not depth** - see the mipmap streaming note
  below. The same instinct applies to allocations: steady small allocations
  cause GC hitches, which read as random frame drops ("sometimes"), not as
  the cost they are.

When a feature is being designed, ask what it costs on a mid-range phone
before asking what it costs here.

## Branches: everything lands on `Dev`

`Dev` is the working branch and is checked out. Commit and push **Dev**:

```bash
git commit -q -m "..."
git push origin Dev        # never bare `git push` - see below
```

`Legacy` is the historical branch and is **frozen** at `e530dbac`. It is
still `origin/HEAD`, which is the trap: **a bare `git push` follows
`origin/HEAD` and pushes `Legacy`, silently leaving `Dev` behind.** Always
name the branch. If a push reports "Everything up-to-date" while `Dev` is
behind `Legacy`, this is why - `git push origin Legacy Dev` pushed `Legacy`
and found `Dev` genuinely unchanged, because the new commit was only ever on
`Legacy`.

If `Dev` needs to move, fast-forward it rather than checking it out, so the
working tree is never disturbed:

```bash
git branch -f Dev Legacy && git push origin Dev   # only if Legacy is ahead
```

Only non-obvious traps live here — things you cannot learn from the project
structure. Add new ones as they are found.

## Verify, don't assume — before you build on a guess

**Before acting on any assumption about how something works, read the code and
confirm it.** This applies in write mode too, and it is not a courtesy for
small things: it is the rule that saves the build cycles.

Why it is written down: being wrong about how the game works cost more time
this session than any bug did. Concretely, on one feature:

- Assumed docking was refused in the right place. It was refused in a
  primitive that `NpcPilot`, `Fleet`, `FleetSpawn` and the world seeders all
  call — so a client could not undock an NPC and could not load a world whose
  ships were saved docked. Found by listing the callers *after* shipping it.
- Assumed there was one undock button. There were two, in two different
  screens, and the relay went on one. Found by sweeping for callers after a
  failed test.
- Assumed "use the local faction's template" would show credits. Reading the
  panel showed the template difference is two rows and Wealth was identical for
  everyone — the premise was wrong before a line was written.
- Assumed ditching destroyed a ship. Reading it showed `unit.Faction = null`
  and the ship persists, unowned and claimable — which is a third case that
  did not exist in the design.

**How to do it cheaply, before writing anything:**

- `grep -rn "<method>(" Assets/Scripts --include=*.cs` — every caller, not the
  one that seemed obvious. Two of the four mistakes above were exactly this.
- Read the *whole* method, not the part you assumed. The template case was one
  expression inside a block I had not read to the end.
- When a claim depends on two systems meeting (a client's request and the
  host's validator), check **both**. Assuming one implies the other is how the
  unowned-ship claim looked finished while the host would have refused it.

A cheap read is minutes. A wrong assumption costs a two-person rebuild and a
play session, and leaves a log that cannot explain itself.

## The editor's focus-in segfault is Unity's, not ours

`signo:11 code:1`, 33 frames, all native, zero managed. Identical twice:

```
OnFocusIn → NativeWindowManager::UpdateActiveWindow
          → ContainerWindow::DoActivateAndBringToFront
          → GtkSignalWaiter::WaitForWindowActivation
          → PumpGtkMainloop → gtk_main_iteration_do
          → g_main_context_iteration → gdk_display_get_event   ← SIGSEGV
```

A use-after-free of the `GdkDisplay`, inside Unity's Linux window manager,
pumping GTK while bringing a window to the front. The frame *offsets* are the
same in every crash; only the mmap base moves (ASLR). Nothing in
`Assets/Scripts` can produce or prevent this stack.

It fires on **focus-in while an asset import is in flight** — the log always
shows `ImportOutOfDateAssets` right above it. Confirm with
`grep -n "Caught fatal signal" Logs/Editor.log` and compare frames to
`Logs/Editor-prev.log`. If a managed frame ever appears, it is a different
crash and worth chasing.

What ours contributes is the *state* it lands in. It was caught twice while
the editor sat at the main menu holding a live session, still applying unit
state in `LateUpdate`, still uploading. That was the `EndSession` bug (fixed
in `0d1f29cb`): the socket outlived the session. So when triaging a crash
here, first check `grep -nE "session ended|Moving to main menu|disconnect"`
and make sure the disconnect actually followed — a session that ended without
one is the thing to fix, not the editor.

## Offline compile verification (`/tmp/opencode/harness`)

C# changes are verified outside the editor with a generated .NET harness:

- Regenerate: `bash /tmp/opencode/harness/check.sh --regen`
- Check: `bash /tmp/opencode/harness/check.sh`
- **It lives in /tmp. A power cut wipes it** — as happened on 2026-10-10. The
  project is safe (everything is committed); only `gen.py` and `check.sh` have
  to be written again. Everything they need survives in the project: Unity's
  managed DLLs under `~/Unity/Hub/Editor/6000.6.4f1/Editor/Data/Managed`, the
  package DLLs under `Library/ScriptAssemblies`, and the vendored ones under
  `Assets/Plugins`.
- **One csproj per asmdef, with that asmdef's real references.** The earlier
  harness compiled everything into everything, which meant it could not see
  the two error classes that have actually bitten:
  - a script using a type from an assembly it does not reference (CS0234)
  - a call that exists in a stub but not in Unity 6 (`Resolution.refreshRateRatio.value`
    is a `double`; a stub said `float`, and it compiled)
- Referencing the **editor's own DLLs rather than a stub** closes the second
  class outright. There is no stub to fall out of step with the editor.
- `editor-scripts` is `Assets/Editor` — loose scripts, which Unity compiles
  into `Assembly-CSharp-Editor`, referencing every game assembly. Platform
  build callbacks must live there; see the trap below.
- **The harness CANNOT catch a loop that never terminates.** `while (num > 0)`
  with nothing in the body changing `num` is valid C# and compiles clean in
  both the harness and the editor — it is a runtime hang, on every launch, and
  it looks exactly like a crash. This happened on 2026-10-10: adding a
  `continue` to `EngineASX.UpdateInactiveUnits` dropped the loop's `num--`
  and froze editor and build before the main menu. **Rule: when editing a
  loop, put the counter next to the read, before any `continue`/`break`, and
  re-read the whole body afterwards.** `grep` the loop for its counter as a
  check — the compiler will not do it.
- **`codec-test` links 18 real source files**, copied by its own `sync.py`
  from wherever they actually live. The previous harness kept hand-maintained
  `<Compile Include>` lists, which is how a new protocol message compiled in
  the game and broke a test project that had never heard of it. Tests link the
  real file, so a test cannot pass against a stale copy.
- `Authority.cs` and `HostSession.cs` are **deliberately not in codec-test**:
  they need `Unit`, `Faction` and `NetListener`, and a stub of `Unit` would
  pass while the real type did something else. Absence is better than a
  confident wrong answer.
- **`checks/prefab-field-check.py` was written and deleted** on 2026-10-10.
  It modelled which fields Unity serialises and produced 1867 findings across
  2084 prefabs, i.e. it was wrong about almost everything it said. Modelling
  Unity's serialisation rules well enough to be trusted is a bigger job than
  it looks, and a checker that cries wolf gets ignored and then catches
  nothing. The prefab trap is still real — a hand-edited clone can drop a
  field and fail silently — so it is caught by **reading the diff on the
  prefab**, not by a script.

**Known blind spot:** a clean build is necessary but not sufficient. The
editor's own compile can still differ, because the harness does not define
every symbol the editor defines — verify with `grep -c "error CS" Logs/Editor.log`
after the editor refocuses (should be 0).

**A NEW type in assembly A cannot be used from assembly B until Unity has
rebuilt `Library/ScriptAssemblies`.** The harness resolves cross-assembly
references to Unity's OWN built DLLs:

```xml
<Reference Include="OpenFrontier">
  <HintPath>.../Library/ScriptAssemblies/OpenFrontier.dll</HintPath>
</Reference>
```

So adding a new file to `OpenFrontier` and using it from `OpenFrontier.IP`
fails the harness with `CS0103: the name 'X' does not exist` **even when the
asmdef reference is correct and the editor will compile it**. The DLL is
stale — it was built by Unity, not by the harness — so a type added a minute
ago is genuinely not in it.

This is not a guard firing falsely, so it must not be worked around by
inlining the shared code back into the caller. Confirm it is staleness and
not a real missing reference before deciding:

- `grep -oE '<Reference Include="AssocName">' -A 3` the generated csproj — is
  the association listed at all?
- `ls -la` the HintPath DLL — is its timestamp older than the new file?
- `strings` that DLL for the type — is the *older* sibling type in it while
  the new one is absent? That is the signature of staleness.
- Is there existing code in the same consumer assembly already using that
  assembly? If yes, the direction is proven.

The editor refocus the user does anyway rebuilds `ScriptAssemblies`, which
clears it. `grep -c "error CS" Logs/Editor.log` is the real confirmation.

## The harness is 15x faster now, and this is how

It was 70 seconds and it was not the compiler. Measured breakdown of the old
run: **prefab-yaml-check.py 53s**, thirteen builds ~14s, codec tests ~1.9s
(dotenv run twice), everything else under half a second.

Four changes, in order of what they were worth:

- **`yaml.load_all(text, Loader=CSafeLoader)` instead of `safe_load_all`.**
  Same parser, compiled instead of interpreted: 6.7s to 0.9s over 200
  prefabs. Fall back to `SafeLoader` if libyaml is missing, never fail.
  Takes the prefab check 53s -> 12.9s.
- **Cache the prefab check on (mtime_ns, size), in
  `checks/.prefab-yaml-cache.json`.** The parse is a pure function of the
  file, so 12.9s -> **142ms** on any run where nothing under `Assets/` was
  touched, which is nearly every run. **The cache stores the messages, not
  just the verdict** - caching the boolean would make a second run of a
  failing check print "0 failures" and quietly invert the check, which is
  this harness's signature failure mode.
- **Build the projects in parallel** (`xargs -P 6`, `HARNESS_JOBS` to
  override). They are independent: no generated csproj has a
  `ProjectReference`, because they reference Unity's own DLLs out of
  `Library/ScriptAssemblies` rather than each other, so there is no ordering
  to respect. 14s -> ~4s. Capped at 6 of 12 cores because the editor is
  usually open and starving it to save three seconds is a bad trade.
- **`--no-restore` on every build, and run codec-test ONCE.** NuGet restore
  runs on every `dotnet build` whether anything changed or not - 853ms
  against 521ms. And `dotnet run` was being invoked twice, once to grep the
  output and again for the exit code; capturing once and reusing removed a
  way for the printed verdict and the returned status to disagree.

Now **4.6s warm, 16.6s cold** (a wiped cache re-parses everything). If it
ever reads 70s again, one of these four has been lost.

**`core.ignorecase` was TRUE on a CASE-SENSITIVE filesystem — it deleted a
tracked file through `git add -A`.** `tools/` and `Tools/` are two real
directories here (different inodes), and git was configured to match paths
case-insensitively. So a file written to `Tools/music/` committed as
`tools/music/`, and the next `git add -A` found nothing at `tools/music/`,
staged a **deletion of all 335 lines**, and declined to re-add the copy at
`Tools/music/` because the ignorecase match said it was already tracked.

The symptom is invisible: `git status` shows nothing alarming, the file is
still on disk, and `git ls-files` for either spelling looks reasonable. 87
generated plots sat in the repo the whole time with no script that makes
them, and commit messages cited the absent file for several commits.

Now `core.ignorecase false`, and the script lives at `tools/music/` so the
working tree, the index and the repo agree on one spelling.

**If a tracked file goes missing from `git ls-tree` while still being on
disk, check `git config core.ignorecase` against `stat -c %i` on both
spellings before anything else.** A case mismatch plus a wrong `ignorecase`
deletes silently, and `git checkout` will not bring it back because git does
not know it was ever supposed to be there.

## A green harness run can have compiled almost nothing

`gen.main()` **writes the csproj files every time it runs**, so it is not a
passive reader: if its paths are wrong, it regenerates projects that point at
nothing, finds fewer assemblies, and the build loop iterates over almost
nothing.

That happened. After the asset move, `gen.py` still pointed at `Assets/Scripts`,
which no longer existed. The harness reported **ALL CHECKS PASSED** — for
several commits — having compiled **one project out of thirteen**. Nothing was
red, because every test it did run passed.

A build that is trusted and wrong is worse than one that is broken and ignored,
so `check.sh` now counts projects built against `.asmdef` files on disk and
fails loudly if they disagree. It also has a **stale check**: a `.cs` file that
is in no generated csproj was never compiled by anything, which is how a
`Gizmos.DrawTriangle` — a method that does not exist — reached the editor
while the harness said everything was fine.

Both guards were verified by breaking `gen.py` on purpose and watching them
fail. If either ever fires, **do not work around it** — the honest reading is
that the harness has stopped seeing your code.

Two hardcoded paths in `gen.py` that moved with the layout and had to be
changed: the assembly roots (`ASSETS`) and the vendored plugin DLL folder
(`Assets/Plugins`, now under `InterstellarPilot2`). A stale one of those does
not error, it silently builds less.

---

## Verification workflow

1. Harness build (fast, catches 99% of issues)
2. Editor refocus by the user → read `Logs/Editor.log` for real-Unity compile errors
3. Runtime/rendering issues are verified by the user in Play mode or on device

Headless Unity does not work in this environment (licensing client fails,
exit 198) — the user performs all editor actions.

## Non-obvious traps

- **Asmdef `Editor/` subfolders do NOT get platform editor-extension DLLs.**
  An `Editor/` folder inside an asmdef compiles into that asmdef's generated
  editor assembly, which lacks references to build-support modules — so
  `using UnityEditor.Android;` fails there with CS0246
  (`IPostGenerateGradleAndroidProject` not found) and kills the Android build
  before Gradle starts. Loose scripts under `Assets/Editor/` compile into
  `Assembly-CSharp-Editor`, which DOES reference them (verified in
  `Library/Bee/artifacts/*/Assembly-CSharp-Editor.rsp`: it lists
  `UnityEditor.Android.Extensions.dll` plus every game assembly). Discord's
  own sample keeps `AndroidPostBuildProcessor` loose for this reason.
  **Platform-specific build callbacks must live in `Assets/Editor/`.**
- **Unity asmdef references are NOT transitive here - use direct
  references only.** `OpenFrontier.IP.Multiplayer` referenced
  `OpenFrontier.IP`, which references `OpenFrontier.IP.SavedGames.V2.Model`,
  and the editor still refused with CS0234 on a namespace the second
  assembly plainly provides. An assembly can only use types from assemblies
  it *directly* references. The compile harness links everything into
  everything, so this is invisible offline and only shows up as CS0234 in
  `Logs/Editor.log`. `asmdef-refs.py` in the harness now catches it (direct
  references only, deliberately); add to that list whenever a script uses a
  project type, or expect CS0234 in the editor.
- **Player factions and player-made units live in reserved id bands**
  (800,000,000 and 900,000,000) - **saves made before 2026-10-10 will not
  find the player's faction at its old number.** Ids are stored verbatim, so
  an older world loads with a stranger where the player's faction should be.
  This was a deliberate choice: the game has changed enough that a fresh save
  is the better outcome, and forcing one beats a world that half-loads.
  `IsPlayerMade(id)` is the test. The engine needs `ReassignFactionId` to move
  a faction between ids, because `factionIdMap` is private.
- **Story universes are baked INTO their scene files:** `unchartered_space.unity`
  (and `a_wormhole_too_far.unity`) contain fully-expanded runtime-named copies
  of every sector's content (`GasCloud_Gas Cloud Cool Blue_-1_No-faction`,
  `AsteroidCluster__-1_No-faction`, ...) — editing the sector PREFABS in
  `Resources/prefabs/worlddata/sectors/` does NOT change the story scenarios.
  The prefabs only serve battles (GUID references from `ScenarioInfoBattle*`)
  and name-based loading (`EngineASX.LoadSectorPrefabs`). Sector-content edits
  for story scenarios must edit the .unity scene too. Scene YAML uses the same
  block structure as prefabs; when deleting objects, scrub `m_Children`
  (indentation-agnostic) and verify no surviving block references the dead
  fileIDs (missions/dialogs reference units).
- **Sun/backdrop render order:** the sun quads (layer 16, queue 3000)
  sit at 0.95×far (~8075); nebula quads (layer 0) were ALSO queue 3000,
  so transparent distance-sort drew nearer nebulae OVER the sun ("sun
  clips into the skybox", worse at 1.6× size). All 338 nebula materials
  are now queue 2999 — the sun always draws after the backdrop, while
  gameplay particles (3000) still distance-sort over it correctly.
- **Mipmap streaming is OFF for good — budget starvation, not
  reduction depth.** This game's working set is enormous (an Instant
  Action capital fleet ≈ 2.3GB+ of 2K maps); any mobile-sane budget
  starves the streamer and textures sit at their smallest loaded mip
  = "white ships". Verified matrix: OFF=fine, 256MB=white (even
  menu), 768-2048MB=white (Instant Action), 4096MB=fine, OFF=fine.
  Mipmaps themselves stay ON (all 746 textures) — only streaming is
  disabled. Do not re-enable.
- **Script GUIDs are deterministic:** `Guid(MD5(assemblyName + namespace + className))`
  (AssetRipper decompile scheme). Never regenerate .meta files; renaming
  namespaces/assemblies is safe because GUIDs live in the .meta files, but the
  scheme no longer matches names after the `Pixelfactor.*` → `OpenFrontier.*` rename.
  Verified: none of six plausible formulas reproduce any existing GUID, so the
  current set is effectively random — generate a fresh UUID hex for a new file.
- **A hand-edited prefab with a null reference fails SILENTLY as
  "nothing happens".** `MultiplayerScreen.prefab` is a hand-edited clone of
  LoadGameScreen's, so a dropped field does not break the build - the C#
  still compiles, the reference reads as null, and whatever was wired to it
  stops working while looking fine. `ItemList` on `ItemListSelectionButton`
  was `{fileID: 0}`; its `Awake` dereferences it, so Unity swallowed a
  NullReferenceException inside `Instantiate` and the screen never opened.
  Nothing in the UI looked broken and no error was shown. `prefab-null-check.py`
  in the harness now catches it - it works from the script side, listing
  fields a component dereferences without a guard - and
  `prefab-field-check.py` catches the other shape (field absent entirely).
  After touching any prefab, run `check.sh`, not just the compile.
- **One MonoBehaviour per file, filename == class name, always.** Unity
  registers exactly one MonoScript per `.cs` file, named after the file. A
  `MonoBehaviour` declared in a file of a *different* name has NO MonoScript,
  so `AddComponent<T>()` and the inspector both refuse it and it serialises as
  `m_Script: {fileID: 0}` — a missing script on the prefab. It is perfectly
  valid C# and compiles clean, so neither compile harness can see it; it only
  fails once the component is actually put on a prefab, several steps from the
  mistake. `SavedHostItemUI` was declared inside `SavedHostItemsUI.cs` (list and
  row genuinely belong together) and the transfer tool silently produced an
  empty row — no error, no exception, the tool reported success.
  `monoscript-check.py` in the harness now fails on it, so **after adding any
  `.cs` file run `check.sh`, not just the compile.**
- **The starfield is pinned at the WORLD ORIGIN, not the camera:**
  `Imphenzia.SpaceForUnity.StaticStars.LateUpdate` force-pins its
  20000-unit-wide star-cube mesh to `Vector3.zero` every frame (DLL,
  uneditable). The main view survives because the DeepSpace "SpaceCamera"
  sits at the sector anchor with far plane 100000 and rotation following
  the main camera (parented under the `SkyboxCamera` object, driven by
  `GameController.LateUpdate`). Nebulae are world-pinned too: they live
  under `ActiveSectorDataGeneric`'s transform — the `PositionAtCamera`
  object is vestigial (it pins only itself; nothing is parented under
  it). Anything that renders the backdrop from a flying-camera position
  (reflection probes!) gets a lopsided or missing sky — capture from the
  SECTOR ANCHOR instead (see `SpaceReflectionProbe`), with far plane
  ~100000.
- **Cameras instantiated mid-scene become rogue BASE cameras in URP:**
  `UrpCameraStacker` only restacks on scene load, so any prefab spawned
  with an enabled Camera (e.g. WormholeAnim) renders AFTER the stack as
  its own base camera — with clear flags that can wipe the frame to black
  (this blacked out the wormhole transition; the anim's camera is now
  disabled in the prefab). Never spawn enabled cameras at runtime unless
  they are explicitly stacked.
- **Play-mode edits to the URP asset can PERSIST in the editor:**
  `ShadowQualitySync` writes `mainLightShadowmapResolution` at runtime;
  the editor re-serialized the asset mid-session and the value stuck
  (4096), while an unrelated setting (`m_PrefilterSoftShadows`) silently
  reverted. After play sessions, diff `Assets/Settings/OpenFrontier-URP.asset`
  before committing — accept intentional drift, restore the rest.
- **No Unity native calls in MonoBehaviour field initializers / static
  constructors** (`new MaterialPropertyBlock()`, `LayerMask.NameToLayer`,
  etc.): `CreateImpl is not allowed to be called from a MonoBehaviour
  constructor` → `TypeInitializationException` poisons the WHOLE type
  (every use throws). Lazy-init with a null check instead
  (`ShieldHitRenderer` was bitten — shield hits errored and rendered
  white/black).
- **URP material postprocessor strips hand-edited YAML** for materials whose
  shader has a known URP ShaderID. Recreate those materials natively via the
  `Material` API; never hand-edit their YAML.
- **The BIRP project** (`InterstellarPilot - Open Frontier BIRP`, sibling
  directory) is a read-only reference of the original game — never modify it.
- **Soft particles are standardized Near=1/Far=0** (empirically verified on
  this OpenGL stack; URP's formula reads 0 behind empty space here).
  Do NOT write new soft-particle fades against `_CameraDepthTexture` —
  `sceneZ` reads ~0 behind open space and any `sceneZ - eyeZ` fade kills
  alpha (this made the legacy-particle additive shaders render lasers
  invisibly; their fade is now a passthrough).
- **`Screen.dpi` can return 0** (Android foldables, Linux editor). Never feed
  it into touch/drag thresholds directly — it once set
  `EventSystem.pixelDragThreshold` to 0 and ate every tap on the sector map.
  Use `OpenFrontier.ScreenDpi.Value` (Screen.dpi → JNI DisplayMetrics → 200
  fallback). Fingers.dll reads `Screen.dpi` in its Awake and falls back to
  its serialized DefaultDPI with a red error; `FingersDpiFix` on the
  GameController prefab's EventSystem object overrides `DeviceInfo` after
  each scene load. The one-time red error at startup is cosmetic and cannot
  be silenced without patching the DLL.
- **Only `Assets/Scripts/OpenFrontier/OpenFrontier.asmdef` references
  `Unity.RenderPipelines.Universal.Runtime`** — any new script touching URP
  types (`UniversalRenderPipelineAsset`, etc.) MUST live in the OpenFrontier
  assembly. The harness links every package into every csproj, so a missing
  asmdef reference passes the harness and fails in the editor with CS0234
  (`ShadowQualitySync` was bitten by this). The Core RP assembly
  (`Unity.RenderPipelines.Core.Runtime`, home of `Volume`/`VolumeProfile`)
  is referenced separately — `PostProcessingQualitySync` needed it added.
- **Never `ShowMessageBox` from a `RenameConfirmed` (or any
  `ScreenBase`-event) callback.** `RenameUnitScreen.OkButtonClick` fires
  `RenameConfirmed` and *then* calls `NavigateBack()`. A message box becomes
  `ScreenNavigator.CurrentScreen`, so `NavigateBack()` sees
  `IsCurrentScreen == false` and refuses — logging
  `Only current panel MessageBoxScreen(Clone) can navigate back` and leaving
  the player stuck on the entry screen, pressing OK to get nowhere. Stash the
  message on the calling screen and raise it from `refresh()`, which runs when
  that screen becomes current again (`RefreshOnEnable: 1`).
- **uGUI positions on ScreenSpace-Overlay canvases are in screen pixels**;
  any hit-test radius authored in "pixels" must be multiplied by
  `Canvas.scaleFactor` or it shrinks physically on high-DPI screens (see
  `SectorMapUnitSelector`).
- **`OpenFrontier.LegacyInput` is a shim over the NEW Input System**, not
  `UnityEngine.Input`: `Mouse.current` is null/stale on touch-only devices,
  so `mousePosition` must mirror the primary touch (fixed in the shim —
  keep that behaviour when migrating the remaining ~60 call sites).
- **No Unity native calls in MonoBehaviour field initializers / static
  constructors** (`LayerMask.NameToLayer`, `Shader.Find`, etc.): type
  initialization can run during serialization, where native calls are
  forbidden — the resulting `TypeInitializationException` poisons the
  whole type (every `AddComponent` fails silently). Look them up lazily
  in Awake/OnEnable. The harness cannot catch this; check Editor.log for
  "is not allowed to be called from a MonoBehaviour constructor".
