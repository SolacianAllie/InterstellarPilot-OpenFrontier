using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class FleetRecentAttacksLog : MonoBehaviour
	{
		[Serializable]
		public class FleetRecentAttackDetail
		{
			public int InflictorUnitId;

			public Fleet Receiver;

			public float TimeOfAttack;
		}

		[SerializeField]
		private Dictionary<ulong, FleetRecentAttackDetail> recentAttacksByUnit = new Dictionary<ulong, FleetRecentAttackDetail>();

		private Dictionary<int, FleetRecentAttackDetail> mostRecentAttacks = new Dictionary<int, FleetRecentAttackDetail>();

		public float RecentAttackTime = 20f;

		public void Clear()
		{
			recentAttacksByUnit.Clear();
			mostRecentAttacks.Clear();
		}

		public void LogAttack(Unit inflictor, Fleet receiver)
		{
			if (inflictor != null)
			{
				FleetRecentAttackDetail value = null;
				ulong key = Helper.OrderedPairId(receiver.UniqueId, inflictor.UniqueId);
				if (!recentAttacksByUnit.TryGetValue(key, out value))
				{
					value = new FleetRecentAttackDetail();
					value.InflictorUnitId = inflictor.UniqueId;
					value.Receiver = receiver;
					recentAttacksByUnit.Add(key, value);
				}
				value.TimeOfAttack = Time.time;
			}
			if (!mostRecentAttacks.TryGetValue(receiver.UniqueId, out var value2))
			{
				value2 = new FleetRecentAttackDetail();
				mostRecentAttacks.Add(receiver.UniqueId, value2);
			}
			value2.TimeOfAttack = Time.time;
			value2.Receiver = receiver;
			value2.InflictorUnitId = ((inflictor != null) ? inflictor.UniqueId : (-1));
		}

		public bool FleetRecentlyAttacked(Fleet receiver, float maxTime)
		{
			if (mostRecentAttacks.TryGetValue(receiver.UniqueId, out var value))
			{
				return Time.time - value.TimeOfAttack < maxTime;
			}
			return false;
		}

		public bool FleetRecentlyAttackedBy(Unit inflictor, Fleet receiver)
		{
			ulong key = Helper.OrderedPairId(receiver.UniqueId, inflictor.UniqueId);
			if (recentAttacksByUnit.TryGetValue(key, out var value))
			{
				return Time.time - value.TimeOfAttack < RecentAttackTime;
			}
			return false;
		}

		public float? GetRecentTimeOfAttack(Unit inflictor, Fleet receiver)
		{
			ulong key = Helper.OrderedPairId(receiver.UniqueId, inflictor.UniqueId);
			if (recentAttacksByUnit.TryGetValue(key, out var value))
			{
				return value.TimeOfAttack;
			}
			return null;
		}
	}
}
