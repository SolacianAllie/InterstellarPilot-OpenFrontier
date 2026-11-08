using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers.Helpers
{
	public static class ActiveFleetOrderReader
	{
		public static ModelActiveFleetOrder Read(BinaryReader reader, FleetOrderType fleetOrderType, int fleetId, Dictionary<int, ModelFaction> factions, Dictionary<int, ModelSector> sectors, Dictionary<int, ModelUnit> units, Dictionary<int, ModelFleet> fleets, Dictionary<int, ModelSectorPatrolPath> patrolPaths, Dictionary<int, ModelPerson> people, Dictionary<int, ModelPassengerGroup> passengerGroups)
		{
			ModelActiveFleetOrder modelActiveFleetOrder = CreateActiveFleetOrderFromType.Create(fleetOrderType);
			modelActiveFleetOrder.TimeoutTime = reader.ReadDouble();
			modelActiveFleetOrder.StartTime = reader.ReadDouble();
			switch (fleetOrderType)
			{
			case FleetOrderType.AttackGroup:
			{
				ModelActiveAttackFleetOrder modelActiveAttackFleetOrder = (ModelActiveAttackFleetOrder)modelActiveFleetOrder;
				int key10 = reader.ReadInt32();
				modelActiveAttackFleetOrder.TargetFleet = fleets.GetValueOrDefault(key10);
				break;
			}
			case FleetOrderType.AttackTarget:
			{
				ModelActiveAttackTargetOrder modelActiveAttackTargetOrder = (ModelActiveAttackTargetOrder)modelActiveFleetOrder;
				int key7 = reader.ReadInt32();
				int key8 = reader.ReadInt32();
				modelActiveAttackTargetOrder.TargetUnit = units.GetValueOrDefault(key7);
				modelActiveAttackTargetOrder.OriginalTargetFaction = factions.GetValueOrDefault(key8);
				break;
			}
			case FleetOrderType.AutonomousBountyHunterObjective:
			{
				ModelActiveUniverseBountyHunterOrder modelActiveUniverseBountyHunterOrder = (ModelActiveUniverseBountyHunterOrder)modelActiveFleetOrder;
				int key9 = reader.ReadInt32();
				modelActiveUniverseBountyHunterOrder.TargetPerson = people.GetValueOrDefault(key9);
				break;
			}
			case FleetOrderType.AutonomousRoamLocationsObjective:
			{
				ModelActiveUniverseRoamOrder modelActiveUniverseRoamOrder = (ModelActiveUniverseRoamOrder)modelActiveFleetOrder;
				int key6 = reader.ReadInt32();
				modelActiveUniverseRoamOrder.CurrentTargetSector = sectors.GetValueOrDefault(key6);
				modelActiveUniverseRoamOrder.CurrentTargetPosition = reader.ReadVec3();
				break;
			}
			case FleetOrderType.Explore:
			{
				ModelActiveExploreOrder modelActiveExploreOrder = (ModelActiveExploreOrder)modelActiveFleetOrder;
				modelActiveExploreOrder.CurrentTargetSector = reader.ReadSector(sectors);
				modelActiveExploreOrder.CurrentTargetWormhole = reader.ReadUnit(units);
				modelActiveExploreOrder.CurrentTargetSectorPosition = reader.ReadVec3();
				break;
			}
			case FleetOrderType.AutonomousTrade:
			case FleetOrderType.ManualTrade:
			case FleetOrderType.Trade:
			{
				ModelActiveTradeOrder modelActiveTradeOrder = (ModelActiveTradeOrder)modelActiveFleetOrder;
				if (reader.ReadBoolean())
				{
					modelActiveTradeOrder.TradeRoute = CustomTradeRouteReader.Read(reader, units);
				}
				modelActiveTradeOrder.EndBuySellTime = reader.ReadDouble();
				modelActiveTradeOrder.LastStateChangeTime = reader.ReadDouble();
				ActiveTradeOrderState currentState2 = (ActiveTradeOrderState)reader.ReadInt32();
				if (modelActiveTradeOrder.TradeRoute != null)
				{
					modelActiveTradeOrder.CurrentState = currentState2;
				}
				break;
			}
			case FleetOrderType.AutonomousTransportPassengers:
			{
				ModelActiveUniversePassengerTransportOrder modelActiveUniversePassengerTransportOrder = (ModelActiveUniversePassengerTransportOrder)modelActiveFleetOrder;
				int key5 = reader.ReadInt32();
				ModelPassengerGroup valueOrDefault = passengerGroups.GetValueOrDefault(key5);
				modelActiveUniversePassengerTransportOrder.PassengerGroup = valueOrDefault;
				modelActiveUniversePassengerTransportOrder.EndBuySellTime = reader.ReadDouble();
				modelActiveUniversePassengerTransportOrder.LastStateChangeTime = reader.ReadDouble();
				ActiveTransportPassengerOrderState currentState = (ActiveTransportPassengerOrderState)reader.ReadInt32();
				if (modelActiveUniversePassengerTransportOrder.PassengerGroup != null)
				{
					modelActiveUniversePassengerTransportOrder.CurrentState = currentState;
				}
				break;
			}
			case FleetOrderType.Mine:
			{
				ModelActiveMineOrder modelActiveMineOrder = (ModelActiveMineOrder)modelActiveFleetOrder;
				modelActiveMineOrder.MineTarget = reader.ReadUnit(units);
				modelActiveMineOrder.State = (ActiveMineOrderState)reader.ReadInt32();
				modelActiveMineOrder.AngleFromAsteroid = reader.ReadSingle();
				modelActiveMineOrder.DistanceFromAsteroid = reader.ReadSingle();
				break;
			}
			case FleetOrderType.Scavenge:
				((ModelActiveScavengeOrder)modelActiveFleetOrder).Position = reader.ReadNullableVec3();
				break;
			case FleetOrderType.Patrol:
			case FleetOrderType.PatrolPath:
			{
				ModelActivePatrolOrder modelActivePatrolOrder = (ModelActivePatrolOrder)modelActiveFleetOrder;
				modelActivePatrolOrder.PathDirection = reader.ReadInt32();
				modelActivePatrolOrder.NodeIndex = reader.ReadInt32();
				modelActivePatrolOrder.StartNodeIndex = reader.ReadInt32();
				break;
			}
			case FleetOrderType.ManualRepair:
			case FleetOrderType.RepairAtNearest:
			{
				ModelActiveRepairFleetOrder modelActiveRepairFleetOrder = (ModelActiveRepairFleetOrder)modelActiveFleetOrder;
				modelActiveRepairFleetOrder.RepairState = (ActiveRepairFleetOrderState)reader.ReadInt32();
				int key4 = reader.ReadInt32();
				modelActiveRepairFleetOrder.CurrentRepairLocationUnit = units.GetValueOrDefault(key4);
				break;
			}
			case FleetOrderType.ManualRearm:
			case FleetOrderType.RearmAtNearest:
			{
				ModelActiveRearmFleetOrder modelActiveRearmFleetOrder = (ModelActiveRearmFleetOrder)modelActiveFleetOrder;
				modelActiveRearmFleetOrder.State = (ActiveRearmFleetOrderState)reader.ReadInt32();
				int key3 = reader.ReadInt32();
				modelActiveRearmFleetOrder.CurrentRearmLocationUnit = units.GetValueOrDefault(key3);
				break;
			}
			case FleetOrderType.Wait:
				((ModelActiveWaitOrder)modelActiveFleetOrder).WaitExpiryTime = reader.ReadDouble();
				break;
			case FleetOrderType.SellCargo:
			{
				ModelActiveSellCargoOrder modelActiveSellCargoOrder = (ModelActiveSellCargoOrder)modelActiveFleetOrder;
				modelActiveSellCargoOrder.SellExpireTime = reader.ReadDouble();
				int sellCargoClass = reader.ReadInt32();
				modelActiveSellCargoOrder.SellCargoClass = (ModelCargoClass)sellCargoClass;
				int key2 = reader.ReadInt32();
				modelActiveSellCargoOrder.TraderTargetUnit = units.GetValueOrDefault(key2);
				modelActiveSellCargoOrder.State = (ActiveSellCargoOrderState)reader.ReadInt32();
				break;
			}
			case FleetOrderType.MoveToNearestFriendlyStation:
			{
				ModelActiveMoveToNearestFriendlyStationOrder modelActiveMoveToNearestFriendlyStationOrder = (ModelActiveMoveToNearestFriendlyStationOrder)modelActiveFleetOrder;
				int key = reader.ReadInt32();
				modelActiveMoveToNearestFriendlyStationOrder.TargetStationUnit = units.GetValueOrDefault(key);
				break;
			}
			case FleetOrderType.EnterWormhole:
				((ModelActiveEnterWormholeOrder)modelActiveFleetOrder).State = (EnterWormholeState)reader.ReadInt32();
				break;
			case FleetOrderType.ExploreSector:
				((ModelActiveExploreSectorOrder)modelActiveFleetOrder).CurrentTargetSectorPosition = reader.ReadNullableVec3();
				break;
			}
			return modelActiveFleetOrder;
		}
	}
}
