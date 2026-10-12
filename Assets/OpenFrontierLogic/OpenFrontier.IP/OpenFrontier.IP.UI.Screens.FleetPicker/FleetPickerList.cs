using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.FleetPicker
{
	public class FleetPickerList : ScrollList<FleetPickerItem>
	{
		public FleetPickerListGenericItem NoneFleetItemPrefab;

		public FleetPickerListGenericItem CreateNewFleetItemPrefab;

		public override GameObject GetItemPrefab(FleetPickerItem item)
		{
			if (item != null)
			{
				switch (item.FleetPickerItemType)
				{
				case FleetPickerItemType.CreateNew:
					return CreateNewFleetItemPrefab.gameObject;
				case FleetPickerItemType.None:
					return NoneFleetItemPrefab.gameObject;
				}
			}
			return base.GetItemPrefab(item);
		}
	}
}
