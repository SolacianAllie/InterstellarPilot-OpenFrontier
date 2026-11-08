using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class UnitComponentsListItem : ScrollListItem<ComponentBay>
	{
		public Image FiringArcImage;

		public Graphic AutoFireEnabledGraphic;

		public Image IconImage;

		public Slider ConditionSlider;

		public Image ConditionSliderColorTarget;

		public TextMeshProUGUI NameLabel;

		public TextMeshProUGUI InstalledLabel;

		public Graphic PoweredUpGraphic;

		public override void Refresh()
		{
			base.Refresh();
			NameLabel.text = Item.GetFriendlyNameAndIfModded();
			RefreshInstalledText();
			RefreshConditionSlider();
			RefreshPoweredUpGraphic();
			RefreshIconImage();
			AutoFireEnabledGraphic.enabled = Item.InstalledComponent is TurretComponent turretComponent && turretComponent.AutoFireActive;
			RefreshFiringArcSprite();
		}

		private void RefreshFiringArcSprite()
		{
			bool flag = Item.ShouldShowFiringArcSprite();
			FiringArcImage.enabled = flag;
			if (flag)
			{
				FiringArcImage.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f - Item.transform.localRotation.eulerAngles.y));
				FiringArcImage.sprite = Item.GetFiringArcSprite();
			}
		}

		private void RefreshIconImage()
		{
			IconImage.sprite = EngineASX.Instance.EngineResources.GetBayComponentOrAmmoSprite(Item);
		}

		private void RefreshInstalledText()
		{
			InstalledLabel.text = ((Item.InstalledComponent != null) ? GetInstalledComponentText() : "[None]");
		}

		private string GetInstalledComponentText()
		{
			string text = Item.InstalledComponent.ComponentClass.GetFriendlyName();
			ProjectileTurretComponent projectileTurretComponent = Item.InstalledComponent as ProjectileTurretComponent;
			if (projectileTurretComponent != null && projectileTurretComponent.UsesAmmo)
			{
				text = $"{text} ({UnitComponentsScreen.GetTurretAmmoLabel(projectileTurretComponent)})";
			}
			return text;
		}

		private void RefreshPoweredUpGraphic()
		{
			PoweredUpGraphic.gameObject.SetActive(Item.InstalledComponent != null);
			if (Item.InstalledComponent != null)
			{
				PoweredUpGraphic.color = (Item.InstalledComponent.UserPowered ? GameController.Instance.GameSettings.ColorSettings.PoweredUpComponentColor : GameController.Instance.GameSettings.ColorSettings.PoweredDownComponentColor);
			}
		}

		private void RefreshConditionSlider()
		{
			bool flag = Item.InstalledComponent != null && Item.InstalledComponent.IsDamaged;
			ConditionSlider.gameObject.SetActive(flag);
			if (flag)
			{
				ConditionSlider.value = Item.InstalledComponent.HealthNormalized;
				ConditionSliderColorTarget.color = EngineASX.Instance.GetHullColor(Item.InstalledComponent.HealthNormalized);
			}
		}
	}
}
