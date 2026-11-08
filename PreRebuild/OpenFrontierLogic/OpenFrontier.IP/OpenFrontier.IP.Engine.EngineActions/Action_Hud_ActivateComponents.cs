using System;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.UI.Screens.Hud;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Hud_ActivateComponents : EngineAction
	{
		public bool ShowComponents;

		public override ActionType Type => ActionType.Hud_ActivateComponents;

		public override void Execute()
		{
			base.Execute();
			foreach (HudComponent value in Enum.GetValues(typeof(HudComponent)))
			{
				engine.Hud.SetHudComponentVisible(value, ShowComponents);
			}
		}
	}
}
