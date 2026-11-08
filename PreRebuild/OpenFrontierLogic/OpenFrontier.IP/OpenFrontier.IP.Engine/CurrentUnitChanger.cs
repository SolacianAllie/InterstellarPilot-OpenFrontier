using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class CurrentUnitChanger : MonoBehaviour
	{
		public void ChangeUnit(Person player, Unit unit)
		{
			if (unit.Sector.IsActive)
			{
				float timeScale = Time.timeScale;
				ScreenBase currentScreen = UIController.Instance.ScreenNavigator.CurrentScreen;
				if (currentScreen != null && currentScreen.previousTimeScale.HasValue)
				{
					timeScale = currentScreen.previousTimeScale.Value;
				}
				UIController.Instance.ScreenNavigator.RemoveAllInStack(null);
				Time.timeScale = 0f;
				ChangeUnitInternal(player, unit);
				Time.timeScale = timeScale;
			}
			else
			{
				ChangeUnitToInactiveSector(player, unit);
			}
		}

		private void ChangeUnitToInactiveSector(Person player, Unit unit)
		{
			EngineASX engine = player.Engine;
			float timeScale = Time.timeScale;
			ScreenBase currentScreen = UIController.Instance.ScreenNavigator.CurrentScreen;
			if (currentScreen != null && currentScreen.previousTimeScale.HasValue)
			{
				timeScale = currentScreen.previousTimeScale.Value;
			}
			Time.timeScale = 0f;
			engine.ActiveSector = null;
			UIController.Instance.ScreenNavigator.RemoveAllInStack(null);
			UIController.Instance.QuickMsg.ClearMessages();
			ChangeUnitInternal(player, unit);
			if (engine.ActiveSector != null)
			{
				engine.ActiveSector.SeparateOverlappingShips();
			}
			Time.timeScale = timeScale;
		}

		private static void ChangeUnitInternal(Person player, Unit unit)
		{
			UIController.Instance.QuickMsg.ClearMessages();
			EngineASX engine = player.Engine;
			engine.CreateIntroFader();
			if (unit.UnitClass.IsPilottable)
			{
				if (!OrdersHelper.IsPilottedByNpc(unit))
				{
					unit.Components.PilotPerson = player;
				}
				else
				{
					player.CurrentUnit = unit;
				}
			}
			else
			{
				player.CurrentUnit = unit;
			}
			engine.SetUIFromPlayerStatus();
			if (engine.HudCamera.isActiveAndEnabled)
			{
				engine.SnapHudCameraToTarget();
			}
		}
	}
}
