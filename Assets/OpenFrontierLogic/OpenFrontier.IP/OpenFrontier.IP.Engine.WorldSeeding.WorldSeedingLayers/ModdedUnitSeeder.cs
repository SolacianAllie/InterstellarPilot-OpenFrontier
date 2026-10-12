using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.CompatibleComponents;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class ModdedUnitSeeder : MonoBehaviour
	{
		private const float MaxEquipmentcargoCapacityMultiplier = 0.5f;

		private static List<ComponentClass> componentClassCache = new List<ComponentClass>();

		public ModdedUnitSeederSettings ModdedUnitSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding modded units...", this, 1);
			}
			ModdedUnitSettings = world.Seeder.Settings.ModdedUnitSettings;
			if (EngineASX.Instance.CompatibleComponentCacher != null)
			{
				foreach (Sector sector in EngineASX.Instance.Sectors)
				{
					if (ModdedUnitSettings.ModShips)
					{
						List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
						if (unitsByType != null)
						{
							foreach (Unit item in unitsByType)
							{
								if (ShouldModUnitRandomly(item, ModdedUnitSettings))
								{
									ModUnitAndChangeEquipment(ModdedUnitSettings, item);
								}
							}
						}
					}
					if (ModdedUnitSettings.ModStations)
					{
						List<Unit> unitsByType2 = sector.GetUnitsByType(UnitType.Station);
						if (unitsByType2 != null)
						{
							foreach (Unit item2 in unitsByType2)
							{
								if (ShouldModUnitRandomly(item2, ModdedUnitSettings))
								{
									ModUnitAndChangeEquipment(ModdedUnitSettings, item2);
								}
							}
						}
					}
				}
				return;
			}
			Debug.LogError("Cannot mod units. Need Engine CompatibleComponentCacher");
		}

		public static void ModUnitAndChangeEquipment(ModdedUnitSeederSettings settings, Unit unit, float maxEquipmentCargoUsage = 0.5f)
		{
			unit.Components.CargoBayComponent.RemoveAllEquipment();
			ModUnit(unit, settings);
			AddUnitEquipment(unit, settings, maxEquipmentCargoUsage);
		}

		public static void AddUnitEquipment(Unit unit, ModdedUnitSeederSettings settings, float maxEquipmentUsage = 0.5f)
		{
			foreach (ComponentBay bay in unit.Components.Bays)
			{
				ProjectileTurretComponent projectileTurretComponent = bay.InstalledComponent as ProjectileTurretComponent;
				if (!(projectileTurretComponent != null))
				{
					continue;
				}
				CargoBayItems component = projectileTurretComponent.ProjectileTurretClass.GetComponent<CargoBayItems>();
				if (!(component != null))
				{
					continue;
				}
				foreach (CargoBayItem item in component.Items)
				{
					if (item != null)
					{
						int b = Mathf.RoundToInt((float)item.Quantity * Random.Range(settings.MinCargoLoadoutMultiplier, settings.MaxCargoLoadoutMultiplier));
						int num = Mathf.Min(unit.Components.CargoBayComponent.GetFreeSpaceFor(item.CargoClass, maxEquipmentUsage), b);
						if (num > 0)
						{
							unit.Components.CargoBayComponent.AddToCargoIfFits(item.CargoClass, num);
						}
					}
				}
			}
		}

		public static void ModUnit(Unit unit, ModdedUnitSeederSettings settings)
		{
			foreach (ComponentBay bay in unit.Components.Bays)
			{
				if (ShouldModComponentBayRandomly(bay, settings))
				{
					ModUnitBaySimple(unit, bay, settings.AllowDowngrade);
				}
			}
		}

		public static void ModUnitBaySimple(Unit unit, ComponentBay componentBay, bool allowDowngrade)
		{
			BayCompatibleComponents unitBayComponents = EngineASX.Instance.CompatibleComponentCacher.GetUnitBayComponents(unit.UnitClass, componentBay.Id);
			if (unitBayComponents != null)
			{
				PopulateComponentClassCache(unit, componentBay, unitBayComponents, allowDowngrade);
				if (componentClassCache.Count > 0)
				{
					ComponentClass random = componentClassCache.GetRandom();
					componentBay.InstallComponent(random).RechargeFull();
					unit.Components.IsModified = true;
				}
			}
		}

		public static ComponentBase ModUnitBayForFaction(Unit unit, ComponentBay componentBay, bool allowDowngrade, Faction faction, int availableCredits)
		{
			BayCompatibleComponents unitBayComponents = EngineASX.Instance.CompatibleComponentCacher.GetUnitBayComponents(unit.UnitClass, componentBay.Id);
			if (unitBayComponents != null)
			{
				PopulateComponentClassCache(unit, componentBay, unitBayComponents, allowDowngrade, availableCredits);
				if (componentClassCache.Count > 0)
				{
					ComponentClass random = componentClassCache.GetRandom();
					int upgradeCost = GetUpgradeCost(unit, componentBay, random);
					faction.ApplyTransaction(-upgradeCost, FactionTransactionType.EquipmentPurchase, null, unit);
					ComponentBase result = componentBay.InstallComponent(random);
					unit.Components.IsModified = true;
					return result;
				}
			}
			return null;
		}

		private static bool ShouldModComponentBayRandomly(ComponentBay bay, ModdedUnitSeederSettings settings)
		{
			if (settings.ModEquipmentBay || bay.BayType.BayType != BayType.Electronics)
			{
				float num = ((bay.BayType.BayType == BayType.Turret) ? settings.ProbabilityOfModdedTurret : settings.ProbabilityOfModdedMisc);
				return Random.value < num;
			}
			return false;
		}

		private static void PopulateComponentClassCache(Unit unit, ComponentBay componentBay, BayCompatibleComponents compatibleComponents, bool allowDowngrade, int? availableCredits = null)
		{
			componentClassCache.Clear();
			foreach (ComponentClass componentClass in compatibleComponents.ComponentClasses)
			{
				if (!(componentClass != componentBay.InstalledComponentClass) || (!allowDowngrade && IsDowngrade(componentBay.InstalledComponentClass, componentClass)) || IsUpgradeSingleMissileWeapon(componentBay, componentClass))
				{
					continue;
				}
				if (componentClass is TurretClass turretClass)
				{
					if (unit.UnitType == UnitType.Station && turretClass.UsesAmmo)
					{
						continue;
					}
					if (turretClass.IsPointDefence)
					{
						if (!CanAddPointDefenceTurret(unit, componentBay, turretClass))
						{
							continue;
						}
					}
					else if (MustAddPointDefenceTurret(unit, componentBay, turretClass))
					{
						continue;
					}
				}
				if (!availableCredits.HasValue || CanAffordUpgrade(unit, componentBay, componentClass, availableCredits.Value, out var _))
				{
					componentClassCache.Add(componentClass);
				}
			}
		}

		public static bool CanAffordUpgrade(Unit unit, ComponentBay componentBay, ComponentClass componentClass, int availableCredits, out int upgradeCost)
		{
			upgradeCost = GetUpgradeCost(unit, componentBay, componentClass);
			return availableCredits > upgradeCost;
		}

		public static int GetUpgradeCost(Unit unit, ComponentBay componentBay, ComponentClass componentClass)
		{
			int num = ComponentTradeHelper.GetComponentClassBuyCost(EngineASX.Instance, componentClass, null, null);
			if (componentBay.InstalledComponent != null)
			{
				num -= ComponentTradeHelper.GetComponentBuyCost(EngineASX.Instance, componentBay.InstalledComponent, null, null);
			}
			return num;
		}

		private static bool MustAddPointDefenceTurret(Unit unit, ComponentBay componentBay, TurretClass turretClass)
		{
			if (unit.UnitType != UnitType.Station)
			{
				return false;
			}
			if (componentBay.GetCompatibleHullType(unit.UnitClass.HullType) < ShipHullType.Frigate)
			{
				return false;
			}
			if (componentBay.WeaponArc.y >= 360f && componentBay.InstalledComponent == null && unit.Components.EstimatedPointDefenceEffectiveness <= 0f)
			{
				return true;
			}
			return false;
		}

		private static bool CanAddPointDefenceTurret(Unit unit, ComponentBay componentBay, TurretClass turretClass)
		{
			if (unit.IsStatic)
			{
				if (componentBay.WeaponArc.y < 360f)
				{
					return false;
				}
			}
			else if (componentBay.WeaponArc.y < 360f && !componentBay.GetIsRearFacingOnly())
			{
				return false;
			}
			if (unit.Components != null && !NewPointDefenceTurretSatisfiesCount(unit.Components, componentBay, turretClass))
			{
				return false;
			}
			return true;
		}

		private static bool NewPointDefenceTurretSatisfiesCount(UnitComponentHolder unitComponents, ComponentBay componentBay, TurretClass turretClass)
		{
			int num = 0;
			int num2 = 0;
			foreach (ComponentBay bay in unitComponents.Bays)
			{
				if (bay.BayType.BayType == BayType.Turret)
				{
					num++;
				}
				if (bay.InstalledComponent is TurretComponent turretComponent && turretComponent.TurretClass.IsPointDefence)
				{
					num2++;
				}
			}
			float num3 = (unitComponents.Unit.IsStation() ? 0.5f : 0.25f);
			if (num > 1)
			{
				return (float)num2 / (float)num < num3;
			}
			return false;
		}

		private static bool IsComponentClassMissileLauncher(ComponentClass componentClass)
		{
			return ((ProjectileTurretClass)componentClass).ProjectileTurretType == TurretType.Missiles;
		}

		private static bool IsUpgradeSingleMissileWeapon(ComponentBay componentBay, ComponentClass componentClass)
		{
			if (componentClass is ProjectileTurretClass && IsComponentClassMissileLauncher(componentClass) && !UnitHasNonMissileLaserOrProjectileWeapons(componentBay.Unit))
			{
				return true;
			}
			return false;
		}

		private static bool UnitHasNonMissileLaserOrProjectileWeapons(Unit unit)
		{
			foreach (TurretComponent turret in unit.Components.Turrets)
			{
				if ((turret.TurretClass is LaserTurretClass && !(turret.TurretClass is TractorTurretClass)) || (turret.TurretClass is ProjectileTurretClass && ((ProjectileTurretClass)turret.TurretClass).ProjectileTurretType == TurretType.Default))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsDowngrade(ComponentClass installedComponentClass, ComponentClass componentClass)
		{
			if (installedComponentClass == null)
			{
				return false;
			}
			if (componentClass == null)
			{
				return true;
			}
			switch (installedComponentClass.ComponentBayType.BayType)
			{
			case BayType.Engine:
			{
				UnitEngineClass unitEngineClass = (UnitEngineClass)installedComponentClass;
				return ((UnitEngineClass)componentClass).EnginePower < unitEngineClass.EnginePower;
			}
			case BayType.PowerGenerator:
			{
				PwrGeneratorClass pwrGeneratorClass = (PwrGeneratorClass)installedComponentClass;
				return ((PwrGeneratorClass)componentClass).PowerGenRate < pwrGeneratorClass.PowerGenRate;
			}
			case BayType.Shield:
			{
				ShieldClass shieldClass = (ShieldClass)installedComponentClass;
				return ((ShieldClass)componentClass).Capacities[0] < shieldClass.Capacities[0];
			}
			case BayType.Capacitor:
			{
				CapacitorClass capacitorClass = (CapacitorClass)installedComponentClass;
				return ((CapacitorClass)componentClass).Capacity < capacitorClass.Capacity;
			}
			case BayType.Turret:
				if (installedComponentClass.IsPointDefenceTurret && !componentClass.IsPointDefenceTurret)
				{
					return true;
				}
				if (!installedComponentClass.IsPointDefenceTurret && componentClass.IsPointDefenceTurret)
				{
					return false;
				}
				return componentClass.BaseCost < installedComponentClass.BaseCost;
			default:
				return componentClass.BaseCost < installedComponentClass.BaseCost;
			}
		}

		public static bool ShouldModUnitRandomly(Unit unit, ModdedUnitSeederSettings settings)
		{
			if (!ShouldModUnit(unit))
			{
				return false;
			}
			return Random.value < settings.ProbabilityOfModdedShip;
		}

		public static bool ShouldModUnit(Unit unit)
		{
			if (unit.IsUnderConstructionOrDismantling)
			{
				return false;
			}
			if (unit.UnitType == UnitType.Ship && unit.UnitClass.ShipType != ShipType.Normal)
			{
				return false;
			}
			return true;
		}
	}
}
