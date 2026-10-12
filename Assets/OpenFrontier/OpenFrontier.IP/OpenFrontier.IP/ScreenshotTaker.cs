using UnityEngine;

namespace OpenFrontier.IP
{
	public class ScreenshotTaker : MonoBehaviour
	{
		public int Counter;

		public KeyCode InputKey = KeyCode.F1;

		public string NamePrefix = "screenshot_";

		public const string DefaultNamePrefix = "screenshot_";

		private static string postFix = ".png";

		public string TargetPath = "C:\\dev\\temp\\asx\\screenshots";

		public const string DefaultPath = "C:\\dev\\temp\\asx\\screenshots";
	}
}
