using System.Linq;
using System.Text;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Missions;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using UnityEngine;

namespace OpenFrontier.IP.Engine.MissionSpecs
{
	public class DestroyGroupMissionSpec : MissionSpec
	{
		public FleetSpawnParams TargetGroupParams;

		public override JobType JobType => JobType.DestroyGroup;

		public override int CalculateRewardCredits(float profitability)
		{
			DestroyGroupMissionSpecSettings destroyGroupMissionSpecSettings = GameController.Instance.GameSettings.MissionSpecSettings.DestroyGroupMissionSpecSettings;
			float num = destroyGroupMissionSpecSettings.MinReward;
			foreach (FleetSpawnShipParams ship in TargetGroupParams.Ships)
			{
				num += ship.UnitClass.CombatRating * destroyGroupMissionSpecSettings.RewardCombatRatingMultiplier;
			}
			float num2 = Mathf.Lerp(1f, GameController.Instance.GameSettings.MissionSettings.MissionsMaxRewardRandomness, profitability);
			num *= num2;
			return Mathf.CeilToInt(num);
		}

		public override int CalculateProfitCredits(float profitability)
		{
			return CalculateRewardCredits(profitability);
		}

		public override string CalculateMissionSpecTitle()
		{
			if (Engine != null && TargetGroupParams.Faction != null)
			{
				Color factionHostilityColor = TargetGroupParams.Faction.GetFactionHostilityColor(EngineASX.Instance.LocalFaction);
				return $"Destroy {UnityRichTextHelper.Color(TargetGroupParams.Faction.GetShortNameElseLong(), factionHostilityColor)} ships";
			}
			return null;
		}

		public override bool IsValid()
		{
			if (TargetGroupParams.Ships.Count > 0 && TargetGroupParams.Faction != null)
			{
				return TargetGroupParams.Faction.IsValidInGame;
			}
			return false;
		}

		public override string CalculateBrief()
		{
			StringBuilder stringBuilder = new StringBuilder();
			Color factionHostilityColor = TargetGroupParams.Faction.GetFactionHostilityColor(EngineASX.Instance.LocalFaction);
			stringBuilder.AppendFormat("We have discovered the location of a group of {0} ships that need eliminating. Head to {1} and destroy the following ships:", UnityRichTextHelper.Color(TargetGroupParams.Faction.GetShortNameElseLong(), factionHostilityColor), UnityRichTextHelper.Color(TargetGroupParams.TargetSector.Name, Engine.GameSettings.TextSceneColor));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			UnitClassListFormatter.FormatList(stringBuilder, TargetGroupParams.Ships.Select((FleetSpawnShipParams e) => e.UnitClass).ToArray(), 0, excludeCountForSingleItems: false, colorEncoding: true);
			Unit localUnit = Engine.LocalUnit;
			if (localUnit != null && localUnit.IsOwnedByPlayer && localUnit.UnitType == UnitType.Ship && localUnit.UnitClass.CombatRating < GetCombatRatingOfTargetShips())
			{
				stringBuilder.AppendLine();
				string text = "We suggest that your current vessel is not powerful enough alone for this mission";
				text = TextFormattingHelper.WarningColor(text, Engine);
				stringBuilder.AppendLine(text);
			}
			return stringBuilder.ToString();
		}

		private float GetCombatRatingOfTargetShips()
		{
			float num = 0f;
			foreach (FleetSpawnShipParams ship in TargetGroupParams.Ships)
			{
				num += ship.UnitClass.CombatRating;
			}
			return num;
		}

		protected override Mission createMission()
		{
			DestroyUnitsMission component = Object.Instantiate(MissionPrefab).GetComponent<DestroyUnitsMission>();
			Fleet fleet = FleetSpawn.SpawnGroupFromParams(TargetGroupParams);
			for (int i = 0; i < fleet.NpcPilots.Count; i++)
			{
				fleet.NpcPilots[i].Settings.AllowDitchShip = false;
				Unit currentUnit = fleet.NpcPilots[i].CurrentUnit;
				component.TargetUnits.Add(currentUnit);
				ModdedUnitSeederSettings moddedUnitSettings = GameController.Instance.GameSettings.MissionSpecSettings.DestroyGroupMissionSpecSettings.ModdedUnitSettings;
				if (ModdedUnitSeeder.ShouldModUnitRandomly(currentUnit, moddedUnitSettings))
				{
					ModdedUnitSeeder.ModUnitAndChangeEquipment(moddedUnitSettings, currentUnit, 1f);
				}
			}
			component.TargetFaction = TargetGroupParams.Faction;
			component.TargetSector = TargetGroupParams.TargetSector;
			component.TargetGroup = fleet;
			component.TargetGroup.ExcludeFromFactionAI = true;
			ProvideIntel(component);
			return component;
		}

		private void ProvideIntel(DestroyUnitsMission mission)
		{
			if (!(EngineASX.Instance.LocalFaction.Intel != null))
			{
				return;
			}
			if (Faction.Intel != null && mission.TargetSector != Unit.Sector)
			{
				Faction.Intel.TrySharePathWithFaction(Unit.Sector, mission.TargetSector, EngineASX.Instance.LocalFaction);
			}
			foreach (Unit targetUnit in mission.TargetUnits)
			{
				EngineASX.Instance.LocalFaction.Intel.DiscoverUnit(targetUnit);
			}
		}
	}
}
