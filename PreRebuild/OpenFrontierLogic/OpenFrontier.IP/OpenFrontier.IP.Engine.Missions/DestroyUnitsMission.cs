using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.MissionObjectives;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Missions
{
	public class DestroyUnitsMission : Mission
	{
		public MissionObjective DestroyShipsObjective;

		private bool hasSetGroupHostileToPlayer;

		public MissionObjective MainObjective;

		private float nextUpdateTIme;

		private Faction targetFaction;

		private Fleet targetGroup;

		private Sector targetSector;

		public List<Unit> TargetUnits = new List<Unit>(8);

		public override MissionType MissionType => MissionType.DestroyGroup;

		public Sector TargetSector
		{
			get
			{
				return targetSector;
			}
			set
			{
				targetSector = value;
			}
		}

		public Faction TargetFaction
		{
			get
			{
				return targetFaction;
			}
			set
			{
				targetFaction = value;
			}
		}

		public Fleet TargetGroup
		{
			get
			{
				return targetGroup;
			}
			set
			{
				targetGroup = value;
			}
		}

		public bool HasSetGroupHostileToPlayer
		{
			get
			{
				return hasSetGroupHostileToPlayer;
			}
			set
			{
				hasSetGroupHostileToPlayer = value;
			}
		}

		public override void AddWaypointsToList(List<PlayerWaypoint> waypoints)
		{
			if (TargetUnits.Count <= 0)
			{
				return;
			}
			foreach (Unit targetUnit in TargetUnits)
			{
				if (targetUnit != null && targetUnit.IsValidAndNotDestroyed)
				{
					waypoints.Add(PlayerWaypoint.FromUnit(targetUnit));
				}
			}
		}

		public override string CalculateTitle()
		{
			return $"Destroy {targetFaction.GetShortNameElseLong()} ships";
		}

		public override string GetMissionObjectiveText(MissionObjective objective)
		{
			if (objective == DestroyShipsObjective)
			{
				Color factionHostilityColor = targetFaction.GetFactionHostilityColor(EngineASX.Instance.LocalFaction);
				return $"Destroy {UnityRichTextHelper.Color(targetFaction.GetShortNameElseLong(), factionHostilityColor)} ships in the {UnityRichTextHelper.Color(targetSector.Name, GameController.Instance.GameSettings.TextSceneColor)} sector";
			}
			return base.GetMissionObjectiveText(objective);
		}

		protected override void update()
		{
			base.update();
			if (!IsFinished && Time.time > nextUpdateTIme)
			{
				SetGroupHostileToPlayerWhenNeeded();
				nextUpdateTIme = Time.time + 1f;
				if (EvaluateComplete())
				{
					MainObjective.Complete(success: true);
				}
			}
		}

		protected override void OnAbortedMission()
		{
			base.OnAbortedMission();
			if (targetGroup != null)
			{
				targetGroup.ClearOrders();
				targetGroup.ExcludeFromFactionAI = false;
			}
		}

		private void SetGroupHostileToPlayerWhenNeeded()
		{
			if (hasSetGroupHostileToPlayer)
			{
				return;
			}
			Unit unit = TargetUnits[0];
			Unit localUnit = Engine.LocalUnit;
			if (localUnit != null && unit != null && Engine.LocalPlayerSector == unit.Sector && Vector3.Distance(unit.SectorPosition, localUnit.SectorPosition) < 1200f)
			{
				if (!unit.Faction.IsHostileTo(Engine.LocalPlayer.Faction))
				{
					unit.Faction.SetAsHostileTo(Engine.LocalFaction);
				}
				unit.NpcPilot.Person.RaiseDialogEventRandomly(Engine.DialogEvents.PlayerStartDestroyMission);
				hasSetGroupHostileToPlayer = true;
			}
		}

		private bool EvaluateComplete()
		{
			foreach (Unit targetUnit in TargetUnits)
			{
				if (targetUnit != null && !targetUnit.IsDestroyed)
				{
					return false;
				}
			}
			return true;
		}
	}
}
