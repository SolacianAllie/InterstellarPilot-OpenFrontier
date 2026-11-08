using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine.MissionGenerators
{
	public class DestroyShipsMissionGenerator : MissionGenerator
	{
		public float MinGateDistanceMultiplier = 1.75f;

		public float MaxGateDistanceMultiplier = 2f;

		private static List<Faction> potentialEnemyFactions = new List<Faction>();

		[FormerlySerializedAs("GroupPrefab")]
		public Fleet FleetPrefab;

		public int MaxUnitCount = 5;

		public DestroyGroupMissionSpec MissionSpecPrefab;

		public Person PilotPrefab;

		public override MissionManagerMissionType MissionType => MissionManagerMissionType.DestroyGroup;

		protected override MissionSpec generateMissionSpec(Unit unit, Faction faction, EngineASX engine)
		{
			UnitClass[] unitClasses = null;
			float enemyShipPower = 0f;
			Faction enemyFaction = GetEnemyFaction(unit, faction, engine, out unitClasses, out enemyShipPower);
			if (enemyFaction != null)
			{
				DestroyGroupMissionSpec destroyGroupMissionSpec = UnityObjectHelper.InstantiateAndGetComponent(MissionSpecPrefab, unit.transform);
				destroyGroupMissionSpec.TargetGroupParams = new FleetSpawnParams();
				destroyGroupMissionSpec.TargetGroupParams.Faction = enemyFaction;
				destroyGroupMissionSpec.TargetGroupParams.FleetPrefab = FleetPrefab;
				destroyGroupMissionSpec.TargetGroupParams.TargetSector = unit.Sector;
				destroyGroupMissionSpec.TargetGroupParams.TargetSectorPosition = GetTargetSectorPosition(destroyGroupMissionSpec, destroyGroupMissionSpec.TargetGroupParams.TargetSector);
				GenerateTargetShips(destroyGroupMissionSpec.TargetGroupParams, engine, enemyFaction, enemyShipPower, unitClasses);
				if (destroyGroupMissionSpec.TargetGroupParams.Ships.Count > 0)
				{
					return destroyGroupMissionSpec;
				}
				Debug.Log($"Cannot generate destroy ships mission as unable to find target ships for enemy faction \"{enemyFaction}\"", this);
			}
			else
			{
				Debug.Log($"Cannot generate destroy ships mission as faction \"{faction}\" doesn't seem to have any relevant enemies", this);
			}
			return null;
		}

		private Vector3 GetTargetSectorPosition(DestroyGroupMissionSpec missionSpec, Sector targetSector)
		{
			Vector3 checkSectorPosition = Geometry.RandomXZUnitVector() * targetSector.GetActualGateDistance() * Random.Range(MinGateDistanceMultiplier, MaxGateDistanceMultiplier);
			return PhysicsNonOverlappingPositionFinder.FindSectorPosition(targetSector, checkSectorPosition, 1000f, GameController.Instance.NonOVerlappingUnitsMask);
		}

		private bool CanGenerateEnemiesForFaction(Faction faction, Faction enemyFaction)
		{
			if (enemyFaction.IsFreelancer)
			{
				return false;
			}
			if (faction != enemyFaction && enemyFaction.GetCountOfUnitType(UnitType.Station) > 0 && !enemyFaction.IsPlayerFaction && faction.IsHostileTo(enemyFaction))
			{
				return true;
			}
			return false;
		}

		private UnitClass[] GetTargetShipsForFaction(EngineASX engine, Faction enemyFaction, out float enemyShipsPower)
		{
			enemyShipsPower = GetRandomEnemyShipsPower(engine);
			float maxEnemyShipNetWorth = enemyShipsPower * engine.GameSettings.MissionSpecSettings.DestroyGroupMissionSpecSettings.EnemyShipTypeMultiplier;
			return engine.UnitClasses.Where((UnitClass e) => e.IsUsable && e.SeedInSandbox && e.UnitType == UnitType.Ship && (float)e.SaleCost < maxEnemyShipNetWorth && (e.Purposes & UnitPurpose.Warship) != 0).ToArray();
		}

		private float GetRandomEnemyShipsPower(EngineASX engine)
		{
			long num = 0L;
			if (engine.LocalPlayer.Faction != null)
			{
				num = engine.LocalPlayer.Faction.GetCachedNetWorth();
			}
			DestroyGroupMissionSpecSettings destroyGroupMissionSpecSettings = engine.GameSettings.MissionSpecSettings.DestroyGroupMissionSpecSettings;
			return Mathf.Max(destroyGroupMissionSpecSettings.MinPlayerNetWorthBasis, num) * Random.Range(destroyGroupMissionSpecSettings.MinEnemyNetWorthMultiplier, destroyGroupMissionSpecSettings.MaxEnemyNetWorthMultiplier);
		}

		private void GenerateTargetShips(FleetSpawnParams spawnParams, EngineASX engine, Faction enemyFaction, float enemyShipsPower, UnitClass[] possibleUnitClasses)
		{
			if (possibleUnitClasses.Length != 0)
			{
				while (enemyShipsPower > 0f && spawnParams.Ships.Count < MaxUnitCount)
				{
					FleetSpawnShipParams fleetSpawnShipParams = new FleetSpawnShipParams();
					fleetSpawnShipParams.UnitClass = possibleUnitClasses.GetRandom();
					fleetSpawnShipParams.PilotPrefab = PilotPrefab;
					spawnParams.Ships.Add(fleetSpawnShipParams);
					enemyShipsPower -= (float)fleetSpawnShipParams.UnitClass.SaleCost;
				}
			}
		}

		private void PopulatePotentialEnemyFactions(EngineASX engine, Faction faction)
		{
			potentialEnemyFactions.Clear();
			foreach (Faction faction2 in engine.Factions)
			{
				if (CanGenerateEnemiesForFaction(faction, faction2))
				{
					potentialEnemyFactions.Add(faction2);
				}
			}
		}

		private Faction GetEnemyFaction(Unit unit, Faction faction, EngineASX engine, out UnitClass[] unitClasses, out float enemyShipPower)
		{
			unitClasses = null;
			enemyShipPower = 0f;
			PopulatePotentialEnemyFactions(engine, faction);
			Faction[] array = new Faction[potentialEnemyFactions.Count];
			for (int i = 0; i < array.Length; i++)
			{
				int index = Random.Range(0, potentialEnemyFactions.Count);
				array[i] = potentialEnemyFactions[index];
				potentialEnemyFactions.RemoveAt(index);
			}
			Faction[] array2 = array;
			foreach (Faction faction2 in array2)
			{
				unitClasses = GetTargetShipsForFaction(engine, faction2, out enemyShipPower);
				if (unitClasses != null && unitClasses.Length != 0)
				{
					return faction2;
				}
			}
			return null;
		}
	}
}
