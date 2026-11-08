namespace OpenFrontier.IP.Engine.Comms
{
	public class CommsStageOptionTargetGroup : ICommsStageOption
	{
		public int NumberTimesSelected { get; set; }

		public ICommsStageOptionGroup OptionGroup { get; set; }

		public string OptionText { get; set; }

		public ICommsStageOptionGroup TargetOptionGroup { get; set; }

		public void Select(ICommsController commsController)
		{
			commsController.OptionGroupFilter = TargetOptionGroup;
			commsController.RefreshStageOptions();
		}
	}
}
