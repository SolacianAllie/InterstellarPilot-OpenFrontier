using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.UI.Screens.Hud;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Hud_ActivateComponent : EngineAction
	{
		public HudComponent ComponentType = HudComponent.CommsButton;

		public bool ShowComponent;

		public override ActionType Type => ActionType.Hud_ActivateComponent;

		public override void Execute()
		{
			base.Execute();
			engine.Hud.SetHudComponentVisible(ComponentType, ShowComponent);
		}
	}
}
