using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Multiplayer.UI;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Tells every client when a ship appears, and when it stops existing.
	/// </summary>
	/// <remarks>
	/// Before this, a ship built by the host after the world snapshot reached
	/// clients as state for an id they had never heard of: UnitStateApplier
	/// counted it unknown and dropped it. And a ship that stopped existing was
	/// simply never mentioned again, so the client's copy stopped moving and
	/// hung there - a frozen corpse for the rest of the session.
	///
	/// Arrivals come from an event, because EngineASX.RegisterUnit is the one
	/// place every unit passes through. Departures cannot: a unit announces its
	/// own, and the only way to hear every one of those is to already hold the
	/// unit. So those are a sweep of UpdatableUnits against what is known -
	/// one dictionary walk, no allocation, and no second list of places a ship
	/// can be destroyed.
	///
	/// Both wait a frame. RegisterUnit runs before callers set unit.Sector, so
	/// at the moment an arrival fires the ship has nowhere to be; and letting
	/// departures settle a moment keeps world loading quiet, since a whole
	/// galaxy is registered and unregistered before anybody is connected.
	/// </remarks>
	public sealed class NewUnitBroadcaster : MonoBehaviour
	{
		public const string ObjectName = "Multiplayer New Unit Broadcaster";

		private static NewUnitBroadcaster instance;

		private readonly List<Unit> pending = new List<Unit>();
		private readonly HashSet<int> known = new HashSet<int>();
		private readonly List<int> gone = new List<int>();
		private readonly List<int> killed = new List<int>();

		/// <summary>Ships whose owner changed, and to whom.</summary>
		private readonly List<KeyValuePair<int, int>> changed =
			new List<KeyValuePair<int, int>>();

		private EngineASX attached;

		private int broadcastTotal;
		private int removedTotal;
		private int killedTotal;
		private int changedTotal;

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
			MultiplayerPump.AddedPumpTick += AttachTo;
			AttachTo();
		}

		private void OnDisable()
		{
			MultiplayerPump.AddedPumpTick -= AttachTo;

			if (attached != null)
			{
				attached.UnitCreated -= Note;
			}

			DestructableUnit.UnitKilled -= NoteKill;
			pending.Clear();
			known.Clear();
		}

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
			DestructableUnit.UnitKilled += NoteKill;

			Debug.Log("Multiplayer: watching for ships appearing and disappearing");
		}

		private void Note(Unit unit)
		{
			if (unit != null && !unit.IsDestroyed)
			{
				pending.Add(unit);
			}
		}

		// The event's own type, so a handler converts to it without ceremony.
		private readonly List<KeyValuePair<Unit, System.Action<Unit, Faction>>> ownershipHooks =
			new List<KeyValuePair<Unit, System.Action<Unit, Faction>>>();

		/// <summary>
		/// Listens to every existing unit for ownership changes.
		/// </summary>
		/// <remarks>
		/// There is no engine-wide event for this, because ownership is a
		/// per-unit setter rather than a registry. Swept rather than hooked
		/// individually: the list only grows when a ship appears, which is
		/// already being told about, and an already-hooked unit is not hooked
		/// twice.
		/// </remarks>
		private void HookOwnershipChanges()
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null || engine.UpdatableUnits == null)
			{
				return;
			}

			for (int i = 0; i < engine.UpdatableUnits.Count; i++)
			{
				Unit unit = engine.UpdatableUnits[i];

				if (unit == null || AlreadyHooked(unit))
				{
					continue;
				}

				Unit captured = unit;

				System.Action<Unit, Faction> handler = (changedUnit, newFaction) =>
					changed.Add(new KeyValuePair<int, int>(
						changedUnit.UniqueId,
						newFaction == null
							? UnitOwnerChangedMessage.Unowned
							: newFaction.UniqueId));

				captured.OwnerChanged += handler;
				ownershipHooks.Add(new KeyValuePair<Unit, System.Action<Unit, Faction>>(captured, handler));
			}
		}

		private bool AlreadyHooked(Unit unit)
		{
			for (int i = 0; i < ownershipHooks.Count; i++)
			{
				if (ownershipHooks[i].Key == unit)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Says a ship changed hands, or stopped belonging to anyone.
		/// </summary>
		/// <remarks>
		/// The case that is neither arriving nor dying: a ship abandoned by
		/// its pilot survives with no owner and becomes claimable, and a client
		/// that is not told still sees it belonging to the faction that gave
		/// it up - so it cannot claim the one ship in the game that is meant to
		/// be free.
		///
		/// Said for every faction change rather than only for abandonment,
		/// because capture and salvage take the same path and all three are
		/// the same fact: the ship is the same ship and it belongs to
		/// somebody else now.
		/// </remarks>
		private void TellAboutOwnership(HostSession session)
		{
			if (changed.Count == 0)
			{
				return;
			}

			for (int i = 0; i < changed.Count; i++)
			{
				session.Broadcast(ProtocolVersion.UnitOwnerChangedMessageId,
					UnitOwnerChangedMessage.OwnedBy(
						changed[i].Key, changed[i].Value).ToBytes());

				changedTotal++;
			}

			Debug.Log("Multiplayer: told clients about " + changed.Count
				+ " ship(s) changing owner. " + changedTotal + " so far this session.");

			changed.Clear();
		}

		private void NoteKill(Unit unit)
		{
			if (unit != null && !unit.IsDestroyed)
			{
				killed.Add(unit.UniqueId);
			}
		}

		private void Update()
		{
			HostSession session = MultiplayerHostService.Session;

			if (session == null || session.Players.Count == 0)
			{
				return;
			}

			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return;
			}

			TellAboutKills(session);
			TellAboutOwnership(session);
			TellAboutArrivals(session);
			TellAboutDepartures(session, engine);
		}

		/// <summary>
		/// Says a ship was destroyed, at the moment it was destroyed.
		/// </summary>
		/// <remarks>
		/// Before the object goes, not after, because the explosion plays in
		/// between and a client told afterwards would set off the whole
		/// animation late - late by however long the animation is, on a ship
		/// the player is watching.
		///
		/// Said separately from removals because a ship you sold must not
		/// explode, and a ship that was shot down should.
		/// </remarks>
		private void TellAboutKills(HostSession session)
		{
			if (killed.Count == 0)
			{
				return;
			}

			for (int i = 0; i < killed.Count; i++)
			{
				session.Broadcast(ProtocolVersion.UnitGoneMessageId,
					UnitGoneMessage.Killed(killed[i]).ToBytes());

				killedTotal++;
			}

			Debug.Log("Multiplayer: told clients about " + killed.Count
				+ " ship(s) being destroyed. " + killedTotal + " so far this session.");

			killed.Clear();
		}

		private void TellAboutArrivals(HostSession session)
		{
			for (int i = pending.Count - 1; i >= 0; i--)
			{
				Unit unit = pending[i];

				if (unit == null || unit.IsDestroyed)
				{
					pending.RemoveAt(i);
					continue;
				}

				// Still waiting for a caller to finish putting it somewhere.
				if (unit.Sector == null || unit.UnitClass == null)
				{
					continue;
				}

				pending.RemoveAt(i);
				known.Add(unit.UniqueId);
				TellEveryone(session, unit);
			}
		}

		private void TellAboutDepartures(HostSession session, EngineASX engine)
		{
			if (known.Count == 0 || engine.UpdatableUnits == null)
			{
				return;
			}

			gone.Clear();

			foreach (Unit unit in engine.UpdatableUnits)
			{
				if (unit != null && !unit.IsDestroyed)
				{
					known.Remove(unit.UniqueId);
				}
			}

			// Whatever was left in known is not in the world any more.
			foreach (int unitId in known)
			{
				gone.Add(unitId);
			}

			if (gone.Count == 0)
			{
				return;
			}

			known.Clear();

			for (int i = 0; i < gone.Count; i++)
			{
				session.Broadcast(ProtocolVersion.UnitGoneMessageId,
					UnitGoneMessage.Removed(gone[i]).ToBytes());

				removedTotal++;
			}

			Debug.Log("Multiplayer: told clients about " + gone.Count
				+ " ship(s) no longer existing. " + removedTotal + " so far this session.");
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

			// Checked before it goes out rather than trusted: a message the
			// receiver will refuse is a ship that silently does not exist
			// anywhere but here, which is the failure this whole thing exists
			// to remove.
			if (!NewUnitMessage.TryFromBytes(message.ToBytes(),
				out NewUnitMessage checkedMessage, out string error))
			{
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