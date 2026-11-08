using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Tells the id scheme which player-unit ids the loaded world already
	/// holds, so it does not hand one out twice.
	///
	/// The scheme hands out ids from a reserved set that only knows what this
	/// session has done. A save does not consult it, and a save made while
	/// somebody was connected contains the shuttle that was made for them,
	/// sitting on an id in the band. So the next session, with an empty set,
	/// gives the same id to the next person to join - and the world ends up
	/// with two units of one id.
	///
	/// That fails quietly and far away. The exporter keys units by id and
	/// throws on the duplicate, the host cannot send its world, and the joiner
	/// waits for a snapshot that will never arrive. Nothing anywhere says
	/// "two units share an id"; the joiner just never turns up.
	///
	/// Runs once the world is up, for every player including the host, because
	/// this has nothing to do with multiplayer being switched on: a player
	/// alone who loads a save and then has somebody join would hit it too.
	/// </summary>
	public sealed class PlayerUnitIdSeeder : MonoBehaviour
	{
		/// <summary>The name of the object this installs itself on.</summary>
		public const string ObjectName = "Multiplayer Player Unit Id Seeder";

		private bool seeded;

		/// <summary>Installs the seeder if it is not already there.</summary>
		public static void EnsureInstalled()
		{
			if (Instance != null)
			{
				return;
			}

			GameObject host = new GameObject(ObjectName);
			host.hideFlags = HideFlags.HideAndDontSave;
			DontDestroyOnLoad(host);
			host.AddComponent<PlayerUnitIdSeeder>();
		}

		public static PlayerUnitIdSeeder Instance { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Install()
		{
			EnsureInstalled();
		}

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
			// Waited for rather than seeded on the first frame: the seeder is
			// installed at startup and the world does not exist yet, and a
			// world with no units in it would seed nothing and look like a
			// save with no player ships in it.
			if (seeded)
			{
				return;
			}

			EngineASX engine = EngineASX.Instance;

			if (engine == null || engine.World == null || !engine.World.HasInitialised)
			{
				return;
			}

			seeded = true;
			Seed(engine);
		}

		/// <summary>Reserves every player-unit id the world already holds.</summary>
		public static int Seed(EngineASX engine)
		{
			int found = 0;

			// Forgiven: a world mid-load is exactly when this is most likely
			// to be called, and a failure here means the duplicate-key export
			// error further down, which says nothing about its cause.
			try
			{
				List<Unit> units = engine.UpdatableUnits;

				if (units == null)
				{
					return 0;
				}

				for (int i = 0; i < units.Count; i++)
				{
					Unit unit = units[i];

					if (unit != null && PlayerIdScheme.IsPlayerMadeUnit(unit.UniqueId))
					{
						PlayerIdScheme.Reserve(unit.UniqueId);
						found++;
					}
				}
			}
			catch (System.Exception e)
			{
				Debug.LogWarning("Multiplayer: could not read the world's existing player"
					+ " ship ids - " + e.Message + ". If a player cannot join because the"
					+ " host cannot export its world, this is why.");
			}

			if (found > 0)
			{
				Debug.Log("Multiplayer: this world already holds " + found
					+ " player ship(s), so their ids will not be handed out again.");
			}

			return found;
		}

		/// <summary>
		/// Starts over, for when the world under us has been replaced.
		/// </summary>
		public static void Forget()
		{
			PlayerIdScheme.ForgetUnitIds();

			if (Instance != null)
			{
				Instance.seeded = false;
			}
		}
	}
}