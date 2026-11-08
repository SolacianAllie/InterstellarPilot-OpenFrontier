using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldObjectTemplates
{
	public static class WorldObjectTemplateInitializer
	{
		public static WorldObjectTemplateInitializerResult InitObjectsInSector(GameObject prefab, Sector sector, Vector3 sectorPosition, Quaternion localRotation)
		{
			WorldObjectTemplateInitializerResult result = default;
			GameObject gameObject = Object.Instantiate(prefab, sector.transform.position + sectorPosition, sector.transform.rotation * localRotation, sector.transform);
			result.Factions = gameObject.GetComponentsInChildren<Faction>();
			Fleet[] componentsInChildren = gameObject.GetComponentsInChildren<Fleet>(includeInactive: true);
			Unit[] componentsInChildren2 = gameObject.GetComponentsInChildren<Unit>(includeInactive: true);
			Person[] componentsInChildren3 = gameObject.GetComponentsInChildren<Person>(includeInactive: true);
			NpcPilot[] componentsInChildren4 = gameObject.GetComponentsInChildren<NpcPilot>(includeInactive: true);
			FleetOrder[] componentsInChildren5 = gameObject.GetComponentsInChildren<FleetOrder>(includeInactive: true);
			Faction[] factions = result.Factions;
			foreach (Faction obj in factions)
			{
				obj.Init();
				obj.CreateOrInitFactionAIIfNeeded();
			}
			Fleet[] array = componentsInChildren;
			foreach (Fleet fleet in array)
			{
				fleet.Sector = sector;
				fleet.Init();
				if (fleet.ShouldZeroPositionY)
				{
					fleet.ZeroPositionY();
				}
			}
			Unit[] array2 = componentsInChildren2;
			for (int i = 0; i < array2.Length; i++)
			{
				WorldBase.InitNewGameUnit(array2[i]);
			}
			Person[] array3 = componentsInChildren3;
			foreach (Person person in array3)
			{
				person.Init();
				person.RefreshName();
				person.FindCurrentUnit();
				EngineASX.Instance.World.OnInitNewGamePerson(person);
			}
			array2 = componentsInChildren2;
			foreach (Unit unit in array2)
			{
				if (unit.Components != null && unit.Components.PilotPerson != null)
				{
					unit.Components.OnPilotChanged(null);
				}
			}
			NpcPilot[] array4 = componentsInChildren4;
			for (int i = 0; i < array4.Length; i++)
			{
				array4[i].Init();
			}
			FleetOrder[] array5 = componentsInChildren5;
			for (int i = 0; i < array5.Length; i++)
			{
				array5[i].Init();
			}
			array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetPositionToLeaderShipPosition();
			}
			array2 = componentsInChildren2;
			foreach (Unit unit2 in array2)
			{
				if (unit2.UnitType == UnitType.Ship && unit2.Components.PilotPerson != null && unit2.Components.PilotPerson.Faction != null && unit2.Faction == null)
				{
					unit2.Faction = unit2.Components.PilotPerson.Faction;
				}
			}
			return result;
		}
	}
}
