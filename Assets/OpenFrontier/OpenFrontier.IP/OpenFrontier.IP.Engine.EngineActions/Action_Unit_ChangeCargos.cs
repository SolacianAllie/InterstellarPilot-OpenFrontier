using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Unit_ChangeCargos : EngineAction
	{
		public List<UnitCargoLoadoutItem> CargoItems = new List<UnitCargoLoadoutItem>();

		public bool IgnoreCargoCapacity;

		public Unit Unit;

		public override ActionType Type => ActionType.Unit_ChangeCargos;

		public override void Execute()
		{
			base.Execute();
			if (!(Unit != null))
			{
				return;
			}
			CargoBayComponent cargoBayComponent = Unit.CargoBayComponent;
			for (int i = 0; i < CargoItems.Count; i++)
			{
				if (CargoItems[i].CargoClass != null)
				{
					cargoBayComponent.AddToCargo(CargoItems[i].CargoClass, CargoItems[i].Quantity, IgnoreCargoCapacity);
				}
			}
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteUnitId(Unit);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Unit = reader.ReadUnitFromId(engine);
		}
	}
}
