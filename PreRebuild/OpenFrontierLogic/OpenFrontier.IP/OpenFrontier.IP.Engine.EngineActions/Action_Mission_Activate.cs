using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Mission_Activate : EngineAction
	{
		public Mission Mission;

		public override ActionType Type => ActionType.Mission_Activate;

		public override void Execute()
		{
			base.Execute();
			Mission.MakeActive();
			EngineASX instance = EngineASX.Instance;
			if (instance != null && instance.LocalPlayer != null && instance.LocalPlayer.ActiveMission == null)
			{
				instance.LocalPlayer.ActiveMission = Mission;
			}
		}
	}
}
