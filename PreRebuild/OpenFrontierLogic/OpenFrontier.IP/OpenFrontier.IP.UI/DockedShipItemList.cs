using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.UI
{
	public class DockedShipItemList : ScrollList<UnitComponentHolder>
	{
		public UnitHangar Hangar;

		public Faction LocalFaction;

		protected override void OnRefreshing()
		{
			List<UnitComponentHolder> list = new List<UnitComponentHolder>();
			if (Hangar != null)
			{
				foreach (UnitComponentHolder item in from e in Hangar.DockedShips
					orderby !e.Unit.IsPlayerCurrentUnit, !e.Unit.IsOwnedByPlayer, e.ShipName
					select e)
				{
					list.Add(item);
				}
			}
			SetItems(list);
			for (int num = 0; num < ActiveItems.Count; num++)
			{
				((DockedShipItemUI)UIItemPool[num]).LocalFaction = LocalFaction;
			}
			base.OnRefreshing();
		}
	}
}
