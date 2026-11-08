using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.ComponentTrade
{
	public class ComponentTradeListItemUI : ScrollListItem<ComponentClass>
	{
		public Image IconImage;

		public Graphic CompatibilityColorTarget;

		private Color defaultColor = Color.white;

		public Text InstalledLabel;

		public Text NameLabel;

		public Text PriceLabel;

		private bool setDefaultColor;

		public ComponentTradeScreen ParentUI => ((ComponentTradeItemList)ParentList).ComponentTradeUI;

		public Color IncompatibleColor => ((ComponentTradeItemList)ParentList).IncompatibleColor;

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null)
			{
				UpdateDefaultColor();
				NameLabel.text = Item.GetFriendlyName();
				UpdatePriceLabel();
				if (CompatibilityColorTarget != null)
				{
					CompatibilityColorTarget.color = (IsComponentCompatible() ? defaultColor : IncompatibleColor);
				}
				ComponentBase installedComponent = ParentUI.CurrentBay.InstalledComponent;
				InstalledLabel.gameObject.SetActive(installedComponent != null && Item == installedComponent.ComponentClass);
				IconImage.sprite = EngineASX.Instance.EngineResources.GetComponentClassOrBaySpriteOrDefault(Item);
			}
		}

		private void Update()
		{
			if (Item != null)
			{
				UpdatePriceLabel();
			}
		}

		private void UpdatePriceLabel()
		{
			int componentClassSaleCost = ComponentTradeHelper.GetComponentClassSaleCost(ParentUI.Eng, Item, ParentUI.DockUI.PlayerDockUnit.Faction, ParentUI.Eng.LocalFaction);
			PriceLabel.text = TextFormattingHelper.FormatCredits(componentClassSaleCost, includeSuffix: true);
			if (ParentUI.IgnoreCost)
			{
				PriceLabel.color = ParentUI.Eng.CreditsNeutralColor;
			}
			else
			{
				PriceLabel.color = (ParentUI.IsComponentAffordable(Item) ? ParentUI.Eng.GameSettings.AffordableColor : ParentUI.Eng.GameSettings.UnaffordableColor);
			}
		}

		public bool IsComponentCompatible()
		{
			string error = null;
			return ParentUI.IsComponentCompatible(ParentUI.CurrentBay, Item, !ParentUI.IgnoreCompatibility, out error);
		}

		protected override void awake()
		{
			base.awake();
			UpdateDefaultColor();
		}

		private void UpdateDefaultColor()
		{
			if (!setDefaultColor)
			{
				setDefaultColor = true;
				defaultColor = CompatibilityColorTarget.color;
			}
		}
	}
}
