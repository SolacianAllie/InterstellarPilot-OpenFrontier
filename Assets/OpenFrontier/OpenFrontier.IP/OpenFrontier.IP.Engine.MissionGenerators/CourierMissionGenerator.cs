using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.MissionGenerators
{
	public class CourierMissionGenerator : MissionGenerator
	{
		public float CargoAmountMinPower = 0.5f;

		public float CargoAmountMaxPower = 3f;

		public List<CargoClass> CargoClasses = new List<CargoClass>();

		public float MaxCargoLoad = 120f;

		public int MaxJumpDistance = 8;

		public float MaxJumpDistancePower = 4f;

		public float MinCargoLoad = 5f;

		public CourierMissionSpec MissionSpecPrefab;

		public override MissionManagerMissionType MissionType => MissionManagerMissionType.Courier;

		private Unit FindRandomTargetStation(Faction missionGiverFaction, Unit currentUnit)
		{
			_ = EngineASX.Instance;
			int maxJumpDistance = Maths.RandomIntWithPower(0, MaxJumpDistance, MaxJumpDistancePower);
			SectorFinder.FindNavigableDiscoveredSectorsWithinJumpDistanceOf(currentUnit.Sector, maxJumpDistance, missionGiverFaction);
			if (SectorFinder.Results.Count > 0)
			{
				List<Unit> unitsByType = SectorFinder.Results.GetRandom().Sector.GetUnitsByType(UnitType.Station);
				if (unitsByType != null)
				{
					return unitsByType.Where((Unit e) => e != currentUnit && e.IsDockable && e.Faction != null && e.Faction.HasAttitudeToFaction(currentUnit.Faction) && !currentUnit.IsHostileToOrAlwaysHostileToTwoWay(e.Faction) && e.GetJumpDistTo(currentUnit) <= maxJumpDistance).GetRandom();
				}
			}
			return null;
		}

		protected override MissionSpec generateMissionSpec(Unit unit, Faction faction, EngineASX engine)
		{
			Unit unit2 = FindRandomTargetStation(faction, unit);
			if (unit2 != null)
			{
				CargoBayItem deliverableCargoType = GetDeliverableCargoType(unit2);
				if (deliverableCargoType != null)
				{
					CourierMissionSpec courierMissionSpec = UnityObjectHelper.InstantiateAndGetComponent(MissionSpecPrefab, unit.transform);
					courierMissionSpec.PickupUnit = unit;
					courierMissionSpec.DestinationUnit = unit2;
					courierMissionSpec.CargoItem = deliverableCargoType;
					return courierMissionSpec;
				}
				Debug.LogWarning($"Cannot generate courier mission for faction: {faction}. No delivered cargo types found.", this);
			}
			else if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Cannot generate courier mission for faction: {faction}. No destination unit found.", this, 2);
			}
			return null;
		}

		private CargoBayItem GetDeliverableCargoType(Unit destination)
		{
			CargoClass random = CargoClasses.GetRandom();
			if (random != null)
			{
				int num = DetermineCargoQuantity(random);
				if (num > 0)
				{
					return new CargoBayItem
					{
						CargoClass = random,
						Quantity = num
					};
				}
			}
			return null;
		}

		private int DetermineCargoQuantity(CargoClass cargoClass)
		{
			if (cargoClass.Volume > 0f)
			{
				long cachedNetWorth = EngineASX.Instance.LocalPlayer.Faction.GetCachedNetWorth();
				float p = Mathf.Lerp(CargoAmountMinPower, CargoAmountMaxPower, 1f - Mathf.Clamp01((float)cachedNetWorth / 2500000f));
				return Mathf.CeilToInt(Mathf.Lerp(MinCargoLoad, MaxCargoLoad, Mathf.Pow(Random.value, p)) / cargoClass.Volume);
			}
			Debug.LogError("Cannot determine cargo quantity. Cargo volume is not greater than zero", this);
			return 0;
		}
	}
}
