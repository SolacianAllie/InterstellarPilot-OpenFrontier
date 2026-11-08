using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Player_HasOpenedMessage : TriggerBase
	{
		public int MessageId = -1;

		public override TriggerType Type => TriggerType.Player_HasOpenedMessage;

		protected override bool evaluate(EngineASX engine)
		{
			if (engine.LocalPlayer != null)
			{
				PlayerActiveMessage messageById = engine.LocalPlayer.GetMessageById(MessageId);
				if (messageById != null && messageById.Opened)
				{
					return true;
				}
			}
			return false;
		}
	}
}
