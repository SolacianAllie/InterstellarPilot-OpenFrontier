using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class HudTargetUI : HudTargetUIBase
	{
		public Image BracketsWidget;

		public Image UnitIconSprite;

		public const float IconSpriteDrawDistance = 300f;

		public void RefreshBracketsColor()
		{
			BracketsWidget.color = CalculateBracketsColor(unit);
		}

		protected override void awake()
		{
			base.awake();
			UnitIconSprite.enabled = false;
		}

		protected override void OnNewUnitSet()
		{
			base.OnNewUnitSet();
			RefreshIconSprite();
		}

		private void RefreshIconSprite()
		{
			bool flag = (unit.UnitType == UnitType.Station || unit.UnitType == UnitType.Projectile) && unit.ActiveUnit != null && unit.ActiveUnit.LastDistanceFromCamera > 300f;
			UnitIconSprite.enabled = flag;
			if (flag)
			{
				UnitIconSprite.sprite = EngineASX.Instance.EngineResources.GetUnitClassThumbnailIconSpriteOrDefault(unit.UnitClass);
			}
		}

		public override void RefreshIconVisibility()
		{
			base.RefreshIconVisibility();
			RefreshIconSprite();
		}
	}
}
