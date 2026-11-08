namespace OpenFrontier.IP.Engine.Comms
{
	public interface ICommsMessage
	{
		string MessageText { get; }

		void Confirm();
	}
}
