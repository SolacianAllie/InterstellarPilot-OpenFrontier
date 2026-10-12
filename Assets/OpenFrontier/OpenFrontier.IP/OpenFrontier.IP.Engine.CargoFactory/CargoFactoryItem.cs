using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.CargoFactory
{
	public class CargoFactoryItem
	{
		private CargoFactoryProfileItem profile;

		private float productionElapsed;

		private CargoFactoryItemState state;

		private UnitCargoFactory cargoFactory;

		private float cargoVolumeForInputs;

		private float cargoVolumeForOuputs;

		private float nextUpdateTime;

		private const float updateFrequency = 2f;

		public bool IsConverter => Profile.IsConverter;

		public bool IsConsumer => profile.IsConsumer;

		public CargoFactoryItemState State
		{
			get
			{
				return state;
			}
			set
			{
				if (state != value)
				{
					state = value;
				}
			}
		}

		public CargoBayComponent CargoBay => cargoFactory.UnitComponents.CargoBayComponent;

		public float ProductionTimeRemaining => Profile.ProductionTime - productionElapsed;

		public float ProductionElapsed
		{
			get
			{
				return productionElapsed;
			}
			set
			{
				productionElapsed = value;
			}
		}

		public EngineASX Engine => cargoFactory.Unit.Engine;

		public CargoFactoryProfileItem Profile
		{
			get
			{
				return profile;
			}
			set
			{
				profile = value;
			}
		}

		public override string ToString()
		{
			return $"[CargoFactoryItem Unit: {cargoFactory.Unit}]";
		}

		public void Init(UnitCargoFactory cargoFactory)
		{
			this.cargoFactory = cargoFactory;
			cargoVolumeForOuputs = GetCargoVolumeForOutputs();
			cargoVolumeForInputs = GetCargoVolumeForInputs();
		}

		public void Update(float elapsedTime)
		{
			if (!(Time.time > nextUpdateTime))
			{
				return;
			}
			switch (state)
			{
			case CargoFactoryItemState.Idle:
				UpdateIdle();
				break;
			case CargoFactoryItemState.Producing:
				productionElapsed += Mathf.Max(2f, elapsedTime);
				if (productionElapsed > profile.ProductionTime)
				{
					TryProduce();
				}
				break;
			case CargoFactoryItemState.ProducedWaitingForFreeSpace:
				TryProduce();
				break;
			}
			nextUpdateTime = Time.time + 2f;
		}

		public void ProcessInputs()
		{
			for (int i = 0; i < profile.Inputs.Count; i++)
			{
				CargoFactoryProfileItemInput cargoFactoryProfileItemInput = profile.Inputs[i];
				if (cargoFactoryProfileItemInput != null && cargoFactoryProfileItemInput.CargoClass != null && cargoFactoryProfileItemInput.Quantity > 0)
				{
					int num = Mathf.Min(cargoFactoryProfileItemInput.Quantity, CargoBay.GetCountOf(cargoFactoryProfileItemInput.CargoClass));
					if (CargoBay.HasCargo(cargoFactoryProfileItemInput.CargoClass, num))
					{
						Engine.OnCargoConsumed(cargoFactory.Unit.Faction, cargoFactoryProfileItemInput.CargoClass, num);
						CargoBay.AddToCargoIfFits(cargoFactoryProfileItemInput.CargoClass, -num);
					}
				}
			}
		}

		public bool HasEnoughInput()
		{
			for (int i = 0; i < profile.Inputs.Count; i++)
			{
				CargoFactoryProfileItemInput cargoFactoryProfileItemInput = profile.Inputs[i];
				if (cargoFactoryProfileItemInput != null && cargoFactoryProfileItemInput.CargoClass != null && !CargoBay.HasCargo(cargoFactoryProfileItemInput.CargoClass, cargoFactoryProfileItemInput.Quantity))
				{
					return false;
				}
			}
			return true;
		}

		public float GetCargoVolumeForInputs()
		{
			float num = 0f;
			for (int i = 0; i < profile.Inputs.Count; i++)
			{
				CargoFactoryProfileItemInput cargoFactoryProfileItemInput = profile.Inputs[i];
				if (cargoFactoryProfileItemInput != null && cargoFactoryProfileItemInput.CargoClass != null)
				{
					num += (float)cargoFactoryProfileItemInput.Quantity * cargoFactoryProfileItemInput.CargoClass.Volume;
				}
			}
			return num;
		}

		public float GetCargoVolumeForOutputs()
		{
			float num = 0f;
			for (int i = 0; i < profile.Outputs.Count; i++)
			{
				CargoFactoryProfileItemOutput cargoFactoryProfileItemOutput = profile.Outputs[i];
				if (cargoFactoryProfileItemOutput != null && cargoFactoryProfileItemOutput.CargoClass != null)
				{
					num += (float)cargoFactoryProfileItemOutput.Quantity * cargoFactoryProfileItemOutput.CargoClass.Volume;
				}
			}
			return num;
		}

		public bool EnoughSpaceForOutputs()
		{
			if (cargoVolumeForOuputs > 0f)
			{
				return CargoBay.FreeSpace >= cargoVolumeForOuputs;
			}
			return true;
		}

		public void Produce()
		{
			ProvideConsumerRevenue();
			ProduceOutputs();
		}

		private void ProduceOutputs()
		{
			foreach (CargoFactoryProfileItemOutput output in profile.Outputs)
			{
				if (output != null && output.CargoClass != null)
				{
					int quantity = output.Quantity;
					CargoBay.AddToCargo(output.CargoClass, quantity, ignoreCapacity: true);
				}
			}
		}

		private void ProvideConsumerRevenue()
		{
			if (!IsConsumer)
			{
				return;
			}
			Faction faction = cargoFactory.Unit.Faction;
			if (!(faction != null))
			{
				return;
			}
			foreach (CargoFactoryProfileItemInput input in profile.Inputs)
			{
				if (input != null && input.CargoClass != null)
				{
					int creditsValue = input.CalculateConsumerRevenue(Engine.EconomySettings, profile.RevenueMultiplier);
					faction.ApplyTransaction(creditsValue, FactionTransactionType.Trade, null, cargoFactory.Unit, input.CargoClass, null, FactionTransactionTaxType.None, input.Quantity);
				}
			}
		}

		public bool HasInputs()
		{
			foreach (CargoFactoryProfileItemInput input in profile.Inputs)
			{
				if (input != null && input.CargoClass != null && input.Quantity > 0)
				{
					return true;
				}
			}
			return false;
		}

		public bool HasItems()
		{
			if (!HasInputs())
			{
				return HasOutputs();
			}
			return true;
		}

		public bool HasOutputs()
		{
			foreach (CargoFactoryProfileItemOutput output in profile.Outputs)
			{
				if (output != null && output.CargoClass != null && output.Quantity > 0)
				{
					return true;
				}
			}
			return false;
		}

		private void TryProduce()
		{
			if (EnoughSpaceForOutputs())
			{
				Produce();
				productionElapsed = 0f;
				State = CargoFactoryItemState.Idle;
				UpdateIdle();
			}
			else
			{
				State = CargoFactoryItemState.ProducedWaitingForFreeSpace;
			}
		}

		private void UpdateIdle()
		{
			if (HasEnoughInput())
			{
				State = CargoFactoryItemState.Producing;
				ProcessInputs();
				TrimCargoWhenNeeded();
			}
		}

		private void TrimCargoWhenNeeded()
		{
			if (Engine.EconomySettings.TrimTraderCargo && cargoFactory.Unit.Components.CargoTrader != null && !cargoFactory.Unit.IsOwnedByPlayer && TraderCargoTrimmer.Trim(cargoFactory.Unit.Components.CargoTrader))
			{
				EngineASX.Instance.DebugInfo.CargoTradersTrimmed++;
			}
		}
	}
}
