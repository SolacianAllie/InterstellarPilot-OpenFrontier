using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Components;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class OrdersScreenShipItem : ScrollListItem<Unit>
	{
		public CloakImageComponent CloakImageComponent;

		public CargoUsageSlider CargoUsageSlider;

		public UnitConditionControllerUI UnitConditionController;

		public TextMeshProUGUI NameLabel;

		public TextMeshProUGUI PilotNameLabel;

		public Graphic UnderAttackGraphic;

		public Graphic BountyGraphic;

		protected override void awake()
		{
			base.awake();
			UnderAttackGraphic.enabled = false;
		}

		public override void Refresh()
		{
			base.Refresh();
			UnitConditionController.LocalUnit = Item;
			CargoUsageSlider.RefreshFromCargoBayComponent(Item.CargoBayComponent);
			RefreshNameLabel();
			CloakImageComponent.TargetCloakComponent = Item.CloakComponent;
			UnderAttackGraphic.enabled = Item.IsUnderAttack();
			UnitConditionController.Tick();
			BountyGraphic.enabled = Item.UnitPilotHasBounty();
		}

		private void RefreshNameLabel()
		{
			string friendlyName = Item.GetFriendlyName();
			NameLabel.text = friendlyName;
			NameLabel.color = Item.Engine.OwnedColor;
			Person pilot = Item.GetPilot();
			if (pilot != null)
			{
				PilotNameLabel.text = pilot.FullNameWithFullRank;
			}
			else
			{
				PilotNameLabel.text = string.Empty;
			}
		}

		public override void Tick()
		{
			base.Tick();
			UnitConditionController.Tick();
			CloakImageComponent.Tick();
		}

		public override void CleanupOnDisable()
		{
			UnitConditionController.CleanupOnDisable();
		}
	}
}
