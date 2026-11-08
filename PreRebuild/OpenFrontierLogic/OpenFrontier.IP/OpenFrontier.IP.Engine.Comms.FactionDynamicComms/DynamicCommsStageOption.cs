namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms
{
	public class DynamicCommsStageOption : ICommsStageOption
	{
		public int NumberTimesSelected { get; set; }

		public ICommsStageOptionGroup OptionGroup { get; set; }

		public string OptionText { get; set; }

		public virtual void Select(ICommsController commsController)
		{
		}
	}
}
