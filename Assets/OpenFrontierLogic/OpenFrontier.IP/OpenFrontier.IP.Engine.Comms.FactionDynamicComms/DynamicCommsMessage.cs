namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms
{
	public class DynamicCommsMessage : ICommsMessage
	{
		public string MessageText { get; set; }

		public virtual void Confirm()
		{
		}
	}
}
