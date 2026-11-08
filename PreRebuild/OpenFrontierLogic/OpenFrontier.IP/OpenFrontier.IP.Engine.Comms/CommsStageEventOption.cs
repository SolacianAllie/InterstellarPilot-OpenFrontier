namespace OpenFrontier.IP.Engine.Comms
{
	public class CommsStageEventOption : ICommsStageOption
	{
		public delegate void SelectedHandler(CommsStageEventOption option, ICommsController commsController);

		public int NumberTimesSelected { get; set; }

		public ICommsStageOptionGroup OptionGroup { get; set; }

		public string OptionText { get; set; }

		public event SelectedHandler Selected;

		public void Select(ICommsController commsController)
		{
			if (Selected != null)
			{
				Selected(this, commsController);
			}
		}
	}
}
