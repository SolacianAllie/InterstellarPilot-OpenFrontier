using OpenFrontier.IP.UI.Screens.NewFleetOrder;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public interface IOrderButton
	{
		NewOrderTarget OrderTarget { get; set; }

		Button Button { get; }

		bool CanStack { get; }

		void Refresh();
	}
}
