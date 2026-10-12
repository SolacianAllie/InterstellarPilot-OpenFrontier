namespace OpenFrontier.IP.Engine.Comms
{
	public interface ICommsStageOption
	{
		string OptionText { get; }

		int NumberTimesSelected { get; set; }

		ICommsStageOptionGroup OptionGroup { get; set; }

		void Select(ICommsController commsController);
	}
}
