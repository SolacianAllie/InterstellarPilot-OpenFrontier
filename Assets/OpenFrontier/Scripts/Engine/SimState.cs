namespace OpenFrontier.Engine
{
	/// <summary>
	/// One entity's simulated body — plain blittable doubles (threading rule
	/// 1: no strings, no managed objects, no registry access inside
	/// per-tick math). Doubles are WHY the pinned-origin architecture is
	/// free: the player's position in a system is EXACTLY (0,0,0) at all
	/// times — to move is to move the whole system around the player — so
	/// render offsets are simPos − playerSimPos, bounded by the scaled
	/// system radius: floats stay in their best-case precision range with
	/// zero mitigation machinery, and no snap/recenter events ever exist.
	/// </summary>
	public struct SimBody
	{
		public double PosX, PosY, PosZ;
		public double VelX, VelY, VelZ;
		public double RotX, RotY, RotZ, RotW; // quaternion, unit length

		/// <summary>The pinned player: exactly at the origin, always. The
		/// player's Unity transform is locked at the origin; rotation is
		/// free. Input feeds velocity into the sim; the world counter-moves.
		/// No Rigidbody flight — motion is kinematic (pinned decisions).</summary>
		public static SimBody PinnedPlayer => default;
	}

	/// <summary>The world's simulated state at one tick. Blittable by
	/// rule; render reads this, sim never reads render.</summary>
	public struct SimSnapshot
	{
		public long TickIndex;
		public double SimTime;
		public SimBody Player;
	}

	/// <summary>
	/// The sim/render decoupling — the pinned M1 deliverable: sim state
	/// lives in plain structs (doubles), render reads a snapshot, and the
	/// last TWO states are kept so interpolation CAN be added later.
	/// Whether we interpolate (smooth 60 fps for fast movers) is an M3
	/// decision; the decoupling itself must exist at M1. Two slots,
	/// swapped on write, zero allocations.
	/// </summary>
	public sealed class SimStateBuffer
	{
		private readonly SimSnapshot[] slots = new SimSnapshot[2];
		private int writeIndex;
		private bool hasAny;

		/// <summary>Sim writes at the end of a tick (Housekeeping side of
		/// the machine); render reads between ticks.</summary>
		public void Write(in SimSnapshot snapshot)
		{
			slots[writeIndex] = snapshot;
			writeIndex ^= 1;
			hasAny = true;
		}

		public bool HasSnapshot => hasAny;

		/// <summary>The most recent snapshot — what M1 renders.</summary>
		public ref readonly SimSnapshot Latest => ref slots[writeIndex ^ 1];

		/// <summary>The one before it — interpolation's other endpoint (M3).</summary>
		public ref readonly SimSnapshot Previous => ref slots[writeIndex];
	}
}
