using OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Stages;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms
{
	public class DynamicCommsHandler : ICommsHandler
	{
		private bool isEnabled = true;

		private DynamicCommsInitialStage initialStage = new DynamicCommsInitialStage();

		public bool IsEnabled
		{
			get
			{
				return isEnabled;
			}
			set
			{
				isEnabled = value;
			}
		}

		public Faction OwnerFaction { get; set; }

		public Person OwnerPilot { get; set; }

		public ICommsStage GetStage()
		{
			initialStage.DynamicCommsHandler = this;
			return initialStage;
		}

		public void Hide()
		{
		}

		public void Show()
		{
		}
	}
}
