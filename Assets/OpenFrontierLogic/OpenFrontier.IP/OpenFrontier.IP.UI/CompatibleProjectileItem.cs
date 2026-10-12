using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class CompatibleProjectileItem : ScrollListItem<ProjectileClass>
	{
		public Image IconImage;

		public Text NameLabel;

		public override void Refresh()
		{
			base.Refresh();
			NameLabel.text = Item.Name;
			if (Item.AmmoClass != null)
			{
				IconImage.sprite = EngineASX.Instance.EngineResources.GetCargoSpriteOrDefault(Item.AmmoClass);
			}
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			if (Item.AmmoClass != null)
			{
				UIController.Instance.ScreenNavigator.ShowCargoInfoScreen(Item.AmmoClass);
			}
		}
	}
}
