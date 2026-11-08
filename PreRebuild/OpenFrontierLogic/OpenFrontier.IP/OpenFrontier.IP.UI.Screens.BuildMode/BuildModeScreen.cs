using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.BuildMode
{
	public class BuildModeScreen : EngineScreen
	{
		public delegate void BuildModeItemSelectedHandler(BuildModeScreen sender, UnitClass unitClass);

		public BuildModeList BuildModeList;

		public event BuildModeItemSelectedHandler BuildModeItemSelected;

		protected override void awake()
		{
			base.awake();
			BuildModeList.ItemClicked += BuildModeList_ItemClicked;
		}

		private void BuildModeList_ItemClicked(ScrollList<UnitClass> sender, ScrollListItem<UnitClass> item)
		{
			if (item != null && BuildModeItemSelected != null)
			{
				BuildModeItemSelected(this, item.Item);
			}
		}

		protected override void refresh()
		{
			base.refresh();
			BuildModeList.SetItems(GetBuildableUnitClasses());
			EngineASX.Instance.TrySetCameraSpectatorEnabled(enabled: false);
		}

		private IEnumerable<UnitClass> GetBuildableUnitClasses()
		{
			return from e in EngineASX.Instance.UnitClasses
				where e.IsUsable && (e.UnitType == UnitType.Station || (e.UnitType == UnitType.Ship && e.ShipType == ShipType.Container)) && e.AllowBuildByPlayer
				orderby e.SaleCost
				select e;
		}
	}
}
