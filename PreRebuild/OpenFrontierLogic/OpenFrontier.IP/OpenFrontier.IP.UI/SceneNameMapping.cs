using OpenFrontier.IP.Engine;
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
using OpenFrontier.IP.UI.Screens.GameMode;
using OpenFrontier.IP.UI.Screens.Hud;
using OpenFrontier.IP.UI.Screens.Hypersleep;
using OpenFrontier.IP.UI.Screens.LoadGame;
using OpenFrontier.IP.UI.Screens.MultiCargoPicker;
using OpenFrontier.IP.UI.Screens.NewFleetOrder;
using OpenFrontier.IP.UI.Screens.Orders.PatrolOrderCreator;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using OpenFrontier.IP.UI.Screens.RequestTaxi;
using OpenFrontier.IP.UI.Screens.SectorMap;
using OpenFrontier.IP.UI.Screens.SectorPositionContextMenu;
using OpenFrontier.IP.UI.Screens.ShipScan;
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
using OpenFrontier.IP.UI.Screens.UniverseSandboxSettings;
using OpenFrontier.IP.UI.Screens.UniverseScenePicker;
using OpenFrontier.IP.UI.Screens.UniverseSelect;
using OpenFrontier.IP.UI.SellShip;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class SceneNameMapping : MonoBehaviour
	{
		public ScreenNavigator ScreenNavigator;

		public ScreenBase BuySectorIntelConfirmScene;

		public ScreenBase CargoPickerScene;

		public ScreenBase DialogScene;

		public ScreenBase CinematicScene;

		public ScreenBase ComponentInfoScene;

		public ScreenBase CreditsScene;

		public ScreenBase EndGameScene;

		public ScreenBase LoadGameScene;

		public ScreenBase MenuSceneName;

		public ScreenBase MessageScene;

		public ScreenBase OptionsScene;

		public ScreenBase IngameMenuScene;

		public ScreenBase ScenarioSelectSceneName;

		public ScreenBase MissionScene;

		public ScreenBase BountyScene;

		public ScreenBase BuySectorIntelScene;

		public ScreenBase CargoTransferScene;

		public ScreenBase AmmoTypesScene;

		public ScreenBase FactionSettingsScene;

		public ScreenBase TransationsScene;

		public ScreenBase HudScene;

		public ScreenBase MessageBoxScene;

		public ScreenBase OrdersScene;

		public ScreenBase NewFleetOrderScreenSceneName;

		public ScreenBase PropertyScene;

		public ScreenBase StoreScene;

		public ScreenBase RenameUnitScene;

		public ScreenBase SectorMapScene;

		public ScreenBase ShipCargoScene;

		public ScreenBase UnitInfoScene;

		public ScreenBase ShipScanScene;

		public ScreenBase GodModeScreenScene;

		public ScreenBase DockedScreenScene;

		public ScreenBase UnitPickerScene;

		public ScreenBase UniverseGameTypeScene;

		public ScreenBase UniverseSelectScene;

		public ScreenBase UniverseSandboxSettingsScene;

		public ScreenBase SkirmishSetupScene;

		public ScreenBase SkirmishTeamSetupScene;

		public ScreenBase CargoInfoScene;

		public ScreenBase CargoTradeItemScene;

		public ScreenBase CargoPricesScene;

		public ScreenBase FactionsScene;

		public ScreenBase PlayerLogScene;

		public ScreenBase MessagesScene;

		public ScreenBase FleetsScene;

		public ScreenBase JobBoardScene;

		public ScreenBase PassengersScreenSceneName;

		public ScreenBase UnitRepairScene;

		public ScreenBase DockedShipsScene;

		public ScreenBase MissionsScene;

		public ScreenBase RequestTaxiScene;

		public ScreenBase ShipTradeItemScene;

		public ScreenBase CargoTradeScene;

		public ScreenBase UniverseMapScene;

		public ScreenBase UnitComponentsScene;

		public ScreenBase ShipTradeScene;

		public ScreenBase SellShipScene;

		public ScreenBase ComponentTradeScene;

		public ScreenBase GameModeScene;

		public ScreenBase BattlesScene;

		public ScreenBase SectorPickerScreenScene;

		public ScreenBase ScratchcardScene;

		public ScreenBase UniverseBlueprintMapScene;

		public ScreenBase EnterNumberScreenScene;

		public ScreenBase BuildModeScreenSceneName;

		public ScreenBase BuildModePlacementScreenSceneName;

		public ScreenBase FleetPickerScreenSceneName;

		public ScreenBase PatrolOrderCreatorScreenName;

		public ScreenBase UnitContextMenuScreenSceneName;

		public ScreenBase TopPilotsScreenSceneName;

		public ScreenBase AdjustSafeAreaScreenSceneName;

		public ScreenBase FleetFormationStylePickerScreenSceneName;

		public ScreenBase HypersleepScreenSceneName;

		public ScreenBase EnterSliderIngameScreenScene;

		public ScreenBase FleetSettingsScreenSceneName;

		public ScreenBase FleetOrderSettingsScreenSceneName;

		public ScreenBase ChangeSectorAppearanceScreenSceneName;

		public ScreenBase SectorPositionContextScreenSceneName;

		public ScreenBase FleetCargoScreen;

		public ScreenBase UnitClassPickerScreen;

		public ScreenBase CreateUnitVariantScreen;

		public ScreenBase CreateUnitVariantComponentsScreen;

		public ScreenBase CreateUnitVariantCargoScreen;

		public ScreenBase ManageCustomVariantsScreen;

		public ScreenBase MultiCargoPickerScreen;

		public ScreenBase ChangeUnitCargoScreen;

		private void Awake()
		{
			if (ScreenNavigator != null)
			{
				CreateScreenNameMapping();
			}
		}

		public void CreateScreenNameMapping()
		{
			ScreenNavigator.ScreenNameMapping.Add(typeof(BuySectorIntelConfirmScreen), BuySectorIntelConfirmScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(CargoPickerScreen), CargoPickerScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(DialogController), DialogScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(CinematicScreen), CinematicScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(ComponentInfoUI), ComponentInfoScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(CreditsUI), CreditsScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(EndGameUI), EndGameScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(LoadGameScreen), LoadGameScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(MainMenuScreen), MenuSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(MessageUI), MessageScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(OptionsScreen), OptionsScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(PauseMenuUI), IngameMenuScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(ScenariosUI), ScenarioSelectSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(MissionScreen), MissionScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(BountyScreen), BountyScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(BuySectorIntelScreen), BuySectorIntelScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(CargoTransferScreen), CargoTransferScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(CompatibleProjectilesScreen), AmmoTypesScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(FactionSettingsScreen), FactionSettingsScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(FactionTransactionsScreen), TransationsScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(HudScreen), HudScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(MessageBoxScreen), MessageBoxScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(OrdersScreen), OrdersScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(NewFleetOrderScreen), NewFleetOrderScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(PropertyScreen), PropertyScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(StoreScreen), StoreScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(RenameUnitScreen), RenameUnitScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(SectorMapScreen), SectorMapScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(UnitCargoScreen), ShipCargoScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(UnitInfoScreen), UnitInfoScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(ShipScanScreen), ShipScanScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(GodModeScreen), GodModeScreenScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(DockedScreen), DockedScreenScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(UnitPickerScreen), UnitPickerScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(UniverseGameTypeScreen), UniverseGameTypeScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(UniverseSelectScreen), UniverseSelectScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(UniverseSandboxSettingsScreen), UniverseSandboxSettingsScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(SkirmishSetupScreen), SkirmishSetupScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(SkirmishTeamSetupScreen), SkirmishTeamSetupScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(CargoInfoScreen), CargoInfoScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(CargoTradeItemScreen), CargoTradeItemScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(CargoPricesScreen), CargoPricesScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(FactionsScreen), FactionsScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(LogScreen), PlayerLogScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(MessagesUI), MessagesScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(FleetsScreen), FleetsScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(JobBoardScreen), JobBoardScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(PassengersScreen), PassengersScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(RepairUI), UnitRepairScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(DockedShipsScreen), DockedShipsScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(MissionsScreen), MissionsScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(RequestTaxiScreen), RequestTaxiScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(ShipBuyScreen), ShipTradeItemScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(CargoTradeScreen), CargoTradeScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(UniverseMapScreen), UniverseMapScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(UnitComponentsScreen), UnitComponentsScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(ShipTradeScreen), ShipTradeScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(SellShipScreen), SellShipScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(ComponentTradeScreen), ComponentTradeScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(GameModeScreen), GameModeScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(BattlesScreen), BattlesScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(SectorPickerScreen), SectorPickerScreenScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(ScratchcardScreen), ScratchcardScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(UniverseBlueprintMapScreen), UniverseBlueprintMapScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(EnterNumberScreen), EnterNumberScreenScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(BuildModeScreen), BuildModeScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(BuildModePlacementScreen), BuildModePlacementScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(FleetPickerScreen), FleetPickerScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(PatrolOrderCreatorScreen), PatrolOrderCreatorScreenName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(UnitContextMenuScreen), UnitContextMenuScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(TopPilotsScreen), TopPilotsScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(AdjustSafeAreaScreen), AdjustSafeAreaScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(FleetFormationStylePickerScreen), FleetFormationStylePickerScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(HypersleepScreen), HypersleepScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(EnterSliderIngameScreen), EnterSliderIngameScreenScene);
			ScreenNavigator.ScreenNameMapping.Add(typeof(FleetSettingsScreen), FleetSettingsScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(FleetOrderSettingsScreen), FleetOrderSettingsScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(ChangeSectorAppearanceScreen), ChangeSectorAppearanceScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(SectorPositionContextScreen), SectorPositionContextScreenSceneName);
			ScreenNavigator.ScreenNameMapping.Add(typeof(FleetCargoScreen), FleetCargoScreen);
			ScreenNavigator.ScreenNameMapping.Add(typeof(UnitClassPickerScreen), UnitClassPickerScreen);
			ScreenNavigator.ScreenNameMapping.Add(typeof(CreateUnitVariantScreen), CreateUnitVariantScreen);
			ScreenNavigator.ScreenNameMapping.Add(typeof(CreateUnitVariantComponentsScreen), CreateUnitVariantComponentsScreen);
			ScreenNavigator.ScreenNameMapping.Add(typeof(CreateUnitVariantCargoScreen), CreateUnitVariantCargoScreen);
			ScreenNavigator.ScreenNameMapping.Add(typeof(ManageUnitVariantsScreen), ManageCustomVariantsScreen);
			ScreenNavigator.ScreenNameMapping.Add(typeof(MultiCargoPickerScreen), MultiCargoPickerScreen);
			ScreenNavigator.ScreenNameMapping.Add(typeof(ChangeUnitCargoScreen), ChangeUnitCargoScreen);
		}
	}
}
