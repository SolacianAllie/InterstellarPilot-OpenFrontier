using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class UnitComponentsScreen : EngineScreen
	{
		public Toggle AutoFireModeDisabledToggle;

		public Toggle AutoFireModeAnyTargetToggle;

		public Toggle AutoFireModeCurrentTargetOnlyToggle;

		private Unit unit;

		public Text InstalledComponentLabel;

		public Text ConditionLabel;

		public Button InfoButton;

		public ShipComponentsItemList ItemList;

		public TextMeshProUGUI PowerLabel;

		public Graphic PoweredGraphic;

		public Toggle PowerToggle;

		public Text SelectedItemLabel;

		public GameObject BayRoot;

		public GameObject InstalledComponentRoot;

		public Text TurretAmmoLabel;

		public GameObject TurretAmmoRoot;

		public Button TurretChangeAmmoButton;

		public GameObject TurretRoot;

		public Toggle AutoFireToggle;

		public TextMeshProUGUI Title;

		public Unit Unit
		{
			get
			{
				return unit;
			}
			set
			{
				unit = value;
			}
		}

		public void OnPowerToggleChanged(bool state)
		{
			if (ItemList.FirstSelectedItem.InstalledComponent != null)
			{
				ItemList.FirstSelectedItem.InstalledComponent.UserPowered = state;
				RefreshPowerButton();
				ItemList.Refresh();
			}
		}

		public static string GetTurretAmmoLabel(ProjectileTurretComponent p)
		{
			if (p.CurProjectileClass != null)
			{
				CargoClass ammoClass = p.CurProjectileClass.AmmoClass;
				int cargoCountOf = p.UnitComponents.GetCargoCountOf(ammoClass);
				return $"{TextFormattingHelper.FormatNumber(cargoCountOf)}x {ammoClass.ClassName}";
			}
			return "[None]";
		}

		protected override void awake()
		{
			base.awake();
			ItemList.SelectedItemChanged += ItemList_SelectedItemChanged;
			if (PowerToggle != null)
			{
				PowerToggle.onValueChanged.AddListener(OnPowerToggleChanged);
			}
			if (TurretChangeAmmoButton != null)
			{
				TurretChangeAmmoButton.onClick.AddListener(ChangeTurretAmmo);
			}
			InfoButton.onClick.AddListener(ShowInfo);
			AutoFireToggle.onValueChanged.AddListener(AutoFireToggleValueChanged);
			AutoFireModeAnyTargetToggle.onValueChanged.AddListener(AutoFireModeAnyTargetValueChanged);
			AutoFireModeCurrentTargetOnlyToggle.onValueChanged.AddListener(AutoFireModeCurrentTargetOnlyValueChanged);
			AutoFireModeDisabledToggle.onValueChanged.AddListener(AutoFireModeDisabledToggleValueChanged);
		}

		private void AutoFireModeDisabledToggleValueChanged(bool value)
		{
			if (value && unit != null)
			{
				unit.Components.InitAutoTurretModuleIfNull();
				unit.Components.AutoTurretModule.FireMode = AutoTurretFireMode.Disabled;
				ItemList.Refresh();
			}
		}

		private void AutoFireModeAnyTargetValueChanged(bool value)
		{
			if (value && unit != null)
			{
				unit.Components.InitAutoTurretModuleIfNull();
				unit.Components.AutoTurretModule.FireMode = AutoTurretFireMode.AnyTarget;
				ItemList.Refresh();
			}
		}

		private void AutoFireModeCurrentTargetOnlyValueChanged(bool value)
		{
			if (value && unit != null)
			{
				unit.Components.InitAutoTurretModuleIfNull();
				unit.Components.AutoTurretModule.FireMode = AutoTurretFireMode.PreferredTargetOnly;
				ItemList.Refresh();
			}
		}

		protected override void refresh()
		{
			base.refresh();
			if (unit != null)
			{
				ItemList.SetItemsFromUnitComponents(unit.Components);
				Title.text = unit.GetFriendlyName() + " Components";
			}
			else
			{
				ItemList.ClearActiveItems();
			}
			RefreshSelectedPanel();
		}

		protected override void update()
		{
			base.update();
			RefreshPowerButton();
		}

		private void ShowInfo()
		{
			UIController.Instance.ScreenNavigator.ShowComponentInfoScreen(ItemList.FirstSelectedItem.InstalledComponent.ComponentClass);
		}

		private void ChangeTurretAmmo()
		{
			if (((ProjectileTurretComponent)ItemList.FirstSelectedItem.InstalledComponent).TrySelectNextProjectileClass(ignoreAmmo: true, conventionalWeaponsOnly: false))
			{
				RefreshSelectedPanel();
				ItemList.Refresh();
			}
		}

		private void ItemList_SelectedItemChanged(ScrollList<ComponentBay> sender, ComponentBay oldItem, ComponentBay newItem)
		{
			RefreshSelectedPanel();
		}

		private void RefreshSelectedPanel()
		{
			BayRoot.gameObject.SetActive(ItemList.FirstSelectedItem != null);
			if (ItemList.FirstSelectedItem != null)
			{
				InstalledComponentLabel.text = ((ItemList.FirstSelectedItem.InstalledComponent != null) ? ItemList.FirstSelectedItem.InstalledComponent.ComponentClass.GetFriendlyName() : "[None]");
				InstalledComponentRoot.SetActive(ItemList.FirstSelectedItem.InstalledComponent != null);
				if (ItemList.FirstSelectedItem.InstalledComponent != null)
				{
					ConditionLabel.text = $"{ItemList.FirstSelectedItem.InstalledComponent.HealthNormalized:P2}";
					RefreshPowerButton();
					TurretComponent turretComponent = ItemList.FirstSelectedItem.InstalledComponent as TurretComponent;
					bool flag = turretComponent != null;
					TurretRoot.gameObject.SetActive(flag);
					if (flag)
					{
						ProjectileTurretComponent projectileTurretComponent = turretComponent as ProjectileTurretComponent;
						bool flag2 = projectileTurretComponent != null && projectileTurretComponent.UsesAmmo;
						TurretAmmoRoot.gameObject.SetActive(flag2);
						if (flag2)
						{
							TurretAmmoLabel.text = GetTurretAmmoLabel(projectileTurretComponent);
							ProjectileClass projectileClass = null;
							if (projectileTurretComponent != null)
							{
								projectileClass = projectileTurretComponent.GetNextProjectileClass(ignoreAmmo: false, conventionalWeaponsOnly: false);
							}
							bool active = projectileTurretComponent != null && projectileClass != null;
							TurretChangeAmmoButton.gameObject.SetActive(active);
							TurretChangeAmmoButton.interactable = unit.IsPilottedByPlayer();
						}
						bool flag3 = turretComponent.CanAutoFire();
						AutoFireToggle.gameObject.SetActive(flag3);
						if (flag3)
						{
							AutoFireToggle.isOn = turretComponent.AutoFire;
						}
						AutoFireToggle.interactable = !turretComponent.IsAlwaysAutoFire && unit.IsPilottedByPlayer();
					}
				}
				SelectedItemLabel.text = ItemList.FirstSelectedItem.GetFriendlyNameAndIfModded();
			}
			unit.Components.InitAutoTurretModuleIfNull();
			switch (unit.Components.AutoTurretModule.FireMode)
			{
			case AutoTurretFireMode.AnyTarget:
				AutoFireModeAnyTargetToggle.isOn = true;
				break;
			case AutoTurretFireMode.PreferredTargetOnly:
				AutoFireModeCurrentTargetOnlyToggle.isOn = true;
				break;
			case AutoTurretFireMode.Disabled:
				AutoFireModeDisabledToggle.isOn = true;
				break;
			}
			Toggle autoFireModeAnyTargetToggle = AutoFireModeAnyTargetToggle;
			Toggle autoFireModeCurrentTargetOnlyToggle = AutoFireModeCurrentTargetOnlyToggle;
			bool flag4 = (AutoFireModeDisabledToggle.interactable = unit.IsPilottedByPlayer());
			bool interactable = (autoFireModeCurrentTargetOnlyToggle.interactable = flag4);
			autoFireModeAnyTargetToggle.interactable = interactable;
		}

		private void RefreshPowerButton()
		{
			bool flag = ItemList.FirstSelectedItem != null && ItemList.FirstSelectedItem.InstalledComponent != null;
			PowerToggle.gameObject.SetActive(flag);
			if (flag)
			{
				bool canSetUserPower = ItemList.FirstSelectedItem.InstalledComponent.ComponentClass.ComponentType.CanSetUserPower;
				PowerToggle.interactable = canSetUserPower;
				PowerToggle.isOn = ItemList.FirstSelectedItem.InstalledComponent.UserPowered;
				PowerLabel.text = (ItemList.FirstSelectedItem.InstalledComponent.UserPowered ? "Online" : "Offline");
				PoweredGraphic.color = (ItemList.FirstSelectedItem.InstalledComponent.UserPowered ? Color.green : Color.red);
				PowerToggle.interactable = unit.IsPilottedByPlayer();
			}
		}

		private void AutoFireToggleValueChanged(bool value)
		{
			TurretComponent turretComponent = (TurretComponent)ItemList.FirstSelectedItem.InstalledComponent;
			if (turretComponent.CanAutoFire())
			{
				turretComponent.AutoFire = value;
				ItemList.Refresh();
			}
		}
	}
}
