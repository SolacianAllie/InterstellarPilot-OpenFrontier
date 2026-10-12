using System.Text;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Missions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.MissionSpecs
{
	public class BreakdownMissionSpec : MissionSpec
	{
		public UnitClass BreakdownUnitClass;

		public Sector BreakdownDestinationSector;

		public Vector3 BreakdownDestinationSectorPosition = Vector3.zero;

		public override JobType JobType => JobType.Breakdown;

		public override bool IsValid()
		{
			return base.IsValid();
		}

		public override int CalculateProfitCredits(float profitability)
		{
			return CalculateReward(profitability);
		}

		public override int CalculateRewardCredits(float profitability)
		{
			return CalculateReward(profitability);
		}

		public override string CalculateMissionSpecTitle()
		{
			return "Ship Recovery";
		}

		public override string CalculateBrief()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("One of our ships has broken down in the {0} sector.", TextFormattingHelper.GetColouredSceneNameAndDistance(BreakdownDestinationSector, Engine.ActiveSector));
			stringBuilder.AppendLine();
			stringBuilder.AppendFormat("Head to the {0} class ship and tractor it back to this station.", BreakdownUnitClass.GetClassAndSeriesName());
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			AppendRequiredTractorBeam(stringBuilder);
			stringBuilder.AppendLine();
			return stringBuilder.ToString();
		}

		private void AppendRequiredTractorBeam(StringBuilder strBuilder)
		{
			string value = "A ship with a Tractor Beam is required for this mission.";
			strBuilder.AppendLine(value);
		}

		protected override Mission createMission()
		{
			BreakdownMission component = Object.Instantiate(MissionPrefab.gameObject).GetComponent<BreakdownMission>();
			component.BaseUnit = Unit;
			component.BreakdownUnit = CreateBreakdownUnit(null);
			component.BreakdownUnit.transform.localRotation = Maths.RandomYRotation();
			component.BreakdownUnit.Faction = Unit.Faction;
			SetBreakdownUnitPosition(component.BreakdownUnit);
			FactionAIBase factionAI = Unit.Faction.FactionAI;
			Person person = WorldHelper.SpawnRandomizedPerson(factionAI.GetPilotPrefab());
			person.Faction = Unit.Faction;
			person.CurrentUnit = component.BreakdownUnit;
			person.AssignFirstPilotRankIfNull();
			factionAI.AISettings.ExcludedUnitIds.Add(component.BreakdownUnit.UniqueId);
			ProvideIntel(component);
			return component;
		}

		private void ProvideIntel(BreakdownMission mission)
		{
			Sector sector = mission.BreakdownUnit.Sector;
			if (EngineASX.Instance.LocalFaction.Intel != null)
			{
				if (Faction.Intel != null && sector != Unit.Sector)
				{
					Faction.Intel.TrySharePathWithFaction(Unit.Sector, sector, EngineASX.Instance.LocalFaction);
				}
				EngineASX.Instance.LocalFaction.Intel.DiscoverUnit(mission.BreakdownUnit);
			}
		}

		private void SetBreakdownUnitPosition(Unit breakdownUnit)
		{
			breakdownUnit.Sector = BreakdownDestinationSector;
			breakdownUnit.transform.localPosition = BreakdownDestinationSectorPosition;
		}

		private Unit CreateBreakdownUnit(Sector sector)
		{
			return WorldHelper.SpawnUnitAndInstallComponents(BreakdownUnitClass.UnitPrefab, sector);
		}

		private int CalculateReward(float profitability)
		{
			BreakdownMissionSpecSettings breakdownMissionSpecSettings = GameController.Instance.GameSettings.MissionSpecSettings.BreakdownMissionSpecSettings;
			float num = CalculateDistanceToBreakdownUnit();
			float num2 = (breakdownMissionSpecSettings.BaseReward + num * breakdownMissionSpecSettings.RewardPerMetreTravelled) * (1f + breakdownMissionSpecSettings.RewardMultiplierPerMass * BreakdownUnitClass.UnitPrefab.Mass);
			float num3 = Mathf.Lerp(1f, GameController.Instance.GameSettings.MissionSettings.MissionsMaxRewardRandomness, profitability);
			return Mathf.CeilToInt(num2 * num3);
		}

		private float CalculateDistanceToBreakdownUnit()
		{
			if (BreakdownDestinationSector == Unit.Sector)
			{
				return Vector3.Distance(BreakdownDestinationSectorPosition, Unit.SectorPosition);
			}
			Unit unit = WorldHelper.FindNearestStationOrWormhole(BreakdownDestinationSector, BreakdownDestinationSectorPosition);
			if (unit != null)
			{
				float? distance = EngineASX.Instance.DistanceCalculator.GetDistance(unit, Unit);
				if (distance.HasValue)
				{
					return distance.Value;
				}
			}
			int jumpDistanceTo = Unit.Sector.GetJumpDistanceTo(BreakdownDestinationSector);
			return Vector3.Distance(BreakdownDestinationSectorPosition, Unit.SectorPosition) + (float)jumpDistanceTo * Unit.Sector.GetActualGateDistance() * 1.15f;
		}
	}
}
