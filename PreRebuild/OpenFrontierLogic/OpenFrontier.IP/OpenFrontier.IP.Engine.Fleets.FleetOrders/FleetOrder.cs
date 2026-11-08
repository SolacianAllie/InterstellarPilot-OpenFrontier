using System;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class FleetOrder : MonoBehaviour
	{
		public bool Notifications = true;

		private int credits;

		public bool HasUnlimitedSpend = true;

		public float Priority = 0.5f;

		public bool AllowTimeout = true;

		public float TimeoutTime = 180f;

		public float MaxDuration;

		public bool AllowCombatInterception = true;

		[SerializeField]
		private FleetOrderCompletionMode completionMode = FleetOrderCompletionMode.Destroy;

		private EngineASX engine;

		public int MaxJumpDistance = -1;

		public FleetOrderCloakPreference PreferCloak;

		public int UniqueId = -1;

		public virtual float AIDefaultMaxDuration => 1800f;

		public virtual FleetOrderType OrderType => FleetOrderType.None;

		public FleetOrderCompletionMode CompletionMode
		{
			get
			{
				return completionMode;
			}
			set
			{
				completionMode = value;
			}
		}

		public EngineASX Engine
		{
			get
			{
				return engine;
			}
			private set
			{
				if (!(engine != value))
				{
					return;
				}
				EngineASX engineASX = engine;
				engine = value;
				if (engineASX != null)
				{
					engineASX.DeregisterFleetOrder(this);
				}
				if (engine != null)
				{
					if (UniqueId < 0)
					{
						UniqueId = engine.GetUniqueFleetOrderId();
					}
					engine.RegisterFleetOrder(this);
				}
			}
		}

		public virtual bool IsOffensive => false;

		public bool IsValid => engine != null;

		public virtual bool CanSpendCredits => false;

		public int Credits
		{
			get
			{
				return credits;
			}
			set
			{
				credits = value;
				if (credits < 0)
				{
					credits = 0;
				}
			}
		}

		public bool CanBeStackedOn()
		{
			return CanBeStackedOnInternal();
		}

		protected virtual bool CanBeStackedOnInternal()
		{
			return true;
		}

		public void SafeDestroy(Fleet fleet)
		{
			if (!HasUnlimitedSpend && credits > 0 && fleet != null && fleet.Faction != null)
			{
				fleet.Faction.ApplyTransaction(credits, FactionTransactionType.FleetTransfer, null, fleet.LeaderUnit);
				credits = 0;
			}
			if (engine != null)
			{
				Engine = null;
				UnityEngine.Object.Destroy(gameObject);
			}
		}

		public void Init()
		{
			Engine = EngineASX.Instance;
		}

		public ActiveFleetOrder CreateActiveFleetOrder()
		{
			ActiveFleetOrder activeFleetOrder = createActiveFleetOrder();
			if (activeFleetOrder != null)
			{
				activeFleetOrder.FleetOrder = this;
			}
			else
			{
				Debug.LogError($"Objective {this} did not return an active objective", this);
			}
			return activeFleetOrder;
		}

		public void AutoNameGameObject()
		{
			name = GetGameObjectAutoName();
		}

		public virtual string GetDescription()
		{
			return null;
		}

		public virtual string GetDescriptionForFaction(Faction faction, Sector currentSector)
		{
			return GetDescription();
		}

		protected virtual ActiveFleetOrder createActiveFleetOrder()
		{
			return null;
		}

		protected virtual string GetGameObjectAutoName()
		{
			return Enum.GetName(typeof(FleetOrderType), OrderType);
		}

		public virtual bool IsRepeatable()
		{
			return false;
		}

		public void SetAsRestrictedCreditsSpend(Fleet fleet)
		{
			HasUnlimitedSpend = false;
			if (fleet.Faction != null)
			{
				fleet.Faction.CouldHaveFleetsWithRestrictedSpend = true;
			}
		}

		public void SetCreditsAndSetAsRestrictedSpend(Fleet fleet, int credits)
		{
			Credits = credits;
			HasUnlimitedSpend = false;
			if (fleet.Faction != null)
			{
				fleet.Faction.CouldHaveFleetsWithRestrictedSpend = true;
			}
		}

		public virtual string Validate(Fleet fleet)
		{
			return null;
		}
	}
}
