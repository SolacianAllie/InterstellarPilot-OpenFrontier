using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Scenario_AllowDialog : TriggerBase
	{
		public override TriggerType Type => TriggerType.Scenario_AllowDialog;

		protected override bool evaluate(EngineASX engine)
		{
			return engine.CanShowDialog();
		}
	}
}
