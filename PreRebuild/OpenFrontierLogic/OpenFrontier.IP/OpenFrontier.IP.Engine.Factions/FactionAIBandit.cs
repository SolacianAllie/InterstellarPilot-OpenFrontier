using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions.Npc;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIBandit : FactionAIBase
	{
		public const int MinPatrolNodesInSector = 2;

		public const int MaxPatrolNodesInSector = 5;

		public override FactionAIType AIType => FactionAIType.Bandit;

		public override bool IsAlwaysAtWarWithFaction(Faction otherFaction)
		{
			if (base.IsAlwaysAtWarWithFaction(otherFaction))
			{
				return true;
			}
			switch (otherFaction.FactionType)
			{
			case FactionType.PassengerTransport:
			case FactionType.Explorer:
			case FactionType.Mercenary:
			case FactionType.Bar:
				return false;
			case FactionType.Bandit:
				if (!GameController.Instance.GameSettings.GameplaySettings.AllowBanditOnBanditAttack)
				{
					return false;
				}
				break;
			}
			if (otherFaction.Virtue - faction.GetOpinion(otherFaction) > faction.Virtue + 0.38f)
			{
				return true;
			}
			return false;
		}

		public override bool WillSellIntelTo(Faction otherFaction)
		{
			if (faction.GetOpinion(otherFaction) < 0.5f - faction.Cooperation * 0.2f)
			{
				return false;
			}
			return base.WillSellIntelTo(otherFaction);
		}

		protected override void AssignOrdersToWarFleet(Fleet fleet)
		{
			if (!(Random.value > AISettings.OffensiveStance) || !TryOrderFleetToDefend(fleet))
			{
				if (Random.value < GameController.Instance.GameSettings.GameplaySettings.ProbabilityOfBanditsRandomAttack && TryOrderFleetToAttack(fleet))
				{
					EngineASX.Instance.DebugInfo.NumTimesBanditsLaunchedAttacks++;
					return;
				}
				FactionAIPatrolSettings patrolSettingsOrDefault = fleet.Faction.FactionAI.GetPatrolSettingsOrDefault();
				PatrolOrder patrolOrder = PatrolRouteCreator.CreatePatrolObjective(fleet.Faction, fleet.Sector, patrolSettingsOrDefault.MinPatrolScenes, patrolSettingsOrDefault.MaxPatrolScenes, 2, 5, 3, considerStationsAsNodes: false);
				patrolOrder.Priority = 0.2f;
				AssignDefaultOrderMaxDuration(patrolOrder);
				fleet.EnqueueOrder(patrolOrder);
				fleet.Faction.FactionAI.LastOrderedPatrolTime = fleet.Engine.ScenarioElapsedTime;
				OnFleetOrdered(fleet);
			}
		}

		public override void SetFleetSettings(Fleet fleet)
		{
			base.SetFleetSettings(fleet);
			fleet.Settings.PreferCloak = true;
		}

		public override bool CanBuildStation(UnitClass unitClass)
		{
			if (!base.CanBuildStation(unitClass))
			{
				return false;
			}
			return true;
		}

		private static Unit GetSingleFactionValidNonDefensiveStationOrNull(Faction faction)
		{
			Unit unit = null;
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && item.UnitClass.StationPurpose != StationPurpose.Defence)
					{
						if (!(unit == null))
						{
							return null;
						}
						unit = item;
					}
				}
			}
			return unit;
		}

		public override float GetDamageMultiplierForIndirectFireFromNpcFaction(Faction sourceFaction)
		{
			return 1f;
		}

		protected override bool CanMergeFleetsWithDifferentCloakCapabilities()
		{
			return true;
		}
	}
}
