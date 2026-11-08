using OpenFrontier.IP.SavedGames.V2.Model;

namespace OpenFrontier.IP.Engine.CachedFleetSettings
{
	public class CachedFleetSettingsItem
	{
		public ModelFleetSettings FleetSettings { get; set; }

		public SectorTarget HomeBase { get; set; }

		public int FormationId { get; set; } = -1;

		public bool IsSameAs(CachedFleetSettingsItem item)
		{
			if (FormationId != item.FormationId)
			{
				return false;
			}
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
