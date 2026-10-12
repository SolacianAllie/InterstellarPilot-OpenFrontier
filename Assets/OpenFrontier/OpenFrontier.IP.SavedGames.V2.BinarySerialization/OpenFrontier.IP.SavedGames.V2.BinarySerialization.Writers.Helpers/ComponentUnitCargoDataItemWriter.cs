using System.IO;
using OpenFrontier.IP.SavedGames.V2.Model;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers.Helpers
{
	public static class ComponentUnitCargoDataItemWriter
	{
		public static void Write(BinaryWriter writer, ModelComponentUnitCargoDataItem cargoItem)
		{
			writer.Write((int)cargoItem.CargoClass);
			writer.Write(cargoItem.Quantity);
		}
	}
}
