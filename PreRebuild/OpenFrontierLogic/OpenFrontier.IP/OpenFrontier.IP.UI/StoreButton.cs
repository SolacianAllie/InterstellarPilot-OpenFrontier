using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class StoreButton : MonoBehaviour
	{
		public void OnClick()
		{
			Debug.Log("Show store", this);
			UIController.Instance.ScreenNavigator.ShowStoreScreen();
		}
	}
}
