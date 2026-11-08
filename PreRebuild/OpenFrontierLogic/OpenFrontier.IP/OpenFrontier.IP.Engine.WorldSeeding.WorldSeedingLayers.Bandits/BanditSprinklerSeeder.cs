using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Scenarios;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.Bandits
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class BanditSprinklerSeeder : MonoBehaviour
	{
		public BanditSprinklerSeederSettings BanditSprinklerSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (!world.Seeder.Settings.FactionSeederSettings.SeedBanditFactions)
			{
				return;
			}
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding stations...", this, 1);
			}
			BanditSprinklerSeederSettings = world.Seeder.Settings.BanditSprinklerSeederSettings;
			List<UnitClass> possibleShipClasses = EngineASX.Instance.UnitClasses.Where((UnitClass e) => e.IsArmed && e.IsUsable && e.UnitType == UnitType.Ship && e.ShipType == ShipType.Normal && !e.IsBarebonesShip && e.HasStrategy(FactionStrategy.War)).ToList();
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				float num = Mathf.Lerp(BanditSprinklerSeederSettings.ProbabilityOfSpawnMultiplierMin, BanditSprinklerSeederSettings.ProbabilityOfSpawnMultiplierMax, EngineASX.Instance.World.Seeder.Settings.FactionSeederSettings.BanditPower01);
				float num2 = Mathf.Lerp(1.5f, 0.1f, sector.SecurityLevel) * num;
				if (Random.value < num2)
				{
					SeedInSector(sector, possibleShipClasses);
				}
			}
		}

		public void SeedInSector(Sector sector, IEnumerable<UnitClass> possibleShipClasses)
		{
			Faction faction = FactionSpawner.CreateFactionAndAI(BanditSprinklerSeederSettings.BanditsSpawnType);
			faction.Name = sector.Name + " Bandits";
			faction.ShortName = "Bandits";
			faction.ChangeHomeSector(sector);
			faction.FactionAI.AssignStrategyIfNull();
			faction.FactionAI.AISettings.HostileWithAll = BanditSprinklerSeederSettings.ManualHostileWithAll;
			int num = Maths.RandomIntWithPower(BanditSprinklerSeederSettings.MinShipCount, BanditSprinklerSeederSettings.MaxShipCount, BanditSprinklerSeederSettings.ShipCountPower);
			FactionBountyBoard nearestBountyBoardToSector = EngineASX.Instance.GetNearestBountyBoardToSector(sector);
			for (int i = 0; i < num; i++)
			{
				float num2 = Random.Range(BanditSprinklerSeederSettings.MinSectorDistanceMultiplier, BanditSprinklerSeederSettings.MaxSectorDistanceMultiplier);
				float num3 = GameController.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginLowerBound * 0.9f * num2;
				_ = Geometry.RandomXZUnitVector() * num3;
				Vector3? position = null;
				for (int j = 0; j < 3; j++)
				{
					Vector3 randomSectorPositionOutsideGateDistance = sector.GetRandomSectorPositionOutsideGateDistance();
					if (!Physics.CheckSphere(sector.ToWorldPosition(randomSectorPositionOutsideGateDistance), 1500f, GameController.Instance.StationsMask, QueryTriggerInteraction.Collide))
					{
						position = randomSectorPositionOutsideGateDistance;
						break;
					}
				}
				float maxShipCombatRating = Maths.RandomFloatWithPower(BanditSprinklerSeederSettings.MinShipCombatRating, BanditSprinklerSeederSettings.MaxShipCombatRating, BanditSprinklerSeederSettings.ShipCombatRatingPower);
				UnitClass random = possibleShipClasses.Where((UnitClass e) => e.CombatRating < maxShipCombatRating).GetRandom();
				if (!(random != null) || !position.HasValue)
				{
					continue;
				}
				Unit unit = SprinkleAtPosition(sector, faction, position, random);
				if (!(Random.value < BanditSprinklerSeederSettings.ProbabilityOfBounty) || !(nearestBountyBoardToSector != null) || !(nearestBountyBoardToSector.Faction != faction) || nearestBountyBoardToSector.Faction.FactionType == FactionType.Bandit)
				{
					continue;
				}
				int bountyValueToPlaceOnUnit = FactionBountySeeder.GetBountyValueToPlaceOnUnit(unit);
				double timeOfSighting = 0.0 - (double)Random.Range(20f, 300f);
				FactionBountyItem factionBountyItem = BountyHelper.AddBountyWithRounding(nearestBountyBoardToSector.Faction, nearestBountyBoardToSector, unit.GetPilot(), bountyValueToPlaceOnUnit, timeOfSighting, updateLastKnownPosition: false);
				if (factionBountyItem != null)
				{
					factionBountyItem.LastKnownSector = sector;
					if (Random.value < BanditSprinklerSeederSettings.ProbabilityOfBountyKnownPosition)
					{
						factionBountyItem.LastKnownSectorPosition = unit.transform.localPosition + Geometry.RandomXZUnitVector() * Random.Range(300f, 1500f);
						factionBountyItem.TimeOfLastSighting = 0f - Random.Range(50f, 500f);
					}
				}
			}
		}

		private Unit SprinkleAtPosition(Sector sector, Faction faction, Vector3? position, UnitClass shipClass)
		{
			Unit unit = WorldHelper.SpawnUnitAndInstallComponents(shipClass.UnitPrefab, sector, position.Value);
			unit.transform.localRotation = Geometry.RandomYRotation();
			Fleet fleet = faction.FactionAI.CreateFleetAndNpcForUnit(unit);
			fleet.FleetStrategy = FactionStrategy.War;
			if (Random.value < 0.9f)
			{
				ProtectOrder protectOrder = faction.FactionAI.OrderFleetToProtectSectorTarget(fleet, SectorTarget.FromSectorPosition(sector, position.Value));
				protectOrder.MaxDuration = 0f;
				protectOrder.AllowTimeout = false;
			}
			fleet.Settings.TargetInterceptionLowerDistance = 1500f;
			fleet.Settings.TargetInterceptionUpperDistance = 1800f;
			fleet.ExcludeFromFactionAI = true;
			fleet.SetHomeBaseToSectorPosition(sector, position.Value);
			return unit;
		}
	}
}
