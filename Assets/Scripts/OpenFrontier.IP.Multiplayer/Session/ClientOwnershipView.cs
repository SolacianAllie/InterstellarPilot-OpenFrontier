using System.Collections.Generic;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// What this client believes about who owns what.
	/// </summary>
	/// <remarks>
	/// Optimistic on purpose, and that is the whole design.
	///
	/// The alternative - waiting for the host to say yes before letting the
	/// player fly - costs a round trip of latency on the one action that
	/// must feel instant, and it makes the ship lurch when the answer
	/// arrives. Taking the ship immediately and asking afterwards has no such
	/// moment, because during the wait the host is still simulating the ship
	/// itself and what it broadcasts for that ship is exactly what the AI
	/// would be doing with it. "The AI has it" is a state this game is in
	/// constantly and looks entirely natural, so the latency hides inside it.
	///
	/// Which gives the rule this class exists to express: the host can take
	/// authority away at any moment, and a client believes it owns a ship
	/// from the instant it asks until the host says otherwise. Nothing else
	/// has to be right for the wait to be invisible.
	///
	/// Engine-free so the transitions can be tested. They are the easy ones to
	/// get subtly wrong - a claim that is re-sent every frame, or a refusal
	/// that clears the wrong entry - and both would show up as a ship that
	/// will not move or one that belongs to two people.
	/// </remarks>
	public sealed class ClientOwnershipView
	{
		private readonly HashSet<int> requested = new HashSet<int>();
		private readonly HashSet<int> confirmed = new HashSet<int>();

		/// <summary>Ships this client has asked for and not been refused.</summary>
		public int RequestedCount => requested.Count;

		/// <summary>Ships the host has confirmed, and so broadcasts about no longer.</summary>
		public int ConfirmedCount => confirmed.Count;

		/// <summary>
		/// Whether this client will simulate the ship rather than obey the host.
		/// </summary>
		/// <remarks>
		/// True from the moment it asks, not from the moment it is confirmed.
		/// That is the entire point of the class.
		/// </remarks>
		public bool BelongsToMe(int unitId) => requested.Contains(unitId);

		/// <summary>Whether the host has confirmed the claim.</summary>
		public bool IsConfirmed(int unitId) => confirmed.Contains(unitId);

		/// <summary>
		/// Asks for a ship.
		/// </summary>
		/// <returns>True the first time only, so the caller sends once.</returns>
		public bool Request(int unitId)
		{
			return requested.Add(unitId);
		}

		/// <summary>
		/// Applies the host's answer, judged against this client's own token.
		/// </summary>
		/// <remarks>
		/// The comparison is the whole of it. A token that is our own means
		/// the claim stood; anything else - the host, or another player -
		/// means it did not, and this client must stop simulating that ship
		/// at once, or the next message to arrive will visibly snatch it out
		/// from under the player.
		/// </remarks>
		public void ApplyAssignment(int unitId, string ownerToken, string myToken)
		{
			if (!string.IsNullOrEmpty(ownerToken) && ownerToken == myToken)
			{
				confirmed.Add(unitId);
				return;
			}

			requested.Remove(unitId);
			confirmed.Remove(unitId);
		}

		/// <summary>Gives a ship back, because it went onto autopilot.</summary>
		public void Release(int unitId)
		{
			requested.Remove(unitId);
			confirmed.Remove(unitId);
		}

		/// <summary>Forgets everything, for when a join ends.</summary>
		public void Clear()
		{
			requested.Clear();
			confirmed.Clear();
		}
	}
}