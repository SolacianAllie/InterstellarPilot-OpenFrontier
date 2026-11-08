using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine
{
	public static class SectorFinder
	{
		public struct SectorResult
		{
			public Sector Sector;

			public int Distance;

			public SectorResult(Sector sector, int distance)
			{
				this = default;
				Sector = sector;
				Distance = distance;
			}
		}

		private struct QueueItem
		{
			public Sector Sector { get; set; }

			public int Jumps { get; set; }
		}

		public static List<SectorResult> Results = new List<SectorResult>(30);

		private static HashSet<int> visitedSectors = new HashSet<int>();

		private static List<QueueItem> queue = new List<QueueItem>();

		public static void FindSectorsWithinJumpDistanceOfSimple(Sector startSector, int maxJumpDist, bool includeUnstableWormholes)
		{
			Results.Clear();
			visitedSectors.Clear();
			queue.Clear();
			QueueItem item = new QueueItem
			{
				Sector = startSector,
				Jumps = 0
			};
			queue.Add(item);
			visitedSectors.Add(item.Sector.UniqueId);
			while (queue.Count > 0)
			{
				QueueItem queueItem = queue[0];
				Sector sector = queueItem.Sector;
				queue.RemoveAt(0);
				Results.Add(new SectorResult(sector, 0));
				if (maxJumpDist >= 0 && queueItem.Jumps >= maxJumpDist)
				{
					continue;
				}
				foreach (SectorNeighbour neighbour in sector.Neighbours)
				{
					if (neighbour.IsStableConnection || includeUnstableWormholes)
					{
						Sector sector2 = neighbour.Sector;
						if (!visitedSectors.Contains(sector2.UniqueId))
						{
							QueueItem item2 = new QueueItem
							{
								Sector = sector2,
								Jumps = queueItem.Jumps + 1
							};
							queue.Add(item2);
							visitedSectors.Add(sector2.UniqueId);
						}
					}
				}
			}
		}

		public static Sector FindNearestSectorWithinJumpDistanceOfSimple(Sector startSector, int maxJumpDist, bool includeUnstableWormholes, Func<Sector, bool> predicate, out int jumps)
		{
			if (predicate(startSector))
			{
				jumps = 0;
				return startSector;
			}
			jumps = -1;
			visitedSectors.Clear();
			queue.Clear();
			QueueItem item = new QueueItem
			{
				Sector = startSector,
				Jumps = 0
			};
			queue.Add(item);
			while (queue.Count > 0)
			{
				QueueItem queueItem = queue[0];
				Sector sector = queueItem.Sector;
				queue.RemoveAt(0);
				visitedSectors.Add(sector.UniqueId);
				Results.Add(new SectorResult(sector, 0));
				if (maxJumpDist >= 0 && queueItem.Jumps >= maxJumpDist)
				{
					continue;
				}
				foreach (SectorNeighbour neighbour in sector.Neighbours)
				{
					if (!neighbour.IsStableConnection && !includeUnstableWormholes)
					{
						continue;
					}
					Sector sector2 = neighbour.Sector;
					if (!visitedSectors.Contains(sector2.UniqueId))
					{
						if (predicate(sector2))
						{
							jumps = queueItem.Jumps + 1;
							return sector2;
						}
						QueueItem item2 = new QueueItem
						{
							Sector = sector2,
							Jumps = queueItem.Jumps + 1
						};
						queue.Add(item2);
					}
				}
			}
			return null;
		}

		public static void FindNavigableDiscoveredSectorsWithinJumpDistanceOf(Sector startSector, int? maxJumpDist, Faction faction, bool includeUnstableWormholes = false)
		{
			if (maxJumpDist < 0)
			{
				maxJumpDist = null;
			}
			Results.Clear();
			visitedSectors.Clear();
			queue.Clear();
			QueueItem item = new QueueItem
			{
				Sector = startSector,
				Jumps = 0
			};
			FactionIntel intel = faction.Intel;
			if (!intel.IsSectorDiscovered(startSector))
			{
				return;
			}
			queue.Add(item);
			while (queue.Count > 0)
			{
				QueueItem queueItem = queue[0];
				Sector sector = queueItem.Sector;
				queue.RemoveAt(0);
				visitedSectors.Add(sector.UniqueId);
				Results.Add(new SectorResult(sector, queueItem.Jumps));
				if (maxJumpDist.HasValue && !(queueItem.Jumps < maxJumpDist))
				{
					continue;
				}
				foreach (SectorNeighbour neighbour in sector.Neighbours)
				{
					if (neighbour.IsStableConnection || includeUnstableWormholes)
					{
						Sector sector2 = neighbour.Sector;
						if (!visitedSectors.Contains(sector2.UniqueId) && intel.CanTraverseIntoNeighbour(neighbour))
						{
							visitedSectors.Add(sector2.UniqueId);
							QueueItem item2 = new QueueItem
							{
								Sector = sector2,
								Jumps = queueItem.Jumps + 1
							};
							queue.Add(item2);
						}
					}
				}
			}
		}
	}
}
