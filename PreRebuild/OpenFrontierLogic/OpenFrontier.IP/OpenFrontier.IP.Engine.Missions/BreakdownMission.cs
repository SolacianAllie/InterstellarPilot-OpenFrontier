using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.MissionObjectives;

namespace OpenFrontier.IP.Engine.Missions
{
	public class BreakdownMission : Mission
	{
		public MissionObjective FlyToShipObjective;

		public MissionObjective ReturnToBaseObjective;

		public Unit BreakdownUnit;

		public Unit BaseUnit;

		public override MissionType MissionType => MissionType.Breakdown;

		public override bool IsValid
		{
			get
			{
				if (base.IsValid && BaseUnit != null)
				{
					return BaseUnit.IsValidAndNotDestroyed;
				}
				return false;
			}
		}

		protected override void OnInit()
		{
			base.OnInit();
			if (OwnerFaction != null)
			{
				if (BreakdownUnit != null)
				{
					OwnerFaction.Intel.DiscoverUnit(BreakdownUnit);
				}
				if (BaseUnit != null)
				{
					OwnerFaction.Intel.DiscoverUnit(BaseUnit);
				}
			}
		}

		public override string CalculateTitle()
		{
			return $"Recover the {BreakdownUnit.GetClassAndSeriesName()}";
		}

		protected override void update()
		{
			base.update();
			if (BreakdownUnit != null && BreakdownUnit.IsValidAndNotDestroyed)
			{
				if (!FlyToShipObjective.IsComplete)
				{
					Unit playerUnit = Engine.PlayerUnit;
					if (playerUnit != null && playerUnit.IsPilottedByPlayer())
					{
						float requiredDistanceFromBreakdownUnit = GameController.Instance.GameSettings.MissionSettings.BreakdownMissionSettings.RequiredDistanceFromBreakdownUnit;
						if (playerUnit.Sector == BreakdownUnit.Sector && playerUnit.IsWithinDistanceOf(BreakdownUnit, requiredDistanceFromBreakdownUnit))
						{
							FlyToShipObjective.Complete(success: true);
						}
					}
				}
				else
				{
					float requiredDistanceFromBase = GameController.Instance.GameSettings.MissionSettings.BreakdownMissionSettings.RequiredDistanceFromBase;
					if (BreakdownUnit.Sector == BaseUnit.Sector && BreakdownUnit.IsWithinDistanceOf(BaseUnit, requiredDistanceFromBase))
					{
						ReturnToBaseObjective.Complete(success: true);
					}
				}
			}
			else
			{
				Finish(accomplished: false);
			}
		}

		public override void AddWaypointsToList(List<PlayerWaypoint> waypoints)
		{
			if (BreakdownUnit != null && BreakdownUnit.IsValidAndNotDestroyed)
			{
				waypoints.Add(PlayerWaypoint.FromUnit(BreakdownUnit));
			}
			if (FlyToShipObjective.IsComplete && BaseUnit != null && BaseUnit.IsValidAndNotDestroyed)
			{
				waypoints.Add(PlayerWaypoint.FromUnit(BaseUnit));
			}
		}

		protected override void OnFinishing(bool accomplished)
		{
			base.OnFinishing(accomplished);
			if (BreakdownUnit != null)
			{
				BreakdownUnit.SafeDestroy();
			}
		}

		public override string GetMissionObjectiveText(MissionObjective objective)
		{
			if (objective == FlyToShipObjective)
			{
				return $"Fly to the ship in the {TextFormattingHelper.GetColouredSceneName(BreakdownUnit.Sector)} sector";
			}
			if (objective == ReturnToBaseObjective)
			{
				return $"Return to {GetFormattedUnitNameAndFaction(BaseUnit)} in the {TextFormattingHelper.GetColouredSceneName(BaseUnit.Sector)} sector";
			}
			return base.GetMissionObjectiveText(objective);
		}

		private string GetFormattedUnitNameAndFaction(Unit unit)
		{
			string text = UnityRichTextHelper.Color(unit.GetFriendlyName(), GameController.Instance.GameSettings.TextUnitNameColor);
			if (unit.Faction != null)
			{
				text = $"{text} ({unit.Faction.GetShortNameElseLong()})";
			}
			return text;
		}
	}
}
