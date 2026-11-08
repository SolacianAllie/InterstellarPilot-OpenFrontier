using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapScanRangeItem : SectorMapItem
	{
		public Unit Unit;

		protected override Vector2? GetSize()
		{
			// This unit's own visual range rather than its scanner reach plus a
			// flat pad, so the circle drawn on the map is the range inside which
			// everything is visible - because detection is now driven by the
			// target's radar cross section, and the smallest thing worth seeing
			// sets the edge.
			//
			// Before, this was Components.ScanRange plus a 1500 pad from the HUD
			// settings, which was a decorative number that happened to be about
			// the size of the answer.
			float num = Unit.VisualRange;
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
