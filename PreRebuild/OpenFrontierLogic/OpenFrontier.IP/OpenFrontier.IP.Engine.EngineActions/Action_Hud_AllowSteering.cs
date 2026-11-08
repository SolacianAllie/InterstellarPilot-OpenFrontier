using OpenFrontier.IP.Common.Triggers;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Hud_AllowSteering : EngineAction
	{
		public bool Allow;

		public override ActionType Type => ActionType.Hud_AllowSteering;

		public override void Execute()
		{
			base.Execute();
			if (engine.Hud != null)
			{
				engine.Hud.AllowPlayerSteering = Allow;
			}
			else
			{
				Debug.LogError("Cannot changed Hud.AllowPlayerSteering as engine.Hud is not assigned");
			}
		}
	}
}
