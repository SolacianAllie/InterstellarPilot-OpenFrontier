namespace OpenFrontier.IP.Engine.Core
{
	public interface ICreditsSource
	{
		int Credits { get; set; }

		bool IsAffordableConsideringReserve(int cost);
	}
}
