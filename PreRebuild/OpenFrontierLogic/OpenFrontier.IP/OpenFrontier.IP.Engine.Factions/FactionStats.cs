using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionStats : MonoBehaviour
	{
		internal double LastTimeMadePeace = -1.0;

		internal double LastTimeWhenToWar = -1.0;

		private int highestScratchcardWin;

		private int scratchcardsScratched;

		private Dictionary<UnitClass, int> unitsLostCountByClass = new Dictionary<UnitClass, int>();

		private Dictionary<UnitClass, int> unitsDestroyedCountByClass = new Dictionary<UnitClass, int>();

		public float StartingVirtue = -1f;

		public int StartingShipCount;

		public int StartingStationCount;

		private int totalShipsClaimed;

		public int StartingCredits;

		public long StartingNetWorth;

		public double TotalDamageReceived;

		public double TotalDamageInflicted;

		public long TotalRevenue;

		public long TotalExpenditure;

		public long TotalExpenditureOnStationUpgrades;

		private int mostShipsOwned;

		private int mostStationsOwned;

		private int numShipsBuilt;

		private int numShipsLost;

		public Dictionary<UnitClass, int> UnitsLostCountByClass => unitsLostCountByClass;

		public Dictionary<UnitClass, int> UnitsDestroyedCountByClass => unitsDestroyedCountByClass;

		public int TotalShipsClaimed
		{
			get
			{
				return totalShipsClaimed;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException();
				}
				totalShipsClaimed = value;
			}
		}

		public int ScratchcardsScratched
		{
			get
			{
				return scratchcardsScratched;
			}
			set
			{
				scratchcardsScratched = value;
			}
		}

		public int HighestScratchcardWin
		{
			get
			{
				return highestScratchcardWin;
			}
			set
			{
				highestScratchcardWin = value;
			}
		}

		public int MostShipsOwned
		{
			get
			{
				return mostShipsOwned;
			}
			set
			{
				mostShipsOwned = value;
			}
		}

		public int MostStationsOwned
		{
			get
			{
				return mostStationsOwned;
			}
			set
			{
				mostStationsOwned = value;
			}
		}

		public int NumShipsBuilt => numShipsBuilt;

		public int NumShipsLost => numShipsLost;

		public void RecordLostUnit(Unit unit)
		{
			RecordUnitsLostCountByClass(unit);
			if (unit.UnitType == UnitType.Ship)
			{
				numShipsLost++;
			}
		}

		public void RecordKilledUnit(Unit unit)
		{
			RecordUnitsKilledCountByClass(unit);
		}

		public void RecordUnitClaimed(Unit unit)
		{
			if (unit.UnitType == UnitType.Ship)
			{
				totalShipsClaimed++;
			}
		}

		private void RecordUnitsLostCountByClass(Unit unit)
		{
			if (!unitsLostCountByClass.ContainsKey(unit.UnitClass))
			{
				unitsLostCountByClass[unit.UnitClass] = 1;
			}
			else
			{
				unitsLostCountByClass[unit.UnitClass]++;
			}
		}

		private void RecordUnitsKilledCountByClass(Unit unit)
		{
			if (!unitsDestroyedCountByClass.ContainsKey(unit.UnitClass))
			{
				unitsDestroyedCountByClass[unit.UnitClass] = 1;
			}
			else
			{
				unitsDestroyedCountByClass[unit.UnitClass]++;
			}
		}

		public int GetUnitsLostCountByType(UnitType unitType)
		{
			int num = 0;
			foreach (KeyValuePair<UnitClass, int> item in unitsLostCountByClass)
			{
				if (item.Key.UnitType == unitType)
				{
					num += item.Value;
				}
			}
			return num;
		}

		public int GetUnitsLostCountByClass(UnitClass unitClass)
		{
			int value = 0;
			if (unitsLostCountByClass.TryGetValue(unitClass, out value))
			{
				return value;
			}
			return 0;
		}

		public int GetUnitsKilledCountByClass(UnitClass unitClass)
		{
			int value = 0;
			if (unitsDestroyedCountByClass.TryGetValue(unitClass, out value))
			{
				return value;
			}
			return 0;
		}

		public int GetUnitsKilledCountByType(UnitType unitType)
		{
			int num = 0;
			foreach (KeyValuePair<UnitClass, int> item in unitsDestroyedCountByClass)
			{
				if (item.Key.UnitType == unitType)
				{
					num += item.Value;
				}
			}
			return num;
		}

		public void RecordShipBuilt(UnitClass unitClass)
		{
			numShipsBuilt++;
		}
	}
}
