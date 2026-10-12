using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class NpcPilotSettings : MonoBehaviour
	{
		public float CombatEfficiency = 0.5f;

		public float RestrictedWeaponPreference = 0.5f;

		public float UseCloakPreference = 0.2f;

		public bool UsesCloak = true;

		public bool AICheatAmmo;

		public bool AllowDitchShip = true;
	}
}
