using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Hud
{
	public struct HudScannerUnit
	{
		public Unit Unit;

		public float DistanceFromLocalUnitIgnoringY;

		public HudScannerUnit(Unit unit, float distance)
		{
			this = default;
			Unit = unit;
			DistanceFromLocalUnitIgnoringY = distance;
		}
	}
}
