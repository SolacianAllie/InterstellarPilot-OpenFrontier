using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class RandomizedFleetSeeder : MonoBehaviour
	{
		public RandomizedFleetSeederSettings RandomFleetSeederSettings;

		public List<FactionType> SpecificFactionTypes = new List<FactionType>();

		public List<FactionType> ExcludeSpecificFactionTypes = new List<FactionType>();

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding fleet randomness...", this, 1);
			}
			RandomFleetSeederSettings = world.Seeder.Settings.RandomizedFleetSeederSettings;
			foreach (Faction faction in world.Engine.Factions)
			{
				if (faction.FactionAI != null && !faction.IsPlayerFaction && (!SpecificFactionTypes.Any() || SpecificFactionTypes.Contains(faction.FactionType)) && (!ExcludeSpecificFactionTypes.Any() || !ExcludeSpecificFactionTypes.Contains(faction.FactionType)))
				{
					RandomizeFactionFleets(world, faction, world.FleetsBeforeSeed);
				}
			}
		}

		private void RandomizeFactionFleets(WorldBase world, Faction faction, HashSet<int> fleetIdsToIgnore)
		{
			float probabilityOfRandomGroupStartPositionMultiplier = faction.FactionAI.FactionTypeInfo.ProbabilityOfRandomGroupStartPositionMultiplier;
			foreach (Fleet fleet in faction.Fleets)
			{
				if (!fleetIdsToIgnore.Contains(fleet.UniqueId))
				{
					fleet.AssignNextOrderIfNoCurrentOrder();
					if (fleet.ActiveOrder != null && fleet.ActiveOrder is ActiveBountyHunterOrder activeBountyHunterOrder)
					{
						activeBountyHunterOrder.SearchForTargetImmediate();
					}
					if (Random.value <= RandomFleetSeederSettings.ProbabilityOfRandomGroupPositionWhenSeeding * probabilityOfRandomGroupStartPositionMultiplier)
					{
						RandomizeFleet(fleet);
					}
					if (fleet.Settings.PreferCloak && fleet.CanAllUnitsCloak())
					{
						fleet.CloakAllUnitsImmediate();
					}
				}
			}
		}

		public static void RandomizeFleet(Fleet fleet)
		{
			if (fleet.ActiveOrder != null && fleet.ActiveOrder.FleetOrder is PatrolOrderBase)
			{
				PatrolOrderBase patrolOrderBase = (PatrolOrderBase)fleet.ActiveOrder.FleetOrder;
				if (patrolOrderBase.NodeCount > 1)
				{
					IPatrolPathNode nodeAtIndex = patrolOrderBase.GetNodeAtIndex(0);
					IPatrolPathNode nodeAtIndex2 = patrolOrderBase.GetNodeAtIndex(patrolOrderBase.NodeCount - 1);
					TryLerpFleet(fleet, nodeAtIndex.Sector, nodeAtIndex.SectorPosition, nodeAtIndex2.Sector, nodeAtIndex2.SectorPosition);
				}
			}
			else if (fleet.NavTarget != null && fleet.NavTarget.IsActive)
			{
				if (fleet.FindPathToTargetWrapper())
				{
					TryLerpFleet(fleet, fleet.NavTarget.GetTargetSector(), fleet.NavTarget.GetTargetSectorPosition());
				}
			}
			else if (fleet.HomeBaseUnit != null && fleet.CanDockAllAtUnit(fleet.HomeBaseUnit))
			{
				fleet.DockAll(fleet.HomeBaseUnit);
			}
		}

		private static bool TryLerpFleet(Fleet fleet, Sector targetSector, Vector3 targetSectorPosition)
		{
			return TryLerpFleet(fleet, fleet.Sector, fleet.SectorPosition, targetSector, targetSectorPosition);
		}

		private static bool TryLerpFleet(Fleet fleet, Sector startSector, Vector3 startSectorPosition, Sector targetSector, Vector3 targetSectorPosition)
		{
			Sector sector = null;
			Vector3 resultSectorPosition = Vector3.zero;
			if (WorldHelper.Lerp(startSector, startSectorPosition, targetSector, targetSectorPosition, fleet.Faction, ignoreHeatmap: false, 0.05f + Random.value * 0.9f, out sector, out resultSectorPosition))
			{
				fleet.Sector = sector;
				fleet.transform.localPosition = resultSectorPosition;
				foreach (NpcPilot npcPilot in fleet.NpcPilots)
				{
					npcPilot.CurrentUnit.Components.DockedInHangarBay = null;
					npcPilot.CurrentUnit.Sector = fleet.Sector;
					npcPilot.CurrentUnit.transform.localPosition = fleet.GetPilotFleetFormationSectorPosition(npcPilot);
				}
				fleet.FindPathToTargetWrapper();
				return true;
			}
			Debug.LogError($"Could not lerp fleet {fleet} to target", fleet);
			return false;
		}
	}
}
