using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Comms;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Dialog_MessageConfirmed : TriggerBase
	{
		public DialogMessage Message;

		private int numTimesConfirmed;

		public override TriggerType Type => TriggerType.Dialog_MessageConfirmed;

		protected override bool evaluate(EngineASX engine)
		{
			if (Message.TimesConfirmed > numTimesConfirmed)
			{
				numTimesConfirmed = Message.TimesConfirmed;
				return true;
			}
			return false;
		}

		private void OnEnable()
		{
			numTimesConfirmed = Message.TimesConfirmed;
		}
	}
}
