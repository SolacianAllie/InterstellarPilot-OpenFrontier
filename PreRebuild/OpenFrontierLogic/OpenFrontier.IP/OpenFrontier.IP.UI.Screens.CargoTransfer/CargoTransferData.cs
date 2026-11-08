using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.CargoTransfer
{
	public class CargoTransferData
	{
		public int TransferUnits { get; set; }

		public CargoBayItem Item { get; set; }

		public float GetTransferVolume()
		{
			return Item.CargoClass.Volume * (float)TransferUnits;
		}
	}
}
