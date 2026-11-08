using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.CargoFactory
{
	[RequireComponent(typeof(UnitComponentHolder))]
	public class UnitCargoFactory : MonoBehaviour
	{
		private bool hasInit;

		private UnitComponentHolder unitComponents;

		public CargoFactoryProfile CargoFactoryProfile;

		private Dictionary<int, int> inputQuantityCache = new Dictionary<int, int>();

		private Dictionary<int, int> outputQuantityCache = new Dictionary<int, int>();

		private float totalInputVolume;

		private float totalOutputVolume;

		private List<CargoFactoryItem> items = new List<CargoFactoryItem>();

		public UnitComponentHolder UnitComponents => unitComponents;

		public Unit Unit => unitComponents.Unit;

		public float TotalOutputVolume => totalOutputVolume;

		public float TotalInputVolume => totalInputVolume;

		public bool IsConsumer
		{
			get
			{
				if (GetTotalInputVolume() > 0f)
				{
					foreach (CargoFactoryItem item in items)
					{
						if (item.HasOutputs())
						{
							return false;
						}
					}
					return true;
				}
				return false;
			}
		}

		public List<CargoFactoryItem> Items => items;

		public void Init()
		{
			if (!hasInit)
			{
				unitComponents = GetComponent<UnitComponentHolder>();
				unitComponents.FactoryComponent = this;
				CreateItems();
				totalInputVolume = GetTotalInputVolume();
				totalOutputVolume = GetTotalOutputVolume();
				hasInit = true;
			}
		}

		private void CreateItems()
		{
			for (int i = 0; i < CargoFactoryProfile.Items.Count; i++)
			{
				if (CargoFactoryProfile.Items[i] != null)
				{
					CargoFactoryItem cargoFactoryItem = new CargoFactoryItem
					{
						Profile = CargoFactoryProfile.Items[i]
					};
					items.Add(cargoFactoryItem);
					cargoFactoryItem.Init(this);
				}
			}
		}

		public void Tick(float elapsedTime)
		{
			if (unitComponents != null && unitComponents.CargoBayComponent != null)
			{
				for (int i = 0; i < items.Count; i++)
				{
					items[i]?.Update(elapsedTime);
				}
			}
		}

		private void TrimInvalidUnits()
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (items[i] == null || !items[i].HasItems())
				{
					items.RemoveAt(i);
					i--;
				}
			}
		}

		private float GetTotalInputVolume()
		{
			float num = 0f;
			foreach (CargoFactoryItem item in items)
			{
				foreach (CargoFactoryProfileItemInput input in item.Profile.Inputs)
				{
					num += (float)input.Quantity * input.CargoClass.Volume;
				}
			}
			return num;
		}

		private float GetTotalOutputVolume()
		{
			float num = 0f;
			foreach (CargoFactoryItem item in items)
			{
				foreach (CargoFactoryProfileItemOutput output in item.Profile.Outputs)
				{
					num += (float)output.Quantity * output.CargoClass.Volume;
				}
			}
			return num;
		}

		public float GetInputVolume(CargoClass cargoClass)
		{
			return (float)GetInputQuantity(cargoClass) * cargoClass.Volume;
		}

		public float GetOutputVolume(CargoClass cargoClass)
		{
			return (float)GetOutputQuantity(cargoClass) * cargoClass.Volume;
		}

		public int GetInputQuantity(CargoClass cargoClass)
		{
			int value = 0;
			if (!inputQuantityCache.TryGetValue(cargoClass.UniqueId, out value))
			{
				value = GetInputQuantityInternal(cargoClass);
				inputQuantityCache[cargoClass.UniqueId] = value;
			}
			return value;
		}

		public int GetOutputQuantity(CargoClass cargoClass)
		{
			int value = 0;
			if (!outputQuantityCache.TryGetValue(cargoClass.UniqueId, out value))
			{
				value = GetOutputQuantityInternal(cargoClass);
				outputQuantityCache[cargoClass.UniqueId] = value;
			}
			return value;
		}

		private int GetInputQuantityInternal(CargoClass cargoClass)
		{
			int num = 0;
			foreach (CargoFactoryItem item in items)
			{
				foreach (CargoFactoryProfileItemInput input in item.Profile.Inputs)
				{
					if (input.CargoClass == cargoClass)
					{
						num += input.Quantity;
					}
				}
			}
			return num;
		}

		private int GetOutputQuantityInternal(CargoClass cargoClass)
		{
			int num = 0;
			foreach (CargoFactoryItem item in items)
			{
				foreach (CargoFactoryProfileItemOutput output in item.Profile.Outputs)
				{
					if (output.CargoClass == cargoClass)
					{
						num += output.Quantity;
					}
				}
			}
			return num;
		}

		private void OnDestroy()
		{
			if (unitComponents != null && unitComponents.FactoryComponent == this)
			{
				unitComponents.FactoryComponent = null;
			}
		}
	}
}
