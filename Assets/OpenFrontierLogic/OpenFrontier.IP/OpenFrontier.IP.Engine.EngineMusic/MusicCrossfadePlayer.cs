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
		/// Whether the current track has reached its pinned ending onset AND
		/// nobody has taken over yet.
		/// </summary>
		/// <remarks>
		/// The measured point at which the music starts coming down. It is
		/// median 68 seconds into a median 88 second track, and it is where
		/// the handover belongs, because that is the point at which the
		/// composer started ending the piece.
		///
		/// The incoming clause is what keeps a handover from repeating: while
		/// the outgoing track is playing its ending underneath the incoming
		/// one, the elapsed time keeps growing past the onset, so without it
		/// this stays true for the whole overlap and the caller asks for
		/// another handover every frame - which restarts the incoming track
		/// from its first sample and sounds like a skip.
		/// </remarks>
		public bool ReachedEndingOnset =>
			currentEntry != null && !incomingWaiting
				&& CurrentElapsed >= currentEntry.EndingOnset;

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

			// Refuse to stack handovers. ReachedEndingOnset already excludes a
			// pending one, so this should be unreachable - but if it is ever
			// reached, the second call would reassign the clip of a source
			// that is mid-playback and restart it from zero, which is the
			// skip sound this whole class exists to avoid.
			if (incomingWaiting)
			{
				return false;
			}

			AudioClip clip = MusicLibrary.ClipFor(entry);

			if (clip == null)
			{
				return false;
			}

			EnsureSources();

			incomingSource.Stop();
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

			AudioClip clip = MusicLibrary.ClipFor(entry);

			if (clip == null)
			{
				// Nothing to fade to. Leave the current track alone rather
				// than fading it out into silence, which is what treating an
				// unloadable track as a successful interruption would do.
				Debug.LogWarning("Music: cannot interrupt to '" + entry.Name
					+ "', its clip would not load. '" + currentEntry.Name
					+ "' keeps playing.");

				return false;
			}

			// Armed but not started. The clip is loaded and put on the spare
			// source now, and PromoteIncoming starts it when the fade ends.
			//
			// It has to be loaded HERE. Deferring the load to the promote left
			// the promoted source with no clip at all - silence - and left
			// incomingStartedAt at zero, so CurrentElapsed became Time.time
			// and ReachedEndingOnset fired immediately on the very next
			// frame. Every interruption was a silent gap followed by a skip.
			EnsureSources();

			incomingSource.Stop();
			incomingSource.clip = clip;
			incomingSource.time = 0f;

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

			// A handover is under way: the incoming track is already playing
			// underneath the outgoing one, and the outgoing still has its
			// composed ending to finish. Nothing happens until it stops.
			//
			// This branch is separate from the one below rather than a test
			// inside it, because getting that wrong is a deadlock: the only
			// thing that clears currentEntry was guarded on
			// !incomingWaiting, so during a handover it never cleared, so
			// PromoteIncoming never ran, so currentEntry stayed on the
			// finished track. Worse, ReachedEndingOnset stayed true with it,
			// and the director kept calling HandOver - restarting the
			// incoming clip from zero every frame. That is why
			// ReachedEndingOnset also excludes a pending handover.
			if (incomingWaiting)
			{
				if (currentEntry == null || currentSource == null || !currentSource.isPlaying)
				{
					PromoteIncoming();
				}

				return;
			}

			// A track that ran out before reaching its measured ending still
			// has to hand over, or the game goes quiet. Ending onset is a
			// measurement and measurements are occasionally wrong.
			if (currentEntry != null && currentSource != null
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

			// Swap, do not re-find. GetComponent<AudioSource>() returns the
			// FIRST AudioSource on the object, which is the one just stopped
			// - it was added first in EnsureSources - so assigning it here
			// pointed both fields at the same component. The "free" slot was
			// the busy one, and the next handover would stop the track that
			// was playing.
			bool wasAlreadyPlaying = incomingSource != null && incomingSource.isPlaying;

			if (incomingSource != null)
			{
				AudioSource outgoing = currentSource;

				currentSource = incomingSource;
				incomingSource = outgoing;

				currentSource.volume = MasterVolume;
				currentSource.loop = false;

				// Two paths arrive here and they differ. A handover's track is
				// already playing underneath and must not be restarted. An
				// interruption's track was armed but deliberately silent while
				// the old one faded, and has to start now.
				//
				// The clock follows the same split: a handover keeps the time
				// it actually began at, so it stays in step with music that
				// has been audible since before the fade; an interruption
				// starts counting from the moment it became audible, or its
				// elapsed time would include the fade it spent not playing.
				if (!wasAlreadyPlaying && currentSource.clip != null)
				{
					currentSource.Play();
					incomingStartedAt = Time.time;
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
				+ (incomingEntry != null ? ", next " + incomingEntry.Name : "");
		}
	}
}