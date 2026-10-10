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

			Debug.Log("Multiplayer: the engine will not hand out ids in the player"
				+ " bands - factions " + PlayerIdScheme.FactionIdBase + "-"
				+ (PlayerIdScheme.FactionIdBase + PlayerIdScheme.FactionIdBand - 1)
				+ ", ships " + PlayerIdScheme.UnitIdBase + "-"
				+ (PlayerIdScheme.UnitIdBase + PlayerIdScheme.UnitIdBand - 1));
		}

		private sealed class PlayerBandPolicy : IReservedIdPolicy
		{
			public bool IsReserved(int id)
			{
				return PlayerIdScheme.IsPlayerMade(id);
			}
		}
	}
}
