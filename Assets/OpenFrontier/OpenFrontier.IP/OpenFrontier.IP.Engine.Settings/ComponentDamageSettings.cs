using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class ComponentDamageSettings : MonoBehaviour
	{
		public float AbsorbtionFromHullHealthPercent = 0.6f;

		public float ArmourToDamageAbsorbtionMin = 0.5f;

		public float ArmourToDamageAbsorbtionMax = 1f;

		public float ArmourAbsorbtionPower = 1f;

		public float MinDamageToOneComponent = 0.3f;

		public float MaxDamageToOneComponent = 1.5f;

		public float DamageToOneComponentPower = 1.2f;

		public int MaxIterations = 4;

		public float ArmourRatingMultiplier = 1.2f;

		public float BaseDamageToComponentDamage = 0.2f;

		public float MinComponentDamagePercent = 0.25f;

		public float MaxComponentDamagePercent = 1f;

		public float ComponentDamagePercentPower = 1.5f;
	}
}
