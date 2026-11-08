using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Scenarios;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	public class CreateEmpireFactionsSeederSettings : MonoBehaviour
	{
		public bool GroupEmpireFactions = true;

		public float MinExpansion;

		public float MaxExpansion = 0.05f;

		public float ExpansionPower = 2f;

		public const float PowerToRequisitionPointConversion = 4000f;

		public float MinNumFactionsPerSector = 0.1f;

		public float MaxNumFactionsPerSector = 0.5f;

		public float NumFactionsPerSectorPower = 4f;

		public FactionSpawnerSpawnType EmpireSpawnType;

		public int MinSectorDistanceBetweenFactions = 3;

		public int MinFactions = 3;

		public int MaxFactions = 10;

		public float MaxTotalEmpireControlledSectorsLower = 0.2f;

		public float MaxTotalEmpireControlledSectorsUpper = 0.6f;

		public float MaxTotalEmpireControlledSectorsPower = 2f;

		public bool PrioritizeSpecialFactions;

		public List<Faction> SpecialFactionPrefabs = new List<Faction>();
	}
}
