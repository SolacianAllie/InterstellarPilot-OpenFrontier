namespace OpenFrontier.IP.Engine.Comms
{
	public interface ICommsController
	{
		ICommsStage CommsStage { get; }

		ICommsStageOptionGroup OptionGroupFilter { get; set; }

		ICommsHandler CommsHandler { get; }

		int NumStagesShownFromCommsHandler { get; }

		void ChangeStage(ICommsStage newStage);

		void Dismiss();

		void RefreshStageOptions();

		void GoToHandlerDefaultStage();
	}
}
