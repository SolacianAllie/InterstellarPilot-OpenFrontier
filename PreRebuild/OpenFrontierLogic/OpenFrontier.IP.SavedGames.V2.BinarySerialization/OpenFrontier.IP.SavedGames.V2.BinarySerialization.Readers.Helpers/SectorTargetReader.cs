using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.SavedGames.V2.Model;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers.Helpers
{
	public static class SectorTargetReader
	{
		public static ModelSectorTarget Read(BinaryReader reader, Dictionary<int, ModelSector> sectors, Dictionary<int, ModelUnit> units, Dictionary<int, ModelFleet> fleets)
		{
			ModelSectorTarget modelSectorTarget = new ModelSectorTarget
			{
				Position = reader.ReadVec3()
			};
			int key = reader.ReadInt32();
			int key2 = reader.ReadInt32();
			int key3 = reader.ReadInt32();
			modelSectorTarget.Sector = sectors.GetValueOrDefault(key);
			modelSectorTarget.TargetUnit = units.GetValueOrDefault(key2);
			modelSectorTarget.TargetFleet = fleets.GetValueOrDefault(key3);
			modelSectorTarget.HadValidTarget = reader.ReadBoolean();
			return modelSectorTarget;
		}
	}
}
