using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace OpenFrontier
{
	/// <summary>
	/// Transitional shim: legacy-shaped input API implemented on the new Input System.
	/// Lets decompiled code keep its original call shape (Input.GetKey etc.) via
	/// 'using Input = OpenFrontier.LegacyInput;' while the backend is UnityEngine.InputSystem.
	/// </summary>
	public static class LegacyInput
	{
		// Mirrors UnityEngine.Input semantics: on touch devices mousePosition
		// follows the primary finger - Mouse.current is null (or stale) on
		// phones, so without this every tap reads as (0,0) and touch hit-tests
		// (sector map selection, HUD tap-targeting) break.
		public static Vector3 mousePosition
		{
			get
			{
				Touchscreen touchscreen = Touchscreen.current;
				if (touchscreen != null)
				{
					foreach (TouchControl touch in touchscreen.touches)
					{
						if (touch.press.isPressed)
						{
							return touch.position.ReadValue();
						}
					}
					// uGUI fires clicks on release, after the touch has ended;
					// the primary touch control keeps its last position. Only
					// prefer it when the mouse has nothing better to report.
					if (Mouse.current == null || Mouse.current.position.ReadValue() == Vector2.zero)
					{
						Vector2 vector = touchscreen.primaryTouch.position.ReadValue();
						if (vector != Vector2.zero)
						{
							return vector;
						}
					}
				}
				return Mouse.current?.position.ReadValue() ?? Vector3.zero;
			}
		}

		public static Vector2 mouseScrollDelta => Mouse.current?.scroll.ReadValue() ?? Vector2.zero;

		public static Vector3 acceleration => Accelerometer.current?.acceleration.ReadValue() ?? Vector3.zero;

		// Active touch count (Touchscreen.touches.Count is the fixed capacity
		// of the touch array, not the number of fingers down).
		public static int touchCount
		{
			get
			{
				Touchscreen touchscreen = Touchscreen.current;
				if (touchscreen == null)
				{
					return 0;
				}
				int num = 0;
				foreach (TouchControl touch in touchscreen.touches)
				{
					if (touch.press.isPressed)
					{
						num++;
					}
				}
				return num;
			}
		}

		public static bool GetMouseButton(int button) => MouseButton(button)?.isPressed ?? false;

		public static bool GetMouseButtonDown(int button) => MouseButton(button)?.wasPressedThisFrame ?? false;

		public static bool GetMouseButtonUp(int button) => MouseButton(button)?.wasReleasedThisFrame ?? false;

		private static ButtonControl MouseButton(int index)
		{
			var mouse = Mouse.current;
			if (mouse == null)
			{
				return null;
			}
			return index switch
			{
				0 => mouse.leftButton,
				1 => mouse.rightButton,
				2 => mouse.middleButton,
				3 => mouse.forwardButton,
				4 => mouse.backButton,
				_ => null,
			};
		}

		public static bool GetKey(KeyCode code) => KeyControl(code)?.isPressed ?? false;

		public static bool GetKeyDown(KeyCode code) => KeyControl(code)?.wasPressedThisFrame ?? false;

		public static bool GetKeyUp(KeyCode code) => KeyControl(code)?.wasReleasedThisFrame ?? false;

		private static KeyControl KeyControl(KeyCode code)
		{
			var keyboard = Keyboard.current;
			if (keyboard == null)
			{
				return null;
			}
			return keyboard[ToKey(code)];
		}

		private static Key ToKey(KeyCode code)
		{
			// KeyCode and InputSystem.Key names align for most keys; a few differ.
			switch (code)
			{
				case KeyCode.Return: return Key.Enter;
				case KeyCode.KeypadEnter: return Key.NumpadEnter;
				case KeyCode.LeftControl: return Key.LeftCtrl;
				case KeyCode.RightControl: return Key.RightCtrl;
				case KeyCode.LeftApple: return Key.LeftApple;
				case KeyCode.RightApple: return Key.RightApple;
				case KeyCode.Alpha0: return Key.Digit0;
				case KeyCode.Alpha1: return Key.Digit1;
				case KeyCode.Alpha2: return Key.Digit2;
				case KeyCode.Alpha3: return Key.Digit3;
				case KeyCode.Alpha4: return Key.Digit4;
				case KeyCode.Alpha5: return Key.Digit5;
				case KeyCode.Alpha6: return Key.Digit6;
				case KeyCode.Alpha7: return Key.Digit7;
				case KeyCode.Alpha8: return Key.Digit8;
				case KeyCode.Alpha9: return Key.Digit9;
				case KeyCode.BackQuote: return Key.Backquote;
				case KeyCode.CapsLock: return Key.CapsLock;
				case KeyCode.ScrollLock: return Key.ScrollLock;
				case KeyCode.Numlock: return Key.NumLock;
				case KeyCode.Print: return Key.PrintScreen;
				case KeyCode.SysReq: return Key.PrintScreen;
				case KeyCode.Menu: return Key.ContextMenu;
				case KeyCode.Exclaim: return Key.Digit1;
				case KeyCode.At: return Key.Digit2;
				case KeyCode.Hash: return Key.Digit3;
				case KeyCode.Dollar: return Key.Digit4;
				case KeyCode.Percent: return Key.Digit5;
				case KeyCode.Caret: return Key.Digit6;
				case KeyCode.Ampersand: return Key.Digit7;
				case KeyCode.Asterisk: return Key.Digit8;
				case KeyCode.LeftParen: return Key.Digit9;
				case KeyCode.RightParen: return Key.Digit0;
				default:
					if (Enum.TryParse(code.ToString(), ignoreCase: true, out Key parsed))
					{
						return parsed;
					}
					return Key.None;
			}
		}

		public static float GetAxis(string axisName)
		{
			var keyboard = Keyboard.current;
			switch (axisName)
			{
				case "Mouse ScrollWheel":
					return Mouse.current?.scroll.ReadValue().y ?? 0f;
				case "Horizontal":
					float h = 0f;
					if (keyboard != null)
					{
						if (keyboard[Key.A].isPressed || keyboard[Key.LeftArrow].isPressed) h -= 1f;
						if (keyboard[Key.D].isPressed || keyboard[Key.RightArrow].isPressed) h += 1f;
					}
					return h;
				case "Vertical":
					float v = 0f;
					if (keyboard != null)
					{
						if (keyboard[Key.S].isPressed || keyboard[Key.DownArrow].isPressed) v -= 1f;
						if (keyboard[Key.W].isPressed || keyboard[Key.UpArrow].isPressed) v += 1f;
					}
					return v;
				default:
					return 0f;
			}
		}

		public static bool GetButtonDown(string buttonName)
		{
			// Legacy InputManager "Fire1": left ctrl or mouse 0
			if (buttonName == "Fire1")
			{
				return GetKeyDown(KeyCode.LeftControl) || GetMouseButtonDown(0);
			}
			return false;
		}

		public static bool GetButton(string buttonName)
		{
			if (buttonName == "Fire1")
			{
				return GetKey(KeyCode.LeftControl) || GetMouseButton(0);
			}
			return false;
		}
	}
}
