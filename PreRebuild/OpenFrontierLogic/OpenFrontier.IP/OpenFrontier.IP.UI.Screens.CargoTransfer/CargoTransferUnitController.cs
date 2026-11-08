using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Screens.UnitCargo;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoTransfer
{
	public class CargoTransferUnitController : MonoBehaviour
	{
		private Unit unit;

		public Text ShipNameText;

		public ShipCargoItemList ItemList;

		public int TransferMultiplier = -1;

		public AdvancedCargoUsageSlider AdvancedCargoUsageSlider;

		public Text TransferText;

		public CargoBayComponent CargoBayComponent
		{
			get
			{
				if (unit != null)
				{
					return unit.CargoBayComponent;
				}
				return null;
			}
		}

		public Unit Unit
		{
			get
			{
				return unit;
			}
			set
			{
				if (unit != value)
				{
					unit = value;
					AdvancedCargoUsageSlider.SetUnit(unit);
				}
			}
		}

		public bool CanDoTransfer(CargoTransferData transferData)
		{
			if (transferData.Item != null && transferData.TransferUnits != 0)
			{
				GetTransferLoad(transferData);
				if (CargoBayComponent != null)
				{
					return CargoBayComponent.CanChangeCargo(transferData.Item.CargoClass, GetTransferUnits(transferData));
				}
				return false;
			}
			return false;
		}

		private int GetTransferUnits(CargoTransferData transferData)
		{
			return transferData.TransferUnits * TransferMultiplier;
		}

		private float GetTransferLoad(CargoTransferData transferData)
		{
			return (float)(transferData.TransferUnits * TransferMultiplier) * transferData.Item.CargoClass.Volume;
		}

		public void DoTransfer(CargoTransferData transferData)
		{
			if (CargoBayComponent != null)
			{
				CargoBayComponent.AddToCargoIfFits(transferData.Item.CargoClass, GetTransferUnits(transferData));
			}
		}

		private float GetCargoUsage()
		{
			if (CargoBayComponent != null)
			{
				return CargoBayComponent.Usage;
			}
			return 0f;
		}

		private string GetTransferText(int transferAmount)
		{
			if (transferAmount > 0)
			{
				return $"+{transferAmount}";
			}
			if (transferAmount < 0)
			{
				return transferAmount.ToString();
			}
			return "-";
		}

		public void Refresh(CargoTransferData transferData = null)
		{
			if (unit != null)
			{
				ShipNameText.text = unit.GetFriendlyName();
				if (CargoBayComponent != null)
				{
					RefreshTransferDisplay(transferData);
					ItemList.SetItems(UIHelper.GetCargoItems(CargoBayComponent));
				}
				else
				{
					ItemList.ClearActiveItems();
				}
			}
		}

		public void RefreshTransferDisplay(CargoTransferData transferData)
		{
			float num = GetCargoUsage();
			if (transferData != null && transferData.Item != null)
			{
				num += GetTransferLoad(transferData);
				if (TransferText != null)
				{
					TransferText.text = GetTransferText(transferData.TransferUnits * TransferMultiplier);
				}
			}
			else
			{
				TransferText.text = "-";
			}
			AdvancedCargoUsageSlider.ExpectedCargoUsage = num;
			AdvancedCargoUsageSlider.Refresh();
		}
	}
}
