using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.UI.Screens.CreateUnitVariant
{
	public class CreateUnitVariantComponentList : ScrollList<ComponentClass>
	{
		public enum SortPriceMode
		{
			Name,
			BuyPrice
		}

		public class ComponentItemComparer : IComparer<ComponentClass>
		{
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
					int actualCost = x.GetActualCost();
					int actualCost2 = y.GetActualCost();
					return actualCost.CompareTo(actualCost2) * SortDirection;
				}
				default:
					return 0;
				}
			}
		}

		public CreateUnitVariantComponentsScreen ParentScreen;

		private new ComponentItemComparer ItemComparer;

		public void SortItems(SortPriceMode sortMode, int sortDirection)
		{
			ItemComparer.SortDirection = sortDirection;
			ItemComparer.SortPriceMode = sortMode;
			Refresh();
		}

		protected override void awake()
		{
			ItemComparer = new ComponentItemComparer();
			base.awake();
		}

		protected override void OnRefreshing()
		{
			base.OnRefreshing();
			List<ComponentClass> list = new List<ComponentClass>();
			foreach (ComponentClass componentClass in EngineASX.Instance.ComponentClasses)
			{
				if (componentClass != null && componentClass.AllowBuy && componentClass.ComponentBayType == ParentScreen.CurrentBay.BayType && ParentScreen.IsComponentCompatible(ParentScreen.CurrentBay, componentClass))
				{
					list.Add(componentClass);
				}
			}
			list.Sort(ItemComparer);
			SetItems(list);
		}
	}
}
