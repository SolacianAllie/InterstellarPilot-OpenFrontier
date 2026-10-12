using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIUnitsUnderAttackProcessor
	{
		private struct UnitUnderAttack
		{
			public int UnitId;

			public Unit AttackerUnit;

			public Faction AttackingFaction;

			public float Time;

			public RecentAttackType AttackType;
		}

		private Queue<UnitUnderAttack> unitsToProcess = new Queue<UnitUnderAttack>(20);

		private FactionAIBase factionAI;

		public FactionAIBase FactionAI
		{
			get
			{
				return factionAI;
			}
			set
			{
				factionAI = value;
			}
		}

		public void Update()
		{
			if (unitsToProcess.Count <= 0)
			{
				return;
			}
			float time = unitsToProcess.Peek().Time;
			if (!(Time.time - time > 2f))
			{
				return;
			}
			UnitUnderAttack unitUnderAttack = unitsToProcess.Dequeue();
			Unit unitByid = EngineASX.Instance.GetUnitByid(unitUnderAttack.UnitId);
			if (unitByid != null && unitByid.Sector != null)
			{
				factionAI.SendDistressCalls(unitByid, unitUnderAttack.AttackingFaction, unitUnderAttack.AttackerUnit);
				if (unitUnderAttack.AttackerUnit != null && unitUnderAttack.AttackerUnit.IsValidAndNotDestroyed)
				{
					factionAI.HandleUnitUnderAttack(unitByid, unitUnderAttack.AttackingFaction, unitUnderAttack.AttackerUnit, unitUnderAttack.AttackType);
				}
			}
		}

		public void RegisterUnitUnderAttack(Unit unit, Fleet fleet, Unit attackerUnit, Faction attackerFaction, RecentAttackType attackType)
		{
			UnitUnderAttack item = new UnitUnderAttack
			{
				AttackerUnit = attackerUnit,
				AttackingFaction = attackerFaction,
				UnitId = unit.UniqueId,
				Time = Time.time,
				AttackType = attackType
			};
			unitsToProcess.Enqueue(item);
		}
	}
}
