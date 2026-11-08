using System.Collections.Generic;
using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.BuySectorIntelConfirm
{
	internal class BuySectorIntelConfirmScreen : EngineScreen
	{
		public ToggleWaypointButtonController ToggleWaypointController;

		private List<Unit> units = new List<Unit>();

		public BuySectorIntelConfirmItemList BuySectorIntelConfirmItemList;

		public List<Unit> Units => units;

		protected override void awake()
		{
			base.awake();
			BuySectorIntelConfirmItemList.SelectedItemChanged += BuySectorIntelConfirmItemList_SelectedItemChanged;
		}

		private void BuySectorIntelConfirmItemList_SelectedItemChanged(ScrollList<Unit> sender, Unit oldItem, Unit newItem)
		{
			RefreshWaypointToggleEnabled();
		}

		private void RefreshWaypointToggleEnabled()
		{
			ToggleWaypointController.CurrentTarget = BuySectorIntelConfirmItemList.FirstSelectedItem;
		}

		protected override void refresh()
		{
			base.refresh();
			BuySectorIntelConfirmItemList.SetItems(units);
			RefreshWaypointToggleEnabled();
		}
	}
}
