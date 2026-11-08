using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class DebugSettings : MonoBehaviour
	{
		public bool DamageEnabled = true;

		public bool FleetTargettingEnabled = true;

		public bool FleetlessNpcTargettingEnabled = true;

		public bool NpcTargettingEnabled = true;

		public bool AIGroupAutoObjectiveAssignEnabled = true;

		public bool FactionAIUpdateEnabled = true;

		public bool FactionUpdateEnabled = true;

		public bool CountermeasureDisruptionEnabled = true;

		public bool CountermeasureFindDisruptedMissile = true;

		public bool IgnoreDockingDistance;

		public bool ShieldsEnabled = true;

		public bool DisableDetectorCollliderCreate;

		public bool UnitUpdateEnabled = true;

		public bool UnitTeamColoursEnabled = true;

		public bool FactionIntelPerformScansEnabled = true;

		public bool FactionIntelPerformScansForPlayerFactionOnly;

		public bool FactionIntelPerformScansForPlayerUnitOnly;

		public bool FactionHeatmapEnabled;

		public bool OptimizeNpcPathfindingPaths;

		public bool MissileUpdateEnabled = true;

		public bool EngineThrustEnabled = true;

		public bool ProjectileUpdateEnabled = true;

		public bool NpcUpdateEnabled = true;

		public bool NpcPathfindingEnabled = true;

		public bool NpcPathfindingPlayerUnitOnly;

		public bool ScanningEnabled = true;

		public bool CollisionEnabled = true;

		public bool FleetUpdateEnabled = true;

		public bool NpcMissileAvoidanceEnabled;

		public bool EditorDrawPathfindingMapForNpc;
	}
}
