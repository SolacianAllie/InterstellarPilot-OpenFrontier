using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.CustomUnitVariants
{
	public static class CustomUnitVariantHelper
	{
		public static int CalculateMoneyValue(CustomUnitVariant customUnitVariant)
		{
			if (customUnitVariant == null)
			{
				Debug.LogError("Expected: customUnitVariant");
				return 0;
			}
			if (customUnitVariant.UnitClass == null)
			{
				Debug.LogError("Expected: customUnitVariant.UnitClass");
				return 0;
			}
			EngineEconomySettings economySettings = EngineASX.Instance.EconomySettings;
			int num = customUnitVariant.UnitClass.SaleCost;
			ComponentBay[] componentsInChildren = customUnitVariant.UnitClass.UnitPrefab.GetComponentsInChildren<ComponentBay>();
			foreach (ComponentBay componentBay in componentsInChildren)
			{
				if (componentBay.InitialComponentClass != null)
				{
					num -= ComponentTradeHelper.AdjustComponentPrice(componentBay.InitialComponentClass.GetRawActualCost(), economySettings);
				}
			}
			UnitCargoLoadoutProfile component = customUnitVariant.UnitClass.GetComponent<UnitCargoLoadoutProfile>();
			if (component != null && component.CargoLoadout != null)
			{
				foreach (UnitCargoLoadoutItem item in component.CargoLoadout.Items)
				{
					num -= item.CargoClass.BasePrice * item.Quantity;
				}
			}
			num += GetDefaultComponentsMoneyValue(customUnitVariant, economySettings);
			foreach (CargoBayItem cargoItem in customUnitVariant.CargoItems)
			{
				num += cargoItem.CargoClass.BasePrice * cargoItem.Quantity;
			}
			return num;
		}

		public static int GetDefaultComponentsMoneyValue(CustomUnitVariant customUnitVariant, EngineEconomySettings economySettings)
		{
			int num = 0;
			ComponentBay[] componentsInChildren = customUnitVariant.UnitClass.UnitPrefab.GetComponentsInChildren<ComponentBay>();
			foreach (ComponentBay bay in componentsInChildren)
			{
				ComponentClass bayComponentClass = GetBayComponentClass(customUnitVariant, bay);
				if (bayComponentClass != null)
				{
					num += ComponentTradeHelper.AdjustComponentPrice(bayComponentClass.GetRawActualCost(), economySettings);
				}
			}
			return num;
		}

		public static ComponentClass GetBayComponentClass(CustomUnitVariant customUnitVariant, ComponentBay bay)
		{
			CustomUnitVariantBayMod customUnitVariantBayMod = customUnitVariant.BayMods.FirstOrDefault((CustomUnitVariantBayMod e) => e.BayId == bay.Id);
			if (customUnitVariantBayMod != null)
			{
				return customUnitVariantBayMod.ComponentClass;
			}
			return bay.InitialComponentClass;
		}

		public static IEnumerable<ComponentClass> GetAllComponentClasses(CustomUnitVariant customUnitVariant)
		{
			ComponentBay[] componentsInChildren = customUnitVariant.UnitClass.UnitPrefab.GetComponentsInChildren<ComponentBay>();
			foreach (ComponentBay bay in componentsInChildren)
			{
				ComponentClass bayComponentClass = GetBayComponentClass(customUnitVariant, bay);
				if (bayComponentClass != null)
				{
					yield return bayComponentClass;
				}
			}
		}

		public static void RemoveComponentClass(CustomUnitVariant unitVariant, ComponentBay bay)
		{
			SetComponentClass(unitVariant, bay, null);
		}

		public static void SetComponentClass(CustomUnitVariant unitVariant, ComponentBay bay, ComponentClass componentClass)
		{
			CustomUnitVariantBayMod customUnitVariantBayMod = unitVariant.BayMods.FirstOrDefault((CustomUnitVariantBayMod e) => e.BayId == bay.Id);
			if (customUnitVariantBayMod != null)
			{
				customUnitVariantBayMod.ComponentClass = componentClass;
				return;
			}
			unitVariant.BayMods.Add(new CustomUnitVariantBayMod
			{
				BayId = bay.Id,
				ComponentClass = componentClass
			});
		}

		public static bool IsComponentCompatible(CustomUnitVariant customUnitVariant, ComponentBay componentBay, ComponentClass componentClass, out string error)
		{
			error = null;
			if (!componentBay.IsComponentClassCompatible(componentClass, customUnitVariant.UnitClass.HullType))
			{
				error = "This component is not compatible with the current bay";
				return false;
			}
			if (GetBayComponentClass(customUnitVariant, componentBay) == null && !SupportsAdditionalComponentOfType(customUnitVariant, componentClass.ComponentType))
			{
				error = "An additional component of this type cannot be installed";
				return false;
			}
			return true;
		}

		public static bool SupportsAdditionalComponentOfType(CustomUnitVariant customUnitVariant, ComponentType componentType)
		{
			int num = CountComponentsOfType(customUnitVariant, componentType) + 1;
			if (num >= componentType.MinCount)
			{
				return num <= componentType.MaxCount;
			}
			return false;
		}

		public static int CountComponentsOfType(CustomUnitVariant customUnitVariant, ComponentType componentType)
		{
			int num = 0;
			foreach (ComponentClass allComponentClass in GetAllComponentClasses(customUnitVariant))
			{
				if (allComponentClass.ComponentType == componentType)
				{
					num++;
				}
			}
			return num;
		}

		public static List<CargoClass> GetCompatibleCargoClasses(CustomUnitVariant customUnitVariant)
		{
			List<CargoClass> list = new List<CargoClass>();
			foreach (ComponentClass allComponentClass in GetAllComponentClasses(customUnitVariant))
			{
				if (!(allComponentClass is ProjectileTurretClass projectileTurretClass) || projectileTurretClass.CompatibleProjectiles.Count <= 0)
				{
					continue;
				}
				foreach (ProjectileClass compatibleProjectile in projectileTurretClass.CompatibleProjectiles)
				{
					if (compatibleProjectile.AmmoClass != null && !list.Contains(compatibleProjectile.AmmoClass))
					{
						list.Add(compatibleProjectile.AmmoClass);
					}
				}
			}
			return list;
		}

		public static List<CargoBayItem> GetDefaultCompatibleCargoItems(CustomUnitVariant customUnitVariant)
		{
			List<CargoBayItem> list = new List<CargoBayItem>();
			foreach (ComponentClass allComponentClass in GetAllComponentClasses(customUnitVariant))
			{
				if (!(allComponentClass is ProjectileTurretClass projectileTurretClass) || projectileTurretClass.CompatibleProjectiles.Count <= 0)
				{
					continue;
				}
				foreach (ProjectileClass compatibleProjectile in projectileTurretClass.CompatibleProjectiles)
				{
					if (compatibleProjectile.AmmoClass != null && compatibleProjectile.DefaultAmmoComplement > 0)
					{
						CargoBayItem cargoBayItem = list.FirstOrDefault((CargoBayItem e) => e.CargoClass == compatibleProjectile.AmmoClass);
						if (cargoBayItem != null)
						{
							cargoBayItem.Quantity += compatibleProjectile.DefaultAmmoComplement;
							continue;
						}
						list.Add(new CargoBayItem
						{
							CargoClass = compatibleProjectile.AmmoClass,
							Quantity = compatibleProjectile.DefaultAmmoComplement
						});
					}
				}
			}
			return list;
		}

		public static void InstallComponentsAddCargoAndRename(CustomUnitVariant customUnitVariant, Unit newUnit)
		{
			newUnit.ClassName = customUnitVariant.Name;
			foreach (ComponentBay bay in newUnit.Components.Bays)
			{
				ComponentClass bayComponentClass = GetBayComponentClass(customUnitVariant, bay);
				if (bayComponentClass != null)
				{
					bay.InstallComponent(bayComponentClass).RechargeFull();
				}
			}
			if (customUnitVariant.CargoItems.Count > 0 && newUnit.Components.CargoBayComponent != null)
			{
				foreach (CargoBayItem cargoItem in customUnitVariant.CargoItems)
				{
					if (cargoItem.CargoClass != null && cargoItem.Quantity > 0)
					{
						newUnit.Components.CargoBayComponent.AddToCargo(cargoItem.CargoClass, cargoItem.Quantity, ignoreCapacity: true);
					}
				}
			}
			newUnit.Components.RefreshIsModded();
		}

		public static string GetNewUnitVariantName(UnitClass unitClass)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (!string.IsNullOrWhiteSpace(unitClass.UnitPrefab.ClassName))
			{
				stringBuilder.Append(unitClass.UnitPrefab.ClassName);
				stringBuilder.Append("-");
			}
			for (int i = 0; i < 3; i++)
			{
				stringBuilder.Append(UnitDesignationBuilder.RandomNumeral());
			}
			stringBuilder.Append(UnitDesignationBuilder.RandomChar());
			return stringBuilder.ToString();
		}

		public static CustomUnitVariant CreateFromUnitClass(UnitClass unitClass)
		{
			CustomUnitVariant customUnitVariant = new CustomUnitVariant
			{
				UnitClass = unitClass,
				Name = unitClass.className
			};
			UnitCargoLoadout defaultCargoLoadout = unitClass.GetDefaultCargoLoadout();
			if (defaultCargoLoadout != null)
			{
				foreach (UnitCargoLoadoutItem item in defaultCargoLoadout.Items)
				{
					if (item.CargoClass != null && item.Quantity > 0)
					{
						customUnitVariant.CargoItems.Add(new CargoBayItem
						{
							CargoClass = item.CargoClass,
							Quantity = item.Quantity
						});
					}
				}
			}
			return customUnitVariant;
		}
	}
}
