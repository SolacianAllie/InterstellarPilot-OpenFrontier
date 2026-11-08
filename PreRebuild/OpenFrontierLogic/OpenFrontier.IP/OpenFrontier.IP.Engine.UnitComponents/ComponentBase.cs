using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public abstract class ComponentBase : MonoBehaviour
	{
		private GameObject activeGameObject;

		public bool AutoChargeEnabled = true;

		private ComponentBay bay;

		[SerializeField]
		private float energySupply = 1f;

		private bool hasActiveUnit;

		private bool hasInit;

		[SerializeField]
		private float healthPoints = 1f;

		public int RechargePriority;

		private UnitComponentHolder unitComponents;

		private bool userPowered = true;

		public abstract ComponentClass ComponentClass { get; }

		public ComponentBay Bay
		{
			get
			{
				return bay;
			}
			set
			{
				if (bay != value)
				{
					ComponentBay componentBay = bay;
					bay = value;
					if (componentBay != null && componentBay.InstalledComponent == this)
					{
						componentBay.InstalledComponent = null;
					}
					if (bay != null)
					{
						bay.InstalledComponent = this;
					}
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"{this}: bay changed from {componentBay} to {bay}", this, 3);
					}
					if (bay != null)
					{
						UnitComponents = bay.UnitComponents;
					}
					else
					{
						UnitComponents = null;
					}
				}
			}
		}

		public Unit Unit
		{
			get
			{
				if (unitComponents != null)
				{
					return unitComponents.Unit;
				}
				return null;
			}
		}

		public UnitComponentHolder UnitComponents
		{
			get
			{
				return unitComponents;
			}
			private set
			{
				if (!(unitComponents != value))
				{
					return;
				}
				UnitComponentHolder unitComponentHolder = unitComponents;
				unitComponents = value;
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log(string.Format("{0}: Changing unit from {1} to {2}", this, (unitComponentHolder != null) ? unitComponentHolder.ToString() : "NULL", (unitComponents != null) ? unitComponents.ToString() : "NULL"), this, 3);
				}
				OnUnitInactive();
				if (unitComponentHolder != null)
				{
					unitComponentHolder.DeregisterComponent(this);
					onDetachedFromUnit(unitComponentHolder);
					if (unitComponentHolder.Unit.NpcPilot != null)
					{
						unitComponentHolder.Unit.NpcPilot.NotifyComponentRemoved(this);
					}
				}
				if (unitComponents != null)
				{
					unitComponents.RegisterComponent(this);
					onAttachedToUnit(unitComponents);
					if (RequiresRecharge())
					{
						unitComponents.anyComponentRequiresRecharge = true;
					}
					if (unitComponents.Unit.NpcPilot != null)
					{
						unitComponents.Unit.NpcPilot.NotifyComponentAdded(this);
					}
					if (unitComponents.Unit.IsActiveInEngine)
					{
						OnUnitActive();
					}
				}
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"{this}: unit changed from {unitComponentHolder} to {unitComponents}", this, 3);
				}
			}
		}

		public float EnergySupply
		{
			get
			{
				return energySupply;
			}
			set
			{
				energySupply = Mathf.Clamp(value, 0f, 3f);
			}
		}

		public float HealthPoints
		{
			get
			{
				return healthPoints;
			}
			set
			{
				healthPoints = Mathf.Clamp(value, 0f, ComponentClass.MaxHealthPoints);
			}
		}

		public GameObject ActiveGameObject => activeGameObject;

		public EngineASX Engine => unitComponents.Engine;

		public bool IsDamaged => healthPoints < ComponentClass.MaxHealthPoints;

		public float NormalizedDamage => 1f - HealthNormalized;

		public Faction Faction
		{
			get
			{
				Unit unit = Unit;
				if (unit != null)
				{
					return unit.Faction;
				}
				return null;
			}
		}

		public bool UserPowered
		{
			get
			{
				return userPowered;
			}
			set
			{
				if (userPowered != value)
				{
					userPowered = value;
					OnUserPoweredChanged();
				}
			}
		}

		public bool IsPoweredAndEnergySupplied
		{
			get
			{
				if (userPowered && unitComponents != null && !unitComponents.IsUnderConstructionOrDismantling)
				{
					return energySupply > 0f;
				}
				return false;
			}
		}

		public int RepairCost
		{
			get
			{
				int actualCost = ComponentClass.GetActualCost();
				return Mathf.RoundToInt(NormalizedDamage * (float)actualCost * Engine.GameSettings.ComponentRepairCostMultiplier * ComponentClass.RepairCostFactor);
			}
		}

		public float HealthNormalized
		{
			get
			{
				return healthPoints / ComponentClass.MaxHealthPoints;
			}
			set
			{
				HealthPoints = Mathf.Clamp01(value) * ComponentClass.MaxHealthPoints;
			}
		}

		public bool CanChangeUserPowered => ComponentClass.CanChangeUserPowered;

		public void RestoreHealth()
		{
			HealthNormalized = 1f;
		}

		public void Init(ComponentBay componentBay)
		{
			if (ComponentClass == null)
			{
				Debug.LogError($"{this}: Trying to init component but haven't set a ComponentClass", this);
				return;
			}
			if (!hasInit)
			{
				init();
			}
			Bay = componentBay;
		}

		public void OnUnitActive()
		{
			if (!hasActiveUnit)
			{
				onUnitActive();
				hasActiveUnit = true;
			}
		}

		public void OnUnitInactive()
		{
			if (hasActiveUnit)
			{
				onUnitInactive();
				hasActiveUnit = false;
			}
		}

		[ContextMenu("Recharge Full")]
		public void RechargeFull()
		{
			rechargeFull();
		}

		[ContextMenu("Remove Charge")]
		public void RemoveCharge()
		{
			removeCharge();
		}

		public void RechargeTick(float elapsedTime)
		{
			rechargeTick(elapsedTime);
		}

		protected virtual void tick(float elapsedTime)
		{
		}

		public int CalculateMoneyValue()
		{
			int rawActualCost = ComponentClass.GetRawActualCost();
			return ComponentTradeHelper.AdjustComponentPrice(Mathf.Max(0, rawActualCost - RepairCost));
		}

		public void ApplyDamage(float hullDamage)
		{
			float num = Mathf.Min(healthPoints, hullDamage);
			HealthPoints -= num;
			processDamage(num);
		}

		public virtual bool CanUseCargoClass(CargoClass c)
		{
			return false;
		}

		public virtual bool RequiresRecharge()
		{
			return false;
		}

		protected virtual void init()
		{
			HealthNormalized = 1f;
			AutoChargeEnabled = ComponentClass.ComponentType.InitialAutoChargeValue;
			hasInit = true;
		}

		protected virtual void onUnitActive()
		{
			if ((bool)activeGameObject)
			{
				Debug.LogError($"{this}: Unit is becoming active but there is already a game object");
				Object.Destroy(activeGameObject);
			}
			activeGameObject = CreateActiveGameObject();
		}

		protected virtual void onUnitInactive()
		{
			if ((bool)activeGameObject)
			{
				Object.Destroy(activeGameObject);
				activeGameObject = null;
			}
		}

		protected virtual GameObject CreateActiveGameObject()
		{
			return null;
		}

		protected virtual void onAttachedToUnit(UnitComponentHolder unit)
		{
		}

		protected virtual void onDetachedFromUnit(UnitComponentHolder unit)
		{
		}

		protected virtual void rechargeFull()
		{
		}

		protected virtual void removeCharge()
		{
		}

		protected virtual void rechargeTick(float elapsedTime)
		{
		}

		protected virtual void processDamage(float actualDamge)
		{
		}

		protected virtual void OnUserPoweredChanged()
		{
			if (unitComponents != null)
			{
				unitComponents.Unit.UpdateDetectionRadius();
			}
		}

		private void OnDestroy()
		{
			if (bay != null)
			{
				bay.InstalledComponent = null;
			}
		}

		public virtual bool ShouldShowFiringArcSprite()
		{
			return false;
		}

		public int GetRechargePriority()
		{
			return ComponentClass.ComponentType.RechargePriority * 100 + ComponentClass.RechargePriority;
		}
	}
}
