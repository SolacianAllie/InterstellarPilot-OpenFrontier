using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios
{
	public class WinScenario : WorldBase
	{
		public float ChangeUnitTime = 8f;

		private float lastChangeUnitTime;

		private Unit spectateUnit;

		public ScreenBase WinUI;

		protected override void OnNewGame()
		{
			base.OnNewGame();
			ChangeUnit();
			Engine.StartCinematic(allowTogglePause: false);
			Engine.CinematicScreen.ClearMsgQueue();
			if (Engine.IntroFaderPrefab != null)
			{
				Object.Instantiate(Engine.IntroFaderPrefab.gameObject);
			}
			UIController.Instance.ScreenNavigator.NavigateToScreen(WinUI);
		}

		protected override void update()
		{
			base.update();
			if (Time.time > lastChangeUnitTime + ChangeUnitTime)
			{
				ChangeUnit();
			}
		}

		private void ChangeUnit()
		{
			Engine.CameraStartSpectateUnit(spectateUnit, allowFlyby: true, allowViewport: false, allowOrbit: true);
			lastChangeUnitTime = Time.time;
		}
	}
}
