using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoPicker
{
	public class CargoPickerScreen : EngineScreen
	{
		public delegate void CargoPickerFinishedHandler(CargoPickerScreen sender, CargoPickerResult result);

		public List<CargoPickerItem> Items = new List<CargoPickerItem>();

		public CargoPickerList CargoPickerList;

		public Slider CargoAmountSlider;

		public Button OkButton;

		public Button CancelButton;

		public Text SliderQuantityLabel;

		public Text TitleLabel;

		public bool AutoSetCargoAmountSliderToMax = true;

		public bool AllowPickQuantity = true;

		public event CargoPickerFinishedHandler Finished;

		protected override void awake()
		{
			base.awake();
			CargoPickerList.SelectedItemChanged += CargoPickerList_SelectedItemChanged;
			OkButton.onClick.AddListener(OkButtonClick);
			CancelButton.onClick.AddListener(CancelButtonClick);
			CargoAmountSlider.onValueChanged.AddListener(CargoAmountSliderValueChanged);
		}

		private void CargoAmountSliderValueChanged(float arg0)
		{
			RefreshCargoQuantityLabel();
		}

		private void RefreshCargoQuantityLabel()
		{
			SliderQuantityLabel.text = TextFormattingHelper.FormatCargoAmount((int)CargoAmountSlider.value);
		}

		private void CancelButtonClick()
		{
			if (Finished != null)
			{
				Finished(this, null);
			}
		}

		private void OkButtonClick()
		{
			if (Finished != null)
			{
				if (CargoPickerList.FirstSelectedItem != null)
				{
					CargoPickerResult result = new CargoPickerResult
					{
						CargoClass = CargoPickerList.FirstSelectedItem.CargoClass,
						Quantity = (int)CargoAmountSlider.value
					};
					Finished(this, result);
				}
				else
				{
					Finished(this, null);
				}
			}
		}

		private void CargoPickerList_SelectedItemChanged(ScrollList<CargoPickerItem> sender, CargoPickerItem oldItem, CargoPickerItem newItem)
		{
			RefreshAmountSlider();
			SetCargoAmountSliderDefaultValue();
		}

		private void SetCargoAmountSliderDefaultValue()
		{
			CargoAmountSlider.normalizedValue = (AutoSetCargoAmountSliderToMax ? 1f : 0f);
		}

		private void RefreshAmountSlider()
		{
			bool flag = AllowPickQuantity && CargoPickerList.FirstSelectedItem != null;
			CargoAmountSlider.gameObject.SetActive(flag);
			if (flag)
			{
				CargoAmountSlider.minValue = CargoPickerList.FirstSelectedItem.MinQuantiy;
				CargoAmountSlider.maxValue = CargoPickerList.FirstSelectedItem.MaxQuantity;
				RefreshCargoQuantityLabel();
			}
		}

		protected override void update()
		{
			base.update();
			OkButton.interactable = CargoPickerList.FirstSelectedItem != null;
		}

		protected override void refresh()
		{
			base.refresh();
			CargoPickerList.SetItems(Items);
			RefreshAmountSlider();
		}
	}
}
