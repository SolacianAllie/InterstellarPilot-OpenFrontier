using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class FleetSettings : MonoBehaviour
	{
		public bool NotifyWhenOrderComplete;

		public bool NotifyWhenScannedHostile;

		public bool NotifyWhenAbandonedUnitFound;

		public bool NotifyWhenAbandonedCargoFound;

		public bool AllowCombatInterception = true;

		public bool AllowAttack = true;

		public bool AllowCombatCloak = true;

		public float Aggression = 0.5f;

		public const float DefaultTargetInterceptionLowerDistance = 2000f;

		public const float DefaultTargetInterceptionUpperDistance = 3500f;

		public float TargetInterceptionLowerDistance = 2000f;

		public float TargetInterceptionUpperDistance = 3500f;

		public bool PreferCloak;

		public DockedPreference PreferToDock;

		public int MaxJumpDistance = 99;

		public bool DestroyWhenNoPilots = true;

		public float FormationTightness = 0.9f;

		public FleetCargoCollectionPreference CargoCollectionPreference = FleetCargoCollectionPreference.CompatibleEquipment;
	}
}
