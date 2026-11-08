using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class AutoSaveSettings : MonoBehaviour
	{
		public bool AutoSaveAfterDuration = true;

		public string AutoSaveAfterDurationKey = "auto_save_after_duration";

		public int AutoSaveAfterDurationMinutes = 15;

		public string AutoSaveAfterDurationMinutesKey = "auto_save_after_duration_minutes";

		public bool AutoSaveOnDocking = true;

		public string AutoSaveOnDockingKey = "auto_save_on_docking";

		public bool AutoSaveOnWormholeExit = true;

		public string AutoSaveOnWormholeExitKey = "auto_save_on_wormhole_exit";
	}
}
