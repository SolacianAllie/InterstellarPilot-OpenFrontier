using System.IO;
using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Player_TimePilotting : TriggerBase
	{
		private bool hasSetTime;

		private float targetTime;

		public float Time = 4f;

		public override TriggerType Type => TriggerType.Player_TimePilotting;

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.Write(hasSetTime);
			writer.Write(targetTime);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			hasSetTime = reader.ReadBoolean();
			targetTime = reader.ReadSingle();
		}

		protected override bool evaluate(EngineASX engine)
		{
			if (hasSetTime)
			{
				if (hasSetTime && engine.ElapsedTimeAsPilot > targetTime)
				{
					return true;
				}
			}
			else
			{
				hasSetTime = true;
				targetTime = engine.ElapsedTimeAsPilot + Time;
			}
			return false;
		}
	}
}
