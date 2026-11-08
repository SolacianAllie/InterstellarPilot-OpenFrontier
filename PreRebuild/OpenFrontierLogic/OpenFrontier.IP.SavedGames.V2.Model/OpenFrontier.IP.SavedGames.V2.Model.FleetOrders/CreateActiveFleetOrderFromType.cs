using System;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders
{
	public static class CreateActiveFleetOrderFromType
	{
		public static ModelActiveFleetOrder Create(FleetOrderType orderType)
		{
			switch (orderType)
			{
			case FleetOrderType.AttackTarget:
				return new ModelActiveAttackTargetOrder();
			case FleetOrderType.AutonomousTrade:
				return new ModelActiveUniverseTradeOrder();
			case FleetOrderType.CollectCargo:
				return new ModelActiveCollectCargoOrder();
			case FleetOrderType.Dock:
				return new ModelActiveDockOrder();
			case FleetOrderType.ManualTrade:
				return new ModelActiveManualTradeOrder();
			case FleetOrderType.Mine:
				return new ModelActiveMineOrder();
			case FleetOrderType.MoveTo:
				return new ModelActiveMoveToOrder();
			case FleetOrderType.Patrol:
			case FleetOrderType.PatrolPath:
				return new ModelActivePatrolOrder();
			case FleetOrderType.RTB:
				return new ModelActiveReturnToBaseOrder();
			case FleetOrderType.Scavenge:
				return new ModelActiveScavengeOrder();
			case FleetOrderType.SellCargo:
				return new ModelActiveSellCargoOrder();
			case FleetOrderType.Trade:
				return new ModelActiveTradeOrder();
			case FleetOrderType.Wait:
				return new ModelActiveWaitOrder();
			case FleetOrderType.AttackGroup:
				return new ModelActiveAttackFleetOrder();
			case FleetOrderType.JoinFleet:
				return new ModelActiveJoinFleetOrder();
			case FleetOrderType.DisposeCargo:
				return new ModelActiveDisposeCargoOrder();
			case FleetOrderType.Protect:
				return new ModelActiveProtectOrder();
			case FleetOrderType.AutonomousBountyHunterObjective:
				return new ModelActiveUniverseBountyHunterOrder();
			case FleetOrderType.AutonomousRoamLocationsObjective:
				return new ModelActiveUniverseRoamOrder();
			case FleetOrderType.ManualRepair:
			case FleetOrderType.RepairAtNearest:
				return new ModelActiveRepairFleetOrder();
			case FleetOrderType.ManualRearm:
			case FleetOrderType.RearmAtNearest:
				return new ModelActiveRearmFleetOrder();
			case FleetOrderType.AutonomousTransportPassengers:
				return new ModelActiveUniversePassengerTransportOrder();
			case FleetOrderType.Explore:
				return new ModelActiveExploreOrder();
			case FleetOrderType.MoveToNearestFriendlyStation:
				return new ModelActiveMoveToNearestFriendlyStationOrder();
			case FleetOrderType.EnterWormhole:
				return new ModelActiveEnterWormholeOrder();
			case FleetOrderType.ExploreSector:
				return new ModelActiveExploreSectorOrder();
			case FleetOrderType.Undock:
				return new ModelActiveUndockOrder();
			case FleetOrderType.MoveToSector:
				return new ModelActiveMoveToSectorOrder();
			case FleetOrderType.WaitForAutoRepair:
				return new ModelActiveWaitForAutoRepairOrder();
			case FleetOrderType.BuildStation:
				return new ModelActiveBuildStationOrder();
			case FleetOrderType.ClaimUnit:
				return new ModelActiveClaimUnitOrder();
			default:
				throw new NotImplementedException($"Order type {(int)orderType}");
			}
		}
	}
}
