using OpenFrontier.IP.SavedGames.V2.Model;

namespace OpenFrontier.IP.Engine.CachedFleetSettings
{
	public class DefaultPlayerFleetSettings
	{
		public ModelFleetSettings FleetSettings { get; set; }

		public SectorTarget HomeBase { get; set; }

		public bool IsSameAs(CachedFleetSettingsItem item)
		{
			if (HomeBase == null != (item.HomeBase == null))
			{
				return false;
			}
			if (HomeBase != null && !HomeBase.IsSameTargetAs(item.HomeBase))
			{
				return false;
			}
			if (!FleetSettings.AreSameAs(item.FleetSettings))
			{
				return false;
			}
			return true;
		}
	}
}
