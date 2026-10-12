using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class FactionSettings : MonoBehaviour
	{
		public float TimeBetweenExpansionSearches = 240f;

		public FactionRelationSettings FactionRelationSettings;

		public FactionRecentDamageSettings FactionRecentDamageSettings;

		public FactionWarDeclarationSettings FactionWarDeclarationSettings;

		public float BanditsHostileVirtueThreshold = 0.35f;

		public FactionShipBuildSettings ShipBuildSettings;

		public FactionAIPatrolSettings AIPatrolSettings;

		public float ProbabilityOfDistressCall = 0.05f;

		public float ProbabilityOfDistressCallGoingToPlayer = 0.5f;

		public int FactionRetireNotificationMinHighestNetWorth = 2000000;

		public float FactionRetireMaxTimeWithNoUnitsBuilt = 600f;

		public double FactionRetireMinFactionAge;

		public float MinAIBuildUnitsInterval = 60f;

		public float MaxAIBuildUnitsInterval = 180f;

		public float TradeOpinionChangePerMillionCr = 0.25f;

		public float OpinionChangeBasedOnRecentDamage = 0.02f;

		public float MaxHostilityCoolDownTime = 12000f;

		public float MinHostilityCoolDownTime = 30f;

		public float HostilityCoolDownTimePower = 4f;

		public float HostilityCoolDownTimeAggressionPower = 4f;

		public float OpinionRolloverScale = 0.25f;

		public float InCombatFriendlyFireDamageMultiplier = 0.5f;

		public float AssistedDamageOpinionEffect = 0.001f;

		public float AIOrderCooldownTime = 30f;

		public float MinOpinionChangeOnMakePeace = 0.15f;

		public float MaxOpinionChangeOnMakePeace = 0.5f;

		public float TaxedTradeValueMultiplierForOpinionChange = 0.25f;
	}
}
