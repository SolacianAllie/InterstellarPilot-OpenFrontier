using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.SectorPositionContextMenu
{
	public class SectorPositionContextScreen : ScreenBase
	{
		public Button SetWaypointButton;

		public Button OrderShipMoveHereButton;

		public SectorTarget SectorTarget;

		public TextMeshProUGUI TitleLabel;

		protected override void awake()
		{
			base.awake();
			SetWaypointButton.onClick.AddListener(SetWaypointButtonClick);
			OrderShipMoveHereButton.onClick.AddListener(OrderShipMoveHereButtonClick);
		}

		protected override void update()
		{
			base.update();
			OrderShipMoveHereButton.interactable = OrdersHelper.CanPlayerOrderUnit(EngineASX.Instance.PlayerUnit);
		}

		protected override void refresh()
		{
			base.refresh();
			TitleLabel.text = TextFormattingHelper.FormatSectorAndPosition(SectorTarget.Sector, SectorTarget.SectorPosition);
		}

		private void OrderShipMoveHereButtonClick()
		{
			OrdersHelper.OrderMoveToSectorTarget(OrdersHelper.FindOrCreateNpcAndFleetAndPilotShip(EngineASX.Instance.PlayerUnit), SectorTarget, stack: false);
			NavigateBack();
		}

		private void SetWaypointButtonClick()
		{
			EngineASX.Instance.LocalPlayer.SetCustomWaypointToSectorPosition(SectorTarget.Sector, SectorTarget.SectorPosition, autoRemove: true);
			NavigateBack();
		}
	}
}
