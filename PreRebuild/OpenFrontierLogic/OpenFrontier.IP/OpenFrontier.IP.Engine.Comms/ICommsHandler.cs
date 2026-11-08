using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Comms
{
	public interface ICommsHandler
	{
		Faction OwnerFaction { get; }

		Person OwnerPilot { get; }

		bool IsEnabled { get; set; }

		ICommsStage GetStage();

		void Hide();

		void Show();
	}
}
