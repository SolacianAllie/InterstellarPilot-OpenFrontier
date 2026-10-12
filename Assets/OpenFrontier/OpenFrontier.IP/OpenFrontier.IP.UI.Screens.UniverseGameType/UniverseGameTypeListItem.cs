using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.IAP;
using OpenFrontier.Unity.Utils;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UniverseGameType
{
	public class UniverseGameTypeListItem : ScrollListItem<UniverseGameTypeInfo>
	{
		public Image IconImage;

		public Text DescriptionLabel;

		public Text NameLabel;

		public RequiredProductItem RequiredProductItem;

		public override void Refresh()
		{
			base.Refresh();
			NameLabel.text = Item.Name;
			DescriptionLabel.text = Item.Description;
			RefreshProductInfo();
			RefreshIconImage();
		}

		private void RefreshIconImage()
		{
			UnitClass unitClass = ItemUnitClass();
			if (IconImage != null)
			{
				IconImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassRenderSpriteOrDefault(unitClass);
			}
		}

		private UnitClass ItemUnitClass()
		{
			if (Item.DisplayIconOverrideUnitClass != null)
			{
				return Item.DisplayIconOverrideUnitClass;
			}
			return Item.PlayerSpawnUnit.UnitClass;
		}

		private void RefreshProductInfo()
		{
			Item.RequiredProducts.TrimNulls();
			bool flag = Item.RequiredProducts.Count > 0;
			RequiredProductItem.gameObject.SetActive(flag);
			if (flag)
			{
				RequiredProductItem.RequiredProducts = Item.RequiredProducts.ToList();
				RequiredProductItem.Refresh();
			}
		}
	}
}
