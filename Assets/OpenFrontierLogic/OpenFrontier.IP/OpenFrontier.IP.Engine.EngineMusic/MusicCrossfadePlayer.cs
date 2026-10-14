using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineMusic
{
	/// <summary>
	/// Plays one track, hands over to the next without a cut, and never
	/// silences itself because it had nothing to say.
	/// </summary>
	/// <remarks>
	/// Two audio sources, because one cannot crossfade. The outgoing track
	/// keeps its own AudioSource and is allowed to finish; the incoming gets
	/// the other. Two paths out, and which one is used depends on whether the
	/// track was allowed to reach its ending:
	///
	///   Natural - the track reached its pinned ending onset, so the handover
	///   happens there and the outgoing plays its own composed ending under
	///   the incoming. Nothing is cut, and the overlap is exactly as long as
	///   the music wants it to be.
	///
	///   Interrupted - something changed the world first: you undocked, you
	///   jumped, a fight started. The outgoing is faded to silence over
	///   InterruptFadeSeconds and the incoming starts after.
	///
	/// The second path exists because the endings are long. Federal Starport
	/// has a 49 second ending. Waiting it out when a player undocks after
	/// twenty seconds would be worse than any cut.
	///
	/// The volume is read from the existing music player rather than kept
	/// here, so the options screen keeps working with no changes to it and no
	/// second volume setting to disagree with the first.
	/// </remarks>
	public sealed class MusicCrossfadePlayer : MonoBehaviour
	{
		/// <summary>How long a cut track takes to reach silence.</summary>
		[Tooltip("How long an interrupted track is faded out before the next one starts.")]
		public float InterruptFadeSeconds = 1.5f;

		private AudioSource currentSource;
		private AudioSource incomingSource;
		private MusicTimingTable.Entry currentEntry;
		private MusicTimingTable.Entry incomingEntry;
		private float currentStartedAt;
		private float incomingStartedAt;
		private float fadeStartedAt;
		private bool fadingOut;
		private bool incomingWaiting;

		/// <summary>What is playing, or null.</summary>
		public MusicTimingTable.Entry Current => currentEntry;

		/// <summary>What is coming, once a handover has been decided.</summary>
		public MusicTimingTable.Entry Incoming => incomingEntry;

		/// <summary>True while the outgoing track is being faded to silence.</summary>
		public bool IsFadingOut => fadingOut;

		/// <summary>Seconds since the current track started.</summary>
		public float CurrentElapsed => currentEntry == null
			? 0f
			: Time.time - currentStartedAt;

		/// <summary>
		/// Whether the current track has reached its pinned ending onset.
		/// </summary>
		/// <remarks>
		/// The measured point at which the music starts coming down. It is
		/// median 68 seconds into a median 88 second track, and it is where
		/// the handover belongs, because that is the point at which the
		/// composer started ending the piece.
		/// </remarks>
		public bool ReachedEndingOnset =>
			currentEntry != null && CurrentElapsed >= currentEntry.EndingOnset;

		/// <summary>How long until the ending onset, or 0 if not known.</summary>
		public float SecondsToEndingOnset
		{
			get
			{
				if (currentEntry == null)
				{
					return 0f;
				}

				return Mathf.Max(0f, currentEntry.EndingOnset - CurrentElapsed);
			}
		}

		private void Awake()
		{
			EnsureSources();
		}

		private void EnsureSources()
		{
			if (currentSource == null)
			{
				currentSource = gameObject.AddComponent<AudioSource>();
				currentSource.playOnAwake = false;
				currentSource.loop = false;
				currentSource.spatialBlend = 0f;
			}

			if (incomingSource == null)
			{
				incomingSource = gameObject.AddComponent<AudioSource>();
				incomingSource.playOnAwake = false;
				incomingSource.loop = false;
				incomingSource.spatialBlend = 0f;
			}
		}

		/// <summary>
		/// The music volume, from the existing player.
		/// </summary>
		/// <remarks>
		/// Read rather than stored so that moving the volume slider in the
		/// options takes effect immediately, which it would not if the value
		/// were latched when a track started - a track can run for three
		/// minutes and the player expects the slider to do something.
		/// </remarks>
		public float MasterVolume
		{
			get
			{
				if (GameController.Instance != null && GameController.Instance.MusicPlayer != null)
				{
					return GameController.Instance.MusicPlayer.Volume;
				}

				return 0.5f;
			}
		}

		/// <summary>
		/// Starts a track immediately, cutting whatever was playing.
		/// </summary>
		/// <remarks>
		/// For the first track of a session and for anything that has to
		/// sound like it began there. Everything else goes through
		/// <see cref="HandOver"/> or <see cref="Interrupt"/>, so that this
		/// stays the rare path and a cut is always deliberate.
		/// </remarks>
		public bool Start(MusicTimingTable.Entry entry)
		{
			if (entry == null)
			{
				return false;
			}

			AudioClip clip = MusicLibrary.ClipFor(entry);

			if (clip == null)
			{
				return false;
			}

			EnsureSources();

			StopBoth();
			incomingWaiting = false;
			fadingOut = false;

			currentSource.clip = clip;
			currentSource.volume = MasterVolume;
			currentSource.time = 0f;
			currentSource.Play();

			currentEntry = entry;
			currentStartedAt = Time.time;

			Debug.Log("Music: playing '" + entry.Name + "' (" + entry.Category
				+ ") from the start. " + entry.Duration.ToString("0")
				+ "s long, ends at " + entry.EndingOnset.ToString("0") + "s.");

			return true;
		}

		/// <summary>
		/// Hands over at the composed ending. The outgoing track is left to
		/// finish underneath.
		/// </summary>
		public bool HandOver(MusicTimingTable.Entry entry)
		{
			if (entry == null || currentEntry == null || entry == currentEntry)
			{
				return false;
			}

			AudioClip clip = MusicLibrary.ClipFor(entry);

			if (clip == null)
			{
				return false;
			}

			EnsureSources();

			incomingSource.clip = clip;
			incomingSource.volume = MasterVolume;
			incomingSource.time = 0f;
			incomingSource.Play();

			incomingEntry = entry;
			incomingStartedAt = Time.time;
			incomingWaiting = true;

			Debug.Log("Music: handing over to '" + entry.Name + "' ("
				+ entry.Category + ") over the ending of '"
				+ currentEntry.Name + "', " + currentEntry.Duration.ToString("0")
				+ "s long with " + (currentEntry.Duration - currentEntry.EndingOnset).ToString("0")
				+ "s of ending left.");

			return true;
		}

		/// <summary>
		/// Cuts to a new track, fading the outgoing one to silence first.
		/// </summary>
		/// <remarks>
		/// The incoming track waits for the fade rather than starting under
		/// it. Starting both at once would be a crossfade, and a crossfade
		/// here means overlapping two pieces of music that were composed to be
		/// heard alone - which is how a transition ends up sounding like a
		/// mistake rather than a change of scene.
		/// </remarks>
		public bool Interrupt(MusicTimingTable.Entry entry)
		{
			if (entry == null)
			{
				return false;
			}

			if (currentEntry == null || currentSource == null || !currentSource.isPlaying)
			{
				return Start(entry);
			}

			incomingEntry = entry;
			incomingWaiting = true;
			fadingOut = true;
			fadeStartedAt = Time.time;

			Debug.Log("Music: interrupted '" + currentEntry.Name + "' after "
				+ CurrentElapsed.ToString("0") + "s of " + currentEntry.Duration.ToString("0")
				+ "s - fading to silence over " + InterruptFadeSeconds
				+ "s, then '" + entry.Name + "' (" + entry.Category + ").");

			return true;
		}

		private void Update()
		{
			if (fadingOut)
			{
				StepFadeOut();

				return;
			}

			// The incoming track has taken over; the old one is on its way out
			// and there is nothing to do until it is finished.
			if (incomingWaiting && currentEntry == null)
			{
				PromoteIncoming();
			}

			// A track that ran out before reaching its measured ending still
			// has to hand over, or the game goes quiet. Ending onset is a
			// measurement and measurements are occasionally wrong.
			if (!incomingWaiting && currentEntry != null && currentSource != null
				&& !currentSource.isPlaying && CurrentElapsed > 1f)
			{
				Debug.Log("Music: '" + currentEntry.Name + "' stopped playing at "
					+ CurrentElapsed.ToString("0") + "s of " + currentEntry.Duration.ToString("0")
					+ "s, before its ending onset at "
					+ currentEntry.EndingOnset.ToString("0") + "s.");

				currentEntry = null;
			}
		}

		private void StepFadeOut()
		{
			float elapsed = Time.time - fadeStartedAt;
			float t = InterruptFadeSeconds <= 0f
				? 1f
				: Mathf.Clamp01(elapsed / InterruptFadeSeconds);

			if (currentSource != null)
			{
				currentSource.volume = MasterVolume * (1f - t);
			}

			if (t >= 1f)
			{
				if (currentSource != null)
				{
					currentSource.Stop();
					currentSource.volume = MasterVolume;
				}

				currentEntry = null;
				fadingOut = false;
				PromoteIncoming();
			}
		}

		private void PromoteIncoming()
		{
			if (incomingEntry == null)
			{
				incomingWaiting = false;
				return;
			}

			if (currentSource != null)
			{
				currentSource.Stop();
				currentSource.clip = null;
			}

			if (incomingSource != null)
			{
				incomingSource.volume = MasterVolume;
				currentSource = incomingSource;
				incomingSource = gameObject.GetComponent<AudioSource>();

				if (incomingSource == null)
				{
					incomingSource = gameObject.AddComponent<AudioSource>();
					incomingSource.playOnAwake = false;
					incomingSource.loop = false;
					incomingSource.spatialBlend = 0f;
				}
			}

			currentEntry = incomingEntry;
			currentStartedAt = incomingStartedAt;
			incomingEntry = null;
			incomingWaiting = false;
		}

		private void StopBoth()
		{
			if (currentSource != null)
			{
				currentSource.Stop();
				currentSource.clip = null;
			}

			if (incomingSource != null && incomingSource != currentSource)
			{
				incomingSource.Stop();
				incomingSource.clip = null;
			}

			currentEntry = null;
			incomingEntry = null;
			incomingWaiting = false;
			fadingOut = false;
		}

		/// <summary>Stops everything, for leaving a game.</summary>
		public void StopAll()
		{
			StopBoth();
		}

		/// <summary>One line describing what is playing and why.</summary>
		public string Describe()
		{
			if (currentEntry == null)
			{
				return fadingOut ? "fading out, nothing playing" : "silent";
			}

			return currentEntry.Name + " (" + currentEntry.Category + ") at "
				+ CurrentElapsed.ToString("0") + "s of " + currentEntry.Duration.ToString("0")
				+ "s, ending at " + currentEntry.EndingOnset.ToString("0") + "s"
				+ (incomingWaiting ? ", handing over to " + incomingEntry.Name : "");
		}
	}
}