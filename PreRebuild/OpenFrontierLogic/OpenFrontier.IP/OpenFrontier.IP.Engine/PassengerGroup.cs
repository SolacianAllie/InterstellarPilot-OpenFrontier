using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class PassengerGroup : IUnique
	{
		private int? cachedRevenue;

		private Unit currentUnit;

		private Unit destination;

		private EngineASX engine;

		public double ExpiryTime;

		private bool hasInit;

		private int passengerCount;

		private Unit source;

		private int uniqueId = -1;

		public int UniqueId
		{
			get
			{
				return uniqueId;
			}
			set
			{
				uniqueId = value;
			}
		}

		public bool IsValid
		{
			get
			{
				if (engine != null && currentUnit != null && currentUnit.IsValidAndNotDestroyed && destination != null)
				{
					return destination.IsValidAndNotDestroyed;
				}
				return false;
			}
		}

		public Unit CurrentUnit
		{
			get
			{
				return currentUnit;
			}
			set
			{
				SetCurrentUnit(value);
			}
		}

		public Unit Destination
		{
			get
			{
				return destination;
			}
			set
			{
				destination = value;
			}
		}

		public Unit Source
		{
			get
			{
				return source;
			}
			set
			{
				source = value;
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
					engineASX.DeregisterPassengerGroup(this);
				}
				if (engine != null)
				{
					if (UniqueId < 0)
					{
						UniqueId = engine.GetUniquePassengerGroupId();
					}
					engine.RegisterPassengerGroup(this);
				}
			}
		}

		public int PassengerCount
		{
			get
			{
				return passengerCount;
			}
			set
			{
				passengerCount = value;
			}
		}

		public bool IsWaitingForPickup
		{
			get
			{
				if (destination != null && source != null)
				{
					return source == currentUnit;
				}
				return false;
			}
		}

		public bool WaitTimeExpired => EngineASX.Instance.ScenarioElapsedTime > ExpiryTime;

		public int? CachedRevenue
		{
			get
			{
				return cachedRevenue;
			}
			set
			{
				cachedRevenue = value;
			}
		}

		private void SetCurrentUnit(Unit value)
		{
			if (!(currentUnit != value))
			{
				return;
			}
			if (value != null && value.Components == null)
			{
				Debug.LogError("Cannot place a pilot on a unit without UnitComponentHolder component");
				return;
			}
			Unit unit = currentUnit;
			currentUnit = value;
			if (unit != null)
			{
				unit.Components.RemovePassengerGroup(this);
			}
			if (currentUnit != null)
			{
				currentUnit.Components.AddPassengerGroup(this);
			}
		}

		public void Init()
		{
			if (!hasInit)
			{
				Engine = EngineASX.Instance;
			}
		}

		public void DeliverPassengers()
		{
			Destination = null;
			SafeDestroy();
		}

		public int? GetCachedRevenueOrCalculate()
		{
			if (cachedRevenue.HasValue)
			{
				return cachedRevenue;
			}
			return CalculateRevenue();
		}

		public int? CalculateRevenue()
		{
			if (LogWrapper.LogMsgs)
			{
				if (source == null)
				{
					Debug.LogError($"{this}: Cannot determine revenue. Source is null");
					return null;
				}
				if (source.Sector == null)
				{
					Debug.LogError($"{this}: Cannot determine revenue. Source scene is null");
					return null;
				}
				if (destination == null)
				{
					Debug.LogError($"{this}: Cannot determine revenue. Destination is null");
					return null;
				}
				if (destination.Sector == null)
				{
					Debug.LogError($"{this}: Cannot determine revenue. Destination scene is null");
					return null;
				}
			}
			return PassengerFareCalculator.CalculateFare(source, destination, passengerCount, GameController.Instance.GameSettings.PassengerFareSettings);
		}

		public void SafeDestroy()
		{
			SetCurrentUnit(null);
			Engine = null;
		}

		public void CacheRevenue()
		{
			cachedRevenue = CalculateRevenue();
		}

		public override string ToString()
		{
			return $"PassengerGroup({passengerCount})_{UniqueId}";
		}
	}
}
