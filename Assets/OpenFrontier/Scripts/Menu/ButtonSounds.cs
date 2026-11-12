using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Menu button feedback: a hover blip when the pointer enters or
	/// navigation focus arrives, and a click blip on activation (pointer
	/// click and controller submit both fire the Button's onClick).
	///
	/// Selection is voiced ONLY for navigation (AxisEventData with a move
	/// direction): a pointer click also sets uGUI selection, and the select
	/// blip on the click frame read as the hover sound replaying; the
	/// code-driven default selection when a screen opens stays silent for
	/// the same reason. The same-frame guard still covers the resting
	/// cursor - pointer enter and a navigation select landing together.
	///
	/// NOTHING here is owned by the button: every blip plays through a
	/// transient DontDestroyOnLoad one-shot that self-destroys after the
	/// clip. A sound the UI owns dies with the UI - the ScreenNavigator
	/// destroys screens on back-navigation, and disabling a GameObject
	/// stops its AudioSources outright - so a button-carried source would
	/// routinely cut a blip mid-play. The survivor player outlives
	/// whichever screen spawned it and costs one short-lived GameObject
	/// per blip (human rate; the same shape Unity's own PlayClipAtPoint
	/// uses, and the same lesson SceneFade learned about objects dying
	/// with their scene).
	///
	/// Clips are authored data: assign them in the Inspector per button or
	/// per prefab. Null clips play nothing, silently.
	///
	/// Mouse-hover and navigation-select can land in the same frame (the
	/// resting cursor over a controller-focused row); a frame guard plays
	/// the blip once.
	/// </summary>
	[RequireComponent(typeof(Selectable))]
	[DisallowMultipleComponent]
	public class ButtonSounds : MonoBehaviour, IPointerEnterHandler, ISelectHandler
	{
		[Tooltip("Played when the pointer enters or navigation focus arrives. Null = silent.")]
		public AudioClip hoverClip;

		[Tooltip("Played on activation (pointer click and controller submit). Null = silent.")]
		public AudioClip clickClip;

		[Tooltip("Volume for both blips.")]
		[Range(0f, 1f)] public float volume = 1f;

		private Selectable selectable;

		private int lastHoverFrame = -1;

		private void Awake()
		{
			selectable = GetComponent<Selectable>();
		}

		private void OnEnable()
		{
			// Clicks route through the Button's onClick so pointer clicks AND
			// controller submit both sound, whatever else is wired there.
			if (selectable is Button button)
			{
				button.onClick.AddListener(PlayClick);
			}
		}

		private void OnDisable()
		{
			if (selectable is Button button)
			{
				button.onClick.RemoveListener(PlayClick);
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			PlayHover();
		}

		public void OnSelect(BaseEventData eventData)
		{
			// A pointer click also SETS selection - as a plain BaseEventData,
			// not an axis event. That is not focus arrival: the enter blip
			// already sounded, and the select blip on the click frame is
			// what read as "the hover sound replaying on click". Only
			// navigation focus sounds; code-driven default selection (a
			// screen opening) stays silent for the same reason.
			if (eventData is AxisEventData axis && axis.moveDir != MoveDirection.None)
			{
				PlayHover();
			}
		}

		private void PlayHover()
		{
			if (hoverClip == null || selectable == null || !selectable.IsInteractable())
			{
				return;
			}

			// One blip when enter and select land in the same frame.
			if (lastHoverFrame == Time.frameCount)
			{
				return;
			}

			lastHoverFrame = Time.frameCount;
			PlayOneShot(hoverClip, volume);
		}

		private void PlayClick()
		{
			if (clickClip == null)
			{
				return;
			}

			PlayOneShot(clickClip, volume);
		}

		/// <summary>
		/// The one-shot: a DontDestroyOnLoad GameObject with a 2D AudioSource,
		/// destroyed after the clip - delayed destroy needs no coroutine, and
		/// the +0.05s guards against clipping the tail. Overlapping blips are
		/// separate players and overlap naturally.
		/// </summary>
		private static void PlayOneShot(AudioClip clip, float volume)
		{
			GameObject player = new GameObject("Button OneShot");
			Object.DontDestroyOnLoad(player);

			AudioSource source = player.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.spatialBlend = 0f; // menu sounds are 2D
			source.PlayOneShot(clip, volume);

			Object.Destroy(player, clip.length + 0.05f);
		}
	}
}
