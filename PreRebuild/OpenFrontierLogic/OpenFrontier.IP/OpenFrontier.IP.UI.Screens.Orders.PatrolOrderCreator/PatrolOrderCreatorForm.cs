using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Orders.PatrolOrderCreator
{
	public class PatrolOrderCreatorForm : MonoBehaviour
	{
		public PatrolOrderCreatorList PatrolOrderCreatorList;

		public Button ConfirmButton;

		public Toggle IsLoopToggle;

		public Toggle RepeatToggle;

		public void Refresh()
		{
			PatrolOrderCreatorList.Refresh();
		}
	}
}
