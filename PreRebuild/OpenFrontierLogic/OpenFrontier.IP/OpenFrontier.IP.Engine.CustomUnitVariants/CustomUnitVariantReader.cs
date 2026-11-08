using System.IO;
using System.Linq;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.Engine.CustomUnitVariants
{
	public class CustomUnitVariantReader
	{
		public CustomUnitVariant Read(BinaryReader reader)
		{
			CustomUnitVariant customUnitVariant = new CustomUnitVariant();
			customUnitVariant.Name = reader.ReadString();
			customUnitVariant.Description = reader.ReadString();
			customUnitVariant.UnitClass = reader.ReadUnitClass(EngineASX.Instance);
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int bayId = reader.ReadInt32();
				int num2 = reader.ReadInt32();
				ComponentClass componentClassById = EngineASX.Instance.GetComponentClassById(num2);
				if ((num2 < 0 || !(componentClassById == null)) && !customUnitVariant.BayMods.Any((CustomUnitVariantBayMod e) => e.BayId == bayId))
				{
					customUnitVariant.BayMods.Add(new CustomUnitVariantBayMod
					{
						BayId = bayId,
						ComponentClass = componentClassById
					});
				}
			}
			int num3 = reader.ReadInt32();
			for (int num4 = 0; num4 < num3; num4++)
			{
				CargoClass cargoClass = reader.ReadCargoClassFromId(EngineASX.Instance);
				int quantity = reader.ReadInt32();
				if (cargoClass != null && !customUnitVariant.CargoItems.Any((CargoBayItem e) => e.CargoClass == cargoClass))
				{
					customUnitVariant.CargoItems.Add(new CargoBayItem
					{
						CargoClass = cargoClass,
						Quantity = quantity
					});
				}
			}
			return customUnitVariant;
		}
	}
}
