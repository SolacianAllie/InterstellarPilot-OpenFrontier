using System.Text;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Missions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.MissionSpecs
{
	public class CourierMissionSpec : MissionSpec
	{
		public CargoBayItem CargoItem;

		public Unit DestinationUnit;

		public Unit PickupUnit;

		public override JobType JobType => JobType.Courier;

		public override bool IsValid()
		{
			if (base.IsValid() && DestinationUnit != null && DestinationUnit.IsDockable && DestinationUnit.Faction != null)
			{
				return DestinationUnit.Faction.IsValidInGame;
			}
			return false;
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
			return "Courier Needed";
		}

		public override string CalculateBrief()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("The following goods need to be delivered to {0} in the {1} sector:", UnityRichTextHelper.Color(UnitNamer.GetNameAndFactionShortNameInParenthesis(DestinationUnit), Engine.GameSettings.TextUnitNameColor), UnityRichTextHelper.Color(TextFormattingHelper.GetSectorNameAndDistance(DestinationUnit.Sector, Engine.ActiveSector), Engine.GameSettings.TextSceneColor));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine($"{CargoItem.Quantity} x {UnityRichTextHelper.Color(CargoItem.CargoClass.ClassName, Engine.GameSettings.TextCargoColor)}");
			stringBuilder.AppendLine();
			AppendRequiredFreeCargoSpace(stringBuilder);
			stringBuilder.AppendLine();
			AppendCurrentShipFreeCargoSpace(stringBuilder, CargoItem.Load);
			AppendHostileTargetWarning(stringBuilder);
			return stringBuilder.ToString();
		}

		private void AppendRequiredFreeCargoSpace(StringBuilder strBuilder)
		{
			string value = $"A ship with {TextFormattingHelper.FormatCargoVolume(CargoItem.Load)} units of free cargo space is required.";
			strBuilder.AppendLine(value);
		}

		private void AppendCurrentShipFreeCargoSpace(StringBuilder strBuilder, float requiredSpace)
		{
			Unit localUnit = Engine.LocalUnit;
			if (localUnit.IsOwnedByPlayer && localUnit.UnitType == UnitType.Ship && localUnit.CargoBayComponent != null)
			{
				string text = null;
				text = ((!(localUnit.CargoBayComponent.Usage > 0f)) ? $"Your current ship has {localUnit.CargoBayComponent.FreeSpace:N1} units of free space" : $"Your current ship has {localUnit.CargoBayComponent.FreeSpace:N1} out of {localUnit.CargoBayComponent.Capacity:N1} units of free space");
				if (requiredSpace > localUnit.CargoBayComponent.FreeSpace)
				{
					text = TextFormattingHelper.WarningColor(text, Engine);
				}
				strBuilder.AppendLine(text);
			}
		}

		private void AppendHostileTargetWarning(StringBuilder strBuilder)
		{
			_ = Engine.LocalUnit;
			if (DestinationUnit.Faction.IsHostileTo(Engine.LocalFaction))
			{
				strBuilder.AppendLine();
				strBuilder.AppendLine(UnityRichTextHelper.Color("Warning: You are currently at war with the destination faction", Engine.TextWarningColor));
			}
			else if (DestinationUnit.Faction.GetOpinion(Engine.LocalFaction) < -0.4f)
			{
				strBuilder.AppendLine();
				strBuilder.AppendLine(UnityRichTextHelper.Color("Warning: You currently have poor relations with the destination faction", Engine.TextWarningColor));
			}
		}

		public int CalculateJumpDist()
		{
			if (PickupUnit != null && DestinationUnit != null)
			{
				return PickupUnit.GetJumpDistTo(DestinationUnit);
			}
			return -1;
		}

		protected override Mission createMission()
		{
			CourierMission component = Object.Instantiate(MissionPrefab.gameObject).GetComponent<CourierMission>();
			component.PickupUnit = PickupUnit;
			component.DestinationUnit = DestinationUnit;
			component.CargoItem = CargoItem.Clone();
			ProvideIntel(component);
			return component;
		}

		private void ProvideIntel(CourierMission mission)
		{
			Sector sector = mission.DestinationUnit.Sector;
			Sector sector2 = mission.PickupUnit.Sector;
			if (EngineASX.Instance.LocalFaction.Intel != null)
			{
				if (Faction.Intel != null && sector != Unit.Sector)
				{
					Faction.Intel.TrySharePathWithFaction(Unit.Sector, sector, EngineASX.Instance.LocalFaction);
					Faction.Intel.TrySharePathWithFaction(Unit.Sector, sector2, EngineASX.Instance.LocalFaction);
				}
				EngineASX.Instance.LocalFaction.Intel.DiscoverUnit(mission.DestinationUnit);
				EngineASX.Instance.LocalFaction.Intel.DiscoverUnit(mission.PickupUnit);
			}
		}

		private int CalculateReward(float profitability)
		{
			GameSettings gameSettings = GameController.Instance.GameSettings;
			CourierMissionSettings courierMissionSettings = gameSettings.MissionSettings.CourierMissionSettings;
			float num = (float)courierMissionSettings.MinReward + CargoItem.Load * courierMissionSettings.RewardLoadMultiplier;
			float value = EngineASX.Instance.DistanceCalculator.GetDistance(PickupUnit, DestinationUnit).Value;
			float num2 = num + value * courierMissionSettings.RewardPerMetreTravelled;
			float num3 = Mathf.Lerp(1f, gameSettings.MissionSettings.MissionsMaxRewardRandomness, profitability);
			return Mathf.CeilToInt(num2 * num3);
		}

		public override void OnDisplayedToPlayer()
		{
			base.OnDisplayedToPlayer();
			if (DestinationUnit != null && DestinationUnit.Faction != null && Engine.LocalFaction != null && DestinationUnit.Faction != EngineASX.Instance.LocalFaction)
			{
				DestinationUnit.Faction.CreateAttitudeIfNone(Engine.LocalFaction);
			}
		}
	}
}
