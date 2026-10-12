using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitRecentAttacksLog : MonoBehaviour
	{
		public struct RecentAttackDetail
		{
			public float MostRecentAttack;

			public float LastBroadcastTime;

			public float AttackStartTime;
		}

		private Dictionary<int, RecentAttackDetail> attackedUnitsById = new Dictionary<int, RecentAttackDetail>(64);

		public void Clear()
		{
			attackedUnitsById.Clear();
		}

		public RecentAttackType LogAttack(Unit receiver)
		{
			if (receiver == null)
			{
				Debug.LogError("Null receiver");
				return RecentAttackType.NewAttack;
			}
			RecentAttackDetail value = default;
			RecentAttackType recentAttackType = RecentAttackType.NewAttack;
			if (attackedUnitsById.TryGetValue(receiver.UniqueId, out value))
			{
				if (Time.time - value.MostRecentAttack < 30f)
				{
					recentAttackType = RecentAttackType.NewAttack;
				}
				else if (Time.time - value.LastBroadcastTime > 10f)
				{
					recentAttackType = RecentAttackType.UpdatedAttack;
					value.LastBroadcastTime = Time.time;
				}
				else
				{
					recentAttackType = RecentAttackType.OldAttack;
				}
			}
			if (recentAttackType == RecentAttackType.NewAttack)
			{
				value.AttackStartTime = Time.time;
			}
			value.MostRecentAttack = Time.time;
			attackedUnitsById[receiver.UniqueId] = value;
			return recentAttackType;
		}

		public float? GetLastTimeOfAttack(Unit unit)
		{
			if (attackedUnitsById.TryGetValue(unit.UniqueId, out var value))
			{
				return value.MostRecentAttack;
			}
			return null;
		}

		public bool IsUnderAttack(Unit unit, float maxTime = 30f)
		{
			float? lastTimeOfAttack = GetLastTimeOfAttack(unit);
			if (lastTimeOfAttack.HasValue)
			{
				return Time.time - lastTimeOfAttack < maxTime;
			}
			return false;
		}
	}
}
