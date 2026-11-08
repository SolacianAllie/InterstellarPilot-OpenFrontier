using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class CargoOptions : MonoBehaviour
	{
		public Toggle TargetLocalUnitToggle;

		public Toggle TargetCurrentTargetToggle;

		public Button ChangeCargoButton;

		public Button RemoveCargoButton;

		private void Awake()
		{
			ChangeCargoButton.onClick.AddListener(ChangeCargoButtonClick);
			RemoveCargoButton.onClick.AddListener(RemoveCargoButtonClick);
		}

		private void Update()
		{
			ChangeCargoButton.interactable = GetTargetCargoBayComponent() != null;
			RemoveCargoButton.interactable = GetTargetCargoBayComponent() != null && !GetTargetCargoBayComponent().IsEmpty;
		}

		private void RemoveCargoButtonClick()
		{
			CargoBayComponent targetCargoBayComponent = GetTargetCargoBayComponent();
			if (targetCargoBayComponent == null)
			{
				UIController.Instance.ShowMessageBox("The target does not have a cargo bay", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				return;
			}
			targetCargoBayComponent.RemoveAllCargo();
			UIController.Instance.ShowMessageBox("Cargo was removed", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
		}

		private void ChangeCargoButtonClick()
		{
			CargoBayComponent targetCargoBayComponent = GetTargetCargoBayComponent();
			if (targetCargoBayComponent == null)
			{
				UIController.Instance.ShowMessageBox("The target does not have a cargo bay", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
			else
			{
				UIController.Instance.ScreenNavigator.ShowChangeUnitCargoScreen(targetCargoBayComponent.Unit, null);
			}
		}

		private CargoBayComponent GetTargetCargoBayComponent()
		{
			Unit targetUnit = GetTargetUnit();
			if (targetUnit != null)
			{
				return targetUnit.CargoBayComponent;
			}
			return null;
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
