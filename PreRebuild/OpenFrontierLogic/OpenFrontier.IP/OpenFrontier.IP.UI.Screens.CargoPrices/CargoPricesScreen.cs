using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoPrices
{
	public class CargoPricesScreen : EngineScreen
	{
		public OrderShipDockAtButtonController OrderShipDockAtButtonController;

		public UnitContextButton SelectedDockContextButton;

		public GameObject SelectedCargoClassRoot;

		private Unit selectedLocation;

		public Button CargoInfoButton;

		public Image CargoIconImage;

		public CargoPriceTypeList CargoTypeList;

		public CargoPriceItemList BuyersGrid;

		public CargoClass CargoClass;

		public Text CargoClassLabel;

		public TextMeshProUGUI CargoClassInfoLabel;

		private float nextRefreshPriceListsTime;

		public float RefreshPriceListInterval = 3f;

		public CargoPriceItemList SellersGrid;

		public bool IncludeEquipment = true;

		protected override void awake()
		{
			base.awake();
			CargoInfoButton.onClick.AddListener(CargoInfoButtonClick);
			RefreshSelectableCargoTypes();
			CargoTypeList.SelectedItemChanged += CargoTypeList_SelectedItemChanged;
			BuyersGrid.SelectedItemChanged += BuyersGrid_SelectedItemChanged;
			SellersGrid.SelectedItemChanged += SellersGrid_SelectedItemChanged;
		}

		private void CargoInfoButtonClick()
		{
			if (CargoClass != null)
			{
				UIController.Instance.ScreenNavigator.ShowCargoInfoScreen(CargoClass);
			}
		}

		public void SetSelectedLocation(Unit unit)
		{
			selectedLocation = unit;
			SelectedDockContextButton.SetUnit(unit);
			RefreshPriceLocationDisplay();
		}

		private void SellersGrid_SelectedItemChanged(ScrollList<CargoPriceData> sender, CargoPriceData oldItem, CargoPriceData newItem)
		{
			if (newItem != null)
			{
				BuyersGrid.FirstSelectedItem = null;
				SetSelectedLocation(newItem.Trader.Unit);
			}
		}

		private void BuyersGrid_SelectedItemChanged(ScrollList<CargoPriceData> sender, CargoPriceData oldItem, CargoPriceData newItem)
		{
			if (newItem != null)
			{
				SellersGrid.FirstSelectedItem = null;
				SetSelectedLocation(newItem.Trader.Unit);
			}
		}

		private void RefreshPriceLocationDisplay()
		{
		}

		private void RefreshSelectableCargoTypes()
		{
			CargoTypeList.Awake();
			CargoTypeList.SetItems(GetCargoPriceTypeDataItems());
		}

		private List<CargoPriceTypeData> GetCargoPriceTypeDataItems()
		{
			return (from e in EngineASX.Instance.CargoClasses
				where !e.IsReserved && (e.IsTraded || IncludeEquipment)
				orderby e.IsDeployable, e.IsEquipment, e.ClassName
				select new CargoPriceTypeData
				{
					CargoClass = e,
					HasPrices = true
				}).ToList();
		}

		private void CargoTypeList_SelectedItemChanged(ScrollList<CargoPriceTypeData> sender, CargoPriceTypeData oldItem, CargoPriceTypeData newItem)
		{
			SetSelectedLocation(null);
			if (newItem != null)
			{
				CargoClass = newItem.CargoClass;
			}
			else
			{
				CargoClass = null;
			}
			RefreshCurrentCargoClassDisplay();
			ResetScrollPositions();
		}

		protected override void update()
		{
			base.update();
			OrderShipDockAtButtonController.TargetUnit = selectedLocation;
			RefreshPriceListsPeriodically();
		}

		private void RefreshPriceListsPeriodically()
		{
			if (Time.time > nextRefreshPriceListsTime)
			{
				nextRefreshPriceListsTime = Time.time + RefreshPriceListInterval;
				RefreshPriceListsActiveItems();
			}
		}

		protected override void refresh()
		{
			base.refresh();
			SelectCargoClassIfNull();
			if (CargoClass != null)
			{
				SyncTypeListSelectedItem();
			}
			RefreshCurrentCargoClassDisplay();
		}

		private void SyncTypeListSelectedItem()
		{
			if (CargoTypeList.FirstSelectedItem != null && CargoTypeList.FirstSelectedItem.CargoClass != CargoClass)
			{
				CargoTypeList.FirstSelectedItem = CargoTypeList.ActiveItems.FirstOrDefault((CargoPriceTypeData e) => e.CargoClass == CargoClass);
				CargoTypeList.ScrollToSelected();
			}
		}

		private void RefreshCurrentCargoClassDisplay()
		{
			SelectedCargoClassRoot.SetActive(CargoClass != null);
			if (CargoClass != null)
			{
				RefreshCurrentCargoIcon();
				RefreshCurrentCargoLabel();
				RefreshPriceLists();
			}
		}

		private void SelectCargoClassIfNull()
		{
			if (CargoClass == null && CargoTypeList.ActiveItems.Count > 0)
			{
				CargoClass = CargoTypeList.ActiveItems.FirstOrDefault().CargoClass;
			}
		}

		private void RefreshCurrentCargoLabel()
		{
			CargoClassLabel.text = CargoClass.ClassName;
			CargoClassInfoLabel.text = CargoClass.GetShortNameElseLong() + " Info";
		}

		private void RefreshCurrentCargoIcon()
		{
			CargoIconImage.sprite = Eng.EngineResources.GetCargoSpriteOrDefault(CargoClass);
		}

		private void RefreshPriceLists()
		{
			if (CargoClass != null)
			{
				BuyersGrid.Refresh();
				SellersGrid.Refresh();
			}
		}

		private void RefreshPriceListsActiveItems()
		{
			if (CargoClass != null)
			{
				BuyersGrid.RefreshVisibleItems();
				SellersGrid.RefreshVisibleItems();
			}
		}

		private void ResetScrollPositions()
		{
			BuyersGrid.ResetScrollPosition();
			SellersGrid.ResetScrollPosition();
		}
	}
}
