using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Core
{
	public class DestroyedUnitInfoController
	{
		public const double MaxAgeSeconds = 1800.0;

		private Dictionary<int, DestroyedUnitInfo> destroyedUnitInfoByUnitId = new Dictionary<int, DestroyedUnitInfo>(100);

		private Dictionary<int, List<DestroyedUnitInfo>> destroyedUnitInfoBySectorId = new Dictionary<int, List<DestroyedUnitInfo>>(20);

		public void Trim()
		{
			Queue<int> queue = new Queue<int>();
			foreach (KeyValuePair<int, DestroyedUnitInfo> item in destroyedUnitInfoByUnitId)
			{
				queue.Enqueue(item.Key);
			}
			while (queue.Count > 0)
			{
				int key = queue.Dequeue();
				DestroyedUnitInfo destroyedUnitInfo = destroyedUnitInfoByUnitId[key];
				if (EngineASX.Instance.ScenarioElapsedTime - destroyedUnitInfo.TimeOfDestruction > 1800.0)
				{
					RemoveDestroyedInSector(destroyedUnitInfo);
					destroyedUnitInfoByUnitId.Remove(key);
				}
			}
		}

		private void RemoveDestroyedInSector(DestroyedUnitInfo destroyedUnitInfo)
		{
			if (destroyedUnitInfoBySectorId.TryGetValue(destroyedUnitInfo.Sector.UniqueId, out var value))
			{
				value.Remove(destroyedUnitInfo);
			}
		}

		private void AddDestroyedInSector(DestroyedUnitInfo destroyedUnitInfo)
		{
			if (!destroyedUnitInfoBySectorId.TryGetValue(destroyedUnitInfo.Sector.UniqueId, out var value))
			{
				value = new List<DestroyedUnitInfo>(20);
				destroyedUnitInfoBySectorId[destroyedUnitInfo.Sector.UniqueId] = value;
			}
			value.Add(destroyedUnitInfo);
		}

		public bool TryGetByUnitId(int unitUniqueId, out DestroyedUnitInfo destroyedUnitInfo)
		{
			return destroyedUnitInfoByUnitId.TryGetValue(unitUniqueId, out destroyedUnitInfo);
		}

		public List<DestroyedUnitInfo> GetDestroyedInSector(Sector sector)
		{
			if (destroyedUnitInfoBySectorId.TryGetValue(sector.UniqueId, out var value))
			{
				return value;
			}
			return null;
		}

		public void RegisterDestroyedUnit(Unit unit, Unit attacker, Faction attackerFaction)
		{
			if (!(unit.Sector == null) && unit.IsStationOrShip())
			{
				DestroyedUnitInfo destroyedUnitInfo = new DestroyedUnitInfo(unit.UnitClass, unit.Sector, unit.SectorPosition, attacker, attackerFaction, EngineASX.Instance.ScenarioElapsedTime);
				destroyedUnitInfoByUnitId.Add(unit.UniqueId, destroyedUnitInfo);
				AddDestroyedInSector(destroyedUnitInfo);
			}
		}
	}
}
