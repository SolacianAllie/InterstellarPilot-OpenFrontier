using System.IO;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers.Helpers
{
	public static class ActiveFleetOrderWriter
	{
		public static void Write(BinaryWriter writer, ModelActiveFleetOrder activeFleetOrder)
		{
			writer.Write(activeFleetOrder.TimeoutTime);
			writer.Write(activeFleetOrder.StartTime);
			switch (activeFleetOrder.Order.OrderType)
			{
			case FleetOrderType.AttackGroup:
			{
				ModelActiveAttackFleetOrder modelActiveAttackFleetOrder = (ModelActiveAttackFleetOrder)activeFleetOrder;
				writer.WriteFleetId(modelActiveAttackFleetOrder.TargetFleet);
				break;
			}
			case FleetOrderType.AttackTarget:
			{
				ModelActiveAttackTargetOrder modelActiveAttackTargetOrder = (ModelActiveAttackTargetOrder)activeFleetOrder;
				writer.WriteUnitId(modelActiveAttackTargetOrder.TargetUnit);
				writer.WriteFactionId(modelActiveAttackTargetOrder.OriginalTargetFaction);
				break;
			}
			case FleetOrderType.AutonomousBountyHunterObjective:
			{
				ModelActiveUniverseBountyHunterOrder modelActiveUniverseBountyHunterOrder = (ModelActiveUniverseBountyHunterOrder)activeFleetOrder;
				writer.WritePersonId(modelActiveUniverseBountyHunterOrder.TargetPerson);
				break;
			}
			case FleetOrderType.AutonomousRoamLocationsObjective:
			{
				ModelActiveUniverseRoamOrder modelActiveUniverseRoamOrder = (ModelActiveUniverseRoamOrder)activeFleetOrder;
				writer.WriteSectorId(modelActiveUniverseRoamOrder.CurrentTargetSector);
				writer.WriteVec3(modelActiveUniverseRoamOrder.CurrentTargetPosition);
				break;
			}
			case FleetOrderType.Explore:
			{
				ModelActiveExploreOrder modelActiveExploreOrder = (ModelActiveExploreOrder)activeFleetOrder;
				writer.WriteSectorId(modelActiveExploreOrder.CurrentTargetSector);
				writer.WriteUnitId(modelActiveExploreOrder.CurrentTargetWormhole);
				writer.WriteVec3(modelActiveExploreOrder.CurrentTargetSectorPosition);
				break;
			}
			case FleetOrderType.AutonomousTrade:
			case FleetOrderType.ManualTrade:
			case FleetOrderType.Trade:
			{
				ModelActiveTradeOrder modelActiveTradeOrder = (ModelActiveTradeOrder)activeFleetOrder;
				writer.Write(modelActiveTradeOrder.TradeRoute != null);
				if (modelActiveTradeOrder.TradeRoute != null)
				{
					CustomTraderRouteWriter.Write(writer, modelActiveTradeOrder.TradeRoute);
				}
				writer.Write(modelActiveTradeOrder.EndBuySellTime);
				writer.Write(modelActiveTradeOrder.LastStateChangeTime);
				writer.Write((int)modelActiveTradeOrder.CurrentState);
				break;
			}
			case FleetOrderType.AutonomousTransportPassengers:
			{
				ModelActiveUniversePassengerTransportOrder modelActiveUniversePassengerTransportOrder = (ModelActiveUniversePassengerTransportOrder)activeFleetOrder;
				writer.WritePassengerGroupId(modelActiveUniversePassengerTransportOrder.PassengerGroup);
				writer.Write(modelActiveUniversePassengerTransportOrder.EndBuySellTime);
				writer.Write(modelActiveUniversePassengerTransportOrder.LastStateChangeTime);
				writer.Write((int)modelActiveUniversePassengerTransportOrder.CurrentState);
				break;
			}
			case FleetOrderType.Mine:
			{
				ModelActiveMineOrder modelActiveMineOrder = (ModelActiveMineOrder)activeFleetOrder;
				writer.WriteUnitId(modelActiveMineOrder.MineTarget);
				writer.Write((int)modelActiveMineOrder.State);
				writer.Write(modelActiveMineOrder.AngleFromAsteroid);
				writer.Write(modelActiveMineOrder.DistanceFromAsteroid);
				break;
			}
			case FleetOrderType.Scavenge:
			{
				ModelActiveScavengeOrder modelActiveScavengeOrder = (ModelActiveScavengeOrder)activeFleetOrder;
				writer.WriteNullableVec3(modelActiveScavengeOrder.Position);
				break;
			}
			case FleetOrderType.Patrol:
			case FleetOrderType.PatrolPath:
			{
				ModelActivePatrolOrder modelActivePatrolOrder = (ModelActivePatrolOrder)activeFleetOrder;
				writer.Write(modelActivePatrolOrder.PathDirection);
				writer.Write(modelActivePatrolOrder.NodeIndex);
				writer.Write(modelActivePatrolOrder.StartNodeIndex);
				break;
			}
			case FleetOrderType.ManualRepair:
			case FleetOrderType.RepairAtNearest:
			{
				ModelActiveRepairFleetOrder modelActiveRepairFleetOrder = (ModelActiveRepairFleetOrder)activeFleetOrder;
				writer.Write((int)modelActiveRepairFleetOrder.RepairState);
				writer.WriteUnitId(modelActiveRepairFleetOrder.CurrentRepairLocationUnit);
				break;
			}
			case FleetOrderType.ManualRearm:
			case FleetOrderType.RearmAtNearest:
			{
				ModelActiveRearmFleetOrder modelActiveRearmFleetOrder = (ModelActiveRearmFleetOrder)activeFleetOrder;
				writer.Write((int)modelActiveRearmFleetOrder.State);
				writer.WriteUnitId(modelActiveRearmFleetOrder.CurrentRearmLocationUnit);
				break;
			}
			case FleetOrderType.Wait:
			{
				ModelActiveWaitOrder modelActiveWaitOrder = (ModelActiveWaitOrder)activeFleetOrder;
				writer.Write(modelActiveWaitOrder.WaitExpiryTime);
				break;
			}
			case FleetOrderType.SellCargo:
			{
				ModelActiveSellCargoOrder modelActiveSellCargoOrder = (ModelActiveSellCargoOrder)activeFleetOrder;
				writer.Write(modelActiveSellCargoOrder.SellExpireTime);
				writer.Write((int)modelActiveSellCargoOrder.SellCargoClass);
				writer.WriteUnitId(modelActiveSellCargoOrder.TraderTargetUnit);
				writer.Write((int)modelActiveSellCargoOrder.State);
				break;
			}
			case FleetOrderType.MoveToNearestFriendlyStation:
			{
				ModelActiveMoveToNearestFriendlyStationOrder modelActiveMoveToNearestFriendlyStationOrder = (ModelActiveMoveToNearestFriendlyStationOrder)activeFleetOrder;
				writer.WriteUnitId(modelActiveMoveToNearestFriendlyStationOrder.TargetStationUnit);
				break;
			}
			case FleetOrderType.EnterWormhole:
			{
				ModelActiveEnterWormholeOrder modelActiveEnterWormholeOrder = (ModelActiveEnterWormholeOrder)activeFleetOrder;
				writer.Write((int)modelActiveEnterWormholeOrder.State);
				break;
			}
			case FleetOrderType.ExploreSector:
			{
				ModelActiveExploreSectorOrder modelActiveExploreSectorOrder = (ModelActiveExploreSectorOrder)activeFleetOrder;
				writer.WriteNullableVec3(modelActiveExploreSectorOrder.CurrentTargetSectorPosition);
				break;
			}
			case FleetOrderType.CollectCargo:
			case FleetOrderType.Dock:
			case FleetOrderType.RTB:
			case FleetOrderType.MoveTo:
			case FleetOrderType.JoinFleet:
			case FleetOrderType.DisposeCargo:
			case FleetOrderType.Protect:
			case FleetOrderType.Undock:
			case FleetOrderType.MoveToSector:
			case FleetOrderType.WaitForAutoRepair:
			case FleetOrderType.Rearm:
			case FleetOrderType.BuildStation:
			case FleetOrderType.ClaimUnit:
				break;
			}
		}
	}
}
