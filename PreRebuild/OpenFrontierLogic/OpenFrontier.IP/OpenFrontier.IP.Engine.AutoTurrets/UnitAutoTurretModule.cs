using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Engine.AutoTurrets
{
	public class UnitAutoTurretModule
	{
		public AutoTurretFireMode FireMode = AutoTurretFireMode.PreferredTargetOnly;

		public const AutoTurretFireMode DefaultFireMode = AutoTurretFireMode.PreferredTargetOnly;

		private readonly UnitComponentHolder unitComponents;

		[NonSerialized]
		public Unit PreferredTurretTarget;

		[NonSerialized]
		public float PreferredTurretTargetScore;

		private List<Unit> hostileTargets = new List<Unit>(4);

		private List<Unit> hostileMissiles = new List<Unit>(4);

		private float nextSearchTime;

		private bool isSearchingForTargets;

		public const float SearchFrequency = 2f;

		public List<Unit> HostileMissiles => hostileMissiles;

		public List<Unit> HostileTargets => hostileTargets;

		public bool HasHostileMissileTargets => hostileMissiles.Count > 0;

		public bool HasHostileTargets => hostileTargets.Count > 0;

		public UnitAutoTurretModule(UnitComponentHolder unitComponents)
		{
			this.unitComponents = unitComponents;
			nextSearchTime = Time.time + UnityEngine.Random.value * 2f;
		}

		public void Update(float elapsedTime)
		{
			if (!isSearchingForTargets && Time.time > nextSearchTime && CanStartSearch())
			{
				StartTargetSearch();
				nextSearchTime = Time.time + 2f;
			}
		}

		private void StartTargetSearch()
		{
			hostileTargets.Clear();
			hostileMissiles.Clear();
			int num = Physics.OverlapSphereNonAlloc(unitComponents.transform.position, unitComponents.HighestRangedTurret, EngineASX.ColliderCache, GameController.Instance.ShipsStationsAndMissilesMask, QueryTriggerInteraction.Collide);
			Faction faction = unitComponents.Unit.Faction;
			for (int i = 0; i < num; i++)
			{
				Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
				if (component.Faction != faction && faction.IsHostileTo(component) && (component.UnitType == UnitType.Projectile || faction.Intel.IsUnitDiscovered(component)))
				{
					if (component.UnitType == UnitType.Projectile)
					{
						hostileMissiles.Add(component);
					}
					else
					{
						hostileTargets.Add(component);
					}
				}
			}
		}

		private bool CanStartSearch()
		{
			if (unitComponents.Unit.Faction != null)
			{
				return unitComponents.Unit.Faction.Intel.SectorHasRecentHostiles(unitComponents.Unit.Sector);
			}
			return false;
		}

		public void ClearTargets()
		{
			isSearchingForTargets = false;
			hostileTargets.Clear();
		}
	}
}
