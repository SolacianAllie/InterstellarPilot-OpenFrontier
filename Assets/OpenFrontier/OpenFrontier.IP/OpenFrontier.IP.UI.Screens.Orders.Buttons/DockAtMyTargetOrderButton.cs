using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class DockAtMyTargetOrderButton : DockOrderButton
	{
		protected override Unit GetTargetUnit()
		{
			return HudTarget;
		}
	}
}
