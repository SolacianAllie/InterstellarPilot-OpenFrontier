using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class ButtonChangeCredits : MonoBehaviour
	{
		public int Change = 1000000;

		private Button button;

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(OnClick);
		}

		public void OnClick()
		{
			EngineASX instance = EngineASX.Instance;
			if (instance != null && instance.LocalPlayer != null)
			{
				instance.AddCreditsToPlayerFactionWithMsg(Change, FactionTransactionType.Gift);
			}
		}
	}
}
