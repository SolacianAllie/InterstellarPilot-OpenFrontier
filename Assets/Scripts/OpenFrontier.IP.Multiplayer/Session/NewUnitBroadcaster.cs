using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Multiplayer.UI;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Tells every client about a ship the moment the world gains one.
	/// </summary>
	/// <remarks>
	/// Before this, a ship built by the host after the world snapshot was
	/// state for an id the client had never heard of: UnitStateApplier counted
	/// it unknown and dropped it. An AI empire that built ships looked exactly
	/// as it had at join, for ever.
	///
	/// Listens to one event rather than to a list of places, because a ship can
	/// arrive from anywhere - an AI shipyard, a mission reward, a station under
	/// construction, a player buying one - and a list of places is a list that
	/// is wrong the moment somebody finds another one.
	///
	/// Deferred by a frame on purpose. EngineASX raises UnitCreated from
	/// RegisterUnit, and every caller sets unit.Sector afterwards, so at the
	/// moment the event fires the unit has no sector and the message could not
	/// say where it is. Waiting is also what keeps world loading quiet: a whole
	/// galaxy is registered before anybody is connected, and by the time there
	/// is anybody to tell, it has all settled.
	/// </remarks>
	public sealed class NewUnitBroadcaster : MonoBehaviour
	{
		public const string ObjectName = "Multiplayer New Unit Broadcaster";

		private static NewUnitBroadcaster instance;

		private readonly List<Unit> pending = new List<Unit>();

		private int broadcastTotal;

		public static void Install()
		{
			if (instance != null)
			{
				return;
			}

			GameObject host = new GameObject(ObjectName);
			host.hideFlags = HideFlags.HideAndDontSave;
			DontDestroyOnLoad(host);
			host.AddComponent<NewUnitBroadcaster>();
		}

		private void Awake()
		{
			instance = this;
		}

		private void OnEnable()
		{
			// Attached when a world arrives rather than at startup, because
			// there is no world to raise the event before then.
			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				MultiplayerPump.AddedPumpTick += AttachTo;
			}
			else
			{
				AttachTo();
			}
		}

		private void OnDisable()
		{
			if (attached != null)
			{
				attached.UnitCreated -= Note;
				attached = null;
			}

			pending.Clear();
			MultiplayerPump.AddedPumpTick -= AttachTo;
		}

		private EngineASX attached;

		private void AttachTo()
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null || engine == attached)
			{
				return;
			}

			if (attached != null)
			{
				attached.UnitCreated -= Note;
			}

			attached = engine;
			attached.UnitCreated += Note;

			Debug.Log("Multiplayer: watching for new ships to tell clients about");
		}

		private void Note(Unit unit)
		{
			if (unit != null && !unit.IsDestroyed)
			{
				pending.Add(unit);
			}
		}

		private void Update()
		{
			HostSession session = MultiplayerHostService.Session;

			if (session == null || session.Players.Count == 0 || pending.Count == 0)
			{
				return;
			}

			for (int i = pending.Count - 1; i >= 0; i--)
			{
				Unit unit = pending[i];

				// Still waiting for a caller to finish putting it somewhere.
				if (unit == null || unit.IsDestroyed)
				{
					pending.RemoveAt(i);
					continue;
				}

				if (unit.Sector == null || unit.UnitClass == null)
				{
					continue;
				}

				pending.RemoveAt(i);
				TellEveryone(session, unit);
			}
		}

		private void TellEveryone(HostSession session, Unit unit)
		{
			NewUnitMessage message = new NewUnitMessage
			{
				UnitId = unit.UniqueId,
				UnitClassId = unit.UnitClass.UniqueID,
				FactionId = unit.Faction == null ? 0 : unit.Faction.UniqueId,
				SectorId = unit.Sector.UniqueId,
				PositionX = unit.transform.localPosition.x,
				PositionY = unit.transform.localPosition.y,
				PositionZ = unit.transform.localPosition.z,
				RotationX = unit.transform.localRotation.x,
				RotationY = unit.transform.localRotation.y,
				RotationZ = unit.transform.localRotation.z,
				RotationW = unit.transform.localRotation.w
			};

			if (!NewUnitMessage.TryFromBytes(message.ToBytes(),
				out NewUnitMessage checkedMessage, out string error))
			{
				// Checked before it goes out rather than trusted, because a
				// message the receiver will refuse is a ship that silently
				// does not exist on anybody's screen but this one.
				Debug.LogWarning("Multiplayer: not telling clients about new ship "
					+ unit.UniqueId + " - " + error);

				return;
			}

			session.Broadcast(ProtocolVersion.NewUnitMessageId, checkedMessage.ToBytes());
			broadcastTotal++;

			Debug.Log("Multiplayer: a new ship exists - " + unit.UniqueId
				+ ", a " + unit.UnitClass.GetSeriesName()
				+ " of faction " + message.FactionId
				+ " in sector " + message.SectorId + ". Told " + broadcastTotal + " so far.");
		}
	}
}