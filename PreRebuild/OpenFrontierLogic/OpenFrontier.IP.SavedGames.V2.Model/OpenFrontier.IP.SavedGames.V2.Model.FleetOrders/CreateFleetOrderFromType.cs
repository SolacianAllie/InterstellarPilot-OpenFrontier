using System;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders
{
	public static class CreateFleetOrderFromType
	{
		public static ModelFleetOrder Create(FleetOrderType orderType)
		{
			return orderType switch
			{
				FleetOrderType.AttackTarget => (ModelFleetOrder)new ModelAttackTargetOrder(), 
				FleetOrderType.AutonomousTrade => new ModelUniverseTradeOrder(), 
				FleetOrderType.CollectCargo => new ModelCollectCargoOrder(), 
				FleetOrderType.Dock => new ModelDockOrder(), 
				FleetOrderType.ManualTrade => new ModelManualTradeOrder(), 
				FleetOrderType.Mine => new ModelMineOrder(), 
				FleetOrderType.MoveTo => new ModelMoveToOrder(), 
				FleetOrderType.Patrol => new ModelPatrolOrder(), 
				FleetOrderType.PatrolPath => new ModelPatrolPathOrder(), 
				FleetOrderType.RTB => new ModelReturnToBaseOrder(), 
				FleetOrderType.Scavenge => new ModelScavengeOrder(), 
				FleetOrderType.SellCargo => new ModelSellCargoOrder(), 
				FleetOrderType.Trade => new ModelTradeOrder(), 
				FleetOrderType.Wait => new ModelWaitOrder(), 
				FleetOrderType.AttackGroup => new ModelAttackFleetOrder(), 
				FleetOrderType.JoinFleet => new ModelJoinFleetOrder(), 
				FleetOrderType.DisposeCargo => new ModelDisposeCargoOrder(), 
				FleetOrderType.Protect => new ModelProtectOrder(), 
				FleetOrderType.AutonomousBountyHunterObjective => new ModelUniverseBountyHunterOrder(), 
				FleetOrderType.AutonomousRoamLocationsObjective => new ModelUniverseRoamOrder(), 
				FleetOrderType.ManualRepair => new ModelManualRepairFleetOrder(), 
				FleetOrderType.RepairAtNearest => new ModelRepairAtNearestStationOrder(), 
				FleetOrderType.ManualRearm => new ModelManualRearmFleetOrder(), 
				FleetOrderType.RearmAtNearest => new ModelRearmAtNearestFleetOrder(), 
				FleetOrderType.AutonomousTransportPassengers => new ModelUniversePassengerTransportOrder(), 
				FleetOrderType.Explore => new ModelExploreOrder(), 
				FleetOrderType.MoveToNearestFriendlyStation => new ModelMoveToNearestFriendlyStationOrder(), 
				FleetOrderType.EnterWormhole => new ModelEnterWormholeOrder(), 
				FleetOrderType.ExploreSector => new ModelExploreSectorOrder(), 
				FleetOrderType.MoveToSector => new ModelMoveToSectorOrder(), 
				FleetOrderType.WaitForAutoRepair => new ModelWaitForAutoRepairOrder(), 
				FleetOrderType.BuildStation => new ModelBuildStationOrder(), 
				FleetOrderType.ClaimUnit => new ModelClaimUnitOrder(), 
				_ => throw new NotImplementedException($"Order type {orderType}"), 
			};
		}
	}
}
