using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class FollowMeOrderButton : FollowOrderButton
	{
		protected override Unit GetTargetUnit()
		{
			return EngineASX.Instance.PlayerUnit;
		}
	}
}
