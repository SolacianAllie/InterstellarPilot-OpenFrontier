using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineMusic
{
	/// <summary>What the world looks like from a music point of view.</summary>
	public struct MusicCombatState
	{
		/// <summary>Hostiles you can see, in this sector.</summary>
		public int HostileCount;

		/// <summary>Whether any of them is shooting at you specifically.</summary>
		public bool AnyTargetingPlayer;

		/// <summary>Low, Medium or High.</summary>
		public string Intensity;

		/// <summary>Threat while nobody has you, Engagement once they do.</summary>
		public string Category;

		/// <summary>Whether anything is happening at all.</summary>
		public bool InCombat => HostileCount > 0;

		public override string ToString()
		{
			if (!InCombat)
			{
				return "no hostiles";
			}

			return HostileCount + " hostile(s), " + Intensity + " "
				+ Category + (AnyTargetingPlayer ? ", targeted" : ", not targeted");
		}
	}

	/// <summary>
	/// Works out whether you are being shot at, and by how many.
	/// </summary>
	/// <remarks>
	/// The yardstick for "nearby" is deliberately the game's own: a hostile
	/// your faction has detected, in the sector you are in, that is hostile to
	/// you and not yours. Not a distance, because the game has no opinion
	/// about distance for music and inventing one would mean a hostile you
	/// can see on a sensor is worth music and one you cannot is not, which is
	/// already the rule - so counting what has been detected is counting what
	/// you can see.
	///
	/// Intensity from the count, and which music from whether anyone is
	/// actually shooting at you rather than merely present:
	///
	///   under 10            Low
	///   10 to 19            Medium
	///   20 and over         High
	///
	/// Threat while none of them are targeting you. The moment one is, it
	/// becomes Engagement, and it changes back the moment that stops - which
	/// is the single most reactive thing in this system, and the one most
	/// worth being able to see in a log.
	///
	/// Counted on a tick rather than every frame. It walks every discovered
	/// unit in the sector, which is cheap but not free, and nothing about a
	/// firefight's size changes in the time it takes to play two frames of
	/// music.
	/// </remarks>
	public sealed class MusicCombatDetector
	{
		private const float SampleInterval = 0.5f;

		/// <summary>
		/// How long a reading must hold before the music believes it.
		/// </summary>
		/// <remarks>
		/// THE FIRST RUN'S LOG WAS A LIST OF THIS BUG. It read:
		///
		///   interrupted 'Combat High Intensity Engagement Section 1'
		///       after 27s of 29s, then 'High Threat Section 1'
		///   interrupted 'Combat High Intensity Threat Section 1'
		///       after 13s of 96s, then 'High Engagement Section 1'
		///
		/// Threat and Engagement flip-flopping every ten to twenty-five
		/// seconds, each flip a hard interrupt and a fresh track. Not two
		/// systems - one system being told to change its mind far too often.
		///
		/// The cause is that "is anything targeting me" is instantaneous and
		/// genuinely does alternate - a pirate reacquires, another loses
		/// target, the player turns - and half a second of sampling catches
		/// every one of those. Music cannot respond to a value that changes
		/// every other frame without sounding like it is malfunctioning, so
		/// the reading has to be steadier than the world is.
		///
		/// Two seconds. Long enough that a couple of targeting swaps are
		/// absorbed, short enough that arriving at a real fight is still
		/// noticed promptly rather than felt late.
		///
		/// A pending reading is held, not sampled: if the reading reverts
		/// before it has settled, the timer restarts from the new value.
		/// Blending the two would give a music state that matches neither.
		/// </remarks>
		private const float StableSeconds = 2f;

		private readonly List<Unit> seen = new List<Unit>(64);
		private float lastSampleAt = -999f;

		private MusicCombatState pending;
		private float pendingSince = -999f;

		public MusicCombatState State { get; private set; }

		/// <summary>The last reading, and whether it is worth re-reading yet.</summary>
		public MusicCombatState Sample(bool force = false)
		{
			if (!force && Time.time - lastSampleAt < SampleInterval)
			{
				return State;
			}

			lastSampleAt = Time.time;

			EngineASX engine = EngineASX.Instance;

			if (engine == null || engine.LocalFaction == null || engine.ActiveSector == null)
			{
				State = default(MusicCombatState);
				return State;
			}

			Unit mine = engine.LocalPlayer == null || engine.LocalPlayer.Person == null
				? null
				: engine.LocalPlayer.Person.CurrentUnit;

			int count = 0;
			bool targeted = false;

			if (mine != null)
			{
				FactionIntel intel = engine.LocalFaction.Intel;

				if (intel != null)
				{
					intel.GetDiscoveredUnitIdsInSectorNonAlloc(engine.ActiveSector, seen);

					for (int i = 0; i < seen.Count; i++)
					{
						Unit other = seen[i];

						if (other == null || other.IsDestroyed || other.Faction == null)
						{
							continue;
						}

						if (other.Faction == engine.LocalFaction)
						{
							continue;
						}

						if (!other.IsHostileTo(mine))
						{
							continue;
						}

						count++;

						if (!targeted && IsShootingAt(other, mine))
						{
							targeted = true;
						}
					}
				}
			}

			MusicCombatState raw = new MusicCombatState
			{
				HostileCount = count,
				AnyTargetingPlayer = targeted,
				Intensity = IntensityFor(count, State.Intensity),
				Category = targeted ? "Engagement" : "Threat"
			};

			return Settle(raw);
		}

		/// <summary>
		/// Holds a reading until it has been steady for StableSeconds.
		/// </summary>
		/// <remarks>
		/// The count itself is NOT compared for settling. Only the category
		/// and the band are, because those are the two the music keys on -
		/// four hostiles becoming five is nothing, while being targeted or
		/// not is everything, and five hostiles becoming twenty is a
		/// different piece of music.
		/// </remarks>
		private MusicCombatState Settle(MusicCombatState raw)
		{
			bool differs = raw.Category != State.Category
				|| raw.Intensity != State.Intensity;

			if (!differs)
			{
				pending = raw;
				pendingSince = -999f;
				return State;
			}

			if (pending.Category != raw.Category || pending.Intensity != raw.Intensity)
			{
				// A different change than the one being held: restart the
				// clock rather than inheriting the previous one's progress.
				pending = raw;
				pendingSince = Time.realtimeSinceStartup;
			}

			if (Time.realtimeSinceStartup - pendingSince >= StableSeconds)
			{
				State = pending;
				pendingSince = -999f;
			}

			return State;
		}

		/// <summary>Whether this unit has the player's ship as its combat target.</summary>
		private static bool IsShootingAt(Unit other, Unit mine)
		{
			Person pilot = other.GetPilot();

			if (pilot == null || pilot.NpcPilot == null)
			{
				return false;
			}

			return pilot.NpcPilot.CombatTarget == mine;
		}

		/// <summary>
		/// The intensity band for a hostile count.
		/// </summary>
		/// <remarks>
		/// Under ten is low, ten to nineteen is medium, twenty and over is
		/// high. Zero and one both come out Low, which is deliberate: a
		/// single pirate is not an encounter, and making one pirate quieter
		/// than three would mean the music got *louder* as a fight got
		/// smaller.
		///
		/// A dead band, because a fight hovers on a boundary. Nineteen
		/// hostiles, one destroyed, nineteen again would otherwise cross the
		/// line every few seconds and restart the music each time - which is
		/// exactly what the first run's log showed, flipping between bands
		/// faster than the tracks themselves are long.
		///
		/// Climbing to a higher band needs the full threshold; dropping back
		/// out needs to fall two below it. That asymmetry is the point: it is
		/// easy to get INTO a big fight and easy to stay out of one, rather
		/// than a fight sitting on twenty flickering in and out of High.
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
		public static string IntensityFor(int hostileCount) => IntensityFor(hostileCount, null);
	}
}