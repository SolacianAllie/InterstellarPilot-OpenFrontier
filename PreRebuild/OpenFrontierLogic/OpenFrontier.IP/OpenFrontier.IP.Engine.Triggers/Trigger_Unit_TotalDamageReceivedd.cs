using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Unit_TotalDamageReceivedd : TriggerBase
	{
		public float Damage;

		public ComparisonOp Operator;

		public Unit Unit;

		public override TriggerType Type => TriggerType.Unit_TotalDamageReceived;

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
			if (Unit != null && Unit.Destructable != null)
			{
				float totalDamageReceived = Unit.Destructable.TotalDamageReceived;
				switch (Operator)
				{
				case ComparisonOp.Greater:
					return totalDamageReceived > Damage;
				case ComparisonOp.LessThan:
					return totalDamageReceived < Damage;
				case ComparisonOp.LessThanOrEqual:
					return totalDamageReceived <= Damage;
				case ComparisonOp.GreaterOrEqual:
					return totalDamageReceived >= Damage;
				}
			}
			return false;
		}
	}
}
