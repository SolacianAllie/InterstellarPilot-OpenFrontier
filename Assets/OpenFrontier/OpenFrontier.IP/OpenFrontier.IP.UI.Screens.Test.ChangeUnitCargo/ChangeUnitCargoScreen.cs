using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Screens.CargoPicker;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test.ChangeUnitCargo
{
	public class ChangeUnitCargoScreen : ScreenBase
	{
		public Image SelectedCargoIconImage;

		public Text TitleLabel;

		public CargoUsageSlider CargoUsageSlider;

		public Button RemoveCargoButton;

		public Text SelectedCargoQuantityText;

		public Slider SelectedCargoQuantitySlider;

		public Button CargoInfoButton;

		public GameObject InfoRoot;

		public GenericCargoList CargoItemList;

		public Text SelectedLabel;

		private Unit unit;

		public Button AddCargoButton;

		public Button AddCargoQuantityButton;

		public Button DeductCargoQuantityButton;

		public Unit Unit
		{
			get
			{
				return unit;
			}
			set
			{
				unit = value;
			}
		}

		public int GetSelectedCargoQuantity()
		{
			if (CargoItemList.FirstSelectedItem == null)
			{
				return 0;
			}
			int maxCountOfCargoItem = GetMaxCountOfCargoItem(CargoItemList.FirstSelectedItem);
			return Mathf.RoundToInt(Mathf.Lerp(1f, maxCountOfCargoItem, SelectedCargoQuantitySlider.value));
		}

		private int GetMaxCountOfCargoItem(CargoBayItem item)
		{
			float num = unit.CargoBayComponent.Capacity;
			CargoBayItem[] cargoItems = unit.CargoBayComponent.GetCargoItems();
			foreach (CargoBayItem cargoBayItem in cargoItems)
			{
				if (cargoBayItem.CargoClass != item.CargoClass)
				{
					num -= cargoBayItem.Load;
				}
			}
			if (item.CargoClass.Volume > 0f)
			{
				return (int)(num / item.CargoClass.Volume);
			}
			return 1000;
		}

		protected override void awake()
		{
			base.awake();
			SelectedCargoQuantitySlider.onValueChanged.AddListener(SelectedCargoQuantitySlider_ValueChanged);
			RemoveCargoButton.onClick.AddListener(RemoveCargoButtonClick);
			CargoInfoButton.onClick.AddListener(ShowCargoInfo);
			CargoItemList.SelectedItemChanged += ItemList_SelectedItemChanged;
			AddCargoButton.onClick.AddListener(AddCargoButtonClick);
			AddCargoQuantityButton.onClick.AddListener(() =>
			{
				ChangeCargoQuantity(1);
			});
			DeductCargoQuantityButton.onClick.AddListener(() =>
			{
				ChangeCargoQuantity(-1);
			});
		}

		private void ChangeCargoQuantity(int value)
		{
			if (CargoItemList.FirstSelectedItem == null)
			{
				return;
			}
			if (value > 0)
			{
				if (CargoItemList.FirstSelectedItem.Quantity < GetMaxCountOfCargoItem(CargoItemList.FirstSelectedItem))
				{
					CargoItemList.FirstSelectedItem.Quantity++;
				}
			}
			else if (CargoItemList.FirstSelectedItem.Quantity > 1)
			{
				CargoItemList.FirstSelectedItem.Quantity--;
			}
			unit.CargoBayComponent.SetCount(CargoItemList.FirstSelectedItem.CargoClass, CargoItemList.FirstSelectedItem.Quantity);
			RefreshCargoUsageSlider();
			RefreshSelectedItemInfo();
			CargoItemList.RefreshVisibleItems();
		}

		private void AddCargoButtonClick()
		{
			List<CargoPickerItem> addableCargoItems = GetAddableCargoItems();
			if (addableCargoItems.Count > 0)
			{
				UIController.Instance.ScreenNavigator.ShowCargoPickerScreen(addableCargoItems, CargoPickerItemPicked, "Select Cargo...", keepInNavigationStack: false, (CargoPickerScreen screen) =>
				{
					screen.AutoSetCargoAmountSliderToMax = false;
				});
			}
			else
			{
				UIController.Instance.ShowMessageBox("No more compatible cargo types can fit", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
		}

		private void CargoPickerItemPicked(CargoPickerScreen sender, CargoPickerResult result)
		{
			if (sender.IsCurrentScreen)
			{
				sender.NavigateBack();
			}
			if (result != null && result.CargoClass != null && result.Quantity > 0)
			{
				CargoBayItem cargoBayItem = new CargoBayItem
				{
					CargoClass = result.CargoClass,
					Quantity = result.Quantity
				};
				unit.CargoBayComponent.AddToCargo(cargoBayItem.CargoClass, cargoBayItem.Quantity, ignoreCapacity: true);
				CargoItemList.Add(cargoBayItem);
				CargoItemList.FirstSelectedItem = cargoBayItem;
				RefreshCargoUsageSlider();
			}
		}

		private List<CargoPickerItem> GetAddableCargoItems()
		{
			List<CargoPickerItem> list = new List<CargoPickerItem>();
			HashSet<int> hashSet = new HashSet<int>();
			float freeSpace = unit.CargoBayComponent.FreeSpace;
			CargoBayItem[] cargoItems = unit.CargoBayComponent.GetCargoItems();
			foreach (CargoBayItem cargoBayItem in cargoItems)
			{
				if (!hashSet.Contains(cargoBayItem.CargoClass.UniqueId))
				{
					hashSet.Add(cargoBayItem.CargoClass.UniqueId);
				}
			}
			bool flag = true;
			HashSet<int> hashSet2 = (from e in unit.Components.YieldAllCompatibleAmmoCargoClasses()
				select e.UniqueId).ToHashSet();
			foreach (CargoClass cargoClass in EngineASX.Instance.CargoClasses)
			{
				if (!cargoClass.IsReserved && !hashSet.Contains(cargoClass.UniqueId) && (flag || hashSet2.Contains(cargoClass.UniqueId)))
				{
					int num = 1000;
					if (cargoClass.Volume > 0f)
					{
						num = (int)(freeSpace / cargoClass.Volume);
					}
					if (num > 0)
					{
						list.Add(new CargoPickerItem
						{
							MinQuantiy = 1,
							MaxQuantity = num,
							CargoClass = cargoClass,
							AvailableText = "max"
						});
					}
				}
			}
			return list;
		}

		protected override void refresh()
		{
			base.refresh();
			if (unit != null)
			{
				CargoItemList.SetItems(from e in unit.CargoBayComponent.GetCargoItems()
					orderby e.Load descending
					select e);
				RefreshCargoUsageSlider();
				RefreshSelectedItemInfo();
				TitleLabel.text = "Customize " + unit.GetFriendlyName() + " cargo";
			}
		}

		private void RefreshCargoUsageSlider()
		{
			CargoUsageSlider.RefreshFromCargoBayComponent(unit.CargoBayComponent);
		}

		private void RemoveCargoButtonClick()
		{
			if (CargoItemList.FirstSelectedItem != null)
			{
				unit.CargoBayComponent.RemoveCargoType(CargoItemList.FirstSelectedItem.CargoClass);
				Refresh();
			}
		}

		private void ItemList_SelectedItemChanged(ScrollList<CargoBayItem> sender, CargoBayItem oldItem, CargoBayItem newItem)
		{
			RefreshSelectedItemInfo();
		}

		private void RefreshSelectedItemInfo()
		{
			InfoRoot.gameObject.SetActive(CargoItemList.FirstSelectedItem != null);
			if (CargoItemList.FirstSelectedItem == null)
			{
				return;
			}
			SelectedLabel.text = CargoItemList.FirstSelectedItem.CargoClass.ClassName;
			SelectedCargoIconImage.sprite = EngineASX.Instance.EngineResources.GetCargoSpriteOrDefault(CargoItemList.FirstSelectedItem.CargoClass);
			int maxCountOfCargoItem = GetMaxCountOfCargoItem(CargoItemList.FirstSelectedItem);
			if (maxCountOfCargoItem > 0)
			{
				if (maxCountOfCargoItem == 1)
				{
					SelectedCargoQuantitySlider.value = 1f;
				}
				else
				{
					SelectedCargoQuantitySlider.value = (float)(CargoItemList.FirstSelectedItem.Quantity - 1) / (float)(maxCountOfCargoItem - 1);
				}
			}
			else
			{
				SelectedCargoQuantitySlider.value = 0f;
			}
			RefreshSelectCargoQuantityText();
		}

		private void ShowCargoInfo()
		{
			UIController.Instance.ScreenNavigator.ShowCargoInfoScreen(CargoItemList.FirstSelectedItem.CargoClass);
		}

		private void SelectedCargoQuantitySlider_ValueChanged(float delta)
		{
			if (unit != null && CargoItemList.FirstSelectedItem != null)
			{
				int selectedCargoQuantity = GetSelectedCargoQuantity();
				CargoItemList.FirstSelectedItem.Quantity = selectedCargoQuantity;
				RefreshSelectCargoQuantityText();
				RefreshCargoUsageSlider();
				CargoItemList.RefreshVisibleItems();
			}
		}

		private void RefreshSelectCargoQuantityText()
		{
			if (CargoItemList.FirstSelectedItem != null)
			{
				SelectedCargoQuantityText.text = TextFormattingHelper.FormatNumber(CargoItemList.FirstSelectedItem.Quantity);
			}
		}
	}
}
