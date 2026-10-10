using UnityEngine;

namespace OpenFrontier.IP.Engine.UniverseWorld
{
	public class WorldPlayerPermissions : MonoBehaviour
	{
		public bool AllowCargoPricesButton = true;

		public bool AllowContinueOnCompletion = true;

		public bool AllowDockMissionSpecs = true;

		public bool AllowDockPassengerModule = true;

		public bool AllowDockUIDockedShips = true;

		public bool AllowDockUIEquipment = true;

		public bool AllowDockUIFactionInfo = true;

		public bool AllowDockUIMap = true;

		public bool AllowDockUIMessages = true;

		public bool AllowDockUIMissions = true;

		public bool AllowDockUIRepair = true;

		public bool AllowDockUIShipCargo = true;

		public bool AllowDockUIShipTrader = true;

		public bool AllowBuySectorIntel = true;

		public bool AllowDockUITrade = true;

		public bool AllowSaving = true;

		public bool AllowTruce = true;

		public bool AllowPropertyScreen = true;

		public bool AllowFactionSettings = true;

		public bool AllowOrders = true;

		/// <summary>
		/// Whether this world is shared with other players.
		/// </summary>
		/// <remarks>
		/// Static because the restriction is a property of the session rather
		/// than of any one world: the same answer applies to every world the
		/// player loads while multiplayer is switched on, and it can change
		/// between them without a new world being built.
		///
		/// Set by the multiplayer layer. Lives here, on the game side of the
		/// assembly boundary, because everything that reads the permissions is
		/// here and cannot reach across to ask.
		/// </remarks>
		public static bool SharedSession { get; private set; }

		/// <summary>
		/// Says whether anybody is actually in this world with you.
		/// </summary>
		/// <remarks>
		/// A method taking a count rather than a public setter, because the
		/// question is not "has the player permitted multiplayer" but "is
		/// another person here". Those were conflated, and the cost was that a
		/// player alone who had ticked the option on so a friend could join
		/// them later lost god mode and hypersleep in their own singleplayer
		/// game - with nobody in it, and nothing on screen saying why.
		///
		/// Both of those are singleplayer conveniences, and what makes them
		/// unsafe is another person being present, not another person being
		/// allowed in. Put here rather than in the caller so that the rule
		/// sits with the thing it governs, and so it can be asked about
		/// without a session object.
		/// </remarks>
		public static void SetSharedSessionFor(int playerCount)
		{
			SharedSession = playerCount > 0;
		}

		private bool allowHypersleep = true;

		/// <summary>
		/// Whether the player may enter hypersleep.
		/// </summary>
		/// <remarks>
		/// A property rather than a field so the shared-session restriction
		/// applies on read. Writing it still works, so the scenario-level
		/// overrides - Skirmish disables several of these - are unaffected.
		/// </remarks>
		public bool AllowHypersleep
		{
			get => allowHypersleep && !SharedSession;
			set => allowHypersleep = value;
		}

		public bool AutoHidePlayerShieldAndHull = true;

		public bool AutoHideCapacitor = true;

		private bool allowGodMode = true;

		/// <summary>
		/// Whether the player may use god mode.
		/// </summary>
		/// <remarks>
		/// A property for the same reason as <see cref="AllowHypersleep"/>:
		/// god mode is a single-player convenience, and letting one player
		/// toggle it in a world other players are in would desync the game for
		/// them without them having done anything.
		/// </remarks>
		public bool AllowGodMode
		{
			get => allowGodMode && !SharedSession;
			set => allowGodMode = value;
		}

		public bool AllowHudViewOptions = true;
	}
}
