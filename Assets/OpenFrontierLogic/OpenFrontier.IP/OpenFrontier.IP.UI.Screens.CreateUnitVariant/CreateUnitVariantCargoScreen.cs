using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Screens.CargoPicker;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CreateUnitVariant
{
	public class CreateUnitVariantCargoScreen : ScreenBase
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

		private CustomUnitVariant customUnitVariant;

		public Button AddCargoButton;

		public Button AddCargoQuantityButton;

		public Button DeductCargoQuantityButton;

		public Button AutoAddCargoButton;

		public CustomUnitVariant CustomUnitVariant
		{
			get
			{
				return customUnitVariant;
			}
			set
			{
				customUnitVariant = value;
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
			float num = customUnitVariant.GetCargoCapacity();
			foreach (CargoBayItem cargoItem in customUnitVariant.CargoItems)
			{
				if (cargoItem != item)
				{
					num -= cargoItem.Load;
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
			AutoAddCargoButton.onClick.AddListener(AutoAddCargoButtonClick);
		}

		private void AutoAddCargoButtonClick()
		{
			List<CargoBayItem> defaultCompatibleCargoItems = CustomUnitVariantHelper.GetDefaultCompatibleCargoItems(customUnitVariant);
			customUnitVariant.CargoItems.Clear();
			customUnitVariant.CargoItems.AddRange(defaultCompatibleCargoItems);
			Refresh();
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
				customUnitVariant.CargoItems.Add(cargoBayItem);
				CargoItemList.Add(cargoBayItem);
				CargoItemList.FirstSelectedItem = cargoBayItem;
				RefreshCargoUsageSlider();
			}
		}

		private List<CargoPickerItem> GetAddableCargoItems()
		{
			List<CargoPickerItem> list = new List<CargoPickerItem>();
			HashSet<int> hashSet = new HashSet<int>();
			float availableCargoCapacity = customUnitVariant.GetAvailableCargoCapacity();
			foreach (CargoBayItem cargoItem in customUnitVariant.CargoItems)
			{
				if (!hashSet.Contains(cargoItem.CargoClass.UniqueId))
				{
					hashSet.Add(cargoItem.CargoClass.UniqueId);
				}
			}
			HashSet<int> hashSet2 = (from e in CustomUnitVariantHelper.GetCompatibleCargoClasses(customUnitVariant)
				select e.UniqueId).ToHashSet();
			foreach (CargoClass cargoClass in EngineASX.Instance.CargoClasses)
			{
				if (cargoClass.IsEquipment && !cargoClass.IsReserved && !hashSet.Contains(cargoClass.UniqueId) && hashSet2.Contains(cargoClass.UniqueId))
				{
					int num = 1000;
					if (cargoClass.Volume > 0f)
					{
						num = (int)(availableCargoCapacity / cargoClass.Volume);
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
			if (customUnitVariant != null)
			{
				CargoItemList.SetItems(customUnitVariant.CargoItems.OrderByDescending((CargoBayItem e) => e.Load));
				RefreshCargoUsageSlider();
				RefreshSelectedItemInfo();
				TitleLabel.text = "Customize " + customUnitVariant.FullName + " cargo";
			}
		}

		private void RefreshCargoUsageSlider()
		{
			CargoUsageSlider.Refresh(customUnitVariant.GetCargoUsage(), customUnitVariant.GetCargoCapacity());
		}

		private void RemoveCargoButtonClick()
		{
			if (CargoItemList.FirstSelectedItem != null)
			{
				customUnitVariant.CargoItems.Remove(CargoItemList.FirstSelectedItem);
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
			if (customUnitVariant != null && CargoItemList.FirstSelectedItem != null)
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
