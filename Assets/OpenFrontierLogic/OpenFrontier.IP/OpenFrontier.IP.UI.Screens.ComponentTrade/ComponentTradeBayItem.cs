using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.ComponentTrade
{
	public class ComponentTradeBayItem : ScrollListItem<ComponentBay>
	{
		public Image FiringArcImage;

		public Image IconImage;

		public Color NothingInstalledColor = Color.gray;

		public Text NameText;

		public Text InstalledText;

		private Color defaultNameTextColor = Color.white;

		protected override void awake()
		{
			base.awake();
			defaultNameTextColor = NameText.color;
		}

		public override void Refresh()
		{
			base.Refresh();
			NameText.text = Item.GetFriendlyName();
			InstalledText.text = ((Item.InstalledComponent != null) ? Item.InstalledComponent.ComponentClass.GetFriendlyName() : "[None]");
			NameText.color = ((Item.InstalledComponent != null) ? defaultNameTextColor : NothingInstalledColor);
			IconImage.sprite = EngineASX.Instance.EngineResources.GetBayComponentOrAmmoSprite(Item);
			RefreshFiringArcSprite();
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
	}
}
