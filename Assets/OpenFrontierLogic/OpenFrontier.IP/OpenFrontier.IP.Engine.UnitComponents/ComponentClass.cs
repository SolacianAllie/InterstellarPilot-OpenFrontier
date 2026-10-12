using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class ComponentClass : MonoBehaviour
	{
		[Tooltip("Order components recharge in when the unit's energy is short. Lower numbers recharge first.")]
		public int RechargePriority;

		[Tooltip("The letter or symbol shown in the turret grid for a part that has no icon of its own.")]
		public string ShortIconName;

		[Tooltip("Names a specific icon sprite instead of the one derived from the component type.")]
		public string IconSpriteNameOverride;

		public const int CostRounding = 25;

		[Tooltip("Whether this part can be bought at a station. Untick to make it something only the game can fit.")]
		public bool AllowBuy = true;

		[Tooltip("Base price, before any faction's buy and sell multipliers are applied to it.")]
		public int BaseCost = 1000;

		[Tooltip("The kind of slot this part fits. Matched by identity, not by category: it fits only a slot whose Bay Type is this exact asset.")]
		public ComponentBayType ComponentBayType;

		[Tooltip("Optional prefab used instead of building the component in code. Leave as None unless you have a reason.")]
		public ComponentBase ComponentPrefab;

		[Tooltip("What this part does - capacitor, engine, shield. Also decides how many of one kind a unit may carry.")]
		public ComponentType ComponentType;

		[Tooltip("Shown in the component info panel.")]
		public string Description = string.Empty;

		[Tooltip("Energy a turret draws per second while it is idle.")]
		public float EnergyChargeRate;

		[Tooltip("Lets this weapon fire in any direction, ignoring the arc its bay is set to.")]
		public bool IgnoreFiringArc;

		[Tooltip("Faction credited as the maker. Cosmetic.")]
		public Faction Manufacturer;

		[Tooltip("Hit points this part has before it is destroyed.")]
		public float MaxHealthPoints = 50f;

		[Tooltip("Largest ship class this part may be fitted to. The ship must be at or below this.")]
		public ShipHullType MaxHullType = ShipHullType.Station;

		[Tooltip("Smallest ship class this part may be fitted to. The ship must be at or above this.")]
		public ShipHullType MinHullType = ShipHullType.Fighter;

		[Tooltip("Display name. Leave empty to fall back to the asset's file name.")]
		public string Name = string.Empty;

		[Tooltip("Multiplier on the cost of repairing this part.")]
		public float RepairCostFactor = 1f;

		[Tooltip("Identifies this part class. Must be unique across the game - saves and the trade UI both look parts up by it.")]
		public int UniqueId;

		/// <summary>
		/// How capable this is, in eight steps. Described, not yet enforced.
		/// </summary>
		/// <remarks>
		/// The internal-module ladder, for capacitors, generators, shields and
		/// engines rather than weapons. Not gating anything yet, because which
		/// class a 250-unit shield belongs in is a balance decision and there are
		/// 141 of them to decide.
		/// </remarks>
		[Tooltip("This part's class. A slot takes a part of its own class or lower, unless Require Exact Fit is on and they must match.")]
		public ModuleClass Class = ModuleClass.Class1;

		/// <summary>
		/// How well made this is, E through A. Described, not yet enforced.
		/// </summary>
		/// <remarks>
		/// E by default, so an untouched asset claims nothing rather than
		/// coming out as A-rated.
		///
		/// Renamed from ModuleRating because the bay calls the same idea a
		/// quality, and having one field say ModuleRating and the other say
		/// nothing was going to read as two separate scales.
		/// </remarks>
		[Tooltip("How well made this is, E being the plainest through to A. Descriptive only - nothing refuses a part because of its quality yet.")]
		public ModuleRating Quality = ModuleRating.E;

		/// <summary>
		/// Which family this part belongs to. It fits only slots of the same family.
		/// </summary>
		/// <remarks>
		/// On the part as well as on the bay, because that is what makes the
		/// categories mean anything. ComponentBayType already stops a turret
		/// going into an engine mount, but it says what shape the slot is, not
		/// what the hardware is for - and it cannot express "a core internal
		/// takes core internals" across two different bay types that both
		/// happen to accept, say, shields.
		///
		/// The comparison is exact, and Legacy is a family like any other:
		/// legacy parts fit legacy slots and nothing else. That is deliberate
		/// rather than convenient. The old parts are being retired rather than
		/// migrated, so a slot given a real category takes only parts of that
		/// category and stays visibly empty until some are built for it. An
		/// exemption for Legacy would let old parts drift into new slots and
		/// quietly be the thing a new bay is still full of.
		///
		/// The consequence to know about: setting a bay's category before
		/// building parts for it makes that bay unfillable immediately, and
		/// InstallBayDefaultComponent installs its InitialComponentClass
		/// without checking compatibility at all, so a prefab left mismatched
		/// gets its old part installed anyway and says nothing about it.
		/// </remarks>
		[Tooltip("Which family this part belongs to. It fits only slots of the same category, so a LEGACY part fits LEGACY slots and nothing else.")]
		public ComponentCategory Category = ComponentCategory.Legacy;

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
