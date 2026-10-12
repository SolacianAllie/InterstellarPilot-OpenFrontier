using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Testing;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class ButtonKillSectorHostilesAsPlayer : MonoBehaviour
	{
		private Button button;

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(OnClick);
		}

		public void OnClick()
		{
			TestHelper.KillActiveSectorHostiles(EngineASX.Instance.LocalFaction, EngineASX.Instance.LocalFaction);
		}
	}
}
