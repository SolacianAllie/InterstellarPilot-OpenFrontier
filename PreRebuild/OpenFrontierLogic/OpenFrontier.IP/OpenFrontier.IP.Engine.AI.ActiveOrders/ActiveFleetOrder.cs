using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveFleetOrder : MonoBehaviour, ICreditsSource
	{
		private double startTime;

		private double timeoutTime;

		private bool isIdle;

		public EngineASX Engine;

		protected Fleet fleet;

		protected FleetOrder fleetOrder;

		private float updateCooldownTime;

		public Fleet Fleet
		{
			get
			{
				return fleet;
			}
			set
			{
				if (this.fleet != value)
				{
					Fleet fleet = this.fleet;
					this.fleet = value;
					if (this.fleet != null && this.fleet.Engine != null)
					{
						Engine = this.fleet.Engine;
					}
					if (fleet != null && fleet.ActiveOrder == this)
					{
						fleet.ActiveOrder = null;
					}
				}
			}
		}

		public virtual bool IsValid => fleet != null;

		public FleetOrder FleetOrder
		{
			get
			{
				return fleetOrder;
			}
			set
			{
				if (fleetOrder != value)
				{
					fleetOrder = value;
				}
			}
		}

		public virtual float BaseTargetInterceptionScoreMultiplier => 0.25f;

		public double TimeoutTime
		{
			get
			{
				return timeoutTime;
			}
			set
			{
				timeoutTime = value;
			}
		}

		public bool IsIdle
		{
			get
			{
				return isIdle;
			}
			set
			{
				if (isIdle != value)
				{
					isIdle = value;
					if (isIdle)
					{
						ResetTimeoutTime();
					}
				}
			}
		}

		public double StartTime
		{
			get
			{
				return startTime;
			}
			set
			{
				startTime = value;
			}
		}

		public double TimeElapsed => Engine.ScenarioElapsedTime - startTime;

		public double TimeIdle
		{
			get
			{
				if (isIdle)
				{
					return Engine.ScenarioElapsedTime - (timeoutTime - (double)fleetOrder.TimeoutTime);
				}
				return 0.0;
			}
		}

		public virtual bool IsComplete => false;

		public int Credits
		{
			get
			{
				if (!fleetOrder.HasUnlimitedSpend)
				{
					return fleetOrder.Credits;
				}
				if (fleet.Faction != null)
				{
					return fleet.Faction.Credits;
				}
				return 0;
			}
			set
			{
				if (!fleetOrder.HasUnlimitedSpend)
				{
					fleetOrder.Credits = value;
				}
				else
				{
					fleet.Faction.Credits = value;
				}
			}
		}

		public ICreditsSource CreditsSource
		{
			get
			{
				if (!fleetOrder.HasUnlimitedSpend)
				{
					return this;
				}
				return fleet.Faction;
			}
		}

		public virtual float GetAdditionalCombatTargetPriority(Unit combatTarget)
		{
			return 0f;
		}

		public virtual void AddSpecificTargets(List<AISpecificTarget> targets)
		{
		}

		public virtual void OnReturningFromCombatState()
		{
			IsIdle = false;
		}

		public void OnGroupReachedTarget()
		{
			onFleetReachedTarget();
		}

		public virtual void OnFleetFailedToFindPathToTarget()
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Fleet {fleet}: Invalidating order {this} because there's no path to the target", fleet, 1);
			}
			OnInvalid("There is no path to the target");
		}

		public void OnInvalid(string message = null)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Objective is no longer valid - finalizing", fleet, 2);
			}
			fleet.OnInvalidFleetOrder(this, message);
			SafeDestroy();
		}

		public void Tick(float elapsedTime)
		{
			if (!(fleet != null) || !(fleet.Leader != null) || !(fleet.Faction != null) || !fleet.Faction.IsValidInGame || !(fleet.ActiveOrder == this) || Time.time < updateCooldownTime)
			{
				return;
			}
			if (IsComplete)
			{
				OnComplete();
			}
			else if (HasTimedOut())
			{
				OnTimeout();
			}
			else if (HasMaxDurationExpired())
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Fleet: {fleet}. Max duration has passed on {Enum.GetName(typeof(FleetOrderType), fleetOrder.OrderType)}({fleetOrder.GetDescriptionForFaction(fleet.Faction, null)})", this, 2);
				}
				fleet.OnFleetOrderMaxDurationReached(this);
				OnComplete();
			}
			else if (IsValid)
			{
				tick(elapsedTime);
			}
			else
			{
				OnInvalid();
			}
		}

		protected virtual void OnTimeout()
		{
			if (LogWrapper.LogMsgs)
			{
				Debug.LogWarning($"Fleet: {fleet}. Timing out trying on {fleetOrder.OrderType}({fleetOrder.GetDescriptionForFaction(fleet.Faction, fleet.Sector)})." + $" Credits: {Credits} Credits Reserve: {fleet.Faction.CreditsReserve}", this);
			}
			fleet.OnGroupObjectiveIimeout(this);
			if (fleet != null)
			{
				OnInvalid("Time out");
			}
		}

		private bool HasMaxDurationExpired()
		{
			if (fleetOrder.MaxDuration > 0f)
			{
				return TimeElapsed > (double)fleetOrder.MaxDuration;
			}
			return false;
		}

		public virtual float ScoreCargoToCollect(NpcPilot aIUnitController, Unit cargoUnit, CargoClass cargoClass, int availableQuantity, CargoOwnership cargoOwnership)
		{
			return 0f;
		}

		protected static float ScoreCargoToCollectBasedOnValue(CargoClass cargoClass, int availableQuantity)
		{
			return cargoClass.BasePrice * availableQuantity;
		}

		public void Init()
		{
			onInit();
			if (fleet != null)
			{
				ResetTargetPosition();
			}
			IsIdle = false;
			updateCooldownTime = Time.time + 1f;
		}

		public bool HasTimedOut()
		{
			if (isIdle && fleetOrder.AllowTimeout)
			{
				return Engine.ScenarioElapsedTime > timeoutTime;
			}
			return false;
		}

		public virtual bool RequestInterception(NpcPilot requestor, Unit target, float distance, float requestorsTargetScore, bool priorityTarget)
		{
			if (fleetOrder.AllowCombatInterception)
			{
				return fleet.RequestInterceptionInternal(requestor, target, distance, requestorsTargetScore, priorityTarget);
			}
			return false;
		}

		public void ResetTargetPosition()
		{
			fleet.ClearTarget();
			resetTargetPosition();
		}

		protected virtual string GetStatusTextInternal(Faction localFaction)
		{
			if (IsFleetMovingToNavTarget() && fleet.IsMovingSignificantly)
			{
				return "Moving";
			}
			return null;
		}

		protected bool IsFleetMovingToNavTarget()
		{
			if (fleet.NavTarget.IsActive)
			{
				return fleet.NavTarget.HasAttemptedToCalculatedPath;
			}
			return false;
		}

		public string GetStatusText(Faction localFaction)
		{
			try
			{
				string statusTextInternal = GetStatusTextInternal(localFaction);
				if (string.IsNullOrWhiteSpace(statusTextInternal))
				{
					return GetFleetStatusText();
				}
				string fleetStatusText = GetFleetStatusText();
				if (!string.IsNullOrWhiteSpace(fleetStatusText))
				{
					return statusTextInternal + " - " + fleetStatusText;
				}
				return statusTextInternal;
			}
			catch
			{
				return "Unknown";
			}
		}

		public string GetFleetStatusText()
		{
			return Fleet.GetFleetStatusText(fleet.CurState);
		}

		public int GetActualMaxJumpDist()
		{
			int a = 999;
			if (fleetOrder.MaxJumpDistance > -1)
			{
				a = Mathf.Min(a, fleetOrder.MaxJumpDistance);
			}
			return Mathf.Min(a, fleet.Settings.MaxJumpDistance);
		}

		public virtual void OnComplete(bool silent = false)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Objective has been completed - finalizing", fleet, 2);
			}
			if (fleet != null)
			{
				fleet.OnOrderCompleted(this, silent);
			}
			switch (fleetOrder.CompletionMode)
			{
			case FleetOrderCompletionMode.Repeat:
				fleet.InsertOrderInQueue(fleetOrder, 0);
				fleetOrder = null;
				break;
			case FleetOrderCompletionMode.Requeue:
				if (fleet.OrderQueue.Count > 0)
				{
					fleet.EnqueueOrder(fleetOrder);
					fleetOrder = null;
				}
				break;
			}
			SafeDestroy();
		}

		protected virtual void onFinalizeObjective()
		{
		}

		protected virtual void onFleetReachedTarget()
		{
		}

		protected virtual void tick(float elapsedTime)
		{
		}

		protected virtual void onInit()
		{
		}

		protected virtual void resetTargetPosition()
		{
		}

		public void SafeDestroy()
		{
			if (fleet != null)
			{
				if (fleet.NavTarget.IsActive && fleet.NavTarget.SourceOrder == this)
				{
					fleet.ClearTarget();
				}
				onFinalizeObjective();
				if (fleetOrder != null)
				{
					fleetOrder.SafeDestroy(fleet);
				}
				Fleet = null;
				UnityEngine.Object.Destroy(this);
			}
		}

		public void AutoNameGameObject()
		{
			name = GetGameObjectAutoName();
		}

		protected virtual string GetGameObjectAutoName()
		{
			return GetType().Name;
		}

		public bool IsJumpDistanceToSceneValid(Sector scene)
		{
			int jumpDistanceTo = fleet.GetHomeSectorOrCurrent().GetJumpDistanceTo(scene);
			int actualMaxJumpDist = GetActualMaxJumpDist();
			return jumpDistanceTo <= actualMaxJumpDist;
		}

		public bool IsJumpDistanceValid(int jumpDistance)
		{
			int actualMaxJumpDist = GetActualMaxJumpDist();
			return jumpDistance <= actualMaxJumpDist;
		}

		public void ResetTimeoutTime()
		{
			if (fleetOrder != null)
			{
				TimeoutTime = EngineASX.Instance.ScenarioElapsedTime + (double)fleetOrder.TimeoutTime;
			}
		}

		public void OnIdle()
		{
			if (!isIdle)
			{
				IsIdle = true;
			}
			else
			{
				ResetTimeoutTime();
			}
		}

		public virtual void OnNpcUnableToDockAtNavpoint(NpcPilot npc, Unit dock, UnableToDockReason unableToDockReason)
		{
			IsIdle = true;
		}

		public virtual void OnFleetEnteredWormhole(Wormhole wormholeBeingEntered)
		{
		}

		public virtual DockedPreference GetPreferToDockWhenIdle()
		{
			return DockedPreference.Dock;
		}

		public virtual void OnCargoCollectedByShip(Unit collectingUnit, Cargo cargo)
		{
		}

		public virtual FleetOrderCloakPreference GetCloakPreference()
		{
			return fleetOrder.PreferCloak;
		}

		public virtual float GetFormationScale()
		{
			return fleet.GetDefaultFormationScale();
		}

		public virtual bool RequestFleetRegroup()
		{
			return true;
		}

		public bool IsAffordableConsideringReserve(int cost)
		{
			if (fleetOrder.HasUnlimitedSpend)
			{
				if (fleet.Faction != null)
				{
					return fleet.Faction.IsAffordableConsideringReserve(cost);
				}
				return false;
			}
			return Credits >= cost;
		}

		public void InvalidateFromInsufficientCredits()
		{
			OnInvalid("Insufficient Credits");
		}

		public virtual bool RequestUndock(NpcPilot npcPilot)
		{
			return true;
		}

		public virtual bool CanFleetAttack()
		{
			return fleet.Settings.AllowAttack;
		}

		public virtual bool CanFleetIntercept()
		{
			return fleet.Settings.AllowCombatInterception;
		}
	}
}
