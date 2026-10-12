using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Comms;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Dialog_StageFinished : TriggerBase
	{
		public DialogStage DialogStage;

		private bool hasFinished;

		public override TriggerType Type => TriggerType.Dialog_StageFinished;

		protected override bool evaluate(EngineASX engine)
		{
			return hasFinished;
		}

		private void DialogStage_FinishedShowing(DialogStage sender)
		{
			if (gameObject.activeSelf)
			{
				hasFinished = true;
				triggerGroup.Evaluate();
			}
		}

		private void OnEnable()
		{
			hasFinished = false;
			if (DialogStage != null)
			{
				DialogStage.FinishedShowing += DialogStage_FinishedShowing;
			}
		}

		private void OnDisable()
		{
			hasFinished = false;
			if (DialogStage != null)
			{
				DialogStage.FinishedShowing -= DialogStage_FinishedShowing;
			}
		}
	}
}
