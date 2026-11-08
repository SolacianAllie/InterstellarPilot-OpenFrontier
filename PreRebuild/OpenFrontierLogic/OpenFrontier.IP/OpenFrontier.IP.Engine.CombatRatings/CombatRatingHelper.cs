using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.CombatRatings
{
	public static class CombatRatingHelper
	{
		public static float CalculateCustomVariantCombatRating(CustomUnitVariant customUnitVariant)
		{
			float num = 0f;
			ComponentBay[] componentsInChildren = customUnitVariant.UnitClass.UnitPrefab.GetComponentsInChildren<ComponentBay>();
			ShieldClass shieldClass = null;
			float maxSpeed = 0f;
			ComponentBay[] array = componentsInChildren;
			foreach (ComponentBay componentBay in array)
			{
				ComponentClass bayComponentClass = CustomUnitVariantHelper.GetBayComponentClass(customUnitVariant, componentBay);
				if (!(bayComponentClass != null))
				{
					continue;
				}
				if (!(bayComponentClass is TurretClass turretClass))
				{
					if (!(bayComponentClass is ShieldClass shieldClass2))
					{
						if (bayComponentClass is UnitEngineClass unitEngineClass)
						{
							maxSpeed = unitEngineClass.CalculateMaxSpeed(customUnitVariant.UnitClass.Drag, customUnitVariant.UnitClass.UnitPrefab.Mass);
						}
					}
					else
					{
						shieldClass = shieldClass2;
					}
					continue;
				}
				num += UnitComponentHolder.CalculateCombatRatingFromTurretClass(turretClass);
				if (componentBay.WeaponArc.y < 180f)
				{
					num *= 0.5f;
				}
				else if (componentBay.WeaponArc.y < 360f)
				{
					num *= 0.75f;
				}
			}
			return CalculateCombatRating(num, customUnitVariant.UnitClass.maxHealth, (shieldClass != null) ? shieldClass.Capacities[0] : 0f, maxSpeed);
		}

		public static float CalculateVanillaUnitClassCombatRating(UnitClass unitClass)
		{
			float num = 0f;
			ComponentBay[] componentsInChildren = unitClass.UnitPrefab.GetComponentsInChildren<ComponentBay>();
			ShieldClass shieldClass = null;
			float maxSpeed = 0f;
			ComponentBay[] array = componentsInChildren;
			foreach (ComponentBay componentBay in array)
			{
				if (!(componentBay.InitialComponentClass != null))
				{
					continue;
				}
				ComponentClass initialComponentClass = componentBay.InitialComponentClass;
				if (!(initialComponentClass is TurretClass turretClass))
				{
					if (!(initialComponentClass is ShieldClass shieldClass2))
					{
						if (initialComponentClass is UnitEngineClass unitEngineClass)
						{
							maxSpeed = unitEngineClass.CalculateMaxSpeed(unitClass.Drag, unitClass.UnitPrefab.Mass);
						}
					}
					else
					{
						shieldClass = shieldClass2;
					}
				}
				else
				{
					num += UnitComponentHolder.CalculateCombatRatingFromTurretClass(turretClass);
				}
			}
			return CalculateCombatRating(num, unitClass.maxHealth, (shieldClass != null) ? shieldClass.Capacities[0] : 0f, maxSpeed);
		}

		public static float CalculateCombatRating(float turretCombatRating, float maxHull, float maxShield, float maxSpeed)
		{
			float num = Mathf.Lerp(1f, 3f, Mathf.Clamp01((maxHull - 200f) / 3800f));
			float num2 = 1f;
			if (maxShield > 0f)
			{
				num2 = Mathf.Lerp(1f, 2f, Mathf.Clamp01((maxShield - 25f) / 1975f));
			}
			float num3 = 1f;
			if (maxSpeed > 0f)
			{
				num3 = Mathf.Lerp(1f, 1.5f, (maxSpeed - 40f) / 110f);
			}
			return turretCombatRating * num * num2 * num3 / 100f;
		}
	}
}
