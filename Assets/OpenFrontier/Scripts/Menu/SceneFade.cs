using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// A transient black overlay that covers a scene handover: fade to black
	/// over the old screen, let the swap happen behind it, then fade away
	/// over the new one and destroy itself. LandingBoot spawns it for the
	/// loading-to-menu transition; later gamemode launches can reuse the same
	/// two calls.
	///
	/// The overlay lives on its own DontDestroyOnLoad canvas (highest sorting
	/// order) so it survives the switch and draws over BOTH screens. The
	/// coroutines deliberately run HERE, not on the caller: the landing scene
	/// unloads mid-transition, and every coroutine on its objects dies at
	/// exactly the moment the fade-in is needed.
	///
	/// The fade-in never races the swap: releasing a held activation does
	/// NOT mean the new scene is live - the load still spends unknown frames
	/// finishing and running the new scene's Awake/OnEnable storm (seconds
	/// on a phone), and a fade started on the release un-covers the OLD
	/// scene frozen mid-stall. StartFadeIn therefore holds full black until
	/// the AsyncOperation reports done, plus one more frame so the new
	/// scene's first render lands behind the cover, and only then lifts.
	///
	/// Everything runs unscaled: the fade can straddle the frozen loading
	/// world (the legacy timeScale-0 shape) and the live menu alike, and the
	/// fade clock is frame-history-proof: the first frame paints the start
	/// state without advancing (its delta would inherit the long frame just
	/// ended - the activation stall), and every later step is capped, so a
	/// slow frame stretches the ramp instead of jumping it.
	/// raycastTarget stays on for the overlay's whole life so the swap cannot
	/// be clicked through; controller selection is unaffected (navigation
	/// never was a raycast).
	/// </summary>
	[DisallowMultipleComponent]
	public class SceneFade : MonoBehaviour
	{
		private const float MaxStepSeconds = 0.05f;

		private static readonly Color Opaque = new Color(0f, 0f, 0f, 1f);

		private Image overlay;

		/// <summary>
		/// Builds the overlay canvas and returns its fader. Purely transient -
		/// the object destroys itself at the end of the fade-in.
		/// </summary>
		public static SceneFade Spawn()
		{
			GameObject go = new GameObject("Scene Fade", typeof(Canvas), typeof(Image), typeof(SceneFade));

			Canvas canvas = go.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = short.MaxValue; // over every UI either screen owns

			RectTransform rect = go.GetComponent<RectTransform>();
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;

			Image image = go.GetComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0f);
			// Blocks pointer input through the black for the overlay's life.
			image.raycastTarget = true;

			DontDestroyOnLoad(go);

			SceneFade fader = go.GetComponent<SceneFade>();
			fader.overlay = image;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			Debug.Log("[SceneFade] overlay spawned - covering the handover.");
#endif

			return fader;
		}

		/// <summary>
		/// Runs on the CALLER's coroutine - legal only while the caller's
		/// scene is still alive (the landing, up to the moment of the swap).
		/// Ends fully opaque.
		/// </summary>
		public IEnumerator FadeToBlack(float seconds)
		{
			yield return Animate(seconds, 0f, 1f);
			overlay.color = Opaque;
		}

		/// <summary>
		/// MUST be started on the fader itself: the caller's scene is already
		/// unloading, and a coroutine left there would be killed mid-fade.
		/// Holds full black until the load is DONE (all new Awakes ran) plus
		/// one frame for the new scene's first render, then lifts. Pass the
		/// AsyncOperation the caller released; null skips the wait.
		/// Destroys the overlay when done.
		/// </summary>
		public void StartFadeIn(float seconds, AsyncOperation activation)
		{
			StartCoroutine(FadeInAfter(activation, seconds));
		}

		private IEnumerator FadeInAfter(AsyncOperation activation, float seconds)
		{
			// Released != live: the released load still finishes and
			// activates over unknown frames, and the OLD scene keeps
			// rendering underneath until the swap. Staying opaque through
			// all of it is what makes the stall invisible.
			if (activation != null)
			{
				while (!activation.isDone)
				{
					yield return null;
				}
			}

			// One frame past done: the new scene's first render (its Start
			// calls included) lands behind the cover before anything shows.
			yield return null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			Debug.Log("[SceneFade] activation done - lifting.");
#endif
			yield return FadeFromBlack(seconds);
		}

		private IEnumerator FadeFromBlack(float seconds)
		{
			yield return Animate(seconds, 1f, 0f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			Debug.Log("[SceneFade] done - destroying the overlay.");
#endif
			Destroy(gameObject);
		}

		private IEnumerator Animate(float seconds, float from, float to)
		{
			// Zero-length still lands the target exactly - a caller can pass
			// 0 to skip a leg.
			if (seconds <= 0f)
			{
				yield break;
			}

			float elapsed = 0f;
			bool primed = false;

			while (elapsed < seconds)
			{
				// The first frame paints the start state and advances
				// nothing: its delta is inherited from the LONG frame just
				// ended (the activation stall before a fade-in), and one
				// frame must not eat the leg. Steps are capped so a slow
				// frame stretches the ramp instead of jumping it.
				if (primed)
				{
					elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds);
				}

				primed = true;
				overlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(Mathf.Lerp(from, to, elapsed / seconds)));
				yield return null;
			}
		}
	}
}
