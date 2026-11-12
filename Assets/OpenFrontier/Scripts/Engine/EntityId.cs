using System.Collections.Generic;

namespace OpenFrontier.Engine
{
	/// <summary>Entity kinds — the top byte of every 64-bit id, so an id
	/// self-describes in a debugger and save validation routes by kind.
	/// Grows as content kinds arrive.</summary>
	public enum EntityKind : byte
	{
		None = 0,
		Unit = 1,
		Faction = 2,
		Fleet = 3,
		Poi = 4,
		System = 5,
		JumpLane = 6,
		Wormhole = 7,
		Effect = 8,
	}

	/// <summary>Where an id came from — the top NIBBLE of the 56-bit
	/// payload. The bands are physical: no allocator can walk its counter
	/// into another band, which retires the legacy trap where a reassign
	/// dragged an int counter through the reserved player band and every
	/// later faction was numbered inside it. (The legacy 800M/900M int
	/// bands are retired with the legacy scheme; IsPlayerMade below is the
	/// fresh test.)</summary>
	public enum EntitySource : byte
	{
		/// <summary>Nibble 0x0–0xC: host-authority counter per kind,
		/// persistent, saved, never recycled.</summary>
		Runtime = 0x0,

		/// <summary>Nibble 0xD: recycled every session, NEVER saved —
		/// projectiles, effects, anything a load must not resurrect.</summary>
		Ephemeral = 0xD,

		/// <summary>Nibble 0xE: pure integer function of (seed, index) —
		/// identical on every machine, nothing to sync, nothing stored.</summary>
		SeedDerived = 0xE,

		/// <summary>Nibble 0xF: fixed constants near the kind's top —
		/// "the player faction lives at a known address".</summary>
		Player = 0xF,
	}

	/// <summary>
	/// The fresh 64-bit, kind-tagged entity id scheme (pinned):
	/// 0 = invalid/none; top byte = kind; the 56-bit payload's top nibble =
	/// source band. Runtime ids come from a per-kind counter starting just
	/// above the kind's base (the host allocates — MP-safe by construction,
	/// clients don't simulate); ephemeral ids RECYCLE and are never saved;
	/// seed-derived ids are integer-only math (threading rule 2 — Burst
	/// floats are not bitwise-portable, hashes must match everywhere);
	/// player things are fixed constants. Allocation is MAIN-THREAD ONLY:
	/// ids are authority decisions, parallel work within a phase never
	/// allocates them.
	/// </summary>
	public static class EntityId
	{
		public const long None = 0;

		private const int KindShift = 56;
		private const long PayloadMask = 0x00FF_FFFF_FFFF_FFFF;
		private const int SourceShift = 52;
		private const long SourceMask = 0xFL << SourceShift;
		private const long DataMask = PayloadMask & ~SourceMask;

		// Runtime band: source nibbles 0x0–0xC, data in the low 52 bits
		// (payload ≤ 0xC_FFFF_FFFF_FFFF keeps the top nibble at 0).
		private const long RuntimeBandMax = 0xC_FFFF_FFFF_FFFF;

		// Ephemeral band: source nibble 0xD, recycled counter in the low 52 bits.
		private const long EphemeralBandMax = DataMask;

		// Seed-derived layout: seed (32 bits) << 20 | local index (20 bits —
		// up to ~1M places of one kind per system).
		private const int SeedShift = 20;
		private const int IndexBits = 20;
		private const int IndexMask = (1 << IndexBits) - 1;

		/// <summary>The player faction's fixed address — the fresh scheme's
		/// "known address"; no legacy numbers to honor.</summary>
		public static readonly long PlayerFactionId = PlayerId(EntityKind.Faction, 1);

		// Per-kind allocator state. Plain managed state — nothing Unity-
		// native here, so no field-initializer trap.
		private static readonly long[] runtimeNext = new long[16];
		private static readonly long[] ephemeralNext = new long[16];
		private static readonly Stack<long>[] ephemeralFree = new Stack<long>[16];

		/// <summary>Next persistent runtime id for a kind. Never recycled;
		/// saves key on these; the host allocates and clients receive.</summary>
		public static long NewRuntimeId(EntityKind kind)
		{
			int k = KindIndex(kind);
			long payload = ++runtimeNext[k];
			if (payload > RuntimeBandMax)
			{
				// Physically unreachable at any conceivable scale; the guard
				// exists so a future bug announces itself instead of
				// silently walking into the ephemeral band.
				UnityEngine.Debug.LogError($"[EntityId] {kind} runtime counter exhausted its band at {payload}.");
				return None;
			}

			return Base(kind) | payload;
		}

		/// <summary>Next ephemeral id for a kind — recycled, never saved.
		/// Pair every allocation with RecycleEphemeral, or effects leak.</summary>
		public static long NewEphemeralId(EntityKind kind)
		{
			int k = KindIndex(kind);
			Stack<long> free = ephemeralFree[k];
			long payload;
			if (free != null && free.Count > 0)
			{
				payload = free.Pop();
			}
			else
			{
				// Source nibble composed through SourceShift like every other
				// band — a hand-written hex literal here was once 16× too
				// large (0xD at bit 60, inside the KIND byte), which the
				// compiler caught and IsEphemeral would have silently
				// rejected. Payload = 0xD << 52 | counter.
				payload = (0xDL << SourceShift) | ++ephemeralNext[k];
				if ((payload & DataMask) > EphemeralBandMax)
				{
					UnityEngine.Debug.LogError($"[EntityId] {kind} ephemeral counter exhausted its band at {payload & DataMask}.");
					return None;
				}
			}

			return Base(kind) | payload;
		}

		/// <summary>Returns an ephemeral id to its kind's free list.</summary>
		public static void RecycleEphemeral(long id)
		{
			if (!IsEphemeral(id))
			{
				UnityEngine.Debug.LogError($"[EntityId] RecycleEphemeral on a non-ephemeral id {id} — refusing.");
				return;
			}

			int k = KindIndex(KindOf(id));
			if (ephemeralFree[k] == null)
			{
				ephemeralFree[k] = new Stack<long>();
			}

			ephemeralFree[k].Push(id & DataMask);
		}

		/// <summary>Seed-derived id: pure integer math, identical on every
		/// machine, nothing to sync for MP, nothing stored. The derivation
		/// seed is 32-bit (a 64-bit world seed derives per-system 32-bit
		/// seeds upstream — generation's business, not id math's).</summary>
		public static long SeedDerivedId(EntityKind kind, uint seed, int localIndex)
		{
			if (localIndex < 0 || localIndex > IndexMask)
			{
				UnityEngine.Debug.LogError($"[EntityId] Seed-derived index {localIndex} outside 0..{IndexMask} for {kind}.");
				return None;
			}

			return Base(kind) | (0xEL << SourceShift) | ((long)seed << SeedShift) | (uint)localIndex;
		}

		/// <summary>Player-band constant address — fixed, never counters.</summary>
		public static long PlayerId(EntityKind kind, ushort slot)
		{
			return Base(kind) | (0xFL << SourceShift) | slot;
		}

		public static EntityKind KindOf(long id)
		{
			return id <= 0 ? EntityKind.None : (EntityKind)((ulong)id >> KindShift);
		}

		public static bool IsValid(long id)
		{
			return id > 0 && KindOf(id) != EntityKind.None;
		}

		/// <summary>The save-side gate: ephemeral ids must never reach a
		/// save (they are recycled within the session). Reject loudly.</summary>
		public static bool IsEphemeral(long id)
		{
			return id > 0 && (id & SourceMask) == (0xDL << SourceShift);
		}

		/// <summary>The player-made test: ids in the fixed player band.
		/// (The legacy 800M/900M int-band test died with that scheme.)</summary>
		public static bool IsPlayerMade(long id)
		{
			return id > 0 && (id & SourceMask) == (0xFL << SourceShift);
		}

		private static long Base(EntityKind kind)
		{
			return (long)kind << KindShift;
		}

		private static int KindIndex(EntityKind kind)
		{
			return (int)kind & 0xF;
		}
	}
}
