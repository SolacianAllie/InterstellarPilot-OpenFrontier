using System.IO;
using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Scenario_TimeElapsed : TriggerBase
	{
		private bool hasSetTime;

		private double targetTime;

		public float Time = 4f;

		public override TriggerType Type => TriggerType.Scenario_TimeElapsed;

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
			targetTime = reader.ReadDouble();
		}

		protected override bool evaluate(EngineASX engine)
		{
			if (hasSetTime)
			{
				if (engine.ScenarioElapsedTime > targetTime)
				{
					return true;
				}
			}
			else
			{
				hasSetTime = true;
				targetTime = engine.ScenarioElapsedTime + (double)Time;
			}
			return false;
		}
	}
}
