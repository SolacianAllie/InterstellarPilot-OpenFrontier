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

		private readonly List<Unit> seen = new List<Unit>(64);
		private float lastSampleAt = -999f;

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

			State = new MusicCombatState
			{
				HostileCount = count,
				AnyTargetingPlayer = targeted,
				Intensity = IntensityFor(count),
				Category = targeted ? "Engagement" : "Threat"
			};

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
		/// </remarks>
		public static string IntensityFor(int hostileCount)
		{
			if (hostileCount >= 20)
			{
				return "High";
			}

			return hostileCount >= 10 ? "Medium" : "Low";
		}
	}
}