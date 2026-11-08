using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Player_NewMessage : EngineAction
	{
		public float Delay = 2f;

		public PlayerActiveMessage Message;

		public bool Notifications = true;

		public bool Important = true;

		public override ActionType Type => ActionType.Player_NewMessage;

		public override void Execute()
		{
			base.Execute();
			if (Delay > 0f)
			{
				engine.LocalPlayer.AddMessageDelayed(Message, Delay, Notifications, Important);
			}
			else
			{
				engine.LocalPlayer.AddMessage(Message, Notifications, Important);
			}
		}
	}
}
