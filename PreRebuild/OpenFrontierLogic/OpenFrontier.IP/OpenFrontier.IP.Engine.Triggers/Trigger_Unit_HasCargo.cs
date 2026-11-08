using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Unit_HasCargo : TriggerBase
	{
		public CargoClass CargoType;

		public ComparisonOp Operator = ComparisonOp.LessThan;

		public int Quantity = 1;

		public Unit Unit;

		public override TriggerType Type => TriggerType.Unit_HasCargo;

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.Write(Unit);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Unit = reader.ReadUnitFromId(engine);
		}

		protected override bool evaluate(EngineASX engine)
		{
			if (Unit != null)
			{
				CargoBayComponent cargoBayComponent = Unit.CargoBayComponent;
				if (cargoBayComponent != null)
				{
					int countOf = cargoBayComponent.GetCountOf(CargoType);
					switch (Operator)
					{
					case ComparisonOp.Greater:
						return countOf > Quantity;
					case ComparisonOp.GreaterOrEqual:
						return countOf >= Quantity;
					case ComparisonOp.LessThan:
						return countOf < Quantity;
					case ComparisonOp.LessThanOrEqual:
						return countOf <= Quantity;
					}
				}
			}
			return false;
		}
	}
}
