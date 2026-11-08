using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class FleetSpawn
	{
		public static Fleet SpawnGroupFromParams(FleetSpawnParams spawnParams)
		{
			Sector sector = ((spawnParams.TargetDock != null) ? spawnParams.TargetDock.Sector : spawnParams.TargetSector);
			Vector3 vector = ((spawnParams.TargetDock != null) ? spawnParams.TargetDock.SectorPosition : spawnParams.TargetSectorPosition);
			vector.y = 0f;
			Vector3 position = sector.ToWorldPosition(vector);
			GameObject gameObject = Object.Instantiate(spawnParams.FleetPrefab.gameObject, position, Quaternion.identity);
			Fleet component = gameObject.GetComponent<Fleet>();
			gameObject.transform.SetParent(sector.transform, worldPositionStays: true);
			component.Init();
			component.Faction = spawnParams.Faction;
			if (spawnParams.HomeBase != null)
			{
				component.SetHomeBaseToUnit(spawnParams.HomeBase);
			}
			List<ShipHullType> unitHullTypes = spawnParams.Ships.Select((FleetSpawnShipParams e) => e.UnitClass.HullType).ToList();
			bool flag = spawnParams.TargetDock != null && spawnParams.TargetDock.GetHangar().CanUnitsFitInHangar(unitHullTypes);
			for (int num = 0; num < spawnParams.Ships.Count; num++)
			{
				FleetSpawnShipParams fleetSpawnShipParams = spawnParams.Ships[num];
				Vector3 position2 = vector + new Vector3((float)num * 50f, 0f, 0f);
				Unit unit = SpawnUnitFromParams(fleetSpawnShipParams, position2, Quaternion.identity);
				if (unit != null)
				{
					if (spawnParams.TargetDock != null)
					{
						if (flag)
						{
							if (!unit.Components.TryDockInUnit(spawnParams.TargetDock))
							{
								Debug.LogError("Failed to spawn unit at dock", unit);
							}
						}
						else
						{
							unit.Sector = spawnParams.TargetDock.Sector;
						}
					}
					else
					{
						unit.Sector = spawnParams.TargetSector;
					}
					unit.Components.InstallDefaultComponents(checkForOverride: true);
					if (!string.IsNullOrEmpty(spawnParams.ShipDesignation))
					{
						unit.UnitName = spawnParams.ShipDesignation;
					}
					NpcPilot npcPilot = WorldHelper.SpawnRandomizedNpcFromPersonPrefab(fleetSpawnShipParams.PilotPrefab, unit.Components);
					if (npcPilot != null)
					{
						npcPilot.Init();
						if (spawnParams.Faction != null)
						{
							npcPilot.SetCombatEfficiencyFromFactionRange(spawnParams.Faction);
						}
						npcPilot.Settings.RestrictedWeaponPreference = Random.value;
						npcPilot.Fleet = component;
						npcPilot.Person.AssignFirstPilotRankIfNull();
					}
					else
					{
						Debug.LogWarning($"{component}: AIGroupSpawner failed to spawn controller", component);
					}
				}
				else
				{
					Debug.LogWarning($"{component}: AIGroupSpawner failed to spawn unit", component);
				}
			}
			return component;
		}

		public static Unit SpawnUnitFromParams(FleetSpawnShipParams shipParams, Vector3 position, Quaternion rotation)
		{
			UnitClass unitClass = shipParams.UnitClass;
			if (unitClass != null)
			{
				Unit component = Object.Instantiate(unitClass.UnitPrefab.gameObject, position, rotation).GetComponent<Unit>();
				component.Init();
				if (component.Components != null && shipParams.AddCargoLoadout)
				{
					component.Components.AddDefaultCargoLoadout();
				}
				if (!string.IsNullOrEmpty(shipParams.ShipName))
				{
					component.Components.ShipName = shipParams.ShipName;
				}
				return component;
			}
			return null;
		}
	}
}
