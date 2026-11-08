using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class RenameSectorsSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Renaming sectors", this, 1);
			}
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				NameSectorForEngine(sector);
			}
		}

		private static void NameSectorForEngine(Sector sector)
		{
			string[] existingNames = EngineASX.Instance.Sectors.Select((Sector e) => e.Name).ToArray();
			sector.Name = NameSector(existingNames);
		}

		private static string NameSector(IEnumerable<string> existingNames)
		{
			string result = "";
			if (!GameController.Instance.SectorNamer.TryGenerateName(existingNames, 20, GameController.Instance.SectorNamer.PrefixProbability, GameController.Instance.SectorNamer.NumberPosfixProbability, EngineASX.Instance.World.SeederRandom, out result))
			{
				Debug.LogError("Could not generate name for new sector");
				result = "Unknown " + Random.Range(101, 873);
			}
			return result;
		}
	}
}
