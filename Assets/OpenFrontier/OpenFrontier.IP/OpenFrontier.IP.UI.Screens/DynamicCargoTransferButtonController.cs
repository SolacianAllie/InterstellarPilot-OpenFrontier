using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.UnitPicker;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens
{
	public class DynamicCargoTransferButtonController : MonoBehaviour
	{
		private Button button;

		public Unit TransferringUnit;

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(ButtonClick);
		}

		private void ButtonClick()
		{
			IEnumerable<Unit> cargoTransferTargetUnits = GetCargoTransferTargetUnits(TransferringUnit);
			if (cargoTransferTargetUnits.Count() > 0)
			{
				if (cargoTransferTargetUnits.Count() == 1 && cargoTransferTargetUnits.First() == TransferringUnit.GetDockUnit())
				{
					UIController.Instance.ScreenNavigator.ShowCargoTransferScreen(TransferringUnit, cargoTransferTargetUnits.First());
				}
				else
				{
					UIController.Instance.ScreenNavigator.ShowUnitPickerScreen(cargoTransferTargetUnits.ToList(), TransferringUnit.Sector, allowNone: false, OnTransferTargetUnitPicker, "Cargo Transfer With...");
				}
			}
		}

		private void OnTransferTargetUnitPicker(UnitPickerScreen sender, bool result, UnitPickerItem item)
		{
			if (result)
			{
				UIController.Instance.ScreenNavigator.ShowCargoTransferScreen(TransferringUnit, item.Unit);
			}
		}

		public void Refresh()
		{
			button.interactable = ShouldShowCargoTransferButton();
		}

		public bool ShouldShowCargoTransferButton()
		{
			if (TransferringUnit != null && TransferringUnit.IsOwnedByPlayer && GetCargoTransferTargetUnits(TransferringUnit).Count() > 0)
			{
				if (TransferringUnit != null)
				{
					return TransferringUnit.CargoBayComponent != null;
				}
				return false;
			}
			return false;
		}

		private static IEnumerable<Unit> GetCargoTransferTargetUnits(Unit transferringUnit)
		{
			List<Unit> list = new List<Unit>();
			FindTransferTargetUnits(transferringUnit, transferringUnit.GetRootUnit(), list);
			return list;
		}

		private static void FindTransferTargetUnits(Unit transferringUnit, Unit unitToSearch, List<Unit> targetList)
		{
			if (unitToSearch != transferringUnit && unitToSearch.Faction == transferringUnit.Faction)
			{
				targetList.Add(unitToSearch);
			}
			if (!unitToSearch.HasDockedUnits())
			{
				return;
			}
			foreach (UnitComponentHolder dockedUnit in unitToSearch.GetDockedUnits())
			{
				FindTransferTargetUnits(transferringUnit, dockedUnit.Unit, targetList);
			}
		}
	}
}
