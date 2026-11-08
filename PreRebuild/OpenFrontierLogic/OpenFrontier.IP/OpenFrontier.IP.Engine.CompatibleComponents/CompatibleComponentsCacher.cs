using System.Collections.Generic;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.CompatibleComponents
{
	public class CompatibleComponentsCacher : MonoBehaviour
	{
		private Dictionary<int, UnitClassCompatibleComponents> unitClassCompatibleComponents = new Dictionary<int, UnitClassCompatibleComponents>(30);

		private static List<ComponentClass> componentClassCache = new List<ComponentClass>();

		public void Cache()
		{
			unitClassCompatibleComponents.Clear();
			UnitClass[] loadedUnitClasses = GameController.Instance.LoadedUnitClasses;
			foreach (UnitClass unitClass in loadedUnitClasses)
			{
				if (NeedToCacheFor(unitClass))
				{
					Cache(unitClass);
				}
			}
		}

		private void Cache(UnitClass unitClass)
		{
			UnitClassCompatibleComponents unitClassCompatibleComponents = CreateUnitMods(unitClass);
			if (unitClassCompatibleComponents != null)
			{
				this.unitClassCompatibleComponents[unitClass.UniqueID] = unitClassCompatibleComponents;
			}
		}

		public UnitClassCompatibleComponents GetUnitMods(UnitClass unitClass)
		{
			return unitClassCompatibleComponents.GetItemOrDefault(unitClass.UniqueID);
		}

		public BayCompatibleComponents GetUnitBayComponents(UnitClass unitClass, int bayId)
		{
			return GetUnitMods(unitClass)?.GetBayComponents(bayId);
		}

		private UnitClassCompatibleComponents CreateUnitMods(UnitClass unitClass)
		{
			UnitClassCompatibleComponents unitClassCompatibleComponents = new UnitClassCompatibleComponents();
			ComponentBay[] componentsInChildren = unitClass.UnitPrefab.GetComponentsInChildren<ComponentBay>();
			foreach (ComponentBay componentBay in componentsInChildren)
			{
				PopulateCompatibleComponentCache(unitClass, componentBay);
				if (componentClassCache.Count <= 0)
				{
					continue;
				}
				BayCompatibleComponents bayCompatibleComponents = new BayCompatibleComponents();
				foreach (ComponentClass item in componentClassCache)
				{
					bayCompatibleComponents.ComponentClasses.Add(item);
				}
				unitClassCompatibleComponents.SetBayComponents(componentBay.Id, bayCompatibleComponents);
			}
			return unitClassCompatibleComponents;
		}

		private static void PopulateCompatibleComponentCache(UnitClass unitClass, ComponentBay componentBay)
		{
			componentClassCache.Clear();
			foreach (ComponentClass componentClass in EngineASX.Instance.ComponentClasses)
			{
				if (componentClass.AllowBuy && componentBay.IsComponentClassCompatible(componentClass, unitClass.HullType))
				{
					componentClassCache.Add(componentClass);
				}
			}
		}

		private bool NeedToCacheFor(UnitClass unitClass)
		{
			if (!unitClass.IsUsable)
			{
				return false;
			}
			return unitClass.UnitType switch
			{
				UnitType.Ship => unitClass.ShipType == ShipType.Normal, 
				UnitType.Station => true, 
				_ => false, 
			};
		}
	}
}
