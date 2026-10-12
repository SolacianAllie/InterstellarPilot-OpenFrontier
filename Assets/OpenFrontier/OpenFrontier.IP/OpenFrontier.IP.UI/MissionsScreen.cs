namespace OpenFrontier.IP.UI
{
	public class MissionsScreen : EngineScreen
	{
		public MissionsScreenItemList MissionList;

		protected override void refresh()
		{
			base.refresh();
			if (MissionList != null)
			{
				MissionList.Refresh();
			}
		}
	}
}
