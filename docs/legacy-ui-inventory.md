# Legacy UI Inventory (PreRebuild reference log)

*Compiled 2026-11-09. Source: decompiled code under `PreRebuild/OpenFrontierLogic/` — reference only.
Per AGENTS.md (2026-11-09): new UIs are hand-authored, never restored from PreRebuild. This log
documents what the old game's UIs did and which engine entry points each one drove, so the new
UIs can satisfy the same contracts without copying their layout or code. All engine call names
are quoted verbatim from the decompile; spot-verified samples (MainMenuScreen, CargoTradeHelper,
ManageUnitVariantsScreen) matched exactly.*

## 1. Architecture spine (verified directly from source)

The whole UI is one navigation system over prefab-per-screen instances:

- **`UIController`** (`OpenFrontier.IP/UIController.cs`) — scene singleton. Owns the
  `ScreenNavigator`, its global forward/back navigation audio clips, `HudQuickMsg` toasts,
  `DockUIHeader` (visible only when the current screen is an `EngineScreen` that
  `ShouldShowDockHeader()`), `SavingGameIcon`, and the retired `VersionWatermarkLabel`
  (the main menu's own version text carries `Versioning.Version` + `StageTag`). Start preloads
  `PauseMenuUI` + `MessageBoxScreen`. Key API: `ShowMessageBox(message, buttons = Ok, callback,
  icon = Info, title, alignment)` — reuses the singleton MessageBoxScreen, plus `ShowError`,
  `ShowInsufficientCreditsMessageBox`, `MessageBoxDismiss()`. Also registers itself to
  `GameController.Instance.UIController`.
- **`ScreenNavigator`** (`OpenFrontier.IP.UI.Screens/ScreenNavigator.cs`) — singleton + navigation
  stack. `screenNameMapping: Dictionary<Type, ScreenBase>` maps screen *type* → prefab. Back =
  pop with fade; Escape = back; Return = `AcceptButton`. `PauseWhenShown` freezes `timeScale`
  while a screen is up. Screens are destroyed on back unless kept in memory.
- **`ScreenBase`** (same folder) — virtual hooks: `refresh()`, `update()`, `lateUpdate()`,
  `onMadeCurrentPanel(bool navigatedForward)`, `onNavigatingBack(ref bool cancel)`. `RefreshOnEnable`
  re-runs `refresh()` every time a screen becomes current — the reason behind the AGENTS.md
  "stash the message, raise it from refresh()" pattern. Other knobs: `restrictNavigationAway`,
  `DeactivateType`, `ShowDefaultBackButton`, `KeepInNavigationStack`, canvas-group fade machinery,
  `HelpButton` (help content compiles into a `MessageBoxScreen`), `EnableHelpSystem` + help walk
  over `HelpResourceItem` children.
- **`ScreenNavigationRequest<T>`** — `LoadOnly` (spawn without pushing), `IgnoreIfExistingRequest`,
  `ReuseLoadedExistingScreen` (`ReuseScreenMode`: `Any` / `IfNotInNavigationStack` / `Never`), result
  surfaced as `ScreenResult` / typed `ResultConcrete`. Consumed by `NavigateToScreen`,
  `NavigateToRootScene`, `LoadAndNavigateToScreen`.
- **`ScreenDeactivateType`** — `Disable` (toggle each Canvas enabled) vs `SetActive` (whole GO off)
  when a screen is not current; destroyed only when out of stack and not `KeepInMemory`.
- **`SceneNameMapping`** (`OpenFrontier.IP/UI/SceneNameMapping.cs`) — a MonoBehaviour in the UI
  scene holding **78 serialized `ScreenBase` prefab references**, wired in the Inspector; `Awake`
  pushes each into `ScreenNavigator.ScreenNameMapping[typeof(X)] = prefab`.
- **`AllScreensLoader`** (`OpenFrontier.IP.UI/AllScreensLoader.cs`) — `Start` iterates every mapped
  type not yet loaded, navigates with `LoadOnly = true`, then immediately `SetActive(false)`:
  it **preloads all 78 screen prefabs at menu startup**. That was a big chunk of the old game's
  boot cost — worth deciding explicitly what the new build preloads at all.
- **`ScreenNavigatorExtensions`** (`OpenFrontier.IP/UI/ScreenNavigatorExtensions.cs`, ~1214 lines,
  ~60 `ShowXxxScreen(...)` extension methods) — the canonical launch route used by every screen:
  build `ScreenNavigationRequest<T>`, navigate, assign context fields, `Refresh()`.
- **`EngineScreen`** (`OpenFrontier.IP/UI/EngineScreen.cs`) — ScreenBase + `Eng` accessor,
  `DockUI` passthrough, `ShowDockHeader()`/`ShouldShowDockHeader()` gating.

## 2. Legacy prefab organization (and the pivot for the new build)

- **Screens were never under `Resources/`.** The registry was `SceneNameMapping` — 78 Inspector-wired
  serialized prefab fields on a scene object, keyed by screen class type at runtime. Spawn is
  on-demand through the navigator; `AllScreensLoader` optionally preloads everything.
- New build (per AGENTS.md 2026-11-09 prefab decision): blank-slate authored prefabs live under
  `Assets/OpenFrontier/Resources/Prefabs/UIX/`. A small type-keyed registry mirroring
  `SceneNameMapping`'s shape fits the house pattern; prefer lazy-load-on-first-navigate over the
  old preload-everything loader for mobile startup.

---

## 3. Screen inventory

### 3.1 Main menu / flow cluster

#### MainMenuScreen — *verified by direct read* (`OpenFrontier.IP/UI/MainMenuScreen.cs`)
- **Purpose**: root landing screen — start/end a run or reach every flow screen.
- **Features**: `QuitButton`, `TutorialsButton`, `StoreButton` (hidden unless `Products.IAPEnabled`),
  `NewsButton`, `ContinueGameButton` (shown only when a save header exists), `LoadGameButton`,
  `MultiplayerButton` (multiplayer lives in another assembly — prefab held as `ScreenBase` field),
  `VersionText` (`Versioning.Version + Versioning.StageTag`).
- **Engine entry points** (verbatim):
  - `SaveGameUtilities.CanFindHeaders(out firstSaveGameHeader)` + `header.CanBeLoaded` — polled
    **at 1 Hz** in `update()` to enable/disable Continue.
  - `GameController.Instance.ScenarioLoader.TryLoadScenario(header)` — Continue's actual load path;
    `ScenarioLoader.LoadingScenario` event signals the world to hide.
  - `SkirmishSetupScreen.PlayRandomSkirmish()` — quick-start from the menu itself.
  - `EngineASX.LoadedAndReady` gates fade-in of the whole menu.
  - `SessionNotices.SayPending()` — why the last session ended (world is already gone).
  - Launch-origin redirect (EngineLaunchSource): special launches jump straight to
    Battles / Tutorials / Scenarios / Skirmish instead of resting on the menu.
  - `onNavigatingBack` from the root menu = **`Application.Quit()`** (menu "back" is quit).
- **Navigation**: root screen; → `CreditsUI`, `OptionsScreen`, `GameModeScreen`, `LoadGameScreen`,
  `StoreScreen`.

#### ScenariosUI (`OpenFrontier.IP/UI/ScenariosUI.cs`)
- **Purpose**: scenario list picker (missions; retitled "Tutorials" when opened as tutorials).
  Built on base `ScenarioUI` (shared list/details/play plumbing).
- **Features**: title/details panel, play button, launch-source tagging.
- **Engine entry points**: base `ScenarioUI.PlayScenario(...)` →
  `GameController.Instance.ScenarioLoader.TryLoadScenario(scenarioLoadData)`.
- **Navigation**: opened via `ShowScenariosScreen(launchSource, showTutorials: false,
  showMissions: true)` from GameModeScreen or `ShowTutorialsScreen()` (tutorials title);
  back to GameModeScreen.

#### PauseMenuUI (`OpenFrontier.IP/UI/PauseMenuUI.cs`)
- **Purpose**: in-game pause overlay — resume, save/autosave, quit, options, load.
- **Features**: save confirm/cancel flow, remembers pause state for back-nav, autosave convenience.
- **Engine entry points**: `EngineIO.Save(instance, out errorMessage)`, `Eng.AutoSaveIfPossible()`,
  `UI.QuitToMainMenu()`; on back restores `Eng.IsPaused = wasPaused.Value`.
- **Navigation**: → `OptionsScreen`, `LoadGameScreen`; back returns to the game if it was not
  paused before opening.

#### OptionsScreen (`OpenFrontier.IP/UI/OptionsScreen.cs`, 801 lines)
- **Purpose**: settings — video/quality, audio, gameplay toggles, safe-area entry.
- **Features**: reads/writes player prefs live behind `PlayerOptions` / `AutoSaveSettings`
  bindings; warns about scale-change consequences.
- **Engine entry points**: `GameController.Instance.PlayerOptions.*` / `AutoSaveSettings.*`,
  `PlayerPrefsHelper.SafeGetFloat(..., PlayerOptionConstants.Video_VSyncKey / MasterVolumeKey)`,
  `QualitySettings.SetQualityLevel(...)`, `GameController.Instance.SavePlayerPrefs()`;
  on back → `GameController.Instance.ApplyAllPlayerPrefs()`.
- **Navigation**: `ShowAdjustSafeAreaScreen()`, rename dialogs, `UIScaleScreen`.

#### UIScaleScreen (`OpenFrontier.IP.UI.Screens.UIScale/UIScaleScreen.cs`)
- **Purpose**: adjust overall UI scale (canvas height), slider with live preview.
- **Engine entry points**: `GameController.Instance.CanvasHeight01 = 1f - AdjustmentSlider.value`
  then `ScreenScaler.ScaleScreens()`; `UIController.Instance.MessageBoxScreen.Cancel()`;
  **changed scale applies only after `UI.QuitToMainMenu()`**.
- **Navigation**: from OptionsScreen; back → OptionsScreen.

#### AdjustSafeAreaScreen (`OpenFrontier.IP.UI.Screens.AdjustSafeArea/AdjustSafeAreaScreen.cs`)
- **Purpose**: tune UI safe-area insets (top/bottom/left/right sliders), live preview.
- **Engine entry points**: `GameController.Instance.SetUISafeAreaTopFrom01(...)` (Bottom/Left/Right
  same), `GameController.Instance.ApplyUISafeArea()`.
- **Navigation**: from OptionsScreen; back → OptionsScreen.
- *New-build note: replaced by our SafeAreaFitter (automatic, anchors-based) — no manual slider needed.*

#### LoadGameScreen (`OpenFrontier.IP.UI.Screens.LoadGame/LoadGameScreen.cs`; helper `LoadGameHelper.cs`)
- **Purpose**: browse, load, delete, rename, export save-game slots; works from MainMenu and Pause.
- **Engine entry points** (verbatim): `GameController.Instance.ScenarioLoader.TryLoadScenario(FirstSelectedItem)`,
  `SaveGameUtilities.LoadHeader(path)`, `SaveGameUtilities.GetSavePath(..., ensureUnique: true)`,
  `FileExporter.TryExportFile(...)` / `FileImporter`, `ShowRenameScreen(...)` then
  `File.Move(..., SaveGameUtilities.Extension)`.
- **Navigation**: from MainMenuScreen / PauseMenuUI; load transitions into the scenario.

#### GameModeScreen (`OpenFrontier.IP.UI.Screens.GameMode/GameModeScreen.cs`)
- **Purpose**: mode chooser — Universe, Sandbox, Skirmish, Scenarios/Missions, Tutorials, Battles.
- **Engine entry points**: **none — pure navigation screen.** The contracts live one level deeper.
- **Navigation**: `NavigateToScreen<UniverseSelectScreen>`, `ShowUniverseSandboxSettingsScreen`,
  `NavigateToScreen<SkirmishSetupScreen>`, `ShowScenariosScreen(showTutorials: false,
  showMissions: true)`, `ShowTutorialsScreen()`, `ShowBattlesScreen()`; back to MainMenuScreen.

#### UniverseGameTypeScreen (`...Screens.UniverseGameType/UniverseGameTypeScreen.cs`)
- **Purpose**: choose universe game-type scenario (campaign sub-flavor list); built on `ScenarioUI`;
  store upsell button when a product applies.
- **Engine entry points**: `ScenarioUI.PlayScenario(UniverseScenarioData, launchSource)`,
  `ScreenNavigator.Instance.ShowStoreScreen(requestedProduct)`.
- **Navigation**: from `ShowUniverseGameTypeScreen(...)` (UniverseSelect / Sandbox settings).

#### UniverseSelectScreen (`...Screens.UniverseSelect/UniverseSelectScreen.cs`)
- **Purpose**: thin selector step of universe flow before game-type screen.
- **Engine entry points**: none — forwards `ScenarioList.FirstSelectedItem`.
- **Navigation**: from GameModeScreen → `ShowUniverseGameTypeScreen(ScenarioList.FirstSelectedItem)`.

#### UniverseSandboxSettingsScreen (`...Screens.UniverseSandboxSettings/`)
- **Purpose**: configure a sandbox universe: seed settings, sector-name pool, generate;
  blueprint preview, numeric entry for seed/index.
- **Engine entry points** (verbatim): `UnityObjectHelper.InstantiateAndGetComponent(GameController.Instance.GameSettings.DefaultWorldSeedSettings)`,
  `WorldBlueprintGenerator().Generate(seedSettings.WorldGeneratorSettings, GameController.Instance.CustomSectorNames)`.
- **Navigation**: forward `ShowUniverseGameTypeScreen(SandboxScenarioInfo, seedSettings)`,
  `ShowUniverseBlueprintScreen(...)` preview, `ShowEnterNumberScreen(...)`; back to GameModeScreen.

#### SectorPickerScreen (`...Screens.UniverseScenePicker/SectorPickerScreen.cs`)
- **Purpose**: generic sector chooser list; raises `ScenePicked` event; no engine logic.

#### SkirmishSetupScreen (`...Screens.Skirmish/SkirmishSetupScreen.cs`) — ScenarioUI subclass
- **Purpose**: configure and launch instant-action skirmishes.
- **Features**: random ship sets / team counts (per-team PlayerPrefs `skirmish_team{NN}_{NN}`),
  "visited" tip flag `visited_skirmish_setup`, battle-mode choice, per-team edit transition.
- **Engine entry points** (verbatim): `GameController.Instance.GetInstantActionsShipSets().GetRandom()`,
  `GameController.Instance.ScenarioLoader.TryLoadScenario(scenarioData)` (via
  `PlaySkirmish(SkirmishScenarioData)`), `CustomUnitVariantIO.LoadCustomVariantShips()`,
  `SkirmishHelper.GetAvailableShips(...)`.
- **Navigation**: from GameModeScreen; team edit → `ShowSkirmishTeamSetupScreen`;
  → `ShowManageCustomUnitVariantsScreen`.
- **Ties to new menu**: `SkirmishSetupScreen.PlayRandomSkirmish()` is what the legacy main menu
  quick-start actually invoked.

#### SkirmishTeamSetupScreen (`...Screens.Skirmish.SkirmishTeamSetup/`)
- **Purpose**: edit one skirmish team's roster (add/remove available ships, ship info popup).
- **Engine entry points**: `SkirmishHelper.GetAvailableShips(...)`; roster handed back via
  `skirmishSetupUI.SetTeamShips(editedTeamIndex, SelectedShipList.ActiveItems)`.
- **Navigation**: from SkirmishSetupScreen; ship selection → `ShowUnitInfoScreen(CurrentShipType.UnitClass.UnitPrefab)`.

#### BattlesScreen (`...Screens.Battles/BattlesScreen.cs`) — ScenarioUI subclass
- **Purpose**: list/play ready-made battle scenarios; shares the `ScenarioUI` details panel
  (`BattlesScenarioDetails.cs` is a data class, not a screen).
- **Engine entry points**: override `GetLoadData(...)` → base
  `ScenarioUI.PlayScenario(...)` → `GameController.Instance.ScenarioLoader.TryLoadScenario(scenarioLoadData)`.
- **Navigation**: from GameModeScreen `ShowBattlesScreen()`.

#### MissionScreen (`...Screens.ActiveMission/MissionScreen.cs`)
- **Purpose**: active-mission tracker HUD panel — mission details, waypoint UI, abort/complete.
- **Engine entry points** (verbatim): `Mission.AbortMission()`, `Mission.CanPlayerManualComplete()` /
  `Mission.ManualComplete()`, `Eng.LocalPlayer.ActiveMission = Mission`,
  `Eng.LocalPlayer.WaypointController.UpdateMissionPathWaypoints()`, `Eng.SetUIFromPlayerStatus()`.
- **Navigation**: overlay opened by `ShowActiveMissionScreen(mission)`.

#### CinematicScreen (`OpenFrontier.IP.OpenFrontier.Engine/CinematicScreen.cs`)
- **Purpose**: cutscene/letterbox overlay with `RollingTextUI` narration and gesture input capture;
  lives in the engine module, not the UI screens tree. Loaded LoadOnly by `EngineASX` and kept
  deactivated until an in-engine cinematic plays.

#### HypersleepScreen (`...Screens.Hypersleep/HypersleepScreen.cs`)
- **Purpose**: hypersleep (time-skip travel) overlay — availability gating with reason text,
  order/statistics display, time-multiplier controls with persistence.
- **Engine entry points** (verbatim): `HypersleepHelper.IsHypersleepAvailable(out reason)`,
  `OrdersHelper.GetFleetCurrentOrder(...)` / `OrdersHelper.IsPilottedByNpc(...)` /
  `OrdersHelper.GetOrdersTextAndFleetStatus(...)`, `DockUIHeader.GetLocationTextNonAlloc(...)`,
  `EngineASX.Instance.SetUIFromPlayerStatus(forceExitFromHypersleep: true)`,
  `GameController.Instance.SaveHypersleepTimeMultiplier()`; on forward entry clears
  `EngineASX.Instance.ActiveSector = null` and multiplies `Time.timeScale`.

#### CreditsUI (`OpenFrontier.IP/UI/CreditsUI.cs`)
- **Purpose**: credits display; **code-behind intentionally empty** — content is
  `CreditsAnimation` (main-menu module). From MainMenuScreen and back.

#### EndGameUI (`OpenFrontier.IP/UI/EndGameUI.cs`)
- **Purpose**: end-of-game/result panel (game over) — exit (optionally continue scenario), retry.
- **Engine entry points**: `Eng.World.NotifyPlayerRequireExit(continueScenario)`,
  `Eng.World.NotifyPlayerRequireRetry()`.
- **Navigation**: opened via `ShowEndGameScreen(request)` → `NavigateToRootScene<EndGameUI>` —
  it is the **root screen** for end-of-session flow.

#### StoreScreen (`OpenFrontier.IP.UI.Screens/StoreScreen.cs`)
- **Purpose**: in-game store — product list, purchase, restore purchases; optional focus product
  for upsell entry points.
- **Engine entry points**: `GameController.Instance.PurchaseBridge.InitialisePurchaser()`,
  `.Purchaser.RestorePurchases()`, `.AnyProductsUnurchased()`,
  `.Purchaser.BuyProductID(Product.Id)` (child rows: `PurchaseBanner`, `StoreButtonBuyProduct`).

#### TopPilotsScreen (`...Screens.TopPilots/TopPilotsScreen.cs`)
- **Purpose**: leaderboard of pilots ranked by kills; per-entry `UnitContextButton`.
- **Engine entry points**: reads `EngineASX.Instance.People` (kills-ordered), gated by
  `EngineASX.Instance.LocalFaction.Intel.IsUnitDiscovered(unit)`.

#### JobBoardScreen (`OpenFrontier.IP/UI/JobBoardScreen.cs`)
- **Purpose**: dock job board — take contract jobs while docked; offer limit guarded by
  `Eng.GameSettings.MaxDynamicMissions`.
- **Engine entry points**: `MissionSpec.CreateMission(EngineASX.Instance.LocalFaction)`,
  `mission.MakeActiveMission()`, `Eng.SetUIFromPlayerStatus()`.

#### BountyScreen (`...Screens.Bounty/BountyScreen.cs`)
- **Purpose**: faction bounty board — list bounty targets, jump/edit waypoint.
- **Engine entry points**: `Eng.LocalPlayer.SetCustomWaypointToSectorPosition(...)`,
  `ShowSectorMapScreenShowingSectorPosition(...)`,
  `EngineASX.Instance.LocalFaction.Intel.DiscoverSector(activeItem.LastKnownSector)`.

#### HelpSystem folder (`...Screens.HelpSystem/`) — help delivery machinery
- `HelpResource` (title + description container), `HelpResourceItem` (one help step pointing at a
  `targetTransform` or `CustomHelpTransform`; nested children), `TargettedHelpButton`
  (`CurrentScreen.ShowHelp(TargetTransform, IncludeScreenPrimaryHelp)`).
- Flow: `ScreenBase.HelpButton` → `ShowHelp()` walks the children, compiles one message, and shows
  it as a single `UIController.Instance.ShowMessageBox(..., Ok, Info)` dialog. Help buttons are
  `ScreenBase` chrome on any screen.

### 3.2 Galaxy / ships / fleet cluster

#### SectorMapScreen (`...Screens.SectorMap/SectorMapScreen.cs`)
- **Purpose**: in-sector tactical map (ships, asteroids, stations, selection, pan/zoom) — the
  main flying overlay map.
- **Features**: delegates to `SectorMapForm`/`SectorMap` widgets; `RestrictNavigationAway()` mode
  (hides nav-away, shows static target info, hides dock header, disables context menu — used by
  patrol creator / position picks); back button only when a back target exists (can be root).
- **Engine entry points**: none directly — engine access lives in the widgets it holds
  (`SectorMap.OnShown()`, `SectorMapForm.Tick()/Refresh()`).

#### UniverseMapScreen (`...Screens.UniverseMap/UniverseMapScreen.cs`)
- **Purpose**: galaxy map — pan/zoom over sectors, jump connections, select sector, fly/waypoint
  there; also reused as a sector picker (callback mode) and patrol-route sector chooser.
- **Features**: sector item grid + connection prefabs, zoomer, selected-sector info panel
  (name/ownership/home HQ factions/security), buttons: ViewSectorMap, FlyToSector, SetWaypoint,
  RenameSector, ViewProperty, ViewFleets, Restrict/Allow autopilot navigation, Confirm selection,
  CenterOnOrigin; `IgnoreIntel`/`CustomDisplayedSectors` variants; per-frame intel-driven refresh.
- **Engine entry points** (verbatim): `OrdersHelper.OrderMoveToSectorTarget(OrdersHelper.FindOrCreateNpcAndFleetAndPilotShip(EngineASX.Instance.PlayerUnit), SectorTarget, stack:false)`,
  `LocalPlayer.SetCustomWaypointToSectorPosition(sector, Vector3.zero, autoRemove:false)`,
  `LocalFaction.Intel.IsSectorDiscovered/IsUnitDiscovered/HasWormholeBeenEntered`,
  `LocalFaction.AutopilotExcludedSectors.Add/Remove`,
  `Sector.GetSectorSecurityLevelDescription/ContainsPlayerProperty/ContainsPlayerFleets/IsControlledByPlayer/MapPosition/Neighbours`,
  `Faction.GetCachedNetWorth/IsHostileToOrAlwaysHostileTo`,
  `EngineASX.GetSecurityColor/GetFactionHostilityColor/AttitudeNeutralColor`, `Eng.Sectors`.
- **Navigation**: to SectorMapScreen, PropertyScreen, FleetsScreen, RenameUnitScreen (sector
  rename); can be root; also pushed by PatrolOrderCreatorScreen and NewFleetOrder flows.

#### UniverseBlueprintMapScreen (`...Screens.UniverseBlueprintMap/`)
- **Purpose**: world-generator debug/authoring view of a `WorldBlueprint` — regenerate seed,
  tweak, inspect drawn blueprint.
- **Engine entry points**: `UIController.Instance.ScreenNavigator.ShowEnterNumberScreen(...)`;
  regeneration via injected `RegenerateCallback` (Func&lt;WorldBlueprint&gt; — provided by the
  world-seeding layer).
- (`Screens.UniverseBlueprint/` = widgets only: `UniverseBlueprintDrawer`, `UniverseBlueprintMapItem`,
  `UniverseBlueprintSectorIcons`.)

#### SectorPositionContextScreen (`...Screens.SectorPositionContextMenu/`)
- **Purpose**: tiny context popup when the player taps empty map space: set a custom waypoint
  here, or order the player's NPC-piloted ship to fly here.
- **Engine entry points**: `OrdersHelper.OrderMoveToSectorTarget(OrdersHelper.FindOrCreateNpcAndFleetAndPilotShip(EngineASX.Instance.PlayerUnit), SectorTarget, stack:false)`,
  `OrdersHelper.CanPlayerOrderUnit(EngineASX.Instance.PlayerUnit)`,
  `EngineASX.Instance.LocalPlayer.SetCustomWaypointToSectorPosition(SectorTarget.Sector, SectorTarget.SectorPosition, autoRemove:true)`;
  both buttons then `NavigateBack()`.

#### ChangeSectorAppearanceScreen (`...Screens.Test/ChangeSectorAppearanceScreen.cs`)
- **Purpose**: live sector-skybox tweaker (OpenFrontier-modified debug screen): reseeds nebula
  count / star intensity and sun HSV.
- **Engine entry points**: `Sector.GenerateSpaceBackgroundWithNewSeed()`,
  `Sector.GenerateSpaceBackground()`,
  `Sector.GetOrCreateCustomAppearanceSettings()/RemoveCustomSectorAppearanceSettings()/HasCustomAppearanceSettings/RandomSeed/DirectionLightColor`,
  `SpaceConstructor.StaticStars.starsIntensity/UpdateMaterial()`,
  `StarColorGenerator.ForSector/HsvToRgb/RgbToHsv/NormalizeStarColor`,
  `SpaceReflectionProbe.Instance.RequestRender()`, `EngineASX.CameraOrbitPlayerUnit()`,
  `SunBillboard.StarTint`.

#### ShipScanScreen (`...Screens.ShipScan/ShipScanScreen.cs`)
- **Purpose**: read-only scan report of any selected unit: what it is, what it carries, who pilots it.
- **Features**: render image, faction context button, panels — Cargo list, Components list,
  Docked ships list, CargoFactory items, Asteroid (yield/types), pilot + current-order text,
  passengers count, Hull/Cargo/Capacitor sliders, `UnitConditionControllerUI`, DockingBay button.
- **Engine entry points** (verbatim): `OrdersHelper.GetOrdersTextAndFleetStatus(Unit, EngineASX.Instance.LocalFaction)`,
  `Unit.GetFriendlyName()/HasAnyCargo()/IsStationOrShip()/IsDockable`, `Unit.Asteroid.RemainingYield`,
  `Asteroid.AsteroidClass.AsteroidYieldItems`, `Unit.Components.*`
  (CargoBayComponent/Capacitor/PassengerCapacity/FactoryComponent/HangarComponent),
  `DockFacilitiesUI.ShouldShowBuyShipButton`.
- **Entry gating**: `EngineASX.Instance.OnUnitScanned(Unit, scanner)` after
  `WorldHelper.CanPlayerScanUnit`.

#### RepairUI (`OpenFrontier.IP/UI/RepairUI.cs`)
- **Purpose**: docked repair shop — per-part and repair-all hull/component repairs plus shield
  recharge at the docked yard.
- **Engine entry points** (verbatim): `RepairHelper.GetComponentRepairCost/GetHullRepairCost/GetCostToRechargeShield(dockFaction, faction, dockUnit, unit)`,
  `DockUI.PlayerDockUnit/PlayerCurrentUnit/AreRepairsNeeded(componentHolder)`,
  `Eng.RegisterTaxedPlayerTrade(DockUI.PlayerDockUnit, -cost, FactionTransactionType.ShipRepairs)`,
  `Unit.Destructable.RestoreHealth()`, `ComponentBase.RestoreHealth()`,
  `ShieldComponent.RechargeFull()`, `Eng.LocalPlayer.Credits/Faction`.

#### UnitInfoScreen (`...Screens.UnitInfo/UnitInfoScreen.cs`)
- **Purpose**: specification sheet for a ship class/unit — description, manufacturer, stats,
  cargo, component bays, hangar bays, factory profile; rotating 3D `ActiveUnitPreviewerUI`.
- **Features**: panel-mode tabs (`PanelMode`: Info/Stats/ComponentBays/Cargo/HangarBays/Factory),
  `ShipStatsGenerator`, bay/hangar/cargo/factory lists.
- **Engine entry points**: `EngineASX.Instance.EngineResources.GetUnitClassRenderSpriteOrDefault(UnitClass)`,
  `EngineASX.Instance.GameSettings.TextHeadingColor`, `UnitClass.UnitSeries.Description/UnitPrefab.ClassName/Manufacturer.GetShortNameElseLong()`,
  `ShipBuyScreen.GetUnitClassification(Unit)`, `Unit.GetComponent<UnitHangar>()/GetComponent<UnitCargoFactory>()`,
  `Unit.Components.Bays`. Terminal info panel.

#### UnitComponentsScreen (`OpenFrontier.IP/UI/UnitComponentsScreen.cs`)
- **Purpose**: manage the current unit's component bays: power on/off, turret auto-fire modes,
  change ammo.
- **Engine entry points**: `Unit.Components.InitAutoTurretModuleIfNull()`,
  `.AutoTurretModule.FireMode = AutoTurretFireMode.*`, `TurretComponent.AutoFire = /CanAutoFire()`,
  `ProjectileTurretComponent.TrySelectNextProjectileClass(ignoreAmmo:true, conventionalWeaponsOnly:false)/GetNextProjectileClass/UsesAmmo/CurProjectileClass.AmmoClass`,
  `UnitComponents.GetCargoCountOf(ammoClass)`, `Unit.IsPilottedByPlayer()`.

#### UnitClassPickerScreen (`...Screens.UnitClassPicker/`)
- **Purpose**: modal "pick a ship class" list for the custom-variant wizard; event
  `UnitClassPicked(UnitClass)`; default list filters `UnitType.Ship, ShipType.Normal, IsUsable,
  SeededInSandbox, !IsBarebonesShip` ordered by series/price; `KeepInNavigationStack=false`.

#### UnitPickerScreen (`...Screens.UnitPicker/`)
- **Purpose**: generic "select a unit" modal over a caller-supplied unit list, optional "[None]"
  row; event `UnitPickedResult(sender, result, item)`; pure data-passer.

#### RenameUnitScreen (`...Screens.RenameUnit/RenameUnitScreen.cs`)
- **Purpose**: generic text-entry naming dialog (ships, fleets, sectors, pilots) with min/max
  chars and optional `RegexPatternValidation`; event `RenameConfirmed(handler, rename, newName)`.
- **Navigation**: pushed from UnitContextMenuScreen (unit + pilot rename), FleetsScreen (fleet),
  UniverseMapScreen (sector) — above those callers.

#### DockedScreen (`OpenFrontier.IP/DockedScreen.cs` — global namespace, IP assembly root)
- **Purpose**: the docked-hub screen — current ship header (`CurrentUnitUI`) plus dock facilities
  panel (`DockFacilitiesUI`) while on station; camera orbit on becoming current;
  `Eng.DockUI.PlayerCurrentUnit` polled each update.
- **Navigation**: **root of the docked stack** — `ShowUnitMainScreen()` does
  `NavigateToRootScene<DockedScreen>`; nearly every dock sub-screen sits above it.

#### DockedShipsScreen (`...Screens.DockedShips/DockedShipsScreen.cs`)
- **Purpose**: ships docked in a hangar: inspect, brief, undock one, transfer into it
  (listens `UnitHangar.DockedUnitsChanged`).
- **Engine entry points** (verbatim): `DockUI.CanUndockShip(holder)`, `DockUI.UndockShip(unit.Components)`,
  `DockUI.PlayerCurrentUnit` + `ShowCargoTransferScreen`; `WorldHelper.CanEnterUnitFromCurrentUnit/CanTransferCargoToUnit`;
  `Eng.LocalPlayer.Person.CurrentUnit = unit` + `Eng.PlayChangeShipAudio()` + `Eng.SetUIFromPlayerStatus()`;
  `Eng.IsPaused = false` on undock; `HudScreen.CanShowOrdersForUnit(Unit)`;
  `CommsHelper.TryGetCommsHandlerFromUnit`.
- **Navigation**: to OrdersScreen, CargoTransferScreen, UnitContextMenuScreen; auto-back
  "Lost contact with target" when the hangar dies.

#### FleetsScreen (`...Screens.Fleets/FleetsScreen.cs`)
- **Purpose**: player fleet manager — sortable filterable list of all fleets/ships with a
  per-selection command menu (ViewFleet, Orders, RenameFleet, DisbandFleet, SplitFleet, NewOrder,
  ClearOrders, EnterFleet, FleetCargo, FleetSettings).
- **Engine entry points**: `FleetsHelper.GetPlayerFleets(showSingleShips)/ShouldShowFleet/ViewFleet`,
  `FleetUtils.SplitFleet(Fleet, EngineASX.Instance.PlayerFleetPrefab)`, `FleetUtils.CanSplitFleet`,
  `OrdersHelper.DisbandPlayerFleet(fleet)`, `Fleet.ClearOrders()/HasAnyOrders/Sector/Name`,
  `EngineASX.ChangePlayerUnit(leaderUnit)`, `NewOrderTarget.CreateFromFleets(items)`.
- **Refresh convention**: full refresh ≤15 s, visible items ≤1 s, validity check ≤2 s.

#### FleetSettingsScreen (`...Screens.FleetSettings/`)
- **Purpose**: per-fleet (or multi-fleet) doctrine editor: advanced orders + combat stances;
  save/restore defaults, apply-to-all-fleets.
- **Engine entry points**: `CachedFleetSettingsController.CreateFromFleet(fleet).IsSameAs(...)/ApplyToFleet(...)`,
  `EngineASX.CachedFleetSettingsController.SetDefaultFleetSettings/RestoreFleetSettingsToDefault/HasDefaultFleetSettings/DefaultFleetSettings`,
  `Fleet.SetFormationPositions()`, `ChangeFleetHomeBase.Change(Fleets, callback)`.

#### FleetFormationStylePickerScreen (`...Screens.FleetFormations/`)
- **Purpose**: small picker for a fleet formation style; `Callback(sender, style)`.
- **Engine entry points**: reads `GameController.Instance.GameSettings.FormationSettings.FormationStyles`.
- *No separate fleet-formations screen — formation UI lives inside FleetSettingsScreen;
  the rest of that folder is widgets (`FleetFormationStyleItem/List`).*

#### FleetPickerScreen (`...Screens.FleetPicker/`)
- **Purpose**: "select fleet…" modal (merge/protect targets, property assignment); event
  `FleetPickedResult(sender, result, item)`; no engine calls.

#### NewFleetOrderScreen (`...Screens.NewFleetOrder/NewFleetOrderScreen.cs`)
- **Purpose**: order-issuing palette for a fleet/unit selection: category pages
  (Combat/Trade/Navigation/Special) of ~50 order buttons aimed at a `NewOrderTarget`.
- **Engine entry points**: `Fleet.AssignNextOrderIfNoCurrentOrder()`; the actual orders execute
  inside each OrderButton (OrdersHelper.* per button); reads
  `NewOrderTarget.OrderedFleets/UnitsWithoutFleet/MixedUnitsAndFleets`.

#### OrdersScreen (`...Screens/OrdersScreen.cs`)
- **Purpose**: single fleet's order board — active + stacked orders, its ships, new/clear orders,
  fleet settings, fleet cargo; 1 Hz throttled refresh.
- **Features**: ship list (`OrdersScreenShipList`, ordered leader→combat-rating) with
  `UnitContextButton`, prefetched `FleetOrderListItem`s sized to
  `GameSettings.MaxPlayerFleetStackedOrders` + delete events, current-location label.
- **Engine entry points** (verbatim): `orderedFleet.OrderQueue.Contains/Remove`, `FleetOrder.SafeDestroy(orderedFleet)`,
  `ActiveFleetOrder.SafeDestroy()`,
  `Fleet.ActiveOrder/HasAnyOrders/IsValid/LeaderUnit/OrderQueue/Ships/GetSectorAndLocationText()/GetFriendlyName()`,
  `OrdersHelper.OrderStop(leader)` (multiplayer: **host owns orders**),
  `OrdersHelper.CanStackOrderOn(fleet)`, `Authority.IsThisCopyAClient`.

#### PatrolOrderCreatorScreen (`...Screens.Orders.PatrolOrderCreator/`)
- **Purpose**: visual patrol-route builder — click map positions for waypoints (64 max), optional
  loop/repeat, confirm as a Patrol order payload; back-guard confirm.
- **Engine entry points**: `SectorTarget.FromSectorPosition(sector, position)`; map interaction via
  `SectorMap.CustomSelectionFilter/SectorMapCurrentTargetController.OnSelecting`; result consumed
  by PatrolOrderButton (which issues the order). Pushes UniverseMapScreen for cross-sector picks.

#### FleetOrderSettingsScreen (`...Screens.FleetOrderSettings/`)
- **Purpose**: per-order tuning: max-duration (log slider 10 s–12 h), requeue-on-completion,
  (un)limited credits spend.
- **Engine entry points**: `FleetOrder.MaxDuration/HasUnlimitedSpend/SetAsRestrictedCreditsSpend(Fleet)/CanSpendCredits/IsRepeatable/GetDescriptionForFaction(LocalFaction, null)`,
  `LocalFaction.ApplyTransaction(creditsValue, FactionTransactionType.FleetTransfer, null, Fleet.LeaderUnit)`,
  `EngineASX.DateTimeUtils.GetShortTimespanDescriptionFromRealSeconds`.

#### FleetCargoScreen (`...Screens.FleetCargo/FleetCargoScreen.cs`)
- **Purpose**: aggregate cargo view of a whole fleet: what each ship holds, ejection, info/prices.
- **Engine entry points**: `Fleet.GetCargoCountOf(cargoClass)/CalculateCargoUsage()/CalculateTotalCargoCapacity()/IsValid/Ships`,
  `UnitComponentHolder.GetCargoCountOf`, `UnitComponentHolder.CargoBayComponent.GetCountOf/EjectCargo`,
  `Fleet.GetFriendlyNameNoPrefix()`, `Eng.EngineResources.GetCargoSpriteOrDefault(cargoClass)`,
  `CargoClass.IsReserved`.
- **Navigation**: to CargoPricesScreen, CargoInfoScreen; pushed from FleetsScreen, OrdersScreen,
  FleetPopupMenu (`ShowFleetCargoScreen(fleet)`); auto-refresh by `CargoBayComponent.LastChangedTime`.

#### PassengersScreen (`OpenFrontier.IP/UI/PassengersScreen.cs`)
- **Purpose**: passenger freight panel — waiting groups at a dock/unit vs groups riding on your
  ship; pickup (capacity + module checks; sets waypoint to destination) and eject.
- **Engine entry points** (verbatim): `PassengerManager.RemoveInvalidOrExpiredPassengerGroupsAtUnit(unit)`,
  `EngineASX.Instance.PassengerManager.PickupPassengers(group, currentUnit)`,
  `LocalFaction.Intel.DiscoverUnit(group.Destination)`,
  `Unit.Components.PassengerModule/.PassengerCount/.PassengerCapacity/.FreePassengerSpace`,
  `ActiveTransportPassengersOrder.ValidateAndClearIfInvalid()`, `DockUI.CanUnitTrade(unit)`,
  `Eng.LocalPlayer.SetCustomWaypointToUnit(destination)`,
  `GameController.Instance.EjectPassengerAudioSource`.

#### RequestTaxiScreen (`...Screens.RequestTaxi/RequestTaxiScreen.cs`)
- **Purpose**: "taxi" = instant unit change to any dockable discovered station anywhere;
  two-stage lists (discovered sectors by `GetJumpDistanceTo` → their dockable stations).
- **Engine entry points**: `LocalFaction.Intel.GetCopyOfDiscoveredSectors()`,
  `Intel.IsUnitDiscoveredOrOwned(unit)`, `Sector.GetJumpDistanceTo(Sector)`,
  `Sector.GetUnitsByType(UnitType.Station)`, `Faction.RequestDock(unit, playerFaction)`
  (denied → "Docking denied" quick message), `Eng.ChangePlayerUnit(station)`,
  `Eng.PlayChangeShipAudio()`, `DockUI.PlayerRootUnit` exclusion.

#### BuySectorIntelScreen (`...Screens.BuySectorIntel/`)
- **Purpose**: at a dock, buy a package revealing undiscovered stations in the current sector.
- **Engine entry points** (verbatim): `Faction.Intel.GetUndiscoveredUnitsInScene(sector)`,
  `Intel.DiscoverUnit(unit)`, `IntelHelper.GetCostToDiscoverUnits(units, faction)`,
  `Eng.RegisterTaxedPlayerTrade(unit.GetRootUnit(), -cost, FactionTransactionType.MiscPurchase)`,
  `IntelHelper.AddIntelPurchasedQuickMessage()`, `CurrentLocation.Sector`.

#### BuySectorIntelConfirmScreen (`...Screens.BuySectorIntelConfirm/`)
- **Purpose**: confirmation/list of exactly which units the intel purchase revealed, with per-unit
  waypoint toggles (`ToggleWaypointButtonController`); no OK/back buttons of its own.

#### UnitContextMenuScreen (`...Screens.UnitContextMenu/UnitContextMenuScreen.cs`)
- **Purpose**: the all-purpose context menu for any selected unit/target — the hub routing to
  info, orders, cargo, trade, scan, bounty, rename, etc.
- **Features**: unit render image + name, pilot block (rename), faction block (`FactionContextButton`),
  Hull/Capacitor/Cargo sliders, `UnitConditionControllerUI`, `UnitPathIconsDisplay`; buttons gated
  per state: SetWaypoint, View, Orders, Enter, Info, Cargo, CargoTransfer, Components, Comms,
  Upgrade, Passengers, Scan, BuyShip, SelfDestruct, Dismantle, Rename, PayBounty,
  `DockingBayButtonController`.
- **Engine entry points** (verbatim, abridged): `BountyHelper.PayOffBountyItem(engine, item, faction)`,
  `EngineASX.GetBountiesOnPerson(pilot)`, `WorldHelper.CanPlayerScanUnit/ShouldShowCommsButton/CanTransferCargoToUnit/CanPlayerViewCargo/CanEnterUnitFromCurrentUnit/AllowToggleWaypoint/EnterUnit(unit)`,
  `EngineASX.OnUnitScanned/CreditsAnimation.AllowCreditBeepAudio/CanPlayerSelfDestructUnit/CanPlayerRenameUnit/CanPlayerRenameUnitNpcPilot`,
  `Unit.Destructable.KillUnitAsPlayer()`, `Unit.Components.StartDismantle()`,
  `Unit.CanDismantleNow(LocalFaction)/CouldBeSelfDestructed()/CouldBeDismantled()/HasAnyCargo()/GetRenderSprite()/GetFriendlyNameForLocalFaction()`,
  `Faction.RequestDock(unit, faction)`, `Faction.GetOrCreateAttitude`, `CommsHelper.OpenCommsWithHandler(handler, faction)`,
  `ComponentUpgradeHelper.CanUpgradeUnit(unit)`, `OrdersHelper.CanPlayerOrderUnit(unit)`,
  `Person.ClearGeneratedName/CustomName/RefreshName/GetNameAndTitleOrFullRank/GetPilotExtraInfoText`,
  `Eng.LocalPlayer.ToggleCustomWaypoint(unit)`, `GameController.GameSettings.PilotNameMin/MaxChars`.
- **Navigation**: forward to UnitInfoScreen, OrdersScreen, UnitCargoScreen, CargoTransferScreen,
  UnitComponentsScreen, EquipmentTradeScreen (upgrade), PassengersScreen, ShipScanScreen,
  RenameUnitScreen (unit + pilot), SectorMapScreen (View unit), ShipTradeScreen (BuyShip),
  FactionsScreen; embedded wherever `UnitContextButton`/`UnitPopupMenu` lives.

*(FoW sweep: `Screens.UniverseMap/` and `Screens.SectorMap/` are widget-only beyond their screens
listed above — `UniverseMap*Zoomer/ConnectionItem/ItemUI`, `SectorMap*Form/UnitItem/Zoomer/SceneDrawer`.)*

### 3.3 Cargo / economy / trade cluster

#### CargoPickerScreen (`...Screens.CargoPicker/CargoPickerScreen.cs`)
- **Purpose**: modal picker for one cargo class plus quantity via slider; result returned through
  a `Finished(this, CargoPickerResult)` event to the caller, not via navigation result.
- **Features**: `CargoPickerList`, `CargoAmountSlider` with min/max from item, Qty label, OK/Cancel,
  `AutoSetCargoAmountSliderToMax` / `AllowPickQuantity` flags.
- **Engine entry points**: `TextFormattingHelper.FormatCargoAmount(...)` only; list data comes
  from callers. Shown `keepInNavigationStack: false`.

#### MultiCargoPickerScreen (`...Screens.MultiCargoPicker/`)
- **Purpose**: multi-select cargo-class picker; returns selected set via
  `Finished(this, MultiCargoPickerResult)`; no quantity control. Pure list plumbing.
- **Navigation**: from Orders buttons (SellCargoTypeOrderButton, TradeCargoTypesOrderButton).

#### CargoTransferScreen (`...Screens.CargoTransfer/CargoTransferScreen.cs`)
- **Purpose**: ship-to-ship cargo transfer between two units; pick an item on either side, set a
  signed transfer amount, execute.
- **Engine entry points**: `CargoBayComponent.GetCountOf(...)` / `.LastChangedTime`;
  `CargoTransferUnitController.DoTransfer(...)/CanDoTransfer(...)`;
  `Eng.GameSettings.CanTransferCargoFromAnyDistance / .MaxTransferCargoDistance`;
  `Unit.IsValidAndNotDestroyed`; `UIController.Instance.QuickMsg.AddMessage(...)`.
- **Navigation**: back only; pushed by `ShowCargoTransferScreen(unit1, unit2)` from
  DockedShipsScreen, TargetInGameMenu, DynamicCargoTransferButtonController, UnitContextMenuScreen.

#### FleetCargoScreen — see §3.2.

#### UnitCargoScreen (`...Screens.UnitCargo/UnitCargoScreen.cs`)
- **Purpose**: the player unit's cargo bay: eject cargo and deploy deployable cargo (stations).
- **Engine entry points** (verbatim): `Unit.GetCargoCountOf(...)`,
  `CargoBayComponent.EjectCargo(...)/.GetCountOf(...)/.CanChangeCargo(...)/.Usage`,
  `DeployableUnitCargoClass.DeployUnit(...)/.CanDeploy(...)`,
  `PhysicsNonOverlappingPositionFinder.FindSectorPosition(...)`,
  `UnitClass.DisplayData.Radius`, `Unit.IsDocked/.IsFullyDecloaked`, `Sector.HasPlanets`,
  `UIHelper.GetCargoItems(...)`, `Eng.GameSettings.CanEjectCargoWhileDocked`.
- **Navigation**: to CargoInfoScreen, CargoPricesScreen; back on deploy; pushed from
  ShipMenuController, UnitPopupMenu, CurrentUnitUI, PropertyUnitPopupMenu, UnitContextMenuScreen.

#### CargoInfoScreen (`OpenFrontier.IP/UI/CargoInfoScreen.cs`)
- **Purpose**: detail/preview card for one cargo class (icon, description, stats) with linked
  station info for deployables; prev/next cycling while a CargoTradeScreen pair is live.
- **Engine entry points**: `CargoClass.RelatedPrefab.GetComponent<Unit>()`,
  `UnitClass.GetClassAndSeriesName()`, `Eng.EngineResources.GetCargoSpriteOrDefault(...)`,
  `CargoClass.IsDeployable`.
- **Data flow**: `static CargoClass ShowItem` handoff field.

#### CargoTradeScreen (`...Screens.CargoTrade/CargoTradeScreen.cs`)
- **Purpose**: docked cargo market — sortable list of everything the trader buys/sells, opening
  each row into the trade panel.
- **Features**: `TradeItemListUI` + `TradeMenuColumnHeader` sort registry
  (Name/BuyPrice/SellPrice/ShipQuantity/DockQuantity), `TraderFilterCargoType` filter cycling,
  `ShowCompatibleAmmoOnlyToggle`, two `AdvancedCargoUsageSlider`s, low-credits warning, stale-flag
  refresh on `CargoBayComponent.CargoAddRemove` event.
- **Engine entry points** (verbatim): `CargoTradeHelper.GetBuyPrice(...)/.GetSellPrice(...)`,
  `Unit.Components.IsCargoClassCompatibleWithAnyComponent(...)`, `dockUnit.GetComponent<CargoTrader>()`,
  `EngineASX.Instance.LocalFaction`, `Faction.Credits`, `Unit.GetDockUnit()`,
  `Eng.SetUIFromPlayerStatus()`, `UnitNamer.GetNameAndFactionLongNameInParenthesisForPlayer(...)`,
  `Maths.WrapValue(...)`.

#### CargoTradeItemScreen (`OpenFrontier.IP/UI/CargoTradeItemScreen.cs`)
- **Purpose**: per-cargo buy/sell table for the docked trade: pick quantity with a power-curve
  slider (negative = sell side), see price/cost/usage, execute.
- **Features**: +/-/BuyMax/SellMax/BuySell buttons, affordability coloring, projected post-trade
  usage sliders, prev/next cargo, Info/ShowPrices; `static Class ShowItem` handoff.
- **Engine entry points** (verbatim): `CargoTradeHelper.GetMaxPlayerQuantityIgnoreCredits(...)`,
  `.GetMaxBuyable(...)`, `.GetMaxSellable(...)`, `.DoTransferToPlayerAndExchangeCredits(...)`,
  `.SellMax(...)`, `.BuyMax(...)`, `.DockHasInfiniteOfCurrentCargoClass(...)`;
  `CargoBayComponent.GetCountOf(...)/.GetFreeSpaceFor(...)/.CanChangeCargo(...)/.Usage`;
  `CargoTrader.GetCargoCountStr(...)/.HasInfiniteOf(...)`; `EngineASX.GetPriceColor(TradeType, CargoClass, int)`;
  `Eng.LocalPlayer.Credits`; `DockUI.PlayBuySellBeep()`; `Unit.TryPlayCargoDoorAudio()`;
  `TextFormattingHelper.FormatCredits(...)`.

#### CargoPricesScreen (`...Screens.CargoPrices/CargoPricesScreen.cs`)
- **Purpose**: market survey for one cargo class: lists all buyers/sellers with prices, and lets
  you order your ship to dock at the selected trader; 3 s periodic refresh of visible rows.
- **Engine entry points**: `EngineASX.Instance.CargoClasses`; `CargoClass.IsTraded/.IsReserved/.IsDeployable/.IsEquipment/.GetShortNameElseLong()`;
  `Eng.EngineResources.GetCargoSpriteOrDefault(...)`; `CargoPriceData.Trader.Unit`;
  `UnitContextButton.SetUnit(...)`.

#### CargoFactory widgets (`...Screens.CargoFactory/` — no screen class)
- `CargoFactoryItemListUI`/`CargoFactoryItemListItemUI` (production job list/row),
  `CargoFactoryItemStatusUI` (Producing / Insufficient Cargo Space / Idle + time-remaining),
  `CargoFactoryProfileItemListUI` and friends (recipe view of `CargoFactoryProfileItem`
  inputs/outputs/credits). They embed in ShipScanScreen and UnitInfoScreen; no navigation of their own.

#### ShipTradeScreen (`OpenFrontier.IP/UI/ShipTradeScreen.cs`)
- **Purpose**: ship dealer catalogue at a station; buy entry point and gateway to custom-variant
  creation/management (CreateNewVariant, ManageVariants, ChangeShip prev/next).
- **Engine entry points**: `UnitShipTrader.TrimDuplicates()/.GetShipItems()`,
  `CustomUnitVariantIO.LoadCustomVariantShips()`,
  `ShipTraderItemWrapper.FromShipTraderItem(...)/.FromCustomVariant(...)/.GetCost(faction, faction)`,
  `UnitClass.IsUsable/.IsBarebonesShip`, `Maths.WrapValue(...)`, `IPProduct`/`Products.HasProduct(...)`.
- **Navigation**: TO ShipBuyScreen (`ShowBuyShipScreen`), StoreScreen (IAP upsell),
  ManageUnitVariantsScreen, UnitClassPickerScreen (variant wizard). Below: DockFacilitiesUI /
  UnitContextMenuScreen.

#### ShipBuyScreen (`...Screens/ShipBuyScreen.cs`)
- **Purpose**: confirmation/purchase screen for N ships of one class, with docking,
  fleet-creation and buy-and-enter options; `CreateFleetToggle`, `DockShipsToggle`,
  Customize button (transient variant editor), IAP guard in the caller, hangar-bay checks,
  `Authority.BuyShipRequest` multiplayer hook.
- **Engine entry points** (verbatim): `Faction.GetMarkedUpPriceAfterOpinionChange(TradeType, int, Faction)`,
  `Maths.RoundUpToInt(...)`, `EconomySettings.ShipSaleRounding`,
  `UnitHangar.GetBestUnoccupiedBay(...)/.UnoccupiedBays`, `WorldHelper.CanEnterUnitAtPosition(...)`,
  `OrdersHelper.CreateFleetForUnits(...)`, `Object.Instantiate(UnitPrefab)` + `Unit.Init()`,
  `UnitComponents.AutoAssignShipName()/InstallDefaultComponents()/AddDefaultCargoLoadout()/TryDockInUnit()/UndockIfDocked()`,
  `Unit.TransformForUndockFrom(...)`,
  `CustomUnitVariantHelper.InstallComponentsAddCargoAndRename(...)/.CreateFromUnitClass(...)/.Clone()`,
  `EngineASX.RegisterTaxedPlayerTrade(..., FactionTransactionType.ShipPurchase, ...)`,
  `EngineResources.GetUnitClassRenderSpriteOrDefault(...)`.

#### SellShipScreen (`...Screens.SellShip/SellShipScreen.cs`)
- **Purpose**: trade-in current ship at the docked dealer for a marked-up, rounded value;
  confirm-only UI.
- **Engine entry points**: `Unit.CalculateCurrentMoneyValue()`,
  `Faction.GetMarkedUpPriceAfterOpinionChange(..., TradeType.Buy)`, `Maths.RoundUpToInt(...)`,
  `EconomySettings.ShipSaleRounding`,
  `EngineASX.RegisterTaxedPlayerTrade(..., FactionTransactionType.ShipPurchase, ...)`,
  `EngineResources.GetUnitClassRenderSpriteOrDefault(...)`, `Unit.SafeDestroy()`,
  `DockUI.MovePlayerToDockUnit()` / `.NavigateToInitialScreen()`.

#### ComponentTradeScreen (`...Screens.ComponentTrade/ComponentTradeScreen.cs`)
- **Purpose**: outfitting screen — pick a unit's component bay, sell what's installed,
  buy/install a replacement (Core Internal auto-refill rule).
- **Features**: `ComponentTradeBayList` + `AvailableComponentsList`, category dropdown
  (Legacy/Hardpoints/Core Internals/Optional Internals/Utility Mounts), installed/selected info +
  price labels, Sell/Buy buttons (`IgnoreCost` relabels Install/Remove for the variant editor),
  prev/next bay and item.
- **Engine entry points** (verbatim): `ComponentTradeHelper.GetComponentClassSaleCost(...)/.GetComponentBuyCost(...)`,
  `ShipComponentsHelper.GetUnitVisibleBays(...)/.GetBaysOfCategory(...)`,
  `ComponentBay.InstallComponent(...)/.DestroyInstalledComponent()/..IsComponentCorrectType(...)/.IsComponentCorrectRating(...)/.BayType.IgnoreInTradeUI/.OrderInTradeUI`,
  `UnitComponentHolder.Components.SupportsAdditionalComponentOfType(...)`,
  `ShipComponents.IsModified = .CalculateIsModded()`,
  `EngineASX.RegisterTaxedPlayerTrade(..., FactionTransactionType.EquipmentPurchase, ...)`,
  `Eng.ComponentClasses`.

#### PropertyScreen (`OpenFrontier.IP.UI.Screens/PropertyScreen.cs`)
- **Purpose**: player property manager — searchable/sortable list of everything your faction owns
  (ships, stations, turrets/minors, docked ships, cargo) filtered by sector, with fleet context
  actions and a search input with debounce.
- **Engine entry points**: `OrdersHelper.CreateAndInitPlayerFleet(...)/.FindOrCreateNpc(...)/.OrderMergeWithFleet(...)/.NoteShipsForTheHost(...)`,
  `FleetsHelper.ShouldShowFleetContextMenu(...)/.TooManyShipsInFleetMessage()`,
  `EngineASX.Instance.PlayerPropertyChangedSector` event, `Faction.GetCountOfUnitType(...)/.GetValidNonMinorStationCount()/.GetCountOfDockedShips()`,
  `Unit.HasDockedUnits()/.GetDockedUnits()/.IsImmediateDockOwnFaction()/.IsMinorStation()/.GetFriendlyName()`.
- **Refresh convention**: 1 s visible items, 0.5 s validity, 15 s full refresh.

#### FactionsScreen (`OpenFrontier.IP/UI/FactionsScreen.cs`)
- **Purpose**: diplomacy overview — known factions with friends/enemies lists, bounties,
  wealth/power/reputation and a comms button.
- **Engine entry points** (verbatim, abridged): `BountyHelper.GetTotalBountyPlacedOnFaction(...)/.GetTotalBountyPlacedOnFactionByFaction(...)`,
  `CommsHelper.OpenCommsWithHandler(...)/.TryGetCommsHandlerFromPerson(...)`,
  `Faction.HasAttitudeToFaction(...)/.GetNeutralityWith(...)/.IsHostileToOrAlwaysHostileTo(...)/.IsAlliedTo(...)/.GetKnownFactionsWithOpinionLessThan/GreaterThan(...)/.GetCachedNetWorth()/.GetWealthLevelDescription()/.GetPowerLevelDescription()/.GetReputationDescription()/.GetHomeSectorNameForPlayer()/.GetFactionTypeDescription()/.Credits`,
  `EngineASX.GetFactionHostilityColor(...)`, `UnityRichTextHelper.Color(...)`.

#### FactionSettingsScreen (`...Screens.FactionSettings/`)
- **Purpose**: settings for the player's own faction: rename faction/pilot/title, illegal-cargo
  trade toggle, preferred formation style, autopilot sector exclusions.
- **Engine entry points**: `Faction.TradeIllegalGoods`, `Faction.PreferredFormationStyle`,
  `Faction.Name/.ShortName/.ClearGeneratedName()`, `LocalFaction.ToggleAllowSectorNavigation(Sector)`,
  `LocalFaction.Intel.DiscoveredSectorIds`,
  `EngineASX.Instance.CachedFleetSettingsController.HasDefaultFleetSettings/.DefaultFleetSettings.FormationId`,
  `Person.CustomName/.CustomTitle/.RefreshName()`, `GameSettings.RandomTitles/.PilotNameMinChars...`.

#### FactionTransactionsScreen (`...Screens.FactionTransactions/`)
- **Purpose**: account ledger for a faction — recent transactions list with jump-to-location
  waypoint buttons; refresh gated on `Faction.LastTransactionTime`.
- **Engine entry points**: `Faction.RecentTransactions`, `Faction.LastTransactionTime`,
  `LocalPlayer.SetCustomWaypointToUnit(...)`, `UnitContextButton.SetUnit(...)`.

#### CreateUnitVariantScreen (`...Screens.CreateUnitVariant/CreateUnitVariantScreen.cs`)
- **Purpose**: variant-editor hub: title/icon/cost/description with entry buttons into cargo and
  bay editing, plus save (overwrite + name-collision guards, `TransientVariant` mode for the
  ShipBuyScreen customize flow, dirty tracking with "Quit without saving?" back guard).
- **Engine entry points** (verbatim): `CustomUnitVariantIO.SaveCustomVariant(...)/.DeleteCustomVariant(...)/.CustomUnitVariantSaveFileExists(...)/.LegacyExtension`,
  `CustomUnitVariantHelper.GetAllComponentClasses(...)/.CalculateMoneyValue(...)/.Clone()`,
  `ShipBuyScreen.GetItemSaleCostPerUnit(...)`, `EngineASX.Instance.UnitClasses`,
  `EngineResources.GetUnitClassRenderSpriteOrDefault(...)`. Variants save to files, not scenes.

#### CreateUnitVariantComponentsScreen (`...Screens.CreateUnitVariant/`)
- **Purpose**: component (bay) layout editor for a custom variant (cost-free); same bay
  navigation and compatibility errors as ComponentTradeScreen without the category dropdown.
- **Engine entry points**: `CustomUnitVariantHelper.SetComponentClass(...)/.RemoveComponentClass(...)/.GetBayComponentClass(...)/.IsComponentCompatible(...)`,
  `ComponentTradeHelper.GetComponentClassSaleCost(...)/.GetComponentClassBuyCost(...)`;
  bays enumerated off `UnitClass.UnitPrefab.GetComponentsInChildren<ComponentBay>()`.

#### CreateUnitVariantCargoScreen (`...Screens.CreateUnitVariant/`)
- **Purpose**: cargo loadout editor for a custom variant — compatible equipment cargo, quantity
  caps to remaining bay space, `AutoAddCargoButton` for the default loadout.
- **Engine entry points**: `CustomUnitVariantHelper.GetDefaultCompatibleCargoItems(...)/.GetCompatibleCargoClasses(...)`,
  `CustomUnitVariant.GetCargoCapacity()/.GetAvailableCargoCapacity()/.GetCargoUsage()/.CargoItems`,
  `EngineASX.Instance.CargoClasses`, `EngineResources.GetCargoSpriteOrDefault(...)`,
  `TextFormattingHelper.FormatNumber(...)`. Opens CargoPickerScreen (`keepInNavigationStack: false`).

#### ManageUnitVariantsScreen (`...Screens.CreateUnitVariant/ManageUnitVariantsScreen.cs`)
- **Purpose**: library of saved custom variants — list/edit/create/delete and OS-browser
  import/export (multi-file support where `FileImporter`/`FileExporter` capabilities allow),
  confirm-before-delete.
- **Engine entry points** (verbatim, spot-verified): `CustomUnitVariantIO.LoadCustomVariantsWithFileName()/.SaveCustomVariant(...)/.DeleteCustomVariants(...)/.TryLoadValidCustomVariantOrError(...)/.PrepareVariantForExport(...)/.GetFullPath(...)`,
  `CustomUnitVariantHelper.CalculateMoneyValue(...)`,
  `FileImporter.TryGetFileFromBrowser(this, (string filePath) => ...)` /
  `TryGetFilesFromBrowser` / `IsImportFileSupported()` / `IsMultiFileImportSupported()`,
  `FileExporter.TryExportFile(s)/IsExportFileSupported/IsMultiFileExportSupported`.

#### ChangeUnitCargoScreen (`...Screens.Test.ChangeUnitCargo/ChangeUnitCargoScreen.cs`)
- **Purpose**: debug/test direct cargo editor for any unit's bay — set arbitrary cargo
  types/quantities ignoring capacity (`AddToCargo(..., ignoreCapacity: true)`).

#### CompatibleProjectilesScreen (`OpenFrontier.IP.UI.Screens/CompatibleProjectilesScreen.cs`)
- **Purpose**: read-only ammo reference — every `ProjectileClass` a given turret type can fire,
  priced low-to-high; rows self-navigate to CargoInfoScreen.
- **Engine entry points**: `ProjectileTurretClass.CompatibleProjectiles`,
  `ProjectileClass.AmmoClass.BasePrice`, `EngineResources.GetCargoSpriteOrDefault(...)`.

#### Cargo widget pool (UI root)
- **AdvancedCargoUsageSlider** — twin-slider cargo usage meter (current + expected projection)
  with usage/capacity label; reads `CargoBayComponent.Usage/Capacity`.
- **CargoUsageSlider** — single-slider usage/capacity bar; caches last values, skips unchanged refreshes.
- **CargoClassLink** — click-to-open component: `ScreenNavigator.ShowCargoInfoScreen(CargoClass)`
  (any cargo label can be a deep-link).
- **CargoTradeHelper** — static trading bridge: buy/sell/max-quantity math,
  `DoTransferToPlayerAndExchangeCredits` (lands as `Engine.RegisterPlayerTrade(...,
  FactionTransactionType.Trade, ...)` + `Engine.TransferToPlayerCargoWithMsg(...)`).
- **CargoStatGenerator** — stat rows for CargoInfoScreen: avg price, volume, projectile/missile/
  countermeasure stats from `CargoClass.RelatedPrefab`.
- **CompatibleProjectileItem / CompatibleProjectileItemList** — ScrollList row+list of
  ProjectileClass; row button opens CargoInfoScreen.
- **ComponentInfoGenerator / ComponentInfoUI** — `StatGeneratorUI<ComponentClass>` stat-row matrix
  (generator rate, turret energy/efficiency/charge/range, shield capacity/regen, cloak times...);
  `ComponentInfoUI` is the ScreenBase component-info panel (name/description +
  `ShowProjectilesScreen` when the item is an ammo-fed projectile turret).

### 3.4 HUD / dialogs / messages cluster

#### HudScreen (`...Screens.Hud/HudScreen.cs`, 1525 lines)
- **Purpose**: the in-flight pilot screen — the entire flight HUD and **the root of the navigation
  stack while flying**. Single `HudScreen.Instance`.
- **Features**: target select via mouse/touch (`TargetInScreenSelector`, T key `TargetInViewSelector`),
  keyboard/tilt steering + throttle (`DesiredTurn` → `ActiveUnitShip`), auto-turn bearing +
  world-space turn-sprite with shader `_Cutoff` reveal, camera mode cycle (`HudCameraMode`
  FixedForward/LockTarget/Free, V key), dock/undock/claim/abandon/enter-ship/comms/target-menu/
  orders/target-info/match-speed buttons, speed label (0.09 s throttle), hostile-targeted beep,
  QuickMsg toasts, dock-header padding, `HudViewOptions` toggles persisting to PlayerPrefs.
- **Engine entry points** (verbatim, abridged): `EngineASX.ShowHud()` → `NavigateToRootScreen(Hud)`;
  `EngineASX.InitialiseCamerasForHud()`; `EngineASX.HudCamera.SetHudTarget(HudTarget.CreateFromUnit())/
  RemoveTargetWithCooldown/HasChangeTargetCooldownExpired`; `EngineASX.ChangePlayerUnit()` +
  `PlayChangeShipAudio()`; `DitchUnitModule.DitchUnitWithAbandon()`; `EngineASX.LocalFaction.ClaimUnit()`;
  `Unit.Components.TryDockInUnit()`; `Faction.RequestDock()`; `ClaimUnitHelper.CanClaimUnit()`;
  `WorldHelper.CanEnterUnit/CanShowUnitInfo/CanShowUnitContext()`; `FactionIntel.IsStaticOrTreatAsStatic()`;
  `FactionIntelProcessor.IsTargetOutOfDetectionRange()`; `CommsHelper.OpenCommsWithHandler/TryGetCommsHandlerFromUnit()`;
  `OrdersHelper.IsPilottedByNpc()`; `NpcPilot.ApplyDesiredBearing()`;
  `Components.AutoTurretModule.PreferredTurretTarget=`; events subscribed:
  `EngineASX.PlayerUnitChanged`, `HudCamera.LostTarget`, `DialogController.CommsStageShown`.
- **Resources**: loaded LoadOnly+deactivated at engine start by `EngineASX.LoadHudScreenCoroutine()`;
  PlayerPrefs keys in `HudViewOptions`.
- **Navigation**: TO UnitContextMenuScreen, OrdersScreen, UnitInfoScreen, SectorMapScreen
  (`ShowSectorMapScreenWhenPilotting`), UniverseMapScreen, PauseMenuUI. `onNavigatingBack()` shows
  the pause menu and **always cancels** — it is never popped by back.

#### DialogController — comms/story dialog (`OpenFrontier.IP.OpenFrontier.Engine/DialogController.cs`, 468 lines)
- **Purpose**: the comms/story dialog: incoming operator message (ship icon, colored
  sender/faction label) + confirm button, then option list; holds camera-to-target staging.
- **Dialog item loading**: stages come from handler chains —
  `CommsHelper.OpenCommsWithHandler(handler)` → `commsHandler.GetStage()`
  (`DialogBase.EntryDialogStage` on pilot prefabs, `Person.CommsHandler`) or AI-generated
  (`FactionDynamicComms.DynamicCommsHandler : ICommsHandler` with stages built at runtime from
  profiles + substitution tokens). `ChangeStage(ICommsStage, pausedGame=true)` stores the stage,
  navigates to itself, `stage.Show(this)`, collects `stage.GetOptions()` (`DialogStage.GetOptions()`
  = `GetComponentsInChildren<DialogOption>()`). `OptionGroup` filtering shows one group + a
  "Nevermind" row.
- **Widgets**: `DialogItemList : ScrollList<ICommsStageOption>` + `DialogItemUI` — row text grey
  when `NumberTimesSelected > 0`.
- **Option select flow**: `DialogOption.Select` — credit cost check →
  `UIController.ShowMessageBox("Insufficient credits")`; `TimesEntered++`;
  `BroadcastMessage("Execute")` (hook script on the same GO); deduct
  `EngineASX.AddCreditsToPlayerFactionWithMsg(-CreditsCost)`; `commsController.ChangeStage(TargetDialogStage)`.
- **Engine entry points**: `EngineASX.DialogController` (Eng-injected);
  `EngineASX.EngineResources.GetUnitClassRenderSpriteOrDefault()`;
  `EngineASX.GetFactionHostilityColor()`; `EngineASX.TrySetCameraSpectatorEnabled(false)`;
  `EngineASX.AddCreditsToPlayerFactionWithMsg()`; camera moves to
  `unit.ActiveUnit.CommsDialogPositioner` within `MaxDistanceForCameraMove` (1500);
  persistence: `DialogOption/DialogMessage.WriteBinary/ReadBinary` (TimesEntered/TimesConfirmed)
  in the save stream.
- **Navigation**: appears above whatever is current (dock or HUD); **back disabled**
  (`AllowNavigateBack()` = false — Escape cannot close it); `Eng.IsPaused` true while a stage is
  open; events out: `CommsStageShown` (HUD listens → `SpeechModel.RemoveRequestsFromPilotInPast`),
  `CommsStageFinished`.

#### MessageBoxScreen (`OpenFrontier.IP.UI.Screens/MessageBoxScreen.cs`)
- **Purpose**: modal dialog box (text scroll + title + icon + Ok/Cancel) for every confirm/info
  popup; target of ScreenBase's help compile.
- **Features**: `ShowMessageArgs{Text,Title,Buttons,Icon,TextAlignment}`, buttons enum
  Ok/OkCancel/Cancel/None, icons from `MessageBoxIconSettings`, auto-size 635–735 × 250–600 by
  text length, `Dismissed` callback with `MessageBoxResult`, `CancelDismiss` keeps the box up;
  **dims the screen below** via `PanelDeactivateArgs.Create(backTarget)` → CanvasGroups
  non-interactable + alpha to `GameController.FadedScreenAlpha`, `Restore()` on close.
- **Engine entry points**: shown via `UIController.ShowMessageBox(...)` (also `ShowError`,
  `ShowInsufficientCreditsMessageBox`, `ScreenBase.ShowHelp`). Shell only — no other engine calls.
- **Navigation**: preloaded LoadOnly by `UIController.LoadMessageBoxSceneCoroutine()` and reused;
  `Show()` → `NavigateToScreen(this)` on top of the current stack; `Finish()` → `NavigateBack()`
  to caller. `AllowNavigateBack()` true only when Buttons==Ok.

#### MessageUI / MessagesUI (`OpenFrontier.IP/UI/`)
- **MessageUI** — reads one `PlayerActiveMessage`: from/to/subject/date + body; prev/next through
  `MessagesUI.ActiveItems`; delete (if `Message.AllowDelete`); `Message.Opened = true`;
  Target → `Eng.LocalPlayer.SetCustomWaypointToUnit/SetCustomWaypointToSectorPosition`;
  View → `ShowSectorMapScreenForUnitRespectingIntel` / `ShowSectorMapScreenShowingWorldPosition`;
  entry via `static MessageUI.ShowMessage` handoff field.
- **MessagesUI** — inbox list with read/unread bold rows, sort headers (From/Timestamp),
  mark-all-read and delete-unimportant; fills from
  `PlayerMessagesHelper.GetDisplayableMessages()`; live refresh on `GamePlayer.MessagedAddRemoved`.
- **Engine entry points**: `GamePlayer.Messages`, `GamePlayer.RemoveMessage()`,
  `EngineASX.DateTimeUtils.GetFormattedGameWorldDateAndAgeFromRealElapsedSeconds()`,
  `EngineASX.DateTimeUtils.GetFormattedEngineTimeStampFromRealElapsedSeconds()`,
  `EngineASX.IsUnitDiscoveredByLocalFaction()`.

#### LogScreen (`OpenFrontier.IP/UI/LogScreen.cs` — EngineScreen base)
- **Purpose**: player stats + XP/level readout and the flying-mode menu grid of dock/utility
  screens; `Eng.World.Permissions.*` gate most buttons (`AllowDockUI*`, `AllowCargoPricesButton`,
  `AllowGodMode`, `AllowPropertyScreen`, `AllowFactionSettingsUiMessagesNavigation`).
- **Engine entry points** (verbatim): `EngineASX.GetNearestBountyBoardToPlayer()`,
  `TopPilotsScreen.GetTopPilots(1).Any()`, `FleetsHelper.PlayerHasAnyVisibleFleets()`,
  `Eng.LocalPlayer.Level/XP/CanLevelUp()/CalculateRemainingLevelUpXP()`,
  `PlayerMessagesHelper.HasAnyMessages()`.
- **Navigation**: TO Property, SectorMap, UniverseMap, Messages, Factions, Missions,
  Transactions, CargoPrices, Bounty, Fleets, TopPilots, FactionSettings, GodMode/Store.

#### ScratchcardScreen (`OpenFrontier.IP.Scratchcard.UI/ScratchcardScreen.cs`)
- **Purpose**: faction scratchcard minigame at a dock — pay stake, rub overlay ≥80 %, 9 slots /
  3-match; prize credited; auto-reset after 4 s.
- **Engine entry points**: `EngineASX.AddCreditsToPlayerFactionWithMsg(-CostPerCard / +PrizeValue, FactionTransactionType.Scratchcard, null, Eng.PlayerRootUnit)`,
  `Faction.Stats.HighestScratchcardWin/ScratchcardsScratched`,
  `ScratchcardDrawer.Draw(ScratchcardSetup, Random)`, `ScratchcardGenerator.Generate`.

### 3.5 HUD sub-systems (all under `OpenFrontier.IP.UI.Screens.Hud.*`)

- **AutoPickTarget/** — `HudAutoPickTarget`: on `AutoScanner.ScanComplete` picks the
  highest-scoring hostile or inbound missile via `HudUnitTargetScorer`, 1 s cooldown.
- **Components/** — `HudShieldAndHullUI` (auto-hide when undamaged; `WorldSpaceShieldAndHullUI`
  pins a world-space canvas at the unit), `TurretComponentUI` (per-turret row, health slider
  tinted `EngineASX.GetHullColor`, `ComponentDamageFlashController`).
- **CurrentTargetUI.WeaponsReadyIndicator/** — pooled per-turret readiness dots, colored from
  AutoFire readiness + `TurretGridUI.CanFireTurret` vs. `HudScreen.CurrentTarget`.
- **QuickTractor/** — `QuickTractorController`: best tractorable cargo via
  `Physics.OverlapSphereNonAlloc` + score (compatible ammo ×2); click →
  `TractorTurretComponent.Fire(cargoUnit)`; compatible-ammo cache (120 s) via
  `NpcPilot.CacheCompatibleAmmoTypes`.
- **ShipComponents/** — `ShipComponentsController`: four slots (PowerGenerator/Capacitor/Engine/
  Shield) + `TurretGridUI`, rebound on player-unit change.
- **TargetInfo/** — `TargetMissileLockController`: toggles a GameObject while
  `EngineASX.MissileLockController.HasMissileLock(target)`.
- **Targeting/** — `TargetInScreenSelector` (finger/mouse pick within `SelectedPixelThreshold`
  90 px), `TargetInViewSelector` (T key best-score unit in view).
- **TargetScorer/** — `HudUnitTargetScorer.GetSelectTargetScore`: distance-to-camera +
  camera-relative bearing, weighted from `GameSettings.PlayerAutoTarget*`.
- *(HUD root companions: `HudComponentManager`, `HudAutoScanner` (2 s scan), `HudScanner`,
  `HudTargetController`+`HudTargetUI` (off-screen target spinners), `HudViewOptions` (PlayerPrefs),
  `ShipComponentUI`, camera controls, `HudAutoPilotController`.)*

### 3.6 Shared widget pool (`OpenFrontier.IP.` root / `IP.UI` root)

- **ScrollList&lt;T&gt;** (`ScrollList.cs`, 942 lines; base `UI.Controls/ScrollListBase.cs`) — THE
  generic list container: pooled `ScrollListItem<T>` rows, `SetItems` sort/retain-selection/
  auto-select-first, visible-range refresh, `NoItemsLabel`, multiselect, events `ItemClicked`,
  `ToggleValueOn`, `SelectedItemChanged`, `ItemsChanged`. Every list screen builds on it.
- **ItemListSelectionButton** — multiselect toolbar (select-all/none, "N / M selected", touch-only
  multiselect toggle).
- **ColumnHeaderGroupUI / ColumnHeaderUI** — sortable column headers with arrow widget;
  subclasses override `applySort()`.
- **ActiveUnitPreviewerUI** — spins a live mocked ActiveUnit 3D preview:
  `EngineASX.LoadActiveUnit(unitClass.ActiveUnitClassName)` reparented to a preview layer, camera
  fitted by `ShieldRingRadius × UnitRadiusMultiplier`.
- **BuildModeButton** / **ButtonSound** — nav shortcuts and per-button click audio (gated by
  `GameController.PlayButtonSounds`).
- **AdaptingEventSystemDragThreshold** — sets `EventSystem.pixelDragThreshold` from DPI
  (`OpenFrontier.ScreenDpi.Value` — never raw `Screen.dpi`), recomputed on resolution change so
  foldables don't eat taps.
- **CustomSafeArea** — updates a RectTransform's anchors to `Screen.safeArea` + game-level insets
  `GameController.UISafeAreaLeft/Right/Top/Bottom` (our SafeAreaFitter supersedes this).
- **CapacitorControlUI / CapacitorSlider / CapacitorSliderUI** — HUD capacitor widgets with
  auto-hide thresholds.
- **CurrentUnitUI** (321 lines) — docked-screen "current ship" button panel: visibility gates from
  `DockUI` + `Eng.World.Permissions.*`; each click fans out to
  `ShowCargoTradeScreen/ShowEquipmentTradeScreen/ShowUnitCargoScreen/ShowSellShipScreen/ShowRepairScreen/ShowPassengerInfoScreen/ShowRenameUnitScreen/ShowOrdersScreen/ShowUnitComponentsScreen/ShowUnitInfoScreen`;
  undock/pilot → `DockUI.UndockCurrentShip/PilotCurrentShip` + `Eng.IsPaused=false`.
- **HelpResourceItem / HelpResource** — data-only help widgets (see §3.1 HelpSystem).
- **UIWinScreen** (21 lines) — win screen: `onNavigatingBack` resets
  `GameController.LaunchOrigin` and `SceneManager.LoadScene("MenuScreenScene", Single)`.
- **Extras worth a line**: `SimpleFader` (full-screen colour fades), `RepeatableButton`
  (hold-to-repeat), `HudQuickMsg`/`QuickMsgUI` (pooled toast queue, max 5),
  `ScreenSizeToScale` (full-screen quads), `SineRotator`/`RotateObject` (idle 3D props).

### 3.7 Screens not registered in SceneNameMapping (orphans)

- `OpenFrontier.IP/UI/StoryUI.cs` — ScreenBase; story-loading entry point; no mapping entry.
- `OpenFrontier.IP/UI/UIWinScreen.cs` — win/exit screen; no mapping entry.
- `OpenFrontier.IP/UI/SkirmishSceneSelectUI.cs` — skirmish scene picker; locates
  `SkirmishSetupScreen` via `UnityObjectHelper.FindComponent<SkirmishSetupScreen>()`; no mapping entry.

---

## 4. Interface patterns (how screens talked to each other and the engine)

- **Navigation**: `ScreenNavigatorExtensions.Show<SomeScreen>(...)` helper methods are the
  standard launch route (~60 of them); results flow back through C# events.
- **Modal pickers** (`CargoPickerScreen`, `MultiCargoPickerScreen`, `UnitPickerScreen`,
  `FleetPickerScreen`, `FleetFormationStylePickerScreen`, `RenameUnitScreen`,
  `UnitClassPickerScreen`) navigate nowhere — they expose `Finished` / `UnitPickedResult` /
  `Callback` events and `NavigateBack()`; several use `KeepInNavigationStack = false` so the
  picker vanishes from the stack entirely.
- **Static handoff fields** for open-then-inspect flows (`CargoInfoScreen.ShowItem`,
  `CargoTradeItemScreen.ShowItem`, `MessageUI.ShowMessage`).
- **Refresh conventions**: 1 Hz / 3 s throttled full refreshes; change-detection via engine
  timestamps (`CargoBayComponent.LastChangedTime`, `Faction.LastTransactionTime`); auto-back when
  the subject dies ("Lost contact with target").
- **Back-guards**: `onNavigatingBack(ref cancel)` for confirm-on-quit and restricted map modes;
  back button hidden when no back target exists; `HudScreen` and root screens never pop.
- **Modal dimming**: MessageBox dims the below-screen through the navigation stack's
  `PanelDeactivateArgs` machinery and restores on close.
- **Screen lifetime**: `DeactivateType` `Disable`-canvas vs `SetActive`-gameobject when off-screen;
  destroyed on back out of stack unless kept in memory; `ReuseScreenMode` decides re-instantiation.
- **Multiplayer seams**: order/checksum-sensitive code routes through `Authority.IsThisCopyAClient`
  and host-owned state (`OrdersHelper.OrderStop(leader)` — host owns orders).

---

## 5. Old screen → new screen contract map

| New UI being authored | Legacy screen(s) | Contract to satisfy |
| --- | --- | --- |
| Main Menu row | `MainMenuScreen` | `SaveGameUtilities.CanFindHeaders` 1 Hz poll; `ScenarioLoader.TryLoadScenario`; `SessionNotices.SayPending()`; version text; back = quit |
| SinglePlayer card row | `GameModeScreen` first — **pure navigation, zero engine calls**; deeper: `ScenariosUI` / `SkirmishSetupScreen` / `BattlesScreen` (all `ScenarioUI` family) | All play paths funnel through `ScenarioUI.PlayScenario(...)` → `GameController.Instance.ScenarioLoader.TryLoadScenario(scenarioLoadData)`. Tutorials = `ScenariosUI` retitled via `ShowTutorialsScreen()`. Quick-start skirmish = `SkirmishSetupScreen.PlayRandomSkirmish()`. |
| Options | `OptionsScreen` + `UIScaleScreen` + `AdjustSafeAreaScreen` | `PlayerOptions` / `PlayerPrefsHelper.SafeGetFloat` bindings; `SavePlayerPrefs()`; `ApplyAllPlayerPrefs()` on back; UI scale applies only after quit-to-menu; safe-area = superseded by our SafeAreaFitter |
| Load / Continue | `MainMenuScreen` + `LoadGameScreen` | `SaveGameUtilities.LoadHeader(path)`, `GetSavePath(..., ensureUnique: true)`, delete/rename/export (`FileExporter`/`FileImporter`, `File.Move(..., .Extension)`), then `ScenarioLoader.TryLoadScenario(header)` |
| Modals / confirm dialogs | `MessageBoxScreen` via `UIController.ShowMessageBox` | auto-sized modal, dims below, Ok/OkCancel, callback with result |
| Credits | `CreditsUI` (empty shell) + `CreditsAnimation` | content animation lives in the main-menu module |
| Pause | `PauseMenuUI` | `EngineIO.Save(instance, out err)`, `Eng.AutoSaveIfPossible()`, `UI.QuitToMainMenu()`, pause-state restore |
| End of run | `EndGameUI` | `Eng.World.NotifyPlayerRequireExit(continueScenario)` / `NotifyPlayerRequireRetry()`; it is a **root** screen |
| In-flight HUD | `HudScreen` + `Screens.Hud.*` sub-systems | root screen while piloting; camera/unit contracts quoted in §3.4 |

**Rebuild heuristics this log suggests:**
1. Navigation screens at the top of the flow (MainMenu, GameMode) carried **no engine logic** —
   contracts live one level deeper. Our new card row can stay a dumb uGUI layout matching
   healthy architecture; the cards each launch the *next* screen, which owns the engine calls.
2. Every "start a game" path (Continue, Load, Skirmish, Scenarios, Battles, Universe types)
   funnels into **`ScenarioLoader.TryLoadScenario(scenarioLoadData)`** — one seam to satisfy.
3. Pickers returned results through **events, not navigation results** — worth keeping as a
   pattern for any future pickers.
4. Lists/throttles/change-detection conventions (§4) are the house interface for "live" screens;
   reuse them for anything that watches the world.
