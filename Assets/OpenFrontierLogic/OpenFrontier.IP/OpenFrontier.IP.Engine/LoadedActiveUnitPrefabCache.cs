using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class LoadedActiveUnitPrefabCache : MonoBehaviour
	{
		private Dictionary<string, ActiveUnit> loadedActiveUnits = new Dictionary<string, ActiveUnit>();

		public ActiveUnit GetActiveUnit(EngineASX engine, string className)
		{
			ActiveUnit value = null;
			if (!loadedActiveUnits.TryGetValue(className, out value))
			{
				value = EngineASX.LoadActiveUnit(className);
				if (value != null)
				{
					loadedActiveUnits[className] = value;
				}
			}
			return value;
		}

		public void ClearCache()
		{
			loadedActiveUnits.Clear();
		}
	}
}
