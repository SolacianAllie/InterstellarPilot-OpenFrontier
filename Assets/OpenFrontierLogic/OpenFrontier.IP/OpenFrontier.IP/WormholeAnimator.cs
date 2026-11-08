using UnityEngine;

namespace OpenFrontier.IP
{
	public class WormholeAnimator : MonoBehaviour
	{
		public delegate void AnimationFinishedHandler(WormholeAnimator sender);

		public AudioSource EntryAudioSourcePrefab;

		public AudioSource FinishAudioSourcePrefab;

		public MoveGameObject Mover;

		public event AnimationFinishedHandler AnimationFinished;

		/// <summary>
		/// Whether a wormhole transit is under way.
		/// </summary>
		/// <remarks>
		/// Set when one of these is created and cleared when it finishes, so
		/// it is true for exactly the window a ship is inside the wormhole.
		///
		/// A static flag rather than a component lookup on purpose. The music
		/// system needs to know this every frame, and FindObjectOfType to
		/// answer it would be a scene walk per frame - which is the exact cost
		/// this project's diagnostics already got wrong once. A property read
		/// is free.
		///
		/// The component already exists precisely to mark this window - it
		/// disables itself the moment the mover stops - so nothing new is
		/// being tracked, only published.
		/// </remarks>
		public static bool Active { get; private set; }

		/// <summary>Forces the flag clear. For a backstop, not normal use.</summary>
		public static void ClearActive() => Active = false;

		private void OnDisable()
		{
			// The component disables itself when the transit finishes, and
			// that path already clears the flag. This is the other exit: a
			// scene change destroying the animator mid-transit, where Update
			// never runs again.
			//
			// Without it the flag can survive as true with nothing left to
			// clear it, and everything downstream decides the ship is still
			// inside a wormhole forever.
			Active = false;
		}

		private void Awake()
		{
			Active = true;

			Object.Instantiate(EntryAudioSourcePrefab);
		}

		private void Update()
		{
			if (!Mover.enabled)
			{
				Active = false;

				if (AnimationFinished != null)
				{
					AnimationFinished(this);
				}
				if (FinishAudioSourcePrefab != null)
				{
					Object.Instantiate(FinishAudioSourcePrefab.gameObject);
				}
				enabled = false;
			}
		}
	}
}
