using System;
using System.Collections.Generic;

namespace OpenFrontier.IP.Engine.FleetOrders
{
	/// <summary>Which order a player is asking for.</summary>
	/// <remarks>
	/// Game vocabulary rather than protocol vocabulary, and that distinction is
	/// the whole reason this file exists here and not next to the wire format.
	///
	/// The orders live in the game assembly, and the game assembly cannot
	/// reference the networking one - that would be a circle, and every other
	/// boundary in this project is a line drawn for exactly this reason. So the
	/// question "what is this player asking for" is asked in the game's own
	/// words, and the networking side answers it in its own.
	///
	/// One number rather than a type name, so that two builds either agree
	/// about what 7 is or refuse to talk.
	/// </remarks>
	public enum FleetOrderKind
	{
		MoveToSectorTarget = 1,
		MoveToSector = 2,
		DockAtTarget = 3,
		ReturnToBase = 4,
		EnterWormhole = 5,
		ClaimUnit = 6,
		CollectCargo = 7,
		Scavenge = 8,
		ExploreSector = 9,
		ProtectTarget = 10,
		RepairAtNearest = 11,
		RepairAt = 12,
		RearmAtNearest = 13,
		RearmAt = 14,
		TransportPassengers = 15,
		MergeWithFleet = 16,
		ManualTrade = 17,
		Patrol = 18,
		Stop = 19
	}

	/// <summary>A player's wish, named in numbers rather than objects.</summary>
	/// <remarks>
	/// Ids rather than references because this crosses to another machine.
	/// Both copies of a world already agree on ids - the client was handed the
	/// host's world rather than generating one of its own - so an id means the
	/// same thing on the far side.
	///
	/// Every field is plain data with no Unity type in it, so that an intent
	/// can be built and checked without a game running. That is what makes the
	/// orders testable at all.
	/// </remarks>
	public struct FleetOrderIntent
	{
		public FleetOrderKind Kind;

		/// <summary>The fleets to give it to, in the player's selection order.</summary>
		public List<int> FleetIds;

		/// <summary>Zero means "none", which is how the engine already says so.</summary>
		/// <remarks>
		/// Not a field initialiser, because the language level this compiles at
		/// does not allow one on a struct without a constructor it cannot have.
		/// Zero rather than -1 for the same reason: every id in a running world
		/// is well above it, so it cannot be mistaken for something real, and
		/// it matches how the engine already signals an absent reference.
		/// </remarks>
		public int TargetUnitId;
		public int TargetSectorId;
		public int TargetFleetId;
		public int CargoClassId;

		public float TargetX;
		public float TargetY;
		public float TargetZ;

		/// <summary>For rearm orders: how much of the equipment to use, 0 to 1.</summary>
		public float Parameter01;

		public bool Stack;
		public bool Loop;
		public bool Repeat;
	}

	/// <summary>
	/// How a client's fleet orders are carried out instead of applied.
	/// </summary>
	/// <remarks>
	/// Filled in by the networking layer; null in a game that has never heard
	/// of it. The orders ask it first every time, and when there is nothing
	/// to ask they carry on and do the work themselves, which is how
	/// singleplayer stays exactly as it was.
	///
	/// Returns true when the order has been handled elsewhere and the caller
	/// must stop. False means carry on as before.
	///
	/// An interface rather than a delegate because there are three arguments
	/// and a signature that reads at the call site; a lambda per order would
	/// be the same thing wearing a worse hat.
	/// </remarks>
	public interface IFleetOrderInterceptor
	{
		bool TryHandle(FleetOrderIntent intent);
	}

	public static class FleetOrderIntents
	{
		/// <summary>
		/// Installed by the networking layer. Left null otherwise.
		/// </summary>
		public static IFleetOrderInterceptor Interceptor { get; set; }

		/// <summary>
		/// Offers an order to whoever is listening. True means it was handled
		/// and nothing further should happen.
		/// </summary>
		/// <remarks>
		/// Written so that with nothing installed it costs a null check and
		/// changes nothing - not a branch that has to be right in every
		/// caller, and not a decision each order has to make for itself.
		/// </remarks>
		public static bool TrySend(FleetOrderIntent intent)
		{
			IFleetOrderInterceptor interceptor = Interceptor;

			if (interceptor == null)
			{
				return false;
			}

			try
			{
				return interceptor.TryHandle(intent);
			}
			catch (Exception)
			{
				// Answered as "not handled" so that an order still happens
				// locally rather than vanishing. On a client that leaves the
				// fleet moving on this machine and not on the host, which is
				// wrong and visible; the other answer leaves the fleet frozen
				// with nothing to show for it.
				return false;
			}
		}
	}
}
