using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class ShipInfoBaysItemUI : ScrollListItem<ComponentBay>
	{
		public Image FiringArcImage;

		public Image IconImage;

		public Text BayNameLabel;

		public Text InstalledLabel;

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null)
			{
				BayNameLabel.text = Item.GetFriendlyName();
				InstalledLabel.text = ((Item.InitialComponentClass != null) ? Item.InitialComponentClass.GetFriendlyName() : "[None]");
				RefreshIconImageSprite();
				RefreshFiringArcSprite();
			}
		}

		private void RefreshFiringArcSprite()
		{
			bool flag = Item.ShouldShowFiringArcSprite(showIf360degrees: true);
			FiringArcImage.enabled = flag;
			if (flag)
			{
				FiringArcImage.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f - Item.transform.localRotation.eulerAngles.y));
				FiringArcImage.sprite = Item.GetFiringArcSprite();
			}
		}

		private void RefreshIconImageSprite()
		{
			IconImage.sprite = GetIconImageSprite();
		}

		private Sprite GetIconImageSprite()
		{
			if (Item.InitialComponentClass != null)
			{
				Sprite componentClassSprite = EngineASX.Instance.EngineResources.GetComponentClassSprite(Item.InitialComponentClass);
				if (componentClassSprite != null)
				{
					return componentClassSprite;
				}
			}
			return EngineASX.Instance.EngineResources.GetComponentBayTypeSpriteOrDefault(Item.BayType);
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			if (Item.InitialComponentClass != null)
			{
				ShowInfo();
			}
		}

		private void ShowInfo()
		{
			if (Item != null && Item.InitialComponentClass != null)
			{
				UIController.Instance.ScreenNavigator.ShowComponentInfoScreen(Item.InitialComponentClass);
			}
		}
	}
}
