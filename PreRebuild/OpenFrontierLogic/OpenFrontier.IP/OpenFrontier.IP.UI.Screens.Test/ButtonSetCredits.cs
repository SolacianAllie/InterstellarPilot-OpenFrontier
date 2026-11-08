using OpenFrontier.IP.Testing;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class ButtonSetCredits : MonoBehaviour
	{
		private Button button;

		public int Credits = 1000000;

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(OnClick);
		}

		public void OnClick()
		{
			TestHelper.SetCredits(Credits);
		}
	}
}
