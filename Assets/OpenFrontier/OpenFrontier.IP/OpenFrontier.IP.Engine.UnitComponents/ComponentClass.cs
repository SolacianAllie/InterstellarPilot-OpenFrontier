using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class ComponentClass : MonoBehaviour
	{
		public int RechargePriority;

		public string ShortIconName;

		public string IconSpriteNameOverride;

		public const int CostRounding = 25;

		public bool AllowBuy = true;

		public int BaseCost = 1000;

		public ComponentBayType ComponentBayType;

		public ComponentBase ComponentPrefab;

		public ComponentType ComponentType;

		public string Description = string.Empty;

		public float EnergyChargeRate;

		public bool IgnoreFiringArc;

		public Faction Manufacturer;

		public float MaxHealthPoints = 50f;

		public ShipHullType MaxHullType = ShipHullType.Station;

		public ShipHullType MinHullType = ShipHullType.Fighter;

		public string Name = string.Empty;

		public float RepairCostFactor = 1f;

		public int UniqueId;

		/// <summary>
		/// How big this component is. Will not fit a smaller slot.
		/// </summary>
		/// <remarks>
		/// Small by default, which makes every one of the 141 existing
		/// components small with nothing to migrate: a field added to a
		/// MonoBehaviour reads as zero from every prefab that predates it, and
		/// Small is zero.
		/// </remarks>
		public HardpointSize Size = HardpointSize.Small;

		/// <summary>
		/// How capable this is, in eight steps. Described, not yet enforced.
		/// </summary>
		/// <remarks>
		/// The internal-module ladder, for capacitors, generators, shields and
		/// engines rather than weapons. Not gating anything yet, because which
		/// class a 250-unit shield belongs in is a balance decision and there are
		/// 141 of them to decide.
		/// </remarks>
		public ModuleClass ModuleClass = ModuleClass.Class1;

		/// <summary>
		/// How well made this is, E through A. Described, not yet enforced.
		/// </summary>
		/// <remarks>
		/// E by default, so an untouched asset claims nothing rather than
		/// coming out as A-rated.
		/// </remarks>
		public ModuleRating ModuleRating = ModuleRating.E;

		public bool CanChangeUserPowered => ComponentType.CanSetUserPower;

		public virtual bool IsWeapon => false;

		public bool IsPointDefenceTurret
		{
			get
			{
				if (this is TurretClass turretClass)
				{
					return turretClass.IsPointDefence;
				}
				return false;
			}
		}

		public ComponentBase CreateComponent(GameObject target)
		{
			return createComponent(target);
		}

		public int GetActualCost()
		{
			if (BaseCost > -1)
			{
				return ComponentTradeHelper.AdjustComponentPrice(BaseCost, EngineASX.Instance.EconomySettings);
			}
			return ComponentTradeHelper.AdjustComponentPrice(CalculateCost(), EngineASX.Instance.EconomySettings);
		}

		public int GetActualCost(EngineEconomySettings engineEconomySettings)
		{
			if (BaseCost > -1)
			{
				return ComponentTradeHelper.AdjustComponentPrice(BaseCost, engineEconomySettings);
			}
			return ComponentTradeHelper.AdjustComponentPrice(CalculateCost(), engineEconomySettings);
		}

		public int GetRawActualCost()
		{
			if (BaseCost > -1)
			{
				return BaseCost;
			}
			return CalculateCost();
		}

		public virtual string GetFriendlyName()
		{
			return Name;
		}

		protected virtual ComponentBase createComponent(GameObject target)
		{
			return null;
		}

		protected virtual int CalculateCost()
		{
			return 1000;
		}

		public virtual string GetIconSpriteName()
		{
			if (!string.IsNullOrEmpty(IconSpriteNameOverride))
			{
				return IconSpriteNameOverride;
			}
			return gameObject.name;
		}
	}
}
