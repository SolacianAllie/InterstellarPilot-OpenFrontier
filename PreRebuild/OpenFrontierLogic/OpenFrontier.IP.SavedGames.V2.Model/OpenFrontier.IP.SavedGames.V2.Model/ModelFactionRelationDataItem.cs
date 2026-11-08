using OpenFrontier.IP.Common.Factions;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelFactionRelationDataItem
	{
		public ModelFaction OtherFaction { get; set; }

		public bool PermanentPeace { get; set; }

		public bool RestrictHostilityTimeout { get; set; }

		public Neutrality Neutrality { get; set; }

		public double HostilityEndTime { get; set; }
	}
}
