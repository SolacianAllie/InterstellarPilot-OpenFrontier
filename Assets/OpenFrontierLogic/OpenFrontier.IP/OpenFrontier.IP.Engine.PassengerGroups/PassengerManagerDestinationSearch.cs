using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.PassengerGroups
{
	public class PassengerManagerDestinationSearch
	{
		public struct PassengerManagerDestinationSearchSearchItem
		{
			public Unit unit;

			public PassengerManagerDestinationSearchSearchItem(Unit unit)
			{
				this = default;
				this.unit = unit;
			}
		}

		private Faction faction;

		private Sector startSector;

		private List<PassengerManagerDestinationSearchSearchItem> searchItems = new List<PassengerManagerDestinationSearchSearchItem>(20);

		private float bestDestinationUnitScore;

		private Unit bestDestinationUnit;

		private Unit currentUnit;

		public int MaxJumpDistance = 10;

		public int MaxSectorsConsidered = 3;

		public bool ScoreControlledSectors = true;

		private static List<Sector> sectorCache = new List<Sector>(8);

		public Unit BestDestinationUnit => bestDestinationUnit;

		public float BestDestinationUnitScore => bestDestinationUnitScore;

		public bool HasFinished => searchItems.Count == 0;

		public void Initialise(Unit currentUnit)
		{
			bestDestinationUnit = null;
			bestDestinationUnitScore = 0f;
			searchItems.Clear();
			faction = currentUnit.Faction;
			this.currentUnit = currentUnit;
			startSector = currentUnit.Sector;
			PopulateSearchItems();
		}

		private void PopulateSearchItems()
		{
			SectorFinder.FindNavigableDiscoveredSectorsWithinJumpDistanceOf(currentUnit.Sector, MaxJumpDistance, currentUnit.Faction);
			if (SectorFinder.Results.Count <= 0)
			{
				return;
			}
			sectorCache.Clear();
			int num = Mathf.Min(MaxSectorsConsidered, SectorFinder.Results.Count);
			for (int i = 0; i < num; i++)
			{
				int index = Random.Range(0, SectorFinder.Results.Count);
				sectorCache.Add(SectorFinder.Results[index].Sector);
				SectorFinder.Results.RemoveAt(index);
			}
			foreach (Sector item in sectorCache)
			{
				List<Unit> unitsByType = item.GetUnitsByType(UnitType.Station);
				if (unitsByType != null)
				{
					foreach (Unit item2 in unitsByType)
					{
						if (PassengerManager.CanUnitBeDestination(currentUnit, item2))
						{
							searchItems.Add(new PassengerManagerDestinationSearchSearchItem(item2));
						}
					}
				}
				if (faction.FactionType != FactionType.Empire)
				{
					continue;
				}
				List<Unit> unitsByType2 = item.GetUnitsByType(UnitType.Ship);
				if (unitsByType2 == null)
				{
					continue;
				}
				foreach (Unit item3 in unitsByType2)
				{
					if (PassengerManager.CanUnitBeDestination(currentUnit, item3))
					{
						searchItems.Add(new PassengerManagerDestinationSearchSearchItem(item3));
					}
				}
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
			return Mathf.Min(searchItems.Count, Mathf.Max(1, Mathf.CeilToInt(EngineASX.Instance.PerformanceSettings.PassengerGroupDestinationSearchItemsPerSecond * Time.deltaTime)));
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
			PassengerManagerDestinationSearchSearchItem passengerManagerDestinationSearchSearchItem = searchItems[searchItems.Count - 1];
			searchItems.RemoveAt(searchItems.Count - 1);
			float? num = ScoreUnit(passengerManagerDestinationSearchSearchItem.unit);
			if (num.HasValue && (bestDestinationUnit == null || num > bestDestinationUnitScore))
			{
				bestDestinationUnit = passengerManagerDestinationSearchSearchItem.unit;
				bestDestinationUnitScore = num.Value;
			}
		}

		private float? ScoreUnit(Unit unit)
		{
			float num = 0f;
			if (!unit.IsValidAndNotDestroyed)
			{
				return null;
			}
			if (unit.Faction == null)
			{
				return null;
			}
			if (!PassengerManager.CanUnitBeDestinationForFaction(faction, unit))
			{
				return null;
			}
			num -= faction.Virtue * unit.Sector.SecurityLevel;
			num += Random.value * 0.4f;
			num += unit.UnitClass.PassengerGroupCountMultiplier;
			if (unit.IsShip())
			{
				num -= Random.value * 10f;
			}
			return num;
		}
	}
}
