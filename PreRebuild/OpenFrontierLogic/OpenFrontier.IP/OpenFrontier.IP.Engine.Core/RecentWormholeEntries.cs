using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core
{
	public class RecentWormholeEntries
	{
		public Dictionary<int, RecentWormholeEntry> unitEntryTimes = new Dictionary<int, RecentWormholeEntry>(100);

		public Wormhole GetRecentEnteredWormhole(Unit unit, float maxAge = 20f)
		{
			if (unitEntryTimes.TryGetValue(unit.UniqueId, out var value) && Time.time - value.Time < maxAge)
			{
				return value.Wormhole;
			}
			return null;
		}

		public void RegisterWormholeEntry(Unit unit, Wormhole wormhole)
		{
			unitEntryTimes[unit.UniqueId] = new RecentWormholeEntry
			{
				Time = Time.time,
				Wormhole = wormhole,
				Unit = unit
			};
		}

		public void Trim()
		{
			int[] array = unitEntryTimes.Keys.ToArray();
			foreach (int num in array)
			{
				Unit unitByid = EngineASX.Instance.GetUnitByid(num);
				if (unitByid == null || !unitByid.IsValidAndNotDestroyed || GetRecentEnteredWormhole(unitByid, 60f) == null)
				{
					unitEntryTimes.Remove(num);
				}
			}
		}
	}
}
