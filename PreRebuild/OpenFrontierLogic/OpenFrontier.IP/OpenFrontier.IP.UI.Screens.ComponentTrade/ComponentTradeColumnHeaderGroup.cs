namespace OpenFrontier.IP.UI.Screens.ComponentTrade
{
	public class ComponentTradeColumnHeaderGroup : ColumnHeaderGroupUI
	{
		public ComponentTradeItemList ItemList;

		protected override void applySort()
		{
			base.applySort();
			ItemList.SortItems(SelectedHeader.GetComponent<ComponentTradeSortMode>().SortMode, SortDirection);
		}
	}
}
