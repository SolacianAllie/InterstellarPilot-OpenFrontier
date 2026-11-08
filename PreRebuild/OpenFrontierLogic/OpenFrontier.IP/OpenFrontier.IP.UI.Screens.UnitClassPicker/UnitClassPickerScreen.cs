using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UnitClassPicker
{
	public class UnitClassPickerScreen : ScreenBase
	{
		public delegate void UnitClassPickedHandler(UnitClass unitClass);

		public Button ConfirmButton;

		public TextMeshProUGUI TitleText;

		public List<UnitClass> UnitClassFilter;

		public UnitClassPickerList UnitClassPickerList;

		public string Title
		{
			get
			{
				return TitleText.text;
			}
			set
			{
				TitleText.text = value;
			}
		}

		public event UnitClassPickedHandler UnitClassPicked;

		protected override void awake()
		{
			base.awake();
			ConfirmButton.onClick.AddListener(ConfirmButtonClick);
		}

		protected override void refresh()
		{
			base.refresh();
			if (UnitClassFilter != null)
			{
				UnitClassPickerList.SetItems(UnitClassFilter);
				return;
			}
			UnitClassPickerList.SetItems(from e in EngineASX.Instance.UnitClasses
				where e.UnitType == UnitType.Ship && e.ShipType == ShipType.Normal && e.IsUsable && e.SeedInSandbox && !e.IsBarebonesShip
				orderby e.UnitSeries.DisplayOrder, e.SaleCost
				select e);
		}

		private void ConfirmButtonClick()
		{
			if (UnitClassPickerList.FirstSelectedItem != null && UnitClassPicked != null)
			{
				UnitClassPicked(UnitClassPickerList.FirstSelectedItem);
			}
		}
	}
}
