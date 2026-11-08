using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class DockAtMyWaypointOrderButton : DockOrderButton
	{
		protected override Unit GetTargetUnit()
		{
			return WaypointTargetUnit;
		}
	}
}
