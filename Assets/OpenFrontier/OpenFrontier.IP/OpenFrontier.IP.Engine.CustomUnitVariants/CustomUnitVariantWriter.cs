using System.IO;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.CustomUnitVariants
{
	public class CustomUnitVariantWriter
	{
		public void Write(CustomUnitVariant customUnitVariant, BinaryWriter writer)
		{
			writer.WriteStringOrEmpty(customUnitVariant.Name);
			writer.WriteStringOrEmpty(customUnitVariant.Description);
			writer.WriteUnitClassId(customUnitVariant.UnitClass);
			writer.Write(customUnitVariant.BayMods.Count);
			for (int i = 0; i < customUnitVariant.BayMods.Count; i++)
			{
				CustomUnitVariantBayMod customUnitVariantBayMod = customUnitVariant.BayMods[i];
				writer.Write(customUnitVariantBayMod.BayId);
				writer.WriteComponentClassId(customUnitVariantBayMod.ComponentClass);
			}
			writer.Write(customUnitVariant.CargoItems.Count);
			for (int j = 0; j < customUnitVariant.CargoItems.Count; j++)
			{
				CargoBayItem cargoBayItem = customUnitVariant.CargoItems[j];
				writer.WriteCargoClassId(cargoBayItem.CargoClass);
				writer.Write(cargoBayItem.Quantity);
			}
		}
	}
}
