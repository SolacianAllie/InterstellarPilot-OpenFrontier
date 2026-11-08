using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public class MineSearchOperation
	{
		private float bestMineScore = float.MinValue;

		private float bestMineDistance = float.MinValue;

		private bool hasFinished;

		private Unit result;

		private Queue<MineSearchOperationItem> searchItems = new Queue<MineSearchOperationItem>(100);

		private int searchItemsInitialCount;

		private HashSet<int> searchedSectors = new HashSet<int>(8);

		private PriorityQueue<MineSearchSectorItem, float> remainingSectorsToSearch = new PriorityQueue<MineSearchSectorItem, float>(8);

		private MineSearchSectorItem currentSectorItem;

		private Sector startSector;

		private Vector3 startPosition = Vector3.zero;

		private Faction faction;

		private int maxJumpDistance = 99;

		public TradeSearchFleetStats FleetStats { get; set; }

		public float BestMineDistance => bestMineDistance;

		public Unit Result => result;

		public bool HasFinished => hasFinished;

		public void Initialise(Sector startSector, Vector3 startPosition, Faction faction, int maxJumpDistance)
		{
			hasFinished = false;
			bestMineScore = float.MinValue;
			bestMineDistance = float.MinValue;
			result = null;
			searchItemsInitialCount = 0;
			this.startSector = startSector;
			this.startPosition = startPosition;
			this.maxJumpDistance = maxJumpDistance;
			this.faction = faction;
		}

		public void InitialiseAndStartSearch(Sector startSector, Vector3 startPosition, Faction faction, int maxJumpDistance)
		{
			Initialise(startSector, startPosition, faction, maxJumpDistance);
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
				StartSearchItem(new MineSearchSectorItem(startSector, 0f, startPosition, 0));
			}
			searchItemsInitialCount = searchItems.Count;
		}

		private void StartSearchItem(MineSearchSectorItem item)
		{
			if (faction == null)
			{
				return;
			}
			currentSectorItem = item;
			Sector sector = currentSectorItem.Sector;
			Vector3 entrySectorPosition = currentSectorItem.EntrySectorPosition;
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Asteroid);
			if (unitsByType != null)
			{
				foreach (Unit item2 in unitsByType)
				{
					searchItems.Enqueue(new MineSearchOperationItem
					{
						Unit = item2
					});
				}
			}
			if (item.JumpDistanceToThisSector < maxJumpDistance)
			{
				foreach (SectorNeighbour neighbour in sector.Neighbours)
				{
					if (!searchedSectors.Contains(neighbour.Sector.UniqueId) && neighbour.IsStableConnection && faction.Intel.CanTraverseIntoNeighbour(neighbour))
					{
						float num = Vector3.Distance(neighbour.ConnectingGate.Unit.SectorPosition, entrySectorPosition);
						float num2 = currentSectorItem.DistanceToThisSector + num;
						MineSearchSectorItem value = new MineSearchSectorItem(neighbour.Sector, num2, neighbour.ConnectingGate.GetTargetSectorPosition(), currentSectorItem.JumpDistanceToThisSector + 1);
						remainingSectorsToSearch.Enqueue(value, 0f - num2);
					}
				}
			}
			searchedSectors.Add(sector.UniqueId);
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
			return Mathf.CeilToInt((float)EngineASX.Instance.PerformanceSettings.AIMineSearchSearchesPerSecond * elapsedTime);
		}

		private void ProcessNextItem()
		{
			MineSearchOperationItem mineSearchOperationItem = searchItems.Dequeue();
			if (!(mineSearchOperationItem.Unit == null) && mineSearchOperationItem.Unit.IsValidAndNotDestroyed && faction.Intel.IsUnitDiscovered(mineSearchOperationItem.Unit))
			{
				float num = Vector3.Distance(currentSectorItem.EntrySectorPosition, mineSearchOperationItem.Unit.SectorPosition);
				float num2 = (faction.IsPlayerFaction ? GameController.Instance.GameSettings.NpcMiningSettings.SearchDistanceFuzzinessPercentagePlayer : GameController.Instance.GameSettings.NpcMiningSettings.SearchDistanceFuzzinessPercentage);
				num *= 1f + Random.value * num2;
				if (!faction.IsPlayerFaction)
				{
					num += Random.value * GameController.Instance.GameSettings.NpcMiningSettings.MinFuzziness;
				}
				num += currentSectorItem.DistanceToThisSector;
				float num3 = 0f - num;
				if (num3 > bestMineScore)
				{
					result = mineSearchOperationItem.Unit;
					bestMineDistance = num;
					bestMineScore = num3;
				}
			}
		}

		private void OnNoMoreSearchItems()
		{
			if (result != null)
			{
				hasFinished = true;
			}
			else if (remainingSectorsToSearch.Count > 0)
			{
				MineSearchSectorItem value = remainingSectorsToSearch.Dequeue().Value;
				StartSearchItem(value);
			}
			else
			{
				hasFinished = true;
			}
		}
	}
}
