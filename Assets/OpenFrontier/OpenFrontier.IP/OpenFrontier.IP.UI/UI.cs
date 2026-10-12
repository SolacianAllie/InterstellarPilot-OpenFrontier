using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public static class UI
	{
		public const int buttonWidth = 400;

		public const int buttonHeight = 120;

		public const int screenBorder = 10;

		public const float preferredScreenWidth = 800f;

		public const float preferredScreenHeight = 480f;

		public const int UnityLeftButton = 0;

		public const int UnityMiddleButton = 2;

		public const int UnityRightButton = 1;

		public static bool IsMobileDevice
		{
			get
			{
				if (Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.Android)
				{
					return Application.platform == RuntimePlatform.MetroPlayerARM;
				}
				return true;
			}
		}

		public static bool TouchInputEnabled
		{
			get
			{
				if (GameController.Instance.ForceTouchInputEnabled)
				{
					return true;
				}
				if (Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.Android)
				{
					return Application.platform == RuntimePlatform.MetroPlayerARM;
				}
				return true;
			}
		}

		public static void QuitToMainMenu()
		{
			GameController.Instance.QuitToMainMenu();
		}
	}
}
