using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Mirrors a <see cref="Selectable"/> (a Button, Toggle, ...) state machine onto
	/// the color of a text label. Exposes the same Colors block the Button itself
	/// has - Normal / Highlighted / Pressed / Selected / Disabled, a multiplier and
	/// the fade duration - via Unity's own <see cref="ColorBlock"/>, so the tint
	/// authoring looks identical to the Button's.
	///
	/// Put it on the same GameObject as the Button; Target Graphic falls back to
	/// the first Text child. It is typed as <see cref="Graphic"/> so a TextMeshPro
	/// label swats in later without a script change.
	///
	/// Why a component instead of Button's built-in Transition: Button's ColorTint
	/// only cross-fades its one Target Graphic (the background Image), so the label
	/// cannot respond to states without either an Animator (five clips per style)
	/// or this. Deliberately a small enhancement, per the UI description: Unity
	/// native first, custom scripting only where it earns its place.
	///
	/// State resolution matches Selectable's priority: Disabled beats everything
	/// (the polled <c>interactable</c> check), then Pressed, Highlighted, Selected,
	/// Normal. Reacts to events, not per-frame simulation: the only Update work is
	/// a single cached bool compare (the house pattern from MainMenuScreen.update,
	/// which throttles its poll similarly) catching code that flips
	/// <c>interactable</c> without touching this - permission gating, "no save
	/// found", etc.
	///
	/// Fades run with <c>ignoreTimeScale: true</c>: behind any PauseWhenShown
	/// screen timeScale is 0, and a scaled fade would hang mid-transition exactly
	/// where the menu is shown (same reasoning as DockUIHeader's unscaled clock).
	/// </summary>
	[RequireComponent(typeof(Selectable))]
	public class SelectableTextColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
	{
		[Tooltip("The label whose color follows the states. Empty finds the first Text child of this button.")]
		public Graphic Target;

		[Tooltip("Label color per state. Same fields as the Button's own Colors block.")]
		public ColorBlock TextColors = ColorBlock.defaultColorBlock;

		/// <summary>
		/// Mirror of Selectable.SelectionState, which is protected - this component
		/// sits outside Selectable, so it carries its own copy of the five states.
		/// </summary>
		private enum LabelState
		{
			Normal,
			Highlighted,
			Pressed,
			Selected,
			Disabled,
		}

		private Selectable selectable;

		private bool pointerInside;

		private bool pointerDown;

		private bool isSelected;

		private bool cachedInteractable = true;

		private void Awake()
		{
			selectable = GetComponent<Selectable>();

			if (Target == null)
			{
				Target = GetComponentInChildren<Text>();
			}

			if (Target == null)
			{
				// TMPro label - TextMeshProUGUI derives from MaskableGraphic, so it
				// fits the same Target	slot; the color-tint API is identical.
				Target = GetComponentInChildren<TMP_Text>();
			}
		}

		private void OnEnable()
		{
			// Re-assert the current state instantly: no fade-in from whatever color
			// the label was last left with.
			cachedInteractable = selectable == null || selectable.interactable;
			Apply(ComputeState(), instant: true);
		}

		private void Update()
		{
			// Not a state poll - state transitions arrive as events immediately.
			// This only catches a script flipping Selectable.interactable, which has
			// no callback. One cached bool compare, cheaper than anything else on
			// this screen.
			bool interactable = selectable == null || selectable.interactable;

			if (interactable != cachedInteractable)
			{
				cachedInteractable = interactable;
				Apply(ComputeState(), instant: false);
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			pointerInside = true;
			Apply(ComputeState(), instant: false);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			pointerInside = false;
			Apply(ComputeState(), instant: false);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			pointerDown = true;
			Apply(ComputeState(), instant: false);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			pointerDown = false;
			// After release under the cursor the label should read highlighted (or
			// selected), which ComputeState already resolves from the remaining
			// flags.
			Apply(ComputeState(), instant: false);
		}

		public void OnSelect(BaseEventData eventData)
		{
			isSelected = true;
			Apply(ComputeState(), instant: false);
		}

		public void OnDeselect(BaseEventData eventData)
		{
			isSelected = false;
			Apply(ComputeState(), instant: false);
		}

		private LabelState ComputeState()
		{
			// Priority order mirrors Selectable's own transition rules.
			if (selectable == null || !selectable.interactable)
			{
				return LabelState.Disabled;
			}

			if (pointerDown)
			{
				// Clicks cancelled by dragging off still read as pressed while the
				// finger stays down; seeing "still held" is the point of the tint.
				return LabelState.Pressed;
			}

			if (pointerInside)
			{
				return LabelState.Highlighted;
			}

			if (isSelected)
			{
				// Keyboard/gamepad focus lives here once the pointer has left.
				return LabelState.Selected;
			}

			return LabelState.Normal;
		}

		private void Apply(LabelState state, bool instant)
		{
			if (Target == null)
			{
				return;
			}

			Color color;

			switch (state)
			{
				case LabelState.Disabled:
					color = TextColors.disabledColor;
					break;
				case LabelState.Pressed:
					color = TextColors.pressedColor;
					break;
				case LabelState.Highlighted:
					color = TextColors.highlightedColor;
					break;
				case LabelState.Selected:
					color = TextColors.selectedColor;
					break;
				default:
					color = TextColors.normalColor;
					break;
			}

			// Same knob the Button's Colors block exposes: brightest the tint can
			// push. Multiplier > 1 lets a dark base color land on an oversaturated
			// press tint; clamped per channel like uGUI does.
			color.r = Mathf.Clamp01(color.r * TextColors.colorMultiplier);
			color.g = Mathf.Clamp01(color.g * TextColors.colorMultiplier);
			color.b = Mathf.Clamp01(color.b * TextColors.colorMultiplier);
			color.a = Mathf.Clamp01(color.a * TextColors.colorMultiplier);

			if (instant)
			{
				Target.canvasRenderer.SetColor(color);
			}
			else
			{
				Target.CrossFadeColor(color, TextColors.fadeDuration, true, true);
			}
		}
	}
}
