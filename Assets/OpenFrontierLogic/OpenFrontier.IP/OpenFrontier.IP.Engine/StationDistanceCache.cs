using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class StationDistanceCache : MonoBehaviour
	{
		private Dictionary<ulong, float> cachedDistances = new Dictionary<ulong, float>();

		public void Clear()
		{
			cachedDistances.Clear();
		}

		public float? GetCachedDistance(Unit unit1, Unit unit2)
		{
			ulong key = Helper.PairId(unit1.UniqueId, unit2.UniqueId);
			float value = 0f;
			if (cachedDistances.TryGetValue(key, out value))
			{
				return value;
			}
			return null;
		}

		public void CacheDistance(Unit unit1, Unit unit2, float distance)
		{
			ulong key = Helper.PairId(unit1.UniqueId, unit2.UniqueId);
			cachedDistances[key] = distance;
		}
	}
}
