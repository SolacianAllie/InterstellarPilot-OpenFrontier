using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public class UnitSearchOperation
	{
		private float bestScore = float.MinValue;

		private float bestDistance = float.MinValue;

		private bool hasFinished;

		private Unit result;

		protected Queue<UnitSearchOperationItem> searchItems = new Queue<UnitSearchOperationItem>(100);

		private int searchItemsInitialCount;

		private HashSet<int> searchedSectors = new HashSet<int>(8);

		private PriorityQueue<UnitSearchSectorItem, float> remainingSectorsToSearch = new PriorityQueue<UnitSearchSectorItem, float>(8);

		private UnitSearchSectorItem currentSectorItem;

		protected Sector startSector;

		protected Sector homeSector;

		protected Vector3 startPosition = Vector3.zero;

		protected Faction faction;

		public int MaxJumpDistance { get; set; } = int.MaxValue;

		public int SearchesPerSeconds { get; set; } = 50;

		public Func<Unit, float, float?> CustomScorer { get; set; }

		public bool SearchAllSectors { get; set; }

		public float BestDistance => bestDistance;

		public Unit Result => result;

		public bool HasFinished => hasFinished;

		public void Initialise(Sector startSector, Sector homeSector, Vector3 startPosition, Faction faction, int maxJumpDistance)
		{
			hasFinished = false;
			bestScore = float.MinValue;
			bestDistance = float.MinValue;
			result = null;
			searchItemsInitialCount = 0;
			this.startSector = startSector;
			this.startPosition = startPosition;
			this.faction = faction;
			this.homeSector = homeSector;
			MaxJumpDistance = maxJumpDistance;
		}

		public void InitialiseAndStartSearch(Sector startSector, Sector homeSector, Vector3 startPosition, Faction faction, int maxJumpDistance)
		{
			Initialise(startSector, homeSector, startPosition, faction, maxJumpDistance);
			PopulateSearchItems();
			if (searchItems.Count == 0)
			{
				OnNoMoreSearchItems();
			}
		}

		public void PopulateSearchItems()
		{
			searchItems.Clear();
			if (startSector != null)
			{
				StartSearchItem(new UnitSearchSectorItem(startSector, 0f, startPosition, 0));
			}
			searchItemsInitialCount = searchItems.Count;
		}

		private void StartSearchItem(UnitSearchSectorItem item)
		{
			if (faction == null)
			{
				return;
			}
			currentSectorItem = item;
			Sector sector = currentSectorItem.Sector;
			Vector3 entrySectorPosition = currentSectorItem.EntrySectorPosition;
			QueueUnitsInSector(sector);
			foreach (SectorNeighbour neighbour in sector.Neighbours)
			{
				if (!searchedSectors.Contains(neighbour.Sector.UniqueId) && neighbour.IsStableConnection && faction.Intel.HasWormholeBeenEntered(neighbour.ConnectingGate))
				{
					int minSectorJumpCount = faction.Intel.GetMinSectorJumpCount(homeSector, neighbour.Sector);
					if (minSectorJumpCount >= 0 && minSectorJumpCount <= MaxJumpDistance)
					{
						float num = Vector3.Distance(neighbour.ConnectingGate.Unit.SectorPosition, entrySectorPosition);
						float num2 = currentSectorItem.DistanceToThisSector + num;
						remainingSectorsToSearch.Enqueue(new UnitSearchSectorItem(neighbour.Sector, num2, neighbour.ConnectingGate.GetTargetSectorPosition(), currentSectorItem.JumpDistanceToThisSector + 1), 0f - num2);
					}
				}
			}
			searchedSectors.Add(sector.UniqueId);
		}

		protected virtual void QueueUnitsInSector(Sector sector)
		{
			List<Unit> stations = GetStations(sector);
			if (stations == null)
			{
				return;
			}
			foreach (Unit item in stations)
			{
				searchItems.Enqueue(new UnitSearchOperationItem
				{
					Unit = item
				});
			}
		}

		private static List<Unit> GetStations(Sector sector)
		{
			return sector.GetUnitsByType(UnitType.Station);
		}

		public float GetSearchPercentageComplete()
		{
			if (searchItemsInitialCount == 0)
			{
				return 1f;
			}
			return 1f - (float)searchItems.Count / (float)searchItemsInitialCount;
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
			int num = Mathf.Min(GetProcessItemCount(elapsedTime), searchItems.Count);
			for (int i = 0; i < num; i++)
			{
				ProcessNextItem();
			}
			if (searchItems.Count == 0)
			{
				OnNoMoreSearchItems();
			}
		}

		private int GetProcessItemCount(float elapsedTime)
		{
			return Mathf.CeilToInt((float)SearchesPerSeconds * elapsedTime);
		}

		private void ProcessNextItem()
		{
			UnitSearchOperationItem unitSearchOperationItem = searchItems.Dequeue();
			if (!(unitSearchOperationItem.Unit == null) && unitSearchOperationItem.Unit.IsValidAndNotDestroyed && faction.Intel.IsUnitDiscovered(unitSearchOperationItem.Unit))
			{
				float distance = Vector3.Distance(currentSectorItem.EntrySectorPosition, unitSearchOperationItem.Unit.SectorPosition);
				float? num = ScoreUnit(unitSearchOperationItem.Unit, distance);
				if (num.HasValue && num > bestScore)
				{
					result = unitSearchOperationItem.Unit;
					bestDistance = distance;
					bestScore = num.Value;
				}
			}
		}

		protected virtual float? ScoreUnit(Unit unit, float distance)
		{
			if (CustomScorer != null)
			{
				return CustomScorer(unit, distance);
			}
			return 0f - distance;
		}

		private void OnNoMoreSearchItems()
		{
			if (!SearchAllSectors && result != null)
			{
				hasFinished = true;
			}
			else if (remainingSectorsToSearch.Count > 0)
			{
				UnitSearchSectorItem value = remainingSectorsToSearch.Dequeue().Value;
				StartSearchItem(value);
			}
			else
			{
				hasFinished = true;
			}
		}
	}
}
