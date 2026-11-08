using System.Collections.Generic;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelPlayer
	{
		public List<ModelUnit> VisitedUnits { get; set; } = new List<ModelUnit>();

		public List<ModelPlayerMessage> Messages { get; set; } = new List<ModelPlayerMessage>();

		public List<ModelPlayerDelayedMessage> DelayedMessages { get; set; } = new List<ModelPlayerDelayedMessage>();

		public ModelMission ActiveJob { get; set; }

		public ModelPlayerWaypoint CustomWaypoint { get; set; }

		public ModelPerson Person { get; set; }

		public ModelFaction Faction => Person?.Faction;

		public ModelPlayerStats Stats { get; set; } = new ModelPlayerStats();
	}
}
