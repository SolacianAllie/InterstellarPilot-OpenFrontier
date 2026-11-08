using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class InGameMenuBase : MonoBehaviour
	{
		private InGameMenuBase backMenu;

		public InGameMenuControllerUI MenuController;

		public bool AllowAutoClose = true;

		public Unit PlayerUnit => Hud.PlayerUnit;

		public HudScreen Hud => MenuController.Hud;

		public InGameMenuBase BackMenu
		{
			get
			{
				return backMenu;
			}
			set
			{
				backMenu = value;
			}
		}

		private void OnEnable()
		{
			MenuController.OpenNewMenu(this);
		}
	}
}
