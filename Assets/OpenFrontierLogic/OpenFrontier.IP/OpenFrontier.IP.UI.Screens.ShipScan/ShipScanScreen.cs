using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.CargoFactory;
using OpenFrontier.IP.UI.Screens.UnitCargo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.ShipScan
{
	public class ShipScanScreen : EngineScreen
	{
		public FactionContextButton FactionContextButton;

		public Image UnitRenderImage;

		public DockingBayButtonController DockingBayButtonController;

		public Transform PanelsRoot;

		public GameObject DefaultPanel;

		public Transform TogglesRoot;

		public Toggle DockedShipsToggle;

		public Toggle FactoryToggle;

		public Toggle CargoToggle;

		public Toggle ComponentsToggle;

		public Toggle AsteroidToggle;

		public GameObject PilotRoot;

		public TextMeshProUGUI PilotText;

		public GameObject CurrentOrderRoot;

		public Transform PassengersLabelRoot;

		public TextMeshProUGUI PassengersLabel;

		public Text ShipNameLabel;

		public Unit Unit;

		public ShipCargoItemList CargoItemList;

		public ShipComponentsItemList ComponentsList;

		public DockedShipItemList DockedShipsList;

		public CargoFactoryItemListUI CargoFactoryList;

		public UnitConditionControllerUI UnitConditionController;

		public Transform HullSliderRoot;

		public HullSlider HullSlider;

		public Transform CargoSliderRoot;

		public CargoUsageSlider CargoSlider;

		public Transform CapacitorSliderRoot;

		public CapacitorSlider CapacitorSlider;

		public TextMeshProUGUI CurrentOrderText;

		public TextMeshProUGUI AsteroidYieldLabel;

		public TextMeshProUGUI AsteroidYieldTypesLabel;

		protected override void awake()
		{
			base.awake();
			UnitConditionController.enabled = false;
		}

		protected override void refresh()
		{
			base.refresh();
			if (Unit != null)
			{
				bool flag = Unit.UnitType == UnitType.Ship && Unit.UnitClass.ShipType == ShipType.Normal;
				CurrentOrderRoot.gameObject.SetActive(flag);
				PilotRoot.gameObject.SetActive(flag);
				if (flag)
				{
					CurrentOrderText.text = OrdersHelper.GetOrdersTextAndFleetStatus(Unit, EngineASX.Instance.LocalFaction);
					PilotText.text = GetPilotText(Unit);
				}
				ShipNameLabel.text = Unit.GetFriendlyName();
				bool flag2 = Unit.Destructable != null && Unit.IsStationOrShip();
				HullSliderRoot.gameObject.SetActive(flag2);
				if (flag2)
				{
					HullSlider.Unit = Unit;
					HullSlider.Refresh();
				}
				RefreshPassengersText();
				ComponentsToggle.gameObject.SetActive(Unit.Components != null);
				CargoToggle.gameObject.SetActive(Unit.Components != null);
				bool flag3 = Unit.Components != null && Unit.Components.CargoBayComponent != null;
				CargoSliderRoot.gameObject.SetActive(flag3);
				if (flag3)
				{
					CargoSlider.RefreshFromCargoBayComponent(Unit.CargoBayComponent);
					CargoToggle.SetTextColor(Unit.HasAnyCargo() ? GameController.Instance.GameSettings.UIButtonColors.DefaultTextColor : GameController.Instance.GameSettings.UIButtonColors.NoItemsTextColor);
				}
				bool flag4 = Unit.Components != null && Unit.Components.Capacitor != null;
				CapacitorSliderRoot.gameObject.SetActive(flag4);
				if (flag4)
				{
					CapacitorSlider.Capacitor = Unit.Components.Capacitor;
					CapacitorSlider.Refresh();
				}
				bool flag5 = Unit.IsStationOrShip();
				UnitConditionController.gameObject.SetActive(flag5);
				if (flag5)
				{
					UnitConditionController.LocalUnit = Unit;
					UnitConditionController.Tick();
				}
				UnitRenderImage.sprite = Unit.UnitClass.GetRenderSprite();
				if (Unit.CargoBayComponent != null)
				{
					CargoItemList.SetItems(UIHelper.GetCargoItems(Unit.CargoBayComponent));
				}
				bool flag6 = Unit.Asteroid != null;
				if (flag6)
				{
					AsteroidYieldLabel.text = TextFormattingHelper.FormatNumber(Unit.Asteroid.RemainingYield);
					AsteroidYieldTypesLabel.text = GetAsteroidYieldTypes(Unit.Asteroid);
				}
				RefreshComponentsList();
				RefreshDockedShipsList();
				RefreshCargoFactoryList();
				AsteroidToggle.gameObject.SetActive(flag6);
				FactoryToggle.gameObject.SetActive(Unit.Components != null && Unit.Components.FactoryComponent != null);
				DockedShipsToggle.gameObject.SetActive(Unit.IsDockable);
				Toggle defaultToggleOn = GetDefaultToggleOn();
				for (int i = 0; i < TogglesRoot.childCount; i++)
				{
					Toggle component = TogglesRoot.GetChild(i).GetComponent<Toggle>();
					component.isOn = component == defaultToggleOn;
				}
				DockingBayButtonController.Unit = Unit;
				DockingBayButtonController.Refresh();
				FactionContextButton.SetFaction(Unit.Faction);
			}
		}

		private string GetAsteroidYieldTypes(Asteroid asteroid)
		{
			if (asteroid.AsteroidClass != null && asteroid.AsteroidClass.AsteroidYieldItems.Count > 0)
			{
				if (asteroid.AsteroidClass.AsteroidYieldItems.Count == 1)
				{
					return asteroid.AsteroidClass.AsteroidYieldItems[0].CargoClass.ClassName;
				}
				float totalYield = asteroid.AsteroidClass.AsteroidYieldItems.Select((AsteroidYieldItem e) => e.Weight).Sum();
				IEnumerable<string> values = from e in asteroid.AsteroidClass.AsteroidYieldItems
					orderby e.Weight descending
					select $"{e.CargoClass.ClassName} {e.Weight / totalYield:P0}";
				return string.Join(", ", values);
			}
			return "-";
		}

		private Toggle GetDefaultToggleOn()
		{
			if (Unit.UnitType == UnitType.Asteroid)
			{
				return AsteroidToggle;
			}
			return CargoToggle;
		}

		private void FactionInfoButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowFactionsScreen(Unit.Faction);
		}

		private string GetPilotText(Unit unit)
		{
			if (unit.Components.PilotPerson != null)
			{
				string text = unit.Components.PilotPerson.FullNameWithFullRank;
				string pilotExtraInfoText = unit.Components.PilotPerson.GetPilotExtraInfoText();
				if (!string.IsNullOrEmpty(pilotExtraInfoText))
				{
					text += $" ({pilotExtraInfoText})";
				}
				return text;
			}
			return "-";
		}

		private void RefreshPassengersText()
		{
			bool flag = Unit.Components != null && Unit.Components.PassengerCapacity > 0;
			PassengersLabelRoot.gameObject.SetActive(flag);
			if (flag)
			{
				PassengersLabel.text = GetPassengersText();
			}
		}

		private string GetPassengersText()
		{
			if (Unit.Components != null)
			{
				return $"{Unit.Components.PassengerCount} / {Unit.Components.PassengerCapacity}";
			}
			return "-";
		}

		private void RefreshCargoFactoryList()
		{
			if (Unit.Components != null && Unit.Components.FactoryComponent != null)
			{
				CargoFactoryList.SetItems(Unit.Components.FactoryComponent.Items);
			}
			else
			{
				CargoFactoryList.ClearActiveItems();
			}
		}

		private void RefreshComponentsList()
		{
			if (Unit.Components != null)
			{
				ComponentsList.SetItemsFromUnitComponents(Unit.Components);
			}
			else
			{
				ComponentsList.ClearActiveItems();
			}
		}

		private void RefreshDockedShipsList()
		{
			if (Unit.Components != null)
			{
				DockedShipsList.Hangar = Unit.Components.HangarComponent;
			}
			else
			{
				DockedShipsList.Hangar = null;
			}
			DockedShipsList.Refresh();
		}
	}
}
