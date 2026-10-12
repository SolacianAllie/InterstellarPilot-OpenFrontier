using OpenFrontier.IP.Engine.Settings.Performance;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class PerformanceSettings : MonoBehaviour
	{
		public float MinNpcCargoCollectorCheckInterval = 1f;

		public float MaxNpcCargoCollectorCheckInterval = 6f;

		public ThrusterDrawDistanceSettings ThrusterDrawDistanceSettings;

		public float NpcTargetScanActiveFrequency = 2f;

		public float NpcTargetScanInactiveFrequency = 6f;

		public float NpcTargetScanProcessedUnitsPerSecond = 20f;

		public float PassengerManagerTimeBetweenReplenish = 0.1f;

		public float AITradeSearchSearchesPerSecond = 100f;

		public float AIBountyHunterSearchSearchesPerSecond = 100f;

		public float AITradeSearchSearchesPerSecondPlayer = 300f;

		public float AICargoSearchSearchesPerSecond = 60f;

		public float AICargoSearchSearchesPerSecondPlayer = 120f;

		public float PreferredTimeBetweenUnitUpdates = 3f;

		public float PreferredTimeBetweenGroupUpdates = 3f;

		public bool CreateImpactParticles = true;

		public bool ShowDamageEffects = true;

		public bool ShowShieldHitEffects = true;

		public float UnitRollMaxDistance = 400f;

		public float AIActivePilotMineCalcInterval = 0.2f;

		public float MaxShieldHitDist = 200f;

		public float UnitTrailsDrawLowerDist = 75f;

		public float UnitTrailsDrawUpperDist = 100f;

		public float FactionAIUpdateInterval = 3f;

		public int FactionIntelMaxScansProcessedPerSecond = 120;

		public float FactionIntelScanInterval = 2f;

		public int MaxHeatmapCellsUpdatedPerSecond = 10;

		public float HeatmapDissipationUpdateFrequency = 10f;

		public float EmpireAIExpansionSearchItemsPerSecond = 4f;

		public float PassengerGroupDestinationSearchItemsPerSecond = 8f;

		public float FactionTraderTargetCacheMaxAge = 120f;

		public bool DisableCollidersInActiveSector = true;

		public float DisableCollidersInActiveSectorLowerDistance = 2000f;

		public float DisableCollidersInActiveSectorUpperDistance = 2200f;

		public float NpcPilotTargetScoresPerSecond = 20f;

		public float AutoTurretTargetScoresPerSecond = 20f;

		public int AIMineSearchSearchesPerSecond = 30;

		public float LightingFxMaxCameraDistanceMultiplier = 5f;

		public float EjectedCargoPhysicsMaxRangeFromCamera = 1500f;

		public float CountermeasureParticlesMaxDistance = 1000f;
	}
}
