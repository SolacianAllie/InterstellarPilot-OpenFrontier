using System;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Replies for when a player tries to join their own session.
	/// </summary>
	/// <remarks>
	/// Randomly chosen from a pool rather than one fixed line. The mistake is
	/// harmless and obvious the moment it is made - the code is on their screen
	/// - so the response is there to be light about it rather than to explain
	/// anything. A single fixed line would read as a scolding, which is the wrong
	/// register for "you pasted your own code".
	///
	/// This is a purely local check, on the machine doing the pasting. The host
	/// cannot make the same call: every joiner in a session presents the same
	/// secret, because it is the session's secret and it travels inside the
	/// code. There is nothing in a handshake that distinguishes the host from
	/// anyone else holding the code, so a host that tried would refuse every
	/// real player too.
	///
	/// Kept in the multiplayer assembly rather than as a localisation table:
	/// these are jokey one-offs rather than game text, and pulling the whole
	/// localisation system into this assembly to hold them would defeat the
	/// point of it being a separable folder.
	/// </remarks>
	public static class SelfJoinReplies
	{
		private static readonly string[] Lines =
		{
			"You can't clone yourself that easily, you know.",
			"Need a Hug?",
			"That's your own ship. Try not to crash into it.",
			"Impressive optimism, but that is you.",
			"You are already here. Present company, etc.",
			"Bold. It's your own code.",
			"Sorry, the transporter can only handle one of you.",
			"You have met yourself. He is doing well.",
			"Cannot join self. Would not even if I could.",
			"That is your own join code. Really.",
			"You have already got a seat. It is this one.",
			"A loop of one. Very zen, very lonely.",
			"You cannot be your own wingman.",
			"The nebula looks the same from both ends, I promise.",
			"Trying to play both sides? Both sides is you."
		};

		/// <summary>Number of replies available.</summary>
		public static int Count => Lines.Length;

		/// <summary>
		/// The reply at an index, for callers that want to choose for themselves.
		/// </summary>
		public static string At(int index)
		{
			return Lines[index];
		}

		/// <summary>
		/// Picks one of the replies, chosen afresh each call.
		/// </summary>
		/// <remarks>
		/// Draws from a <see cref="System.Random"/> of its own rather than
		/// UnityEngine.Random. Two reasons: a shared RNG would couple a joke to
		/// whatever else in the frame had been drawn, and a static System.Random
		/// is itself a contention point if this ever ends up on more than one
		/// thread. Unity's is not usable outside the engine at all, which is
		/// what stops the pool being exercised by the test suite.
		/// </remarks>
		public static string Pick()
		{
			return Lines[rng.Next(Lines.Length)];
		}

		private static readonly System.Random rng = new System.Random();
	}
}
