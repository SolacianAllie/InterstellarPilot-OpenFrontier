using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class World : WorldBase
	{
		public bool DiscoverNormalJumpGatesInInitialScene = true;

		public bool DiscoverStaticUnitsInInitialScene = true;

		public bool DiscoverStationsInInitialScene;

		public UnitComponentHolder NewGameSpawnLocation;

		public List<UnitComponentHolder> NewGameSpawnLocations = new List<UnitComponentHolder>();

		public bool RespawnOnNewGame = true;

		public Unit RespawnShipPrefab;

		public List<UnitComponentHolder> RespawnTargets = new List<UnitComponentHolder>();

		public bool RevealJumpGates;

		public bool RevealWorldMap;

		public void RespawnAtUnit(UnitComponentHolder respawnDock)
		{
			Debug.Log("World is spawning player at: " + respawnDock, this);
			if (!(respawnDock != null))
			{
				return;
			}
			RespawnPlayerIfNull();
			if (Engine.LocalPlayer != null)
			{
				if (RespawnShipPrefab != null)
				{
					Unit unit = WorldHelper.SpawnUnitAndInstallComponents(RespawnShipPrefab, respawnDock.Unit.Sector);
					unit.Faction = Engine.LocalPlayer.Faction;
					if (!unit.Components.TryDockInUnit(respawnDock.Unit))
					{
						unit.Sector = respawnDock.Unit.Sector;
						unit.transform.localPosition = respawnDock.Unit.GetSafeUndockSectorPosition(unit);
					}
					unit.UpdateGasCloud();
				}
				Engine.LocalPlayer.Person.CurrentUnit = respawnDock.Unit;
			}
			else
			{
				Debug.LogError("Cannot respawn player at unit. No player set", this);
			}
		}

		public void RespawnPlayerIfNull()
		{
			if (Engine.LocalPlayer == null)
			{
				Engine.LocalPlayer = SpawnNewPlayer();
			}
		}

		protected override void OnInit()
		{
			base.OnInit();
		}

		protected override void OnNewGame()
		{
			base.OnNewGame();
			if (RespawnOnNewGame)
			{
				OnRespawnOnNewGame();
				if (Engine.LocalPlayer != null && Engine.LocalPlayer.Sector == null)
				{
					Debug.LogWarning("Spawned player but pilot does not have a scene", this);
				}
			}
			if (Seeder != null && Seeder.Settings.DiscoverySettings != null)
			{
				if (Seeder.Settings.DiscoverySettings.DiscoverEverything)
				{
					Engine.LocalPlayer.Faction.Intel.DiscoverAllSectors();
					Engine.LocalPlayer.Faction.Intel.EnterAllWormholes(includeUnstable: false);
					EngineASX.Instance.EnumerateUnitsWithPredicate((Unit unit) =>
					{
						Engine.LocalPlayer.Faction.Intel.DiscoverUnit(unit);
					}, (Unit unit) => unit != null && unit.Faction != null && unit.IsDiscoverableType && (unit.UnitType == UnitType.Station || unit.UnitType == UnitType.Asteroid));
					Engine.LocalPlayer.Faction.InvalidateTraderTargets();
				}
				else if (Seeder.Settings.DiscoverySettings.DiscoverAllNormalJumpGates)
				{
					Engine.LocalPlayer.Faction.Intel.EnterAllWormholes(includeUnstable: false);
				}
			}
			if (DiscoverStaticUnitsInInitialScene)
			{
				DiscoverStaticUnitsInCurrentScene();
			}
			if (DiscoverStationsInInitialScene)
			{
				DiscoverStationUnitsInScene();
			}
			if (DiscoverNormalJumpGatesInInitialScene)
			{
				DiscoverNormalJumpGatesInScene();
			}
			if (RevealWorldMap)
			{
				Engine.LocalPlayer.Faction.Intel.DiscoverAllSectors();
			}
			if (RevealJumpGates)
			{
				Engine.LocalPlayer.Faction.Intel.DiscoverAllWormholes();
			}
			AutoSaveGameIfRequired();
		}

		private void AutoSaveGameIfRequired()
		{
			if (Permissions.AllowSaving && GameController.Instance.GameSaveEnabled && Engine.LocalUnit != null && (!Engine.GameSettings.SaveOnlyWhenDocked || Engine.LocalUnit.GetRootUnit().IsStatic))
			{
				// What this save is about to record as the player's faction
				// name, said every time. The creation autosave is the one with
				// no second chance: nothing has been played yet, so nothing
				// will be written over it. DefaultFactionName has to be
				// applied before this, and the way to know is to see it in
				// here.
				Faction saving = Engine.LocalPlayer == null
					? null
					: Engine.LocalPlayer.Faction;

				Debug.Log("Saving: faction recorded as '"
					+ (saving == null ? "<no faction>" : saving.Name) + "'");

				Engine.AutoSaveIfPossible();
			}
		}

		protected virtual void OnRespawnOnNewGame()
		{
			UnitComponentHolder unitComponentHolder = NewGameSpawnLocation;
			if (unitComponentHolder == null)
			{
				unitComponentHolder = NewGameSpawnLocations.GetRandom();
			}
			if (unitComponentHolder != null)
			{
				RespawnAtUnit(unitComponentHolder);
			}
			else
			{
				Respawn();
			}
		}

		private UnitComponentHolder GetRespawnUnit(Sector searchSector, Vector3 searchSectorPosition)
		{
			UnitComponentHolder unitComponentHolder = null;
			float num = 0f;
			for (int i = 0; i < RespawnTargets.Count; i++)
			{
				UnitComponentHolder unitComponentHolder2 = RespawnTargets[i];
				if (Engine.LocalPlayer == null || Engine.LocalPlayer.Faction.Intel.IsSectorDiscovered(unitComponentHolder2.Unit.Sector))
				{
					float num2 = 0f;
					num2 = ((!(unitComponentHolder2.Unit.Sector == searchSector)) ? ((float)unitComponentHolder2.Unit.Sector.GetJumpDistanceTo(searchSector) * 100000f) : Vector3.Distance(unitComponentHolder2.Unit.SectorPosition, searchSectorPosition));
					if (unitComponentHolder == null || num2 < num)
					{
						unitComponentHolder = unitComponentHolder2;
					}
				}
			}
			return unitComponentHolder;
		}

		private void Respawn()
		{
			UnitComponentHolder respawnUnit = GetRespawnUnit(Engine.ActiveSector, Engine.ActiveSector.ToLocalPosition(GameController.Instance.MainCamera.transform.position));
			RespawnAtUnit(respawnUnit);
		}
	}
}
