using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.MultiCargoPicker
{
	public class MultiCargoPickerItem
	{
		public int? QuantityAvailable { get; set; }

		public CargoClass CargoClass { get; set; }

		public string AvailableText { get; set; } = "available";
	}
}
