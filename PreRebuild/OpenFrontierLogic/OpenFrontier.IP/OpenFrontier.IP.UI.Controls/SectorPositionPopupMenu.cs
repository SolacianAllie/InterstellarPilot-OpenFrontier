using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	public class SectorPositionPopupMenu : MonoBehaviour
	{
		private PopupMenu popupMenu;

		public Button SetWaypointButton;

		public Button OrderShipMoveHereButton;

		public SectorTarget SectorTarget { get; set; }

		private void Awake()
		{
			popupMenu = GetComponent<PopupMenu>();
			popupMenu.Opening += PopupMenu_Opening;
			SetWaypointButton.onClick.AddListener(SetWaypointButtonClick);
			OrderShipMoveHereButton.onClick.AddListener(OrderShipMoveHereButtonClick);
		}

		private bool PopupMenu_Opening(PopupMenu sender)
		{
			RefreshButtons();
			return true;
		}

		private void RefreshButtons()
		{
			OrderShipMoveHereButton.gameObject.SetActive(OrdersHelper.CanPlayerOrderUnit(EngineASX.Instance.PlayerUnit));
		}

		private void OrderShipMoveHereButtonClick()
		{
			OrdersHelper.OrderMoveToSectorTarget(OrdersHelper.FindOrCreateNpcAndFleetAndPilotShip(EngineASX.Instance.PlayerUnit), SectorTarget, stack: false);
		}

		private void SetWaypointButtonClick()
		{
			EngineASX.Instance.LocalPlayer.SetCustomWaypointToSectorPosition(SectorTarget.Sector, SectorTarget.SectorPosition, autoRemove: true);
		}
	}
}
