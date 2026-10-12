using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Factions.Npc;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.TraderHeatmap;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIOutlaw : FactionAIBase
	{
		private float nextLookForRaidTargetsTime;

		private OutlawTraderHeatmapSearch traderHeatmapSearch;

		private Dictionary<int, float> alreadyScannedUnits = new Dictionary<int, float>(100);

		private OutlawRaidTargetsSearch raidTargetsSearch;

		public const float RaidTargetsTimeInterval = 4f;

		public override FactionAIType AIType => FactionAIType.Outlaw;

		protected override void AssignOrdersToIdleFleet(Fleet fleet, Unit groupLeaderUnit)
		{
			if (fleet.HomeBaseUnit != null && fleet.HomeBaseUnit.UnitClass.StationPurpose == StationPurpose.Bar && Random.value < 0.1f)
			{
				OrderFleetReturnToBase(fleet);
			}
			else
			{
				base.AssignOrdersToIdleFleet(fleet, groupLeaderUnit);
			}
		}

		public override bool WillSellIntelTo(Faction otherFaction)
		{
			if (faction.GetOpinion(otherFaction) < 0.5f - faction.Cooperation * 0.2f)
			{
				return false;
			}
			return base.WillSellIntelTo(otherFaction);
		}

		public override float GetStationTypeBuildPriority(UnitClass unitClass)
		{
			if (unitClass.StationPurpose == StationPurpose.Factory && unitClass.UniqueID == GameController.Instance.UnitClasses.Lab.UniqueID)
			{
				return 1.5f;
			}
			return base.GetStationTypeBuildPriority(unitClass);
		}

		public override int GetPreferredCountOfUnitClass(UnitClass unitClass)
		{
			if (unitClass.StationPurpose == StationPurpose.Factory && unitClass.Legal)
			{
				return 0;
			}
			return base.GetPreferredCountOfUnitClass(unitClass);
		}

		protected override void onWorldInit()
		{
			base.onWorldInit();
			nextLookForRaidTargetsTime = Time.time + Random.value * 4f;
		}

		protected override void UpdateLight()
		{
			base.UpdateLight();
			if (traderHeatmapSearch != null)
			{
				UpdateTraderHeatmapSearch();
			}
			if (raidTargetsSearch != null)
			{
				UpdateRaidTargetsSearch();
			}
			else if (Time.time > nextLookForRaidTargetsTime)
			{
				TryStartRaidTargetsSearch();
				nextLookForRaidTargetsTime = Time.time + 4f;
			}
		}

		private void TryStartRaidTargetsSearch()
		{
			if (faction.Fleets.Count > 0)
			{
				Fleet random = faction.Fleets.GetRandom();
				if (random != null && random.FleetStrategy == FactionStrategy.War && FactionAIBase.CanOrderFleet(random) && CanOverrideFleetOrder(random, 0.5f) && !random.InCombat)
				{
					StartRaidTargetsSearch(random);
				}
			}
		}

		private void StartRaidTargetsSearch(Fleet randomFleet)
		{
			raidTargetsSearch = OutlawRaidTargetsSearch.Initialize(this, randomFleet);
		}

		private void UpdateRaidTargetsSearch()
		{
			if (raidTargetsSearch.IsSearching)
			{
				raidTargetsSearch.ProcessSearch();
				return;
			}
			if (raidTargetsSearch.BestTarget.HasValue && raidTargetsSearch.TotalSearchScore > GameController.Instance.GameSettings.PirateRaidSettings.MinScoreBeforeAttack)
			{
				Unit unit = raidTargetsSearch.BestTarget.Value.Unit;
				if (unit != null && unit.Faction != faction && faction.GetOpinion(unit.Faction) < 0.25f && FactionAIBase.CanOrderFleet(raidTargetsSearch.Fleet) && !raidTargetsSearch.Fleet.InCombat && CanOverrideFleetOrder(raidTargetsSearch.Fleet, 0.5f) && RequestPermissionToDeclareWarOn(unit.Faction, isBeingAttacked: false))
				{
					EngineASX.Instance.DebugInfo.NumTimesOutlawLaunchedRaid++;
					OnPickedRandomUnitToKill(unit, raidTargetsSearch.Fleet);
				}
			}
			nextLookForRaidTargetsTime = Time.time + 4f;
			raidTargetsSearch = null;
		}

		private void UpdateTraderHeatmapSearch()
		{
			if (traderHeatmapSearch.Fleet == null)
			{
				return;
			}
			traderHeatmapSearch.ProcessSearch();
			if (traderHeatmapSearch.IsSearching)
			{
				return;
			}
			if (FactionAIBase.CanOrderFleet(traderHeatmapSearch.Fleet) && CanOverrideFleetOrder(traderHeatmapSearch.Fleet, 0.55f))
			{
				if (traderHeatmapSearch.BestTarget.HasValue)
				{
					TraderHeatmapTarget value = traderHeatmapSearch.BestTarget.Value;
					MoveToOrder moveToOrder = UnityObjectHelper.NewGameObject<MoveToOrder>();
					moveToOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
					moveToOrder.TimeoutTime = 120f;
					moveToOrder.Priority = 0.55f;
					Vector3 sectorPosition = value.SectorPosition + Geometry.RandomXZUnitVector() * Random.Range(0f, 600f);
					moveToOrder.Target = SectorTarget.FromSectorPosition(value.Sector, sectorPosition);
					AssignDefaultOrderMaxDuration(moveToOrder);
					traderHeatmapSearch.Fleet.SetOrder(moveToOrder);
					OnFleetOrdered(traderHeatmapSearch.Fleet);
					EngineASX.Instance.DebugInfo.NumTimesOutlawOrderToMoveToTraderTarget++;
				}
				else
				{
					OrderPatrolInternal(traderHeatmapSearch.Fleet);
				}
			}
			traderHeatmapSearch = null;
		}

		protected override Vector3? GetBestHomeSectorPosition(Sector homeSector)
		{
			Vector3? bestHomeSectorPositionFromOwnedStations = GetBestHomeSectorPositionFromOwnedStations(homeSector);
			if (bestHomeSectorPositionFromOwnedStations.HasValue)
			{
				return bestHomeSectorPositionFromOwnedStations;
			}
			List<Unit> unitsByType = homeSector.GetUnitsByType(UnitType.GasCloud);
			if (unitsByType != null && unitsByType.Count > 0)
			{
				return unitsByType.GetRandom().GetRandomSectorPositionWithinRadius();
			}
			return homeSector.GetRandomFringeSectorPosition();
		}

		public bool TryConsiderUnitForAttack(Unit detector, Unit detected)
		{
			if (!alreadyScannedUnits.TryGetValue(detected.UniqueId, out var value) || Time.time > value)
			{
				if (detected.Components != null && detected.Faction != null && detected.Faction != faction && Random.value < GameController.Instance.GameSettings.PirateRaidSettings.ChanceOfConsideringRaid)
				{
					return true;
				}
				if (alreadyScannedUnits.Count > 50)
				{
					alreadyScannedUnits.Clear();
				}
				float num = Random.Range(GameController.Instance.GameSettings.PirateRaidSettings.MinTimeBeforeRescan, GameController.Instance.GameSettings.PirateRaidSettings.MaxTimeBeforeRescan);
				alreadyScannedUnits[detected.UniqueId] = Time.time + num;
			}
			return false;
		}

		private bool CanOrderOurFleetToAttack(Fleet ourFleet)
		{
			if (FactionAIBase.CanOrderFleet(ourFleet))
			{
				return CanOverrideFleetOrder(ourFleet, 0.55f);
			}
			return false;
		}

		private void OnPickedRandomUnitToKill(Unit target, Fleet detectorFleet)
		{
			if (detectorFleet.IsInActiveSector && detectorFleet.Leader != null)
			{
				detectorFleet.Leader.Person.RaiseDialogEventRandomly(EngineASX.Instance.DialogEvents.OutlawRandomAttack, 1f);
			}
			AttackTargetOrder attackTargetOrder = UnityObjectHelper.NewGameObject<AttackTargetOrder>();
			attackTargetOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			attackTargetOrder.TimeoutTime = 120f;
			attackTargetOrder.Priority = 0.6f;
			attackTargetOrder.CompleteWhenNotHostile = faction.IsHostileTo(target.Faction);
			AssignDefaultOrderMaxDuration(attackTargetOrder);
			detectorFleet.SetOrder(attackTargetOrder);
			OnFleetOrdered(detectorFleet);
			faction.SetAsHostileTo(target.Faction);
		}

		protected override void OrderPatrol(Fleet fleet)
		{
			if (!TryOrderFleetToTraderHeatmapTarget(fleet))
			{
				OrderPatrolInternal(fleet);
			}
		}

		private void OrderPatrolInternal(Fleet fleet)
		{
			FactionAIPatrolSettings patrolSettingsOrDefault = fleet.Faction.FactionAI.GetPatrolSettingsOrDefault();
			PatrolOrder patrolOrder = PatrolRouteCreator.CreatePatrolObjective(fleet.Faction, fleet.Sector, patrolSettingsOrDefault.MinPatrolScenes, patrolSettingsOrDefault.MaxPatrolScenes, 2, 4, AISettings.MaxJumpDistanceFromHomeSector, considerStationsAsNodes: false);
			AssignDefaultOrderMaxDuration(patrolOrder);
			patrolOrder.Priority = 0.2f;
			fleet.EnqueueOrder(patrolOrder);
			OnFleetOrdered(fleet);
		}

		private bool TryOrderFleetToTraderHeatmapTarget(Fleet fleet)
		{
			if (traderHeatmapSearch == null && Random.value < 0.12f)
			{
				return false;
			}
			if (traderHeatmapSearch == null || traderHeatmapSearch.Fleet == fleet)
			{
				if (traderHeatmapSearch == null)
				{
					traderHeatmapSearch = OutlawTraderHeatmapSearch.Init(fleet);
					EngineASX.Instance.DebugInfo.NumTimesOutlawStartedSearchForTrader++;
				}
				return true;
			}
			return false;
		}

		public override float GetDamageMultiplierForIndirectFireFromNpcFaction(Faction sourceFaction)
		{
			if (sourceFaction.FactionType == FactionType.Empire)
			{
				return 0.2f;
			}
			return 1f;
		}

		public override float GetStationScoreAsFleetHomeBase(Unit unit, Fleet fleet, float maxRandomness = 1f)
		{
			float num = base.GetStationScoreAsFleetHomeBase(unit, fleet, maxRandomness);
			if (unit.UnitClass.StationPurpose == StationPurpose.Bar)
			{
				num += 50f;
			}
			return num;
		}

		protected override bool CanMergeFleetsWithDifferentCloakCapabilities()
		{
			return true;
		}
	}
}
