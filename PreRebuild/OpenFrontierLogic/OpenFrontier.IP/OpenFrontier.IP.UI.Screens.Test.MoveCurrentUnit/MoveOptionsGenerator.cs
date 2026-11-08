using System;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Testing;
using OpenFrontier.IP.Testing.MovePlayer;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.SectorMap;
using OpenFrontier.IP.UI.Screens.UniverseMap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI.Screens.Test.MoveCurrentUnit
{
	public class MoveOptionsGenerator : MonoBehaviour
	{
		public CanvasGroup MoveOptionsCanvasGroup;

		public Toggle TargetLocalUnitToggle;

		public Toggle TargetCurrentTargetToggle;

		public Button ButtonPrefab;

		public Transform TargetTransform;

		public Button MoveToButton;

		private void Awake()
		{
			MoveToButton.onClick.AddListener(MoveToButtonClick);
			MoveOptionsCanvasGroup.interactable = TargetLocalUnitToggle.isOn;
			TargetLocalUnitToggle.onValueChanged.AddListener((bool value) =>
			{
				MoveOptionsCanvasGroup.interactable = TargetLocalUnitToggle.isOn;
			});
		}

		private void Update()
		{
			MoveOptionsCanvasGroup.interactable = TargetLocalUnitToggle.isOn;
			MoveToButton.interactable = GetTargetUnit() != null;
		}

		private void MoveToButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowCustomUniverseMapScreen((UniverseMapScreen universeMap) =>
			{
				universeMap.Title = "Select Sector";
				universeMap.AllowSectorSelection = true;
				universeMap.IgnoreIntel = true;
				universeMap.EnabledSectors = null;
				universeMap.ShowSelectedSectorInfo = false;
				universeMap.SectorSelectedCallback = SectorPicked;
				universeMap.SelectSector(EngineASX.Instance.ActiveSector);
				universeMap.CenterOnSector(EngineASX.Instance.ActiveSector);
				universeMap.RestrictNavigationAway();
			});
		}

		private void SectorPicked(UniverseMapScreen sender, Sector selectedSector)
		{
			if (selectedSector != null)
			{
				ShowSectorMap(selectedSector);
			}
		}

		private void ShowSectorMap(Sector selectedSector)
		{
			UIController.Instance.ScreenNavigator.ShowSectorMapScreen(selectedSector, (SectorMapScreen sectorMap) =>
			{
				sectorMap.SectorMapForm.Title = "Select target to move to";
				sectorMap.SectorMapForm.AllowSelectionConfirm = true;
				sectorMap.SectorMapForm.SectorMap.CustomSelectionFilter = (SectorMapSelectionItem item) => item.Unit == null;
				sectorMap.SectorMapForm.SelectedCallback = SectorMapItemPicked;
				sectorMap.RestrictNavigationAway();
			});
		}

		private void SectorMapItemPicked(SectorMapForm sender, SectorMapSelectionItem selected)
		{
			Unit targetUnit = GetTargetUnit();
			if (!CanMoveTargetUnit(selected.Sector, out var errorMessage))
			{
				UIController.Instance.ShowMessageBox(errorMessage, MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				return;
			}
			Sector sector = targetUnit.Sector;
			if (targetUnit.IsPlayerCurrentUnit)
			{
				try
				{
					MovePlayerTestUtils.MoveLocalUnitToSectorPosition(selected.Sector, selected.SectorPosition);
					MoveUtils.OnUnitMoved(targetUnit, sector);
					return;
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					UIController.Instance.ShowMessageBox("An error occured attempting to move the unit", MessageBoxButtons.Ok, null, MessageBoxIcon.Error, "Move unit");
					return;
				}
			}
			targetUnit.UndockIfDocked();
			targetUnit.Sector = selected.Sector;
			targetUnit.transform.localPosition = selected.SectorPosition;
		}

		public bool CanMoveTargetUnit(Sector sector, out string errorMessage)
		{
			return MoveUtils.CanMoveUnitToSector(GetTargetUnit(), sector, out errorMessage);
		}

		public void Generate()
		{
			foreach (MoveCurrentUnitDestinationType moveType in Enum.GetValues(typeof(MoveCurrentUnitDestinationType)))
			{
				Button button = UnityEngine.Object.Instantiate(ButtonPrefab, TargetTransform);
				button.gameObject.name = $"Button_{moveType}";
				button.onClick.AddListener(() =>
				{
					OnButtonClick(moveType);
				});
				button.GetComponentInChildren<TextMeshProUGUI>().text = TextUtils.FromTitleCase(Enum.GetName(typeof(MoveCurrentUnitDestinationType), moveType));
			}
		}

		private void OnButtonClick(MoveCurrentUnitDestinationType moveType)
		{
			try
			{
				ExecuteMoveType(moveType);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				UIController.Instance.ShowError("Error executing action");
			}
		}

		private void ExecuteMoveType(MoveCurrentUnitDestinationType moveType)
		{
			switch (moveType)
			{
			case MoveCurrentUnitDestinationType.ToCurrentTarget:
				if (EngineASX.Instance.Hud.CurrentTarget == null)
				{
					UIController.Instance.ShowMessageBox("No target selected", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				}
				else if (EngineASX.Instance.Hud.CurrentTarget == EngineASX.Instance.LocalUnit)
				{
					UIController.Instance.ShowMessageBox("Unable to move to current target", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				}
				else
				{
					MovePlayerTestUtils.MoveLocalUnitToCurrentTarget();
				}
				break;
			case MoveCurrentUnitDestinationType.ToFringeSector:
				MovePlayerTestUtils.MoveLocalUnitToFringeSector();
				break;
			case MoveCurrentUnitDestinationType.ToRandomSectorPosition:
				MovePlayerTestUtils.MoveLocalUnitToSectorPosition(EngineASX.Instance.LocalUnitSector, EngineASX.Instance.LocalUnitSector.GetRandomSectorPositionWithinGateDistance());
				break;
			case MoveCurrentUnitDestinationType.ToEdgeOfSector:
				MovePlayerTestUtils.MoveLocalUnitToSectorPosition(EngineASX.Instance.LocalUnitSector, EngineASX.Instance.LocalUnitSector.GetRandomSectorPositionOutsideGateDistance());
				break;
			case MoveCurrentUnitDestinationType.ToNearestRefinery:
				MovePlayerTestUtils.MoveToNearestStationOfPurpose(StationPurpose.Refinery);
				break;
			case MoveCurrentUnitDestinationType.ToNearestShipyard:
				MovePlayerTestUtils.MoveToNearestStationOfPurpose(StationPurpose.Shipyard);
				break;
			case MoveCurrentUnitDestinationType.ToNearestTradeStation:
				MovePlayerTestUtils.MoveToNearestStationOfPurpose(StationPurpose.TradeStation);
				break;
			case MoveCurrentUnitDestinationType.ToRandomAbandonedShip:
				MovePlayerTestUtils.MoveToAbandonedShip();
				break;
			case MoveCurrentUnitDestinationType.ToRandomAsteroid:
				MovePlayerTestUtils.MoveToRandomUnitOfType(UnitType.Asteroid);
				break;
			case MoveCurrentUnitDestinationType.ToRandomBanditShip:
				MovePlayerTestUtils.MoveToRandomShipOfFactionType(FactionType.Bandit);
				break;
			case MoveCurrentUnitDestinationType.ToRandomBar:
				MovePlayerTestUtils.MoveToRandomBar();
				break;
			case MoveCurrentUnitDestinationType.ToRandomBountyHunterShip:
				MovePlayerTestUtils.MoveToRandomShipOfFactionStrategy(FactionStrategy.BountyHunt);
				break;
			case MoveCurrentUnitDestinationType.ToRandomCargo:
				MovePlayerTestUtils.MoveToRandomUnitOfType(UnitType.Cargo);
				break;
			case MoveCurrentUnitDestinationType.ToRandomWarship:
				MovePlayerTestUtils.MoveToRandomShipOfFactionStrategy(FactionStrategy.War);
				break;
			case MoveCurrentUnitDestinationType.ToRandomEmpireShip:
				MovePlayerTestUtils.MoveToRandomShipOfFactionType(FactionType.Empire);
				break;
			case MoveCurrentUnitDestinationType.ToRandomEquipmentDealerShip:
				MovePlayerTestUtils.MoveToRandomShipOfFactionStrategy(FactionStrategy.DealEquipment);
				break;
			case MoveCurrentUnitDestinationType.ToRandomExplorerShip:
				MovePlayerTestUtils.MoveToRandomShipOfFactionType(FactionType.Explorer);
				break;
			case MoveCurrentUnitDestinationType.ToRandomGasCloud:
				MovePlayerTestUtils.MoveToRandomUnitOfType(UnitType.GasCloud);
				break;
			case MoveCurrentUnitDestinationType.ToRandomLaboratory:
				MovePlayerTestUtils.MoveToRandomLab();
				break;
			case MoveCurrentUnitDestinationType.ToRandomMinerShip:
				MovePlayerTestUtils.MoveToRandomShipOfFactionStrategy(FactionStrategy.Mine);
				break;
			case MoveCurrentUnitDestinationType.ToRandomOutlawShip:
				MovePlayerTestUtils.MoveToRandomShipOfFactionType(FactionType.Outlaw);
				break;
			case MoveCurrentUnitDestinationType.ToRandomPassengerTransportShip:
				MovePlayerTestUtils.MoveToRandomShipOfFactionStrategy(FactionStrategy.PassengerTransport);
				break;
			case MoveCurrentUnitDestinationType.ToRandomRefinery:
				MovePlayerTestUtils.MoveToNearestStationOfPurpose(StationPurpose.Refinery);
				break;
			case MoveCurrentUnitDestinationType.ToRandomScavengerShip:
				MovePlayerTestUtils.MoveToRandomShipOfFactionStrategy(FactionStrategy.Scavenge);
				break;
			case MoveCurrentUnitDestinationType.ToRandomTraderShip:
				MovePlayerTestUtils.MoveToRandomShipOfFactionStrategy(FactionStrategy.Trade);
				break;
			case MoveCurrentUnitDestinationType.ToRandomTradeStation:
				MovePlayerTestUtils.MoveToRandomStationPurpose(StationPurpose.TradeStation);
				break;
			case MoveCurrentUnitDestinationType.ToRandomUnitUnderAttack:
				MovePlayerTestUtils.MoveToRandomUnitUnderAttack();
				break;
			case MoveCurrentUnitDestinationType.ToRandomShipUnderAttack:
				MovePlayerTestUtils.MoveToRandomShipUnderAttack();
				break;
			case MoveCurrentUnitDestinationType.ToRandomUnstableWormhole:
				MovePlayerToRandomUnstableWormhole.Move();
				break;
			case MoveCurrentUnitDestinationType.ToRandomEscort:
				MovePlayerTestUtils.MoveToRandomShipOfFactionStrategy(FactionStrategy.Escort);
				break;
			case MoveCurrentUnitDestinationType.ChangeSector:
				MovePlayerTestUtils.MoveLocalUnitToAnotherSector();
				break;
			}
		}

		private Unit GetTargetUnit()
		{
			if (TargetLocalUnitToggle.isOn)
			{
				return EngineASX.Instance.LocalUnit;
			}
			return EngineASX.Instance.Hud.CurrentTarget;
		}
	}
}
