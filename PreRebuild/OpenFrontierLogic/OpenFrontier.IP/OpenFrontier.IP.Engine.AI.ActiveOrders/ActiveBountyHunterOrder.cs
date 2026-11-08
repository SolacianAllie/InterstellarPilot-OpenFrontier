using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveBountyHunterOrder : ActiveFleetOrder
	{
		private Person targetPilot;

		private int? lastKnownTargetPilotUnitUniqueId;

		private float lastStartSearchTime = float.MinValue;

		public const float MinOpinionWithBountySource = -0.1f;

		public const float MaxOpinionWithTarget = 0.3f;

		public const float MinTargetScore = -3f;

		public AutonomousBountyHunterOrder AutonomousBountyHunterObjective;

		private BountyHunterSearchOperation searchOperation;

		public Person TargetPilot
		{
			get
			{
				return targetPilot;
			}
			set
			{
				if (targetPilot != value)
				{
					targetPilot = value;
				}
			}
		}

		protected override void onInit()
		{
			base.onInit();
			if (TargetPilot == null)
			{
				StartSearchForNewTarget();
			}
		}

		public bool IsTargetPilotValid(Person pilot)
		{
			Sector scene;
			Vector3 sectorPosition;
			if (pilot != null && pilot.IsActiveInGame && pilot.Faction != null && pilot.Faction != fleet.Faction && fleet.Faction.GetOpinion(pilot.Faction) < 0.3f && pilot.CurrentUnit != null)
			{
				return Engine.GetBountyPilotLastKnownSectorPosition(pilot, out scene, out sectorPosition);
			}
			return false;
		}

		private bool DoesTargetPilotHaveBounty()
		{
			return targetPilot.GetTotalBounty() > 0;
		}

		protected override void resetTargetPosition()
		{
			if (targetPilot != null)
			{
				Sector scene = null;
				Vector3 sectorPosition = Vector3.zero;
				if (Engine.GetBountyPilotLastKnownSectorPosition(targetPilot, out scene, out sectorPosition))
				{
					fleet.SetTargetToSectorPosition(this, scene, sectorPosition);
				}
			}
			else if (fleet.HomeBaseUnit != null && fleet.HomeBaseUnit.IsValidAndNotDestroyed)
			{
				fleet.SetTargetToDock(this, fleet.HomeBaseUnit);
			}
		}

		protected override void tick(float elapsedTime)
		{
			if (targetPilot != null)
			{
				if (!IsTargetPilotValid(targetPilot) || !DoesTargetPilotHaveBounty())
				{
					if (targetPilot.Faction != null && fleet.Faction != null && fleet.Faction.IsHostileTo(targetPilot.Faction))
					{
						fleet.Faction.MakePeace(targetPilot.Faction);
					}
					TargetPilot = null;
				}
				else if (!lastKnownTargetPilotUnitUniqueId.HasValue && targetPilot.IsPilot)
				{
					lastKnownTargetPilotUnitUniqueId = targetPilot.CurrentUnit.UniqueId;
				}
			}
			else if (lastKnownTargetPilotUnitUniqueId.HasValue)
			{
				if (fleet.CurState == FleetState.CombatInterception && EngineASX.Instance.DestroyedUnitInfoController.TryGetByUnitId(lastKnownTargetPilotUnitUniqueId.Value, out var destroyedUnitInfo) && destroyedUnitInfo.AttackerFaction != null && destroyedUnitInfo.AttackerFaction != fleet.Faction && destroyedUnitInfo.Attacker != null && destroyedUnitInfo.Attacker.GetPilot() != null && fleet.Faction.FactionAI != null && fleet.Sector == destroyedUnitInfo.Sector && Vector3.Distance(fleet.SectorPosition, destroyedUnitInfo.SectorPosition) < 1500f)
				{
					EngineASX.Instance.DebugInfo.NumTimesBountyHunterPissedOffByStolenKill++;
					fleet.Faction.FactionAI.HandleBountyHunterTargetDestroyedByOtherFaction(fleet, destroyedUnitInfo.AttackerFaction);
				}
				lastKnownTargetPilotUnitUniqueId = null;
			}
			if (targetPilot != null)
			{
				SetHostileToTargetFaction();
			}
		}

		private void Update()
		{
			if (IsValid && searchOperation != null && targetPilot == null)
			{
				UpdateSearching(Time.deltaTime);
			}
		}

		private void UpdateSearching(float elapsedTime)
		{
			if (searchOperation != null)
			{
				if (!searchOperation.HasFinished)
				{
					searchOperation.Process(elapsedTime);
					return;
				}
				if (searchOperation.Result != null)
				{
					TargetPilot = searchOperation.Result.Person;
				}
				searchOperation = null;
			}
			else if (Time.time > lastStartSearchTime + 10f)
			{
				StartSearchForNewTarget();
			}
		}

		public void SearchForTargetImmediate()
		{
			StartSearchForNewTarget();
			searchOperation.ProcessUntilCompletion();
			if (searchOperation.Result != null)
			{
				TargetPilot = searchOperation.Result.Person;
			}
		}

		public void StartSearchForNewTarget()
		{
			lastStartSearchTime = Time.time;
			searchOperation = new BountyHunterSearchOperation();
			searchOperation.Initialise(this);
		}

		public override void AddSpecificTargets(List<AISpecificTarget> targets)
		{
			if (IsTargetPilotValid(targetPilot) && (targetPilot.IsRootUnitSameFaction || fleet.Faction.IsHostileTo(targetPilot.RootUnit.Faction)))
			{
				targets.Add(new AISpecificTarget
				{
					AdditionalPriority = 40f,
					Unit = targetPilot.CurrentUnit
				});
			}
		}

		private void SetHostileToTargetFaction()
		{
			if (!(targetPilot.Faction != null) || !targetPilot.Faction.IsValidInGame)
			{
				return;
			}
			if (!fleet.Faction.IsHostileTo(targetPilot.Faction))
			{
				fleet.Faction.SetAsHostileTo(targetPilot.Faction);
				return;
			}
			FactionAttitude attitude = fleet.Faction.GetAttitude(targetPilot.Faction);
			if (attitude != null)
			{
				fleet.Faction.UpdateHostilityCoolDownTime(attitude, attitude.Opinion);
			}
		}

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			if (targetPilot != null)
			{
				return "Hunting";
			}
			if (searchOperation != null)
			{
				return "Searching";
			}
			return base.GetStatusTextInternal(localFaction);
		}

		public override void OnFleetFailedToFindPathToTarget()
		{
			TargetPilot = null;
		}

		public override bool CanFleetAttack()
		{
			return true;
		}

		public override bool CanFleetIntercept()
		{
			return true;
		}
	}
}
