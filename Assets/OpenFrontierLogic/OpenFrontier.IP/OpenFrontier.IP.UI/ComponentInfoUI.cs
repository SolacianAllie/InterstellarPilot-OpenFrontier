using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Screens;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class ComponentInfoUI : ScreenBase
	{
		public Button CompatibleProjectilesButton;

		public ComponentInfoGenerator ComponentInfoGenerator;

		public Text ComponentNameLabel;

		public ComponentClass CurrentComponentClass;

		public Text DescriptionLabel;

		public string GetComponentDescription(ComponentClass componentClass)
		{
			if (!string.IsNullOrEmpty(componentClass.Description))
			{
				return componentClass.Description;
			}
			if (!string.IsNullOrEmpty(componentClass.ComponentType.Description))
			{
				return componentClass.ComponentType.Description;
			}
			return "No description available";
		}

		protected override void awake()
		{
			base.awake();
			CompatibleProjectilesButton.onClick.AddListener(CompatibleProjectilesButton_Activated);
		}

		protected override void refresh()
		{
			base.refresh();
			if (CurrentComponentClass != null)
			{
				DescriptionLabel.text = GetComponentDescription(CurrentComponentClass);
				ComponentNameLabel.text = CurrentComponentClass.GetFriendlyName();
				if (ComponentInfoGenerator != null)
				{
					ComponentInfoGenerator.Item = CurrentComponentClass;
					ComponentInfoGenerator.Refresh();
				}
			}
			CompatibleProjectilesButton.interactable = CurrentComponentClass != null && CanShowCompatibleProjectiles();
		}

		private void CompatibleProjectilesButton_Activated()
		{
			ProjectileTurretClass projectileTurretClass = CurrentComponentClass as ProjectileTurretClass;
			if (projectileTurretClass != null)
			{
				UIController.Instance.ScreenNavigator.ShowProjectilesScreen(projectileTurretClass);
			}
		}

		private int ProjectilesWithAmmoCount(IEnumerable<ProjectileClass> projectileTypes)
		{
			return projectileTypes.Count((ProjectileClass e) => e.AmmoClass != null);
		}

		private bool CanShowCompatibleProjectiles()
		{
			if (CurrentComponentClass is ProjectileTurretClass)
			{
				return ProjectilesWithAmmoCount(((ProjectileTurretClass)CurrentComponentClass).CompatibleProjectiles) > 0;
			}
			return false;
		}
	}
}
