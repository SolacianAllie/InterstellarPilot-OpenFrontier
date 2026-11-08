using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.CargoTransfer
{
	public class CargoTransferButton : MonoBehaviour
	{
		public int TransferQuantity = 1;

		public CargoTransferScreen TransferScreen;

		public void OnClick()
		{
			TransferScreen.AddToTransfer(TransferQuantity);
		}
	}
}
