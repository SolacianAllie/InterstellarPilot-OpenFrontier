using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Comms;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Dialog_NumTimesStageShown : TriggerBase
	{
		public int Count;

		public DialogStage DialogStage;

		public override TriggerType Type => TriggerType.Dialog_NumTimesStageShown;

		protected override bool evaluate(EngineASX engine)
		{
			if (DialogStage != null)
			{
				return DialogStage.NumTimesShown > Count;
			}
			return false;
		}
	}
}
