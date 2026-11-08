using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIAttackModule
	{
		private Unit bestAttackTarget;

		private float bestAttackTargetScore;

		private HashSet<int> possibleAttackTargetCache = new HashSet<int>(20);

		private Queue<int> attackTargetProcessQueue = new Queue<int>(20);

		private HashSet<int> sectorsWithHostileTargets = new HashSet<int>(8);

		public Faction Faction;

		private float lastProcessedTime;

		public bool IsProcessing => attackTargetProcessQueue.Count > 0;

		public HashSet<int> LastSearchSectorsWithHostiles => sectorsWithHostileTargets;

		public Unit BestAttackTarget => bestAttackTarget;

		public void Update()
		{
			if (IsProcessing)
			{
				ProcessHostileTargets();
			}
			else if (Time.time > lastProcessedTime + 10f)
			{
				StartProcessing();
			}
		}

		private void StartProcessing()
		{
			sectorsWithHostileTargets.Clear();
			attackTargetProcessQueue.Clear();
			lastProcessedTime = Time.time;
			foreach (int item in possibleAttackTargetCache)
			{
				attackTargetProcessQueue.Enqueue(item);
			}
		}

		private void ProcessHostileTargets()
		{
			int num = Mathf.Min(2, attackTargetProcessQueue.Count);
			if (num <= 0)
			{
				return;
			}
			for (int i = 0; i < num; i++)
			{
				int id = attackTargetProcessQueue.Dequeue();
				Unit unitByid = EngineASX.Instance.GetUnitByid(id);
				if (unitByid != null && unitByid.IsValidAndNotDestroyed && IsUnitValidAttackTarget(unitByid))
				{
					if (!sectorsWithHostileTargets.Contains(unitByid.Sector.UniqueId))
					{
						sectorsWithHostileTargets.Add(unitByid.Sector.UniqueId);
					}
					float num2 = ScoreHostileTargetForAttack(unitByid);
					if (bestAttackTarget == null || num2 > bestAttackTargetScore)
					{
						bestAttackTarget = unitByid;
						bestAttackTargetScore = num2;
					}
				}
			}
		}

		public void ClearBestAttackTarget()
		{
			bestAttackTarget = null;
			bestAttackTargetScore = 0f;
		}

		public bool IsUnitValidAttackTarget(Unit unit)
		{
			if (unit != null && unit.IsValidAndNotDestroyed)
			{
				return Faction.IsHostileTo(unit.Faction);
			}
			return false;
		}

		private float ScoreHostileTargetForAttack(Unit unit)
		{
			float num = (unit.Sector.IsSectorANeighbourOfControllingFaction(Faction) ? 5f : 1f);
			float num2 = 0f;
			num2 = FactionAIBase.GetUnitDefendOrderPriority(unit) * num;
			int jumpDistanceTo = unit.Sector.GetJumpDistanceTo(Faction.HomeSector);
			num2 -= (float)jumpDistanceTo * 5f;
			num2 += Random.value * 5f;
			if (unit.Sector == Faction.HomeSector)
			{
				num2 += 10f;
			}
			else if (unit.Sector.ControllingFaction == Faction)
			{
				num2 += 10f;
			}
			return num2;
		}

		internal void NotifyScannedHostileTargetByFleet(Fleet scanner, Unit hostileUnit, float distance)
		{
			if (possibleAttackTargetCache.Count < 50 && !possibleAttackTargetCache.Contains(hostileUnit.UniqueId))
			{
				possibleAttackTargetCache.Add(hostileUnit.UniqueId);
			}
		}

		internal void NotifyScannedHostileTargetByNpc(NpcPilot scanner, Unit hostileUnit, float distance)
		{
			if (possibleAttackTargetCache.Count < 50 && !possibleAttackTargetCache.Contains(hostileUnit.UniqueId))
			{
				possibleAttackTargetCache.Add(hostileUnit.UniqueId);
			}
		}
	}
}
