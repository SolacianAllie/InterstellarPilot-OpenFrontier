using System;
using System.Collections.Generic;
using UnityEngine;
using Discord.Sdk;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.Discord
{
	/// <summary>
	/// Discord Rich Presence for Open Frontier, powered by the
	/// Discord Social SDK (com.discord.partnersdk).
	///
	/// Two modes:
	///
	/// - Direct RPC (default): no authorization whatsoever. Naming
	///   the application is enough for the SDK to publish presence
	///   straight to the running Discord client (the Discord app on
	///   Android, SDK 1.10+). No prompt, no stored tokens, no portal
	///   OAuth configuration - presence simply appears whenever a
	///   Discord client is available.
	///
	/// - Authenticated: the SDK's device authorization flow, which
	///   shows presence even when no Discord client is running. The
	///   player authorizes the app once (tokens are stored in
	///   PlayerPrefs and refreshed silently afterwards), which
	///   requires the redirect URLs and "public client" setting on
	///   the Discord application.
	///
	/// Authentication, when enabled, runs as a chain with a
	/// watchdog: stored refresh token -> stored access token ->
	/// interactive re-prompt. Each stage that fails (including a
	/// stored token the Discord server has rejected, which only
	/// shows up as an async socket drop) advances to the next
	/// stage, so a player who loses their data, revokes the app,
	/// or switches accounts is re-prompted instead of silently
	/// losing presence. A declined prompt is not nagged - the
	/// chain gives up for the launch and re-prompts next time.
	///
	/// The SDK pumps its own callbacks through the Unity player
	/// loop (see NativeMethods' static constructor in the
	/// package), so every callback below already runs on the
	/// main thread.
	/// </summary>
	public class DiscordPresenceManager : MonoBehaviour
	{
		[Tooltip("Discord application configuration (the Application ID from the Discord Developer Portal). Presence stays offline while this is unset.")]
		[SerializeField]
		private DiscordPresenceConfig config;

		[Tooltip("Direct mode: publish presence straight to the running Discord client over RPC, with no account authorization at all - no login prompt, no stored tokens, no portal OAuth setup. Presence appears whenever Discord is open (on Android, whenever the Discord app is installed and signed in). Disable to use the authenticated flow instead, which shows presence even with no Discord client running, but asks the player to authorize the app once.")]
		[SerializeField]
		private bool useDirectRpc = true;

		[Tooltip("Attempt to connect on startup. Players can opt out by setting PlayerPrefs 'DiscordPresenceEnabled' to 0.")]
		[SerializeField]
		private bool connectOnStartup = true;

		[Tooltip("How often the presence is pushed to Discord, in seconds. This is just a keepalive - it must stay at or below the rotate interval, or cards would be skipped.")]
		[SerializeField]
		private float updateInterval = 5f;

		[Tooltip("How often the presence rotates to the next piece of information, in seconds. The ship icon and its tooltip stay put; only the two text lines rotate, cycling through the ship, the sector, the treasury and any scenario briefing the game has data for.")]
		[SerializeField]
		private float rotateInterval = 10f;

		[Tooltip("Activity asset name for the large image, exactly as uploaded to the application's Rich Presence assets in the Discord Developer Portal.")]
		[SerializeField]
		private string largeImageAsset = "openfrontier";

		[Tooltip("Optional activity asset name used when the player's unit has no mapped asset of its own. Leave empty to show no small image at all.")]
		[SerializeField]
		private string fallbackImageAsset = "";

		[Tooltip("Optional first presence button label (with its URL below). Leave either empty to show no buttons.")]
		[SerializeField]
		private string buttonOneLabel = "";

		[SerializeField]
		private string buttonOneUrl = "";

		[Tooltip("Optional second presence button label (with its URL below).")]
		[SerializeField]
		private string buttonTwoLabel = "";

		[SerializeField]
		private string buttonTwoUrl = "";

		private const string EnabledKey = "DiscordPresenceEnabled";
		private const string AccessTokenKey = "DiscordAccessToken";
		private const string RefreshTokenKey = "DiscordRefreshToken";

		// The name Discord shows above the presence. SetName
		// overrides the application name registered in the portal,
		// so this is what players actually see.
		private const string GameDisplayName = "InterstellarPilot: Open Frontier";

		// Presence updates are throttled; a failure is logged once
		// so a persistent Discord outage can't flood the console.
		private const float MinUpdateInterval = 5f;

		// How long a token handshake may take before the auth
		// chain assumes it failed and advances.
		private const float TokenConnectTimeout = 10f;

		// How long the authorization prompt may go unanswered
		// (the player has to approve it in Discord) before the
		// chain gives up for this launch.
		private const float AuthorizationPromptTimeout = 120f;

		// Minimum spacing between silent recovery attempts while
		// the game runs, so a dead connection can't spam the
		// auth chain (or the re-prompt) every few seconds.
		private const float RecoveryCooldown = 60f;

		// Swapping ships, docking, jumping, entering or leaving the
		// menu has to be on screen at once rather than up to a whole
		// update interval later, so the state is polled this often and
		// any change pushes immediately.
		private const float StateCheckInterval = 0.5f;

		private enum AuthStage
		{
			Idle,
			TokenConnect,
			Prompting,
		}

		/// <summary>
		/// One "screen" of presence text. Discord gives us two short
		/// text lines, so instead of cramming everything into one
		/// static pair, the presence rotates through several of these
		/// - the ship you fly, where you are, what you own - and shows
		/// each for a while. Cards whose data isn't available (no
		/// description, no faction, menu instead of flight) are simply
		/// left out of the cycle.
		/// </summary>
		private class PresenceCard
		{
			public string Details;
			public string State;

			public PresenceCard(string details, string state)
			{
				Details = details;
				State = state;
			}
		}

		private const int MaxLineLength = 128;

		// How many cargo types fit on one card before the manifest
		// spills onto another card in the rotation.
		private const int TypesPerCargoCard = 3;

		private readonly List<PresenceCard> presenceCards = new List<PresenceCard>();
		private int presenceCardIndex;
		private float nextCardRotationTime;

		private Client client;
		private bool statusReady;
		private bool directRpcActive;
		private AuthStage authStage;
		private bool triedRefresh;
		private bool triedAccessToken;
		private bool triedPrompt;
		private float authStageDeadline = float.MaxValue;
		private float lastRecoveryAttempt = -RecoveryCooldown;
		private bool reportedPresenceFailure;
		private string pendingCodeVerifier;
		private float nextPresenceUpdate;
		private float nextStateCheckTime;
		private string lastStateSignature;
		private ulong sessionStartEpoch;

		/// <summary>True while the SDK reports the authenticated connection as Ready.</summary>
		public bool IsAuthorized => statusReady;

		/// <summary>True whenever presence can be published: the authenticated connection is Ready, or direct RPC mode is active.</summary>
		public bool IsPresenceLive => statusReady || directRpcActive;

		/// <summary>True when credentials from a previous authorization are stored on this device.</summary>
		public bool HasStoredCredentials => !string.IsNullOrEmpty(PlayerPrefs.GetString(AccessTokenKey, ""));

		private void Awake()
		{
			sessionStartEpoch = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			// The first push shows the opening card; rotation starts
			// one interval later so the hero card gets its full time.
			nextCardRotationTime = Time.time + Math.Max(updateInterval, rotateInterval);
		}

		private void Start()
		{
			if (!connectOnStartup || PlayerPrefs.GetInt(EnabledKey, 1) == 0)
			{
				return;
			}
			if (config == null || config.ApplicationId == 0UL)
			{
				Debug.LogWarning("[Discord] Rich Presence is not configured: set an Application ID in the DiscordPresenceConfig asset. Presence will stay offline.");
				return;
			}
			CreateClient();
			if (useDirectRpc)
			{
				StartDirectRpc();
			}
			else
			{
				BeginAuthentication();
			}
		}

		private void Update()
		{
			if (!directRpcActive)
			{
				UpdateAuthWatchdog();
			}
			if (client == null || (!statusReady && !directRpcActive))
			{
				return;
			}
			if (Time.time >= nextStateCheckTime)
			{
				nextStateCheckTime = Time.time + StateCheckInterval;
				CheckForStateChange();
			}
			if (Time.time >= nextPresenceUpdate)
			{
				nextPresenceUpdate = Time.time + Math.Max(MinUpdateInterval, updateInterval);
				if (Time.time >= nextCardRotationTime)
				{
					nextCardRotationTime = Time.time + Math.Max(updateInterval, rotateInterval);
					presenceCardIndex++;
				}
				PushPresence();
			}
		}

		/// <summary>
		/// Anything that changes what the presence should say - a
		/// different ship, docking, a jump, entering or leaving the
		/// menu, a rename - pushes immediately and restarts the
		/// rotation, so the new state leads with its most relevant card
		/// and holds it for a full interval.
		///
		/// The state is polled twice a second rather than evented:
		/// the engine object is recreated on every scenario change, so
		/// event subscriptions would need constant re-wiring, while a
		/// signature comparison survives all of it.
		/// </summary>
		private void CheckForStateChange()
		{
			string signature = BuildStateSignature();
			if (string.Equals(signature, lastStateSignature, StringComparison.Ordinal))
			{
				return;
			}
			lastStateSignature = signature;
			presenceCardIndex = 0;
			nextCardRotationTime = Time.time + Math.Max(updateInterval, rotateInterval);
			nextPresenceUpdate = Time.time;
		}

		private string BuildStateSignature()
		{
			EngineASX engine = EngineASX.Instance;
			if (engine == null || engine.World == null)
			{
				return "no-world";
			}
			Unit ship = GetPresenceUnit(engine);
			Sector sector = engine.ActiveSector;
			ScenarioInfo scenario = engine.World.ScenarioInfo;
			return string.Concat(
				engine.GetEntityId().ToString(), "|",
				IsInMainMenu() ? "menu" : "game", "|",
				ship != null ? ship.GetEntityId().ToString() : "-", "|",
				ship != null && ship.IsDocked ? "docked" : "-", "|",
				GetUnitName(ship), "|",
				sector != null ? sector.UniqueId.ToString() : "-", "|",
				scenario != null ? scenario.UniqueId.ToString() : "-");
		}

		// ------------------------------------------------------------------
		// Authentication chain
		// ------------------------------------------------------------------

		/// <summary>
		/// Direct RPC mode: no authorization at all. Naming the
		/// application is enough for the SDK to publish presence
		/// directly to the running Discord client (the Discord app
		/// on Android, SDK 1.10+) - nothing is prompted and nothing
		/// is stored, so there is no auth state to lose or recover.
		/// </summary>
		private void StartDirectRpc()
		{
			if (client == null)
			{
				return;
			}
			try
			{
				client.SetApplicationId(config.ApplicationId);
			}
			catch (Exception e)
			{
				Debug.LogWarning("[Discord] Direct RPC mode could not start: " + e.Message);
				return;
			}
			directRpcActive = true;
			client.IsDiscordAppInstalled(installed =>
			{
				if (installed)
				{
					Debug.Log("[Discord] Direct RPC mode - Rich Presence active (no authorization needed).");
				}
				else
				{
					Debug.Log("[Discord] No Discord client found - presence will appear once Discord is running.");
				}
			});
			PushPresence();
		}

		private void CreateClient()
		{
			if (client != null)
			{
				return;
			}
			try
			{
				client = new Client(new ClientCreateOptions());
				client.SetStatusChangedCallback(OnDiscordStatusChanged);
			}
			catch (Exception e)
			{
				client = null;
				Debug.LogWarning("[Discord] Failed to create the Social SDK client: " + e.Message);
			}
		}

		/// <summary>
		/// (Re)runs the authentication chain from the beginning,
		/// remembering which stages already failed this launch.
		/// </summary>
		private void BeginAuthentication()
		{
			triedRefresh = false;
			triedAccessToken = false;
			triedPrompt = false;
			AdvanceAuthentication();
		}

		/// <summary>
		/// Clears any stored credentials and authenticates again -
		/// for players who want to switch accounts or recover from
		/// a rejected app authorization.
		/// </summary>
		public void RestartAuthentication()
		{
			if (directRpcActive)
			{
				Debug.Log("[Discord] RestartAuthentication only applies to the authenticated flow - Direct RPC mode needs no authorization.");
				return;
			}
			PlayerPrefs.DeleteKey(AccessTokenKey);
			PlayerPrefs.DeleteKey(RefreshTokenKey);
			PlayerPrefs.Save();
			if (client != null)
			{
				try
				{
					client.Disconnect();
				}
				catch
				{
					// Re-authenticating - the old socket is irrelevant.
				}
			}
			statusReady = false;
			BeginAuthentication();
		}

		private void AdvanceAuthentication()
		{
			if (client == null)
			{
				return;
			}
			// Order: silent refresh, then the stored access
			// token, then the interactive re-prompt.
			if (!triedRefresh && HasStoredRefreshToken())
			{
				triedRefresh = true;
				RefreshAndConnect(
					PlayerPrefs.GetString(RefreshTokenKey, ""),
					AdvanceAuthentication);
				return;
			}
			if (!triedAccessToken && HasStoredCredentials)
			{
				triedAccessToken = true;
				ConnectWithStoredAccessToken();
				return;
			}
			if (!triedPrompt)
			{
				triedPrompt = true;
				StartDeviceAuthorization();
				return;
			}
			authStage = AuthStage.Idle;
			Debug.Log("[Discord] Authentication attempts exhausted; presence stays offline until the next launch.");
		}

		private static bool HasStoredRefreshToken()
		{
			return !string.IsNullOrEmpty(PlayerPrefs.GetString(RefreshTokenKey, ""));
		}

		private void RefreshAndConnect(string refreshToken, Action onFailure)
		{
			client.RefreshToken(config.ApplicationId, refreshToken,
				(result, accessToken, newRefreshToken, tokenType, expiresIn, scopes) =>
				{
					if (result.Successful() && !string.IsNullOrEmpty(accessToken))
					{
						StoreTokens(accessToken, newRefreshToken);
						ConnectWithAccessToken(accessToken);
					}
					else
					{
						onFailure?.Invoke();
					}
				});
		}

		private void ConnectWithStoredAccessToken()
		{
			client.UpdateToken(AuthorizationTokenType.Bearer,
				PlayerPrefs.GetString(AccessTokenKey, ""),
				result =>
				{
					if (result.Successful())
					{
						ConnectWithAccessToken(null);
					}
					else
					{
						AdvanceAuthentication();
					}
				});
		}

		/// <summary>
		/// Hands the (already stored) access token to the SDK and
		/// opens the socket. The token's validity only shows up
		/// asynchronously, so the watchdog watches for Ready - or
		/// a socket drop - and advances the chain on failure.
		/// </summary>
		private void ConnectWithAccessToken(string accessToken)
		{
			Action connect = () =>
			{
				client.Connect();
				authStage = AuthStage.TokenConnect;
				authStageDeadline = Time.time + TokenConnectTimeout;
			};
			if (!string.IsNullOrEmpty(accessToken))
			{
				// The socket must not open before the token is
				// set, so connect from inside the UpdateToken
				// callback (the SDK sample's pattern).
				client.UpdateToken(AuthorizationTokenType.Bearer, accessToken, _ => connect());
			}
			else
			{
				connect();
			}
		}

		private void StartDeviceAuthorization()
		{
			authStage = AuthStage.Prompting;
			authStageDeadline = Time.time + AuthorizationPromptTimeout;
			try
			{
				AuthorizationCodeVerifier verifier = client.CreateAuthorizationCodeVerifier();
				pendingCodeVerifier = verifier.Verifier();
				var args = new AuthorizationArgs();
				args.SetClientId(config.ApplicationId);
				args.SetScopes(Client.GetDefaultCommunicationScopes());
				args.SetCodeChallenge(verifier.Challenge());
				client.Authorize(args, OnAuthorizeCompleted);
			}
			catch (Exception e)
			{
				authStage = AuthStage.Idle;
				Debug.LogWarning("[Discord] Device authorization could not start: " + e.Message);
			}
		}

		private void OnAuthorizeCompleted(ClientResult result, string code, string redirectUri)
		{
			if (authStage == AuthStage.Prompting)
			{
				authStage = AuthStage.Idle;
				authStageDeadline = float.MaxValue;
			}
			if (client == null)
			{
				return;
			}
			if (!result.Successful())
			{
				// Declined or timed out: no nag this launch -
				// the chain restarts (and re-prompts) next time.
				Debug.Log("[Discord] Authorization not completed (" + result.Error() + "). Presence stays offline until the next launch.");
				return;
			}
			client.GetToken(config.ApplicationId, code, pendingCodeVerifier, redirectUri,
				(tokenResult, accessToken, refreshToken, tokenType, expiresIn, scopes) =>
				{
					if (tokenResult.Successful() && !string.IsNullOrEmpty(accessToken))
					{
						StoreTokens(accessToken, refreshToken);
						ConnectWithAccessToken(accessToken);
					}
					else
					{
						Debug.LogWarning("[Discord] Token exchange failed; presence stays offline.");
					}
				});
		}

		private void StoreTokens(string accessToken, string refreshToken)
		{
			PlayerPrefs.SetString(AccessTokenKey, accessToken);
			if (!string.IsNullOrEmpty(refreshToken))
			{
				PlayerPrefs.SetString(RefreshTokenKey, refreshToken);
			}
			PlayerPrefs.Save();
		}

		/// <summary>
		/// The chain's watchdog: a token handshake that neither
		/// reaches Ready nor drops within the timeout has failed,
		/// so the chain moves on (eventually to the re-prompt).
		/// </summary>
		private void UpdateAuthWatchdog()
		{
			if (authStage != AuthStage.Idle && Time.time >= authStageDeadline)
			{
				if (authStage == AuthStage.Prompting)
				{
					// The prompt went unanswered: give up for
					// this launch rather than nagging.
					authStage = AuthStage.Idle;
					authStageDeadline = float.MaxValue;
					Debug.Log("[Discord] Authorization prompt unanswered; presence stays offline until the next launch.");
				}
				else
				{
					AdvanceAuthentication();
				}
			}
		}

		private void OnDiscordStatusChanged(Client.Status status, Client.Error error, int errorDetail)
		{
			bool wasReady = statusReady;
			statusReady = (status == Client.Status.Ready);
			switch (status)
			{
			case Client.Status.Ready:
				authStage = AuthStage.Idle;
				authStageDeadline = float.MaxValue;
				Debug.Log("[Discord] Connected - Rich Presence active.");
				PushPresence();
				break;
			case Client.Status.Disconnected:
				if (authStage == AuthStage.TokenConnect)
				{
					// The stored token was rejected (or the
					// socket never came up): advance the chain.
					AdvanceAuthentication();
				}
				else if (wasReady && error != Client.Error.None
				         && Time.time - lastRecoveryAttempt > RecoveryCooldown)
				{
					// Was authorized and the connection dropped
					// (expired token, revoked app, network blip):
					// refresh silently first; on failure the full
					// chain runs, re-prompting if needed.
					lastRecoveryAttempt = Time.time;
					if (HasStoredRefreshToken())
					{
						RefreshAndConnect(
							PlayerPrefs.GetString(RefreshTokenKey, ""),
							AdvanceAuthentication);
					}
					else
					{
						AdvanceAuthentication();
					}
				}
				break;
			}
		}

		// ------------------------------------------------------------------
		// Presence
		// ------------------------------------------------------------------

		private void PushPresence()
		{
			if (client == null || (!statusReady && !directRpcActive))
			{
				return;
			}
			EngineASX engine = EngineASX.Instance;
			bool inMenu = IsInMainMenu();
			// The icon and tooltip follow whatever the player is in
			// or docked inside; the cargo manifest stays with the unit
			// they actually command, so a shuttle parked in a carrier
			// still shows its own freight.
			Unit presenceUnit = inMenu ? null : GetPresenceUnit(engine);
			Unit playerUnit = inMenu ? null : GetPlayerUnit(engine);
			Faction player = FindPlayerFaction(engine);
			Sector sector = engine != null ? engine.ActiveSector : null;
			string scenarioTitle = GetScenarioTitle(engine);

			BuildPresenceCards(inMenu, engine, playerUnit, player, sector, scenarioTitle);
			if (presenceCards.Count == 0)
			{
				return;
			}
			PresenceCard card = presenceCards[WrapIndex(presenceCardIndex, presenceCards.Count)];

			// What we just pushed IS the current state, so the
			// signature check doesn't immediately push again.
			lastStateSignature = BuildStateSignature();

			try
			{
				using (var activity = new Activity())
				{
					activity.SetName(GameDisplayName);
					activity.SetType(ActivityTypes.Playing);
					activity.SetSupportedPlatforms(ActivityGamePlatforms.Desktop | ActivityGamePlatforms.Android);
					activity.SetDetails(card.Details);
					activity.SetState(card.State);
					using (var assets = new ActivityAssets())
					{
						assets.SetLargeImage(largeImageAsset);
						assets.SetLargeText(string.IsNullOrEmpty(scenarioTitle)
							? GameDisplayName
							: scenarioTitle);
						// The small image is whatever the player is
						// actually piloting or manning: their ship,
						// their station, or a turret/satellite they
						// took the controls of.
						ApplyUnitImage(assets, presenceUnit,
							BuildPresenceTooltip(playerUnit, presenceUnit));
						activity.SetAssets(assets);
					}
					using (var timestamps = new ActivityTimestamps())
					{
						timestamps.SetStart(sessionStartEpoch);
						activity.SetTimestamps(timestamps);
					}
					AddButton(activity, buttonOneLabel, buttonOneUrl);
					AddButton(activity, buttonTwoLabel, buttonTwoUrl);
					client.UpdateRichPresence(activity, result =>					{
						if (result.Successful())
						{
							reportedPresenceFailure = false;
						}
						else if (!reportedPresenceFailure)
						{
							reportedPresenceFailure = true;
							Debug.Log("[Discord] Presence update failed: " + result.Error()
								+ (directRpcActive ? " (no Discord client running?)" : ""));
						}
					});
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("[Discord] Presence update threw: " + e.Message);
			}
		}

		/// <summary>
		/// Builds the rotation: every card the current game state can
		/// fill in, in reading order. Cards with no data are skipped,
		/// so a player in the menu simply cycles a single card.
		/// </summary>
		private void BuildPresenceCards(bool inMenu, EngineASX engine, Unit ship,
			Faction player, Sector sector, string scenarioTitle)
		{
			presenceCards.Clear();
			if (inMenu)
			{
				presenceCards.Add(new PresenceCard("Cruising the open frontier", "Main menu"));
				return;
			}

			int shipCount = CountShips(player);
			int fleetCount = player != null && player.Fleets != null ? player.Fleets.Count : 0;

			// What the player is carrying. (The unit itself is the
			// small image and its tooltip - the text never names it.)
			AddCargoCards(ship);

			// Where you are, and how friendly the neighbourhood is.
			if (sector != null)
			{
				var location = new List<string>();
				if (!string.IsNullOrEmpty(scenarioTitle))
				{
					location.Add(scenarioTitle);
				}
				location.Add(SecurityLabel(sector));
				int stations = sector.GetCountOfUnitType(UnitType.Station);
				if (stations > 0)
				{
					location.Add(stations.ToString("N0") + " station" + (stations == 1 ? "" : "s"));
				}
				int jumps = sector.JumpDistanceToNearestControlledSector;
				if (jumps > 0)
				{
					location.Add(jumps + (jumps == 1 ? " jump from the frontier" : " jumps from the frontier"));
				}
				presenceCards.Add(new PresenceCard("In " + sector.Name, string.Join(" - ", location)));
			}

			// The treasury - what your faction is worth.
			if (player != null)
			{
				string fleets = fleetCount + " fleet" + (fleetCount == 1 ? "" : "s");
				presenceCards.Add(new PresenceCard(
					player.Credits.ToString("N0") + " credits",
					player.Name + " treasury - " + shipCount.ToString("N0")
						+ " ship" + (shipCount == 1 ? "" : "s") + " in " + fleets));
			}

			// The sector's own blurb, when the scenario wrote one.
			if (sector != null && !string.IsNullOrWhiteSpace(sector.Description))
			{
				presenceCards.Add(new PresenceCard(
					sector.Name + " surveyed", sector.Description));
			}

			// How much of the universe the faction has mapped.
			if (player != null && player.Intel != null && engine != null && engine.Sectors != null)
			{
				int charted = player.Intel.DiscoveredScenesCount;
				int total = engine.Sectors.Count;
				if (charted > 0 && total > 0)
				{
					presenceCards.Add(new PresenceCard(
						charted.ToString("N0") + " sectors charted",
						"of " + total.ToString("N0") + " in the universe - "
							+ (charted * 100 / total) + "% explored"));
				}
			}

			// Finally the scenario itself.
			if (!string.IsNullOrEmpty(scenarioTitle))
			{
				ScenarioInfo scenario = engine != null && engine.World != null
					? engine.World.ScenarioInfo
					: null;
				string blurb = scenario != null && !string.IsNullOrWhiteSpace(scenario.Description)
					? scenario.Description
					: scenario != null && !string.IsNullOrWhiteSpace(scenario.Objectives)
						? scenario.Objectives
						: "Open Frontier";
				// Labelled, so the line reads as what the player is
				// playing rather than another floating fact.
				presenceCards.Add(new PresenceCard("Playing: " + scenarioTitle, blurb));
			}

			for (int i = 0; i < presenceCards.Count; i++)
			{
				presenceCards[i].Details = Truncate(presenceCards[i].Details);
				presenceCards[i].State = Truncate(presenceCards[i].State);
			}
		}

		/// <summary>
		/// What the player is hauling: total units, plus the top cargo
		/// types. A hold with more types than fit on one card gets one
		/// card per page, so the deck rotation walks through the whole
		/// manifest instead of hiding all but the first three. Skipped
		/// entirely for an empty hold.
		/// </summary>
		private void AddCargoCards(Unit ship)
		{
			if (ship == null || !ship.IsValidAndNotDestroyed || ship.Components == null)
			{
				return;
			}
			CargoBayComponent cargoBay = ship.Components.CargoBayComponent;
			if (cargoBay == null || cargoBay.DistintCount == 0)
			{
				return;
			}
			int total = 0;
			var top = new List<KeyValuePair<CargoClass, int>>();
			foreach (KeyValuePair<CargoClass, int> entry in cargoBay.Cargos)
			{
				total += entry.Value;
				top.Add(entry);
			}
			if (total <= 0)
			{
				return;
			}
			// Biggest hauls first, so the most interesting cargo leads.
			top.Sort((a, b) => b.Value.CompareTo(a.Value));

			string details = "Hauling " + total.ToString("N0") + " units";
			string typeLabel = cargoBay.DistintCount + " cargo type"
				+ (cargoBay.DistintCount == 1 ? "" : "s");
			for (int offset = 0; offset < top.Count; offset += TypesPerCargoCard)
			{
				var names = new List<string>();
				int last = Math.Min(top.Count, offset + TypesPerCargoCard);
				for (int i = offset; i < last; i++)
				{
					string cargoName = top[i].Key != null ? top[i].Key.ClassName : null;
					names.Add(string.IsNullOrEmpty(cargoName) ? top[i].Key.ShortName : cargoName);
				}
				string state = typeLabel + " - " + string.Join(", ", names);
				if (last < top.Count)
				{
					state += " (+" + (top.Count - last) + " more)";
				}
				presenceCards.Add(new PresenceCard(details, state));
			}
		}

		private static string SecurityLabel(Sector sector)
		{
			float level = Mathf.Clamp01(sector.AdjustedSecurityLevel01);
			string label = level < 0.34f ? "Low security" : level < 0.67f ? "Mid security" : "High security";
			return label + " (" + Mathf.RoundToInt(level * 100f) + "%)";
		}

		private static string Truncate(string text)
		{
			if (string.IsNullOrEmpty(text) || text.Length <= MaxLineLength)
			{
				return text;
			}
			return text.Substring(0, MaxLineLength - 1) + "…";
		}

		private static int WrapIndex(int index, int count)
		{
			return ((index % count) + count) % count;
		}

		/// <summary>
		/// The unit's name, wherever the game keeps it. Ships store
		/// their name in the ShipName component; stations, turrets,
		/// satellites, storage lockers and every other structure use
		/// UnitName (EngineASX.RenameUnit splits it the same way), so
		/// both are checked and every unit type gets named presence.
		/// </summary>
		private static string GetUnitName(Unit unit)
		{
			if (unit == null)
			{
				return string.Empty;
			}
			UnitComponentHolder components = unit.Components;
			if (unit.UnitType == UnitType.Ship && components != null
				&& !string.IsNullOrEmpty(components.ShipName))
			{
				return components.ShipName;
			}
			if (!string.IsNullOrEmpty(unit.UnitName))
			{
				return unit.UnitName;
			}
			return components != null && !string.IsNullOrEmpty(components.ShipName)
				? components.ShipName
				: string.Empty;
		}

		/// <summary>
		/// The unit's display class. Ships get "series-variant" from
		/// the game itself, but Unit.ClassName is a SHIP variant field
		/// and stays empty for stations and structures - so their
		/// display name lives on the class, and is used as the
		/// fallback here (and as the icon mapping key).
		/// </summary>
		private static string GetUnitClassLabel(Unit unit)
		{
			if (unit == null)
			{
				return string.Empty;
			}
			string label = unit.GetClassAndSeriesName(false);
			if (!string.IsNullOrEmpty(label))
			{
				return label;
			}
			if (unit.UnitClass != null)
			{
				if (!string.IsNullOrEmpty(unit.UnitClass.className))
				{
					return unit.UnitClass.className;
				}
				if (unit.UnitClass.UnitSeries != null && !string.IsNullOrEmpty(unit.UnitClass.UnitSeries.Name))
				{
					return unit.UnitClass.UnitSeries.Name;
				}
			}
			return string.Empty;
		}

		/// <summary>
		/// Hover text for the small image. Normally the unit's own
		/// identity, but when the player is berthed inside something
		/// the icon shows the host, so the hover says what they are
		/// docked in. Manually occupying a station, or sitting in a
		/// hangar without a ship, is NOT docking - there is no host to
		/// name - so the unit simply identifies itself.
		/// </summary>
		private static string BuildPresenceTooltip(Unit playerUnit, Unit presenceUnit)
		{
			string label = BuildUnitTooltip(presenceUnit);
			if (string.IsNullOrEmpty(label))
			{
				return label;
			}
			bool berthed = playerUnit != null
				&& playerUnit.IsValidAndNotDestroyed
				&& playerUnit.IsDocked
				&& presenceUnit != null
				&& !ReferenceEquals(playerUnit, presenceUnit);
			return berthed ? "Docked in: " + label : label;
		}

		/// <summary>
		/// Small-image tooltip: the unit's full class, its variant and
		/// its name. Stations and structures have no variant, so they
		/// read as their type (plus a name, when given one).
		/// </summary>
		private static string BuildUnitTooltip(Unit unit)
		{
			if (unit == null || !unit.IsValidAndNotDestroyed)
			{
				return string.Empty;
			}
			string line = GetUnitClassLabel(unit);
			string name = GetUnitName(unit);
			if (string.IsNullOrEmpty(line))
			{
				return name;
			}
			return string.IsNullOrEmpty(name) ? line : line + " \"" + name + "\"";
		}

		/// <summary>
		/// Picks the activity asset for the unit's class (an explicit
		/// mapping first, then the class name lowercased without
		/// spaces), and attaches it with the tooltip.
		/// </summary>
		private void ApplyUnitImage(ActivityAssets assets, Unit unit, string tooltip)
		{
			if (assets == null)
			{
				return;
			}
			string assetKey = null;
			if (unit != null && unit.IsValidAndNotDestroyed && unit.UnitClass != null)
			{
				UnitSeries series = unit.UnitClass.UnitSeries;
				assetKey = config != null
					? config.ResolveImageKey(
						series != null ? series.Name : null,
						// For ships UnitClass.className is the variant
						// suffix, not a class name - the series name
						// is their key, so only non-ships match on it.
						unit.UnitType == UnitType.Ship ? null : unit.UnitClass.className)
					: null;
			}
			if (string.IsNullOrEmpty(assetKey))
			{
				assetKey = fallbackImageAsset;
			}
			if (string.IsNullOrEmpty(assetKey))
			{
				return;
			}
			assets.SetSmallImage(assetKey);
			if (!string.IsNullOrEmpty(tooltip))
			{
				assets.SetSmallText(tooltip);
			}
		}

		/// <summary>
		/// State line: where they are, who they are, what they
		/// command and what they're worth.
		/// </summary>
		private static string BuildStateLine(Sector sector, Faction player, int shipCount)
		{
			string line = sector != null ? sector.Name : "Open Frontier";
			if (player != null)
			{
				line += " - " + player.Name;
				if (shipCount > 0)
				{
					line += " - " + shipCount.ToString("N0") + " ship" + (shipCount == 1 ? "" : "s");
				}
				line += " - " + player.Credits.ToString("N0") + " cr";
			}
			return line;
		}

		private static string GetScenarioTitle(EngineASX engine)
		{
			if (engine == null || engine.World == null || engine.World.ScenarioInfo == null)
			{
				return string.Empty;
			}
			return engine.World.ScenarioInfo.Title;
		}

		private static int CountShips(Faction faction)
		{
			if (faction == null || faction.Fleets == null)
			{
				return 0;
			}
			int total = 0;
			for (int i = 0; i < faction.Fleets.Count; i++)
			{
				Fleet fleet = faction.Fleets[i];
				if (fleet != null && fleet.Ships != null)
				{
					total += fleet.Ships.Count;
				}
			}
			return total;
		}

		/// <summary>
		/// The unit the player is actually commanding (their ship, or
		/// the station they're docked at).
		/// </summary>
		private static Unit GetPlayerUnit(EngineASX engine)
		{
			if (engine == null)
			{
				return null;
			}
			Unit unit = engine.PlayerUnit;
			return unit != null && unit.IsValidAndNotDestroyed ? unit : null;
		}

		/// <summary>
		/// The unit the presence should represent: what the player is
		/// in, or - when docked - whatever they are docked inside.
		/// GetRootUnit is the game's own notion of that outer unit, so
		/// a shuttle parked in a carrier's hangar shows the carrier
		/// (with the carrier's own ship icon), and a ship berthed at a
		/// station shows the station, exactly as if it were a station.
		/// </summary>
		private static Unit GetPresenceUnit(EngineASX engine)
		{
			Unit unit = GetPlayerUnit(engine);
			if (unit == null)
			{
				return null;
			}
			Unit host = unit.GetRootUnit();
			return host != null && host.IsValidAndNotDestroyed ? host : unit;
		}

		private static void AddButton(Activity activity, string label, string url)
		{
			if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(url))
			{
				return;
			}
			using (var button = new ActivityButton())
			{
				button.SetLabel(label);
				button.SetUrl(url);
				activity.AddButton(button);
			}
		}

		private static bool IsInMainMenu()
		{
			EngineASX engine = EngineASX.Instance;
			GameController gameController = GameController.Instance;
			if (engine == null || engine.World == null || gameController == null)
			{
				return false;
			}
			ScenarioInfo scenario = engine.World.ScenarioInfo;
			ScenarioInfo mainMenuScenario = gameController.MainMenuScenario;
			return scenario != null && mainMenuScenario != null
				&& (scenario == mainMenuScenario || scenario.UniqueId == mainMenuScenario.UniqueId);
		}

		private static Faction FindPlayerFaction(EngineASX engine)
		{
			if (engine == null || engine.Factions == null)
			{
				return null;
			}
			for (int i = 0; i < engine.Factions.Count; i++)
			{
				Faction faction = engine.Factions[i];
				if (faction != null && faction.IsPlayerFaction)
				{
					return faction;
				}
			}
			return null;
		}

		private void TearDown()
		{
			if (client != null)
			{
				try
				{
					client.Disconnect();
					client.Dispose();
				}
				catch
				{
					// Shutting down - the native library is going away.
				}
				client = null;
			}
		}

		private void OnApplicationQuit()
		{
			TearDown();
		}

		private void OnDestroy()
		{
			TearDown();
		}
	}
}
