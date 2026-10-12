using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class CargoClassLink : MonoBehaviour
	{
		public CargoClass CargoClass;

		private void OnClick()
		{
			if (CargoClass != null)
			{
				UIController.Instance.ScreenNavigator.ShowCargoInfoScreen(CargoClass);
			}
		}
	}
}
