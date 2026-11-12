using System.Collections;
using OpenFrontier.Gamemodes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Boot conductor for the landing scene - build index 0, the default scene
	/// the game always starts with. The original game's landing scene held a
	/// blank screen until its engine and UI controllers had self-initialised,
	/// then jumped to the main menu; this reproduces that shape with what the
	/// rebuild needs: the loading screen is up while the menu scene loads and
	/// boot-time services warm, and a short minimum display keeps a fast load
	/// from reading as a flash. The handover itself is a cross-fade
	/// (SceneFade): fadeOutSeconds to black over the loading screen, the swap
	/// happens behind full black and STAYS black until the menu scene has
	/// finished activating (the activation stall is invisible), then
	/// fadeInSeconds fading away over the menu - one second of fade at the
	/// defaults.
	///
	/// Everything here is unscaled: a loading screen runs while the world can
	/// be frozen (the legacy LoadingScreenUI ran at timeScale 0), so waits use
	/// WaitForSecondsRealtime and elapsed time reads Time.unscaledTime.
	///
	/// The Main Menu scene must keep working standalone - it stays directly
	/// playable for iteration - so nothing in it may depend on this having run.
	/// </summary>
	public class LandingBoot : MonoBehaviour
	{
		[Tooltip("Scene switched to once boot init finishes - must sit right after the landing scene in build settings.")]
		public string menuSceneName = "Main Menu";

		[Tooltip("Shortest time the loading screen stays visible, so a fast load never reads as a flash.")]
		public float minimumDisplaySeconds = 0.5f;

		[Tooltip("Seconds the loading screen fades to black before the menu scene activates.")]
		public float fadeOutSeconds = 0.5f;

		[Tooltip("Seconds the black fades away over the menu after it activates.")]
		public float fadeInSeconds = 0.5f;

		[Tooltip("Filled Image driven by the real scene-load progress (0..1). Optional - without it the screen just waits out the beat.")]
		public Image progressFill;

		private void Start()
		{
			StartCoroutine(BootSequence());
		}

		private IEnumerator BootSequence()
		{
			float started = Time.unscaledTime;

			// Boot init - the landing scene's whole reason to exist. Today the
			// menu stack needs exactly one thing warm (the lazy gamemode
			// registry); grow this block as boot-time services arrive.
			GamemodeRegistry.Warmup();

			AsyncOperation load = SceneManager.LoadSceneAsync(menuSceneName);

			if (load == null)
			{
				Debug.LogError($"[LandingBoot] Could not start loading '{menuSceneName}' - is it in build settings right after the landing scene? Staying on the loading screen.");
				yield break;
			}

			// Activation is held so the minimum beat below is honoured even
			// when the load finishes instantly (in the editor it always does).
			load.allowSceneActivation = false;

			while (load.progress < 0.89f)
			{
				ApplyFill(load.progress / 0.89f);
				yield return null;
			}

			ApplyFill(1f);

			float elapsed = Time.unscaledTime - started;

			if (elapsed < minimumDisplaySeconds)
			{
				yield return new WaitForSecondsRealtime(minimumDisplaySeconds - elapsed);
			}

			// Cross-fade handover: the overlay spawns while this scene is
			// still alive and covers the loading screen, the swap happens
			// behind full black, and the fade-away runs on the fader itself -
			// it survives the scene unload, this coroutine does not. The
			// fader holds the black until the menu has truly activated
			// (isDone + one frame) before lifting.
			SceneFade fade = SceneFade.Spawn();
			yield return fade.FadeToBlack(fadeOutSeconds);
			load.allowSceneActivation = true;
			fade.StartFadeIn(fadeInSeconds, load);
		}

		private void ApplyFill(float value)
		{
			if (progressFill != null)
			{
				progressFill.fillAmount = Mathf.Clamp01(value);
			}
		}
	}
}
