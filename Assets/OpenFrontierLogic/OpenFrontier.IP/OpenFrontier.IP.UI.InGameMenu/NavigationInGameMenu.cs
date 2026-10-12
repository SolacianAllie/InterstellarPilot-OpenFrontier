using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.InGameMenu
{
	public class NavigationInGameMenu : MonoBehaviour
	{
		public Button ClearWaypointButton;

		public HudScreen Hud;

		private void Awake()
		{
			ClearWaypointButton.onClick.AddListener(ClearWaypointButtonClick);
		}

		private void Update()
		{
			ClearWaypointButton.interactable = Hud != null && Hud.Eng.LocalPlayer != null && Hud.Eng.LocalPlayer.HasCustomWaypoint;
		}

		private void ClearWaypointButtonClick()
		{
			Hud.Eng.LocalPlayer.ClearCustomWaypoint();
		}
	}
}
