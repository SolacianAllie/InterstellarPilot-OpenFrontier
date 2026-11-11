using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace OpenFrontier
{
	/// <summary>
	/// The per-frame watcher behind <see cref="ActiveInput"/>: a handful of
	/// input-state reads per frame, no allocations, no native calls beyond
	/// state access - a few nanoseconds against the frame budget, and the
	/// only way to know which device the player actually touched last.
	///
	/// Policy, not just detection: pointer input (touch, mouse) and typing
	/// CLEAR whatever gamepad-style focus exists, because that focus is
	/// meaningless the moment another scheme is in use - "swap back upon any
	/// of those input methods being used". Keyboard NAVIGATION keys are the
	/// one exception: arrows/WASD/Enter/Space drive uGUI selection
	/// themselves, so clearing on those would fight the very navigation they
	/// perform (and eat Submit clicks). Input fields holding focus are never
	/// cleared either - editing text IS keyboard use. Establishing focus is
	/// NOT this component's job: the menu code lights its default when the
	/// scheme flips to Gamepad and on held navigation input, so the flip
	/// lands after the module has already run the frame's Submit/Cancel
	/// against an empty selection.
	/// </summary>
	public class InputSchemeTracker : MonoBehaviour
	{
		private void Update()
		{
			// Touch first: on devices that synthesize a mouse from touch, a
			// touch frame would otherwise flip the scheme to KeyboardMouse.
			Touchscreen touchscreen = Touchscreen.current;

			if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
			{
				ActiveInput.Set(InputScheme.Touch);
				Deselect();
				return;
			}

			Mouse mouse = Mouse.current;

			if (mouse != null
				&& (mouse.delta.ReadValue() != Vector2.zero
					|| mouse.scroll.ReadValue() != Vector2.zero
					|| mouse.leftButton.wasPressedThisFrame
					|| mouse.rightButton.wasPressedThisFrame
					|| mouse.middleButton.wasPressedThisFrame))
			{
				ActiveInput.Set(InputScheme.KeyboardMouse);
				Deselect();
				return;
			}

			Keyboard keyboard = Keyboard.current;

			if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
			{
				ActiveInput.Set(InputScheme.KeyboardMouse);

				if (!NavigationKeysPressed())
				{
					Deselect();
				}

				return;
			}

			Gamepad gamepad = Gamepad.current;

			if (gamepad != null
				&& (gamepad.dpad.ReadValue().sqrMagnitude > 0.04f
					|| gamepad.leftStick.ReadValue().sqrMagnitude > 0.04f
					|| gamepad.rightStick.ReadValue().sqrMagnitude > 0.04f
					|| gamepad.anyButton.wasPressedThisFrame))
			{
				ActiveInput.Set(InputScheme.Gamepad);
			}
		}

		private void Deselect()
		{
			EventSystem eventSystem = EventSystem.current;

			if (eventSystem == null)
			{
				return;
			}

			GameObject selected = eventSystem.currentSelectedGameObject;

			if (selected == null || selected.GetComponent<TMP_InputField>() != null)
			{
				return;
			}

			eventSystem.SetSelectedGameObject(null);
		}

		/// <summary>
		/// Was any of the keys the UI module's Navigate/Submit actions are
		/// built from pressed this frame? Only called on frames where some
		/// key was pressed at all.
		/// </summary>
		private static bool NavigationKeysPressed()
		{
			Keyboard keyboard = Keyboard.current;

			return keyboard.leftArrowKey.wasPressedThisFrame
				|| keyboard.rightArrowKey.wasPressedThisFrame
				|| keyboard.upArrowKey.wasPressedThisFrame
				|| keyboard.downArrowKey.wasPressedThisFrame
				|| keyboard.wKey.wasPressedThisFrame
				|| keyboard.aKey.wasPressedThisFrame
				|| keyboard.sKey.wasPressedThisFrame
				|| keyboard.dKey.wasPressedThisFrame
				|| keyboard.enterKey.wasPressedThisFrame
				|| keyboard.numpadEnterKey.wasPressedThisFrame
				|| keyboard.spaceKey.wasPressedThisFrame;
		}
	}
}
