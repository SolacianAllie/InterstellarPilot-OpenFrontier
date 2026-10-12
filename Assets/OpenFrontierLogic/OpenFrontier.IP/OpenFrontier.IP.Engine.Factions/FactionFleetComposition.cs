using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Core.Units;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionFleetComposition
	{
		public class FactionFleetCompositionItem
		{
			public float TotalRp { get; set; }

			public float RpPercentage { get; set; }
		}

		private Dictionary<FactionStrategy, FactionFleetCompositionItem> rpByFactionStategy = new Dictionary<FactionStrategy, FactionFleetCompositionItem>();

		private Faction faction;

		private int totalFactionShipRp;

		private float totalShipSize;

		private int numShips;

		public int TotalFactionShipRp => totalFactionShipRp;

		public Faction Faction
		{
			get
			{
				return faction;
			}
			set
			{
				faction = value;
			}
		}

		public float AverageShipSize
		{
			get
			{
				if (totalShipSize > 0f)
				{
					return totalShipSize / (float)numShips;
				}
				return 0f;
			}
		}

		public int NumShips => numShips;

		public float TotalShipSize => totalShipSize;

		public IEnumerable<KeyValuePair<FactionStrategy, FactionFleetCompositionItem>> RpByFactionStategy => rpByFactionStategy;

		public void Compile()
		{
			rpByFactionStategy.Clear();
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Ship);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				if (item != null && ShouldConsiderUnitClass(item.UnitClass))
				{
					AddShipInternal(item);
				}
			}
		}

		public void AddUnit(Unit ship)
		{
			if (ShouldConsiderUnitClass(ship.UnitClass))
			{
				AddShipInternal(ship);
			}
		}

		public void RemoveUnit(Unit ship)
		{
			if (ShouldConsiderUnitClass(ship.UnitClass))
			{
				RemoveShipInternal(ship);
			}
		}

		private void AddShipInternal(Unit ship)
		{
			numShips++;
			foreach (ShipPurposeItem shipPurpose in ship.UnitClass.ShipPurposes)
			{
				if (!rpByFactionStategy.TryGetValue(shipPurpose.FactionStrategy, out var value))
				{
					value = new FactionFleetCompositionItem();
				}
				value.TotalRp += (float)ship.UnitClass.RpCost * shipPurpose.Effectiveness;
				rpByFactionStategy[shipPurpose.FactionStrategy] = value;
			}
			totalFactionShipRp += ship.UnitClass.RpCost;
			totalShipSize += ship.UnitClass.RelativeShipSaleCost;
			foreach (KeyValuePair<FactionStrategy, FactionFleetCompositionItem> item in rpByFactionStategy)
			{
				item.Value.RpPercentage = item.Value.TotalRp / (float)totalFactionShipRp;
			}
		}

		private void RemoveShipInternal(Unit ship)
		{
			numShips--;
			foreach (ShipPurposeItem shipPurpose in ship.UnitClass.ShipPurposes)
			{
				if (rpByFactionStategy.TryGetValue(shipPurpose.FactionStrategy, out var value))
				{
					value.TotalRp -= (float)ship.UnitClass.RpCost * shipPurpose.Effectiveness;
				}
			}
			totalFactionShipRp -= ship.UnitClass.RpCost;
			totalShipSize -= ship.UnitClass.RelativeShipSaleCost;
			if (totalFactionShipRp < 0)
			{
				totalFactionShipRp = 0;
			}
			foreach (KeyValuePair<FactionStrategy, FactionFleetCompositionItem> item in rpByFactionStategy)
			{
				item.Value.RpPercentage = item.Value.TotalRp / (float)totalFactionShipRp;
			}
		}

		public bool ShouldConsiderUnitClass(UnitClass unitClass)
		{
			if (unitClass.UnitType == UnitType.Ship)
			{
				return unitClass.ShipType == ShipType.Normal;
			}
			return false;
		}

		public float GetPercentageOf(FactionStrategy strategy)
		{
			if (rpByFactionStategy.TryGetValue(strategy, out var value))
			{
				return value.RpPercentage;
			}
			return 0f;
		}
	}
}
