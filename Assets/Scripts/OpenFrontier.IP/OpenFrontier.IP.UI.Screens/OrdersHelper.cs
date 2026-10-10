using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.FleetOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.UI.Screens.NewFleetOrder;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI.Screens
{
	public static class OrdersHelper
	{
		private static StringBuilder orderTextBuilder = new StringBuilder();

		// The helper below exists for one reason: a client's order is not
		// carried out here. It is offered to whoever is listening, and when
		// nobody is - which is every singleplayer game - it returns false and
		// the body below runs exactly as it always has.
		//
		// The orders ask first every time rather than branching on whether this
		// is a client, because that decision belongs to whoever knows, and
		// putting it in twenty places would mean twenty chances to get it
		// wrong and to forget updating one of them.

		private static FleetOrderIntent Intent(
			FleetOrderKind kind,
			Fleet fleet,
			int targetUnit = 0,
			int targetSector = 0,
			float x = 0f,
			float y = 0f,
			float z = 0f,
			bool stack = false)
		{
			return new FleetOrderIntent
			{
				Kind = kind,
				FleetIds = OneFleet(fleet),
				LooseUnitIds = FleetOrderIntents.TakeLooseUnits(),
				TargetUnitId = targetUnit,
				TargetSectorId = targetSector,
				TargetX = x,
				TargetY = y,
				TargetZ = z,
				Stack = stack
			};
		}

		private static List<int> OneFleet(Fleet fleet)
		{
			// A client names ships, never fleets.
			//
			// A client builds fleets locally - it has to, or none of the game's
			// UI works, because the property screen, the cargo view and the
			// orders screen are all built around one. That fleet is a view of
			// a thing the host owns, and its id means nothing to anybody but
			// this machine.
			//
			// So the fleet is left out of the order and its ships go instead.
			// The host looks each ship up, finds the fleet it is really in -
			// building one if it is in none - and orders that. Which is what
			// would have happened had the click been made on the host, and it
			// cannot be wrong about fleet ids because it never used one.
			//
			// One place, so every one of the nineteen order kinds is covered
			// by this one line rather than by nineteen careful ones.
			if (Authority.IsThisCopyAClient)
			{
				return new List<int>(0);
			}

			var ids = new List<int>(1);

			if (fleet != null)
			{
				ids.Add(fleet.UniqueId);
			}

			return ids;
		}

		private static int Id(Unit unit)
		{
			return unit == null ? -1 : unit.UniqueId;
		}

		private static int Id(Sector sector)
		{
			return sector == null ? -1 : sector.UniqueId;
		}

		private static int Id(Wormhole wormhole)
		{
			// A wormhole is a component on a unit rather than a thing with an
			// id of its own, so the id that crosses the wire is the unit's -
			// and the host reads the wormhole back off it.
			if (wormhole == null)
			{
				return -1;
			}

			Unit host = wormhole.GetComponent<Unit>();

			return host == null ? -1 : host.UniqueId;
		}

		public static void SetFleetOrder(Fleet fleet, FleetOrder objective, bool stack)
		{
			if (!stack)
			{
				fleet.ClearOrders();
			}
			fleet.EnqueueOrder(objective);
			if (!stack)
			{
				fleet.AssignNextOrder();
			}
			fleet.DiscoverOwnUnits();
		}

		public static void OrderMineAsteroids(NewOrderTarget orderTarget, bool stack, Sector targetSector = null)
		{
			foreach (Fleet item in orderTarget.CreateAndReturnFleets())
			{
				if (item.HasMiningEquipment())
				{
					OrderMineAsteroids(item, stack, null, targetSector);
				}
			}
		}

		public static void OrderMineAsteroids(Fleet fleet, bool stack, Asteroid manualAsteroidTarget = null, Sector targetSector = null)
		{
			MineOrder mineOrder = UnityObjectHelper.NewGameObject<MineOrder>();
			mineOrder.CompletionMode = FleetOrderCompletionMode.Requeue;
			mineOrder.AllowTimeout = false;
			mineOrder.TargetSector = targetSector;
			mineOrder.ManualMineTarget = manualAsteroidTarget;
			SetFleetOrder(fleet, mineOrder, stack);
			SellCargoOrder sellCargoOrder = UnityObjectHelper.NewGameObject<SellCargoOrder>();
			sellCargoOrder.CompletionMode = FleetOrderCompletionMode.Requeue;
			sellCargoOrder.CompleteWhenNoCargoToSell = true;
			sellCargoOrder.CompleteWhenNoBuyerFound = false;
			sellCargoOrder.MinBuyPriceMultiplier = 0.05f;
			sellCargoOrder.AllowTimeout = false;
			fleet.EnqueueOrder(sellCargoOrder);
		}

		public static void OrderMineAsteroid(NewOrderTarget orderTarget, Unit asteroid, bool stack)
		{
			foreach (Fleet item in orderTarget.CreateAndReturnFleets())
			{
				if (item.HasMiningEquipment())
				{
					OrderMineAsteroids(item, stack, asteroid.Asteroid);
				}
			}
		}

		public static void OrderScavenge(NewOrderTarget orderTarget, Sector sector, bool stack)
		{
			foreach (Fleet item in orderTarget.CreateAndReturnFleets())
			{
				if (item.HasAnyShipGotATractorTurret())
				{
					OrderScavenge(item, sector, stack);
				}
			}
		}

		public static void OrderScavenge(Fleet fleet, Sector sector, bool stack)
		{
			if (FleetOrderIntents.TrySend(Intent(
				FleetOrderKind.Scavenge, fleet, targetSector: Id(sector), stack: stack)))
			{
				return;
			}

			ScavengeOrder scavengeOrder = UnityObjectHelper.NewGameObject<ScavengeOrder>();
			scavengeOrder.TargetSector = sector;
			SetFleetOrder(fleet, scavengeOrder, stack: true);
		}

		internal static void OrderBuildStation(Fleet fleet, UnitClass unitClassToBuild, Sector sector, Vector3 sectorPosition, bool stack)
		{
			BuildStationOrder buildStationOrder = UnityObjectHelper.NewGameObject<BuildStationOrder>();
			buildStationOrder.UnitClass = unitClassToBuild;
			buildStationOrder.Sector = sector;
			buildStationOrder.SectorPosition = sectorPosition;
			buildStationOrder.InsufficientCreditsMode = InsufficientCreditsMode.Abort;
			buildStationOrder.SetCreditsAndSetAsRestrictedSpend(fleet, unitClassToBuild.SaleCost);
			fleet.Faction.ApplyTransaction(-unitClassToBuild.SaleCost, FactionTransactionType.FleetTransfer, null, fleet.LeaderUnit);
			SetFleetOrder(fleet, buildStationOrder, stack);
		}

		public static void OrderManualTrade(NewOrderTarget orderTarget, Unit buyLocation, Unit sellLocation, CargoClass cargoClass, bool stack)
		{
			foreach (Fleet item in orderTarget.CreateAndReturnFleets())
			{
				OrderManualTrade(item, buyLocation, sellLocation, cargoClass, stack);
			}
		}

		public static void OrderManualTrade(Fleet fleet, Unit buyLocation, Unit sellLocation, CargoClass cargoClass, bool stack)
		{
			ManualTradeOrder manualTradeOrder = UnityObjectHelper.NewGameObject<ManualTradeOrder>();
			manualTradeOrder.CustomTradeRoute = new AITradeRoute
			{
				BuyLocation = buyLocation.CargoTrader,
				SellLocation = sellLocation.CargoTrader,
				CargoClass = cargoClass
			};
			SetFleetOrder(fleet, manualTradeOrder, stack);
		}

		public static void OrderClaimUnit(NewOrderTarget orderTarget, Unit unit, bool stack)
		{
			foreach (Fleet item in orderTarget.CreateAndReturnFleets())
			{
				OrderClaimUnit(item, unit, stack);
			}
		}

		public static void OrderClaimUnit(Fleet fleet, Unit unit, bool stack)
		{
			if (FleetOrderIntents.TrySend(Intent(
				FleetOrderKind.ClaimUnit, fleet, targetUnit: Id(unit), stack: stack)))
			{
				return;
			}

			ClaimUnitOrder claimUnitOrder = UnityObjectHelper.NewGameObject<ClaimUnitOrder>();
			claimUnitOrder.TargetUnit = unit;
			SetFleetOrder(fleet, claimUnitOrder, stack);
		}

		public static void OrderExploreSector(NewOrderTarget orderTarget, Sector selectedSector, bool stack)
		{
			foreach (Fleet item in orderTarget.CreateAndReturnFleets())
			{
				OrderExploreSector(item, selectedSector, stack);
			}
		}

		public static void OrderCollectCargo(NewOrderTarget orderTarget, Unit unit, bool stack)
		{
			foreach (Fleet item in orderTarget.CreateAndReturnFleets())
			{
				if (item.HasAnyShipGotATractorTurret())
				{
					OrderCollectCargo(item, unit, stack);
				}
			}
		}

		public static void OrderCollectCargo(Fleet fleet, Unit unit, bool stack)
		{
			if (FleetOrderIntents.TrySend(Intent(
				FleetOrderKind.CollectCargo, fleet, targetUnit: Id(unit), stack: stack)))
			{
				return;
			}

			CollectCargoOrder collectCargoOrder = UnityObjectHelper.NewGameObject<CollectCargoOrder>();
			collectCargoOrder.TargetUnit = unit;
			SetFleetOrder(fleet, collectCargoOrder, stack);
		}

		public static void OrderExploreSector(Fleet fleet, Sector sector, bool stack)
		{
			if (FleetOrderIntents.TrySend(Intent(
				FleetOrderKind.ExploreSector, fleet, targetSector: Id(sector), stack: stack)))
			{
				return;
			}

			ExploreSectorOrder exploreSectorOrder = UnityObjectHelper.NewGameObject<ExploreSectorOrder>();
			exploreSectorOrder.Sector = sector;
			exploreSectorOrder.AllowTimeout = false;
			SetFleetOrder(fleet, exploreSectorOrder, stack);
		}

		internal static void OrderMoveToSector(NewOrderTarget orderTarget, Sector sector, bool stack)
		{
			foreach (Fleet item in orderTarget.CreateAndReturnFleets())
			{
				OrderMoveToSector(item, sector, stack);
			}
		}

		private static void OrderMoveToSector(Fleet fleet, Sector sector, bool stack)
		{
			MoveToSectorOrder moveToSectorOrder = UnityObjectHelper.NewGameObject<MoveToSectorOrder>();
			moveToSectorOrder.TargetSector = sector;
			moveToSectorOrder.AllowTimeout = false;
			SetFleetOrder(fleet, moveToSectorOrder, stack: true);
		}

		public static void OrderReturnToBase(NewOrderTarget newOrderTarget, bool stack)
		{
			foreach (Fleet orderedFleet in newOrderTarget.OrderedFleets)
			{
				if (orderedFleet.IsHomeBaseValid)
				{
					OrderReturnToBase(orderedFleet, stack);
				}
			}
		}

		public static void OrderReturnToBase(Fleet fleet, bool stack)
		{
			if (FleetOrderIntents.TrySend(Intent(
				FleetOrderKind.ReturnToBase, fleet, stack: stack)))
			{
				return;
			}

			ReturnToBaseOrder returnToBaseOrder = UnityObjectHelper.NewGameObject<ReturnToBaseOrder>();
			returnToBaseOrder.AllowTimeout = false;
			SetFleetOrder(fleet, returnToBaseOrder, stack);
		}

		public static bool CanFleetFollowTarget(NewOrderTarget orderTarget, Unit targetUnit)
		{
			if (targetUnit == null)
			{
				return false;
			}
			if (targetUnit.IsStatic)
			{
				return false;
			}
			if (targetUnit.UnitType != UnitType.Ship)
			{
				return false;
			}
			if (orderTarget.IsUnitOrdered(targetUnit))
			{
				return false;
			}
			return true;
		}

		internal static void OrderProtectTarget(NewOrderTarget newOrderTarget, SectorTarget target, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderProtectTarget(item, target, stack);
			}
		}

		public static void OrderProtectTarget(Fleet orderedFleet, SectorTarget target, bool stack)
		{
			ProtectOrder protectOrder = UnityObjectHelper.NewGameObject<ProtectOrder>();
			protectOrder.Target = target;
			protectOrder.MatchTargetOrientation = true;
			SetFleetOrder(orderedFleet, protectOrder, stack);
		}

		public static void OrderMergeWithFleet(NewOrderTarget newOrderTarget, Fleet targetFleet, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderMergeWithFleet(item, targetFleet, stack);
			}
		}

		public static void OrderMergeWithFleet(Fleet orderedFleet, Fleet targetFleet, bool stack)
		{
			JoinFleetOrder joinFleetOrder = UnityObjectHelper.NewGameObject<JoinFleetOrder>();
			joinFleetOrder.TargetFleet = targetFleet;
			SetFleetOrder(orderedFleet, joinFleetOrder, stack);
		}

		public static void OrderTransportPassengers(NewOrderTarget newOrderTarget, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				if (item.Ships.Any((UnitComponentHolder e) => e.Unit.UnitHasPassengerCapacity()))
				{
					OrderTransportPassengers(item, stack);
				}
			}
		}

		public static void OrderTransportPassengers(Fleet orderedFleet, bool stack)
		{
			AutonomousTransportPassengersOrder autonomousTransportPassengersOrder = UnityObjectHelper.NewGameObject<AutonomousTransportPassengersOrder>();
			autonomousTransportPassengersOrder.AllowTimeout = false;
			SetFleetOrder(orderedFleet, autonomousTransportPassengersOrder, stack);
		}

		public static NpcPilot FindNpcPilot(Unit unit)
		{
			if (unit.Components != null)
			{
				if (unit.NpcPilot != null)
				{
					return unit.NpcPilot;
				}
				foreach (Person item in unit.Components.Crew)
				{
					if (item.NpcPilot != null)
					{
						return item.NpcPilot;
					}
				}
				return null;
			}
			return null;
		}

		public static void OrderRepairAtNearest(NewOrderTarget orderTarget, bool stack)
		{
			foreach (Fleet item in orderTarget.CreateAndReturnFleets())
			{
				OrderRepairAtNearest(item, stack);
			}
		}

		private static void OrderRepairAtNearest(Fleet fleet, bool stack)
		{
			RepairAtNearestStationOrder repairAtNearestStationOrder = UnityObjectHelper.NewGameObject<RepairAtNearestStationOrder>();
			repairAtNearestStationOrder.AllowTimeout = false;
			SetFleetOrder(fleet, repairAtNearestStationOrder, stack: true);
		}

		public static void OrderRepairAt(NewOrderTarget newOrderTarget, Unit repairLocation, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderRepairAt(item, repairLocation, stack);
			}
		}

		public static void OrderRepairAt(Fleet fleet, Unit repairLocation, bool stack)
		{
			ManualRepairFleetOrder manualRepairFleetOrder = UnityObjectHelper.NewGameObject<ManualRepairFleetOrder>();
			manualRepairFleetOrder.SpecificRepairLocation = repairLocation;
			manualRepairFleetOrder.AllowTimeout = false;
			SetFleetOrder(fleet, manualRepairFleetOrder, stack);
		}

		public static void OrderRearmAt(NewOrderTarget newOrderTarget, Unit rearmLocation, float equipmentUsage01, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderRearmAt(item, rearmLocation, equipmentUsage01, stack);
			}
		}

		public static void OrderRearmAt(Fleet fleet, Unit rearmLocation, float equipmentUsage01, bool stack)
		{
			ManualRearmOrder manualRearmOrder = UnityObjectHelper.NewGameObject<ManualRearmOrder>();
			manualRearmOrder.SpecificRearmLocation = rearmLocation;
			manualRearmOrder.AllowTimeout = false;
			manualRearmOrder.EquipmentUsage = equipmentUsage01;
			SetFleetOrder(fleet, manualRearmOrder, stack);
		}

		public static void OrderRearmAtNearest(NewOrderTarget newOrderTarget, float equipmentUsage01, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderRearmAtNearest(item, equipmentUsage01, stack);
			}
		}

		public static void OrderRearmAtNearest(Fleet fleet, float equipmentUsage01, bool stack)
		{
			RearmAtNearestOrder rearmAtNearestOrder = UnityObjectHelper.NewGameObject<RearmAtNearestOrder>();
			rearmAtNearestOrder.AllowTimeout = false;
			rearmAtNearestOrder.EquipmentUsage = equipmentUsage01;
			SetFleetOrder(fleet, rearmAtNearestOrder, stack);
		}

		public static bool CanStackOrderOn(Fleet orderedFleet)
		{
			if (!(orderedFleet == null) && orderedFleet.HasAnyOrders)
			{
				return orderedFleet.GetLastOrder().CanBeStackedOn();
			}
			return true;
		}

		public static bool CanStackOrderOn(NewOrderTarget newOrderTarget)
		{
			foreach (Fleet orderedFleet in newOrderTarget.OrderedFleets)
			{
				if (!CanStackOrderOn(orderedFleet))
				{
					return false;
				}
			}
			return true;
		}

		public static NpcPilot FindOrCreateNpc(Unit unit, bool pilotShip = true)
		{
			NpcPilot npcPilot = FindNpcPilot(unit);
			if (npcPilot == null && unit.Faction != null)
			{
				npcPilot = FindNpcPilotFromPilotPool(unit.Faction);
			}
			if (npcPilot == null)
			{
				npcPilot = CreateNpcPilot(unit);
			}
			if (pilotShip)
			{
				npcPilot.CurrentUnit = unit;
				npcPilot.Person.IsPilot = true;
			}
			return npcPilot;
		}

		private static NpcPilot FindNpcPilotFromPilotPool(Faction faction)
		{
			foreach (Person person in faction.People)
			{
				if (person != null && person.CurrentUnit == null && person != faction.LeaderPerson)
				{
					if (person.NpcPilot != null)
					{
						return person.NpcPilot;
					}
					NpcPilot npcPilot = person.gameObject.AddComponent<NpcPilot>();
					npcPilot.Init();
					return npcPilot;
				}
			}
			return null;
		}

		public static Fleet FindOrCreateNpcAndFleet(Unit unit)
		{
			NpcPilot controller = FindOrCreateNpc(unit, pilotShip: false);
			return FindOrCreateFleet(unit, controller);
		}

		public static Fleet FindOrCreateNpcAndFleetAndPilotShip(Unit unit)
		{
			NpcPilot controller = FindOrCreateNpc(unit);
			return FindOrCreateFleet(unit, controller);
		}

		private static Fleet FindOrCreateFleet(Unit unit, NpcPilot controller)
		{
			if (controller.Fleet != null)
			{
				return controller.Fleet;
			}

			Fleet fleet = CreateAndInitPlayerFleet(unit.Faction, unit.Sector, unit.SectorPosition, EngineASX.Instance.PlayerFleetPrefab);

			// Null means this copy may not build fleets, which is a client.
			// Returning it unchanged rather than dereferencing it - which is
			// what killed the orders menu: the menu builds a fleet purely to
			// have something to show, and a client answering "I may not" with
			// an exception reads as a button that does nothing.
			//
			// The caller carries on with a null fleet, which the order path
			// already understands: it hands the ships over instead.
			if (fleet == null)
			{
				return null;
			}

			fleet.transform.localRotation = unit.transform.localRotation;
			controller.Fleet = fleet;
			if (unit.IsOwnedByPlayer)
			{
				EngineASX.Instance.CachedFleetSettingsController.ApplyCachedUnitFleetSettings(unit);
			}
			return controller.Fleet;
		}

		public static Fleet FindFleetFromUnit(Unit unit)
		{
			NpcPilot npcPilot = FindNpcPilot(unit);
			if (npcPilot != null)
			{
				return npcPilot.Fleet;
			}
			return null;
		}

		public static string GetQueuedOrdersText(Fleet fleet)
		{
			if (fleet != null && fleet.OrderQueue.Count > 0)
			{
				return string.Join(", ", from order in fleet.OrderQueue
					where order != null
					select GetFleetOrderText(order, fleet.Faction, fleet.Sector));
			}
			return "None";
		}

		public static string GetFleetOrderText(FleetOrder order, Faction faction, Sector currentSector)
		{
			if (order != null)
			{
				string descriptionForFaction = order.GetDescriptionForFaction(faction, currentSector);
				if (string.IsNullOrWhiteSpace(descriptionForFaction))
				{
					return "Unknown order";
				}
				return descriptionForFaction;
			}
			return null;
		}

		public static string GetActiveOrderTextWithQueuedCount(ActiveFleetOrder activeOrder, Faction localFaction)
		{
			orderTextBuilder.Length = 0;
			if (activeOrder != null)
			{
				string text = null;
				string value = activeOrder.FleetOrder.GetDescriptionForFaction(localFaction, activeOrder.Fleet.Sector);
				if (string.IsNullOrWhiteSpace(value))
				{
					value = "Unknown";
				}
				else
				{
					text = activeOrder.GetStatusText(localFaction);
				}
				orderTextBuilder.Append(value);
				if (!string.IsNullOrEmpty(text))
				{
					orderTextBuilder.Append(" - " + text);
				}
				int queueOrderCount = GetQueueOrderCount(activeOrder.Fleet);
				if (queueOrderCount > 0)
				{
					orderTextBuilder.Append($" (+{queueOrderCount} more)");
				}
				return orderTextBuilder.ToString();
			}
			return null;
		}

		public static string GetActiveOrderText(Fleet fleet)
		{
			if (fleet != null && fleet.ActiveOrder != null)
			{
				return GetActiveOrderText(fleet.ActiveOrder);
			}
			return "None";
		}

		public static string GetActiveOrderText(ActiveFleetOrder activeOrder)
		{
			orderTextBuilder.Length = 0;
			if (activeOrder != null)
			{
				string text = null;
				string value = activeOrder.FleetOrder.GetDescriptionForFaction(activeOrder.Fleet.Faction, activeOrder.Fleet.Sector);
				if (string.IsNullOrWhiteSpace(value))
				{
					value = "Unknown";
				}
				else
				{
					text = activeOrder.GetStatusText(activeOrder.Fleet.Faction);
				}
				orderTextBuilder.Append(value);
				if (!string.IsNullOrEmpty(text))
				{
					orderTextBuilder.Append(" - " + text);
				}
				return orderTextBuilder.ToString();
			}
			return null;
		}

		public static string GetOrdersTextAndFleetStatus(Unit unit, Faction localFaction)
		{
			try
			{
				return GetOrdersTextAndFleetStatus(unit.GetFleet(), localFaction);
			}
			catch
			{
				return "Unknown";
			}
		}

		public static bool HasFleetGotStatusToDisplay(Unit unit)
		{
			Fleet fleet = unit.GetFleet();
			if (fleet == null)
			{
				return false;
			}
			if (!(fleet.ActiveOrder != null))
			{
				return fleet.CurState != FleetState.Idle;
			}
			return true;
		}

		public static bool HasFleetGotStatusToDisplay(Fleet fleet)
		{
			if (!(fleet.ActiveOrder != null))
			{
				return fleet.CurState != FleetState.Idle;
			}
			return true;
		}

		public static string GetOrdersTextAndFleetStatus(Fleet fleet, Faction localFaction)
		{
			if (fleet == null)
			{
				return "No order";
			}
			ActiveFleetOrder activeOrder = fleet.ActiveOrder;
			if (activeOrder != null)
			{
				return GetActiveOrderTextWithQueuedCount(activeOrder, localFaction);
			}
			string fleetStatus = fleet.GetFleetStatus();
			if (fleetStatus != null)
			{
				return "No order - " + fleetStatus;
			}
			return "No order";
		}

		public static ActiveFleetOrder GetFleetCurrentOrder(Unit unit)
		{
			Fleet fleet = unit.GetFleet();
			if (fleet != null)
			{
				return fleet.ActiveOrder;
			}
			return null;
		}

		public static int GetQueueOrderCount(Fleet fleet)
		{
			if (fleet != null)
			{
				return fleet.OrderQueue.Count;
			}
			return 0;
		}

		public static int GetQueueOrderCount(Unit unit)
		{
			return GetQueueOrderCount(unit.GetFleet());
		}

		public static FleetOrder GetGroupCurrentOrNextObjective(Unit unit)
		{
			NpcPilot npcPilot = FindNpcPilot(unit);
			if (npcPilot != null && npcPilot.Fleet != null)
			{
				return npcPilot.Fleet.GetCurrentOrNextOrder();
			}
			return null;
		}

		public static bool CanPlayerOrderUnit(Unit orderedUnit)
		{
			if (orderedUnit.IsPilottable() && orderedUnit.IsOwnedByPlayer)
			{
				return EngineASX.Instance.World.Permissions.AllowOrders;
			}
			return false;
		}

		public static void OrderPatrol(NewOrderTarget newOrderTarget, IEnumerable<SectorTarget> sectorTargets, bool isLoop, bool repeat, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderPatrol(item, sectorTargets, isLoop, repeat, stack);
			}
		}

		public static void OrderPatrol(Fleet fleet, IEnumerable<SectorTarget> sectorTargets, bool isLoop, bool repeat, bool stack)
		{
			PatrolOrder patrolOrder = UnityObjectHelper.NewGameObject<PatrolOrder>();
			foreach (SectorTarget sectorTarget in sectorTargets)
			{
				patrolOrder.Nodes.Add(new AIPatrolPathNode
				{
					Sector = sectorTarget.Sector,
					SectorPosition = sectorTarget.SectorPosition
				});
			}
			patrolOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			patrolOrder.AllowTimeout = true;
			patrolOrder.IsLoop = isLoop;
			patrolOrder.IsLooping = repeat;
			SetFleetOrder(fleet, patrolOrder, stack);
		}

		public static void OrderDockAtTarget(NewOrderTarget newOrderTarget, Unit targetUnit, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderDockAtTarget(item, targetUnit, stack);
			}
		}

		public static void OrderDockAtTarget(Fleet orderedFleet, Unit targetUnit, bool stack)
		{
			if (FleetOrderIntents.TrySend(Intent(
				FleetOrderKind.DockAtTarget, orderedFleet,
				targetUnit: Id(targetUnit), stack: stack)))
			{
				return;
			}

			DockOrder dockOrder = UnityObjectHelper.NewGameObject<DockOrder>();
			dockOrder.TargetDock = targetUnit;
			dockOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			dockOrder.AllowTimeout = true;
			SetFleetOrder(orderedFleet, dockOrder, stack);
		}

		public static bool CanOrderDockAtTarget(NewOrderTarget orderTarget, Unit targetUnit)
		{
			foreach (Unit allUnit in orderTarget.AllUnits)
			{
				if (!CanOrderDockAtTarget(allUnit, targetUnit))
				{
					return false;
				}
			}
			return true;
		}

		public static bool CanOrderDockAtTarget(Unit orderedUnit, Unit targetUnit)
		{
			if (CanPlayerOrderUnit(orderedUnit) && orderedUnit != null && targetUnit != null && orderedUnit != targetUnit && orderedUnit.IsValidAndNotDestroyed && orderedUnit.IsMobile && orderedUnit.CanFitInDockIgnoreOccupancy(targetUnit) && targetUnit.IsDockable)
			{
				return !targetUnit.IsHostileTo(orderedUnit);
			}
			return false;
		}

		public static bool CanOrderDockAtTarget(Fleet orderedFleet, Unit targetUnit)
		{
			if (orderedFleet != null && targetUnit != null && orderedFleet != targetUnit.GetFleet() && orderedFleet.IsMobile && targetUnit.IsDockable && orderedFleet.CanUnitsFitInHangarIgnoreOccupancy(targetUnit))
			{
				return !targetUnit.IsHostileTo(orderedFleet);
			}
			return false;
		}

		public static bool CanOrderRepairAtTarget(NewOrderTarget newOrderTarget, Unit targetUnit)
		{
			foreach (Unit allUnit in newOrderTarget.AllUnits)
			{
				if (CanOrderRepairAtTarget(allUnit, targetUnit))
				{
					return true;
				}
			}
			return false;
		}

		public static bool CanOrderRepairAtTarget(Unit orderedUnit, Unit targetUnit)
		{
			if (CanOrderDockAtTarget(orderedUnit, targetUnit))
			{
				return targetUnit.UnitClass.HasRepairFacilities;
			}
			return false;
		}

		public static bool CanOrderRearmAtTarget(NewOrderTarget newOrderTarget, Unit targetUnit)
		{
			foreach (Unit allUnit in newOrderTarget.AllUnits)
			{
				if (CanOrderRearmAtTarget(allUnit, targetUnit))
				{
					return true;
				}
			}
			return false;
		}

		public static bool CanOrderRearmAtTarget(Unit orderedUnit, Unit unit)
		{
			if (CanOrderDockAtTarget(orderedUnit, unit))
			{
				return unit.IsEquipmentTrader();
			}
			return false;
		}

		public static void OrderMoveToSectorTarget(NewOrderTarget newOrderTarget, SectorTarget sectorTarget, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderMoveToSectorTarget(item, sectorTarget, stack);
			}
		}

		public static void OrderMoveToSectorTarget(Fleet fleet, SectorTarget sectorTarget, bool stack)
		{
			if (FleetOrderIntents.TrySend(Intent(
				FleetOrderKind.MoveToSectorTarget,
				fleet,
				targetUnit: Id(sectorTarget == null ? null : sectorTarget.TargetUnit),
				targetSector: Id(sectorTarget == null ? null : sectorTarget.Sector),
				x: sectorTarget == null ? 0f : sectorTarget.SectorPosition.x,
				y: sectorTarget == null ? 0f : sectorTarget.SectorPosition.y,
				z: sectorTarget == null ? 0f : sectorTarget.SectorPosition.z,
				stack: stack)))
			{
				return;
			}

			if (sectorTarget.TargetUnit != null && sectorTarget.TargetUnit.UnitType == UnitType.Waypoint)
			{
				sectorTarget = SectorTarget.FromSectorPosition(sectorTarget.GetTargetSector(), sectorTarget.GetTargetSectorPosition());
			}
			MoveToOrder moveToOrder = UnityObjectHelper.NewGameObject<MoveToOrder>();
			moveToOrder.Target = sectorTarget;
			moveToOrder.AllowTimeout = true;
			moveToOrder.MatchTargetOrientation = true;
			moveToOrder.CompleteOnReachTarget = !moveToOrder.Target.IsMovingTarget;
			SetFleetOrder(fleet, moveToOrder, stack);
		}

		public static void OrderEnterWormhole(NewOrderTarget newOrderTarget, Wormhole wormhole, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderEnterWormhole(item, wormhole, stack);
			}
		}

		public static void OrderEnterWormhole(Fleet fleet, Wormhole wormhole, bool stack)
		{
			if (FleetOrderIntents.TrySend(Intent(
				FleetOrderKind.EnterWormhole, fleet, targetUnit: Id(wormhole))))
			{
				return;
			}

			EnterWormholeOrder enterWormholeOrder = UnityObjectHelper.NewGameObject<EnterWormholeOrder>();
			enterWormholeOrder.TargetWormhole = wormhole;
			enterWormholeOrder.AllowTimeout = true;
			SetFleetOrder(fleet, enterWormholeOrder, stack);
		}

		public static void StopGroupOrderFromTimingOut(Unit orderedUnit)
		{
			Fleet fleet = FindFleetFromUnit(orderedUnit);
			if (!(fleet != null))
			{
				return;
			}
			if (fleet.ActiveOrder != null)
			{
				fleet.ActiveOrder.FleetOrder.AllowTimeout = false;
			}
			foreach (FleetOrder item in fleet.OrderQueue)
			{
				item.AllowTimeout = false;
			}
		}

		public static void CancelAutoPilot(Unit unit)
		{
			OrderStop(unit);
			NpcPilot npcPilot = FindNpcPilot(unit);
			if (npcPilot != null)
			{
				npcPilot.Person.IsPilot = false;
				npcPilot.Fleet = null;
			}
		}

		public static void OrderStop(Unit unit)
		{
			StopUnit(unit);
			Fleet fleet = FindFleetFromUnit(unit);
			if (!(fleet != null))
			{
				return;
			}
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if (ship != null && ship.Unit != null)
				{
					StopUnit(ship.Unit);
				}
			}
			fleet.ClearOrders();
		}

		private static void StopUnit(Unit unit)
		{
			unit.Components.EngineThrottle = 0f;
			if (unit.ActiveUnit != null && unit.ActiveUnit.ActiveUnitShip != null)
			{
				unit.ActiveUnit.ActiveUnitShip.DesiredTurn = 0f;
			}
		}

		public static bool IsPilottedByNpc(Unit playerUnit)
		{
			if (playerUnit.Components != null && playerUnit.Components.PilotPerson != null)
			{
				return playerUnit.Components.PilotPerson != playerUnit.Engine.LocalPlayer.Person;
			}
			return false;
		}

		public static bool DoesUnitHaveOrders(Unit unit)
		{
			if (unit.NpcPilot != null)
			{
				NpcPilot npcPilot = unit.NpcPilot;
				if (npcPilot != null && npcPilot.Fleet != null && npcPilot.Fleet.ActiveOrder != null)
				{
					return true;
				}
			}
			return false;
		}

		public static Fleet CreateAndInitPlayerFleet(Faction faction, Sector sector, Vector3 sectorPosition, Fleet fleetPrefab)
		{
		// A copy that does not own the world does not make fleets.
		//
		// This is the only place a fleet is created - everything else comes
		// through here - so it is the only place the rule has to be stated.
		//
		// A fleet made on a client is an id the host has never issued. The
		// player then selects it, gives it an order, and the host refuses,
		// because from the host's side that fleet genuinely does not exist:
		//
		//     client: This copy selected: fleet 100149 of 'Frontier Corp 2'
		//     host:   named fleet 100149, which this world does not have
		//
		// Both halves true, and nothing anywhere said the client had built
		// one. The refusal was correct; the bug was upstream of it, in a
		// property screen's "create new fleet" that ran on both machines.
		//
		// Null rather than a local stand-in, because a stand-in is the same
		// bug wearing a disguise. Callers are expected to cope, and the ones
		// that cannot say so.
		if (Authority.IsThisCopyAClient)
		{
			Debug.LogWarning("OrdersHelper: this copy cannot make a fleet - the world"
				+ " that owns the ships has to make it. Nothing was created.");
		}

		Authority.Declined();

		return null;
		}

		private static NpcPilot CreateNpcPilot(Unit unit)
		{
			Person person = UnityObjectHelper.InstantiateAndGetComponent(unit.Engine.WingmenPilotPrefab);
			person.Init();
			person.transform.SetParent(unit.transform);
			person.transform.localPosition = Vector3.zero;
			person.Faction = unit.Faction;
			person.DestroyGameObjectOnKill = true;
			person.AutoAssignAvatarProfileFromFactionIfNone();
			person.RandomizeNameAndPersonalityAndAssignDialog();
			person.AssignFirstPilotRankIfNull();
			NpcPilotSettings npcPilotSettings = person.GetComponent<NpcPilotSettings>();
			if (npcPilotSettings == null)
			{
				npcPilotSettings = person.gameObject.AddComponent<NpcPilotSettings>();
			}
			npcPilotSettings.CombatEfficiency = Maths.RandomFloatWithPower(0.2f, 1f, 0.8f);
			NpcPilot component = person.GetComponent<NpcPilot>();
			component.Init();
			component.Settings.AICheatAmmo = false;
			if (unit.Faction != null)
			{
				component.SetCombatEfficiencyFromFactionRange(unit.Faction);
			}
			return component;
		}

		private static void RemoveAutoPilotController(NpcPilot controller)
		{
			if (controller != null)
			{
				if (controller.Fleet != null)
				{
					UnityEngine.Object.DestroyImmediate(controller.Fleet.gameObject);
				}
				UnityEngine.Object.DestroyImmediate(controller.gameObject);
			}
		}

		public static void OrderAttackUnit(NewOrderTarget newOrderTarget, Unit targetUnit, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderAttackUnit(item, targetUnit, stack);
			}
		}

		public static void OrderAttackUnit(Fleet fleet, Unit targetUnit, bool stack)
		{
			AttackTargetOrder attackTargetOrder = UnityObjectHelper.NewGameObject<AttackTargetOrder>();
			attackTargetOrder.TargetUnit = targetUnit;
			attackTargetOrder.CompleteWhenNotHostile = fleet.Faction.IsHostileTo(targetUnit);
			attackTargetOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			attackTargetOrder.AllowTimeout = false;
			attackTargetOrder.AttackPriority = 40f;
			SetFleetOrder(fleet, attackTargetOrder, stack);
		}

		public static bool CanOrderAttackTarget(NewOrderTarget newOrderTarget, Unit target)
		{
			foreach (Unit allUnit in newOrderTarget.AllUnits)
			{
				if (CanOrderAttackTarget(target, allUnit, allUnit.Faction))
				{
					return true;
				}
			}
			return false;
		}

		public static bool CanOrderAttackTarget(Unit target, Unit orderedUnit, Faction orderedUnitFaction)
		{
			if (target != null && target.IsValidAndNotDestroyed && target.UnitClass.CanFireAt && target != orderedUnit && (target.GetFleet() == null || target.GetFleet() != orderedUnit.GetFleet()) && target.Destructable != null)
			{
				return !target.IsDocked;
			}
			return false;
		}

		public static bool CanOrderCollectTarget(NewOrderTarget newOrderTarget, Unit targetUnit)
		{
			foreach (Unit allUnit in newOrderTarget.AllUnits)
			{
				if (CanOrderCollectTarget(allUnit, targetUnit))
				{
					return true;
				}
			}
			return false;
		}

		public static bool CanOrderCollectTarget(Unit unit, Unit target)
		{
			if (unit.CargoBayComponent != null && unit.HasTractorBeam() && target != null && target.IsValidAndNotDestroyed && target.UnitType == UnitType.Cargo)
			{
				return target.CargoComponent.Quantity > 0;
			}
			return false;
		}

		public static void OrderAutonomousTrade(NewOrderTarget newOrderTarget, bool stack, Action<AutonomousTradeOrder> setupAction = null)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderAutonomousTrade(item, stack, setupAction);
			}
		}

		public static void OrderAutonomousTrade(Fleet fleet, bool stack, Action<AutonomousTradeOrder> setupAction = null)
		{
			AutonomousTradeOrder autonomousTradeOrder = UnityObjectHelper.NewGameObject<AutonomousTradeOrder>();
			autonomousTradeOrder.AllowTimeout = false;
			setupAction?.Invoke(autonomousTradeOrder);
			SetFleetOrder(fleet, autonomousTradeOrder, stack);
		}

		public static void OrderSellCargo(NewOrderTarget newOrderTarget, bool stack, Action<SellCargoOrder> setupAction = null)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderSellCargo(item, stack, setupAction);
			}
		}

		public static void OrderSellCargo(Fleet fleet, bool stack, Action<SellCargoOrder> setupAction = null)
		{
			SellCargoOrder sellCargoOrder = UnityObjectHelper.NewGameObject<SellCargoOrder>();
			sellCargoOrder.AllowTimeout = false;
			setupAction?.Invoke(sellCargoOrder);
			SetFleetOrder(fleet, sellCargoOrder, stack);
		}

		public static void OrderFollowTarget(NewOrderTarget newOrderTarget, Unit target, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderFollowTarget(item, target, stack);
			}
		}

		public static void OrderFollowTarget(Fleet fleet, Unit target, bool stack)
		{
			MoveToOrder moveToOrder = UnityObjectHelper.NewGameObject<MoveToOrder>();
			moveToOrder.Target = new SectorTarget
			{
				TargetUnit = target,
				HadSceneObject = true
			};
			moveToOrder.MatchTargetOrientation = true;
			moveToOrder.AllowTimeout = true;
			moveToOrder.PreferredRelativeVectorFromTarget = Vector3.back;
			moveToOrder.CompleteOnReachTarget = false;
			SetFleetOrder(fleet, moveToOrder, stack);
		}

		public static void DisbandPlayerFleet(Fleet fleet)
		{
			if (fleet == null)
			{
				Debug.LogError("Null fleet");
				return;
			}
			if (EngineASX.Instance.LocalUnit.GetFleet() == fleet && EngineASX.Instance.PlayerUnitAutoPilotEnabled)
			{
				EngineASX.Instance.LocalUnit.Components.PilotPerson = null;
			}
			fleet.SafeDestroy();
		}

		public static bool CanOrderProtectTarget(NewOrderTarget newOrderTarget, Unit targetUnit)
		{
			if (targetUnit == null)
			{
				return false;
			}
			if (targetUnit.Destructable == null)
			{
				return false;
			}
			if (newOrderTarget.IsUnitOrdered(targetUnit))
			{
				return false;
			}
			if (!newOrderTarget.AllUnitsMobile)
			{
				return false;
			}
			if (targetUnit.IsHostileToOrAlwaysHostileToTwoWay(newOrderTarget.Faction))
			{
				return false;
			}
			return true;
		}

		public static bool CanOrderProtectTarget(Fleet fleet, Unit targetUnit)
		{
			if (targetUnit != null && targetUnit.GetFleet() != fleet && fleet.IsMobile)
			{
				return !targetUnit.IsHostileToOrAlwaysHostileToTwoWay(fleet.Faction);
			}
			return false;
		}

		public static void OrderWaitForAutoRepair(NewOrderTarget newOrderTarget, bool stack)
		{
			foreach (Fleet item in newOrderTarget.CreateAndReturnFleets())
			{
				OrderWaitForAutoRepair(item, stack);
			}
		}

		public static void OrderWaitForAutoRepair(Fleet fleet, bool stack)
		{
			WaitForAutoRepairOrder waitForAutoRepairOrder = UnityObjectHelper.NewGameObject<WaitForAutoRepairOrder>();
			waitForAutoRepairOrder.ComponentsConditionThreshold = 1f;
			waitForAutoRepairOrder.HullConditionThreshold = 1f;
			waitForAutoRepairOrder.ShieldConditionThreshold = 1f;
			SetFleetOrder(fleet, waitForAutoRepairOrder, stack);
		}

		public static List<Sector> GetPlayerUniverseMapPickableSectors()
		{
			return EngineASX.Instance.LocalFaction.Intel.GetCopyOfDiscoveredSectors().ToList();
		}

		public static List<Sector> GetPlayerUniverseMapPickableSectorsWithEnabledNavigation()
		{
			return (from e in EngineASX.Instance.LocalFaction.Intel.GetCopyOfDiscoveredSectors()
				where !e.IsExcludedFromPlayerNavigation()
				select e).ToList();
		}

		public static void OrderUndock(NewOrderTarget orderTarget, bool stack)
		{
			foreach (Fleet item in orderTarget.CreateAndReturnFleets())
			{
				OrderUndock(item, stack);
			}
		}

		public static void OrderUndock(Fleet fleet, bool stack)
		{
			UndockOrder undockOrder = UnityObjectHelper.NewGameObject<UndockOrder>();
			undockOrder.AllowTimeout = false;
			SetFleetOrder(fleet, undockOrder, stack: true);
		}

		public static void OrderExplore(NewOrderTarget orderTarget, bool stack)
		{
			foreach (Fleet item in orderTarget.CreateAndReturnFleets())
			{
				OrderExplore(item, stack);
			}
		}

		public static void OrderExplore(Fleet fleet, bool stack)
		{
			ExploreOrder exploreOrder = UnityObjectHelper.NewGameObject<ExploreOrder>();
			exploreOrder.AllowTimeout = false;
			SetFleetOrder(fleet, exploreOrder, stack: true);
		}

		public static Fleet CreateFleetForUnits(Sector sector, Vector3 sectorPosition, IEnumerable<Unit> units)
		{
			Fleet fleet = CreateAndInitPlayerFleet(EngineASX.Instance.LocalFaction, sector, sectorPosition, EngineASX.Instance.PlayerFleetPrefab);

			// Null means this copy may not make one, which is a client. Handing
			// the ships over instead, rather than quietly dropping them, is
			// what lets the host build the fleet when the order arrives.
			if (fleet == null)
			{
				FleetOrderIntents.NoteLooseUnits(IdsOf(units));

				return null;
			}

			foreach (Unit unit in units)
			{
				FindOrCreateNpc(unit).Fleet = fleet;
			}
			return fleet;
		}

		/// <summary>
		/// Hands ships to whoever owns the world, to be put in a fleet there.
		/// </summary>
		/// <remarks>
		/// For a copy that may not build fleets. The ships ride along with
		/// the next order and the host builds one when it carries it out,
		/// which is the same thing the host would have done had the click
		/// happened on its machine.
		/// </remarks>
		public static void NoteShipsForTheHost(IEnumerable<Unit> units)
		{
			FleetOrderIntents.NoteLooseUnits(IdsOf(units));
		}

		/// <summary>The ids of some units, skipping the ones that are gone.</summary>
		private static List<int> IdsOf(IEnumerable<Unit> units)
		{
			List<int> ids = new List<int>();

			if (units == null)
			{
				return ids;
			}

			foreach (Unit unit in units)
			{
				if (unit != null && !unit.IsDestroyed)
				{
					ids.Add(unit.UniqueId);
				}
			}

			return ids;
		}
	}
}
