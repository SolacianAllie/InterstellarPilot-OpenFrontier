using System.Collections.Generic;
using System.Text;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Claiming;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Engine;
using OpenFrontier.IP.UI.HUDScannerDisplay;
using OpenFrontier.IP.UI.Screens.Hud.Components;
using OpenFrontier.IP.UI.Screens.Hud.QuickTractor;
using OpenFrontier.IP.UI.Screens.Hud.ShipComponents;
using OpenFrontier.IP.UI.Screens.Hud.Targeting;
using OpenFrontier.IP.UI.Screens.UniverseMap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.UI.Screens.Hud
{
	public class HudScreen : EngineScreen
	{
		public delegate void PlayerTargetChangedHandler(HudScreen sender, Unit oldTarget);

		public GameObject TouchTargetButton;

		public HudComponentManager ComponentManager;

		public RectTransform Padder;

		public HudViewOptions ViewOptions;

		public Button TargetContextMenuButton;

		public MissileLockButtonsController MissileLockButtonsController;

		public HudShieldAndHullUI WorldSpaceConditionController;

		public HudShieldAndHullUI ScreenSpaceConditionController;

		public QuickTractorController QuickTractorController;

		public ShipComponentsController ShipComponentsController;

		public HudTargetController HudTargetController;

		public HUDScannerDisplayListController HUDScannerDisplayListController;

		public TargetInScreenSelector TargetInScreenSelector;

		public HudAutoScanner AutoScanner;

		public TargetInViewSelector TargetInViewSelector;

		public HUDScannerDisplayList ScannerDisplayList;

		private const float timeBetweenCanDockCheck = 1f;

		private static HudScreen instance = null;

		internal static List<HudScannerUnit> nextUnitCache = new List<HudScannerUnit>();

		public bool AllowDeselectTargetWithMouse;

		public bool AllowPlayerSteering = true;

		public bool AllowPlayerTargetting = true;

		public bool AllowPlayerThrottle = true;

		private bool autoTurnAngularArrived = true;

		public float CheckCanDockFrequency = 0.2f;

		public Button ClaimShipButton;

		/// <summary>
		/// Shown on the claim button when the target is your own ship.
		/// </summary>
		/// <remarks>
		/// One button with two meanings rather than two buttons, because every
		/// menu in this game is a fixed set of prefab buttons and adding one is
		/// prefab surgery - the thing that fails silently.
		///
		/// The two meanings are exact complements. ClaimUnitHelper will only
		/// claim a ship that belongs to nobody; abandoning is only ever
		/// allowed on a ship that belongs to you. So there is never a moment
		/// where both are true and the button has to choose between two
		/// arguments for being wrong.
		///
		/// Left null it keeps the claim icon and says so once, rather than
		/// drawing nothing. The button still does the right thing either way.
		/// </remarks>
		public Sprite AbandonShipIcon;

		public Button CommsButton;

		public GameObject CommsButtonRoot;

		public float CommsMaxResponseTime = 3f;

		public float CommsMinResponseTime = 0.3f;

		public float controlPanelButtonSize = 92f;

		private int controlPanelButtonSpacing = 3;

		private Unit currentTarget;

		private float DesiredAngleY;

		private float desiredBearing;

		public Button DockButton;

		public GameObject DockButtonRoot;

		public Button EnterShipButton;

		public Button GameSpeedButton;

		public AudioSource HostileTargetsAudioSource;

		public InGameMenuControllerUI InGameMenuController;

		public float InGameMenuTimeout = 5f;

		private float lastDistToTarget;

		private float lastPlayedHostileTargetsBeep = float.MinValue;

		private Faction lastPlayerHostileTargetsBeepOpposingFaction;

		public float lastRequestDockTime;

		private Unit lastRequestedDock;

		private Faction lastTargettedByFaction;

		private float lastTargettedByTime;

		public HudCameraMode CameraMode;

		public Button MatchTargetSpeedButton;

		public GameObject MissileLockGameObject;

		private float newDesiredTurn;

		private float nextCanDockCheck;

		private Unit oldPlayerUnit;

		public GameObject PlayerCapacitorRoot;

		public HudShieldAndHullUI PlayerConditionController;

		private TractorTurretComponent playerTractorTurret;

		private bool playerUnitHasChanged;

		public Button SectorMapButton;

		public GameObject ShipTurnSpriteL;

		public GameObject ShipTurnSpriteR;

		public bool ShowTargetSprites = true;

		public TextMeshProUGUI SpdLabel;

		public SpeechModel SpeechModel;

		public UnitConditionControllerUI TargetConditionController;

		public Button TargetInfoButton;

		public UnitInfoUI TargetInfoUI;

		public Button TargetMenuButton;

		public Button TargetMissileLockButton;

		public Button TargetOrdersButton;

		public GameObject TargetRoot;

		public GameObject ThrottleRoot;

		public Slider ThrottleSlider;

		public float ThrottleButtonSensitivity = 0.5f;

		public ThrottleProgressBarUI ThrottleControllerUI;

		public float TimeBetweenHostileTargettedMsg = 10f;

		public float TotalTurnInput;

		public GameObject TractorLootButtonRoot;

		public float TurnButtonSensitivity = 1f;

		public GameObject TurnButtonsFlashRoot;

		public GameObject TurnButtonsRoot;

		public Vector3 TurnSpritePositionOffset = new Vector3(0f, -4f, 0f);

		public GameObject TurnSpriteRoot;

		public float TurnSpriteShieldRingMultiplier = 1f;

		public float TurnSpriteThreshold = 5f;

		public List<TurretGridUI> TurretGrids;

		public UnitCompass UnitCompass;

		public float UnitTurnRestoreSpd = 0.5f;

		public Button UniverseMapButton;

		private static StringBuilder spdLabelStringBuilder = new StringBuilder(8);

		private float lastSpeedLabelUpdate;

		public GameObject PlayerHullRoot => PlayerConditionController.HullRoot.gameObject;

		public GameObject PlayerShieldsRoot => PlayerConditionController.ShieldsRoot.gameObject;

		public bool AutoTurnEnabled
		{
			get
			{
				if (!GameController.Instance.AutoTurnEnabled)
				{
					return false;
				}
				return !UI.IsMobileDevice;
			}
		}

		public Faction CurrentTargetFaction
		{
			get
			{
				if (currentTarget != null)
				{
					return currentTarget.Faction;
				}
				return null;
			}
		}

		public Unit CurrentTarget
		{
			get
			{
				return currentTarget;
			}
			set
			{
				if (currentTarget != value && Eng != null)
				{
					Unit unit = currentTarget;
					currentTarget = value;
					if (unit != null)
					{
						unit.Killed -= playerTarget_Killed;
					}
					if (PlayerUnit != null && PlayerUnit.Components.AutoTurretModule != null)
					{
						PlayerUnit.Components.AutoTurretModule.PreferredTurretTarget = currentTarget;
					}
					if (TargetConditionController != null)
					{
						TargetConditionController.LocalUnit = currentTarget;
						TargetConditionController.TargetUnit = PlayerUnit;
					}
					if (PlayerConditionController != null)
					{
						PlayerConditionController.TargetUnit = currentTarget;
					}
					if (TargetInfoUI != null)
					{
						TargetInfoUI.LocalUnit = PlayerUnit;
						TargetInfoUI.ForeignUnit = currentTarget;
					}
					if (currentTarget != null)
					{
						currentTarget.Killed += playerTarget_Killed;
						CacheDistanceToCurrentTarget(PlayerUnit);
					}
					if (CurrentTargetChanged != null)
					{
						CurrentTargetChanged(this, unit);
					}
					UpdateTargetMenuButtonEnabled();
				}
			}
		}

		public Faction PlayerFaction
		{
			get
			{
				if (Eng.LocalPlayer != null)
				{
					return Eng.LocalPlayer.Faction;
				}
				return null;
			}
		}

		public int ControlPanelButtonSpacing
		{
			get
			{
				return controlPanelButtonSpacing;
			}
			set
			{
				controlPanelButtonSpacing = value;
			}
		}

		public Unit PlayerUnit
		{
			get
			{
				if (Eng != null)
				{
					return Eng.PlayerUnit;
				}
				return null;
			}
		}

		public float LastDistToTarget => lastDistToTarget;

		public float LastTargettedByTime => lastTargettedByTime;

		public Faction LastTargettedByFaction => lastTargettedByFaction;

		public bool AutoTurnAngularArrived
		{
			get
			{
				return autoTurnAngularArrived;
			}
			private set
			{
				if (autoTurnAngularArrived != value)
				{
					autoTurnAngularArrived = value;
				}
			}
		}

		public bool CurrentTargetIsPlayerCustomPathTarget
		{
			get
			{
				if (currentTarget != null)
				{
					return currentTarget.IsPlayerCustomPathTarget();
				}
				return false;
			}
		}

		public bool CurrentTargetIsPlayerMissionPathTarget
		{
			get
			{
				if (currentTarget != null)
				{
					return currentTarget.IsPlayerMissionPathTarget();
				}
				return false;
			}
		}

		public static HudScreen Instance => instance;

		public TractorTurretComponent PlayerTractorTurret => playerTractorTurret;

		public bool AlwaysShowShieldAndHull => !GameController.Instance.PlayerOptions.UI_AutoHideShields;

		public event PlayerTargetChangedHandler CurrentTargetChanged;

		public static bool CanShowOrdersForUnit(Unit unit)
		{
			if (unit.UnitType == UnitType.Ship && unit.IsOwnedByPlayer && !unit.IsUnderConstructionOrDismantling)
			{
				return unit.UnitClass.ShipType == ShipType.Normal;
			}
			return false;
		}

		public static bool IsViewportPositionInScreen(ref Vector3 screenPos)
		{
			if (screenPos.x >= 0f && screenPos.x < 1f && screenPos.y >= 0f && screenPos.y < 1f)
			{
				return screenPos.z > 0f;
			}
			return false;
		}

		public void TryDockAtTarget()
		{
			if (currentTarget == null || PlayerUnit == null
				|| !CanRequestDockAtUnit(PlayerUnit, currentTarget))
			{
				return;
			}

			// Asked before anything is done locally.
			//
			// Which hangar a ship is in is world state - the sector map
			// filters on it, and a joiner reads it when it takes a snapshot -
			// so a client that docked its own ship put it in a hangar the host
			// had never heard of, and the two copies stayed different for the
			// rest of the session with nothing to reconcile them. And only the
			// host knows what is in a hangar, so only the host can say whether
			// there is room.
			//
			// Returns false on a host and in singleplayer, where the dock
			// happens here as it always did.
			if (Authority.DockRequest != null
				&& Authority.DockRequest(PlayerUnit.UniqueId, currentTarget.UniqueId))
			{
				return;
			}

			RequestDockAtTarget(PlayerUnit, currentTarget);
		}

		public bool CanRequestDockAtUnit(Unit playerUnit, Unit targetUnit)
		{
			UnitHangar hangar = targetUnit.GetHangar();
			if (hangar != null)
			{
				if (hangar.CanUnitFitInHangarIgnoreOccupancy(playerUnit.Components) && AllowDockAtTarget(playerUnit, targetUnit))
				{
					return IsInDockableRangeOf(playerUnit, targetUnit);
				}
				return false;
			}
			return false;
		}

		public void ActivateCommsWithTarget()
		{
			ICommsHandler commsHandler = null;
			if (ShouldShowCommsButton(out commsHandler))
			{
				CommsHelper.OpenCommsWithHandler(commsHandler, EngineASX.Instance.LocalFaction);
			}
		}

		public void TargetSelectRightButton()
		{
			if (!isMouseOverHUD() && AllowPlayerTargetting && GameController.Instance.RightClickTargetting)
			{
				SetCurrentTargetToTargetAtMouse(Input.mousePosition);
			}
		}

		public void TargetSelectLeftButton(Vector2 touchPosition)
		{
			if (AllowPlayerTargetting && !GameController.Instance.RightClickTargetting)
			{
				SetCurrentTargetToTargetAtMouse(new Vector3(touchPosition.x, touchPosition.y, 0f));
			}
			else if (AutoTurnEnabled && !isMouseOverHUD() && AllowPlayerSteering)
			{
				SetDesiredBearingAtMouse(new Vector3(touchPosition.x, touchPosition.y, 0f));
			}
		}

		public void RefreshTurnButtonVisibility()
		{
			TurnButtonsRoot.gameObject.SetActive(UI.TouchInputEnabled && !GameController.Instance.TiltControlEnabled && PlayerUnit.IsMobile && !OrdersHelper.IsPilottedByNpc(PlayerUnit));
		}

		public bool CanUseThrottle()
		{
			if (AllowPlayerThrottle)
			{
				return !OrdersHelper.IsPilottedByNpc(PlayerUnit);
			}
			return false;
		}

		public bool ShouldShowCommsButton(out ICommsHandler commsHandler)
		{
			commsHandler = null;
			if (currentTarget != null)
			{
				return CommsHelper.TryGetCommsHandlerFromUnit(currentTarget, out commsHandler);
			}
			return false;
		}

		public void UpdateInactive()
		{
			if (Eng.PlayerUnit != null)
			{
				AutoScanner.UpdateTargetting(Eng.PlayerUnit);
			}
		}

		public void MoveDesiredTurnToZero(Unit playerUnit)
		{
			if (playerUnit.ActiveUnit.ActiveUnitShip.DesiredTurn != 0f)
			{
				playerUnit.ActiveUnit.ActiveUnitShip.DesiredTurn = Mathf.MoveTowards(playerUnit.ActiveUnit.ActiveUnitShip.DesiredTurn, 0f, UnitTurnRestoreSpd * Time.deltaTime);
			}
		}

		public void ClearDesiredTurn()
		{
			if (PlayerUnit.ActiveUnit != null && PlayerUnit.ActiveUnit.ActiveUnitShip != null)
			{
				PlayerUnit.ActiveUnit.ActiveUnitShip.DesiredTurn = 0f;
			}
		}

		public bool isMouseOverHUD()
		{
			return false;
		}

		public void OnPlayerUnitChanged(Unit oldUnit)
		{
			Unit playerUnit = PlayerUnit;
			if (Eng.HudCamera != null)
			{
				Eng.HudCamera.SpectateTarget = playerUnit;
			}
			TrySetCompassToUnit(playerUnit);
			UpdateConditionControllerWithCurrentUnit();
			UpdateTractorTurretRef();
		}

		public void UpdateConditionControllerWithCurrentUnit()
		{
			if (PlayerConditionController != null)
			{
				PlayerConditionController.LocalUnit = PlayerUnit;
				PlayerConditionController.gameObject.SetActive(PlayerUnit != null);
			}
		}

		public void UpdateTractorTurretRef()
		{
			playerTractorTurret = null;
			Unit playerUnit = PlayerUnit;
			if (playerUnit != null && playerUnit.Components != null)
			{
				playerTractorTurret = playerUnit.Components.TractorTurret;
			}
		}

		public void NotifyPlayerTargettedByAI(Unit sourceUnit)
		{
			if (sourceUnit.Sector != EngineASX.Instance.LocalUnit.Sector || (sourceUnit.IsFullyCloaked && !GameController.Instance.GameSettings.GameplaySettings.NotifyPlayerTargettedByCloakedShip) || sourceUnit.IsDocked || Eng.IsPlayerUnitDocked)
			{
				return;
			}
			float num = Vector3.Distance(sourceUnit.SectorPosition, EngineASX.Instance.LocalUnit.SectorPosition);
			NpcPilot npcPilot = sourceUnit.NpcPilot;
			if (!(npcPilot != null) || npcPilot.CombatMode == NpcPilot.AIControllerCombatMode.Offensive || (!(num > 800f) && !sourceUnit.IsFullyCloaked))
			{
				if (sourceUnit.IsInActiveSector && num < GameController.Instance.GameSettings.GameplaySettings.NotifyPlayerTargettedMaxDistanceToTriggerInCombat)
				{
					Eng.NotifyPlayerInCombat();
				}
				lastTargettedByTime = Time.time;
				lastTargettedByFaction = sourceUnit.Faction;
				float num2 = Time.time - lastPlayedHostileTargetsBeep;
				if (num2 > TimeBetweenHostileTargettedMsg && (sourceUnit.Faction != lastPlayerHostileTargetsBeepOpposingFaction || num2 < 120f))
				{
					lastPlayerHostileTargetsBeepOpposingFaction = sourceUnit.Faction;
					HostileTargetsAudioSource.Play();
					lastPlayedHostileTargetsBeep = Time.time;
					string text = null;
					string friendlyName = sourceUnit.GetFriendlyName();
					text = ((!(sourceUnit.Faction != null)) ? $"Targetted by {friendlyName}" : $"Targetted by {friendlyName} ({sourceUnit.Faction.GetShortNameElseLong()})");
					UIController.Instance.QuickMsg.AddMessage(text);
				}
			}
		}

		public GameObject GetHudComponent(HudComponent componentType)
		{
			return componentType switch
			{
				HudComponent.ControlsBg => null, 
				HudComponent.PlayerSpdLabel => SpdLabel.gameObject, 
				HudComponent.CommsButton => CommsButtonRoot, 
				HudComponent.Compass => UnitCompass.gameObject, 
				HudComponent.PlayerCapacitor => PlayerCapacitorRoot, 
				HudComponent.TurretComponents => ComponentManager.GetHudComponent(HudComponent.TurretComponents).gameObject, 
				HudComponent.PlayerHull => PlayerHullRoot, 
				HudComponent.PlayerShields => PlayerShieldsRoot, 
				HudComponent.TargetPanel => TargetRoot, 
				HudComponent.TargetSprites => null, 
				HudComponent.Throttle => ThrottleRoot.gameObject, 
				HudComponent.DockButton => DockButtonRoot, 
				HudComponent.TractorLoot => TractorLootButtonRoot, 
				HudComponent.TurnButtons => TurnButtonsFlashRoot, 
				_ => null, 
			};
		}

		public void SetHudComponentVisible(HudComponent componentType, bool visible)
		{
			GameObject hudComponent = GetHudComponent(componentType);
			if (hudComponent != null)
			{
				hudComponent.SetActive(visible);
			}
		}

		public void RequestDockAtTarget(Unit playerUnit, Unit targetUnit)
		{
			if (!(targetUnit != lastRequestedDock) && !(Time.time > lastRequestDockTime + 3f))
			{
				return;
			}
			lastRequestedDock = targetUnit;
			lastRequestDockTime = Time.time;
			if (targetUnit.Faction == null || targetUnit.Faction.IsPlayerFaction || targetUnit.Faction.RequestDock(targetUnit, playerUnit))
			{
				if (!playerUnit.Components.TryDockInUnit(targetUnit))
				{
					UIController.Instance.QuickMsg.AddMessage("No available bays in hangar");
				}
			}
			else
			{
				UIController.Instance.QuickMsg.AddMessage("Dock Denied");
			}
		}

		public bool AllowDockAtTarget(Unit playerUnit, Unit targetUnit)
		{
			if (playerUnit != null && targetUnit.Faction != null && !targetUnit.Faction.IsHostileToOrAlwaysHostileTo(playerUnit) && !OrdersHelper.IsPilottedByNpc(playerUnit) && playerUnit.Engine.GameSettings.AllowPlayerDock && playerUnit.Engine.AllowPlayerDock && targetUnit != null && targetUnit.IsDockable)
			{
				return true;
			}
			return false;
		}

		public bool IsInDockableRangeOf(Unit playerUnit, Unit dockUnit)
		{
			if (!PlayerUnit.Engine.GameSettings.DebugSettings.IgnoreDockingDistance)
			{
				return Vector3.Distance(playerUnit.transform.position, dockUnit.transform.position) - playerUnit.UnitClass.ShieldRingRadius - dockUnit.UnitClass.ShieldRingRadius < Eng.GameSettings.DockableDistance;
			}
			return true;
		}

		public void SetAutoTurnDesiredAngleY(Vector3 worldPosition)
		{
			float yBearing = Unit.GetYBearing(worldPosition - PlayerUnit.transform.position);
			SetAutoTurnDesiredAngleY(yBearing);
		}

		public void SetAutoTurnDesiredAngleY(float angleY)
		{
			DesiredAngleY = angleY;
			UpdateAutoTurnDesiredBearing();
			if (AutoTurnBearingAboveThreshold())
			{
				AutoTurnAngularArrived = false;
			}
		}

		public void ClearAutoTurnDesiredBearing()
		{
			if (!AutoTurnAngularArrived)
			{
				if (PlayerUnit != null)
				{
					DesiredAngleY = Unit.GetYBearing(PlayerUnit.transform.forward);
				}
				else
				{
					DesiredAngleY = 0f;
				}
				AutoTurnAngularArrived = true;
			}
		}

		protected override void awake()
		{
			instance = this;
			base.awake();
			ClaimShipButton.onClick.AddListener(ClaimCurrentTarget);
			EnterShipButton.onClick.AddListener(EnterCurrentTarget);
			CommsButton.onClick.AddListener(ActivateCommsWithTarget);
			DockButton.onClick.AddListener(TryDockAtTarget);
			TargetInfoButton.onClick.AddListener(ShowTargetInfo);
			TargetContextMenuButton.onClick.AddListener(TargetContextMenuButtonClick);
			if (MatchTargetSpeedButton != null)
			{
				MatchTargetSpeedButton.onClick.AddListener(MatchTargetSpeed);
			}
			TargetOrdersButton.onClick.AddListener(ShowCurrentTargetOrders);
			ScannerDisplayList.Awake();
			if (SectorMapButton != null)
			{
				SectorMapButton.onClick.AddListener(ShowSectorMap);
			}
			if (UniverseMapButton != null)
			{
				UniverseMapButton.onClick.AddListener(ShowUniverseMap);
			}
			CommsButton.gameObject.SetActive(value: false);
			DockButton.gameObject.SetActive(value: false);
			SpdLabel.text = "0";
			if (GameController.Instance.PreferCameraLock)
			{
				CameraMode = HudCameraMode.LockTarget;
			}
		}

		private void TargetContextMenuButtonClick()
		{
			if (currentTarget != null && currentTarget.IsValidAndNotDestroyed && WorldHelper.CanShowUnitContext(currentTarget))
			{
				UIController.Instance.ScreenNavigator.ShowUnitContextMenuScreen(currentTarget);
			}
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			if (Eng != null)
			{
				Eng.InitialiseCamerasForHud();
			}
			foreach (TurretGridUI turretGrid in TurretGrids)
			{
				turretGrid.Refresh();
			}
			UpdateTractorTurretRef();
			RefreshHudPadder();
		}

		protected override void start()
		{
			base.start();
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("HUD: Start", this, 1);
			}
			UpdateTargetMenuButtonEnabled();
			Eng.HudCamera.LostTarget += UnitSpectator_LostTarget;
			Eng.DialogController.CommsStageShown += DialogController_DialogShown;
			Eng.PlayerUnitChanged += Engine_PlayerUnitChanged;
			if (PlayerUnit != null)
			{
				playerUnitHasChanged = true;
				OnPlayerUnitChanged(null);
			}
			ApplyShieldsDisplayType();
		}

		protected override void update()
		{
			base.update();
			Unit playerUnit = Eng.PlayerUnit;
			if (playerUnitHasChanged)
			{
				playerUnitHasChanged = false;
				OnPlayerUnitChanged(oldPlayerUnit);
			}
			if (playerUnit != null && playerUnit.ActiveUnit != null)
			{
				if (PlayerUnit.Components.AutoTurretModule != null)
				{
					PlayerUnit.Components.AutoTurretModule.PreferredTurretTarget = currentTarget;
				}
				if (currentTarget != null && Eng.HudCamera.HasChangeTargetCooldownExpired)
				{
					Eng.HudCamera.SetHudTarget(HudTarget.CreateFromUnit(currentTarget));
				}
				ComponentManager.SetHudComponentInteractable(HudComponent.TurretComponents, !Eng.PlayerUnitAutoPilotEnabled);
				if (Eng.IsPlayerPilotAndHudCurrentPanel)
				{
					PlayerShipControl(playerUnit);
				}
				else
				{
					ClearAutoTurnDesiredBearing();
					PilotUnitWhenPossible(playerUnit);
				}
				if (IsCurrentScreen)
				{
					UpdateMiscHudInput();
				}
				UpdateAutoTurnSprite();
				RefreshDockButton();
				RefreshTurnButtonVisibility();
				RefreshCommsButtonVisibility();
				UpdateSpeedLabelText();
				if (currentTarget != null)
				{
					CacheDistanceToCurrentTarget(playerUnit);
				}
				else if (Eng.HudCamera.HasTarget && Eng.HudCamera.CurrentHudTarget.Value.Unit != null)
				{
					Eng.HudCamera.RemoveTargetWithcooldown();
				}
				HUDScannerDisplayListController.Tick();
			}
			RefreshButtonEnabledStates();
			if (currentTarget != null && Input.GetKeyDown(KeyCode.Backspace))
			{
				CurrentTarget = null;
			}
		}

		private void UpdateMiscHudInput()
		{
			if (Input.GetKeyDown(KeyCode.T))
			{
				TargetInViewSelector.SelectTargetInView();
			}
			if (Input.GetKeyDown(KeyCode.V))
			{
				ToggleCameraMode();
			}
		}

		protected override void lateUpdate()
		{
			HudTargetController.Tick();
		}

		private void PilotUnitWhenPossible(Unit playerUnit)
		{
			if (playerUnit.IsOwnedByPlayer && playerUnit.UnitClass.IsPilottable && !OrdersHelper.IsPilottedByNpc(playerUnit))
			{
				playerUnit.Components.PilotPerson = Eng.LocalPlayer.Person;
			}
		}

		private void CacheDistanceToCurrentTarget(Unit playerUnit)
		{
			lastDistToTarget = Vector3.Distance(playerUnit.transform.position, CurrentTarget.transform.position);
		}

		protected override void onDisable()
		{
			base.onDisable();
			if (PlayerUnit != null && PlayerUnit.ActiveUnit != null)
			{
				ClearDesiredTurn();
			}
			if (InGameMenuController != null)
			{
				InGameMenuController.CurrentGameMenu = null;
			}
			ScannerDisplayList.CleanupOnDisable();
			foreach (TurretGridUI turretGrid in TurretGrids)
			{
				turretGrid.CleanupOnDisable();
			}
			DockButton.gameObject.SetActive(value: false);
			CommsButton.gameObject.SetActive(value: false);
			QuickTractorController.CleanupOnDisable();
		}

		public void RefreshTurretGrids()
		{
			foreach (TurretGridUI turretGrid in TurretGrids)
			{
				turretGrid.Refresh();
			}
		}

		protected override bool onNavigatingBack()
		{
			UIController.Instance.ScreenNavigator.ShowPauseMenu(Eng.IsPaused);
			return false;
		}

		private void ShowCurrentTargetOrders()
		{
			if (currentTarget != null && CanShowOrdersForUnit(currentTarget))
			{
				UIController.Instance.ScreenNavigator.ShowOrdersScreen(currentTarget);
			}
		}

		private void MatchTargetSpeed()
		{
			if (currentTarget != null)
			{
				float currentMaxSpeed = PlayerUnit.Components.CurrentMaxSpeed;
				if (currentMaxSpeed > 0f)
				{
					PlayerUnit.Components.EngineThrottle = currentTarget.CurrentSpeed / currentMaxSpeed;
				}
				else
				{
					PlayerUnit.Components.EngineThrottle = 0f;
				}
			}
		}

		private void ShowTargetInfo()
		{
			if (currentTarget != null)
			{
				UIController.Instance.ScreenNavigator.ShowUnitInfoScreen(currentTarget);
			}
		}

		private void ShowSectorMap()
		{
			if (PlayerUnit.Sector != null)
			{
				UIController.Instance.ScreenNavigator.ShowSectorMapScreenWhenPilotting();
			}
			else
			{
				Debug.LogError("Cannot show sector map. Player does not have a scene");
			}
		}

		private void ShowUniverseMap()
		{
			UIController.Instance.ScreenNavigator.ShowCustomUniverseMapScreen((UniverseMapScreen screen) =>
			{
				screen.AutoSelectPlayerSector();
				screen.CenterOnPlayerSector();
			});
		}

		private void SetDesiredBearingAtMouse(Vector3 screenPosition)
		{
			Ray ray = GameController.Instance.MainCamera.ScreenPointToRay(screenPosition);
			Plane plane = new Plane(Vector3.up, 0f);
			float enter = 0f;
			if (plane.Raycast(ray, out enter))
			{
				Vector3 point = ray.GetPoint(enter);
				point.y = 0f;
				SetAutoTurnDesiredAngleY(point);
			}
		}

		private void Engine_PlayerUnitChanged(EngineASX sender, Unit oldUnit)
		{
			ClearAutoTurnDesiredBearing();
			InGameMenuController.Close();
			ShipComponentsController.PlayerUnit = PlayerUnit;
			ShipComponentsController.OnPlayerUnitChanged();
			ShipComponentsController.Refresh();
			playerUnitHasChanged = true;
			oldPlayerUnit = oldUnit;
		}

		private void DialogController_DialogShown(DialogController sender, ICommsStage commsStage)
		{
			Person ownerPilot = commsStage.Handler.OwnerPilot;
			if (ownerPilot != null)
			{
				SpeechModel.RemoveRequestsFromPilotInPast(ownerPilot);
			}
		}

		private void UnitSpectator_LostTarget(HudCamera sender, Unit lostTarget)
		{
		}

		private void ControlShipWithKeys()
		{
			if (AllowPlayerSteering)
			{
				if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
				{
					PlayerUnit.ActiveUnit.ActiveUnitShip.DesiredTurn -= TurnButtonSensitivity * Time.deltaTime;
					ClearAutoTurnDesiredBearing();
				}
				if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
				{
					PlayerUnit.ActiveUnit.ActiveUnitShip.DesiredTurn += TurnButtonSensitivity * Time.deltaTime;
					ClearAutoTurnDesiredBearing();
				}
			}
			if (CanUseThrottle())
			{
				if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
				{
					PlayerUnit.Components.EngineThrottle += ThrottleButtonSensitivity * Time.deltaTime;
				}
				if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
				{
					PlayerUnit.Components.EngineThrottle -= ThrottleButtonSensitivity * Time.deltaTime;
				}
			}
		}

		private void ToggleCameraMode()
		{
			switch (CameraMode)
			{
			case HudCameraMode.FixedForward:
				CameraMode = HudCameraMode.LockTarget;
				break;
			case HudCameraMode.LockTarget:
				CameraMode = HudCameraMode.FixedForward;
				break;
			case HudCameraMode.Free:
				CameraMode = HudCameraMode.FixedForward;
				break;
			}
		}

		private void RefreshDockButton()
		{
			if (Time.time > nextCanDockCheck)
			{
				DockButton.gameObject.SetActive(PlayerUnit != null && currentTarget != null && CanRequestDockAtUnit(PlayerUnit, currentTarget));
				nextCanDockCheck = Time.time + 1f;
			}
		}

		private void RefreshButtonEnabledStates()
		{
			TargetInfoButton.interactable = WorldHelper.CanShowUnitInfo(currentTarget);
			if (MatchTargetSpeedButton != null)
			{
				MatchTargetSpeedButton.interactable = currentTarget != null;
			}
			EnterShipButton.interactable = currentTarget != null && CanEnterUnit(currentTarget, lastDistToTarget);
			ClaimShipButton.interactable = currentTarget != null
				&& (CanClaimUnit(currentTarget, lastDistToTarget)
					|| CanAbandonUnit(currentTarget));

			RefreshClaimButtonIcon();
			TargetOrdersButton.interactable = currentTarget != null && CanShowOrdersForUnit(currentTarget);
		}

		private void PlayerShipControl(Unit playerUnit)
		{
			ControlShipWithKeys();
			if (!UI.IsMobileDevice)
			{
				UpdateAutoTurn();
			}
			else if (AllowPlayerSteering && GameController.Instance.IsTiltControlEnabled)
			{
				UpdateTurnFromAccelerometer();
			}
			if ((!AutoTurnEnabled || AutoTurnAngularArrived) && !GameController.Instance.IsTiltControlEnabled)
			{
				MoveDesiredTurnToZero(playerUnit);
			}
		}

		private void RefreshCommsButtonVisibility()
		{
			ICommsHandler commsHandler = null;
			bool flag = ShouldShowCommsButton(out commsHandler);
			if (flag != CommsButton.gameObject.activeSelf)
			{
				CommsButton.gameObject.SetActive(flag);
			}
		}

		private void UpdateSpeedLabelText()
		{
			if (SpdLabel != null && Time.time > lastSpeedLabelUpdate + 0.09f)
			{
				spdLabelStringBuilder.Length = 0;
				Rigidbody rBody = PlayerUnit.RBody;
				if (rBody != null)
				{
					spdLabelStringBuilder.Append(Mathf.RoundToInt(rBody.linearVelocity.magnitude));
					SpdLabel.SetText(spdLabelStringBuilder);
				}
				else
				{
					SpdLabel.text = null;
				}
				lastSpeedLabelUpdate = Time.time;
			}
		}

		private void UpdateAutoTurnSprite()
		{
			float num = Mathf.Abs(desiredBearing);
			bool flag = !AutoTurnAngularArrived && num > TurnSpriteThreshold;
			TurnSpriteRoot.gameObject.SetActive(flag);
			if (flag)
			{
				Vector3 position = PlayerUnit.transform.position;
				Vector3 eulerAngles = PlayerUnit.transform.rotation.eulerAngles;
				GameObject gameObject = null;
				GameObject gameObject2 = null;
				if (desiredBearing > 0f)
				{
					gameObject = ShipTurnSpriteR;
					gameObject2 = ShipTurnSpriteL;
				}
				else
				{
					gameObject = ShipTurnSpriteL;
					gameObject2 = ShipTurnSpriteR;
				}
				float num2 = num / 180f;
				float value = 1f - num2 * 0.5f;
				gameObject.GetComponent<Renderer>().material.SetFloat("_Cutoff", value);
				float num3 = PlayerUnit.UnitClass.ShieldRingRadius * TurnSpriteShieldRingMultiplier;
				gameObject.transform.localScale = new Vector3(num3, num3, num3);
				gameObject.transform.position = position + TurnSpritePositionOffset;
				Vector3 localEulerAngles = new Vector3(90f, eulerAngles.y, 0f);
				gameObject.transform.localEulerAngles = localEulerAngles;
				gameObject.gameObject.SetActive(value: true);
				gameObject2.gameObject.SetActive(value: false);
			}
		}

		private bool SetCurrentTargetToTargetAtMouse(Vector3 touchPosition)
		{
			Unit targetAtScreenPos = TargetInScreenSelector.GetTargetAtScreenPos(touchPosition, currentTarget);
			if (targetAtScreenPos != null && !IsUnitValidAsCurrentTarget(targetAtScreenPos))
			{
				return false;
			}
			if (CurrentTarget != targetAtScreenPos && (targetAtScreenPos != null || AllowDeselectTargetWithMouse))
			{
				CurrentTarget = targetAtScreenPos;
				return true;
			}
			return false;
		}

		private void playerTarget_Killed(Unit sender, DestroyedUnitArgs args)
		{
			ClearTarget();
		}

		public void ClearTarget()
		{
			if (Eng != null)
			{
				CurrentTarget = null;
			}
		}

		public void ClearTargetIfInvalid()
		{
			if (Eng != null && currentTarget != null && !IsCurrentTargetValid())
			{
				CurrentTarget = null;
			}
		}

		public bool IsCurrentTargetValid()
		{
			return IsUnitValidAsCurrentTarget(currentTarget);
		}

		public bool IsUnitValidAsCurrentTarget(Unit unit)
		{
			if (unit == null || !unit.IsValidAndNotDestroyed)
			{
				return false;
			}
			if (!unit.Sector.IsActive || unit.Sector != EngineASX.Instance.LocalUnit.Sector)
			{
				return false;
			}
			if (unit.IsDiscoverableType && !EngineASX.Instance.LocalFaction.Intel.IsUnitDiscoveredOrOwned(unit, GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeWhenPlayerPilotting))
			{
				return false;
			}
			UnitType unitType = unit.UnitType;
			if (unitType == UnitType.Wormhole || unitType == UnitType.Asteroid || unitType == UnitType.Waypoint)
			{
				return true;
			}
			if (!unit.IsTargettable(EngineASX.Instance.LocalFaction) || unit.HasSameRootUnitAs(EngineASX.Instance.LocalUnit))
			{
				return false;
			}
			if (GameController.Instance.GameSettings.GameplaySettings.ClearPlayerTargetWhenInvalidInstantly && !FactionIntel.IsStaticOrTreatAsStatic(unit))
			{
				float distanceIgnoreY = Maths.GetDistanceIgnoreY(EngineASX.Instance.LocalUnit.SectorPosition, unit.SectorPosition);
				if (FactionIntelProcessor.IsTargetOutOfDetectionRange(EngineASX.Instance.LocalUnit, unit, distanceIgnoreY))
				{
					EngineASX.Instance.LocalFaction.Intel.ChangeTimeOfDiscovery(unit, 0f - GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeWhenPlayerPilotting);
					return true;
				}
			}
			return true;
		}

		private void UpdateTargetMenuButtonEnabled()
		{
			TargetMenuButton.interactable = currentTarget != null;
		}

		private void TrySetCompassToUnit(Unit unit)
		{
			if (UnitCompass != null)
			{
				UnitCompass.TargetUnit = unit;
			}
		}

		private bool AutoTurnBearingAboveThreshold()
		{
			return Mathf.Abs(desiredBearing) > 0.1f;
		}

		private void UpdateAutoTurnDesiredBearing()
		{
			desiredBearing = Maths.WrapValue(DesiredAngleY - PlayerUnit.transform.localEulerAngles.y, -180f, 180f);
		}

		private void AutoTurnAngularArrive()
		{
			newDesiredTurn = 0f;
			ClearDesiredTurn();
			if (PlayerUnit.ActiveUnit.ActiveUnitShip != null)
			{
				PlayerUnit.ActiveUnit.ActiveUnitShip.CurrentTurn = 0f;
			}
			AutoTurnAngularArrived = true;
			float yBearing = Unit.GetYBearing(PlayerUnit.transform.forward);
			PlayerUnit.transform.localRotation = Quaternion.Euler(new Vector3(0f, yBearing, 0f));
		}

		private void UpdateTurnFromAccelerometer()
		{
			float num = Input.acceleration.x - GameController.Instance.TiltControlDefaultAccelerationX;
			float turn = 0f;
			if (num != 0f)
			{
				turn = Mathf.Pow(Mathf.Abs(num), GameController.Instance.TiltPower) * Mathf.Sign(num) * GameController.Instance.TiltSensitivity;
			}
			PlayerUnit.ActiveUnit.ActiveUnitShip.DesiredTurn = GetNewShipDesiredTurn(turn);
		}

		private float GetNewShipDesiredTurn(float turn)
		{
			return Mathf.Lerp(PlayerUnit.ActiveUnit.ActiveUnitShip.DesiredTurn, turn, 2f * Time.deltaTime);
		}

		private void UpdateAutoTurn()
		{
			if (PlayerUnit.IsActiveInEngine && !AutoTurnAngularArrived)
			{
				UpdateAutoTurnDesiredBearing();
				ActiveUnitShip activeUnitShip = PlayerUnit.ActiveUnit.ActiveUnitShip;
				if (Mathf.Abs(desiredBearing + activeUnitShip.CurrentTurn * Time.deltaTime) < 0.1f)
				{
					AutoTurnAngularArrive();
				}
				else
				{
					newDesiredTurn = NpcPilot.ApplyDesiredBearing(desiredBearing, activeUnitShip.CurrentTurn, PlayerUnit.UnitClass.TurnAcceleration, PlayerUnit.UnitClass.turnRate, Eng.GameSettings.ActiveUnitTurnArrivalAggression);
				}
				if (activeUnitShip.DesiredTurn != newDesiredTurn)
				{
					activeUnitShip.DesiredTurn = Mathf.MoveTowards(activeUnitShip.DesiredTurn, newDesiredTurn, Eng.GameSettings.AIActivePilotTurnChangeRate * Time.deltaTime);
				}
			}
		}

		private void EnterCurrentTarget()
		{
			if (CanEnterUnit(currentTarget, lastDistToTarget))
			{
				Eng.ChangePlayerUnit(currentTarget);
				EngineASX.Instance.PlayChangeShipAudio();
			}
		}

		private void ClaimCurrentTarget()
		{
			if (currentTarget == null)
			{
				return;
			}

			// Your own ship abandons; anything else claims. Checked in that
			// order because abandoning is the one that changes the world and
			// therefore has to go to the host.
			if (CanAbandonUnit(currentTarget))
			{
				AbandonShip(currentTarget);
				return;
			}

			if (CanClaimUnit(currentTarget, lastDistToTarget))
			{
				Eng.LocalFaction.ClaimUnit(currentTarget, null);
				TargetInfoUI.Invalidate();
			}
		}

		/// <summary>
		/// Whether this ship is the player's to abandon.
		/// </summary>
		/// <remarks>
		/// Three things, and each is there for a reason.
		///
		/// Their faction, because abandoning a ship you do not own is not a
		/// thing - it is the claim button's job, and the two cases cannot
		/// both be true because a claimable ship belongs to nobody.
		///
		/// Their own person flying it, because abandoning hands the ship over
		/// by taking the pilot out of it. A ship in a fleet has a different
		/// pilot and a different meaning entirely - that is selling, and it
		/// goes through the dock.
		///
		/// And not their last ship. Abandoning sets the pilot's CurrentUnit to
		/// null, and nothing in this game handles a player with no ship: that
		/// state does not exist anywhere outside the NPC code. With ship
		/// buying not yet relayed, a player's shuttle is currently the only
		/// ship they have, so allowing this would let a player strand
		/// themselves with no ship and no way back.
		///
		/// Not reusing DitchUnitModule.CouldDitch, which is the other thing
		/// that decides whether a ship may be ditched: that one returns true
		/// only for a ship with an NPC pilot, so it is exactly false for the
		/// ship a player is flying. It is the AI's gate, and asking it would
		/// have made this button permanently inert.
		/// </remarks>
		private bool CanAbandonUnit(Unit unit)
		{
			if (unit == null || unit.IsDestroyed)
			{
				return false;
			}

			Faction theirs = Eng.LocalFaction;

			if (theirs == null || unit.Faction != theirs)
			{
				return false;
			}

			if (unit.UnitType != UnitType.Ship || !unit.IsActiveInEngine)
			{
				return false;
			}

			Person pilot = unit.GetPilot();

			if (pilot == null || pilot != Eng.LocalPlayer.Person)
			{
				return false;
			}

			// Never the last one. See above - there is no game state for a
			// player with no ship.
			return theirs.GetCountOfUnitType(UnitType.Ship) > 1;
		}

		/// <summary>
		/// Asks the host to abandon a ship. The host decides.
		/// </summary>
		/// <remarks>
		/// Abandoning changes the world: the ship stops belonging to anybody
		/// and becomes free for either side to take. So it cannot happen on
		/// this copy of the world - it has to be asked for, like every other
		/// action a client takes.
		///
		/// Returns false on a host and in singleplayer, where the ship is
		/// abandoned here as it always was.
		/// </remarks>
		private void AbandonShip(Unit unit)
		{
			if (Authority.AbandonShipRequest != null
				&& Authority.AbandonShipRequest(unit.UniqueId))
			{
				return;
			}

			EngineASX.Instance.DitchUnitModule.DitchUnitWithAbandon(
				unit, null, null, DamageDirectType.Direct);

			TargetInfoUI.Invalidate();
		}

		private bool CanEnterUnit(Unit unit, float distance)
		{
			return WorldHelper.CanEnterUnit(PlayerUnit, unit, distance);
		}

		/// <summary>
		/// Swaps the claim button's icon when the button means something else.
		/// </summary>
		/// <remarks>
		/// Only the icon changes. The button, its position and its wiring are
		/// the same object either way, which is the whole reason for doing it
		/// this way rather than adding a second button - every menu in this
		/// game is a fixed list of prefab buttons, and a hand-edited prefab
		/// fails silently rather than loudly.
		///
		/// Says so once if the sprite was never assigned. Silently showing the
		/// claim icon on an abandon button would be worse than showing nothing:
		/// the player would be about to give away a ship.
		/// </remarks>
		private void RefreshClaimButtonIcon()
		{
			if (ClaimShipButton == null)
			{
				return;
			}

			bool abandoning = CanAbandonUnit(currentTarget);
			Image image = ClaimShipButton.GetComponentInChildren<Image>();

			if (image == null)
			{
				return;
			}

			// Captured rather than remembered from Awake, because there is no
			// Awake here and adding one for this would be a larger change than
			// it looks. The first refresh happens while the button still means
			// "claim" - the player cannot be abandoning anything yet - so this
			// always takes the claim sprite.
			if (claimButtonIcon == null && !abandoning)
			{
				claimButtonIcon = image.sprite;
			}

			if (abandoning && AbandonShipIcon == null)
			{
				if (!warnedAboutAbandonIcon)
				{
					warnedAboutAbandonIcon = true;

					Debug.LogWarning("The HUD's claim button means \"abandon ship\""
						+ " on your own ship, but AbandonShipIcon has no sprite"
						+ " assigned, so it is showing the claim icon instead."
						+ " Assign one on the HudScreen component.");
				}

				return;
			}

			image.sprite = abandoning ? AbandonShipIcon : claimButtonIcon;
		}

		private bool warnedAboutAbandonIcon;

		/// <summary>The claim button's own icon, captured on Awake.</summary>
		private Sprite claimButtonIcon;

		private bool CanClaimUnit(Unit unit, float distance)
		{
			distance -= unit.UnitClass.ShieldRingRadius;
			distance -= PlayerUnit.GetRootUnit().UnitClass.ShieldRingRadius;
			if (distance < Eng.GameSettings.ClaimShipDistance)
			{
				return ClaimUnitHelper.CanClaimUnit(unit);
			}
			return false;
		}

		public void ApplyShieldsDisplayType()
		{
			if (GameController.Instance.PlayerOptions.UI_ScreenSpaceShields)
			{
				PlayerConditionController = ScreenSpaceConditionController;
				ScreenSpaceConditionController.gameObject.SetActive(value: true);
				WorldSpaceConditionController.gameObject.SetActive(value: false);
			}
			else
			{
				PlayerConditionController = WorldSpaceConditionController;
				ScreenSpaceConditionController.gameObject.SetActive(value: false);
				WorldSpaceConditionController.gameObject.SetActive(value: true);
			}
			UpdateConditionControllerWithCurrentUnit();
		}

		protected override void OnNavigateBackFromEscapeKey()
		{
			UIController.Instance.ScreenNavigator.ShowPauseMenu(EngineASX.Instance.IsPaused);
		}

		public override bool ShouldShowDockHeader()
		{
			if (!EngineASX.Instance.World.Permissions.AllowHudViewOptions)
			{
				return true;
			}
			if (GameController.Instance.PlayerOptions.UI_ShowHudHeader)
			{
				return base.ShouldShowDockHeader();
			}
			return false;
		}

		public void RefreshHudPadder()
		{
			if (EngineASX.LoadedAndReady)
			{
				Vector2 anchoredPosition = Padder.anchoredPosition;
				anchoredPosition.y = (ShouldShowDockHeader() ? (-50f) : 0f);
				Padder.anchoredPosition = anchoredPosition;
				Padder.sizeDelta = anchoredPosition;
			}
		}
	}
}
