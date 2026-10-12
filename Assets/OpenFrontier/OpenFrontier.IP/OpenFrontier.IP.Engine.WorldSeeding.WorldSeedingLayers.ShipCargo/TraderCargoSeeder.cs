using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.ShipCargo
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class TraderCargoSeeder : MonoBehaviour
	{
		public TraderCargoSeederSettings ShipCargoSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Spawning fleet cargo...", this, 1);
			}
			ShipCargoSeederSettings = world.Seeder.Settings.ShipCargoSeederSettings;
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (!faction.IsAIFactionType)
				{
					continue;
				}
				foreach (Fleet fleet in faction.Fleets)
				{
					if (!fleet.ExcludeFromFactionAI)
					{
						TrySpawnFleetWithRandomCargoOnNewGame(fleet, EngineASX.Instance.EconomySettings.TraderInitWithCargoProbability);
					}
				}
			}
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Completed spawning fleet cargo", this, 1);
			}
		}

		public void TrySpawnFleetWithRandomCargoOnNewGame(Fleet fleet, float probabilityOfCargo)
		{
			if (!(fleet.ActiveOrder is ActiveAutonomousTradeOrder activeAutonomousTradeOrder))
			{
				return;
			}
			if (activeAutonomousTradeOrder.TradeRoute == null)
			{
				CompositeTradeSearchOperation compositeTradeSearchOperation = new CompositeTradeSearchOperation
				{
					TradeSearchOptions = new TradeSearchOptions
					{
						IgnoreDistanceCost = true,
						IgnoreProfitability = false
					}
				};
				compositeTradeSearchOperation.Initialise(activeAutonomousTradeOrder);
				compositeTradeSearchOperation.ProcessUntilCompletion();
				activeAutonomousTradeOrder.TradeRoute = compositeTradeSearchOperation.Result;
			}
			if (activeAutonomousTradeOrder.TradeRoute == null || !(Random.value < probabilityOfCargo))
			{
				return;
			}
			CargoBayComponent cargoBayComponent = activeAutonomousTradeOrder.TradeRoute.BuyLocation.Unit.CargoBayComponent;
			float freeCargoSpace = fleet.GetFreeCargoSpace();
			int countOf = cargoBayComponent.GetCountOf(activeAutonomousTradeOrder.TradeRoute.CargoClass);
			int maxPurchasable = TradeSearchOperation.GetMaxPurchasable(activeAutonomousTradeOrder.TradeRoute.CargoClass, countOf, freeCargoSpace);
			int num = Mathf.RoundToInt(Mathf.Lerp(fleet.Engine.EconomySettings.TraderInitWithCargoMinVolume, fleet.Engine.EconomySettings.TraderInitWithCargoMaxVolume, Random.value) * (float)maxPurchasable);
			if (num <= 0)
			{
				return;
			}
			foreach (NpcPilot npcPilot in fleet.NpcPilots)
			{
				int amount = (int)(npcPilot.CurrentUnit.CargoBayComponent.FreeSpace / freeCargoSpace * (float)num);
				npcPilot.CurrentUnit.CargoBayComponent.AddToCargoIfFits(activeAutonomousTradeOrder.TradeRoute.CargoClass, amount);
			}
			activeAutonomousTradeOrder.CurrentState = ActiveTradeOrderState.GoSell;
			if (ShipCargoSeederSettings.RemoveCargoFromSource)
			{
				activeAutonomousTradeOrder.TradeRoute.BuyLocation.Unit.CargoBayComponent.AddToCargo(activeAutonomousTradeOrder.TradeRoute.CargoClass, -num, ignoreCapacity: false);
			}
		}
	}
}
