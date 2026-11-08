using System.Collections.Generic;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveSellCargoOrder : ActiveFleetOrder
	{
		private const float timeBeforeFindBuyer = 4f;

		private CargoClass sellCargoClass;

		private CargoTrader curTraderTarget;

		private float nextBuyerFind;

		public SellCargoOrder SellCargoObjective;

		private double sellExpireTime;

		private ActiveSellCargoOrderState state;

		public ActiveSellCargoOrderState State
		{
			get
			{
				return state;
			}
			set
			{
				if (state == value)
				{
					return;
				}
				state = value;
				if (state == ActiveSellCargoOrderState.Selling)
				{
					IsIdle = true;
					if (SellCargoObjective.CustomSellCargoTime > 0f)
					{
						sellExpireTime = Engine.ScenarioElapsedTime + (double)SellCargoObjective.CustomSellCargoTime;
					}
					else
					{
						sellExpireTime = ActiveTradeOrder.GetTradeCompletionTime(fleet, Engine);
					}
				}
				IsIdle = state == ActiveSellCargoOrderState.None;
				ResetTargetPosition();
			}
		}

		public CargoClass SellCargoClass
		{
			get
			{
				return sellCargoClass;
			}
			set
			{
				sellCargoClass = value;
			}
		}

		public double SellExpireTime
		{
			get
			{
				return sellExpireTime;
			}
			set
			{
				sellExpireTime = value;
			}
		}

		public CargoTrader TraderTarget
		{
			get
			{
				return curTraderTarget;
			}
			set
			{
				curTraderTarget = value;
			}
		}

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			switch (state)
			{
			case ActiveSellCargoOrderState.None:
				if (sellCargoClass != null)
				{
					return "Looking for buyer of " + sellCargoClass.GetShortNameElseLong();
				}
				return "Looking for buyer";
			case ActiveSellCargoOrderState.MoveToDock:
				if (curTraderTarget != null)
				{
					if (sellCargoClass != null)
					{
						return "Sell " + sellCargoClass.GetShortNameElseLong() + " at " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(localFaction, fleet.Sector, curTraderTarget.Unit, colourByHostility: true, shortName: true);
					}
					return $"Move to {UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(localFaction, fleet.Sector, curTraderTarget.Unit, colourByHostility: true, shortName: true)}";
				}
				break;
			case ActiveSellCargoOrderState.Selling:
				if (sellCargoClass != null)
				{
					return "Transferring " + sellCargoClass.GetShortNameElseLong() + " to dock";
				}
				return "Transferring cargo to dock";
			}
			return base.GetStatusTextInternal(localFaction);
		}

		public CargoClass FindBestCargoToSell(out float volume)
		{
			volume = -1f;
			if (SellCargoObjective.SellOnlyListedCargos)
			{
				return FindBestCargoToSell(SellCargoObjective.SellCargoClasses, includeEquipment: true, checkFactionWillTrade: false, out volume);
			}
			return FindBestCargoToSell(fleet.GetCargoBayClasses(), SellCargoObjective.SellEquipment, checkFactionWillTrade: true, out volume);
		}

		public CargoClass FindBestCargoToSell(IEnumerable<CargoClass> cargos, bool includeEquipment, bool checkFactionWillTrade, out float volume)
		{
			CargoClass cargoClass = null;
			volume = -1f;
			float num = 0f;
			foreach (CargoClass cargo in cargos)
			{
				if ((!cargo.IsEquipment | includeEquipment) && (!checkFactionWillTrade || fleet.Faction.WillTradeCargoType(cargo)))
				{
					float num2 = (float)fleet.GetCargoCount(cargo) * cargo.Volume;
					if (num2 > 0f && (cargoClass == null || num2 > num))
					{
						cargoClass = cargo;
						num = num2;
					}
				}
			}
			return cargoClass;
		}

		protected override void onInit()
		{
			base.onInit();
			state = ActiveSellCargoOrderState.None;
		}

		protected override void tick(float elapsedTime)
		{
			switch (state)
			{
			case ActiveSellCargoOrderState.None:
			{
				if (!(Engine.ScenarioElapsedTime > sellExpireTime) || !(Time.time > nextBuyerFind))
				{
					break;
				}
				float volume = 0f;
				SellCargoClass = FindBestCargoToSell(out volume);
				if (sellCargoClass != null)
				{
					if (SellCargoObjective.ManualBuyer != null)
					{
						curTraderTarget = SellCargoObjective.ManualBuyer;
					}
					else
					{
						curTraderTarget = FindBestBuyer(sellCargoClass);
					}
					if (curTraderTarget != null)
					{
						State = ActiveSellCargoOrderState.MoveToDock;
					}
					else
					{
						if (!fleet.IsOwnedByPlayer)
						{
							fleet.DumpTradeableCargoOfType(sellCargoClass);
						}
						if (SellCargoObjective.CompleteWhenNoBuyerFound)
						{
							OnComplete();
						}
					}
				}
				else if (SellCargoObjective.CompleteWhenNoCargoToSell)
				{
					OnComplete();
				}
				nextBuyerFind = Time.time + 4f;
				break;
			}
			case ActiveSellCargoOrderState.Selling:
				if (curTraderTarget != null)
				{
					if (!(Engine.ScenarioElapsedTime > sellExpireTime))
					{
						break;
					}
					Unit unit = curTraderTarget.Unit;
					if (unit != null)
					{
						int cargoCount = fleet.GetCargoCount(sellCargoClass);
						if (ActiveTradeOrder.CanSellCargo(fleet.Faction, sellCargoClass, cargoCount, 0f, unit, out var maxSellable))
						{
							foreach (NpcPilot npcPilot in fleet.NpcPilots)
							{
								int num = Mathf.Min(maxSellable, npcPilot.CurrentUnit.GetCargoCountOf(sellCargoClass));
								if (num > 0)
								{
									maxSellable -= num;
									ActiveTradeOrder.SellCargo(fleet.Faction, npcPilot.CurrentUnit, sellCargoClass, num, unit, CreditsSource);
								}
							}
							if (fleet.LeaderUnit != null)
							{
								fleet.LeaderUnit.TryPlayCargoDoorAudio();
							}
							fleet.InvalidateCargoUsageStats();
							State = ActiveSellCargoOrderState.None;
						}
						else
						{
							IsIdle = true;
						}
					}
					else
					{
						Debug.LogError("Trying to sell cargo but no longer docked", this);
					}
				}
				else
				{
					State = ActiveSellCargoOrderState.None;
				}
				break;
			case ActiveSellCargoOrderState.MoveToDock:
				if (curTraderTarget != null)
				{
					if (fleet.AllUnitsAtDock(curTraderTarget.Unit))
					{
						State = ActiveSellCargoOrderState.Selling;
					}
				}
				else
				{
					State = ActiveSellCargoOrderState.None;
				}
				break;
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			if (state == ActiveSellCargoOrderState.MoveToDock)
			{
				if (curTraderTarget != null)
				{
					State = ActiveSellCargoOrderState.Selling;
				}
				else
				{
					State = ActiveSellCargoOrderState.None;
				}
			}
		}

		protected override void onFinalizeObjective()
		{
			base.onFinalizeObjective();
			curTraderTarget = null;
			sellCargoClass = null;
			State = ActiveSellCargoOrderState.None;
		}

		protected override void resetTargetPosition()
		{
			switch (state)
			{
			case ActiveSellCargoOrderState.MoveToDock:
				if (curTraderTarget != null)
				{
					fleet.SetTargetToDock(this, curTraderTarget.Unit);
				}
				break;
			case ActiveSellCargoOrderState.Selling:
				if (curTraderTarget != null)
				{
					fleet.SetTargetToDock(this, curTraderTarget.Unit);
				}
				break;
			}
		}

		private CargoTrader FindBestBuyer(CargoClass c)
		{
			CargoTrader result = null;
			if (c != null)
			{
				float bestTraderPriceMultiplier = 0f;
				float bestTraderScore = 0f;
				int actualMaxJumpDist = GetActualMaxJumpDist();
				int cargoCount = fleet.GetCargoCount(sellCargoClass);
				float cachedMinMoveSpeed = fleet.GetCachedMinMoveSpeed();
				float distanceScorePerMetreTravelled = fleet.GetDistanceScorePerMetreTravelled(cachedMinMoveSpeed);
				List<ShipHullType> npcShipHullTypes = fleet.NpcShipHullTypes;
				result = TradeSearchOperation.GetBestSellLocation(Engine, fleet.Faction, fleet.GetHomeSectorOrCurrent(), fleet.Sector, fleet.SectorPosition, actualMaxJumpDist, distanceScorePerMetreTravelled, sellCargoClass, cargoCount, SellCargoObjective.MinBuyPriceMultiplier, null, npcShipHullTypes, ignoreDistanceCost: false, ignoreProfitability: false, out bestTraderPriceMultiplier, out bestTraderScore, out var _);
			}
			return result;
		}

		public override DockedPreference GetPreferToDockWhenIdle()
		{
			if (state == ActiveSellCargoOrderState.Selling)
			{
				return DockedPreference.Dock;
			}
			return base.GetPreferToDockWhenIdle();
		}

		public override void OnNpcUnableToDockAtNavpoint(NpcPilot controller, Unit unit, UnableToDockReason unableToDockReason)
		{
			if (state != ActiveSellCargoOrderState.None)
			{
				switch (unableToDockReason)
				{
				case UnableToDockReason.Refused:
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"Fleet {fleet} refused permission to dock. Invalidating state of ${this}", this, 1);
					}
					State = ActiveSellCargoOrderState.None;
					return;
				case UnableToDockReason.DockingBaysFull:
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"Fleet {fleet} unable to dock as bays full. Invalidating state of ${this}", this, 1);
					}
					State = ActiveSellCargoOrderState.None;
					return;
				}
			}
			base.OnNpcUnableToDockAtNavpoint(controller, unit, unableToDockReason);
		}

		public override bool RequestUndock(NpcPilot npcPilot)
		{
			return state != ActiveSellCargoOrderState.Selling;
		}
	}
}
