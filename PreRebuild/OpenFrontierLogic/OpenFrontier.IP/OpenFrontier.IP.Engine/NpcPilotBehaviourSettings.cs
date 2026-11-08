using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class NpcPilotBehaviourSettings : MonoBehaviour
	{
		public float MinAttackRunDistance = 100f;

		public NpcPilotBehaviourCombatSettings CombatSettings;

		public float MinWeaponFireCooldownTime = 0.1f;

		public float MaxWeaponFireCooldownTime = 0.5f;

		public float MinUndockComponentUseCooldownTime = 1f;

		public float MaxUndockComponentUseCooldownTime = 3f;

		public float RequirePathfindThresholdDistance = 30f;

		public float PathfindFrequency = 4f;
	}
}
