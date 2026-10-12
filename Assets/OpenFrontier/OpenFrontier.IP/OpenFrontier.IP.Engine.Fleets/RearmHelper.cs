using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets
{
	public static class RearmHelper
	{
		private struct RearmComplement
		{
			public CargoClass CargoClass;

			public int RequiredQuantity;

			public RearmComplement(CargoClass cargoClass, int requiredQuantity)
			{
				this = default;
				CargoClass = cargoClass;
				RequiredQuantity = requiredQuantity;
			}
		}

		public static PriorityQueue<RearmItem, float> RearmItemsQueue = new PriorityQueue<RearmItem, float>(20);

		private static Dictionary<int, RearmComplement> ammoComplements = new Dictionary<int, RearmComplement>(20);

		public static void PopulateRearmItems(Unit unit, float requiredEquipmentVolume)
		{
			RearmItemsQueue.Clear();
			ammoComplements.Clear();
			if (requiredEquipmentVolume <= 0f)
			{
				return;
			}
			float num = 0f;
			foreach (TurretComponent turret in unit.Components.Turrets)
			{
				if (!turret.TurretClass.UsesAmmo || !(turret is ProjectileTurretComponent projectileTurretComponent))
				{
					continue;
				}
				foreach (ProjectileClass compatibleProjectile in projectileTurretComponent.ProjectileTurretClass.CompatibleProjectiles)
				{
					if (compatibleProjectile.AmmoClass != null && compatibleProjectile.DefaultAmmoComplement > 0)
					{
						if (ammoComplements.TryGetValue(compatibleProjectile.AmmoClass.UniqueId, out var value))
						{
							value.RequiredQuantity += compatibleProjectile.DefaultAmmoComplement;
							ammoComplements[compatibleProjectile.AmmoClass.UniqueId] = value;
						}
						else
						{
							ammoComplements[compatibleProjectile.AmmoClass.UniqueId] = new RearmComplement(compatibleProjectile.AmmoClass, compatibleProjectile.DefaultAmmoComplement);
						}
						num += compatibleProjectile.AmmoClass.Volume * (float)compatibleProjectile.DefaultAmmoComplement;
					}
				}
			}
			float num2 = requiredEquipmentVolume + unit.CargoBayComponent.EquipmentLoad;
			foreach (KeyValuePair<int, RearmComplement> ammoComplement in ammoComplements)
			{
				RearmComplement value2 = ammoComplement.Value;
				float num3 = (float)value2.RequiredQuantity * value2.CargoClass.Volume / num * num2;
				float volumeOf = unit.CargoBayComponent.GetVolumeOf(value2.CargoClass);
				if (num3 > volumeOf)
				{
					int num4 = Mathf.CeilToInt((num3 - volumeOf) / value2.CargoClass.Volume);
					if (num4 > 0)
					{
						RearmItemsQueue.Enqueue(new RearmItem(value2.CargoClass, num4), num3 - volumeOf);
					}
				}
			}
		}

		public static int? GetCostToRearmUnit(Unit unit, Unit currentRearmLocation, RearmItem item)
		{
			if (currentRearmLocation.Components.CargoTrader.GetSellPrice(item.CargoClass, unit.Faction, item.RequiredQuantity, out var pricePerUnit))
			{
				return pricePerUnit * item.RequiredQuantity;
			}
			Debug.LogError("Equipment dealer did not give a price for rearm. This is not expected", currentRearmLocation);
			return null;
		}

		public static void PerformRearmAndApplyTransaction(Unit unit, Unit currentRearmLocation, RearmItem rearmItem, int cost, ICreditsSource creditsSource)
		{
			_ = unit.CargoBayComponent.IsOverloaded;
			unit.CargoBayComponent.AddToCargo(rearmItem.CargoClass, rearmItem.RequiredQuantity, ignoreCapacity: true);
			unit.Faction.RegisterNormalTradeWithFaction(currentRearmLocation, currentRearmLocation.Faction, -cost, FactionTransactionType.Trade, rearmItem.CargoClass, null, rearmItem.RequiredQuantity, creditsSource);
		}
	}
}
