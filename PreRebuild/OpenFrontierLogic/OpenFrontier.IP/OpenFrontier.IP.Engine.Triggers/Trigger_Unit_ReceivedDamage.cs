using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Unit_ReceivedDamage : TriggerBase
	{
		private float tDamage;

		public Unit Unit;

		public override TriggerType Type => TriggerType.Unit_ReceivedDamage;

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
			if (Unit != null && Unit.Destructable != null && Unit.Destructable.TotalDamageReceived > tDamage)
			{
				tDamage = Unit.Destructable.TotalDamageReceived;
				return true;
			}
			return false;
		}

		private void OnEnable()
		{
			if (Unit != null && Unit.Destructable != null)
			{
				tDamage = Unit.Destructable.TotalDamageReceived;
			}
		}
	}
}
