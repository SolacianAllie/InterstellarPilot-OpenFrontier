using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UnitCargo
{
	public class UnitCargoScreen : EngineScreen
	{
		public Image CargoIconImage;

		public Text UnitNameText;

		public Button DeployButton;

		public CargoUsageSlider CargoUsageSlider;

		public Button EjectButton;

		public Text EjectCargoQuantityLabel;

		public GameObject EjectCargoRoot;

		public Slider EjectCargoSlider;

		public Button InfoButton;

		public GameObject InfoRoot;

		public ShipCargoItemList ItemList;

		public Button PricesButton;

		public Text SelectedLabel;

		private Unit sourceUnit;

		public bool NavigateBackWhenUnitInvaild;

		private float lastCargoBayChangeTime;

		public Unit SourceUnit
		{
			get
			{
				return sourceUnit;
			}
			set
			{
				sourceUnit = value;
			}
		}

		public int GetEjectCargoQuantity()
		{
			if (ItemList.FirstSelectedItem == null)
			{
				return 0;
			}
			int cargoCountOf = sourceUnit.GetCargoCountOf(ItemList.FirstSelectedItem.CargoClass);
			int num = Mathf.Min(1, cargoCountOf);
			return num + Mathf.RoundToInt(EjectCargoSlider.value * (float)(cargoCountOf - num));
		}

		public void EjectSelectedQuantity()
		{
			if (ItemList.FirstSelectedItem == null)
			{
				return;
			}
			int ejectCargoQuantity = GetEjectCargoQuantity();
			if (ejectCargoQuantity > 0)
			{
				CargoClass cargoClass = ItemList.FirstSelectedItem.CargoClass;
				sourceUnit.Components.CargoBayComponent.EjectCargo(cargoClass, ejectCargoQuantity);
				int countOf = sourceUnit.Components.CargoBayComponent.GetCountOf(cargoClass);
				if (countOf == 0 || ItemList.FirstSelectedItem == null)
				{
					Refresh();
					return;
				}
				ItemList.FirstSelectedItem.Quantity = countOf;
				ItemList.SelectedUIItem.Refresh();
				lastCargoBayChangeTime = sourceUnit.CargoBayComponent.LastChangedTime;
				RefreshSelectedItemInfo();
			}
		}

		public void DeployCargo()
		{
			string msg = "";
			if (CanDeployCargo(out msg))
			{
				CargoClass cargoClass = ItemList.FirstSelectedItem.CargoClass;
				DeployableUnitCargoClass component = cargoClass.GetComponent<DeployableUnitCargoClass>();
				if (component != null)
				{
					Vector3 checkSectorPosition = sourceUnit.SectorPosition + sourceUnit.transform.forward * 50f;
					float deployableRadius = GetDeployableRadius(cargoClass);
					Vector3 sectorPosition = PhysicsNonOverlappingPositionFinder.FindSectorPosition(sourceUnit.Sector, checkSectorPosition, deployableRadius, GameController.Instance.NonOVerlappingUnitsMask);
					component.DeployUnit(sourceUnit.Sector, sectorPosition, sourceUnit);
					NavigateBack();
				}
			}
			else if (msg != null)
			{
				UIController.Instance.ShowMessageBox(msg, MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
		}

		protected override void awake()
		{
			base.awake();
			EjectCargoSlider.onValueChanged.AddListener(EjectCargoSlider_ValueChanged);
			EjectButton.onClick.AddListener(EjectSelectedQuantity);
			InfoButton.onClick.AddListener(ShowCargoInfo);
			PricesButton.onClick.AddListener(ShowPrices);
			DeployButton.onClick.AddListener(DeployCargo);
			ItemList.SelectedItemChanged += ItemList_SelectedItemChanged;
		}

		protected override void onEnable()
		{
			base.onEnable();
			EjectCargoSlider.value = 1f;
		}

		protected override void refresh()
		{
			base.refresh();
			if (Eng != null && sourceUnit != null)
			{
				ItemList.SetItems(UIHelper.GetCargoItems(sourceUnit.CargoBayComponent));
				CargoUsageSlider.RefreshFromCargoBayComponent(sourceUnit.CargoBayComponent);
				RefreshSelectedItemInfo();
				UnitNameText.text = sourceUnit.GetFriendlyName();
				if (sourceUnit.CargoBayComponent != null)
				{
					lastCargoBayChangeTime = sourceUnit.CargoBayComponent.LastChangedTime;
				}
				PricesButton.interactable = ItemList.FirstSelectedItem != null && !ItemList.FirstSelectedItem.CargoClass.IsReserved;
			}
		}

		protected override void update()
		{
			base.update();
			if (NavigateBackWhenUnitInvaild && UnitsInvalid() && GetBackTarget() != null)
			{
				if (Eng.PlayerUnit != null && Eng.PlayerUnit.IsValidAndNotDestroyed)
				{
					UIController.Instance.QuickMsg.AddMessage("Lost contact with target");
				}
				NavigateBack();
			}
			else if (sourceUnit != null && sourceUnit.CargoBayComponent != null && sourceUnit.CargoBayComponent.LastChangedTime > lastCargoBayChangeTime)
			{
				Refresh();
			}
		}

		private bool UnitsInvalid()
		{
			if (!(sourceUnit == null))
			{
				return !sourceUnit.IsValidAndNotDestroyed;
			}
			return true;
		}

		private void ItemList_SelectedItemChanged(ScrollList<CargoBayItem> sender, CargoBayItem oldItem, CargoBayItem newItem)
		{
			RefreshSelectedItemInfo();
		}

		private void RefreshSelectedItemInfo()
		{
			InfoRoot.gameObject.SetActive(ItemList.FirstSelectedItem != null);
			if (ItemList.FirstSelectedItem != null)
			{
				DeployButton.interactable = IsSelectedItemDeployable();
				SelectedLabel.text = ItemList.FirstSelectedItem.CargoClass.ClassName;
				CargoIconImage.sprite = Eng.EngineResources.GetCargoSpriteOrDefault(ItemList.FirstSelectedItem.CargoClass);
			}
			EjectCargoRoot.gameObject.SetActive(ItemList.FirstSelectedItem != null && CanEjectCargo());
			if (ItemList.FirstSelectedItem != null)
			{
				RefreshEjectQuantity();
			}
		}

		private void ShowPrices()
		{
			UIController.Instance.ScreenNavigator.ShowCargoPricesScreen(ItemList.FirstSelectedItem.CargoClass);
		}

		private void ShowCargoInfo()
		{
			UIController.Instance.ScreenNavigator.ShowCargoInfoScreen(ItemList.FirstSelectedItem.CargoClass);
		}

		private void EjectCargoSlider_ValueChanged(float delta)
		{
			if ((bool)sourceUnit)
			{
				RefreshEjectQuantity();
			}
		}

		public bool IsSelectedItemDeployable()
		{
			return ItemList.FirstSelectedItem.CargoClass.GetComponent<DeployableUnitCargoClass>() != null;
		}

		private float GetDeployableRadius(CargoClass cargoClass)
		{
			Unit component = cargoClass.RelatedPrefab.GetComponent<Unit>();
			if (component != null)
			{
				return component.UnitClass.DisplayData.Radius;
			}
			return 0f;
		}

		private bool CanDeployCargo(out string msg)
		{
			msg = null;
			DeployableUnitCargoClass component = ItemList.FirstSelectedItem.CargoClass.GetComponent<DeployableUnitCargoClass>();
			if (component != null)
			{
				if (!sourceUnit.IsDocked)
				{
					if (sourceUnit.IsFullyDecloaked)
					{
						Unit blockingUnit = null;
						Unit component2 = ItemList.FirstSelectedItem.CargoClass.RelatedPrefab.GetComponent<Unit>();
						GetDeployableRadius(ItemList.FirstSelectedItem.CargoClass);
						if (component2 != null && component2.UnitClass.UnitType == UnitType.Station && component2.UnitClass.StationPurpose == StationPurpose.TradeStation && !sourceUnit.Sector.HasPlanets)
						{
							msg = "Trade station can only be built in a Planet sector";
							return false;
						}
						if (component.CanDeploy(sourceUnit.Sector, sourceUnit.SectorPosition, out blockingUnit))
						{
							return true;
						}
						if (blockingUnit != null)
						{
							msg = $"Cannot deploy - too close to \"{blockingUnit.GetFriendlyName()}\"";
						}
						else
						{
							msg = "Cannot deploy - too close to objects";
						}
					}
					else
					{
						msg = "Cannot deploy while cloaked";
					}
				}
				else
				{
					msg = "Cannot deploy while docked";
				}
			}
			return false;
		}

		private bool CanEjectCargo()
		{
			if (sourceUnit.IsDocked)
			{
				return Eng.GameSettings.CanEjectCargoWhileDocked;
			}
			return true;
		}

		private void RefreshEjectQuantity()
		{
			EjectCargoQuantityLabel.text = TextFormattingHelper.FormatNumber(GetEjectCargoQuantity());
		}
	}
}
