using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios
{
	public class FactionSpawnerSettings : MonoBehaviour
	{
		public UnitClass RefineryUnitClass;

		public float TimeBetweenFactionSpawns = 600f;

		public float TimeBetweenMinorFactionSpawns = 60f;

		public FactionSpawnerSpawnType[] FactionTypes;
	}
}
