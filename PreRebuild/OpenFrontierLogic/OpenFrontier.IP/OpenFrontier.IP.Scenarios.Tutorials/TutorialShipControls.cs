using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.EngineActions;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios.Tutorials
{
	public class TutorialShipControls : MonoBehaviour
	{
		public Action_Hud_ShowSpeech ShowSteeringMessage;

		public Action_Hud_ShowSpeech ThrottleMessage;

		public WorldBase WorldBase;

		private void Awake()
		{
			WorldBase.Initialised += WorldBase_Initialised;
		}

		private void WorldBase_Initialised(WorldBase sender)
		{
			CustomSetup();
		}

		private void CustomSetup()
		{
			string text = "Let's look at how to steer your ship. ";
			text = ((!GameController.Instance.ForceTouchInputEnabled) ? (text + "Use keys A and D to turn the ship left and right.") : (text + "Use the HUD's on-screen buttons to turn left and right."));
			ShowSteeringMessage.Message = text;
			ThrottleMessage.Message = "It's quite simple really - use Keys W and S to change engine power or drag the on-screen slider...";
		}
	}
}
