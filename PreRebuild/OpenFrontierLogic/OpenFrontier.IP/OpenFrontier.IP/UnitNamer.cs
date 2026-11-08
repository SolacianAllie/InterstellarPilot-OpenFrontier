using System.Text;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Intel;
using OpenFrontier.IP.Scenarios;

namespace OpenFrontier.IP
{
	public static class UnitNamer
	{
		private static StringBuilder stringBuilder = new StringBuilder();

		public static string GetPilotNameShipAndFleet(Person person)
		{
			if (person == null)
			{
				return null;
			}
			stringBuilder.Length = 0;
			bool flag = false;
			string nameAndRank = person.GetNameAndRank();
			if (!string.IsNullOrWhiteSpace(nameAndRank))
			{
				stringBuilder.Append(nameAndRank);
				flag = true;
			}
			Unit currentUnit = person.CurrentUnit;
			if (currentUnit != null)
			{
				if (flag)
				{
					stringBuilder.Append(" in ");
				}
				else
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(currentUnit.GetClassAndSeriesName());
			}
			Fleet fleet = currentUnit.GetFleet();
			if (fleet != null)
			{
				string friendlyName = fleet.GetFriendlyName();
				if (!string.IsNullOrWhiteSpace(friendlyName))
				{
					stringBuilder.AppendFormat(" ({0})", friendlyName);
				}
			}
			return stringBuilder.ToString();
		}

		public static string GetDesignation(Unit unit, bool shortName = false)
		{
			if (!string.IsNullOrEmpty(unit.UnitName))
			{
				if (unit.UnitClass.StationPurpose == StationPurpose.Bar)
				{
					if (shortName && !string.IsNullOrEmpty(unit.UnitShortName))
					{
						return "Bar \"" + unit.UnitShortName + "\"";
					}
					return "Bar \"" + unit.UnitName + "\"";
				}
				if (shortName && !string.IsNullOrEmpty(unit.UnitShortName))
				{
					return "\"" + unit.UnitShortName + "\"";
				}
				return "\"" + unit.UnitName + "\"";
			}
			if (unit.UnitType == UnitType.Ship && unit.UnitClass.ShipType == ShipType.Normal && unit.Components.PilotPerson != null && unit.Faction != null && unit.Faction.FactionAI != null)
			{
				Fleet fleet = unit.GetFleet();
				if (fleet != null && fleet.FleetStrategy != FactionStrategy.Unspecified)
				{
					string designationFromFleetStrategy = GetDesignationFromFleetStrategy(unit.Faction, fleet.FleetStrategy, shortName);
					if (designationFromFleetStrategy != null)
					{
						return designationFromFleetStrategy;
					}
				}
				FactionTypeInfo factionTypeInfo = unit.Faction.FactionTypeInfo;
				if (factionTypeInfo != null)
				{
					if (shortName && !string.IsNullOrEmpty(factionTypeInfo.ShortCustomUnitDesignation))
					{
						return factionTypeInfo.ShortCustomUnitDesignation;
					}
					return factionTypeInfo.CustomUnitDesignation;
				}
			}
			return null;
		}

		private static string GetDesignationFromFleetStrategy(Faction faction, FactionStrategy fleetStrategy, bool shortName)
		{
			switch (fleetStrategy)
			{
			case FactionStrategy.War:
				return faction.FactionTypeInfo.WarPartyUnitDesignation;
			case FactionStrategy.Scout:
				if (faction.FactionType == FactionType.Explorer)
				{
					return "Explorer";
				}
				return "Scout";
			case FactionStrategy.Escort:
				return "Escort";
			case FactionStrategy.BountyHunt:
				if (!shortName)
				{
					return "Bounty Hunter";
				}
				return "Hunter";
			case FactionStrategy.DealEquipment:
				if (!shortName)
				{
					return "Equipment Dealer";
				}
				return "Eq. Dealer";
			case FactionStrategy.Explore:
				return "Explorer";
			case FactionStrategy.Mine:
				return "Miner";
			case FactionStrategy.PassengerTransport:
				return "Transport";
			case FactionStrategy.Scavenge:
				return "Scavenger";
			case FactionStrategy.Trade:
				return "Trader";
			default:
				return null;
			}
		}

		public static string GetNameWithFactionAndFleet(Faction ourFaction, Unit unit, bool usePilotNamesAsDesignations, bool preferShortUnitName, bool preferShortFactionName, out bool showingShipNameInLabel)
		{
			Faction faction = unit.Faction;
			bool flag = faction != null && !faction.IsPlayerFaction && unit.UnitClass.StationPurpose != StationPurpose.Bar;
			string text = GetName(ourFaction, unit, usePilotNamesAsDesignations, flag && preferShortUnitName, out showingShipNameInLabel);
			if (flag)
			{
				text = ((!preferShortFactionName) ? (text + $" ({faction.GetFriendlyName()})") : (text + $" ({faction.GetShortFriendlyName()})"));
			}
			return text;
		}

		public static string GetName(Faction ourFaction, Unit unit, bool usePilotNamesAsDesignations, bool preferShortName, out bool showingShipNameInLabel)
		{
			showingShipNameInLabel = false;
			switch (unit.UnitType)
			{
			case UnitType.Cargo:
			case UnitType.Asteroid:
				return unit.GetFriendlyName(preferShortName);
			case UnitType.Wormhole:
				return GetWormholeName(ourFaction, unit.WormholeComponent, preferShortName);
			default:
			{
				string designation = GetDesignation(unit, preferShortName);
				if (!string.IsNullOrEmpty(designation))
				{
					return designation;
				}
				if (unit.Components != null)
				{
					if (unit.Faction == null)
					{
						return GetNameOfFactionlessUnit(unit);
					}
					if (unit.UnitType == UnitType.Ship)
					{
						if (unit.IsOwnedByPlayer)
						{
							Fleet fleet = unit.GetFleet();
							if (fleet != null && fleet.Ships.Count > 1)
							{
								return fleet.GetFriendlyName();
							}
							showingShipNameInLabel = true;
							return unit.GetFriendlyName(shortName: true);
						}
						if (usePilotNamesAsDesignations && unit.Components.PilotPerson != null)
						{
							if (!preferShortName)
							{
								return unit.Components.PilotPerson.FullNameWithFullRank;
							}
							return unit.Components.PilotPerson.ShortNameWithShortRank;
						}
						if (unit.UnitClass.ShipType == ShipType.Normal)
						{
							showingShipNameInLabel = true;
							return $"\"{unit.Components.ShipName}\"";
						}
					}
					else if (unit.UnitType == UnitType.Station)
					{
						return unit.GetFriendlyName(preferShortName);
					}
				}
				if (unit.UnitClass.ShipType == ShipType.Container)
				{
					return unit.UnitClass.GetClassAndSeriesName(shortName: true);
				}
				if (!preferShortName || string.IsNullOrWhiteSpace(unit.UnitClass.ShortClassName))
				{
					return unit.ClassName;
				}
				return unit.UnitClass.ShortClassName;
			}
			}
		}

		public static string GetNameOfFactionlessUnit(Unit unit)
		{
			if (unit.UnitType == UnitType.Station)
			{
				return "Abandoned " + unit.GetClassAndSeriesName(shortName: true);
			}
			if (unit.IsShip())
			{
				return "Abandoned " + unit.GetClassAndSeriesName(shortName: true);
			}
			return "Abandoned Object";
		}

		public static string GetUnexploredWormholeName(Wormhole wormhole)
		{
			return "Wormhole " + wormhole.Unit.GetDesignation();
		}

		public static string GetWormholeName(Faction ourFaction, Wormhole wormhole, bool preferShortName)
		{
			if (!string.IsNullOrWhiteSpace(wormhole.Unit.UnitName))
			{
				return wormhole.Unit.UnitName;
			}
			if (ourFaction.Intel.HasWormholeBeenEntered(wormhole))
			{
				return GetWormholeNameFromTargetSector(wormhole, preferShortName);
			}
			return GetUnexploredWormholeName(wormhole);
		}

		private static string GetWormholeNameFromTargetSector(Wormhole wormhole, bool preferShortName)
		{
			if (wormhole.ActualTargetSector != null && !string.IsNullOrEmpty(wormhole.ActualTargetSector.Name))
			{
				if (preferShortName)
				{
					return wormhole.ActualTargetSector.Name;
				}
				return "Wormhole to " + wormhole.ActualTargetSector.Name;
			}
			return "Unknown Wormhole";
		}

		public static string GetFriendlyNameAndFactionShortNameAndSector(Unit unit)
		{
			string friendlyName = unit.GetFriendlyName();
			if (unit.Faction != null)
			{
				return friendlyName + " (" + unit.Faction.GetShortNameElseLong() + ")" + ((unit.Sector != null) ? (" in " + unit.Sector.Name) : null);
			}
			return friendlyName + " in " + unit.Sector.Name;
		}

		public static string GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(Faction ourFaction, Sector ourSector, Unit unit, bool colourByHostility = true, bool shortName = false)
		{
			string friendlyNameForFaction = unit.GetFriendlyNameForFaction(ourFaction, shortName, colourByHostility);
			Sector targetSector = null;
			if (unit.IsStatic)
			{
				targetSector = unit.Sector;
			}
			else
			{
				WorldNavpoint? lastKnownPosition = ourFaction.Intel.GetLastKnownPosition(unit, float.MaxValue);
				if (lastKnownPosition.HasValue)
				{
					targetSector = lastKnownPosition.Value.Sector;
				}
			}
			string sectorNameAndDistanceForFaction = GetSectorNameAndDistanceForFaction(ourFaction, ourSector, targetSector, includeInText: true);
			if (unit.Faction != null && unit.Faction != ourFaction && unit.Faction.Name != unit.UnitName)
			{
				string text = (colourByHostility ? UnityRichTextHelper.ColorFromOpinion(unit.Faction.GetShortNameElseLong(), ourFaction, unit.Faction) : unit.Faction.GetShortNameElseLong());
				return "[" + friendlyNameForFaction + "] (" + text + ")" + sectorNameAndDistanceForFaction;
			}
			return "[" + friendlyNameForFaction + "]" + sectorNameAndDistanceForFaction;
		}

		public static string GetSectorNameAndDistanceForFaction(Faction ourFaction, Sector ourSector, Sector targetSector, bool includeInText = false)
		{
			string result = string.Empty;
			if (targetSector != null)
			{
				result = ((!includeInText) ? targetSector.Name : (" in " + targetSector.Name));
				result += AppendJumpDistanceIfMoreThanZero(ourSector, targetSector, ourFaction);
			}
			return result;
		}

		public static string AppendJumpDistanceIfMoreThanZero(Sector ourSector, Sector targetSector, Faction ourFaction)
		{
			if (ourSector != null && ourSector != targetSector)
			{
				UniversePath universePath = ourFaction.Intel.GetUniversePath(ourSector, targetSector);
				if (universePath == null || universePath.Jumps < 0)
				{
					return " (<sprite index= 0> -)";
				}
				if (universePath.Jumps > 0)
				{
					return $" (<sprite index= 0> {universePath.Jumps})";
				}
			}
			return string.Empty;
		}

		public static string GetNameAndFactionShortNameInParenthesis(Unit unit)
		{
			string friendlyName = unit.GetFriendlyName();
			if (unit.Faction != null && unit.Faction.Name != unit.UnitName)
			{
				return friendlyName + " (" + unit.Faction.GetShortNameElseLong() + ")";
			}
			return friendlyName;
		}

		public static string GetNameAndFactionLongNameInParenthesis(Unit unit)
		{
			string friendlyName = unit.GetFriendlyName();
			if (unit.Faction != null && unit.Faction.Name != unit.UnitName)
			{
				return friendlyName + " (" + unit.Faction.GetLongNameElseShort() + ")";
			}
			return friendlyName;
		}

		public static string GetNameAndFactionShortNameInParenthesisForPlayer(Unit unit, bool shortName = false)
		{
			string friendlyNameForFaction = unit.GetFriendlyNameForFaction(EngineASX.Instance.LocalFaction, shortName);
			if (unit.Faction != null && unit.Faction.Name != unit.UnitName && !unit.Faction.IsPlayerFaction)
			{
				return friendlyNameForFaction + " (" + unit.Faction.GetShortNameElseLong() + ")";
			}
			return friendlyNameForFaction;
		}

		public static string GetNameAndFactionLongNameInParenthesisForPlayer(Unit unit)
		{
			string friendlyNameForFaction = unit.GetFriendlyNameForFaction(EngineASX.Instance.LocalFaction);
			if (unit.Faction != null && unit.Faction.Name != unit.UnitName && !unit.Faction.IsPlayerFaction)
			{
				return friendlyNameForFaction + " (" + unit.Faction.GetLongNameElseShort() + ")";
			}
			return friendlyNameForFaction;
		}
	}
}
