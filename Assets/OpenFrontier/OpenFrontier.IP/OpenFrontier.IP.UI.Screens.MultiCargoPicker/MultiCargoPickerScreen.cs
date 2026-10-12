using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.MultiCargoPicker
{
	public class MultiCargoPickerScreen : EngineScreen
	{
		public delegate void CargoPickerFinishedHandler(MultiCargoPickerScreen sender, MultiCargoPickerResult result);

		public List<MultiCargoPickerItem> Items = new List<MultiCargoPickerItem>();

		public MultiCargoPickerList CargoPickerList;

		public Button OkButton;

		public Button CancelButton;

		public Text TitleLabel;

		public event CargoPickerFinishedHandler Finished;

		protected override void awake()
		{
			base.awake();
			CargoPickerList.SelectedItemChanged += CargoPickerList_SelectedItemChanged;
			OkButton.onClick.AddListener(OkButtonClick);
			CancelButton.onClick.AddListener(CancelButtonClick);
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
			if (Finished == null)
			{
				return;
			}
			if (CargoPickerList.FirstSelectedItem != null)
			{
				MultiCargoPickerResult multiCargoPickerResult = new MultiCargoPickerResult();
				multiCargoPickerResult.CargoClasses.AddRange(CargoPickerList.SelectedItems.Select((MultiCargoPickerItem e) => e.CargoClass));
				Finished(this, multiCargoPickerResult);
			}
			else
			{
				Finished(this, null);
			}
		}

		private void CargoPickerList_SelectedItemChanged(ScrollList<MultiCargoPickerItem> sender, MultiCargoPickerItem oldItem, MultiCargoPickerItem newItem)
		{
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
		}
	}
}
