using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Engine.CargoLeakage
{
	public class CargoLeakageController : MonoBehaviour
	{
		private static List<CargoClass> cargoClassCache = new List<CargoClass>();

		public CargoLeakageSettings CargoLeakageSettings => EngineASX.Instance.GameSettings.CargoLeakageSettings;

		public List<CargoBayItem> LeakCargo(Unit damagedUnit, float damage)
		{
			List<CargoBayItem> cargoItemsToLeak = GetCargoItemsToLeak(damagedUnit, damage);
			if (cargoItemsToLeak != null && cargoItemsToLeak.Count > 0)
			{
				LeakCargoItems(damagedUnit, damage, cargoItemsToLeak);
			}
			return null;
		}

		private void LeakCargoItems(Unit damagedUnit, float damage, List<CargoBayItem> itemsToLeak)
		{
			foreach (CargoBayItem item in itemsToLeak)
			{
				damagedUnit.CargoBayComponent.EjectCargo(item.CargoClass, item.Quantity, playAudio: false);
			}
		}

		public List<CargoBayItem> GetCargoItemsToLeak(Unit damagedUnit, float damage)
		{
			if (damagedUnit.CargoBayComponent != null && damagedUnit.CargoBayComponent.Usage > 0f && CalculateCargoLeak(damagedUnit, damage))
			{
				int numCargoTypesToLeak = GetNumCargoTypesToLeak(damagedUnit);
				if (numCargoTypesToLeak > 0)
				{
					List<CargoBayItem> list = new List<CargoBayItem>();
					cargoClassCache.Clear();
					cargoClassCache.AddRange(from e in damagedUnit.CargoBayComponent.Cargos
						orderby ScoreProbabilityOfCargoLeak(e.Key, e.Value)
						select e.Key);
					for (int num = 0; num < numCargoTypesToLeak; num++)
					{
						CargoClass cargoClass = cargoClassCache[cargoClassCache.Count - 1];
						int countOf = damagedUnit.CargoBayComponent.GetCountOf(cargoClass);
						int num2 = Mathf.RoundToInt(Mathf.Lerp(1f, countOf, Mathf.Pow(Random.value, CargoLeakageSettings.CargoVolumePower) * CargoLeakageSettings.MaxPercentageLeaked));
						if (num2 > 0)
						{
							list.Add(new CargoBayItem
							{
								CargoClass = cargoClass,
								Quantity = num2
							});
						}
						cargoClassCache.RemoveAt(cargoClassCache.Count - 1);
					}
					return list;
				}
			}
			return null;
		}

		private static float ScoreProbabilityOfCargoLeak(CargoClass cargoClass, int quantity)
		{
			return cargoClass.Volume * (float)quantity + Random.value * 4f + (cargoClass.IsEquipment ? (-4f) : 0f);
		}

		public int GetNumCargoTypesToLeak(Unit damagedUnit)
		{
			int num = Mathf.Min(damagedUnit.CargoBayComponent.DistintCount, CargoLeakageSettings.MaxCargoTypesLeaked);
			if (num <= CargoLeakageSettings.MinCargoTypesLeaked)
			{
				return num;
			}
			return Mathf.RoundToInt(Mathf.Lerp(CargoLeakageSettings.MinCargoTypesLeaked, num, Mathf.Pow(Random.value, CargoLeakageSettings.NumCargoTypesLeakedPower)));
		}

		public bool CalculateCargoLeak(Unit damagedUnit, float damage)
		{
			float num = Mathf.Clamp01(damage / CargoLeakageSettings.DamageProbabilityReferenceValue) * CargoLeakageSettings.ProbabilityFromDamageFactor;
			float num2 = damagedUnit.CargoBayComponent.UsagePercent * CargoLeakageSettings.ProbabilityFromCargoBayUsageFactor;
			if (Random.value < num + num2)
			{
				return true;
			}
			return false;
		}
	}
}
