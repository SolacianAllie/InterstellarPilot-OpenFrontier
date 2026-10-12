using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AutoTurrets
{
	public class PointDefenceTurretModule
	{
		private readonly UnitComponentHolder unitComponents;

		private List<Projectile> hostileTargets = new List<Projectile>(4);

		private float nextSearchTime;

		private bool isSearchingForTargets;

		public const float SearchFrequency = 1.25f;

		public float? FireCooldownTime { get; set; }

		public List<Projectile> HostileTargets => hostileTargets;

		public bool HasHostileTargets => hostileTargets.Count > 0;

		public PointDefenceTurretModule(UnitComponentHolder unitComponents)
		{
			this.unitComponents = unitComponents;
			nextSearchTime = Time.time + Random.value * 1.25f;
		}

		public void Update(float elapsedTime)
		{
			if (!isSearchingForTargets && Time.time > nextSearchTime && CanStartSearch())
			{
				StartTargetSearch();
				nextSearchTime = Time.time + 1.25f;
			}
		}

		private void StartTargetSearch()
		{
			hostileTargets.Clear();
			int num = Physics.OverlapSphereNonAlloc(unitComponents.transform.position, unitComponents.HighestRangePointDefenceTurret * 1.35f, EngineASX.ColliderCache, GameController.Instance.MissileLayer, QueryTriggerInteraction.Collide);
			Faction faction = unitComponents.Unit.Faction;
			for (int i = 0; i < num; i++)
			{
				Projectile component = EngineASX.ColliderCache[i].GetComponent<Projectile>();
				Unit unit = component.Unit;
				if (unit.Faction != faction && faction.IsHostileTo(unit) && unit.SecondsSinceInit > 1f)
				{
					hostileTargets.Add(component);
				}
			}
		}

		private bool CanStartSearch()
		{
			if (unitComponents.Unit.Faction != null && unitComponents.Unit.Sector.GetCountOfUnitType(UnitType.Projectile) > 0)
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
