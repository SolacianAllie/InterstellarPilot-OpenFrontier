using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class UnitConditionControllerStatic : MonoBehaviour
	{
		private bool localUnitHasShields;

		public Image HullWidget;

		[SerializeField]
		private Unit localUnit;

		public Image[] ShieldBarWidgets = new Image[6];

		public Unit LocalUnit
		{
			get
			{
				return localUnit;
			}
			set
			{
				if (!(localUnit != value))
				{
					return;
				}
				localUnit = value;
				if (localUnit != null && localUnit.IsValid)
				{
					HullWidget.sprite = EngineASX.Instance.EngineResources.GetUnitThumbnailSpriteOrCargoClassSprite(localUnit);
					localUnitHasShields = LocalUnit.Components != null && localUnit.Components.ShieldComponent != null;
					RefreshShieldWidgetsActive();
					if (localUnitHasShields)
					{
						SetShieldWidgetsColor();
					}
					HullWidget.color = GetHullWidgetColor();
				}
			}
		}

		private void Awake()
		{
		}

		public Color GetHullWidgetColor()
		{
			if (localUnit.UnitClass.ApplyThumbnailHullColor)
			{
				return LocalUnit.Engine.GetUnitHullColor(LocalUnit);
			}
			if (localUnit.UnitClass.DisplayData != null && localUnit.UnitClass.DisplayData.ForceThumbnailIconSpriteColor)
			{
				return localUnit.UnitClass.DisplayData.ThumbnailIconSpriteColor;
			}
			return Color.white;
		}

		public void RefreshShieldWidgetsActive()
		{
			SetShieldWidgetsEnabled(localUnitHasShields);
		}

		private void SetShieldWidgetsEnabled(bool enabled)
		{
			for (int i = 0; i < 6; i++)
			{
				if (i < ShieldBarWidgets.Length && ShieldBarWidgets[i] != null)
				{
					ShieldBarWidgets[i].enabled = enabled;
				}
			}
		}

		private void SetShieldWidgetsColor()
		{
			for (int i = 0; i < 6; i++)
			{
				SetShieldWidgetColor(i);
			}
		}

		private void SetShieldWidgetColor(int index)
		{
			ShieldBarWidgets[index].color = localUnit.Engine.GetUnitShieldColor(localUnit, index);
		}
	}
}
