using UnityEngine;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Menu music: one looping 2D track owned by the Main Menu scene root -
	/// never under UI, because screens are spawned and destroyed freely and
	/// a screen-carried source would stop with them. The source is
	/// self-configured here so authoring cannot forget a setting: 2D (music
	/// is ambience, not a world object), looping, playOnAwake off - the
	/// component drives the start, so an authored clip cannot blast before
	/// the scene is live.
	///
	/// It starts at scene activation, which lands during the landing's
	/// fade-in leg: the track swells as the black lifts over the menu.
	/// A future gamemode launch will want to stop or crossfade it - that
	/// work owns its own hook; the launch is still a logged stub today.
	/// </summary>
	[RequireComponent(typeof(AudioSource))]
	[DisallowMultipleComponent]
	public class MenuMusic : MonoBehaviour
	{
		[Tooltip("The looping menu track. Null = silent.")]
		public AudioClip track;

		[Tooltip("Music volume.")]
		[Range(0f, 1f)] public float volume = 1f;

		private AudioSource source;

		private void Awake()
		{
			source = GetComponent<AudioSource>();
			source.playOnAwake = false;
			source.loop = true;
			source.spatialBlend = 0f;
			source.clip = track;
			source.volume = volume;

			if (track != null)
			{
				source.Play();
			}
		}
	}
}
