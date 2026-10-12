using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionEmpireExpansionSearch
	{
		public struct FactionEmpireExpansionSearchItem
		{
			public Sector Sector;

			public FactionEmpireExpansionSearchItem(Sector sector)
			{
				this = default;
				Sector = sector;
			}
		}

		private FactionAIBase factionAI;

		private List<FactionEmpireExpansionSearchItem> searchItems = new List<FactionEmpireExpansionSearchItem>(20);

		private float bestSectorScore;

		private Sector bestSector;

		public bool ScoreControlledSectors = true;

		public Sector BestSector => bestSector;

		public float BestSectorScore => bestSectorScore;

		public bool HasFinished => searchItems.Count == 0;

		public FactionAIBase FactionAI => factionAI;

		public void Initialise(FactionAIBase factionAI)
		{
			bestSector = null;
			bestSectorScore = 0f;
			searchItems.Clear();
			this.factionAI = factionAI;
			PopulateSearchItems();
		}

		private void PopulateSearchItems()
		{
			foreach (Sector controlledSector in factionAI.Faction.ControlledSectors)
			{
				foreach (SectorNeighbour neighbour in controlledSector.Neighbours)
				{
					if (neighbour.IsStableConnection && neighbour.Sector.ControllingFaction != factionAI.Faction && factionAI.Faction.Intel.CanTraverseIntoNeighbour(neighbour))
					{
						searchItems.Add(new FactionEmpireExpansionSearchItem(neighbour.Sector));
					}
				}
			}
			if (factionAI.Faction.HomeSector != null && factionAI.Faction.HomeSector.ControllingFaction == null)
			{
				searchItems.Add(new FactionEmpireExpansionSearchItem(factionAI.Faction.HomeSector));
			}
		}

		public void ProcessUntilCompletion()
		{
			while (!HasFinished)
			{
				Process();
			}
		}

		public void Process()
		{
			int itemsToProcess = GetItemsToProcess();
			ProcessItems(itemsToProcess);
		}

		private int GetItemsToProcess()
		{
			return Mathf.Min(searchItems.Count, Mathf.Max(1, Mathf.CeilToInt(EngineASX.Instance.PerformanceSettings.EmpireAIExpansionSearchItemsPerSecond * Time.deltaTime)));
		}

		public void ProcessItems(int processCount)
		{
			for (int i = 0; i < processCount; i++)
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

		private void OnNoMoreSearchItems()
		{
		}

		private void ProcessNextItem()
		{
			FactionEmpireExpansionSearchItem factionEmpireExpansionSearchItem = searchItems[searchItems.Count - 1];
			searchItems.RemoveAt(searchItems.Count - 1);
			float? num = ScoreSector(factionEmpireExpansionSearchItem.Sector);
			if (num.HasValue && (bestSector == null || num > bestSectorScore))
			{
				bestSector = factionEmpireExpansionSearchItem.Sector;
				bestSectorScore = num.Value;
			}
		}

		private float? ScoreSector(Sector sector)
		{
			float num = 0f;
			if (sector.ControllingFaction != null && !ScoreControlledSectors)
			{
				return null;
			}
			if (sector.ControllingFaction != null)
			{
				if (sector.ControllingFaction != factionAI.Faction)
				{
					num += (factionAI.Faction.Aggression * 2f - 1f) * 5f;
					float opinion = factionAI.Faction.GetOpinion(sector.ControllingFaction);
					num -= opinion * 5f;
				}
			}
			else
			{
				num++;
			}
			if (sector.HasPlanets)
			{
				num += 10f;
			}
			if (sector.HasAsteroidClusters)
			{
				num += 5f;
			}
			foreach (SectorNeighbour neighbour in sector.Neighbours)
			{
				if (neighbour.IsStableConnection && factionAI.Faction.Intel.IsSectorDiscovered(neighbour.Sector))
				{
					if (neighbour.Sector.ControllingFaction == factionAI.Faction)
					{
						num += 0.5f;
					}
					else
					{
						num = ((!(neighbour.Sector.ControllingFaction != null)) ? (num - 0.2f) : (num - 0.5f));
					}
				}
			}
			return num;
		}
	}
}
