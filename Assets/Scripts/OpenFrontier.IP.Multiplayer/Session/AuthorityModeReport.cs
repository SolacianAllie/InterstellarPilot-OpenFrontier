using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Says, once per world, whether this copy is running the game or
	/// following it.
	/// </summary>
	/// <remarks>
	/// Exists because the question "is the authority gate working?" otherwise
	/// has no answer in the mode where it matters least. The running count of
	/// declined simulations only prints while connected to somebody, so a
	/// singleplayer game - where a gate that had leaked would hold the whole
	/// world still without a single error - shows nothing at all. Silence
	/// looks identical to correct.
	///
	/// So this answers it directly, and in both directions:
	///
	/// - Alone, it walks every ship in the world through the same decision the
	///   engine makes and prints how many would be declined. It must be zero.
	///   A non-zero number here is the whole migration broken, in the one mode
	///   where nobody would otherwise find out until their game appeared to
	///   hang.
	/// - In a session, it prints the same numbers for the ships that *are*
	///   being declined, which is what the periodic report then counts up to.
	///
	/// One line per world, not per frame, because a check that reports
	/// continuously is a check that gets ignored.
	/// </remarks>
	public sealed class AuthorityModeReport : MonoBehaviour
	{
		/// <summary>The name of the object this installs itself on.</summary>
		public const string ObjectName = "Multiplayer Authority Report";

		/// <summary>Installs the reporter if it is not already there.</summary>
		public static void EnsureInstalled()
		{
			if (Instance != null)
			{
				return;
			}

			GameObject host = new GameObject(ObjectName);
			host.hideFlags = HideFlags.HideAndDontSave;
			DontDestroyOnLoad(host);
			host.AddComponent<AuthorityModeReport>();
		}

		public static AuthorityModeReport Instance { get; private set; }

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
			EngineASX engine = EngineASX.Instance;

			// Reported once, and again only if the world is replaced under us.
			if (engine == null || engine.World == null || !engine.World.HasInitialised
				|| reportedWorld == engine.World)
			{
				return;
			}

			reportedWorld = engine.World;
			Report(engine);
		}

		private WorldBase reportedWorld;

		private static void Report(EngineASX engine)
		{
			List<Unit> units = engine.UpdatableUnits;

			int total = units == null ? 0 : units.Count;
			int declined = 0;

			if (units != null)
			{
				for (int i = 0; i < units.Count; i++)
				{
					Unit unit = units[i];

					if (unit != null && !Authority.OwnsUnit(unit))
					{
						declined++;
					}
				}
			}

			int factions = engine.Factions == null ? 0 : engine.Factions.Count;
			int factionsDeclined = 0;

			if (engine.Factions != null)
			{
				for (int i = 0; i < engine.Factions.Count; i++)
				{
					if (engine.Factions[i] != null && !Authority.OwnsFaction(engine.Factions[i]))
					{
						factionsDeclined++;
					}
				}
			}

			bool client = Authority.IsThisCopyAClient;

			string line = client
				? "Authority: CLIENT - following another player's game; would simulate "
					+ (total - declined) + " of " + total + " ship(s) and "
					+ (factions - factionsDeclined) + " of " + factions + " faction(s)"
				: "Authority: AUTHORITATIVE - this copy simulates all " + total
					+ " ship(s) and all " + factions + " faction(s)";

			if (!client && declined > 0)
			{
				line += " BUT " + declined + " WOULD BE DECLINED, WHICH IS WRONG"
					+ " - a world alone must never be held still";
			}

			Debug.Log(line);
			NetLog.Client(line);
		}
	}
}
