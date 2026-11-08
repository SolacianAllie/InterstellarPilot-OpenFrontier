using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveExploreOrder : ActiveFleetOrder
	{
		private static List<Wormhole> wormholeCache = new List<Wormhole>();

		public ExploreOrder ExploreObjective;

		private Vector3 currentTargetSectorPosition = Vector3.zero;

		private Sector currentTargetSector;

		private Wormhole currentTargetWormhole;

		private float nextFindTargetTime;

		public override float BaseTargetInterceptionScoreMultiplier => 0.1f;

		public Vector3 CurrentTargetSectorPosition
		{
			get
			{
				return currentTargetSectorPosition;
			}
			set
			{
				currentTargetSectorPosition = value;
			}
		}

		public Sector CurrentTargetSector
		{
			get
			{
				return currentTargetSector;
			}
			set
			{
				currentTargetSector = value;
			}
		}

		public Wormhole CurrentTargetWormhole
		{
			get
			{
				return currentTargetWormhole;
			}
			set
			{
				currentTargetWormhole = value;
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			ClearExplorationTarget();
			FindNewExplorationTarget();
		}

		private void ClearExplorationTarget()
		{
			currentTargetSector = null;
			currentTargetWormhole = null;
		}

		public override void OnFleetEnteredWormhole(Wormhole wormholeBeingEntered)
		{
			base.OnFleetEnteredWormhole(wormholeBeingEntered);
			if (wormholeBeingEntered != null && wormholeBeingEntered == currentTargetWormhole)
			{
				ClearExplorationTarget();
			}
		}

		protected override void tick(float elapsedTime)
		{
			FindTargetPeriodicallyIfNone();
		}

		private void FindTargetPeriodicallyIfNone()
		{
			if (currentTargetSector == null && Time.time > nextFindTargetTime)
			{
				FindNewExplorationTarget();
				nextFindTargetTime = Time.time + 10f;
			}
		}

		private void FindNewExplorationTarget()
		{
			Wormhole firstUnexploredWormholeWithTargetInCurrentScene = GetFirstUnexploredWormholeWithTargetInCurrentScene();
			if (firstUnexploredWormholeWithTargetInCurrentScene != null)
			{
				SetTargetToWormhole(firstUnexploredWormholeWithTargetInCurrentScene);
			}
			else if (Random.value < 0.25f)
			{
				Wormhole anyDiscoveredWormholeWithTargetInCurrentScene = GetAnyDiscoveredWormholeWithTargetInCurrentScene();
				if (anyDiscoveredWormholeWithTargetInCurrentScene != null)
				{
					SetTargetToWormhole(anyDiscoveredWormholeWithTargetInCurrentScene);
				}
				else
				{
					SetTargetToRandomPositionInCurrentSector();
				}
			}
			else
			{
				SetTargetToRandomPositionInCurrentSector();
			}
		}

		private void SetTargetToRandomPositionInCurrentSector()
		{
			currentTargetSector = fleet.Sector;
			currentTargetSectorPosition = currentTargetSector.GetRandomSafeDeploymentSectorPosition(0f, 0.9f, 50f, GameController.Instance.StaticNonOverlappingMask);
		}

		private void SetTargetToWormhole(Wormhole unexploredWormhole)
		{
			currentTargetSector = null;
			currentTargetWormhole = unexploredWormhole;
		}

		private Wormhole GetAnyDiscoveredWormholeWithTargetInCurrentScene()
		{
			wormholeCache.Clear();
			FactionIntel intel = fleet.Faction.Intel;
			List<Unit> unitsByType = fleet.Sector.GetUnitsByType(UnitType.Wormhole);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (intel.IsUnitDiscoveredOrOwned(item))
					{
						Wormhole wormholeComponent = item.WormholeComponent;
						if (wormholeComponent.ActualTargetSector != null)
						{
							wormholeCache.Add(wormholeComponent);
						}
					}
				}
			}
			return wormholeCache.GetRandom();
		}

		private Wormhole GetFirstUnexploredWormholeWithTargetInCurrentScene()
		{
			wormholeCache.Clear();
			FactionIntel intel = fleet.Faction.Intel;
			List<Unit> unitsByType = fleet.Sector.GetUnitsByType(UnitType.Wormhole);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (intel.IsUnitDiscoveredOrOwned(item))
					{
						Wormhole wormholeComponent = item.WormholeComponent;
						Sector actualTargetSector = wormholeComponent.ActualTargetSector;
						if (actualTargetSector != null && !intel.IsSectorDiscovered(actualTargetSector))
						{
							wormholeCache.Add(wormholeComponent);
						}
					}
				}
			}
			return wormholeCache.GetRandom();
		}

		protected override void resetTargetPosition()
		{
			if (currentTargetSector != null)
			{
				fleet.SetTargetToSectorPosition(this, currentTargetSector, currentTargetSectorPosition);
			}
			else if (currentTargetWormhole != null)
			{
				fleet.SetTargetToWormhole(this, currentTargetWormhole);
			}
		}

		public override void OnFleetFailedToFindPathToTarget()
		{
			ClearExplorationTarget();
		}
	}
}
