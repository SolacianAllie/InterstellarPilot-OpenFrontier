using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens.ComponentTrade;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class UpgradeUnitOptions : MonoBehaviour
	{
		public Button CustomUpgradeButton;

		public Toggle TargetLocalUnitToggle;

		public Toggle TargetCurrentTargetToggle;

		public Button ButtonPrefab;

		public Transform UpgradeButtonsRoot;

		public Toggle AutoAddAmmoToggle;

		private void Awake()
		{
			foreach (ComponentClass componentClass in EngineASX.Instance.ComponentClasses.Where((ComponentClass e) => e.AllowBuy && e is TurretClass turretClass && (turretClass.AIConventionalWeapon || turretClass.IsMiningLaser || turretClass.IsPointDefence)))
			{
				Button button = UnityEngine.Object.Instantiate(ButtonPrefab);
				button.transform.SetParent(UpgradeButtonsRoot);
				button.transform.localScale = Vector3.one;
				button.onClick.AddListener(() =>
				{
					UpgradeUnit(componentClass);
				});
				button.SetText(componentClass.Name);
			}
			CustomUpgradeButton.onClick.AddListener(CustomUpgradeButtonClick);
		}

		private void CustomUpgradeButtonClick()
		{
			Unit targetUnit = GetTargetUnit();
			if (targetUnit != null && targetUnit.Components != null)
			{
				UIController.Instance.ScreenNavigator.ShowEquipmentTradeScreen(targetUnit, null, null, (ComponentTradeScreen screen) =>
				{
					screen.PauseWhenShown = true;
					screen.IgnoreCompatibility = true;
					screen.IgnoreCost = true;
				});
			}
		}

		private void UpgradeUnit(ComponentClass componentClass)
		{
			try
			{
				Unit targetUnit = GetTargetUnit();
				IEnumerable<ComponentBay> enumerable = FindBaysForComponentClass(targetUnit, componentClass);
				int num = 0;
				foreach (ComponentBay item in enumerable)
				{
					item.InstallComponent(componentClass);
					num++;
					if (!(componentClass is TurretClass { UsesAmmo: not false } turretClass) || !AutoAddAmmoToggle.isOn)
					{
						continue;
					}
					foreach (ProjectileClass compatibleProjectile in ((ProjectileTurretClass)turretClass).CompatibleProjectiles)
					{
						targetUnit.Components.CargoBayComponent.AddToCargoIfFits(compatibleProjectile.AmmoClass, compatibleProjectile.DefaultAmmoComplement);
					}
				}
				if (num > 0)
				{
					UIController.Instance.ShowMessageBox($"{num} bays were upgraded", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
				}
				else
				{
					UIController.Instance.ShowMessageBox("No compatible bays were found to upgrade", MessageBoxButtons.Ok);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				UIController.Instance.ShowError("Failed to ugprade unit");
			}
		}

		private IEnumerable<ComponentBay> FindBaysForComponentClass(Unit unit, ComponentClass componentClass)
		{
			foreach (ComponentBay bay in unit.Components.Bays)
			{
				if (bay.BayType == componentClass.ComponentBayType)
				{
					yield return bay;
				}
			}
		}

		private Unit GetTargetUnit()
		{
			if (TargetLocalUnitToggle.isOn)
			{
				return EngineASX.Instance.LocalUnit;
			}
			return EngineASX.Instance.Hud.CurrentTarget;
		}
	}
}
