using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineMusic
{
	/// <summary>Where the threat logic has got to.</summary>
	public enum MusicThreatState
	{
		/// <summary>Nothing worth reacting to. Ordinary music.</summary>
		Normal = 0,

		/// <summary>Enough hostiles to matter, but no shooting.</summary>
		Threat = 1,

		/// <summary>The player is attacking something they can see, or is being shot at.</summary>
		Engagement = 2,

		/// <summary>
		/// The fight is over but the threat music has not finished.
		/// </summary>
		/// <remarks>
		/// Not a state the world is in - a state the MUSIC is in. Engagement
		/// ends the moment the shooting stops, but cutting straight back to
		/// ambience at that instant is abrupt: the player has just survived
		/// something and the music should let them out of it rather than
		/// snatching it away. So the threat theme is played out, and only
		/// when it reaches its own ending does the ordinary music return.
		/// </remarks>
		AllClear = 3
	}

	/// <summary>What the world looks like from a music point of view.</summary>
	public struct MusicCombatState
	{
		/// <summary>Hostiles the player's faction has detected in this sector.</summary>
		public int HostileCount;

		/// <summary>Player attacking a visible hostile, or targeted by one.</summary>
		public bool Engaged;

		/// <summary>Low, Medium or High.</summary>
		public string Intensity;

		/// <summary>Threat while merely present, Engagement once shooting.</summary>
		public string Category;

		/// <summary>Which step of the threat logic we are on.</summary>
		public MusicThreatState ThreatState;

		public bool IsThreat => ThreatState == MusicThreatState.Threat
			|| ThreatState == MusicThreatState.Engagement
			|| ThreatState == MusicThreatState.AllClear;

		public override string ToString()
		{
			if (ThreatState == MusicThreatState.Normal)
			{
				return HostileCount <= ThreatThreshold
					? "calm (" + HostileCount + " seen, threshold "
						+ ThreatThreshold + ")"
					: "calm";
			}

			return HostileCount + " seen, " + Intensity + " " + Category
				+ ", " + ThreatState
				+ (Engaged ? " (engaged)" : "");
		}

		/// <summary>
		/// How many spotted hostiles it takes before the music reacts at all.
		/// </summary>
		/// <remarks>
		/// A gate, not an intensity. Intensity still comes from the count -
		/// under ten is Low, ten to nineteen Medium, twenty and over High -
		/// but nothing plays until the count clears this.
		///
		/// The old behaviour was "one hostile is enough", and with thirty-one
		/// threat sections between Low and High a lone scout crossing your
		/// path could start a theme and drop it again a few seconds later. A
		/// patrol has to look like a patrol.
		///
		/// Three is a judgement call and is meant to be moved. The thing it
		/// buys is that ordinary space traffic - a freighter, a patrol of two -
		/// stays ordinary, and only an actual encounter changes the music.
		/// </remarks>
		public const int ThreatThreshold = 3;

		/// <summary>
		/// How far the count has to fall before a threat is forgotten.
		/// </summary>
		/// <remarks>
		/// A second, lower threshold, so the gate itself does not chatter.
		///
		/// With one threshold a patrol sitting on three ships is unstable:
		/// one drifts out of sensor range, the count drops to two which is
		/// below the line, and twenty seconds later it drifts back and is over
		/// it again. Each crossing was an instant change of music, and the
		/// symptom was themes interrupting and restarting several times a
		/// minute as bandits crossed sensor range.
		///
		/// Deferring the change to the next track removes the cut. This
		/// removes much of the reason to defer, and both are wanted: the gate
		/// should be stable, and even a stable gate should not cut a track in
		/// half.
		///
		/// Entry and exit are deliberately not symmetric. It takes three ships
		/// to start worrying and one to stop, so a fight has to build before
		/// the music admits it, and a scattering patrol stands down sooner
		/// than it stood up.
		/// </remarks>
		public const int ThreatThresholdToStandDown = 1;
	}

	/// <summary>
	/// Decides between calm, threat music and combat music.
	/// </summary>
	/// <remarks>
	/// THE FOUR STEPS
	///
	///   Normal      fewer than ThreatThreshold hostiles visible. Ordinary
	///               music.
	///   Threat      enough hostiles visible. Threat music, no shooting yet.
	///   Engagement  the player is attacking something they can see, or
	///               something they can see is attacking them.
	///   AllClear    the shooting stopped. Threat music PLAYS OUT to its own
	///               ending, and only then does the ordinary music return.
	///
	/// ENGAGEMENT USES THE GAME'S OWN ANSWER, not a reimplementation.
	///
	/// EngineASX.PlayerInCombat is raised by exactly the two things wanted
	/// here - HudScreen raises it when something targets the player within
	/// NotifyPlayerTargettedMaxDistanceToTriggerInCombat, and HudAutoScanner
	/// raises it when the player attacks hostile targets it has scanned - and
	/// it clears itself AudioCombatMusicTimeout (25s) after the last such
	/// event. So "attacking or being targeted" and "beyond sensor range of
	/// enemies for a long time" are both already answered correctly, by the
	/// thing that has to answer them correctly anyway. Reimplementing it here
	/// would be a second opinion about when a fight is over, and the two
	/// would disagree.
	///
	/// WHAT "SPOTTED" MEANS is the game's own: units the player's faction
	/// has detected, in the active sector, hostile and not its own. Not a
	/// distance, because the game has no opinion about distance for music,
	/// and counting what has been detected is counting what is visible.
	///
	/// COUNTS EVERY HALF SECOND, as asked. Nothing about how many ships are
	/// pointed at you changes faster than that, and the walk is over every
	/// discovered unit in the sector, so sixty times a second would buy
	/// nothing and cost plenty.
	///
	/// BUT ENTERING IS FAST AND LEAVING IS SLOW, deliberately. The old code
	/// settled every change over two seconds, including entering, which meant
	/// the music could be two seconds late to react to a fight starting. An
	/// attack should be heard immediately; letting go of it should be
	/// deliberate. Entering Threat or Engagement therefore takes effect on the
	/// first sample, and only the way back to calm waits.
	/// </remarks>
	public sealed class MusicCombatDetector
	{
		private const float SampleInterval = 0.5f;

		/// <summary>How long calm has to hold before the music lets go of the fight.</summary>
		/// <remarks>
		/// Two seconds, to absorb a target swapping one frame for another.
		/// The AllClear step already holds the music for much longer than
		/// this, so this is only about not flapping between Threat and calm
		/// while the count wobbles around the threshold.
		/// </remarks>
		private const float SettleSeconds = 2f;

		private readonly List<Unit> seen = new List<Unit>(64);
		private float lastSampleAt = -999f;

		private bool threatRaised;
		private MusicThreatState pendingState = MusicThreatState.Normal;
		private float pendingSince = -999f;

		public MusicCombatState State { get; private set; }

		/// <summary>
		/// Whether the threat music is being allowed to finish before the
		/// ordinary music returns.
		/// </summary>
		/// <remarks>
		/// True from the moment a fight ends until the director spends it, by
		/// handing over from the threat track to ordinary music. Consuming it
		/// is the director's job, because the handover is what the linger is
		/// actually waiting for.
		/// </remarks>
		public bool AllClearPending { get; private set; }

		/// <summary>
		/// The music has reached the end of the threat track during a
		/// linger, so the ordinary music may return.
		/// </summary>
		public void ConsumeAllClear()
		{
			AllClearPending = false;
			pendingState = MusicThreatState.Normal;
			pendingSince = -999f;

			// Straight to Normal, rather than waiting out the settling period
			// on the way down.
			//
			// The linger is waited for HERE - at the handover - because the
			// handover is the point at which the threat track has finished
			// playing out. Settling the state separately would leave the music
			// key reading Combat for two more seconds while the ordinary track
			// was already sounding, and when the key finally changed it would
			// hard-cut that track and start another one. So the fight is over
			// the moment the linger is spent, and the settle belongs only to
			// the way in.
			MusicCombatState served = State;
			served.ThreatState = MusicThreatState.Normal;
			State = served;
		}

		/// <summary>
		/// Forgets any fight entirely. Used when docking or a wormhole
		/// takes the music somewhere else.
		/// </summary>
		/// <remarks>
		/// Without this, arriving at a station mid-fight would leave a threat
		/// linger pending, and leaving the station would drop the player
		/// straight back into threat music for a fight that ended several
		/// sectors ago.
		/// </remarks>
		public void Reset()
		{
			AllClearPending = false;
			threatRaised = false;
			pendingState = MusicThreatState.Normal;
			pendingSince = -999f;
			State = default(MusicCombatState);
		}

		/// <summary>The last reading, and whether it is worth re-reading yet.</summary>
		public MusicCombatState Sample(bool force = false)
		{
			if (!force && Time.realtimeSinceStartup - lastSampleAt < SampleInterval)
			{
				return State;
			}

			lastSampleAt = Time.realtimeSinceStartup;

			EngineASX engine = EngineASX.Instance;

			if (engine == null || engine.LocalFaction == null || engine.ActiveSector == null)
			{
				State = default(MusicCombatState);
				return State;
			}

			Unit mine = engine.LocalPlayer == null || engine.LocalPlayer.Person == null
				? null
				: engine.LocalPlayer.Person.CurrentUnit;

			int count = CountHostiles(mine);

			// The game's own answer to "is the player fighting", which already
			// covers attacking a visible hostile and being targeted by one,
			// and already expires on its own timer.
			bool engaged = engine.PlayerInCombat;

			// The gate, with hysteresis: it takes more ships to start
			// worrying than to stop, so a patrol hovering on the line does
			// not oscillate.
			if (!threatRaised && count > MusicCombatState.ThreatThreshold)
			{
				threatRaised = true;
			}
			else if (threatRaised && count <= MusicCombatState.ThreatThresholdToStandDown)
			{
				threatRaised = false;
			}

			MusicThreatState raw = engaged
				? MusicThreatState.Engagement
				: threatRaised
					? MusicThreatState.Threat
					: MusicThreatState.Normal;

			State = new MusicCombatState
			{
				HostileCount = count,
				Engaged = engaged,
				Intensity = IntensityFor(count, State.Intensity),
				Category = engaged ? "Engagement" : "Threat",
				ThreatState = Settle(raw)
			};

			return State;
		}

		/// <summary>
		/// Holds a change to calm, and lets a change into danger through at once.
		/// </summary>
		private MusicThreatState Settle(MusicThreatState raw)
		{
			if (raw == State.ThreatState)
			{
				pendingState = raw;
				pendingSince = -999f;

				if (raw == MusicThreatState.Normal)
				{
					AllClearPending = false;
				}

				return raw;
			}

			// Into danger, or back to calm from a fight: through at once.
			if (raw == MusicThreatState.Engagement || raw == MusicThreatState.Threat)
			{
				pendingState = raw;
				pendingSince = -999f;
				AllClearPending = false;

				return raw;
			}

			// Heading back to calm.
			//
			// Straight from Engagement to Normal is the fight ending, and that
			// is the AllClear step: the threat music stays and plays out. The
			// linger is only skipped when the world stopped being dangerous
			// for a different reason - never spotted enough hostiles at all -
			// which is a quiet sector rather than a won fight.
			if (State.ThreatState == MusicThreatState.Engagement
				&& raw == MusicThreatState.Normal)
			{
				pendingState = raw;
				pendingSince = -999f;
				AllClearPending = true;

				return MusicThreatState.AllClear;
			}

			if (pendingState != raw)
			{
				pendingState = raw;
				pendingSince = Time.realtimeSinceStartup;
			}

			if (Time.realtimeSinceStartup - pendingSince >= SettleSeconds)
			{
				pendingSince = -999f;

				return raw;
			}

			return State.ThreatState;
		}

		/// <summary>Hostiles the player can see, in this sector.</summary>
		private int CountHostiles(Unit mine)
		{
			if (mine == null)
			{
				return 0;
			}

			FactionIntel intel = EngineASX.Instance.LocalFaction.Intel;

			if (intel == null)
			{
				return 0;
			}

			seen.Clear();
			intel.GetDiscoveredUnitIdsInSectorNonAlloc(
				EngineASX.Instance.ActiveSector, seen);

			int count = 0;

			for (int i = 0; i < seen.Count; i++)
			{
				Unit other = seen[i];

				if (other == null || other.IsDestroyed || other.Faction == null)
				{
					continue;
				}

				if (other.Faction == EngineASX.Instance.LocalFaction)
				{
					continue;
				}

				if (other.IsHostileTo(mine))
				{
					count++;
				}
			}

			return count;
		}

		/// <summary>
		/// The intensity band for a hostile count, with a dead band.
		/// </summary>
		/// <remarks>
		/// Under ten is Low, ten to nineteen Medium, twenty and over High,
		/// with an asymmetric dead band so a fight sitting on twenty does not
		/// flicker between Medium and High. Climbing needs the full
		/// threshold; dropping out needs to fall two below.
		/// </remarks>
		public static string IntensityFor(int hostileCount, string current)
		{
			if (current == "High")
			{
				return hostileCount >= 18 ? "High" : "Medium";
			}

			if (current == "Medium")
			{
				if (hostileCount >= 20)
				{
					return "High";
				}

				return hostileCount >= 8 ? "Medium" : "Low";
			}

			if (hostileCount >= 20)
			{
				return "High";
			}

			return hostileCount >= 10 ? "Medium" : "Low";
		}

		/// <summary>The intensity band for a count, with no history.</summary>
		public static string IntensityFor(int hostileCount) =>
			IntensityFor(hostileCount, null);
	}
}
