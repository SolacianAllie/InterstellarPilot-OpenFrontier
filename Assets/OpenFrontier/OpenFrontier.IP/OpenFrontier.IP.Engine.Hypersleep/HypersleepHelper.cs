using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.Hypersleep;
using OpenFrontier.IP.UI.Screens.MessageBox;

namespace OpenFrontier.IP.Engine.Hypersleep
{
	public static class HypersleepHelper
	{
		private static object MessageBox;

		public static bool ShouldShowHypersleepOption()
		{
			return EngineASX.Instance.World.Permissions.AllowHypersleep;
		}

		public static bool IsHypersleepAvailable(out string reason)
		{
			reason = null;

			// Checked here as well as where the button is hidden. Hiding the
			// control is only the first line: this is the check that actually
			// stands between the player and a shared world being fast-forwarded
			// underneath everyone else, and a screen already open would otherwise
			// sail past it.
			if (!ShouldShowHypersleepOption())
			{
				reason = "Hypersleep is not available in a multiplayer session";
				return false;
			}

			Unit localRootUnit = EngineASX.Instance.LocalRootUnit;
			if (localRootUnit.IsUnderAttack())
			{
				reason = "Cannot enter hypersleep when current " + (localRootUnit.IsShip() ? "ship" : "station") + " is under attack";
				return false;
			}
			return true;
		}

		public static void TryEnterHypersleep()
		{
			if (!IsHypersleepAvailable(out var reason))
			{
				UIController.Instance.ShowMessageBox(reason, MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
			else
			{
				UIController.Instance.ScreenNavigator.ShowHypersleepScreen();
			}
		}

		public static bool IsHypersleepActive()
		{
			return UIController.Instance.ScreenNavigator.CurrentScreen is HypersleepScreen;
		}
	}
}
