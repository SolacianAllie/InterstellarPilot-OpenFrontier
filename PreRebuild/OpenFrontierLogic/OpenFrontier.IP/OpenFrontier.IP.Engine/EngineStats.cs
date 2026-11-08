using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class EngineStats : MonoBehaviour
	{
		public struct RetiredFactionInfo
		{
			public string Name;

			public FactionType FactionType;

			public double TimeOfRetire;

			public int NumShipsBuilt;

			public int NumShipsLost;
		}

		private StatsCargo allCargoStats = new StatsCargo();

		private Dictionary<int, StatsCargo> cargoStatLookup = new Dictionary<int, StatsCargo>();

		private List<StatsCargo> cargoStats = new List<StatsCargo>();

		private EngineASX engine;

		private Dictionary<UnitType, int> destroyedUnitCountsByType = new Dictionary<UnitType, int>();

		private Dictionary<int, int> destroyedUnitCountsByClass = new Dictionary<int, int>();

		private Dictionary<FactionType, int> retiredFactionsByType = new Dictionary<FactionType, int>();

		private double totalHullDamageReceived;

		private double totalShieldDamageReceived;

		private int shipsBuiltByAI;

		private List<RetiredFactionInfo> retiredFactionInfos = new List<RetiredFactionInfo>();

		public List<RetiredFactionInfo> RetiredFactionInfos => retiredFactionInfos;

		public double TotalHullDamageReceived
		{
			get
			{
				return totalHullDamageReceived;
			}
			set
			{
				totalHullDamageReceived = value;
			}
		}

		public double TotalShieldDamageReceived
		{
			get
			{
				return totalShieldDamageReceived;
			}
			set
			{
				totalShieldDamageReceived = value;
			}
		}

		public StatsCargo AllCargoStats => allCargoStats;

		public Dictionary<UnitType, int> DestroyedUnitsByType => destroyedUnitCountsByType;

		public Dictionary<int, int> DestroyedUnitCountsByClass => destroyedUnitCountsByClass;

		public int ShipsBuiltByAI => shipsBuiltByAI;

		public void Init()
		{
			engine = EngineASX.Instance;
			foreach (CargoClass cargoClass in engine.CargoClasses)
			{
				StatsCargo statsCargo = new StatsCargo();
				statsCargo.CargoType = cargoClass;
				cargoStats.Add(statsCargo);
				cargoStatLookup.Add(cargoClass.UniqueId, statsCargo);
			}
		}

		public StatsCargo GetCargoStats(int id)
		{
			return cargoStatLookup[id];
		}

		public void AddCargoProduced(CargoClass c, int quantity)
		{
			GetCargoStats(c.UniqueId).ProducedVolume += (float)quantity * c.Volume;
			GetCargoStats(c.UniqueId).ProducedValue += quantity * c.BasePrice;
			allCargoStats.ProducedVolume += (float)quantity * c.Volume;
			allCargoStats.ProducedValue += quantity * c.BasePrice;
		}

		public void AddCargoConsumed(CargoClass c, int quantity)
		{
			GetCargoStats(c.UniqueId).ConsumedVolume += (float)quantity * c.Volume;
			GetCargoStats(c.UniqueId).ConsumedValue += quantity * c.BasePrice;
			allCargoStats.ConsumedVolume += (float)quantity * c.Volume;
			allCargoStats.ConsumedValue += quantity * c.BasePrice;
		}

		public void AddCargoBoughtByAI(CargoClass c, int quantity)
		{
			GetCargoStats(c.UniqueId).BoughtByAIVolume += (float)quantity * c.Volume;
			GetCargoStats(c.UniqueId).BoughtByAIValue += quantity * c.BasePrice;
			allCargoStats.BoughtByAIVolume += (float)quantity * c.Volume;
			allCargoStats.BoughtByAIValue += quantity * c.BasePrice;
		}

		public void AddCargoSoldByAI(CargoClass c, int quantity)
		{
			GetCargoStats(c.UniqueId).SoldByAIVolume += (float)quantity * c.Volume;
			GetCargoStats(c.UniqueId).SoldByAIValue += quantity * c.BasePrice;
			allCargoStats.SoldByAIVolume += (float)quantity * c.Volume;
			allCargoStats.SoldByAIValue += quantity * c.BasePrice;
		}

		public void AddCargoMined(CargoClass c, int quantity)
		{
			GetCargoStats(c.UniqueId).MinedVolume += (float)quantity * c.Volume;
			GetCargoStats(c.UniqueId).MinedValue += quantity * c.BasePrice;
			allCargoStats.MinedVolume += (float)quantity * c.Volume;
			allCargoStats.MinedValue += quantity * c.BasePrice;
		}

		public void AddCargoTrimmed(CargoClass c, int quantity)
		{
			GetCargoStats(c.UniqueId).TrimmedVolume += (float)quantity * c.Volume;
			GetCargoStats(c.UniqueId).TrimmedValue += quantity * c.BasePrice;
			allCargoStats.TrimmedVolume += (float)quantity * c.Volume;
			allCargoStats.TrimmedValue += quantity * c.BasePrice;
		}

		public void AddDamageInflicted(float damage)
		{
			totalHullDamageReceived += damage;
		}

		public void AddRetiredFaction(Faction faction)
		{
			retiredFactionsByType[faction.FactionType] = GetRetiredFactionCount(faction.FactionType) + 1;
			retiredFactionInfos.Add(new RetiredFactionInfo
			{
				FactionType = faction.FactionType,
				Name = faction.GetLongNameElseShort(),
				TimeOfRetire = EngineASX.Instance.ScenarioElapsedTime,
				NumShipsBuilt = ((faction.Stats != null) ? faction.Stats.NumShipsBuilt : (-1)),
				NumShipsLost = ((faction.Stats != null) ? faction.Stats.NumShipsLost : (-1))
			});
		}

		public void AddDestroyedUnit(Unit unit)
		{
			destroyedUnitCountsByClass[unit.UnitClass.UniqueID] = GetDestroyedUnitCountByClass(unit.UnitClass) + 1;
			destroyedUnitCountsByType[unit.UnitType] = GetDestroyedUnitCountByType(unit.UnitType) + 1;
		}

		public int GetRetiredFactionCount(FactionType factionType)
		{
			if (retiredFactionsByType.TryGetValue(factionType, out var value))
			{
				return value;
			}
			return 0;
		}

		public int GetDestroyedUnitCountByType(UnitType unitType)
		{
			if (destroyedUnitCountsByType.TryGetValue(unitType, out var value))
			{
				return value;
			}
			return 0;
		}

		public int GetDestroyedUnitCountByClass(UnitClass unitClass)
		{
			if (destroyedUnitCountsByClass.TryGetValue(unitClass.UniqueID, out var value))
			{
				return value;
			}
			return 0;
		}

		public void AddShipBuildByAI(Faction faction, UnitClass ship)
		{
			shipsBuiltByAI++;
		}
	}
}
