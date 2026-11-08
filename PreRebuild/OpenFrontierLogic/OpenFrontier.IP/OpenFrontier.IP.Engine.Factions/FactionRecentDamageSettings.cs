using OpenFrontier.IP.Common;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionRecentDamageSettings : MonoBehaviour
	{
		public float RecentAttacksThreshold = 10f;

		public float RecentAttacksOpinionAddition = 10f;

		public float RecentAttacksAgressionFactor = 12f;

		public float RecentAttacksDamageScale = 0.04f;

		public float OpinionChangeBasedOnRecentDamage = 0.001f;

		public float RecentAttacksDissipationRatePerSecond = 0.05f;

		public float RecentAttacksMaxValue = 800f;

		public float RecentAttacksDissipationChkRate = 4f;

		public float RecentAttacksTimeBeforeDissipation = 120f;

		public float RecentAttacksExistingDamageFactor = 0.01f;

		public float MinDamageReceivedPerDestroyedUnit = 16f;

		public float MaxDamageReceivedPerDestroyedUnit = 1000f;

		public float DestroyedUnitReferenceCreditsValue = 1000000f;

		public float DamageReceivedPerKilledPerson = 100f;

		public float StolenCargoDamageConversion = 0.085f;

		public float StolenCargoBaseDamage = 40f;

		public DamageDirectType StolenCargoDamageType;

		public bool ShipScanDamageEnabled = true;

		public float ScannedShipMaxDamage = 50f;

		public float ScannedShipDamageMinAggression = 0.1f;

		public float ScannedShipDamageMaxOpinion = 0.6f;

		public float IndirectDamageMultiplier = 0.25f;

		public float NpcFactionIndirectDamageMultiplier = 0.01f;

		public float MinDamagePerAttack = 10f;
	}
}
