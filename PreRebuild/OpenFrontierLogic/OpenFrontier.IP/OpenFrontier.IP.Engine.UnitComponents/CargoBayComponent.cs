using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class CargoBayComponent : MonoBehaviour
	{
		public delegate void CargoAddRemoveHandler(CargoBayComponent cargoBay, CargoClass cargoClass, int quantityChange);

		private float lastChangedTime;

		private Dictionary<CargoClass, int> cargos = new Dictionary<CargoClass, int>(10);

		private UnitComponentHolder componentHolder;

		private float load;

		private float equpmentLoad;

		private float tradableCargoLoad;

		private float tradableCargoValue;

		public float LastChangedTime
		{
			get
			{
				return lastChangedTime;
			}
			set
			{
				lastChangedTime = value;
			}
		}

		public float TradableCargoValue => tradableCargoValue;

		public float TradableCargoLoad => tradableCargoLoad;

		public float EquipmentLoad => equpmentLoad;

		public float EquipmentLoad01 => equpmentLoad / Capacity;

		public float FreeSpace
		{
			get
			{
				float num = Capacity - load;
				if (num < 0f)
				{
					num = 0f;
				}
				return num;
			}
		}

		public float FreeSpacePercentage => FreeSpace / Capacity;

		public IEnumerable<KeyValuePair<CargoClass, int>> Cargos
		{
			get
			{
				foreach (KeyValuePair<CargoClass, int> cargo in cargos)
				{
					yield return cargo;
				}
			}
		}

		public IEnumerable<CargoClass> CargoClasses => cargos.Keys;

		public float UsagePercent => load / Capacity;

		public float Usage => load;

		public int DistintCount => cargos.Count;

		public bool IsEmpty => cargos.Count == 0;

		public float Capacity => UnitComponents.CargoCapacity;

		public UnitComponentHolder UnitComponents
		{
			get
			{
				return componentHolder;
			}
			set
			{
				componentHolder = value;
			}
		}

		public Unit Unit
		{
			get
			{
				UnitComponentHolder unitComponents = UnitComponents;
				if (unitComponents != null)
				{
					return unitComponents.Unit;
				}
				return null;
			}
		}

		public bool IsFull => FreeSpace == 0f;

		public bool IsOverloaded => load > Capacity;

		public float Load => load;

		public event CargoAddRemoveHandler CargoAddRemove;

		public bool CanChangeCargo(float additionalVolume)
		{
			float num = Usage + additionalVolume;
			if (num >= 0f)
			{
				if (!(num <= Capacity))
				{
					return additionalVolume < 0f;
				}
				return true;
			}
			return false;
		}

		public bool CanChangeCargo(CargoClass cargoType, int quantityChange)
		{
			if (quantityChange < 0)
			{
				return HasCargo(cargoType, Mathf.Abs(quantityChange));
			}
			return (int)(FreeSpace / cargoType.Volume) >= quantityChange;
		}

		public int GetFreeSpaceFor(CargoClass cargoClass)
		{
			if (cargoClass.Volume > 0f)
			{
				return Mathf.Max(0, (int)(FreeSpace / cargoClass.Volume));
			}
			return int.MaxValue;
		}

		public int GetFreeSpaceFor(CargoClass cargoClass, float cargoCapactyMultiplier)
		{
			if (cargoClass.Volume > 0f)
			{
				float num = Capacity * cargoCapactyMultiplier - load;
				return Mathf.Max(0, (int)(num / cargoClass.Volume));
			}
			return int.MaxValue;
		}

		public bool AddToCargoIfFits(CargoClass cargoType, int amount)
		{
			return AddToCargo(cargoType, amount, ignoreCapacity: false);
		}

		public bool AddToCargoIfFits(CargoBayItem cargoBayItem)
		{
			return AddToCargoIfFits(cargoBayItem.CargoClass, cargoBayItem.Quantity);
		}

		public bool AddToCargo(CargoClass cargoType, int delta, bool ignoreCapacity)
		{
			if (delta != 0)
			{
				int value = 0;
				cargos.TryGetValue(cargoType, out value);
				int num = value;
				if (delta < 0 && Mathf.Abs(delta) > num)
				{
					delta = -num;
				}
				float num2 = (float)delta * cargoType.Volume;
				if ((num2 <= 0f || num2 <= FreeSpace) | ignoreCapacity)
				{
					num += delta;
					load += num2;
					if (cargoType.IsEquipment)
					{
						equpmentLoad += num2;
					}
					if (cargoType.IsTraded)
					{
						tradableCargoLoad += num2;
						tradableCargoValue += num2 * (float)cargoType.BasePrice;
					}
					if (equpmentLoad < 0f)
					{
						equpmentLoad = 0f;
					}
					if (tradableCargoLoad < 0f)
					{
						tradableCargoLoad = 0f;
					}
					if (tradableCargoValue < 0f)
					{
						tradableCargoValue = 0f;
					}
					if (load < 0f)
					{
						load = 0f;
					}
					if (num <= 0)
					{
						num = 0;
						cargos.Remove(cargoType);
						if (cargos.Count == 0)
						{
							ClearCounts();
						}
					}
					else
					{
						cargos[cargoType] = num;
					}
					if (CargoAddRemove != null)
					{
						CargoAddRemove(this, cargoType, num - value);
					}
					LastChangedTime = Time.time;
					return true;
				}
				if (LogWrapper.LogMsgs)
				{
					Debug.LogWarningFormat(this, "Failed to cargo of unit {0}. Usage would exceed available space", Unit);
				}
				return false;
			}
			return true;
		}

		public bool HasCargo()
		{
			return cargos.Count > 0;
		}

		public bool HasCargo(CargoClass cargoType)
		{
			return GetCountOf(cargoType) > 0;
		}

		public bool HasCargo(CargoClass cargoType, int amount)
		{
			return GetCountOf(cargoType) >= amount;
		}

		public int GetCountOf(CargoClass cargoType)
		{
			int value = 0;
			if (cargos.TryGetValue(cargoType, out value))
			{
				return value;
			}
			return 0;
		}

		public float GetVolumeOf(CargoClass cargoType)
		{
			if (cargos.TryGetValue(cargoType, out var value))
			{
				return (float)value * cargoType.Volume;
			}
			return 0f;
		}

		public float GetNormalizedCapacity()
		{
			return load / Capacity;
		}

		public void RemoveCargoType(CargoClass cargoType)
		{
			SetCount(cargoType, 0);
		}

		public void SetCount(CargoClass cargoType, int count)
		{
			int value = 0;
			cargos.TryGetValue(cargoType, out value);
			AddToCargoIfFits(cargoType, count - value);
		}

		public CargoBayItem[] GetCargoItems()
		{
			CargoBayItem[] array = new CargoBayItem[cargos.Count];
			int num = 0;
			foreach (KeyValuePair<CargoClass, int> cargo in cargos)
			{
				array[num] = new CargoBayItem
				{
					CargoClass = cargo.Key,
					Quantity = cargo.Value
				};
				num++;
			}
			return array;
		}

		public IEnumerable<KeyValuePair<CargoClass, int>> GetCargos()
		{
			return cargos;
		}

		public int CalculateCargoValue()
		{
			int num = 0;
			foreach (KeyValuePair<CargoClass, int> cargo in cargos)
			{
				if (!cargo.Key.IsReserved)
				{
					num += cargo.Key.BasePrice * cargo.Value;
				}
			}
			return num;
		}

		public int CalculateCargoValueExcludingEquipment()
		{
			int num = 0;
			foreach (KeyValuePair<CargoClass, int> cargo in cargos)
			{
				if (!cargo.Key.IsReserved && !cargo.Key.IsEquipment)
				{
					num += cargo.Key.BasePrice * cargo.Value;
				}
			}
			return num;
		}

		public void RemoveAllCargo()
		{
			cargos.Clear();
			ClearCounts();
		}

		private void ClearCounts()
		{
			load = 0f;
			equpmentLoad = 0f;
			tradableCargoLoad = 0f;
			tradableCargoValue = 0f;
		}

		public void RemoveAllEquipment()
		{
			CargoClass[] array = cargos.Keys.Where((CargoClass e) => e.IsEquipment).ToArray();
			foreach (CargoClass cargoType in array)
			{
				SetCount(cargoType, 0);
			}
		}

		public void EjectCargo(CargoClass cargoClass, int quantity, bool playAudio = true)
		{
			quantity = Mathf.Min(GetCountOf(cargoClass), quantity);
			if (quantity > 0)
			{
				AddToCargoIfFits(cargoClass, -quantity);
				if (!Unit.IsDocked)
				{
					CreateEjectedCargoItem(cargoClass, quantity);
				}
			}
			if (playAudio && Unit.IsInActiveSector)
			{
				Unit.PlayEjectCargoAudioSource();
			}
		}

		public void GiveMinCargo(CargoClass cargoClass, int minAmount)
		{
			int num = minAmount - GetCountOf(cargoClass);
			if (num > 0)
			{
				AddToCargo(cargoClass, num, ignoreCapacity: true);
			}
		}

		private Cargo CreateEjectedCargoItem(CargoClass cargoClass, int quantity)
		{
			Vector3 vector = Unit.transform.TransformDirection(Vector3.back);
			float num = 2f;
			Vector3 sectorPosition = Unit.SectorPosition;
			int num2;
			if (Unit.IsActiveInEngine && Unit.ActiveUnit != null)
			{
				num2 = ((Unit.ActiveUnit.LastDistanceFromCamera < GameController.Instance.GameSettings.PerformanceSettings.EjectedCargoPhysicsMaxRangeFromCamera) ? 1 : 0);
				if (num2 != 0)
				{
					goto IL_0096;
				}
			}
			else
			{
				num2 = 0;
			}
			num += 10f;
			sectorPosition.y += Random.Range(-10f, 10f);
			goto IL_0096;
			IL_0096:
			sectorPosition += vector * (Unit.UnitClass.ShieldRingRadius * 1.2f) + Maths.RandomXZDirection() * Random.value * num;
			Cargo cargo = Unit.CreateCargoItem(cargoClass, quantity, sectorPosition);
			cargo.Unit.Faction = Unit.Faction;
			if (num2 != 0)
			{
				ApplyForcesIfActive(cargo, vector);
			}
			cargo.SetSpawnTime();
			cargo.SetHealthBasedOnVolume();
			return cargo;
		}

		private void ApplyForcesIfActive(Cargo cargo, Vector3 worldBack)
		{
			if (Unit.IsActiveInEngine && Unit.ActiveUnit != null)
			{
				Rigidbody component = cargo.gameObject.GetComponent<Rigidbody>();
				if (component != null)
				{
					component.AddForce(UnitComponents.Engine.GameSettings.EjectCargoForce * worldBack, ForceMode.Force);
				}
			}
		}

		public bool HasTradableCargos()
		{
			foreach (KeyValuePair<CargoClass, int> cargo in cargos)
			{
				if (cargo.Key.IsTraded && cargo.Value > 0)
				{
					return true;
				}
			}
			return false;
		}
	}
}
