using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Intel;
using OpenFrontier.IP.Engine.Fleets.ActiveObjectives;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveMineOrder : ActiveFleetOrder
	{
		private MineSearchOperation searchOperation;

		public const float MaxDistanceFromMine = 100f;

		public const float MinDistanceFromMine = 70f;

		private const float mineTargetScoreDistanceScore = 7f;

		private const float mineTargetScoreGroupsScore = 1f;

		private const float mineTargetScoreRandomness = 1f;

		public MineOrder MineObjective;

		private Unit mineTarget;

		private float angleFromAsteroid;

		private float distanceFromAsteroid;

		private ActiveMineOrderState state;

		public ActiveMineOrderState State
		{
			get
			{
				return state;
			}
			internal set
			{
				state = value;
			}
		}

		public Unit MineTarget
		{
			get
			{
				return mineTarget;
			}
			internal set
			{
				mineTarget = value;
			}
		}

		public float AngleFromAsteroid
		{
			get
			{
				return angleFromAsteroid;
			}
			set
			{
				angleFromAsteroid = value;
			}
		}

		public float DistanceFromAsteroid
		{
			get
			{
				return distanceFromAsteroid;
			}
			set
			{
				distanceFromAsteroid = value;
			}
		}

		public bool IsMineTargetValid
		{
			get
			{
				if (mineTarget != null)
				{
					return mineTarget.IsValidAndNotDestroyed;
				}
				return false;
			}
		}

		public bool IsSearching => searchOperation != null;

		public void ChangeState(ActiveMineOrderState newState)
		{
			if (state == newState)
			{
				return;
			}
			state = newState;
			if (state != ActiveMineOrderState.None && mineTarget == null)
			{
				Debug.LogWarning("MineObjective: Cannot change state because no mine target set", fleet);
				state = ActiveMineOrderState.None;
				return;
			}
			switch (state)
			{
			case ActiveMineOrderState.None:
				ClearMineTarget();
				break;
			case ActiveMineOrderState.MoveToTarget:
				if (mineTarget != null && mineTarget.IsValidAndNotDestroyed)
				{
					SetTargetToMineTarget();
				}
				SetRandomPositionFromAsteroid();
				break;
			case ActiveMineOrderState.Mining:
				SetRandomPositionFromAsteroid();
				if (mineTarget != null && mineTarget.IsValidAndNotDestroyed)
				{
					SetTargetToMineTarget();
				}
				break;
			}
		}

		public void ClearMineTarget()
		{
			ChangeMineTarget(null);
		}

		public void ChangeMineTarget(Unit newMineTarget)
		{
			if (mineTarget != newMineTarget)
			{
				_ = mineTarget;
				mineTarget = newMineTarget;
				if (mineTarget == null)
				{
					ChangeState(ActiveMineOrderState.None);
				}
				else
				{
					ChangeState(ActiveMineOrderState.MoveToTarget);
				}
			}
		}

		public void SetRandomPositionFromAsteroid()
		{
			angleFromAsteroid = Random.value * 360f;
			distanceFromAsteroid = Random.Range(70f, 100f);
		}

		public override void OnReturningFromCombatState()
		{
			base.OnReturningFromCombatState();
			ChangeState(ActiveMineOrderState.MoveToTarget);
		}

		public virtual Sector GetActualTargetSector()
		{
			if (MineObjective.TargetSector != null)
			{
				return MineObjective.TargetSector;
			}
			return fleet.Sector;
		}

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			switch (state)
			{
			case ActiveMineOrderState.None:
				return "Searching...";
			case ActiveMineOrderState.MoveToTarget:
				if (mineTarget != null)
				{
					if (MineObjective.TargetSector == null)
					{
						return "Move to " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(localFaction, fleet.Sector, mineTarget);
					}
					return "Move to [" + mineTarget.GetFriendlyName() + "]";
				}
				break;
			case ActiveMineOrderState.Mining:
				if (mineTarget != null && mineTarget.IsValidAndNotDestroyed)
				{
					if (MineObjective.TargetSector == null)
					{
						return "Mining " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(localFaction, fleet.Sector, mineTarget);
					}
					return "Mining [" + mineTarget.GetFriendlyName() + "]";
				}
				break;
			}
			return base.GetStatusTextInternal(localFaction);
		}

		public void StartSearchForMineTarget()
		{
			searchOperation = new MineSearchOperation();
			if (MineObjective.TargetSector == null)
			{
				Sector startSector = fleet.Sector;
				Vector3 startPosition = fleet.SectorPosition;
				if (fleet.IsHomeBaseValid)
				{
					startSector = fleet.HomeSector;
					startPosition = fleet.HomeSectorPosition;
				}
				searchOperation.InitialiseAndStartSearch(startSector, startPosition, fleet.Faction, GetActualMaxJumpDist());
				return;
			}
			Vector3 startPosition2 = Vector3.zero;
			if (fleet.Sector == MineObjective.TargetSector)
			{
				startPosition2 = fleet.SectorPosition;
			}
			else
			{
				UniversePath universePath = fleet.Faction.Intel.GetUniversePath(fleet.Sector, MineObjective.TargetSector);
				if (universePath != null && universePath.Jumps > 0 && universePath.Nodes.Count > 0)
				{
					startPosition2 = universePath.Nodes[universePath.Nodes.Count - 1].Wormhole.GetTargetSectorPosition();
				}
			}
			searchOperation.InitialiseAndStartSearch(MineObjective.TargetSector, startPosition2, fleet.Faction, 0);
		}

		public float GetSearchPercentageComplete()
		{
			if (IsSearching)
			{
				return searchOperation.GetSearchPercentageComplete();
			}
			return 0f;
		}

		protected override void onInit()
		{
			base.onInit();
		}

		protected override void tick(float elapsedTime)
		{
			base.tick(elapsedTime);
			if (!(fleet != null))
			{
				return;
			}
			if (CollectCargoHelper.CheckFullCargoBay(fleet, MineObjective.FullCargoThreshold))
			{
				CompleteDueToFullCargoBay();
				return;
			}
			IsIdle = State == ActiveMineOrderState.None;
			switch (state)
			{
			case ActiveMineOrderState.MoveToTarget:
			case ActiveMineOrderState.Mining:
				if (!IsMineTargetValid)
				{
					ChangeState(ActiveMineOrderState.None);
				}
				break;
			case ActiveMineOrderState.None:
				LookForMineTarget();
				if (mineTarget != null)
				{
					ChangeState(ActiveMineOrderState.MoveToTarget);
				}
				break;
			}
		}

		private void Update()
		{
			if (IsValid && IsSearching && !searchOperation.HasFinished && fleet != null)
			{
				searchOperation.Process(Time.deltaTime);
			}
		}

		private void LookForMineTarget()
		{
			if (MineObjective.ManualMineTarget != null)
			{
				ChangeMineTarget(MineObjective.ManualMineTarget.Unit);
			}
			else if (!IsSearching)
			{
				StartSearchForMineTarget();
			}
			else if (searchOperation.HasFinished)
			{
				if (searchOperation.Result != null)
				{
					ChangeMineTarget(searchOperation.Result);
				}
				searchOperation = null;
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			if (state == ActiveMineOrderState.MoveToTarget)
			{
				ChangeState(ActiveMineOrderState.Mining);
			}
		}

		protected override void onFinalizeObjective()
		{
			base.onFinalizeObjective();
			ChangeState(ActiveMineOrderState.None);
			ClearMineTarget();
		}

		protected override void resetTargetPosition()
		{
			ActiveMineOrderState activeMineOrderState = state;
			if ((uint)(activeMineOrderState - 1) <= 1u)
			{
				if (mineTarget != null && mineTarget.IsValidAndNotDestroyed)
				{
					SetTargetToMineTarget();
				}
			}
			else
			{
				base.resetTargetPosition();
			}
		}

		private void SetTargetToMineTarget()
		{
			float num = distanceFromAsteroid + mineTarget.UnitClass.ShieldRingRadius;
			Vector3 sectorPosition = mineTarget.SectorPosition + Quaternion.Euler(0f, angleFromAsteroid, 0f) * Vector3.forward * num;
			fleet.SetTargetToSectorPosition(this, mineTarget.Sector, sectorPosition);
			fleet.NavTarget.Rotation = Quaternion.LookRotation(Vector3.Normalize(mineTarget.transform.position - fleet.transform.position), Vector3.up);
			fleet.NavTarget.ArrivalThreshold = 10f;
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
			if (cargoUnit.CargoComponent.CargoClass.IsOre)
			{
				return 100f;
			}
			return 0f;
		}

		public override FleetOrderCloakPreference GetCloakPreference()
		{
			if (state == ActiveMineOrderState.Mining)
			{
				return FleetOrderCloakPreference.None;
			}
			return base.GetCloakPreference();
		}

		public override float GetFormationScale()
		{
			if (state == ActiveMineOrderState.Mining)
			{
				return 0.25f;
			}
			return base.GetFormationScale();
		}
	}
}
