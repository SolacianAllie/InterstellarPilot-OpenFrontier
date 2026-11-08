using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios
{
	public class UniverseWorldSpawnData : MonoBehaviour
	{
		public UniverseGameTypeInfo GameTypeInfo;

		public int SpawnedUnitIndex = -1;

		public float PlayerVirtue = 0.5f;

		public bool IsPlayerSpawn
		{
			get
			{
				UniverseGameTypeSpawnUnit spawnedUnitInfo = GetSpawnedUnitInfo();
				if (spawnedUnitInfo != null)
				{
					return spawnedUnitInfo == GameTypeInfo.PlayerSpawnUnit;
				}
				return false;
			}
		}

		public Unit Spawn(UniverseWorld world, UniverseScenarioData scenarioData)
		{
			FactionSetup component = GetComponent<FactionSetup>();
			if (component != null)
			{
				component.ApplyAttitudes();
			}
			if (SpawnedUnitIndex > -1)
			{
				UniverseGameTypeSpawnUnit spawnedUnitInfo = GetSpawnedUnitInfo();
				if (spawnedUnitInfo != null)
				{
					world.Engine.LocalFaction.Virtue = PlayerVirtue;
					Unit unit = CreatePlayerUnit(world, scenarioData, spawnedUnitInfo);
					SetupNewPlayerUnit(world, scenarioData, spawnedUnitInfo, unit);
					return unit;
				}
				Debug.LogErrorFormat(this, "The unit index {0} is invalid for this game type", SpawnedUnitIndex);
			}
			return null;
		}

		private void SetupNewPlayerUnit(UniverseWorld world, UniverseScenarioData scenarioData, UniverseGameTypeSpawnUnit spawnUnit, Unit newPlayerUnit)
		{
			newPlayerUnit.Faction = world.Engine.LocalPlayer.Faction;
			if (spawnUnit == scenarioData.GameTypeInfo.PlayerSpawnUnit)
			{
				world.Engine.LocalPlayer.Person.CurrentUnit = newPlayerUnit;
			}
			Unit unit = UnityObjectHelper.FindInParentsOrSelf<Unit>(gameObject);
			if (unit != null)
			{
				newPlayerUnit.Sector = unit.Sector;
				if (!newPlayerUnit.Components.TryDockInUnit(unit))
				{
					newPlayerUnit.transform.localPosition = unit.GetSafeUndockSectorPosition(newPlayerUnit);
				}
			}
			else
			{
				Sector componentInParent = GetComponentInParent<Sector>();
				if (componentInParent != null)
				{
					newPlayerUnit.Sector = componentInParent;
					Vector3 position = transform.position;
					position.y = 0f;
					newPlayerUnit.transform.position = position;
					newPlayerUnit.transform.rotation = transform.rotation;
				}
				else
				{
					Debug.LogError("Unable to determine to spawn unit in ", this);
				}
			}
			ApplySpecificGameModesToSingleUnit(world, newPlayerUnit);
		}

		private void ApplySpecificGameModesToSingleUnit(UniverseWorld world, Unit newPlayerUnit)
		{
			if (GameTypeInfo.GameType == UniverseGameType.Scavenger)
			{
				SetupScavenengerGameType(world, newPlayerUnit);
			}
		}

		private static void SetupScavenengerGameType(UniverseWorld world, Unit newPlayerUnit)
		{
			if (world.ScavengerModeTractorTurretClass != null)
			{
				ComponentBay componentBay = newPlayerUnit.Components.Bays.FirstOrDefault((ComponentBay e) => e.BayType.BayType == BayType.TractorBeam);
				if (componentBay != null)
				{
					componentBay.InstallComponent(world.ScavengerModeTractorTurretClass).UserPowered = true;
				}
				else
				{
					Debug.LogWarning("Cannot install a tractor beam on the player's ship because couldn't find the right bay type");
				}
			}
			else
			{
				Debug.LogWarning("Cannot install a tractor beam on the player's ship because world hasn't defined a tractor beam type");
			}
		}

		private Unit CreatePlayerUnit(UniverseWorld world, UniverseScenarioData scenarioData, UniverseGameTypeSpawnUnit spawnUnit)
		{
			Unit unit = WorldHelper.SpawnUnitAndInstallComponents(spawnUnit.UnitClass.UnitPrefab, null);
			unit.Destructable.HealthNormalized = spawnUnit.HealthMultiplier;
			return unit;
		}

		private UniverseGameTypeSpawnUnit GetSpawnedUnitInfo()
		{
			if (SpawnedUnitIndex >= 0 && SpawnedUnitIndex < GameTypeInfo.SpawnUnits.Count)
			{
				return GameTypeInfo.SpawnUnits[SpawnedUnitIndex];
			}
			return null;
		}
	}
}
