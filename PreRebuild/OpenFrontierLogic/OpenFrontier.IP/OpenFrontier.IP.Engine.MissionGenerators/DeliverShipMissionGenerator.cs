using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.Unity.Utils;

namespace OpenFrontier.IP.Engine.MissionGenerators
{
	public class DeliverShipMissionGenerator : MissionGenerator
	{
		public DeliverShipMissionSpec MissionSpecPrefab;

		public override MissionManagerMissionType MissionType => MissionManagerMissionType.DeliverShip;

		protected override MissionSpec generateMissionSpec(Unit unit, Faction faction, EngineASX engine)
		{
			Unit destination = GetDestination(faction);
			if (destination != null)
			{
				long cachedNetWorth = engine.LocalFaction.GetCachedNetWorth();
				UnitClass random = GetDeliverShipMissionUnitTypes(engine, unit, destination, cachedNetWorth).GetRandom();
				if (random != null)
				{
					DeliverShipMissionSpec deliverShipMissionSpec = UnityObjectHelper.InstantiateAndGetComponent(MissionSpecPrefab, unit.transform);
					deliverShipMissionSpec.DestinationUnit = destination;
					deliverShipMissionSpec.UnitClass = random;
					return deliverShipMissionSpec;
				}
			}
			return null;
		}

		private Unit GetDestination(Faction faction)
		{
			return faction.GetUnitsByType(UnitType.Station)?.Where((Unit e) => e != null && e.IsValidAndNotDestroyed && e.IsDockable).GetRandom();
		}

		private List<UnitClass> GetDeliverShipMissionUnitTypes(EngineASX engine, Unit missionsourceUnit, Unit destinationUnit, long playerNetWorth)
		{
			return engine.UnitClasses.Where((UnitClass e) => CanDeliverUnitClassToDestination(e, missionsourceUnit, destinationUnit, playerNetWorth)).ToList();
		}

		private bool UnitHasExistingDeliverShipMissionSpec(Unit missionSourceUnit, UnitClass unitClass, Unit destinationUnit)
		{
			List<MissionSpec> jobsAtUnit = EngineASX.Instance.GetJobsAtUnit(missionSourceUnit);
			if (jobsAtUnit != null)
			{
				foreach (MissionSpec item in jobsAtUnit)
				{
					DeliverShipMissionSpec deliverShipMissionSpec = item as DeliverShipMissionSpec;
					if (deliverShipMissionSpec != null && deliverShipMissionSpec.UnitClass == unitClass && deliverShipMissionSpec.DestinationUnit == destinationUnit)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool CanDeliverUnitClassToDestination(UnitClass unitClass, Unit missionSourceUnit, Unit destinationUnit, long playerNetWorth)
		{
			if (UnitHasExistingDeliverShipMissionSpec(missionSourceUnit, unitClass, destinationUnit))
			{
				return false;
			}
			if (unitClass.UnitType == UnitType.Ship && unitClass.SeedInSandbox && unitClass.SaleCost < playerNetWorth && unitClass.IsUsable && !unitClass.IsBarebonesShip && (unitClass.RequiredProduct == null || unitClass.RequiredProduct.IsPurchased) && !unitClass.ExcludeFromDeliverShipMission && !missionSourceUnit.IsSellerOfUnitClass(unitClass))
			{
				return !destinationUnit.IsSellerOfUnitClass(unitClass);
			}
			return false;
		}
	}
}
