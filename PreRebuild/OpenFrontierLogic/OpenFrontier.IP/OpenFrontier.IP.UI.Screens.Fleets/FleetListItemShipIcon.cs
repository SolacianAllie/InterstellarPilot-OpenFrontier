using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Fleets
{
	public class FleetListItemShipIcon : MonoBehaviour
	{
		public Image ShipImage;

		public UnitConditionControllerUI ConditionControllerUI;

		private Unit unit;

		public void SetUnit(Unit unit)
		{
			if (unit != this.unit)
			{
				this.unit = unit;
				ConditionControllerUI.LocalUnit = this.unit;
				ShipImage.sprite = this.unit.GetIconSprite();
			}
		}

		public void Refresh()
		{
			bool active = unit.IsShieldDamaged() || unit.IsHullDamaged();
			ConditionControllerUI.gameObject.SetActive(active);
		}
	}
}
