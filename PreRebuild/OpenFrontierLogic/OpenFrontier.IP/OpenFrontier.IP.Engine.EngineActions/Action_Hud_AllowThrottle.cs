using OpenFrontier.IP.Common.Triggers;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Hud_AllowThrottle : EngineAction
	{
		public bool Allow;

		public override ActionType Type => ActionType.Hud_AllowThrottle;

		public override void Execute()
		{
			base.Execute();
			if (engine.Hud != null)
			{
				engine.Hud.AllowPlayerThrottle = Allow;
			}
			else
			{
				Debug.LogError("Cannot changed Hud.AllowPlayerThrottle as engine.Hud is not assigned");
			}
		}
	}
}
