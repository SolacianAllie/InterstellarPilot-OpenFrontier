using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Player_CurrentHudTarget : TriggerBase
	{
		public Unit Target;

		public override TriggerType Type => TriggerType.Player_CurrentHudTarget;

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteUnitId(Target);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Target = reader.ReadUnitFromId(engine);
		}

		protected override bool evaluate(EngineASX engine)
		{
			if (engine.Hud != null)
			{
				return engine.Hud.CurrentTarget == Target;
			}
			return false;
		}
	}
}
