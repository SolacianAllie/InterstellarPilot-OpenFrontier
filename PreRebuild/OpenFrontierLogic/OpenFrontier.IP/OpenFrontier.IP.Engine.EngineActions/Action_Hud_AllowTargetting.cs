using OpenFrontier.IP.Common.Triggers;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Hud_AllowTargetting : EngineAction
	{
		public bool Allow;

		public override ActionType Type => ActionType.Hud_AllowTargetting;

		public override void Execute()
		{
			base.Execute();
			if (engine.Hud != null)
			{
				engine.Hud.AllowPlayerTargetting = Allow;
			}
			else
			{
				Debug.LogError("Cannot changed Hud.AllowPlayerTargetting as engine.Hud is not assigned");
			}
		}
	}
}
