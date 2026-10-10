using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>One host sample, kept in the order it arrived.</summary>
	public struct UnitSample
	{
		public float Time;
		public Vector3 Position;
		public Quaternion Rotation;
		public Vector3 Velocity;
		public float Throttle;
	}

	/// <summary>
	/// Draws each ship between the host's samples instead of on them.
	///
	/// This is the piece that was missing. The host sends a position ten times
	/// a second, and the client used to write each one straight onto the
	/// transform as it arrived - so a ship did not move so much as jump, ten
	/// times a second, and the gaps between jumps were dead time. Everything
	/// wrong with how a client's world looked came from here: the snapping,
	/// the ships that sat still and then leapt, the ones that drifted off and
	/// got pulled back.
	///
	/// The fix is to stop drawing the newest sample and start drawing a
	/// moment in the past. Roughly one broadcast interval behind, which is
	/// enough that there is always a later sample to interpolate *towards*
	/// - being behind is the whole trick. A ship drawn at the newest known
	/// position has to wait for the network to tell it where to go next; a
	/// ship drawn between two known positions is already going there.
	///
	/// Nothing here touches rigidbodies. The ships being drawn are asleep
	/// (see <see cref="AuthorityGate"/>) and are moved by writing their
	/// transform, which is a rendering concern and should not start a physics
	/// argument. A rigidbody that has just been given a velocity and a
	/// position will integrate for one step and end up somewhere between the
	/// two, which is why the same ship used to drift and then snap back.
	/// </summary>
	public sealed class UnitInterpolator : MonoBehaviour
	{
		/// <summary>The name of the object this installs itself on.</summary>
		public const string ObjectName = "Multiplayer Interpolator";

		/// <summary>
		/// How far behind the host's newest sample to draw.
		/// </summary>
		/// <remarks>
		/// A shade over two broadcast intervals, so there is always a sample
		/// on the far side of the moment being drawn and the interpolation
		/// never has to guess. One interval would be enough on a perfect
		/// network and not enough on a real one; much more and the ship lags
		/// visibly behind its own wake.
		/// </remarks>
		public const float DelaySeconds = 0.22f;

		/// <summary>Samples kept per ship.</summary>
		/// <remarks>
		/// About a second at the broadcast rate. Enough to interpolate
		/// smoothly through a hiccup of two or three missed updates, which is
		/// what a GC pause or a slow frame looks like from the other side.
		/// </remarks>
		private const int Capacity = 12;

		private struct Track
		{
			// Not initialised inline: a struct field initialiser would make
			// every read of this allocate and copy a dozen samples' worth of
			// array, and this is read once per ship per frame.
			public UnitSample[] Samples;

			public int Count;
			public int Head;

			public static Track New()
			{
				return new Track { Samples = new UnitSample[Capacity] };
			}

			public void Add(UnitSample sample)
			{
				// Full: drop the oldest by overwriting it. Newer samples are
				// the only ones that can be drawn towards, and the delay is a
				// fifth of a second, so a second of history is far more than
				// the render pass can reach.
				if (Count == Capacity)
				{
					Samples[Head] = sample;
					Head = (Head + 1) % Capacity;
					return;
				}

				Samples[(Head + Count) % Capacity] = sample;
				Count++;
			}

			/// <summary>The sample at an index counted from the oldest.</summary>
			public UnitSample At(int index)
			{
				return Samples[(Head + index) % Capacity];
			}

			public UnitSample Oldest => At(0);
			public UnitSample Newest => At(Count - 1);
		}

		private static readonly Dictionary<int, Track> tracks = new Dictionary<int, Track>();

		/// <summary>
		/// The client's local time minus the host's.
		/// </summary>
		/// <remarks>
		/// Measured rather than negotiated. The two clocks have no reason to
		/// agree and never will - one counts since the editor opened, the
		/// other since a session did - so all that is needed is the
		/// difference, and one good sample is enough to find it.
		///
		/// Smoothed, because the offset is really two round trips and two
		/// clocks, and taking the newest measurement outright would step every
		/// ship once per measurement. Eased in instead, a tenth at a time, so a
		/// wrong reading is corrected over a second rather than applied as a
		/// lurch that every ship in view shares.
		/// </remarks>
		private static float clockOffset;

		private static bool clockKnown;

		/// <summary>Installs the interpolator if it is not already there.</summary>
		public static void EnsureInstalled()
		{
			if (Instance != null)
			{
				return;
			}

			GameObject host = new GameObject(ObjectName);
			host.hideFlags = HideFlags.HideAndDontSave;
			DontDestroyOnLoad(host);
			host.AddComponent<UnitInterpolator>();
		}

		public static UnitInterpolator Instance { get; private set; }

		/// <summary>Forgets every sample. Called when the world changes under us.</summary>
		public static void Forget()
		{
			tracks.Clear();
			clockKnown = false;
		}

		private void Awake()
		{
			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		/// <summary>
		/// Keeps one sample per ship from a batch that just arrived.
		/// </summary>
		public static void Record(UnitStateMessage message)
		{
			if (message.Units == null || message.Units.Length == 0)
			{
				return;
			}

			NoteClock(message.Time);

			float when = message.Time;

			for (int i = 0; i < message.Units.Length; i++)
			{
				UnitState state = message.Units[i];

				Track track = tracks.TryGetValue(state.Id, out Track found)
					? found
					: Track.New();

				// Out of order, or a duplicate. A sample older than the newest
				// one already held would make the ship jump backwards, and
				// arriving twice can happen when a socket redelivers.
				if (track.Count > 0 && when <= track.Newest.Time)
				{
					continue;
				}

				track.Add(new UnitSample
				{
					Time = when,
					Position = new Vector3(state.PositionX, state.PositionY, state.PositionZ),
					Rotation = new Quaternion(
						state.RotationX, state.RotationY, state.RotationZ, state.RotationW),

					// Kept, and used below. The host has been sending this
					// since the first version of the state message and
					// nothing ever read it.
					Velocity = new Vector3(state.VelocityX, state.VelocityY, state.VelocityZ),
					Throttle = state.Throttle
				});

				tracks[state.Id] = track;
			}
		}

		private static void NoteClock(float hostTime)
		{
			float measured = hostTime - Time.unscaledTime;

			if (!clockKnown)
			{
				clockOffset = measured;
				clockKnown = true;
				return;
			}

			clockOffset += (measured - clockOffset) * 0.1f;
		}

		/// <summary>
		/// The host's clock, as this client would express it.
		/// </summary>
		private static float HostNow => Time.unscaledTime - clockOffset;

		private void LateUpdate()
		{
			if (tracks.Count == 0 || !clockKnown)
			{
				return;
			}

			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return;
			}

			float renderTime = HostNow - DelaySeconds;

			// Copied, because a ship whose samples have all gone stale is
			// dropped from the collection while this walks it.
			List<int> ids = new List<int>(tracks.Keys);

			for (int i = 0; i < ids.Count; i++)
			{
				int id = ids[i];

				Track track = tracks[id];
				Unit unit = engine.GetUnitByid(id);

				// The ship is gone, or has not arrived. Either way there is
				// nothing to draw it on, and holding its samples would grow
				// the collection for a ship that no longer exists.
				if (unit == null || unit.IsDestroyed || track.Count == 0)
				{
					tracks.Remove(id);
					continue;
				}

				// The ship this player is flying. It is simulated here, so
				// there is nothing to interpolate - and drawing it from the
				// host's samples would put a round trip between the stick and
				// the ship, which is the one thing a control must never do.
				if (AuthorityGate.IsLocallyOwned(engine, unit))
				{
					continue;
				}

				unit.transform.localPosition = PositionAt(track, renderTime);
				unit.transform.localRotation = RotationAt(track, renderTime);

				// Applied through the holder, which clamps it, so a client
				// cannot be pushed outside the range the engine would have
				// allowed - a number read off a socket is not a number to
				// trust, and a plume scaled by thirty is a plume nobody wants
				// to look at.
				if (unit.Components != null)
				{
					unit.Components.EngineThrottle = ThrottleAt(track, renderTime);
				}
			}
		}

		/// <summary>
		/// Where the ship was at a moment between its samples, or its newest
		/// position if that moment is in the future.
		/// </summary>
		private static Vector3 PositionAt(Track track, float when)
		{
			UnitSample newest = track.Newest;

			if (when >= newest.Time)
			{
				return Extrapolated(newest, when);
			}

			if (track.Count == 1)
			{
				return newest.Position;
			}

			for (int i = track.Count - 1; i > 0; i--)
			{
				UnitSample b = track.At(i);
				UnitSample a = track.At(i - 1);

				if (when < a.Time || when > b.Time)
				{
					continue;
				}

				float span = b.Time - a.Time;

				// Two samples stamped the same instant. There is no span to
				// interpolate across, so this is a stand still rather than a
				// division by zero and a ship at the origin.
				if (span <= 0f)
				{
					return a.Position;
				}

				return Vector3.LerpUnclamped(a.Position, b.Position, (when - a.Time) / span);
			}

			return track.Oldest.Position;
		}

		/// <summary>Facing the same way, the same moment.</summary>
		/// <summary>
		/// Carries a ship onward from its newest sample, briefly.
		/// </summary>
		/// <remarks>
		/// Holding at the newest sample instead is what made a client's world
		/// look like it stepped. It steps whenever the moment being drawn is
		/// ahead of the newest message, and change detection guarantees it
		/// often: the host only sends a ship that has moved, so a ship moving
		/// slowly enough to stay under the threshold sends every few ticks
		/// rather than every tick, and for a moment in every gap there is
		/// nothing to interpolate towards.
		///
		/// So the gap is carried through on the velocity the host has been
		/// sending all along and nothing was reading, which turns a step into
		/// a continuation.
		///
		/// Bounded, because a ship that has just been told to stop would
		/// otherwise coast on the last velocity it had for ever, and a
		/// dropped connection would send every visible ship sailing away.
		/// Past the bound it holds, which is wrong for a moment and visible,
		/// rather than wrong indefinitely and invisible.
		/// </remarks>
		private static Vector3 Extrapolated(UnitSample newest, float when)
		{
			float ahead = when - newest.Time;

			if (ahead <= 0f || ahead > MaxExtrapolationSeconds)
			{
				return newest.Position;
			}

			return newest.Position + (newest.Velocity * ahead);
		}

		/// <summary>How far past its last message a ship may be carried.</summary>
		/// <remarks>
		/// About a fifth of a second, which is two broadcast intervals: enough
		/// to carry a ship through the gap that change detection leaves, and
		/// short enough that a ship told to stop has visibly stopped rather
		/// than sailed off.
		/// </remarks>
		private const float MaxExtrapolationSeconds = 0.2f;

		/// <summary>
		/// How hard the engines were being pushed at a moment between samples.
		/// </summary>
		/// <remarks>
		/// Interpolated for the same reason the position is. Throttle arrives
		/// with the rest of the sample, ten times a second at most, and a
		/// plume that steps between those is the same artefact as a ship that
		/// steps - it draws the eye to the gap rather than to the motion.
		///
		/// No extrapolation here, unlike position. A ship that has stopped
		/// should show no flame at all, and carrying the last throttle forward
		/// would show full flame on a ship that has been told to stop. It
		/// eases to the newest value instead, which arrives within a fifth of
		/// a second and looks like an engine being shut off.
		/// </remarks>
		private static float ThrottleAt(Track track, float when)
		{
			UnitSample newest = track.Newest;

			if (when >= newest.Time)
			{
				return newest.Throttle;
			}

			for (int i = track.Count - 1; i > 0; i--)
			{
				UnitSample b = track.At(i);
				UnitSample a = track.At(i - 1);

				if (when < a.Time || when > b.Time)
				{
					continue;
				}

				float span = b.Time - a.Time;

				if (span <= 0f)
				{
					return a.Throttle;
				}

				return Mathf.LerpUnclamped(a.Throttle, b.Throttle, (when - a.Time) / span);
			}

			return track.Oldest.Throttle;
		}

		private static Quaternion RotationAt(Track track, float when)
		{
			UnitSample newestSample = track.Newest;

			if (when >= newestSample.Time)
			{
				return newestSample.Rotation;
			}

			if (track.Count == 1)
			{
				return newestSample.Rotation;
			}

			for (int i = track.Count - 1; i > 0; i--)
			{
				UnitSample b = track.At(i);
				UnitSample a = track.At(i - 1);

				if (when < a.Time || when > b.Time)
				{
					continue;
				}

				float span = b.Time - a.Time;

				if (span <= 0f)
				{
					return a.Rotation;
				}

				// Shortest way round. Slerping the long way makes a ship spin
				// most of a full turn when its nose crosses due north, which
				// is a spectacular bug that only appears on a heading.
				return Quaternion.SlerpUnclamped(
					a.Rotation, b.Rotation, (when - a.Time) / span);
			}

			return track.Oldest.Rotation;
		}
	}
}