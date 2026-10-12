using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapScreen : EngineScreen
	{
		public SectorMapForm SectorMapForm;

		public Button BackButton;

		public SectorMap SectorMap => SectorMapForm.SectorMap;

		public SectorMapZoomer Zoomer => SectorMapForm.Zoomer;

		protected override void awake()
		{
			base.awake();
			if (BackButton != null)
			{
				BackButton.onClick.AddListener(BackButtonClick);
			}
		}

		private void BackButtonClick()
		{
			NavigateBack();
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			SectorMapForm.SectorMap.OnShown();
		}

		protected override void update()
		{
			base.update();
			RefreshBackButtonVisible();
			SectorMapForm.Tick();
		}

		private void RefreshBackButtonVisible()
		{
			if (BackButton != null)
			{
				BackButton.gameObject.SetActive(GetBackTarget() != null);
			}
		}

		protected override void refresh()
		{
			base.refresh();
			RefreshBackButtonVisible();
			SectorMapForm.Refresh();
		}

		public void RestrictNavigationAway()
		{
			RestrictNavigationAwayEnabled = true;
			SectorMapForm.StaticTargetInfoTransform.gameObject.SetActive(value: true);
			ShowDockHeader = false;
			SectorMapForm.SectorMap.SectorMapCurrentTargetController.EnabledSelectedUnitContextMenu = false;
		}
	}
}
