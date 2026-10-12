using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class GameplaySettings : MonoBehaviour
	{
		public float CombatEfficiencyChangePerKill = 0.05f;

		public float VirtueChangeRateMultiplier = 0.25f;

		public float ProbabilityOfFactionAIUpgradingStation = 0.1f;

		public float CaptureProbabilityMultiplier = 0.2f;

		public float WeaponsFireSensorRangeChange = 1500f;

		public bool ShieldsUpWhenCloaked = true;

		public bool DecloakWhenReceiveDamage = true;

		public bool DecloakUnitsWhenCollisionDamage = true;

		public bool CanDetectCloakedUnits = true;

		public bool CanTargetCloakedUnits = true;

		public bool NotifyPlayerTargettedByCloakedShip = true;

		public float NotifyPlayerTargettedMaxDistanceToTriggerInCombat = 2500f;

		public float CargoLifetime = 1800f;

		public float CargoOreLifetime = 600f;

		public double CargoPlayerEjectedLifetime = 600.0;

		public float CargoBaseHealth = 60f;

		public float CargoHealthPerVolumeUnit = 20f;

		public float CargoMaxHealth = 1000f;

		public float GlobalDamageMultiplier = 0.5f;

		public float GlobalHullDamageMultiplier = 0.8f;

		public float ProbabilityOfBanditsRandomAttack = 0.05f;

		public float ProbabilityOfEmpireRandomAttack = 0.8f;

		public float MinAsteroidHealth = 200f;

		public float MaxAsteroidHealth = 1200f;

		public float WormholeEntryScanCooldownTime = 2f;

		public float SeededStaticIntelMultiplier = 1f;

		public float MinTimeBeforeFactionSectorExpansion = 400f;

		public float MinDamageBeforeNpcShipNeedsRepair = 0.2f;

		public float NpcFactionSmallShipRatio = 2f;

		public float MinTimeBeforeFactionShipBuild = 180f;

		public float MaxTimeBeforeFactionShipBuild = 360f;

		public float LaserInaccuracyMultiplier;

		public float PowerGeneratorRateMultiplier = 1.25f;

		public double TimeBeforeUnitRecapture = 120.0;

		public float CapturedUnitAutoTurretCooldownTime = 10f;

		public float LaserOvershootMultiplier = 1.5f;

		public float ProbabilityCountermeasuresDisruptMissileInactiveSector = 1f;

		public float ProbabilityOfMaleNpc = 0.65f;

		public float DismantledStationRefundPercent = 1f;

		public float DismantledShipRefundPercent = 0.5f;

		public bool AllowBanditOnBanditAttack;

		public float FleetStrategyAssignment_ScoreFuzziness = 0.3f;

		public float FleetStrategyAssignment_ShipEffectivenessWeight = 0.8f;

		public float FleetStrategyAssignment_FactionStrategyWeight = 0.2f;

		public bool ClearPlayerTargetWhenInvalidInstantly;

		public bool ClearNpcTargetWhenInvalidInstantly;

		public float ExplosionComponentDamageMultiplier = 0.25f;
	}
}
