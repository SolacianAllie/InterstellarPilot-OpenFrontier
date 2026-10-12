using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.ComponentTrade
{
	public class ComponentTradeItemList : ScrollList<ComponentClass>
	{
		public enum SortPriceMode
		{
			Name,
			BuyPrice
		}

		public class ComponentItemComparer : IComparer<ComponentClass>
		{
			public ComponentTradeItemList ParentUI;

			public int SortDirection = 1;

			public SortPriceMode SortPriceMode = SortPriceMode.BuyPrice;

			public int Compare(ComponentClass x, ComponentClass y)
			{
				switch (SortPriceMode)
				{
				case SortPriceMode.Name:
					return x.GetFriendlyName().CompareTo(y.GetFriendlyName()) * SortDirection;
				case SortPriceMode.BuyPrice:
				{
					int buyPrice = ParentUI.ComponentTradeUI.GetBuyPrice(x);
					int buyPrice2 = ParentUI.ComponentTradeUI.GetBuyPrice(y);
					return buyPrice.CompareTo(buyPrice2) * SortDirection;
				}
				default:
					return 0;
				}
			}
		}

		public ComponentTradeScreen ComponentTradeUI;

		public Color IncompatibleColor = Color.white;

		private new ComponentItemComparer ItemComparer;

		public Toggle ShowCompatibleOnlyCheck;

		public EngineASX Eng => ComponentTradeUI.Eng;

		public void SortItems(SortPriceMode sortMode, int sortDirection)
		{
			ItemComparer.SortDirection = sortDirection;
			ItemComparer.SortPriceMode = sortMode;
			Refresh();
		}

		protected override void awake()
		{
			ItemComparer = new ComponentItemComparer();
			ItemComparer.ParentUI = this;
			ShowCompatibleOnlyCheck.onValueChanged.AddListener(ShowCompatibleOnlyCheck_Activated);
			base.awake();
		}

		protected override void OnRefreshing()
		{
			base.OnRefreshing();
			List<ComponentClass> list = new List<ComponentClass>();
			if (ComponentTradeUI.UpgradingUnit != null)
			{
				List<ComponentClass> list2 = Eng.ComponentClasses;
				if (ComponentTradeUI.ComponentTrader != null && ComponentTradeUI.ComponentTrader.SellsSpecificComponents)
				{
					list2 = ComponentTradeUI.ComponentTrader.AvailableComponentClasses.Where((ComponentClass e) => e != null).ToList();
				}
				foreach (ComponentClass item in list2)
				{
					if (item != null && item.AllowBuy && item.ComponentBayType == ComponentTradeUI.CurrentBay.BayType)
					{
						string error = null;
						if (ComponentTradeUI.IsComponentCompatible(ComponentTradeUI.CurrentBay, item, ShowCompatibleOnlyCheck.isOn, out error))
						{
							list.Add(item);
						}
					}
				}
			}
			list.Sort(ItemComparer);
			SetItems(list);
		}

		private void ShowCompatibleOnlyCheck_Activated(bool state)
		{
			Refresh();
			ResetScrollPosition();
		}
	}
}
