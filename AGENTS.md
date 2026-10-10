# Notes for Coding Agents

Only non-obvious traps live here — things you cannot learn from the project
structure. Add new ones as they are found.

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

- Regenerate: `python3 /tmp/opencode/gen_harness2.py` (rebuilds csprojs against live `Assets/Scripts`)
- **New `.cs` files are INVISIBLE to the harness until regen** — a stale csproj silently skips them, so their errors only surface in the editor (`MainMenuWorldController.cs` hid three missing-using bugs this way). Always regen after creating files.
- **`gen.sh --regen` ONLY rebuilds `All.csproj`.** The three test projects
  (`handshake-test`, `probe-test`, `codec-test`) keep hand-maintained
  `<Compile Include>` lists, and they pull in *different* subsets — so a new
  protocol message that `All.csproj` compiles fine can still break
  `handshake-test`, which links `ClientSession.cs` and therefore needs the
  message type too. Symptom: `ALL CHECKS FAILED` with **no `error CS`
  anywhere in `check.sh` output** and `codec tests / handshake tests /
  probe tests` reporting only "The build failed". Run
  `dotnet build handshake-test/handshake-test.csproj` directly to see it.
  Add the new file to every test project that already lists a sibling of it,
  and add new test `.cs` files to `codec-test.csproj` explicitly too.
- **The harness CANNOT catch a loop that never terminates.** `while (num > 0)`
  with nothing in the body changing `num` is valid C# and compiles clean in
  both `All.csproj` and the editor — it is a runtime hang, on every launch,
  and it looks exactly like a crash. This happened on 2026-10-10: adding a
  `continue` to `EngineASX.UpdateInactiveUnits` dropped the loop's `num--`
  and froze editor and build before the main menu. **Rule: when editing a
  loop, put the counter next to the read, before any `continue`/`break`, and
  re-read the whole body afterwards.** `grep` the loop for its counter as a
  check — the compiler will not do it.
- Build: `cd /tmp/opencode/harness && dotnet build All.csproj -v q -nologo`

**Known blind spot:** the harness compiles against a UnityEngine *reference stub*,
not the real Unity 6 assemblies. It cannot catch Unity-version-specific API
differences. Example that slipped through (fixed in commit `e330257c`):
`Resolution.refreshRateRatio.value` is a `double` in Unity 6 but `float` in the
stub, so `Mathf.RoundToInt(value)` passed the harness but failed in the editor
with CS1503 (double → float).

**Rule:** a clean harness build is necessary but not sufficient. After any code
change, confirm zero `error CS` lines in the editor's `Logs/Editor.log`
(or ask the user for the Console output) before considering the change done.

**Editor-folder scripts are NOT compile-checked by the harness at all** (it
skips `Editor/` dirs, and only walks `Assets/Scripts`). Anything behind a
platform define (`#if UNITY_ANDROID`) is doubly invisible: the harness sees
an empty file, and the editor won't compile it until the user switches build
target. Verify those by grepping `Logs/Editor.log` for the target switch.

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
