using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapScanRangeItem : SectorMapItem
	{
		public Unit Unit;

		protected override Vector2? GetSize()
		{
			float num = Unit.Components.ScanRange + GameController.Instance.GameSettings.HudSettings.SectorMapScanRangeRange;
			if (Unit.UnitGasCloud != null)
			{
				num *= Unit.UnitGasCloud.GasCloudClass.DetectionRangeMultiplier;
			}
			return new Vector2(num * 2f, num * 2f);
		}

		protected override Vector3 GetWorldPosition()
		{
			return Unit.transform.position;
		}

		protected override bool ShouldShowSprite()
		{
			if (Unit != null)
			{
				return SectorMap.ShouldShowScanRangeItemForUnit(Unit);
			}
			return false;
		}
	}
}
