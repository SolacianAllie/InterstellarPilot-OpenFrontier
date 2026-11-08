using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class FollowMyTargetOrderButton : FollowOrderButton
	{
		protected override Unit GetTargetUnit()
		{
			return HudTarget;
		}
	}
}
