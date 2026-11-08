using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios
{
	public class TurretControllerSpawner : MonoBehaviour
	{
		public void SpawnTurretNpcs(EngineASX engine)
		{
			EngineASX.Instance.EnumerateUnitsWithPredicate((Unit unit) =>
			{
				AutomateTurrets(unit);
			}, (Unit unit) => ShouldAutomateTurrets(unit) && unit.NpcPilot == null);
		}

		public static bool ShouldAutomateTurrets(Unit unit)
		{
			if (unit != null && unit.IsStationOrShip() && !unit.UnitClass.IsPilottable)
			{
				return unit.Faction != null;
			}
			return false;
		}

		public static void SetupAutoTurretModule(Unit unit)
		{
			unit.Components.InitAutoTurretModuleIfNull();
			unit.Components.AutoTurretModule.FireMode = AutoTurretFireMode.AnyTarget;
		}

		public void AutomateTurrets(Unit unit)
		{
			SetupAutoTurretModule(unit);
			unit.Components.SetConventionalTurretsAutoFire(autoFire: true);
		}
	}
}
