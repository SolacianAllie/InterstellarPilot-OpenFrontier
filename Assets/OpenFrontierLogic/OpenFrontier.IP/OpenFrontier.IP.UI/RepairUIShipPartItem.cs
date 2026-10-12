using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class RepairUIShipPartItem : ScrollListItem<RepairItem>
	{
		public Graphic ConditionGraphic;

		public Slider ConditionSlider;

		public Text Label;

		public RepairUI RepairUI;

		public override void Refresh()
		{
			base.Refresh();
			if (RepairUI == null)
			{
				RepairUI = UnityObjectHelper.FindInParentsOrSelf<RepairUI>(gameObject);
			}
			if (ConditionSlider != null && RepairUI.DockUI.PlayerCurrentUnit != null)
			{
				Label.text = RepairUI.GetComponentPartName(Item.Component);
				ConditionSlider.value = RepairUI.GetCurrentComponentCondition(RepairUI.DockUI.PlayerCurrentUnit, Item.Component);
				if (ConditionGraphic != null)
				{
					ConditionGraphic.color = RepairUI.DockUI.Engine.GetHullColor(ConditionSlider.value);
				}
			}
		}
	}
}
