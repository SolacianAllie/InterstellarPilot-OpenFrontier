using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Missions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.MissionSpecs
{
	public class DeliverShipMissionSpec : MissionSpec
	{
		public Unit DestinationUnit;

		public UnitClass UnitClass;

		public override JobType JobType => JobType.DeliverShip;

		public override bool IsValid()
		{
			if (base.IsValid() && UnitClass != null && DestinationUnit != null)
			{
				return DestinationUnit.IsDockable;
			}
			return false;
		}

		public override int CalculateProfitCredits(float profitability)
		{
			return CalculatePlayerReward(profitability);
		}

		public override int CalculateRewardCredits(float profitability)
		{
			return CalculatePlayerReward(profitability);
		}

		public override string CalculateBrief()
		{
			return $"We urgently need a {UnityRichTextHelper.Color(UnitClass.GetClassAndSeriesName(), Engine.GameSettings.TextUnitClassColor)} class ship delivered to {UnityRichTextHelper.Color(DestinationUnit.GetFriendlyName(), Engine.GameSettings.TextUnitNameColor)} in the {UnityRichTextHelper.Color(TextFormattingHelper.GetSectorNameAndDistance(DestinationUnit.Sector, Engine.ActiveSector), Engine.GameSettings.TextSceneColor)} sector. We will pay the full value of the ship plus a percentage for your trouble.";
		}

		public override string CalculateMissionSpecTitle()
		{
			if (UnitClass != null)
			{
				return $"Deliver a {UnityRichTextHelper.Color(UnitClass.GetClassAndSeriesName(), Engine.GameSettings.TextUnitClassColor)}";
			}
			return "Deliver a ship";
		}

		protected override Mission createMission()
		{
			DeliverShipMission component = Object.Instantiate(MissionPrefab.gameObject).GetComponent<DeliverShipMission>();
			component.UnitClass = UnitClass;
			component.Destination = DestinationUnit;
			ProvideIntel(component);
			return component;
		}

		private int CalculatePlayerReward(float profitability)
		{
			float num = Mathf.Lerp(1f, GameController.Instance.GameSettings.MissionSettings.MissionsMaxRewardRandomness, profitability);
			return Mathf.RoundToInt((float)UnitClass.SaleCost * GameController.Instance.GameSettings.MissionsDeliverShipPercentage * num) + GameController.Instance.GameSettings.MissionsDeliverShipBaseIncome;
		}

		private void ProvideIntel(DeliverShipMission mission)
		{
			Sector sector = DestinationUnit.Sector;
			if (EngineASX.Instance.LocalFaction.Intel != null)
			{
				if (Faction.Intel != null && sector != Unit.Sector)
				{
					Faction.Intel.TrySharePathWithFaction(Unit.Sector, sector, EngineASX.Instance.LocalFaction);
				}
				EngineASX.Instance.LocalFaction.Intel.DiscoverUnit(DestinationUnit);
			}
		}
	}
}
