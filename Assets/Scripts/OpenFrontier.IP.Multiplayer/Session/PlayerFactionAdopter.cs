using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Moves this player's own faction into the reserved player band.
	/// </summary>
	/// <remarks>
	/// A player is a player whether they are alone or in someone else's
	/// galaxy, so both are numbered from the same place. That was not true at
	/// first, where only a joiner's faction was: the host's came from the
	/// scenario, through the engine's counter, like any generated faction.
	/// The result was that "is this a player's?" answered differently for the
	/// person hosting and the person joining, in the same world, which is the
	/// worst version of that question to get wrong.
	///
	/// The cost is real and was accepted deliberately: ids are stored verbatim
	/// in saves, so a world saved before this loads with the player's faction
	/// at its old number and a stranger's where it should be. Given how much
	/// else has changed, a fresh save is the better outcome, and forcing one
	/// is honestly kinder than a world that half-loads.
	///
	/// Waits rather than hooking. A new game's faction does not exist until
	/// part-way through world creation, and the order of that is the engine's
	/// business rather than this component's. The test is simply "is my
	/// faction in the band yet", which is true in one world forever and false
	/// in none.
	/// </remarks>
	public sealed class PlayerFactionAdopter : MonoBehaviour
	{
		private const string ObjectName = "PlayerFactionAdopter";

		/// <summary>Raised once the faction has been moved, or not.</summary>
		public static event System.Action<Faction> Adopted;

		/// <summary>
		/// Installed at startup, for every player, hosting or not.
		/// </summary>
		/// <remarks>
		/// This used to be installed when hosting started, on the reasoning
		/// that it only mattered with someone else in the session. That was
		/// wrong, and visibly so: a player alone kept an engine-numbered
		/// faction, and the first time they joined a session their faction
		/// changed number under them. A player's faction number cannot depend
		/// on whether a second copy of the game happens to be running.
		///
		/// A runtime initialize hook rather than a call from the host service,
		/// because this has to happen in a game the player never opens the
		/// multiplayer menu for, and because the engine cannot reference this
		/// assembly to do it from the other side.
		/// </remarks>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Install()
		{
			EnsureInstalled();
		}

		/// <summary>Installs the adopter if it is not already there.</summary>
		public static void EnsureInstalled()
		{
			if (Instance != null)
			{
				return;
			}

			GameObject host = new GameObject(ObjectName);
			host.hideFlags = HideFlags.HideAndDontSave;
			DontDestroyOnLoad(host);
			host.AddComponent<PlayerFactionAdopter>();
		}

		private static PlayerFactionAdopter Instance { get; set; }

		private void Awake()
		{
			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		private void Update()
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null || engine.World == null)
			{
				return;
			}

			Faction mine = engine.LocalFaction;

			if (mine == null || PlayerIdScheme.IsPlayerMade(mine.UniqueId))
			{
				// Either there is no faction yet or it is already ours, and in
				// both cases there is nothing to do on any later frame.
				return;
			}

			int wanted = PlayerIdScheme.FactionIdFor(PlayerIdentity.Token);

			if (wanted < 0 || engine.ContainsFactionById(wanted))
			{
				return;
			}

			if (!engine.ReassignFactionId(mine, wanted))
			{
				return;
			}

			Debug.Log("Multiplayer: this player's faction moved to the reserved band as "
				+ wanted + " (was " + mine.UniqueId + ")");

			Debug.Log("DiagC after band adoption id=" + mine.UniqueId
				+ " name='" + mine.Name + "' generatedNameId=" + mine.GeneratedNameId);

			Adopted?.Invoke(mine);
		}
	}
}