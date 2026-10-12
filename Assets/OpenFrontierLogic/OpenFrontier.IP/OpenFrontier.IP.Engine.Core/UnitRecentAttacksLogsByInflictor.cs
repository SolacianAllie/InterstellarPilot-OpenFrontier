using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core
{
	public class UnitRecentAttacksLogsByInflictor
	{
		private Dictionary<ulong, float> recentAttacks = new Dictionary<ulong, float>(100);

		public void Clear()
		{
			recentAttacks.Clear();
		}

		public void LogAttack(Unit inflictor, Unit receiver)
		{
			if (inflictor != null && receiver != null)
			{
				ulong key = Helper.OrderedPairId(receiver.UniqueId, inflictor.UniqueId);
				recentAttacks[key] = Time.time;
			}
		}

		public bool IsUnitAttackingUnit(Unit inflictor, Unit receiver, float maxTime = 30f)
		{
			float? lastTimeOfAttack = GetLastTimeOfAttack(inflictor, receiver);
			if (lastTimeOfAttack.HasValue)
			{
				return lastTimeOfAttack - Time.time < maxTime;
			}
			return false;
		}

		public float? GetLastTimeOfAttack(Unit inflictor, Unit receiver)
		{
			ulong key = Helper.OrderedPairId(receiver.UniqueId, inflictor.UniqueId);
			if (recentAttacks.TryGetValue(key, out var value))
			{
				return value;
			}
			return null;
		}
	}
}
