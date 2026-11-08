using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.TopPilots
{
	public class TopPilotsScreen : EngineScreen
	{
		public TopPilotsList TopPilotsList;

		public int MaxItemsDisplayed = 10;

		public UnitContextButton UnitContextButton;

		protected override void awake()
		{
			base.awake();
			TopPilotsList.SelectedItemChanged += TopPilotsList_SelectedItemChanged;
		}

		private void TopPilotsList_SelectedItemChanged(ScrollList<Person> sender, Person oldItem, Person newItem)
		{
			RefreshUnitContextButton();
		}

		private void RefreshUnitContextButton()
		{
			UnitContextButton.Unit = GetSelectedPersonUnit();
			UnitContextButton.Refresh();
		}

		private Unit GetSelectedPersonUnit()
		{
			if (TopPilotsList.FirstSelectedItem != null && TopPilotsList.FirstSelectedItem.CurrentUnit != null && EngineASX.Instance.LocalFaction.Intel.IsUnitDiscovered(TopPilotsList.FirstSelectedItem.CurrentUnit))
			{
				return TopPilotsList.FirstSelectedItem.CurrentUnit;
			}
			return null;
		}

		protected override void refresh()
		{
			base.refresh();
			TopPilotsList.SetItems(GetItems());
			RefreshUnitContextButton();
		}

		private IEnumerable<Person> GetItems()
		{
			return GetTopPilots(MaxItemsDisplayed);
		}

		public static IEnumerable<Person> GetTopPilots(int max)
		{
			return (from e in EngineASX.Instance.People
				where e != null && e.IsActiveInGame && e.Kills > 0 && e.Faction != null && !e.IsAutoPilot
				orderby e.Kills descending
				select e).Take(max);
		}
	}
}
