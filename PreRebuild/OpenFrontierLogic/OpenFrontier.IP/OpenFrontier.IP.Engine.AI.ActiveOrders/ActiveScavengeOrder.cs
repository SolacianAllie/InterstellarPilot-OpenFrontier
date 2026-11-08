using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.ActiveObjectives;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveScavengeOrder : ActiveFleetOrder
	{
		private const float minSearchInterval = 3f;

		private const float maxSearchInterval = 6f;

		private float nextSearchTime;

		private Vector3? roamSectorLocalPosition;

		public ScavengeOrder ScavengeObjective;

		private CollectCargoSearchOperation searchOperation;

		public bool IsSearching => searchOperation != null;

		private bool HasSearchFoundItem
		{
			get
			{
				if (searchOperation != null)
				{
					return searchOperation.BestCargoUnit != null;
				}
				return false;
			}
		}

		public Vector3? RoamSectorLocalPosition
		{
			get
			{
				return roamSectorLocalPosition;
			}
			set
			{
				roamSectorLocalPosition = value;
			}
		}

		protected override void tick(float elapsedTime)
		{
			base.tick(elapsedTime);
			if (!(base.fleet != null) || base.fleet.CurState == FleetState.CombatInterception)
			{
				return;
			}
			if (CollectCargoHelper.CheckFullCargoBay(base.fleet, ScavengeObjective.FullCargoThreshold))
			{
				CompleteDueToFullCargoBay();
			}
			else if (IsSearching)
			{
				if (!searchOperation.HasFinished)
				{
					return;
				}
				Unit bestCargoUnit = searchOperation.BestCargoUnit;
				searchOperation = null;
				if (bestCargoUnit != null && FactionAIBase.HasFleetOrderCooldownTimeElapsed(base.fleet))
				{
					CollectCargoOrder collectCargoOrder = UnityObjectHelper.NewGameObject<CollectCargoOrder>();
					collectCargoOrder.TargetUnit = bestCargoUnit;
					collectCargoOrder.Notifications = false;
					FactionAIBase factionAI = base.fleet.Faction.FactionAI;
					Fleet fleet = base.fleet;
					base.fleet.InsertOrderAndRequeueActive(collectCargoOrder);
					if (factionAI != null)
					{
						factionAI.OnFleetOrdered(fleet);
					}
				}
			}
			else
			{
				if (Time.time > nextSearchTime)
				{
					StartSearch();
					searchOperation.Process(elapsedTime);
					nextSearchTime = Time.time + Random.Range(3f, 6f);
				}
				if (!HasSearchFoundItem && !roamSectorLocalPosition.HasValue)
				{
					SetNewRoamPosition();
					ResetTargetPosition();
				}
			}
		}

		private void Update()
		{
			if (IsValid && searchOperation != null)
			{
				searchOperation.Process(Time.deltaTime);
			}
		}

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			if (IsSearching)
			{
				return "Searching";
			}
			return base.GetStatusTextInternal(localFaction);
		}

		private void StartSearch()
		{
			if (searchOperation == null)
			{
				searchOperation = new CollectCargoSearchOperation();
				searchOperation.MaxRange = float.MaxValue;
			}
			Sector actualTargetSector = GetActualTargetSector();
			Vector3 sectorTargetPosition = ((actualTargetSector == fleet.Sector) ? fleet.SectorPosition : Vector3.zero);
			searchOperation.InitialiseAndPopulateSearchItem(fleet, actualTargetSector, sectorTargetPosition);
		}

		public virtual Sector GetActualTargetSector()
		{
			if (ScavengeObjective.TargetSector != null)
			{
				return ScavengeObjective.TargetSector;
			}
			return fleet.Sector;
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			roamSectorLocalPosition = null;
		}

		protected override void resetTargetPosition()
		{
			if (roamSectorLocalPosition.HasValue)
			{
				fleet.SetTargetToSectorPosition(this, GetActualTargetSector(), roamSectorLocalPosition.Value);
				fleet.NavTarget.ArrivalThreshold = 300f;
			}
			else
			{
				base.resetTargetPosition();
			}
		}

		private void SetNewRoamPosition()
		{
			roamSectorLocalPosition = GetRoamLocalPosition();
		}

		private Vector3 GetRoamLocalPosition()
		{
			return Geometry.RandomXZUnitVector() * GetActualTargetSector().GetActualGateDistance() * 0.8f;
		}

		private void CompleteDueToFullCargoBay()
		{
			if (!fleet.InCombat)
			{
				fleet.Leader.Person.RaiseDialogEventRandomly(Engine.DialogEvents.CargoBayFull);
			}
			OnComplete();
		}

		public override float ScoreCargoToCollect(NpcPilot aIUnitController, Unit cargoUnit, CargoClass cargoClass, int availableQuantity, CargoOwnership cargoOwnership)
		{
			if (!CollectCargoHelper.IsCargoOwnershipCompatible(cargoOwnership, ScavengeObjective.CollectOwnerMode))
			{
				return float.MinValue;
			}
			if (!fleet.Faction.WillTradeCargoType(cargoClass))
			{
				return float.MinValue;
			}
			return ActiveFleetOrder.ScoreCargoToCollectBasedOnValue(cargoClass, availableQuantity);
		}

		public override FleetOrderCloakPreference GetCloakPreference()
		{
			return FleetOrderCloakPreference.None;
		}
	}
}
