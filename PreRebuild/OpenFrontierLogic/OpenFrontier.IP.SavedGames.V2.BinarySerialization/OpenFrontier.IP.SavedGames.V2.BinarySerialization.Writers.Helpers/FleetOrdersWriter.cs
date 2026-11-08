using System;
using System.IO;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.Models;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers.Helpers
{
	public static class FleetOrdersWriter
	{
		public static void Write(BinaryWriter writer, ModelFleetOrderCollection fleetOrders)
		{
			writer.Write(fleetOrders.Orders.Count);
			foreach (ModelFleetOrder order in fleetOrders.Orders)
			{
				WriteOrder(writer, order);
			}
			writer.Write(fleetOrders.QueuedOrders.Count);
			foreach (ModelFleetOrder queuedOrder in fleetOrders.QueuedOrders)
			{
				writer.Write(fleetOrders.Orders.IndexOf(queuedOrder));
			}
			writer.Write(fleetOrders.CurrentOrder != null);
			if (fleetOrders.CurrentOrder != null)
			{
				writer.Write(fleetOrders.Orders.IndexOf(fleetOrders.CurrentOrder.Order));
				ActiveFleetOrderWriter.Write(writer, fleetOrders.CurrentOrder);
			}
		}

		public static void WriteOrder(BinaryWriter writer, ModelFleetOrder order)
		{
			writer.Write((int)order.OrderType);
			writer.Write(order.Id);
			writer.Write((int)order.CompletionMode);
			writer.Write(order.AllowCombatInterception);
			writer.Write((int)order.CloakPreference);
			writer.Write(order.MaxJumpDistance);
			writer.Write(order.AllowTimeout);
			writer.Write(order.TimeoutTime);
			writer.Write(order.MaxDuration);
			writer.Write(order.Priority);
			writer.Write(order.Notifications);
			switch (order.OrderType)
			{
			case FleetOrderType.AttackGroup:
			{
				ModelAttackFleetOrder modelAttackFleetOrder = (ModelAttackFleetOrder)order;
				writer.WriteFleetId(modelAttackFleetOrder.Target);
				writer.Write(modelAttackFleetOrder.AttackPriority);
				break;
			}
			case FleetOrderType.CollectCargo:
			{
				ModelCollectCargoOrder modelCollectCargoOrder = (ModelCollectCargoOrder)order;
				writer.WriteUnitId(modelCollectCargoOrder.TargetUnit);
				break;
			}
			case FleetOrderType.Scavenge:
			{
				ModelScavengeOrder modelScavengeOrder = (ModelScavengeOrder)order;
				writer.WriteSectorId(modelScavengeOrder.TargetSector);
				writer.Write((int)modelScavengeOrder.CollectOwnerMode);
				break;
			}
			case FleetOrderType.Mine:
			{
				ModelMineOrder modelMineOrder = (ModelMineOrder)order;
				writer.WriteSectorId(modelMineOrder.TargetSector);
				writer.Write((int)modelMineOrder.CollectOwnerMode);
				writer.WriteUnitId(modelMineOrder.ManualMineTarget);
				break;
			}
			case FleetOrderType.Dock:
			{
				ModelDockOrder modelDockOrder = (ModelDockOrder)order;
				writer.WriteUnitId(modelDockOrder.TargetDock);
				break;
			}
			case FleetOrderType.Patrol:
			{
				ModelPatrolOrder modelPatrolOrder = (ModelPatrolOrder)order;
				writer.Write(modelPatrolOrder.PathDirection);
				writer.Write(modelPatrolOrder.IsLooping);
				writer.Write(modelPatrolOrder.Nodes.Count);
				foreach (ModelPatrolPathNode node in modelPatrolOrder.Nodes)
				{
					writer.WriteSectorId(node.Sector);
					writer.WriteVec3(node.SectorPosition);
				}
				writer.Write(modelPatrolOrder.IsLoop);
				break;
			}
			case FleetOrderType.PatrolPath:
			{
				ModelPatrolPathOrder modelPatrolPathOrder = (ModelPatrolPathOrder)order;
				writer.Write(modelPatrolPathOrder.PathDirection);
				writer.Write(modelPatrolPathOrder.IsLooping);
				writer.WriteSectorPatrolPathId(modelPatrolPathOrder.PatrolPath);
				break;
			}
			case FleetOrderType.Wait:
			{
				ModelWaitOrder modelWaitOrder = (ModelWaitOrder)order;
				writer.Write(modelWaitOrder.WaitTime);
				break;
			}
			case FleetOrderType.AttackTarget:
			{
				ModelAttackTargetOrder modelAttackTargetOrder = (ModelAttackTargetOrder)order;
				writer.WriteUnitId(modelAttackTargetOrder.TargetUnit);
				writer.Write(modelAttackTargetOrder.AttackPriority);
				break;
			}
			case FleetOrderType.Trade:
			{
				ModelTradeOrder modelTradeOrder = (ModelTradeOrder)order;
				writer.Write(modelTradeOrder.MinBuyQuantity);
				writer.Write(modelTradeOrder.MinBuyCargoPercentage);
				break;
			}
			case FleetOrderType.ManualTrade:
			{
				ModelManualTradeOrder modelManualTradeOrder = (ModelManualTradeOrder)order;
				writer.Write(modelManualTradeOrder.MinBuyQuantity);
				writer.Write(modelManualTradeOrder.MinBuyCargoPercentage);
				writer.Write(modelManualTradeOrder.CustomTradeRoute != null);
				if (modelManualTradeOrder.CustomTradeRoute != null)
				{
					CustomTraderRouteWriter.Write(writer, modelManualTradeOrder.CustomTradeRoute);
				}
				break;
			}
			case FleetOrderType.AutonomousTrade:
			{
				ModelUniverseTradeOrder modelUniverseTradeOrder = (ModelUniverseTradeOrder)order;
				writer.Write(modelUniverseTradeOrder.MinBuyQuantity);
				writer.Write(modelUniverseTradeOrder.MinBuyCargoPercentage);
				writer.Write(modelUniverseTradeOrder.TradeOnlySpecificCargoClasses);
				writer.Write(modelUniverseTradeOrder.TradeSpecificCargoClasses.Count);
				{
					foreach (ModelCargoClass tradeSpecificCargoClass in modelUniverseTradeOrder.TradeSpecificCargoClasses)
					{
						writer.Write((int)tradeSpecificCargoClass);
					}
					break;
				}
			}
			case FleetOrderType.JoinFleet:
			{
				ModelJoinFleetOrder modelJoinFleetOrder = (ModelJoinFleetOrder)order;
				writer.WriteFleetId(modelJoinFleetOrder.TargetFleet);
				break;
			}
			case FleetOrderType.MoveTo:
			{
				ModelMoveToOrder modelMoveToOrder = (ModelMoveToOrder)order;
				writer.Write(modelMoveToOrder.CompleteOnReachTarget);
				writer.Write(modelMoveToOrder.ArrivalThreshold);
				writer.Write(modelMoveToOrder.MatchTargetOrientation);
				writer.WriteNullableVec3(modelMoveToOrder.PreferredRelativeVectorFromTarget);
				writer.Write(modelMoveToOrder.Target != null);
				if (modelMoveToOrder.Target != null)
				{
					SectorTargetWriter.Write(writer, modelMoveToOrder.Target);
				}
				break;
			}
			case FleetOrderType.Protect:
			{
				ModelProtectOrder modelProtectOrder = (ModelProtectOrder)order;
				writer.Write(modelProtectOrder.CompleteOnReachTarget);
				writer.Write(modelProtectOrder.ArrivalThreshold);
				writer.Write(modelProtectOrder.MatchTargetOrientation);
				writer.WriteNullableVec3(modelProtectOrder.PreferredRelativeVectorFromTarget);
				writer.Write(modelProtectOrder.Target != null);
				if (modelProtectOrder.Target != null)
				{
					SectorTargetWriter.Write(writer, modelProtectOrder.Target);
				}
				break;
			}
			case FleetOrderType.SellCargo:
			{
				ModelSellCargoOrder modelSellCargoOrder = (ModelSellCargoOrder)order;
				writer.Write(modelSellCargoOrder.FreeUnitsCompleteThreshold);
				writer.Write(modelSellCargoOrder.MinBuyPriceMultiplier);
				writer.Write(modelSellCargoOrder.SellOnlyListedCargos);
				writer.Write(modelSellCargoOrder.CompleteWhenNoBuyerFound);
				writer.Write(modelSellCargoOrder.CompleteWhenNoCargoToSell);
				writer.WriteUnitId(modelSellCargoOrder.ManualBuyerUnit);
				writer.Write(modelSellCargoOrder.CustomSellCargoTime);
				writer.Write(modelSellCargoOrder.SellCargoClasses.Count);
				foreach (ModelCargoClass sellCargoClass in modelSellCargoOrder.SellCargoClasses)
				{
					writer.Write((int)sellCargoClass);
				}
				writer.Write(modelSellCargoOrder.SellEquipment);
				break;
			}
			case FleetOrderType.ManualRearm:
			{
				ModelManualRearmFleetOrder modelManualRearmFleetOrder = (ModelManualRearmFleetOrder)order;
				writer.Write(modelManualRearmFleetOrder.EquipmentCargoUsage);
				writer.Write((int)modelManualRearmFleetOrder.InsufficientCreditsMode);
				writer.WriteUnitId(modelManualRearmFleetOrder.RearmLocationUnit);
				break;
			}
			case FleetOrderType.RearmAtNearest:
			{
				ModelRearmAtNearestFleetOrder modelRearmAtNearestFleetOrder = (ModelRearmAtNearestFleetOrder)order;
				writer.Write(modelRearmAtNearestFleetOrder.EquipmentCargoUsage);
				writer.Write((int)modelRearmAtNearestFleetOrder.InsufficientCreditsMode);
				break;
			}
			case FleetOrderType.ManualRepair:
			{
				ModelManualRepairFleetOrder modelManualRepairFleetOrder = (ModelManualRepairFleetOrder)order;
				writer.Write((int)modelManualRepairFleetOrder.InsufficientCreditsMode);
				writer.WriteUnitId(modelManualRepairFleetOrder.RepairLocationUnit);
				break;
			}
			case FleetOrderType.RepairAtNearest:
			{
				ModelRepairAtNearestStationOrder modelRepairAtNearestStationOrder = (ModelRepairAtNearestStationOrder)order;
				writer.Write((int)modelRepairAtNearestStationOrder.InsufficientCreditsMode);
				break;
			}
			case FleetOrderType.MoveToNearestFriendlyStation:
			{
				ModelMoveToNearestFriendlyStationOrder modelMoveToNearestFriendlyStationOrder = (ModelMoveToNearestFriendlyStationOrder)order;
				writer.Write(modelMoveToNearestFriendlyStationOrder.CompleteOnReachTarget);
				break;
			}
			case FleetOrderType.EnterWormhole:
			{
				ModelEnterWormholeOrder modelEnterWormholeOrder = (ModelEnterWormholeOrder)order;
				writer.WriteUnitId(modelEnterWormholeOrder.TargetWormhole);
				break;
			}
			case FleetOrderType.ExploreSector:
			{
				ModelExploreSectorOrder modelExploreSectorOrder = (ModelExploreSectorOrder)order;
				writer.WriteSectorId(modelExploreSectorOrder.Sector);
				break;
			}
			case FleetOrderType.MoveToSector:
			{
				ModelMoveToSectorOrder modelMoveToSectorOrder = (ModelMoveToSectorOrder)order;
				writer.WriteSectorId(modelMoveToSectorOrder.TargetSector);
				break;
			}
			case FleetOrderType.WaitForAutoRepair:
			{
				ModelWaitForAutoRepairOrder modelWaitForAutoRepairOrder = (ModelWaitForAutoRepairOrder)order;
				writer.Write(modelWaitForAutoRepairOrder.HullConditionThreshold);
				writer.Write(modelWaitForAutoRepairOrder.ComponentsConditionThreshold);
				writer.Write(modelWaitForAutoRepairOrder.ShieldConditionThreshold);
				break;
			}
			case FleetOrderType.BuildStation:
			{
				ModelBuildStationOrder modelBuildStationOrder = (ModelBuildStationOrder)order;
				writer.Write((int)modelBuildStationOrder.UnitClass);
				writer.WriteSectorId(modelBuildStationOrder.Sector);
				writer.WriteVec3(modelBuildStationOrder.SectorPosition);
				writer.Write((int)modelBuildStationOrder.InsufficientCreditsMode);
				break;
			}
			case FleetOrderType.ClaimUnit:
			{
				ModelClaimUnitOrder modelClaimUnitOrder = (ModelClaimUnitOrder)order;
				writer.WriteUnitId(modelClaimUnitOrder.Unit);
				break;
			}
			default:
				throw new Exception($"Unable to read data for objective of type {order.OrderType}. Unknown type");
			case FleetOrderType.RTB:
			case FleetOrderType.DisposeCargo:
			case FleetOrderType.AutonomousTransportPassengers:
			case FleetOrderType.AutonomousRoamLocationsObjective:
			case FleetOrderType.AutonomousBountyHunterObjective:
			case FleetOrderType.Explore:
				break;
			}
		}
	}
}
