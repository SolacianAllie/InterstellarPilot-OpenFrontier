using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;

namespace OpenFrontier.IP.Engine
{
	public static class EngineExtensions
	{
		public static int AbandonedCargoCount(this EngineASX engine)
		{
			int count = 0;
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				if (unit != null && unit.IsValidAndNotDestroyed && unit.UnitType == UnitType.Cargo && unit.Faction == null)
				{
					count++;
				}
			});
			return count;
		}

		public static int AbandonedCargoValue(this EngineASX engine)
		{
			int value = 0;
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				if (unit != null && unit.IsValidAndNotDestroyed && unit.UnitType == UnitType.Cargo && unit.Faction == null)
				{
					value += unit.CargoComponent.CreditsValue;
				}
			});
			return value;
		}

		public static int AbandonedStationCount(this EngineASX engine)
		{
			int count = 0;
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				if (unit != null && unit.IsValidAndNotDestroyed && unit.UnitType == UnitType.Station && unit.Faction == null)
				{
					count++;
				}
			});
			return count;
		}

		public static int AbandonedShipCount(this EngineASX engine)
		{
			int count = 0;
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				if (unit != null && unit.IsValidAndNotDestroyed && unit.UnitType == UnitType.Ship && unit.Faction == null)
				{
					count++;
				}
			});
			return count;
		}

		public static int AbandonedShipValue(this EngineASX engine)
		{
			int value = 0;
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				if (unit != null && unit.IsValidAndNotDestroyed && unit.UnitType == UnitType.Ship && unit.Faction == null)
				{
					value += unit.CalculateCurrentMoneyValue();
				}
			});
			return value;
		}

		public static FactionBountyBoard GetNearestBountyBoardToPlayer(this EngineASX engine, int maxJumpDistance = 4)
		{
			Sector localPlayerSector = EngineASX.Instance.LocalPlayerSector;
			_ = EngineASX.Instance.LocalFaction;
			if (localPlayerSector != null)
			{
				Sector sector = SectorFinder.FindNearestSectorWithinJumpDistanceOfSimple(localPlayerSector, maxJumpDistance, includeUnstableWormholes: false, (Sector e) => e.IsDiscoveredByLocalFaction() && e.ControllingFaction != null && e.ControllingFaction.BountyBoard != null, out var _);
				if (sector == null)
				{
					return null;
				}
				return sector.ControllingFaction.BountyBoard;
			}
			return null;
		}

		public static FactionBountyBoard GetNearestBountyBoardToPlayerSector(this EngineASX engine)
		{
			Sector localPlayerSector = EngineASX.Instance.LocalPlayerSector;
			_ = EngineASX.Instance.LocalFaction;
			if (localPlayerSector != null)
			{
				Sector sector = SectorFinder.FindNearestSectorWithinJumpDistanceOfSimple(localPlayerSector, 4, includeUnstableWormholes: false, (Sector e) => e.IsDiscoveredByLocalFaction() && e.ControllingFaction != null && e.ControllingFaction.BountyBoard != null, out var _);
				if (sector == null)
				{
					return null;
				}
				return sector.ControllingFaction.BountyBoard;
			}
			return null;
		}

		public static FactionBountyBoard GetNearestBountyBoardToSector(this EngineASX engine, Sector sourceSector)
		{
			if (sourceSector != null)
			{
				Sector sector = SectorFinder.FindNearestSectorWithinJumpDistanceOfSimple(sourceSector, 4, includeUnstableWormholes: false, (Sector e) => e.ControllingFaction != null && e.ControllingFaction.BountyBoard != null, out var _);
				if (sector == null)
				{
					return null;
				}
				return sector.ControllingFaction.BountyBoard;
			}
			return null;
		}

		public static long GetTotalNonBanditNetWorth(this EngineASX engine)
		{
			long num = 0L;
			foreach (Faction faction in engine.Factions)
			{
				if (faction != null && faction.IsValidInGame && faction.FactionType != FactionType.Bandit)
				{
					num += faction.GetCachedNetWorth();
				}
			}
			return num;
		}

		public static long GetTotalBanditNetWorth(this EngineASX engine)
		{
			long num = 0L;
			foreach (Faction faction in engine.Factions)
			{
				if (faction != null && faction.IsValidInGame && faction.FactionType == FactionType.Bandit)
				{
					num += faction.GetCachedNetWorth();
				}
			}
			return num;
		}
	}
}
