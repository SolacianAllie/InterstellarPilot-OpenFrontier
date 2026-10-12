using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class MissileLockController
	{
		public delegate void MissileLockHandler(MissileLockController sender, Unit entity);

		internal Dictionary<int, List<Missile>> missileLocks = new Dictionary<int, List<Missile>>(10);

		public event MissileLockHandler EntityGotMissileLock;

		public event MissileLockHandler EntityLostMissleLock;

		public bool HasMissileLock(Unit unit)
		{
			return missileLocks.ContainsKey(unit.UniqueId);
		}

		public List<Missile> GetMissileLocks(Unit unit)
		{
			if (missileLocks.ContainsKey(unit.UniqueId))
			{
				return missileLocks[unit.UniqueId];
			}
			return null;
		}

		public void RemoveMissileLocks(Unit unit)
		{
			missileLocks.Remove(unit.UniqueId);
		}

		public Missile GetNearestMissileLock(Unit unit, out float nearestDist)
		{
			Missile result = null;
			List<Missile> value = null;
			nearestDist = float.MaxValue;
			if (missileLocks.TryGetValue(unit.UniqueId, out value))
			{
				for (int i = 0; i < value.Count; i++)
				{
					float num = Vector3.Distance(unit.transform.position, value[i].transform.position);
					if (num < nearestDist)
					{
						nearestDist = num;
						result = value[i];
					}
				}
			}
			return result;
		}

		public Missile GetNearestNonDisruptedMissileLock(Unit unit, out float nearestDist)
		{
			Missile result = null;
			List<Missile> value = null;
			nearestDist = float.MaxValue;
			if (missileLocks.TryGetValue(unit.UniqueId, out value))
			{
				foreach (Missile item in value)
				{
					if (!item.IsDisrupted)
					{
						float num = Vector3.Distance(unit.transform.position, item.transform.position);
						if (num < nearestDist)
						{
							nearestDist = num;
							result = item;
						}
					}
				}
			}
			return result;
		}

		internal void AddMissileLock(Missile projectile, Unit target)
		{
			List<Missile> value = null;
			if (!missileLocks.TryGetValue(target.UniqueId, out value))
			{
				value = new List<Missile>(8);
				missileLocks[target.UniqueId] = value;
				if (EntityGotMissileLock != null)
				{
					EntityGotMissileLock(this, target);
				}
			}
			value.Add(projectile);
			if (target.NpcPilot != null)
			{
				target.NpcPilot.NotifyMissileLock(projectile, value.Count);
			}
		}

		internal void RemoveMissileLock(Missile projectile, Unit unit)
		{
			List<Missile> value = null;
			if (!missileLocks.TryGetValue(unit.UniqueId, out value))
			{
				return;
			}
			value.Remove(projectile);
			if (value.Count == 0)
			{
				missileLocks.Remove(unit.UniqueId);
				if (EntityLostMissleLock != null)
				{
					EntityLostMissleLock(this, unit);
				}
			}
		}
	}
}
