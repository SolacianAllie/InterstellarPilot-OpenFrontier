using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.billing;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitClass : MonoBehaviour
	{
		public float RelativeShipSaleCost;

		public float CaptureLikelihood = 0.05f;

		public List<ShipPurposeItem> ShipPurposes = new List<ShipPurposeItem>();

		public float AIBuildPreferenceMultiplierFudge = 1f;

		public float PassengerGroupCountMultiplier = 1f;

		public bool Legal = true;

		public int AICreditsReserve;

		public StationPurpose StationPurpose;

		public UnitClassDisplayData DisplayData;

		public bool CanTradeAtOwnStation;

		public bool PurposeWarship;

		public bool PurposeTrader;

		public bool PurposeMiner;

		public bool PurposeStealth;

		public bool PurposePassengerTransport;

		public bool PurposeCourier;

		public bool PurposeEquipmentDealer;

		public bool PurposeScout;

		public float CombatRating;

		public IPProduct RequiredProduct;

		public float AIMaxAttackDist = 400f;

		public int BaseCost = 10000;

		public bool CanFireAt = true;

		public string className;

		public string ShortClassName;

		public string Description;

		public string ShortDescription;

		public float Drag = 0.5f;

		public bool ExcludeFromDeliverShipMission;

		public bool GenerateMissions;

		public bool HasRepairFacilities;

		public float RepairFacilitiesCostMultiplier = 1f;

		public ShipHullType HullType = ShipHullType.Frigate;

		public ShipType ShipType;

		public bool IsArmed;

		public bool IsCivilian;

		public bool IsFreighter;

		public bool IsPilottable;

		public bool IsUsable = true;

		public bool ExcludeFromGodModeSpawn;

		public bool AllowBuildByPlayer = true;

		public bool SeedInSandbox = true;

		public Faction Manufacturer;

		public float maxHealth = 100f;

		public float RepairCostMultiplier = 0.5f;

		public int RpCost;

		public int SaleCost = 20000;

		public float TurnAcceleration = 90f;

		public float turnRate = 1f;

		public int UniqueID;

		public Unit UnitPrefab;

		public float ConstructionRateMultiplier = 1f;

		public UnitSeries UnitSeries;

		public UnitType UnitType = UnitType.Ship;

		public float DetectionRange = 1500f;

		public PlanetClass PlanetClass;

		public UnitPurpose Purposes
		{
			get
			{
				UnitPurpose unitPurpose = UnitPurpose.None;
				if (PurposeWarship)
				{
					unitPurpose |= UnitPurpose.Warship;
				}
				if (PurposeTrader)
				{
					unitPurpose |= UnitPurpose.Trader;
				}
				if (PurposeMiner)
				{
					unitPurpose |= UnitPurpose.Miner;
				}
				if (PurposeCourier)
				{
					unitPurpose |= UnitPurpose.Courier;
				}
				if (PurposePassengerTransport)
				{
					unitPurpose |= UnitPurpose.PassengerTransport;
				}
				if (PurposeStealth)
				{
					unitPurpose |= UnitPurpose.Stealth;
				}
				if (PurposeEquipmentDealer)
				{
					unitPurpose |= UnitPurpose.EquipmentDealer;
				}
				return unitPurpose;
			}
		}

		public bool IsTurret => StationPurpose == StationPurpose.Defence;

		public bool IsBarebonesShip
		{
			get
			{
				if (UnitType == UnitType.Ship)
				{
					return string.IsNullOrEmpty(UnitPrefab.ClassName);
				}
				return false;
			}
		}

		public float ShieldRingRadius => DisplayData.Radius;

		public bool HasComponentTrader => UnitPrefab.GetComponent<UnitComponentTrader>() != null;

		public bool ApplyThumbnailHullColor => DisplayData.ApplyThumbnailHullColor;

		public string ActiveUnitClassName
		{
			get
			{
				if (DisplayData != null)
				{
					return DisplayData.ActiveUnitClassName;
				}
				return null;
			}
		}

		public int XpValue => RpCost / 2;

		public bool ShowWithOwnershipColours
		{
			get
			{
				UnitType unitType = UnitType;
				if ((uint)(unitType - 1) <= 1u || unitType == UnitType.Cargo)
				{
					return true;
				}
				return false;
			}
		}

		public string GetSeriesName()
		{
			if (UnitSeries != null)
			{
				return UnitSeries.Name;
			}
			return UnitPrefab.ClassName;
		}

		[ContextMenu("Update Combat Rating")]
		public void UpdateCombatRating()
		{
		}

		public string GetClassAndSeriesName(bool shortName = false)
		{
			if (UnitPrefab != null)
			{
				return UnitPrefab.GetClassAndSeriesName(shortName);
			}
			return null;
		}

		public string GetDescription()
		{
			if (UnitSeries != null && !string.IsNullOrWhiteSpace(UnitSeries.Description))
			{
				return UnitSeries.Description;
			}
			return Description;
		}

		public string GetShortDescriptionElseLong()
		{
			if (UnitSeries != null)
			{
				if (!string.IsNullOrWhiteSpace(UnitSeries.ShortDescription))
				{
					return UnitSeries.ShortDescription;
				}
				if (!string.IsNullOrWhiteSpace(UnitSeries.Description))
				{
					return UnitSeries.Description;
				}
			}
			if (!string.IsNullOrWhiteSpace(ShortDescription))
			{
				return ShortDescription;
			}
			return Description;
		}

		public string GetClassName(bool shortName = false)
		{
			if (shortName && !string.IsNullOrEmpty(ShortClassName))
			{
				return ShortClassName;
			}
			return UnitPrefab.ClassName;
		}

		public UnitDebris CreateDebris(EngineASX engine, Sector sector, Vector3 sectorPosition)
		{
			GameSettings gameSettings = engine.GameSettings;
			for (int i = 0; i < gameSettings.LootableCargoClasses.Length; i++)
			{
				if (gameSettings.LootableCargoClasses[i] == engine.ScrapMetalCargoClass)
				{
					int num = Mathf.CeilToInt((float)SaleCost / gameSettings.LootableCargoShipValueDivisors[i]);
					if (num < gameSettings.LootableCargoMinValues[i])
					{
						num = gameSettings.LootableCargoMinValues[i];
					}
					int num2 = Mathf.CeilToInt((float)num * gameSettings.LootableCargoMaxMultiplier);
					if (num2 > gameSettings.LootableCargoMaxValues[i])
					{
						num2 = gameSettings.LootableCargoMaxValues[i];
					}
					int scrapQuantity = Random.Range(num, num2 + 1);
					UnitClass random = engine.DebrisUnitClasses.GetRandom();
					if (random != null)
					{
						Unit unit = Object.Instantiate(random.UnitPrefab);
						unit.transform.SetParent(sector.transform, worldPositionStays: true);
						unit.transform.localPosition = sectorPosition;
						unit.transform.rotation = Random.rotation;
						unit.Init(autoFindParents: false);
						UnitDebris component = unit.GetComponent<UnitDebris>();
						component.RelatedUnitClass = this;
						component.Unit.Sector = sector;
						component.ScrapQuantity = scrapQuantity;
						component.Expires = true;
						component.UpdateName();
						return component;
					}
				}
			}
			return null;
		}

		private static bool IsPowerOfTwo(ulong x)
		{
			return (x & (x - 1)) == 0;
		}

		public void Validate()
		{
			if (!IsUsable)
			{
				return;
			}
			if (UnitPrefab == null)
			{
				Debug.LogErrorFormat(this, "{0}: no UnitPrefab", this);
			}
			if (RequiredProduct != null)
			{
				Debug.LogError(name + ": Ships/stations should no longer have IP product assigned", this);
			}
			if (UnitType == UnitType.Ship || UnitType == UnitType.Station)
			{
				if (UnitPrefab.DamageCollider == null)
				{
					Debug.LogError($"Unit class {this} is missing a damage collider", this);
				}
				if (!UnitPrefab.DamageCollider.transform.IsChildOf(UnitPrefab.transform))
				{
					Debug.LogError($"Unit class {this} has a damage collider that isn't a child", this);
				}
				ValidateIncorrectAmmo();
			}
			foreach (ShipPurposeItem shipPurpose in ShipPurposes)
			{
				if (shipPurpose.Effectiveness < 0f)
				{
					Debug.LogError($"Unit class {this} has a negative ship effectiveness: {shipPurpose.Effectiveness}", this);
				}
				if (!IsPowerOfTwo((ulong)shipPurpose.FactionStrategy))
				{
					Debug.LogError($"Unit class {this} has an unknown ship strategy: {shipPurpose.FactionStrategy}", this);
				}
			}
			if (UnitPrefab != null)
			{
				UnitComponentHolder component = UnitPrefab.GetComponent<UnitComponentHolder>();
				if (component != null)
				{
					component.ValidateDuplicateComponentBays();
				}
			}
			ComponentBay[] componentsInChildren = UnitPrefab.transform.GetComponentsInChildren<ComponentBay>();
			ValidateComponentBays(componentsInChildren);
			if (UnitType == UnitType.Ship)
			{
				if (!string.IsNullOrEmpty(UnitPrefab.ClassName) && UnitPrefab.ClassName != "A" && string.IsNullOrEmpty(Description))
				{
					Debug.LogWarningFormat(this, "{0}: has className but no description", this);
				}
				if (UnitPrefab == null)
				{
					Debug.LogErrorFormat(this, "{0}: no UnitPrefab", this);
				}
				else
				{
					if (UnitPrefab.UnitClass != this)
					{
						Debug.LogErrorFormat(this, "{0}: UnitClass of prefab differs", this);
					}
					if (IsBarebonesShip)
					{
						ComponentBay[] array = componentsInChildren;
						foreach (ComponentBay componentBay in array)
						{
							if (componentBay.InitialComponentClass != null && !(componentBay.InitialComponentClass is PwrGeneratorClass) && !(componentBay.InitialComponentClass is UnitEngineClass) && !(componentBay.InitialComponentClass is CapacitorClass) && !(componentBay.InitialComponentClass is ShieldClass))
							{
								Debug.LogErrorFormat(this, "{0}: Barebones ships should only have PowerGenerator / Capacitor / Engine / Shield. Found a {1}", this, componentBay.InitialComponentClass);
							}
						}
					}
				}
			}
			if (UnitType == UnitType.Station)
			{
				ComponentBay[] array = UnitPrefab.transform.GetComponentsInChildren<ComponentBay>();
				foreach (ComponentBay componentBay2 in array)
				{
					if (componentBay2.InitialComponentClass == null && componentBay2.BayType != null && componentBay2.BayType.BayType == BayType.PowerGenerator)
					{
						Debug.LogError($"Station unit class {this} should have a PowerGenerator");
					}
				}
			}
			if (string.IsNullOrEmpty(ActiveUnitClassName))
			{
				return;
			}
			ActiveUnit activeUnit = EngineASX.LoadActiveUnit(ActiveUnitClassName);
			if (activeUnit != null)
			{
				ActiveUnit component2 = activeUnit.GetComponent<ActiveUnit>();
				if (component2 != null)
				{
					if (component2.ActiveUnitClass == null)
					{
						Debug.LogError(string.Format("Unit validation error: Class {0} has an ActiveUnit with a null ActiveUnitClass", name, ActiveUnitClassName), this);
					}
				}
				else
				{
					Debug.LogError($"Unit validation error: Class {name} has an unrecognised ActiveUnit name \"{ActiveUnitClassName}\"", this);
				}
			}
			else
			{
				Debug.LogError($"Unit validation error: Class {name} has an unrecognised ActiveUnit name \"{ActiveUnitClassName}\"", this);
			}
		}

		private void ValidateComponentBays(ComponentBay[] componentBays)
		{
			foreach (ComponentBay componentBay in componentBays)
			{
				if (componentBay.BayType == null)
				{
					Debug.LogError($"Unit class {this} - component bay \"{componentBay}\" does not have a bay type");
				}
			}
		}

		private void ValidateIncorrectAmmo()
		{
			UnitCargoLoadoutProfile component = GetComponent<UnitCargoLoadoutProfile>();
			if (!(component != null))
			{
				return;
			}
			foreach (UnitCargoLoadoutItem item in component.CargoLoadout.Items)
			{
				if (item.CargoClass != null && item.CargoClass.IsEquipment && !IsCargoClassCompatible(item.CargoClass))
				{
					Debug.LogWarning($"UnitClass {this} has cargo class {item.CargoClass.ClassName} which isn't compatible with any component", this);
				}
			}
		}

		public bool IsCargoClassCompatible(CargoClass cargoClass)
		{
			if (UnitPrefab != null)
			{
				UnitComponentHolder component = UnitPrefab.GetComponent<UnitComponentHolder>();
				if (component != null)
				{
					ComponentBay[] componentsInChildren = component.GetComponentsInChildren<ComponentBay>();
					foreach (ComponentBay componentBay in componentsInChildren)
					{
						if (!(componentBay.InitialComponentClass != null) || !(componentBay.InitialComponentClass is ProjectileTurretClass projectileTurretClass))
						{
							continue;
						}
						foreach (ProjectileClass compatibleProjectile in projectileTurretClass.CompatibleProjectiles)
						{
							if (compatibleProjectile.AmmoClass == cargoClass)
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		public int CalculateDefaultHullValue(EngineEconomySettings economySettings)
		{
			return CalculateHullValue(maxHealth, economySettings);
		}

		public int CalculateHullValue(float healthPoints, EngineEconomySettings economySettings)
		{
			return Mathf.RoundToInt(healthPoints * economySettings.HullValuePerHullPoint);
		}

		public bool StationPurposeRequiresDefence()
		{
			if (StationPurpose != StationPurpose.Satellite)
			{
				return StationPurpose != StationPurpose.Defence;
			}
			return false;
		}

		public bool IsMinorStation()
		{
			if (UnitType == UnitType.Station)
			{
				if (StationPurpose != StationPurpose.Satellite)
				{
					return StationPurpose == StationPurpose.Defence;
				}
				return true;
			}
			return false;
		}

		public bool HasStrategy(FactionStrategy strategy)
		{
			return (EngineASX.Instance.GetUnitClassShipStrategyFlags(this) & strategy) != 0;
		}

		public float GetEffectivenessAtStrategy(FactionStrategy strategy)
		{
			return EngineASX.Instance.GetUnitClassEffectivenessAtStrategy(this, strategy);
		}

		public Sprite GetThumbnailIconSprite()
		{
			return EngineASX.Instance.EngineResources.GetUnitClassThumbnailIconSpriteOrDefault(this);
		}

		public Sprite GetRenderSprite()
		{
			return EngineASX.Instance.EngineResources.GetUnitClassRenderSpriteOrDefault(this);
		}

		public Sprite GetIconSprite()
		{
			return EngineASX.Instance.EngineResources.GetUnitClassIconSpriteOrDefault(this);
		}

		public UnitCargoLoadout GetDefaultCargoLoadout()
		{
			if (UnitPrefab == null)
			{
				return null;
			}
			UnitCargoLoadout component = UnitPrefab.GetComponent<UnitCargoLoadout>();
			if (component == null)
			{
				UnitCargoLoadoutProfile component2 = GetComponent<UnitCargoLoadoutProfile>();
				if (component2 != null)
				{
					return component2.CargoLoadout;
				}
			}
			return component;
		}
	}
}
