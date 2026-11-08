using System.Collections.Generic;
using OpenFrontier.IP.Engine.Fleets.FleetFormations;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class FormationSettings : MonoBehaviour
	{
		public float AIFormationMinUnitDist = 120f;

		public float AIFormationUnitRadiusMultiplier = 2.5f;

		public List<FleetFormationStyle> FormationStyles = new List<FleetFormationStyle>();

		public FleetFormationStyle DefaultFormationStyle;

		public float MaxFormationScale = 2f;

		public float MinFormationScale = 0.25f;

		public List<NpcFleetFormationStylePreference> NpcFleetFormationStylePreferences;
	}
}
