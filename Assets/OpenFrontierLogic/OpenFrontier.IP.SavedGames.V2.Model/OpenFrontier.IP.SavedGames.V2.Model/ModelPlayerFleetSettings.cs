namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelPlayerFleetSettings
	{
		public bool NotifyWhenOrderComplete { get; set; }

		public bool NotifyWhenScannedHostile { get; set; }

		public bool NotifyWhenAbandonedUnitFound { get; set; }

		public bool NotifyWhenAbandonedCargoFound { get; set; }

		public bool AreSameAs(ModelPlayerFleetSettings modelPlayerFleetSettings)
		{
			if (NotifyWhenOrderComplete == modelPlayerFleetSettings.NotifyWhenOrderComplete && NotifyWhenScannedHostile == modelPlayerFleetSettings.NotifyWhenScannedHostile && NotifyWhenAbandonedUnitFound == modelPlayerFleetSettings.NotifyWhenAbandonedUnitFound)
			{
				return NotifyWhenAbandonedCargoFound == modelPlayerFleetSettings.NotifyWhenAbandonedCargoFound;
			}
			return false;
		}
	}
}
