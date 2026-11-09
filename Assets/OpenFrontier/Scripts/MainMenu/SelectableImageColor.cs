using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Twin of <see cref="SelectableTextColor"/> for IMAGE targets: mirrors a
	/// <see cref="Selectable"/> (a Button, Toggle, ...) state machine onto the
	/// color of embedded child Graphics - icons, bullets, separators that are
	/// painted differently from the label text, so they carry their OWN Colors
	/// block instead of riding the text one. Exposes the same five states
	/// (Normal / Highlighted / Pressed / Selected / Disabled), a multiplier and
	/// the fade duration via Unity's own <see cref="ColorBlock"/>; authoring
	/// looks identical to the Button's, just with image-appropriate values.
	///
	/// Deliberately a copy of the twin's state machine (event handlers plus the
	/// single-bool interactable poll), not a shared abstract base: two sibling
	/// components under the one-MonoBehaviour-per-file rule, and a state-machine
	/// change that rare can carry the twin-sync tax. KEEP THE TWO IN STEP when
	/// either is touched - the twin notes any behaviour divergence here.
	///
	/// No auto-fill, unlike the text twin: only explicitly-wired images are
	/// tinted. The Button's own Target Graphic must stay OUT of these slots -
	/// its built-in ColorTint already covers it, and this would double it.
	///
	/// GREYSCALE: with the Saturation shader assigned, each target image gets
	/// its own material instance and the state machine also drives a
	/// per-state desaturation value (1 = fully greyscale, 0 = full color; the
	/// ColorBlock tint then shades the grey). Stock uGUI cannot desaturate by
	/// tinting - the multiply only ever darkens - so one decorator shader
	/// earns its keep here. Defaults keep Normal/Disabled grey and the
	/// hands-on states colored. Saturation changes chase their target over
	/// the same fadeDuration as the color crossfade, unscaled, so the image
	/// does not pop while the label crossfades.
	///
	/// State resolution matches Selectable's priority: Disabled beats
	/// everything (the polled <c>interactable</c> check), then Pressed,
	/// Highlighted, Selected, Normal. Reacts to events, not per-frame
	/// simulation; the only Update work is one cached bool compare. Fades run
	/// with unscaled time so they survive a PauseWhenShown screen where
	/// timeScale reads 0.
	/// </summary>
	[RequireComponent(typeof(Selectable))]
	public class SelectableImageColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
	{
		[Tooltip("The image that follows the states - wire it explicitly; the Button's own Target Graphic stays unwired (its built-in ColorTint already covers it).")]
		public Graphic Target;

		[Tooltip("Extra images that follow the same states (multi-icon buttons wire each one here). Unwired images keep their authored colors; the tint multiplies, so static per-image colors stay intact.")]
		public List<Graphic> ExtraTargets = new List<Graphic>();

		[Tooltip("Image color per state. Same fields as the Button's own Colors block, authored for this button's icons rather than its labels.")]
		public ColorBlock ImageColors = ColorBlock.defaultColorBlock;

		[Tooltip("Optional: the OpenFrontier/UI Saturation shader. When set, each target image receives its own material instance and the Greyscale fields below drive its saturation per state. Leave empty for tint-only.")]
		public Shader SaturationShader;

		[Tooltip("Desaturation in the Normal state: 1 = fully greyscale (the tint color then shades it), 0 = original texture colors.")]
		[Range(0f, 1f)] public float GreyscaleNormal = 1f;

		[Tooltip("Desaturation in the Highlighted (hover) state.")]
		[Range(0f, 1f)] public float GreyscaleHighlighted;

		[Tooltip("Desaturation in the Pressed state.")]
		[Range(0f, 1f)] public float GreyscalePressed;

		[Tooltip("Desaturation in the Selected (keyboard/gamepad focus) state.")]
		[Range(0f, 1f)] public float GreyscaleSelected;

		[Tooltip("Desaturation while the button is uninteractable - the classic greyed-out look for a dead icon. Reads 1 (fully grey) by default.")]
		[Range(0f, 1f)] public float GreyscaleDisabled = 1f;

		/// <summary>
		/// Mirror of Selectable.SelectionState, which is protected - this component
		/// sits outside Selectable, so it carries its own copy of the five states.
		/// Same copy lives in SelectableTextColor; keep both in step with
		/// Selectable's real priority rules.
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
			PrepareGreyscale();
		}

		private void OnEnable()
		{
			// Re-assert the current state instantly: no fade-in from whatever color
			// the image was last left with.
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

			// Transient saturation chase: runs only while a value is still
			// converging, so the idle cost here is one bool check.
			ChaseSaturation();
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
			// After release under the cursor the image should read highlighted (or
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
			if (Target == null && ExtraTargets.Count == 0)
			{
				return;
			}

			Color color;
			switch (state)
			{
				case LabelState.Disabled:
					color = ImageColors.disabledColor;
					break;
				case LabelState.Pressed:
					color = ImageColors.pressedColor;
					break;
				case LabelState.Highlighted:
					color = ImageColors.highlightedColor;
					break;
				case LabelState.Selected:
					color = ImageColors.selectedColor;
					break;
				default:
					color = ImageColors.normalColor;
					break;
			}

			// Same knob the Button's Colors block exposes: brightest the tint can
			// push. Multiplier > 1 lets a dark base color land on an oversaturated
			// press tint; clamped per channel like uGUI does.
			color.r = Mathf.Clamp01(color.r * ImageColors.colorMultiplier);
			color.g = Mathf.Clamp01(color.g * ImageColors.colorMultiplier);
			color.b = Mathf.Clamp01(color.b * ImageColors.colorMultiplier);
			color.a = Mathf.Clamp01(color.a * ImageColors.colorMultiplier);

			ApplyTo(Target, color, instant);

			for (int i = 0; i < ExtraTargets.Count; i++)
			{
				// Wired into both lists is authoring shorthand for "primary" -
				// skipping keeps it from being double-faded per state change.
				if (ExtraTargets[i] == Target)
				{
					continue;
				}

				ApplyTo(ExtraTargets[i], color, instant);
			}

			if (SaturationShader == null)
			{
				return;
			}

			float grey = ComputeGreyscale(state);

			DriveSaturation(Target, grey, instant);

			for (int i = 0; i < ExtraTargets.Count; i++)
			{
				if (ExtraTargets[i] == Target)
				{
					continue;
				}

				DriveSaturation(ExtraTargets[i], grey, instant);
			}
		}

		private void ApplyTo(Graphic target, Color color, bool instant)
		{
			if (target == null)
			{
				// A null entry (deleted image, half-wired slot) must not error
				// out a state change for the targets that did make it.
				return;
			}

			if (instant)
			{
				target.canvasRenderer.SetColor(color);
			}
			else
			{
				target.CrossFadeColor(color, ImageColors.fadeDuration, true, true);
			}
		}

		// GREYSCALE ---------------------------------------------------------------
		// One material instance per image: two icons in different states (one
		// hovered, one normal) cannot share a material - the shared float would
		// re-tint them identically. The instances also stop uGUI from batching
		// these images together; that is the deliberate, menu-scale cost of
		// per-image desaturation. Never attach two of these components to the
		// same button: the second instance would capture the first as its
		// "original" and both would fight over the one slot.
		private class SaturationRig
		{
			public Material material;
			public Material original;
			public float current;
			public float target;
		}

		private readonly Dictionary<Graphic, SaturationRig> rigs = new Dictionary<Graphic, SaturationRig>();

		private bool chasing;

		private int saturationId;

		private int SaturationPropertyId
		{
			get
			{
				// Shader.PropertyToID is a native call, so it stays out of field
				// initializers and static constructors (the type-initialization
				// poisoning trap) and resolves lazily on first use.
				if (saturationId == 0)
				{
					saturationId = Shader.PropertyToID("_Saturation");
				}

				return saturationId;
			}
		}

		private void PrepareGreyscale()
		{
			if (SaturationShader == null)
			{
				// Tint-only component: the greyscale fields stay inert and the
				// Update chase stays idle. Not an error state - greyscale is
				// an optional feature.
				return;
			}

			Material probe = new Material(SaturationShader);

			if (!probe.HasProperty("_Saturation"))
			{
#if UNITY_EDITOR
				// Developer-only diagnostic; a release build keeps the inert
				// behaviour without shipping the warning.
				Debug.LogWarning($"[SelectableImageColor] {name}: SaturationShader has no _Saturation property - greyscale stays inert, tint-only.", this);
#endif
				Destroy(probe);
				SaturationShader = null;
				return;
			}

			Destroy(probe);
		}

		private float ComputeGreyscale(LabelState state)
		{
			switch (state)
			{
				case LabelState.Disabled:
					return GreyscaleDisabled;
				case LabelState.Pressed:
					return GreyscalePressed;
				case LabelState.Highlighted:
					return GreyscaleHighlighted;
				case LabelState.Selected:
					return GreyscaleSelected;
				default:
					return GreyscaleNormal;
			}
		}

		private void DriveSaturation(Graphic graphic, float grey, bool instant)
		{
			if (graphic == null || SaturationShader == null)
			{
				return;
			}

			if (!rigs.TryGetValue(graphic, out SaturationRig rig))
			{
				AddRig(graphic);
				rigs.TryGetValue(graphic, out rig);
			}

			if (rig == null)
			{
				return;
			}

			rig.target = grey;

			if (instant)
			{
				rig.current = grey;
				FlushSaturation(graphic, rig, grey);
				return;
			}

			chasing = true;
		}

		private void AddRig(Graphic graphic)
		{
			if (graphic == null || rigs.ContainsKey(graphic))
			{
				return;
			}

			Material instance = new Material(SaturationShader) { name = $"{graphic.name} Grey tint" };

			rigs[graphic] = new SaturationRig
			{
				material = instance,
				original = graphic.material,
				// Sentinel outside the 0..1 domain: the FIRST apply is instant
				// (OnEnable), so no stale value ever chases.
				current = -1f,
				target = -1f
			};

			graphic.material = instance;
		}

		private void ChaseSaturation()
		{
			if (!chasing)
			{
				return;
			}

			float step = Time.unscaledDeltaTime / Mathf.Max(ImageColors.fadeDuration, 0.0001f);
			bool anyMoving = false;

			foreach (KeyValuePair<Graphic, SaturationRig> pair in rigs)
			{
				Graphic graphic = pair.Key;
				SaturationRig rig = pair.Value;

				if (graphic == null || rig == null || Mathf.Approximately(rig.current, rig.target))
				{
					continue;
				}

				rig.current = Mathf.MoveTowards(rig.current, rig.target, step);
				FlushSaturation(graphic, rig, rig.current);

				if (rig.current != rig.target)
				{
					anyMoving = true;
				}
			}

			chasing = anyMoving;
		}

		private void FlushSaturation(Graphic graphic, SaturationRig rig, float value)
		{
			rig.material.SetFloat(SaturationPropertyId, value);

			// Under a Mask the drawing material is a stencil-variant clone of
			// our instance (the stock Masking path), so the renderer's CURRENT
			// material gets the value too - otherwise the clone keeps whatever
			// it snapshotted when the mask created it. Outside a mask this is
			// our own instance and the second write is skipped.
			Material drawn = graphic.canvasRenderer.GetMaterial();

			if (drawn != null && !ReferenceEquals(drawn, rig.material))
			{
				drawn.SetFloat(SaturationPropertyId, value);
			}
		}

		private void OnDestroy()
		{
			foreach (KeyValuePair<Graphic, SaturationRig> pair in rigs)
			{
				SaturationRig rig = pair.Value;

				if (rig == null)
				{
					continue;
				}

				if (pair.Key != null && rig.original != null)
				{
					// Put the graphic back on its authored material: removing
					// this component must not leave an orphaned grey material
					// behind.
					pair.Key.material = rig.original;
				}

				if (rig.material != null)
				{
					Destroy(rig.material);
				}
			}

			rigs.Clear();
		}
	}
}
