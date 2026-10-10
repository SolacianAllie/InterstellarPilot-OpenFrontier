using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.Engine.Fleets.FleetFormations;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.Engine.WorldGeneration.Models;
using OpenFrontier.IP.Engine.WorldSeeding;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.IP.Scratchcard.UI;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.ActiveMission;
using OpenFrontier.IP.UI.Screens.AdjustSafeArea;
using OpenFrontier.IP.UI.Screens.Battles;
using OpenFrontier.IP.UI.Screens.Bounty;
using OpenFrontier.IP.UI.Screens.BuildMode;
using OpenFrontier.IP.UI.Screens.BuildModePlacement;
using OpenFrontier.IP.UI.Screens.BuySectorIntel;
using OpenFrontier.IP.UI.Screens.BuySectorIntelConfirm;
using OpenFrontier.IP.UI.Screens.CargoPicker;
using OpenFrontier.IP.UI.Screens.CargoPrices;
using OpenFrontier.IP.UI.Screens.CargoTrade;
using OpenFrontier.IP.UI.Screens.CargoTransfer;
using OpenFrontier.IP.UI.Screens.ComponentTrade;
using OpenFrontier.IP.UI.Screens.CreateUnitVariant;
using OpenFrontier.IP.UI.Screens.DockedShips;
using OpenFrontier.IP.UI.Screens.EnterNumber;
using OpenFrontier.IP.UI.Screens.EnterSlider;
using OpenFrontier.IP.UI.Screens.FactionSettings;
using OpenFrontier.IP.UI.Screens.FactionTransactions;
using OpenFrontier.IP.UI.Screens.FleetCargo;
using OpenFrontier.IP.UI.Screens.FleetFormations;
using OpenFrontier.IP.UI.Screens.FleetOrderSettings;
using OpenFrontier.IP.UI.Screens.FleetPicker;
using OpenFrontier.IP.UI.Screens.FleetSettings;
using OpenFrontier.IP.UI.Screens.Fleets;
using OpenFrontier.IP.UI.Screens.Hypersleep;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.MultiCargoPicker;
using OpenFrontier.IP.UI.Screens.NewFleetOrder;
using OpenFrontier.IP.UI.Screens.Orders.PatrolOrderCreator;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using OpenFrontier.IP.UI.Screens.RequestTaxi;
using OpenFrontier.IP.UI.Screens.SectorMap;
using OpenFrontier.IP.UI.Screens.SectorPositionContextMenu;
using OpenFrontier.IP.UI.Screens.ShipScan;
using OpenFrontier.IP.UI.Screens.ShipTrader;
using OpenFrontier.IP.UI.Screens.Skirmish;
using OpenFrontier.IP.UI.Screens.Skirmish.SkirmishTeamSetup;
using OpenFrontier.IP.UI.Screens.Test;
using OpenFrontier.IP.UI.Screens.Test.ChangeUnitCargo;
using OpenFrontier.IP.UI.Screens.TopPilots;
using OpenFrontier.IP.UI.Screens.UnitCargo;
using OpenFrontier.IP.UI.Screens.UnitClassPicker;
using OpenFrontier.IP.UI.Screens.UnitContextMenu;
using OpenFrontier.IP.UI.Screens.UnitInfo;
using OpenFrontier.IP.UI.Screens.UnitPicker;
using OpenFrontier.IP.UI.Screens.UniverseBlueprintMap;
using OpenFrontier.IP.UI.Screens.UniverseGameType;
using OpenFrontier.IP.UI.Screens.UniverseMap;
using OpenFrontier.IP.UI.Screens.UniverseScenePicker;
using OpenFrontier.IP.UI.SellShip;
using OpenFrontier.IP.billing;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public static class ScreenNavigatorExtensions
	{
		public static void ShowChangeUnitCargoScreen(this ScreenNavigator screenNavigator, Unit unit, Action<ChangeUnitCargoScreen> setupAction)
		{
			if (unit.CargoBayComponent == null)
			{
				Debug.LogError("Cannot modify cargo for this unit as it doesn't have a cargo bay", unit);
				return;
			}
			ScreenNavigationRequest<ChangeUnitCargoScreen> screenNavigationRequest = new ScreenNavigationRequest<ChangeUnitCargoScreen>();
			screenNavigator.NavigateToScreen(screenNavigationRequest);
			ChangeUnitCargoScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.Unit = unit;
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowSkirmishTeamSetupScreen(this ScreenNavigator screenNavigator, SkirmishTeam team, Action<SkirmishTeamSetupScreen> setupAction)
		{
			ScreenNavigationRequest<SkirmishTeamSetupScreen> screenNavigationRequest = new ScreenNavigationRequest<SkirmishTeamSetupScreen>();
			UIController.Instance.ScreenNavigator.NavigateToScreen(screenNavigationRequest);
			SkirmishTeamSetupScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowManageCustomUnitVariantsScreen(this ScreenNavigator screenNavigator)
		{
			ScreenNavigationRequest<ManageUnitVariantsScreen> screenNavigationRequest = new ScreenNavigationRequest<ManageUnitVariantsScreen>();
			screenNavigator.NavigateToScreen(screenNavigationRequest);
			screenNavigationRequest.ResultConcrete.Refresh();
		}

		public static void ShowCreateShipVariantWizard(this ScreenNavigator screenNavigator, List<UnitClass> unitClassesFilter)
		{
			ScreenNavigationRequest<UnitClassPickerScreen> screenNavigationRequest = new ScreenNavigationRequest<UnitClassPickerScreen>();
			screenNavigator.NavigateToScreen(screenNavigationRequest);
			UnitClassPickerScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.KeepInNavigationStack = false;
			resultConcrete.UnitClassFilter = unitClassesFilter;
			resultConcrete.Title = "Select class to customize...";
			resultConcrete.UnitClassPicked += (UnitClass unitClass) =>
			{
				string newUnitVariantName = CustomUnitVariantHelper.GetNewUnitVariantName(unitClass);
				CustomUnitVariant customUnitVariant = new CustomUnitVariant
				{
					UnitClass = unitClass,
					Name = newUnitVariantName
				};
				screenNavigator.ShowCreateCustomVariantScreen(customUnitVariant, isNewItem: true, null);
			};
			resultConcrete.Refresh();
		}

		public static void ShowCreateCustomVariantScreen(this ScreenNavigator screenNavigator, CustomUnitVariant customUnitVariant, bool isNewItem, string originalItemFileName, Action<CreateUnitVariantScreen> setupAction = null)
		{
			ScreenNavigationRequest<CreateUnitVariantScreen> screenNavigationRequest = new ScreenNavigationRequest<CreateUnitVariantScreen>();
			screenNavigator.NavigateToScreen(screenNavigationRequest);
			CreateUnitVariantScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.IsItemDirty = isNewItem;
			resultConcrete.CustomUnitVariant = customUnitVariant;
			resultConcrete.OriginalUnitVariantFileName = originalItemFileName;
			if (!isNewItem)
			{
				resultConcrete.CacheOriginalVariant();
			}
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowCreateUnitVariantCargoScreen(this ScreenNavigator screenNavigator, CustomUnitVariant customUnitVariant)
		{
			ScreenNavigationRequest<CreateUnitVariantCargoScreen> screenNavigationRequest = new ScreenNavigationRequest<CreateUnitVariantCargoScreen>();
			screenNavigator.NavigateToScreen(screenNavigationRequest);
			CreateUnitVariantCargoScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.CustomUnitVariant = customUnitVariant;
			resultConcrete.Refresh();
		}

		public static void ShowCreateUnitVariantComponentsScreen(this ScreenNavigator screenNavigator, CustomUnitVariant customUnitVariant)
		{
			ScreenNavigationRequest<CreateUnitVariantComponentsScreen> screenNavigationRequest = new ScreenNavigationRequest<CreateUnitVariantComponentsScreen>();
			screenNavigator.NavigateToScreen(screenNavigationRequest);
			CreateUnitVariantComponentsScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.UnitVariant = customUnitVariant;
			resultConcrete.Refresh();
		}

		public static void ShowPauseMenu(this ScreenNavigator uiController, bool wasPaused)
		{
			ScreenNavigationRequest<PauseMenuUI> screenNavigationRequest = new ScreenNavigationRequest<PauseMenuUI>();
			uiController.NavigateToScreen(screenNavigationRequest);
			PauseMenuUI resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.WasPaused = wasPaused;
			resultConcrete.Eng.IsPaused = true;
			resultConcrete.Refresh();
		}

		public static void ShowAdjustSafeAreaScreen(this ScreenNavigator uiController, Action callback = null)
		{
			ScreenNavigationRequest<AdjustSafeAreaScreen> screenNavigationRequest = new ScreenNavigationRequest<AdjustSafeAreaScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			AdjustSafeAreaScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.AdjustedCallback = callback;
			resultConcrete.Refresh();
		}

		public static void ShowBuySectorIntelConfirmScreen(this ScreenNavigator uiController, IEnumerable<Unit> units)
		{
			ScreenNavigationRequest<BuySectorIntelConfirmScreen> screenNavigationRequest = new ScreenNavigationRequest<BuySectorIntelConfirmScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			BuySectorIntelConfirmScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			if (units != null)
			{
				resultConcrete.Units.AddRange(units);
			}
			resultConcrete.Refresh();
		}

		public static void ShowFactionSettingsScreen(this ScreenNavigator uiController, Faction faction)
		{
			ScreenNavigationRequest<FactionSettingsScreen> screenNavigationRequest = new ScreenNavigationRequest<FactionSettingsScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			FactionSettingsScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.Faction = faction;
			resultConcrete.Refresh();
		}

		public static void ShowScreenType(this ScreenNavigator uiController, UIScreenType screenType, GameObject screenContext = null)
		{
			if (screenType == UIScreenType.OrdersScreen)
			{
				Unit component = screenContext.GetComponent<Unit>();
				if (component != null)
				{
					uiController.ShowOrdersScreen(component);
				}
			}
			else
			{
				Debug.LogFormat(uiController, "Unable to show screen type: {0}. Not implemented", screenType);
			}
		}

		public static void ShowProjectilesScreen(this ScreenNavigator uiController, ProjectileTurretClass turretClass)
		{
			ScreenNavigationRequest<CompatibleProjectilesScreen> screenNavigationRequest = new ScreenNavigationRequest<CompatibleProjectilesScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			CompatibleProjectilesScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.TurretClass = turretClass;
			resultConcrete.Refresh();
		}

		public static void ShowRequestTaxiScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<RequestTaxiScreen> screenNavigationRequest = new ScreenNavigationRequest<RequestTaxiScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			screenNavigationRequest.ResultConcrete.Refresh();
		}

		public static void ShowActiveMissionScreen(this ScreenNavigator uiController, Mission mission)
		{
			ScreenNavigationRequest<MissionScreen> screenNavigationRequest = new ScreenNavigationRequest<MissionScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			MissionScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.Mission = mission;
			resultConcrete.Refresh();
			resultConcrete.RefreshMissionStateChangeTime();
		}

		public static void ShowUniverseSectorPicker(this ScreenNavigator uiController, bool allowNone, bool allowMultiple, List<Sector> sectors, List<Sector> selectedSectors, string title = "Select Sectors...", SectorPickerScreen.ScenePickedHandler callback = null)
		{
			ScreenNavigationRequest<SectorPickerScreen> screenNavigationRequest = new ScreenNavigationRequest<SectorPickerScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			SectorPickerScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.AllowNone = allowNone;
			resultConcrete.ItemList.IsMultiSelectEnabled = allowMultiple;
			if (allowNone)
			{
				SectorPickerItem item = new SectorPickerItem
				{
					Sector = null
				};
				resultConcrete.ItemList.Add(item);
			}
			foreach (Sector sector in sectors)
			{
				SectorPickerItem item2 = new SectorPickerItem
				{
					Sector = sector
				};
				resultConcrete.ItemList.Add(item2);
				resultConcrete.ItemList.SetItemSelected(item2, selectedSectors?.Contains(sector) ?? false);
			}
			resultConcrete.Refresh();
			resultConcrete.TitleText.text = title;
			resultConcrete.ScenePicked += callback;
		}

		public static void ShowFleetFormationStylePickerScreen(this ScreenNavigator uiController, FleetFormationStyle currentFormationStyle, Action<FleetFormationStylePickerScreen, FleetFormationStyle> callback = null)
		{
			ScreenNavigationRequest<FleetFormationStylePickerScreen> screenNavigationRequest = new ScreenNavigationRequest<FleetFormationStylePickerScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			FleetFormationStylePickerScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.DefaultItem = currentFormationStyle;
			resultConcrete.Refresh();
			resultConcrete.Callback = callback;
		}

		public static void ShowUnitPickerScreen(this ScreenNavigator uiController, List<Unit> units, Sector localSector, bool allowNone = true, UnitPickerScreen.UnitPickedHandler callback = null, string title = "Select Unit..", bool keepInNavigationStack = false)
		{
			ScreenNavigationRequest<UnitPickerScreen> screenNavigationRequest = new ScreenNavigationRequest<UnitPickerScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			UnitPickerScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.KeepInNavigationStack = keepInNavigationStack;
			resultConcrete.AllowNone = allowNone;
			resultConcrete.Units = units;
			resultConcrete.LocalSector = localSector;
			resultConcrete.Refresh();
			resultConcrete.TitleText.text = title;
			resultConcrete.UnitPickResult += callback;
		}

		public static void ShowFleetPicker(this ScreenNavigator uiController, List<Fleet> fleets, FleetPickerScreen.FleetPickedHandler callback, bool allowNone, bool allowCreateNew, string title = "Select fleet..", bool keepInNavigationStack = false)
		{
			ScreenNavigationRequest<FleetPickerScreen> screenNavigationRequest = new ScreenNavigationRequest<FleetPickerScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			FleetPickerScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.KeepInNavigationStack = keepInNavigationStack;
			resultConcrete.FleetItems = fleets.Select((Fleet e) => new FleetPickerItem
			{
				Fleet = e,
				FleetPickerItemType = FleetPickerItemType.Fleet
			}).ToList();
			if (allowCreateNew)
			{
				resultConcrete.FleetItems.Insert(0, new FleetPickerItem
				{
					FleetPickerItemType = FleetPickerItemType.CreateNew
				});
			}
			if (allowNone)
			{
				resultConcrete.FleetItems.Insert(0, new FleetPickerItem
				{
					FleetPickerItemType = FleetPickerItemType.None
				});
			}
			resultConcrete.Refresh();
			resultConcrete.TitleText.text = title;
			resultConcrete.FleetPickedResult += callback;
		}

		public static void ShowSectorPositionContextMenuScreen(this ScreenNavigator uiController, SectorTarget sectorTarget)
		{
			ScreenNavigationRequest<SectorPositionContextScreen> screenNavigationRequest = new ScreenNavigationRequest<SectorPositionContextScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			SectorPositionContextScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.SectorTarget = sectorTarget;
			resultConcrete.Refresh();
		}

		public static void ShowUnitContextMenuScreen(this ScreenNavigator uiController, Unit unit)
		{
			ScreenNavigationRequest<UnitContextMenuScreen> screenNavigationRequest = new ScreenNavigationRequest<UnitContextMenuScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			UnitContextMenuScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.Unit = unit;
			resultConcrete.Refresh();
		}

		public static void ShowBuySectorIntelScreen(this ScreenNavigator uiController, Unit currentLocation, GamePlayer player)
		{
			ScreenNavigationRequest<BuySectorIntelScreen> screenNavigationRequest = new ScreenNavigationRequest<BuySectorIntelScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			BuySectorIntelScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.CurrentLocation = currentLocation;
			resultConcrete.BuyingPlayer = player;
			resultConcrete.Refresh();
		}

		public static void ShowCargoPickerScreen(this ScreenNavigator uiController, List<CargoPickerItem> items, CargoPickerScreen.CargoPickerFinishedHandler callback, string title = "Select Cargo...", bool keepInNavigationStack = false, Action<CargoPickerScreen> setupAction = null)
		{
			ScreenNavigationRequest<CargoPickerScreen> screenNavigationRequest = new ScreenNavigationRequest<CargoPickerScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			CargoPickerScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.KeepInNavigationStack = keepInNavigationStack;
			resultConcrete.Items = items;
			resultConcrete.TitleLabel.text = title;
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
			if (callback != null)
			{
				resultConcrete.Finished += callback;
			}
		}

		public static void ShowMultiCargoPickerScreen(this ScreenNavigator uiController, Action<MultiCargoPickerScreen> setupAction = null)
		{
			ScreenNavigationRequest<MultiCargoPickerScreen> screenNavigationRequest = new ScreenNavigationRequest<MultiCargoPickerScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			MultiCargoPickerScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowShipTradeScreen(this ScreenNavigator uiController, Unit dockUnit, ShipBuyScreen.OnShipBoughtHandler onShipBoughtHandler)
		{
			ScreenNavigationRequest<ShipTradeScreen> screenNavigationRequest = new ScreenNavigationRequest<ShipTradeScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			ShipTradeScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.ShipTraderUnit = dockUnit;
			resultConcrete.Refresh();
		}

		public static void ShowBuyShipScreen(this ScreenNavigator uiController, Unit shipTrader, ShipTraderItemWrapper unitShipTraderItem, ShipBuyScreen.OnShipBoughtHandler onShipBoughtHandler)
		{
			ScreenNavigationRequest<ShipBuyScreen> screenNavigationRequest = new ScreenNavigationRequest<ShipBuyScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			ShipBuyScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.ShipTraderUnit = shipTrader;
			resultConcrete.CurrentItem = unitShipTraderItem;
			if (onShipBoughtHandler != null)
			{
				resultConcrete.OnShipBought += onShipBoughtHandler;
			}
			resultConcrete.Refresh();
		}

		public static void ShowDockedShipsScreen(this ScreenNavigator uiController, UnitHangar hangar)
		{
			ScreenNavigationRequest<DockedShipsScreen> screenNavigationRequest = new ScreenNavigationRequest<DockedShipsScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			DockedShipsScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.Hangar = hangar;
			resultConcrete.NavigateBackWhenUnitInvaild = true;
			resultConcrete.Refresh();
		}

		public static void ShowUnitCargoScreen(this ScreenNavigator uiController, Unit unit)
		{
			ScreenNavigationRequest<UnitCargoScreen> screenNavigationRequest = new ScreenNavigationRequest<UnitCargoScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			UnitCargoScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.SourceUnit = unit;
			resultConcrete.NavigateBackWhenUnitInvaild = true;
			resultConcrete.Refresh();
		}

		public static void ShowFleetCargoScreen(this ScreenNavigator uiController, Fleet fleet)
		{
			ScreenNavigationRequest<FleetCargoScreen> screenNavigationRequest = new ScreenNavigationRequest<FleetCargoScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			FleetCargoScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.SourceFleet = fleet;
			resultConcrete.NavigateBackWhenSourceInvaild = true;
			resultConcrete.Refresh();
		}

		public static void ShowStoreScreen(this ScreenNavigator uiController, IPProduct requestedProduct = null)
		{
			ScreenNavigationRequest<StoreScreen> screenNavigationRequest = new ScreenNavigationRequest<StoreScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			StoreScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.RequestedProduct = requestedProduct;
			resultConcrete.Refresh();
		}

		public static void ShowBountyScreen(this ScreenNavigator uiController, FactionBountyBoard bountyBoard)
		{
			ScreenNavigationRequest<BountyScreen> screenNavigationRequest = new ScreenNavigationRequest<BountyScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			BountyScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.BountyBoard = bountyBoard;
			resultConcrete.Refresh();
		}

		public static void ShowBattlesScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<BattlesScreen> screenNavigationRequest = new ScreenNavigationRequest<BattlesScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			screenNavigationRequest.ResultConcrete.Refresh();
		}

		public static void ShowCargoTransferScreen(this ScreenNavigator uiController, Unit unit1, Unit unit2)
		{
			ScreenNavigationRequest<CargoTransferScreen> screenNavigationRequest = new ScreenNavigationRequest<CargoTransferScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			CargoTransferScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.LeftTransferController.Unit = unit1;
			resultConcrete.RightTransferController.Unit = unit2;
			resultConcrete.NavigateBackWhenUnitInvaild = true;
			resultConcrete.Refresh();
		}

		public static void ShowEquipmentTradeScreen(this ScreenNavigator uiController, Unit upgradingUnit, Unit dockUnit, UnitComponentTrader componentTrader, Action<ComponentTradeScreen> setupAction = null)
		{
			ScreenNavigationRequest<ComponentTradeScreen> screenNavigationRequest = new ScreenNavigationRequest<ComponentTradeScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			ComponentTradeScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.ComponentTrader = componentTrader;
			resultConcrete.DockUnit = dockUnit;
			resultConcrete.UpgradingUnit = upgradingUnit;
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowRepairScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<RepairUI> screenNavigationRequest = new ScreenNavigationRequest<RepairUI>();
			uiController.NavigateToScreen(screenNavigationRequest);
			screenNavigationRequest.ResultConcrete.Refresh();
		}

		public static void ShowFactionsScreen(this ScreenNavigator uiController, Faction displayedFaction = null)
		{
			ScreenNavigationRequest<FactionsScreen> screenNavigationRequest = new ScreenNavigationRequest<FactionsScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			FactionsScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.ShowMinorFactions = displayedFaction != null && displayedFaction.IsMinor;
			resultConcrete.ShowBandits = displayedFaction != null && displayedFaction.FactionType == FactionType.Bandit;
			resultConcrete.ShowFreelancers = displayedFaction != null && displayedFaction.IsFreelancer;
			resultConcrete.Refresh();
			if (displayedFaction != null)
			{
				resultConcrete.ItemList.FirstSelectedItem = displayedFaction;
				resultConcrete.ItemList.ScrollToSelected();
			}
			resultConcrete.Init();
		}

		public static void ShowMissionsScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<MissionsScreen> request = new ScreenNavigationRequest<MissionsScreen>();
			uiController.NavigateToScreen(request);
		}

		public static void ShowUnitMainScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<DockedScreen> screenNavigationRequest = new ScreenNavigationRequest<DockedScreen>();
			uiController.NavigateToRootScene(screenNavigationRequest);
			screenNavigationRequest.ResultConcrete.Refresh();
		}

		public static void ShowTransactionsScreen(this ScreenNavigator uiController, Faction faction)
		{
			ScreenNavigationRequest<FactionTransactionsScreen> screenNavigationRequest = new ScreenNavigationRequest<FactionTransactionsScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			FactionTransactionsScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.Faction = faction;
			resultConcrete.Refresh();
		}

		public static void ShowJobBoardScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<JobBoardScreen> screenNavigationRequest = new ScreenNavigationRequest<JobBoardScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			screenNavigationRequest.ResultConcrete.Refresh();
		}

		public static void ToggleMessagesScreen(this ScreenNavigator uiController)
		{
			if (uiController.CurrentScreen is MessagesUI messagesUI)
			{
				messagesUI.NavigateBack();
			}
			else
			{
				uiController.ShowMessagesScreen();
			}
		}

		public static void ToggleGodModeScreen(this ScreenNavigator screenNavigator)
		{
			// The permission is checked here as well as where the log screen's
			// button is hidden, because the G hotkey reaches god mode without
			// going near that button. Hiding the control was only the first line,
			// and without this a shared session could still be god-moded by a
			// single keypress.
			if (!CanUseGodMode())
			{
				UIController.Instance.ShowMessageBox(
					"God mode is not available in a multiplayer session.",
					MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				return;
			}

			if (screenNavigator.CurrentScreen is GodModeScreen godModeScreen)
			{
				godModeScreen.NavigateBack();
			}
			else
			{
				screenNavigator.ShowGodModeScreen();
			}
		}

		/// <summary>
		/// Whether this world permits god mode.
		/// </summary>
		private static bool CanUseGodMode()
		{
			EngineASX engine = EngineASX.Instance;

			return engine == null
				|| engine.World == null
				|| engine.World.Permissions.AllowGodMode;
		}

		public static void ShowMessagesScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<MessagesUI> request = new ScreenNavigationRequest<MessagesUI>();
			uiController.NavigateToScreen(request);
		}

		public static void ShowCargoTradeScreen(this ScreenNavigator uiController, Unit currentUnit, Unit dockUnit)
		{
			ScreenNavigationRequest<CargoTradeScreen> screenNavigationRequest = new ScreenNavigationRequest<CargoTradeScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			CargoTradeScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.DockedUnit = currentUnit;
			resultConcrete.DockUnit = dockUnit;
			resultConcrete.InvalidateWhenUnitsNull = true;
			resultConcrete.Refresh();
		}

		public static void ShowSellShipScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<SellShipScreen> screenNavigationRequest = new ScreenNavigationRequest<SellShipScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			screenNavigationRequest.ResultConcrete.Refresh();
		}

		public static void ShowUnitComponentsScreen(this ScreenNavigator uiController, Unit unit)
		{
			ScreenNavigationRequest<UnitComponentsScreen> screenNavigationRequest = new ScreenNavigationRequest<UnitComponentsScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			UnitComponentsScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.Unit = unit;
			resultConcrete.Refresh();
		}

		public static void ShowGodModeScreen(this ScreenNavigator uiController)
		{
			GodModeUtils.OnGodModeActionExecuted();
			ScreenNavigationRequest<GodModeScreen> request = new ScreenNavigationRequest<GodModeScreen>();
			uiController.NavigateToScreen(request);
		}

		public static void ShowShipScanScreen(this ScreenNavigator uiController, Unit unit)
		{
			ScreenNavigationRequest<ShipScanScreen> screenNavigationRequest = new ScreenNavigationRequest<ShipScanScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			ShipScanScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.Unit = unit;
			resultConcrete.Refresh();
		}

		public static void ShowUnitInfoScreen(this ScreenNavigator uiController, Unit unit)
		{
			if (unit.UnitType == UnitType.Cargo)
			{
				if (unit.CargoComponent != null && unit.CargoComponent.CargoClass != null)
				{
					uiController.ShowCargoInfoScreen(unit.CargoComponent.CargoClass);
				}
			}
			else
			{
				ShowShipInfoScreen(uiController, unit);
			}
		}

		public static void ShowShipInfoScreen(ScreenNavigator uiController, Unit unit)
		{
			ScreenNavigationRequest<UnitInfoScreen> screenNavigationRequest = new ScreenNavigationRequest<UnitInfoScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			UnitInfoScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.Unit = unit;
			resultConcrete.Refresh();
		}

		public static void ShowScratchcardScreen(this ScreenNavigator uiController, Faction faction)
		{
			ScreenNavigationRequest<ScratchcardScreen> screenNavigationRequest = new ScreenNavigationRequest<ScratchcardScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			ScratchcardScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.Faction = faction;
			resultConcrete.Refresh();
		}

		public static void ShowBuildModePlacementScreen(this ScreenNavigator uiController, UnitClass unitClass, Action<BuildModePlacementScreen> setupAction)
		{
			EngineASX.Instance.TrySetCameraSpectatorEnabled(enabled: false);
			ScreenNavigationRequest<BuildModePlacementScreen> screenNavigationRequest = new ScreenNavigationRequest<BuildModePlacementScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			BuildModePlacementScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			ThrowIfScreenNull(screenNavigationRequest, resultConcrete);
			resultConcrete.UnitClass = unitClass;
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowChangeSectorAppearanceScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<ChangeSectorAppearanceScreen> screenNavigationRequest = new ScreenNavigationRequest<ChangeSectorAppearanceScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			ChangeSectorAppearanceScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			ThrowIfScreenNull(screenNavigationRequest, resultConcrete);
			resultConcrete.Refresh();
		}

		private static void ThrowIfScreenNull<T>(ScreenNavigationRequest<T> loadRequest, ScreenBase screen) where T : ScreenBase
		{
			if (screen == null)
			{
				throw new Exception($"Screen could not be loaded for request for screen type of {typeof(T)}");
			}
		}

		public static void ShowHypersleepScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<HypersleepScreen> screenNavigationRequest = new ScreenNavigationRequest<HypersleepScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			screenNavigationRequest.ResultConcrete.Refresh();
		}

		public static void ShowBuildModeScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<BuildModeScreen> screenNavigationRequest = new ScreenNavigationRequest<BuildModeScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			BuildModeScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.BuildModeItemSelected += (BuildModeScreen sender, UnitClass unitClass) =>
			{
				BuildModeHelper.AttemptToBuildStation(unitClass);
			};
			resultConcrete.Refresh();
		}

		public static void ShowCustomBuildModeScreen(this ScreenNavigator uiController, BuildModeScreen.BuildModeItemSelectedHandler itemSelectedHandler)
		{
			ScreenNavigationRequest<BuildModeScreen> screenNavigationRequest = new ScreenNavigationRequest<BuildModeScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			BuildModeScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.BuildModeItemSelected += itemSelectedHandler;
			resultConcrete.Refresh();
		}

		public static void TogglePropertyScreen(this ScreenNavigator screenNavigator)
		{
			if (screenNavigator.CurrentScreen is PropertyScreen propertyScreen)
			{
				propertyScreen.NavigateBack();
			}
			else
			{
				screenNavigator.ShowPropertyScreen();
			}
		}

		public static void ShowPropertyScreen(this ScreenNavigator uiController, Action<PropertyScreen> setupAction = null)
		{
			ScreenNavigationRequest<PropertyScreen> screenNavigationRequest = new ScreenNavigationRequest<PropertyScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			PropertyScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.ResetFiltersWhenShown();
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowTopPilotsScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<TopPilotsScreen> screenNavigationRequest = new ScreenNavigationRequest<TopPilotsScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			screenNavigationRequest.ResultConcrete.Refresh();
		}

		public static void ShowSectorMapScreen(this ScreenNavigator uiController, Sector sector, Action<SectorMapScreen> setupAction)
		{
			ScreenNavigationRequest<SectorMapScreen> screenNavigationRequest = new ScreenNavigationRequest<SectorMapScreen>();
			screenNavigationRequest.ReuseLoadedExistingScreen = ReuseScreenMode.IfNotInNavigationStack;
			uiController.NavigateToScreen(screenNavigationRequest);
			SectorMapScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.SectorMapForm.Sector = sector;
			resultConcrete.RestrictNavigationAwayEnabled = false;
			resultConcrete.SectorMapForm.SectorMap.SectorMapCurrentTargetController.SyncSelectionWithHud = false;
			resultConcrete.SectorMapForm.SectorMap.SectorMapCurrentTargetController.ClearSelection();
			resultConcrete.SectorMapForm.SectorMap.SectorMapCurrentTargetController.EnabledSelectedUnitContextMenu = true;
			resultConcrete.ShowDockHeader = true;
			resultConcrete.SectorMapForm.AllowSelectionConfirm = false;
			resultConcrete.SectorMapForm.SelectedCallback = null;
			resultConcrete.SectorMapForm.SetTitleToSectorName();
			resultConcrete.SectorMapForm.SectorMap.CustomUnitDisplayFilter = null;
			resultConcrete.SectorMapForm.SectorMap.CustomSelectionFilter = null;
			resultConcrete.SectorMapForm.SectorMap.UpdateMapSize();
			resultConcrete.SectorMap.CenterOnSectorPosition(Vector3.zero);
			resultConcrete.SectorMapForm.Redraw();
			resultConcrete.SectorMapForm.SetNextMapAutoRebuildTime();
			resultConcrete.SectorMapForm.StaticTargetInfoTransform.gameObject.SetActive(value: false);
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowSectorMapScreenShowingUnit(this ScreenNavigator uiController, Unit unit)
		{
			uiController.ShowSectorMapScreen(unit.Sector, (SectorMapScreen sectorMapScreen) =>
			{
				sectorMapScreen.SectorMapForm.SelectAndCenterOnUnit(unit);
			});
		}

		public static void ShowSectorMapScreenForUnitRespectingIntel(this ScreenNavigator screenNavigator, Unit unit, Sector fallBackSector, Vector3 fallBackSectorPosition)
		{
			if (unit != null && unit.Sector != null)
			{
				float maxDiscoveryAge = (unit.IsInActiveSector ? GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeActiveSector : GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime);
				FactionIntel.DiscoveredUnitData? discoveryData = EngineASX.Instance.LocalFaction.Intel.GetDiscoveryData(unit.UniqueId, float.MaxValue);
				if (discoveryData.HasValue)
				{
					screenNavigator.ShowSectorMapScreen(discoveryData.Value.Sector, (SectorMapScreen sectorMapScreen) =>
					{
						if (EngineASX.Instance.LocalFaction.Intel.IsUnitDiscovered(unit, maxDiscoveryAge))
						{
							sectorMapScreen.SectorMapForm.SelectAndCenterOnUnit(unit);
						}
						else
						{
							sectorMapScreen.SectorMap.SelectAndCenterOnSectorPosition(discoveryData.Value.SectorPosition);
						}
					});
					return;
				}
			}
			else
			{
				Debug.LogError("Expected unit with valid sector");
			}
			if (fallBackSector != null)
			{
				screenNavigator.ShowSectorMapScreen(fallBackSector, (SectorMapScreen sectorMapScreen) =>
				{
					sectorMapScreen.SectorMap.SelectAndCenterOnSectorPosition(fallBackSectorPosition);
				});
			}
		}

		public static void ShowSectorMapScreenShowingSectorPosition(this ScreenNavigator screenNavigator, Sector sector, Vector3 sectorPosition)
		{
			screenNavigator.ShowSectorMapScreen(sector, (SectorMapScreen sectorMapScreen) =>
			{
				sectorMapScreen.SectorMap.SelectAndCenterOnSectorPosition(sectorPosition);
			});
		}

		public static void ShowSectorMapScreenShowingWorldPosition(this ScreenNavigator screenNavigator, Sector sector, Vector3 worldPosition)
		{
			screenNavigator.ShowSectorMapScreen(sector, (SectorMapScreen sectorMapScreen) =>
			{
				sectorMapScreen.SectorMap.SelectAndCenterOnSectorPosition(sector.ToLocalPosition(worldPosition));
			});
		}

		public static void ToggleSectorMapScreenWhenPilotting(this ScreenNavigator screenNavigator)
		{
			if (screenNavigator.CurrentScreen is SectorMapScreen sectorMapScreen)
			{
				sectorMapScreen.NavigateBack();
			}
			else
			{
				screenNavigator.ShowSectorMapScreenWhenPilotting();
			}
		}

		public static void ShowSectorMapScreenWhenPilotting(this ScreenNavigator uiController)
		{
			uiController.ShowSectorMapScreen(EngineASX.Instance.ActiveSector, (SectorMapScreen sectorMapScreen) =>
			{
				if (EngineASX.Instance.Hud.CurrentTarget != null && EngineASX.Instance.Hud.CurrentTarget.Sector == EngineASX.Instance.LocalUnit.Sector)
				{
					sectorMapScreen.SectorMap.SelectedUnit = EngineASX.Instance.Hud.CurrentTarget;
				}
				else
				{
					sectorMapScreen.SectorMap.SelectedUnit = EngineASX.Instance.LocalUnit;
				}
				sectorMapScreen.SectorMap.SectorMapCurrentTargetController.SyncSelectionWithHud = true;
				sectorMapScreen.SectorMapForm.AllowSelectionConfirm = false;
				sectorMapScreen.SectorMap.CenterOnUnit(EngineASX.Instance.LocalUnit);
			});
		}

		public static void ToggleLogScreen(this ScreenNavigator screenNavigator)
		{
			if (screenNavigator.CurrentScreen is LogScreen logScreen)
			{
				logScreen.NavigateBack();
			}
			else
			{
				screenNavigator.ShowLogScreen();
			}
		}

		public static void ShowLogScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<LogScreen> screenNavigationRequest = new ScreenNavigationRequest<LogScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			screenNavigationRequest.ReuseLoadedExistingScreen = ReuseScreenMode.Any;
			screenNavigationRequest.ResultConcrete.Refresh();
		}

		public static void ShowCargoTradeItemScreen(this ScreenNavigator uiController, Unit dockedUnit, Unit dockUnit, CargoClass cargoClass)
		{
			ScreenNavigationRequest<CargoTradeItemScreen> screenNavigationRequest = new ScreenNavigationRequest<CargoTradeItemScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			CargoTradeItemScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.DockedUnit = dockedUnit;
			resultConcrete.DockUnit = dockUnit;
			resultConcrete.CargoClass = cargoClass;
			resultConcrete.Refresh();
		}

		public static void ToggleCustomUniverseMapScreen(this ScreenNavigator screenNavigator, Action<UniverseMapScreen> setupAction, Action<UniverseMapScreen> postRefreshAction = null)
		{
			if (screenNavigator.CurrentScreen is UniverseMapScreen universeMapScreen)
			{
				universeMapScreen.NavigateBack();
			}
			else
			{
				screenNavigator.ShowCustomUniverseMapScreen(setupAction, postRefreshAction);
			}
		}

		public static void ShowCustomUniverseMapScreen(this ScreenNavigator uiController, Action<UniverseMapScreen> setupAction, Action<UniverseMapScreen> postRefreshAction = null)
		{
			ScreenNavigationRequest<UniverseMapScreen> screenNavigationRequest = new ScreenNavigationRequest<UniverseMapScreen>();
			screenNavigationRequest.ReuseLoadedExistingScreen = ReuseScreenMode.IfNotInNavigationStack;
			uiController.NavigateToScreen(screenNavigationRequest);
			UniverseMapScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.ClearSelectedItem();
			resultConcrete.Zoomer.ResetZoom();
			resultConcrete.RestrictNavigationAwayEnabled = false;
			resultConcrete.IgnoreIntel = false;
			resultConcrete.SelectingSectorItem = null;
			resultConcrete.CustomDisplayedSectors = null;
			resultConcrete.Title = "Universe Map";
			resultConcrete.AllowSectorSelection = true;
			resultConcrete.SectorSelectedCallback = null;
			resultConcrete.ShowSelectedSectorInfo = true;
			resultConcrete.ShowDockHeader = true;
			resultConcrete.EnabledSectors = null;
			resultConcrete.AllowSectorSelectionPick = false;
			resultConcrete.RebuildIfNeededAndApplyPlayerIntel();
			foreach (UniverseMapItemUI mapItem in resultConcrete.MapItems)
			{
				mapItem.PatrolPathCreatorImage.enabled = false;
			}
			setupAction?.Invoke(resultConcrete);
			resultConcrete.AllowSectorSelectionPick = resultConcrete.SectorSelectedCallback != null;
			resultConcrete.Refresh();
			postRefreshAction?.Invoke(resultConcrete);
		}

		public static void ShowPatrolOrderCreatorScreen(this ScreenNavigator uiController, Action<PatrolOrderCreatorScreen> setupAction)
		{
			ScreenNavigationRequest<PatrolOrderCreatorScreen> screenNavigationRequest = new ScreenNavigationRequest<PatrolOrderCreatorScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			PatrolOrderCreatorScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowNewFleetOrderScreen(this ScreenNavigator screenNavigator, NewOrderTarget newOrderTarget, Action orderIssuedCallback, bool stack = true)
		{
			ScreenNavigationRequest<NewFleetOrderScreen> screenNavigationRequest = new ScreenNavigationRequest<NewFleetOrderScreen>();
			screenNavigator.NavigateToScreen(screenNavigationRequest);
			NewFleetOrderScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.OrderTarget = newOrderTarget;
			resultConcrete.OrderIssuedCallback = orderIssuedCallback;
			resultConcrete.StackOrder = stack;
			resultConcrete.Refresh();
		}

		public static void ShowNewFleetOrderScreen(this ScreenNavigator screenNavigator, Unit orderedUnit, Action orderIssuedCallback, bool stack = true)
		{
			NewOrderTarget newOrderTarget = NewOrderTarget.CreateFromUnits(new Unit[1] { orderedUnit });
			screenNavigator.ShowNewFleetOrderScreen(newOrderTarget, orderIssuedCallback, stack);
		}

		public static void ShowNewFleetOrderScreen(this ScreenNavigator screenNavigator, Fleet orderedFleet, Action orderIssuedCallback, bool stack = true)
		{
			NewOrderTarget newOrderTarget = NewOrderTarget.CreateFromFleets(new Fleet[1] { orderedFleet });
			screenNavigator.ShowNewFleetOrderScreen(newOrderTarget, orderIssuedCallback, stack);
		}

		public static void ShowOrdersScreen(this ScreenNavigator uiController, Unit unit)
		{
			Fleet fleet = unit.GetFleet();
			if (fleet == null)
			{
				fleet = OrdersHelper.FindOrCreateNpcAndFleetAndPilotShip(unit);
			}
			uiController.ShowOrdersScreen(fleet);
		}

		public static void ShowOrdersScreen(this ScreenNavigator uiController, Fleet fleet)
		{
			ScreenNavigationRequest<OrdersScreen> screenNavigationRequest = new ScreenNavigationRequest<OrdersScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			OrdersScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.OrderedFleet = fleet;
			resultConcrete.NavigateBackWhenFleetInvalid = true;
			resultConcrete.Refresh();
		}

		public static void ShowFleetSettingsScreen(this ScreenNavigator uiController, Fleet fleet)
		{
			uiController.ShowFleetSettingsScreen(new Fleet[1] { fleet });
		}

		public static void ShowFleetSettingsScreen(this ScreenNavigator uiController, IEnumerable<Fleet> fleets)
		{
			ScreenNavigationRequest<FleetSettingsScreen> screenNavigationRequest = new ScreenNavigationRequest<FleetSettingsScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			FleetSettingsScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.Fleets = fleets;
			resultConcrete.Refresh();
		}

		public static void ShowFleetOrderSettingsScreen(this ScreenNavigator uiController, Fleet fleet, FleetOrder fleetOrder)
		{
			ScreenNavigationRequest<FleetOrderSettingsScreen> screenNavigationRequest = new ScreenNavigationRequest<FleetOrderSettingsScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			FleetOrderSettingsScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.FleetOrder = fleetOrder;
			resultConcrete.Fleet = fleet;
			resultConcrete.Refresh();
		}

		public static void ShowRenameSectorScreen(this ScreenNavigator uiController, Sector sector, RenameUnitScreen.RenameConfirmedHandler callback)
		{
			ShowRenameScreen(uiController, sector.Name, sector.Name, 3, 30, callback);
		}

		public static void ShowRenameScreen(this ScreenNavigator uiController, string currentName, int minCharacters, int maxCharacters, RenameUnitScreen.RenameConfirmedHandler callback, string validRegexPattern = null, string validRegexPatternMessage = null)
		{
			ShowRenameScreen(uiController, currentName, currentName, minCharacters, maxCharacters, callback, validRegexPattern, validRegexPatternMessage);
		}

		private static void ShowRenameScreen(ScreenNavigator uiController, string currentName, string originalName, int minCharacters, int maxCharacters, RenameUnitScreen.RenameConfirmedHandler callback, string validRegexPattern = null, string validRegexPatternMessage = null)
		{
			ScreenNavigationRequest<RenameUnitScreen> screenNavigationRequest = new ScreenNavigationRequest<RenameUnitScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			RenameUnitScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.CurrentName = currentName;
			resultConcrete.NewName = originalName;
			resultConcrete.MinCharacters = minCharacters;
			resultConcrete.MaxCharacters = maxCharacters;
			resultConcrete.RenameConfirmed += callback;
			resultConcrete.RegexPatternValidation = validRegexPattern;
			resultConcrete.RegexPatternValidationMessage = validRegexPatternMessage;
			resultConcrete.Refresh();
		}

		public static void ShowRenameScreen(this ScreenNavigator uiController, Action<RenameUnitScreen> setupAction)
		{
			ScreenNavigationRequest<RenameUnitScreen> screenNavigationRequest = new ScreenNavigationRequest<RenameUnitScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			RenameUnitScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.CurrentName = null;
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowRenameUnitScreen(this ScreenNavigator uiController, Unit unit, RenameUnitScreen.RenameConfirmedHandler callback)
		{
			string newName = ((unit.UnitType == UnitType.Ship) ? unit.Components.ShipName : unit.GetFriendlyName());
			string friendlyName = unit.GetFriendlyName();
			ScreenNavigationRequest<RenameUnitScreen> screenNavigationRequest = new ScreenNavigationRequest<RenameUnitScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			RenameUnitScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.CurrentName = friendlyName;
			resultConcrete.NewName = newName;
			resultConcrete.RenameConfirmed += callback;
			resultConcrete.MinCharacters = 1;
			if (callback == null)
			{
				resultConcrete.RenameConfirmed += (RenameUnitScreen handler, bool rename, string newName2) =>
				{
					if (rename)
					{
						EngineASX.Instance.RenameUnit(unit, newName2);
					}
				};
			}
			resultConcrete.Refresh();
		}

		public static void ShowEnterNumberScreen(this ScreenNavigator uiController, int? initialValue, EnterNumberScreen.EnterNumberConfirmedHandler callback, string prompt = "Enter value", Action<EnterNumberScreen> setupAction = null)
		{
			ScreenNavigationRequest<EnterNumberScreen> screenNavigationRequest = new ScreenNavigationRequest<EnterNumberScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			EnterNumberScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			if (initialValue.HasValue)
			{
				resultConcrete.InputField.text = initialValue.Value.ToString();
			}
			resultConcrete.PromptText.text = prompt;
			resultConcrete.EnterNumberConfirmed += callback;
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowEnterSliderScreen(this ScreenNavigator uiController, Action<EnterSliderIngameScreen> setupAction = null)
		{
			ScreenNavigationRequest<EnterSliderIngameScreen> screenNavigationRequest = new ScreenNavigationRequest<EnterSliderIngameScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			EnterSliderIngameScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowComponentInfoScreen(this ScreenNavigator uiController, ComponentClass component)
		{
			ScreenNavigationRequest<ComponentInfoUI> screenNavigationRequest = new ScreenNavigationRequest<ComponentInfoUI>();
			uiController.NavigateToScreen(screenNavigationRequest);
			ComponentInfoUI resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.CurrentComponentClass = component;
			resultConcrete.Refresh();
		}

		public static void ShowCargoInfoScreen(this ScreenNavigator uiController, CargoClass cargoClass)
		{
			ScreenNavigationRequest<CargoInfoScreen> screenNavigationRequest = new ScreenNavigationRequest<CargoInfoScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			CargoInfoScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.CargoClass = cargoClass;
			resultConcrete.Refresh();
		}

		public static void ShowCargoPricesScreen(this ScreenNavigator uiController, CargoClass cargoClass)
		{
			ScreenNavigationRequest<CargoPricesScreen> screenNavigationRequest = new ScreenNavigationRequest<CargoPricesScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			CargoPricesScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.CargoClass = cargoClass;
			resultConcrete.Refresh();
		}

		public static void ToggleFleetsScreen(this ScreenNavigator screenNavigator)
		{
			if (screenNavigator.CurrentScreen is FleetsScreen fleetsScreen)
			{
				fleetsScreen.NavigateBack();
			}
			else if (!FleetsHelper.PlayerHasAnyVisibleFleets())
			{
				UIController.Instance.ShowMessageBox("There are no fleets to display", MessageBoxButtons.Ok);
			}
			else
			{
				screenNavigator.ShowFleetsScreen();
			}
		}

		public static void ShowFleetsScreen(this ScreenNavigator uiController, Action<FleetsScreen> setupAction = null)
		{
			ScreenNavigationRequest<FleetsScreen> screenNavigationRequest = new ScreenNavigationRequest<FleetsScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			FleetsScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.ResetFiltersWhenShown();
			setupAction?.Invoke(resultConcrete);
			resultConcrete.Refresh();
		}

		public static void ShowPassengerInfoScreen(this ScreenNavigator uiController, Unit unit, Unit dockUnit)
		{
			ScreenNavigationRequest<PassengersScreen> screenNavigationRequest = new ScreenNavigationRequest<PassengersScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			PassengersScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.CurrentUnit = unit;
			resultConcrete.DockUnit = dockUnit;
			resultConcrete.Refresh();
		}

		public static void ShowEndGameScreen(this ScreenNavigator uiController)
		{
			ScreenNavigationRequest<EndGameUI> screenNavigationRequest = new ScreenNavigationRequest<EndGameUI>();
			uiController.NavigateToRootScene(screenNavigationRequest);
			screenNavigationRequest.ResultConcrete.Refresh();
		}

		public static void ShowScenariosScreen(this ScreenNavigator uiController)
		{
			uiController.ShowScenariosScreen(GameController.EngineLaunchSource.ScenariosUI, showTutorials: false, showMissions: true);
		}

		public static void ShowTutorialsScreen(this ScreenNavigator uiController)
		{
			uiController.ShowScenariosScreen(GameController.EngineLaunchSource.TutorialsUI, showTutorials: true, showMissions: false, "Tutorials");
		}

		public static void ShowScenariosScreen(this ScreenNavigator uiController, GameController.EngineLaunchSource launchSource, bool showTutorials, bool showMissions, string title = "Scenarios")
		{
			ScreenNavigationRequest<ScenariosUI> screenNavigationRequest = new ScreenNavigationRequest<ScenariosUI>();
			uiController.NavigateToScreen(screenNavigationRequest);
			ScenariosUI resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.ScenarioItemList.ShowTutorials = showTutorials;
			resultConcrete.ScenarioItemList.ShowMissions = showMissions;
			resultConcrete.LaunchOrigin = launchSource;
			if (resultConcrete.TitleText != null)
			{
				resultConcrete.TitleText.text = title;
			}
			resultConcrete.Refresh();
		}

		public static void ShowUniverseGameTypeScreen(this ScreenNavigator uiController, ScenarioInfo scenarioInfo, WorldSeedSettings seedSettings = null)
		{
			ScreenNavigationRequest<UniverseGameTypeScreen> screenNavigationRequest = new ScreenNavigationRequest<UniverseGameTypeScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			UniverseGameTypeScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.UniverseScenarioData.ScenarioInfo = scenarioInfo;
			resultConcrete.UniverseScenarioData.SeedSettings = seedSettings;
			resultConcrete.Refresh();
		}

		public static void ShowUniverseBlueprintScreen(this ScreenNavigator uiController, WorldBlueprint worldBlueprint, WorldGeneratorSettings worldGeneratorSettings, CreateBlueprintSectorsSeederSettings createBlueprintSectorsSeederSettings, Func<WorldBlueprint> regenerateCallback)
		{
			ScreenNavigationRequest<UniverseBlueprintMapScreen> screenNavigationRequest = new ScreenNavigationRequest<UniverseBlueprintMapScreen>();
			uiController.NavigateToScreen(screenNavigationRequest);
			UniverseBlueprintMapScreen resultConcrete = screenNavigationRequest.ResultConcrete;
			resultConcrete.WorldBlueprint = worldBlueprint;
			resultConcrete.WorldGeneratorSettings = worldGeneratorSettings;
			resultConcrete.CreateBlueprintSectorsSeederSettings = createBlueprintSectorsSeederSettings;
			resultConcrete.RegenerateCallback = regenerateCallback;
			resultConcrete.Refresh();
		}
	}
}
