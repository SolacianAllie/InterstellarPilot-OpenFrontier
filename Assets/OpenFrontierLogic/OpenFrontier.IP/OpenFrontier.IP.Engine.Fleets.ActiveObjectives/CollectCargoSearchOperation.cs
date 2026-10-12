using System.Collections.Generic;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets.ActiveObjectives
{
	public class CollectCargoSearchOperation
	{
		private bool hasFinished;

		private Unit bestCargo;

		private float bestScore;

		private Fleet fleet;

		private Queue<CollectCargoSearchOperationItem> searchItems = new Queue<CollectCargoSearchOperationItem>(100);

		private Vector3 sectorPosition = Vector3.zero;

		private Sector sectorTarget;

		private float maxRange = 1500f;

		private float largestCargoSpace;

		public Unit BestCargoUnit => bestCargo;

		public float BestScore => bestScore;

		public float MaxRange
		{
			get
			{
				return maxRange;
			}
			set
			{
				maxRange = value;
			}
		}

		public bool HasFinished => hasFinished;

		public void Initialise(Fleet fleet, Sector sectorTarget, Vector3 sectorTargetPosition)
		{
			hasFinished = false;
			bestCargo = null;
			bestScore = 0f;
			this.sectorTarget = sectorTarget;
			this.fleet = fleet;
			largestCargoSpace = 0f;
			sectorPosition = sectorTargetPosition;
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if (ship != null && ship.Unit != null && ship.Unit.IsValidAndNotDestroyed && ship.CargoBayComponent != null && ship.TractorTurret != null)
				{
					largestCargoSpace = Mathf.Max(largestCargoSpace, ship.CargoBayComponent.FreeSpace);
				}
			}
		}

		public void InitialiseAndPopulateSearchItem(Fleet fleet, Sector sectorTarget, Vector3 sectorTargetPosition)
		{
			Initialise(fleet, sectorTarget, sectorTargetPosition);
			PopulateSearchItems();
			if (searchItems.Count == 0)
			{
				hasFinished = true;
			}
		}

		public void PopulateSearchItems()
		{
			searchItems.Clear();
			Faction faction = fleet.Faction;
			if (!(faction != null))
			{
				return;
			}
			IEnumerable<int> allDiscoveredUnitIdsOfType = faction.Intel.GetAllDiscoveredUnitIdsOfType(UnitType.Cargo);
			if (allDiscoveredUnitIdsOfType == null)
			{
				return;
			}
			foreach (int item in allDiscoveredUnitIdsOfType)
			{
				Unit unitByid = EngineASX.Instance.GetUnitByid(item);
				if (unitByid != null)
				{
					searchItems.Enqueue(new CollectCargoSearchOperationItem(unitByid));
				}
			}
		}

		public void ProcessUntilCompletion()
		{
			while (!hasFinished)
			{
				Process(0.1f);
			}
		}

		public void Process(float elapsedTime)
		{
			int tradeSearches = GetTradeSearches(elapsedTime);
			for (int i = 0; i < tradeSearches; i++)
			{
				if (searchItems.Count > 0)
				{
					ProcessNextItem();
					continue;
				}
				OnNoMoreSearchItems();
				break;
			}
		}

		private void ProcessNextItem()
		{
			CollectCargoSearchOperationItem collectCargoSearchOperationItem = searchItems.Dequeue();
			if (!IsCargoUnitValidTarget(collectCargoSearchOperationItem.CargoUnit))
			{
				return;
			}
			float distanceIgnoreY = Maths.GetDistanceIgnoreY(collectCargoSearchOperationItem.CargoUnit.SectorPosition, sectorPosition);
			if (distanceIgnoreY < maxRange)
			{
				float num = 0f - distanceIgnoreY;
				num += (float)collectCargoSearchOperationItem.CargoUnit.CargoComponent.CreditsValue;
				if (bestCargo == null || num > bestScore)
				{
					bestCargo = collectCargoSearchOperationItem.CargoUnit;
					bestScore = num;
				}
			}
		}

		private int GetTradeSearches(float elapsedTime)
		{
			if (fleet.IsPlayerFaction())
			{
				return Mathf.CeilToInt(EngineASX.Instance.PerformanceSettings.AICargoSearchSearchesPerSecondPlayer * elapsedTime);
			}
			return Mathf.CeilToInt(EngineASX.Instance.PerformanceSettings.AICargoSearchSearchesPerSecond * elapsedTime);
		}

		private void OnNoMoreSearchItems()
		{
			hasFinished = true;
		}

		protected virtual bool IsCargoUnitValidTarget(Unit cargoUnit)
		{
			if (cargoUnit != null && cargoUnit.IsValidAndNotDestroyed)
			{
				if (cargoUnit.Sector != sectorTarget)
				{
					return false;
				}
				if (cargoUnit.CargoComponent == null)
				{
					return false;
				}
				if (cargoUnit.CargoComponent.Volume > largestCargoSpace * 0.9f)
				{
					return false;
				}
				if (cargoUnit.Tractorer == null)
				{
					CargoOwnership cargoOwnership = CollectCargoHelper.CalculateCargoOwnership(cargoUnit.CargoComponent, fleet.Faction);
					return fleet.ScoreCargoToCollect(null, cargoUnit, cargoUnit.CargoComponent.CargoClass, cargoUnit.CargoComponent.Quantity, cargoOwnership) > float.MinValue;
				}
			}
			return false;
		}
	}
}
