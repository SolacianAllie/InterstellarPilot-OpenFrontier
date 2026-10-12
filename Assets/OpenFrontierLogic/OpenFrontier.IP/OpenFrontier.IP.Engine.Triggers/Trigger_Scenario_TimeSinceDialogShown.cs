using OpenFrontier.IP.Common.Triggers;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Scenario_TimeSinceDialogShown : TriggerBase
	{
		public bool IncludePendingRequests = true;

		public float TimeSinceShown = 0.5f;

		public override TriggerType Type => TriggerType.Scenario_TimeSinceDialogShown;

		protected override bool evaluate(EngineASX engine)
		{
			SpeechModel speechModel = engine.Hud.SpeechModel;
			if (!speechModel.IsShowingSpeech && engine.Hud.SpeechModel.MessagesShownCount > 0 && (!IncludePendingRequests || speechModel.Requests.Count == 0))
			{
				return Time.time - engine.Hud.SpeechModel.TimeLastDialogRemoved > TimeSinceShown;
			}
			return false;
		}
	}
}
