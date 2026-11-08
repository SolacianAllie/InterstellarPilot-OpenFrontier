using System;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CreateFleetsSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Creating fleets...", this, 1);
			}
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction.IsAIFactionType)
				{
					CreatePilotsAndFleets(faction);
				}
			}
		}

		public void CreatePilotsAndFleets(Faction faction)
		{
			FactionAIBase factionAI = faction.FactionAI;
			Fleet fleetPrefab = faction.FactionAI.GetFleetPrefab();
			Person pilotPrefab = faction.FactionAI.GetPilotPrefab();
			if (fleetPrefab != null && pilotPrefab != null)
			{
				factionAI.CreatePilotsAndHandleSurplusShips(createPilots: true);
				factionAI.PopulatePilottedUnitsWithoutAFleet();
				while (FactionAIBase.IdleUnitCache.Count > 0)
				{
					Unit value = FactionAIBase.IdleUnitCache.Dequeue().Value;
					Fleet fleet = factionAI.GetBestFleetForUnit(value);
					if (fleet != null)
					{
						factionAI.FindOrCreateNpcPilotForUnit(pilotPrefab, value).Fleet = fleet;
					}
					else
					{
						fleet = factionAI.CreateFleetAndNpcForUnitAndAssignStrategy(fleetPrefab, pilotPrefab, value);
					}
					if (!fleet.HasHomeBase)
					{
						fleet.SetHomeBase(faction.FactionAI.FindBestHomeBaseForFleet(fleet));
					}
				}
			}
			foreach (Fleet fleet2 in faction.Fleets)
			{
				if (fleet2.ExcludeFromFactionAI)
				{
					continue;
				}
				fleet2.transform.localRotation = Geometry.RandomYRotation();
				fleet2.SetHomeBase(factionAI.FindBestHomeBaseForFleet(fleet2));
				Vector3 checkSectorPosition;
				if (fleet2.HomeBaseUnit != null)
				{
					fleet2.Sector = fleet2.HomeBaseUnit.Sector;
					checkSectorPosition = fleet2.HomeBaseUnit.SectorPosition + Geometry.RandomXZUnitVector() * UnityEngine.Random.Range(400f, 800f);
				}
				else
				{
					checkSectorPosition = fleet2.SectorPosition;
				}
				fleet2.transform.localPosition = PhysicsNonOverlappingPositionFinder.FindSectorPosition(fleet2.Sector, checkSectorPosition, 250f, GameController.Instance.StaticNonOverlappingMask);
				foreach (NpcPilot npcPilot in fleet2.NpcPilots)
				{
					if (npcPilot.CurrentUnit.IsDocked)
					{
						throw new Exception("Oh shit");
					}
					npcPilot.CurrentUnit.Sector = fleet2.Sector;
					npcPilot.CurrentUnit.transform.localPosition = fleet2.GetPilotFleetFormationSectorPosition(npcPilot);
				}
			}
		}
	}
}
