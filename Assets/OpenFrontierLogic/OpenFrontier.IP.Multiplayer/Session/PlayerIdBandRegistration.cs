using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Tells the engine which ids belong to players, before any world exists.
	/// </summary>
	/// <remarks>
	/// A separate file from <see cref="PlayerIdScheme"/> on purpose. That class
	/// is the part of the scheme that can be reasoned about without a game, and
	/// the offline tests are built around it running on its own. Giving it a
	/// dependency on the engine to satisfy an interface would cost that, and
	/// the isolation is what is being bought here: it is the reason the tests
	/// could not see that the engine was handing out player-band ids to
	/// ordinary AI for weeks.
	///
	/// So the scheme stays pure and this adapts it.
	///
	/// Registered before the first scene loads, so it covers every world the
	/// player loads from then on - including a save written before multiplayer
	/// was ever switched on, whose id counters may already be anywhere.
	/// </remarks>
	public static class PlayerIdBandRegistration
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void AnnounceReservedBands()
		{
			ReservedIds.Policy = new PlayerBandPolicy();
			PlayerFactions.Test = new PlayerBandFactionTest();
			PlayerFactions.TreatEveryFactionAsAi = false;

			Debug.Log("Multiplayer: the engine will not hand out ids in the player"
				+ " bands - factions " + PlayerIdScheme.FactionIdBase + "-"
				+ (PlayerIdScheme.FactionIdBase + PlayerIdScheme.FactionIdBand - 1)
				+ ", ships " + PlayerIdScheme.UnitIdBase + "-"
				+ (PlayerIdScheme.UnitIdBase + PlayerIdScheme.UnitIdBand - 1)
				+ "; a faction in that band is drawn as a player's");
		}

		private sealed class PlayerBandPolicy : IReservedIdPolicy
		{
			public bool IsReserved(int id)
			{
				return PlayerIdScheme.IsPlayerMade(id);
			}
		}

		/// <summary>
		/// Answers "is a person playing this faction" from the number alone.
		/// </summary>
		/// <remarks>
		/// The band is the whole mechanism, and it works on every machine for
		/// free. Both copies of a world hold the same factions with the same
		/// ids - one is a save of the other - so both agree that 900380616 is
		/// somebody's without being told, and without a message, and without
		/// either side keeping a list that could fall out of step.
		///
		/// That is why this is not the host's joiner table. The host knows who
		/// has joined; the client does not, and would have to be told. A
		/// number that already says it needs neither.
		/// </remarks>
		private sealed class PlayerBandFactionTest : IPlayerFactionTest
		{
			public bool IsPlayerFaction(int factionId)
			{
				return PlayerIdScheme.IsPlayerMadeFaction(factionId);
			}
		}
	}
}
