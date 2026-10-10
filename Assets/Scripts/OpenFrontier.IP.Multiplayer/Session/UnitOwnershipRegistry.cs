using System;
using System.Collections.Generic;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Who owns which ship, according to the host.
	/// </summary>
	/// <remarks>
	/// The one place that answer is decided. Free of engine types on purpose:
	/// ownership is a disagreement between players, and the interesting cases
	/// are two people reaching for the same hull at the same moment, which is
	/// worth being able to test rather than only observe.
	///
	/// Unowned means the host has it, which is why there is no separate
	/// "host owns everything" list - it is simply the absence of an entry, and
	/// the host simulating a ship nobody has claimed needs no bookkeeping.
	/// </remarks>
	public sealed class UnitOwnershipRegistry
	{
		private readonly Dictionary<int, string> owners = new Dictionary<int, string>();

		/// <summary>Raised whenever a ship's owner changes, for the host to broadcast.</summary>
		public event Action<int, string> OwnershipChanged;

		/// <summary>Ships currently held by a player rather than the host.</summary>
		public int ClaimedCount => owners.Count;

		/// <summary>Who owns this ship, or null if the host does.</summary>
		public string OwnerOf(int unitId)
		{
			return owners.TryGetValue(unitId, out string token) ? token : null;
		}

		/// <summary>Whether this ship belongs to that player. A null token means the host.</summary>
		public bool IsOwnedBy(int unitId, string token)
		{
			string owner = OwnerOf(unitId);

			return owner != null && owner == token;
		}

		/// <summary>
		/// Asks for a ship on behalf of a player.
		/// </summary>
		/// <param name="reason">Why it was refused, or null if it was granted.</param>
		/// <returns>False when somebody else already has it.</returns>
		public bool TryClaim(int unitId, string claimantToken, out string reason)
		{
			reason = null;

			if (string.IsNullOrEmpty(claimantToken))
			{
				reason = "no player token";
				return false;
			}

			string current = OwnerOf(unitId);

			if (current != null && current != claimantToken)
			{
				// The case this whole class exists for. Two players reaching
				// for one hull: the first one to be heard keeps it, and the
				// second is told who has it rather than being left to
				// discover the answer by watching their ship not move.
				reason = "already claimed";
				return false;
			}

			if (current == claimantToken)
			{
				// Already theirs. Not an error, and deliberately not a change:
				// a client re-asking every frame must not produce a
				// broadcast every frame.
				return true;
			}

			Set(unitId, claimantToken);
			return true;
		}

		/// <summary>Gives a ship back to the host.</summary>
		/// <returns>Whether anything changed.</returns>
		public bool Release(int unitId)
		{
			if (!owners.ContainsKey(unitId))
			{
				return false;
			}

			Set(unitId, null);
			return true;
		}

		/// <summary>
		/// Drops every ship a player held, for when they leave.
		/// </summary>
		/// <remarks>
		/// Necessary rather than tidy. A ship left owned by someone who has
		/// gone would never be handed to anybody else again, because every
		/// later claim would hit "already claimed" against a player who can
		/// never release it. One dead connection would strand their ship in
		/// the world permanently.
		/// </remarks>
		public List<int> ReleaseAllFor(string token)
		{
			var released = new List<int>();

			if (string.IsNullOrEmpty(token))
			{
				return released;
			}

			// Collected first: raising the change event while walking the
			// dictionary being changed is how a dictionary throws.
			foreach (KeyValuePair<int, string> entry in owners)
			{
				if (entry.Value == token)
				{
					released.Add(entry.Key);
				}
			}

			for (int i = 0; i < released.Count; i++)
			{
				Set(released[i], null);
			}

			return released;
		}

		private void Set(int unitId, string token)
		{
			if (token == null)
			{
				owners.Remove(unitId);
			}
			else
			{
				owners[unitId] = token;
			}

			OwnershipChanged?.Invoke(unitId, token ?? string.Empty);
		}
	}
}