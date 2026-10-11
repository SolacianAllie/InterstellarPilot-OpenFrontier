using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Builds the ships the host says exist, on this copy.
	/// </summary>
	/// <remarks>
	/// The first thing on a client that creates anything. Every other applier
	/// writes onto things that were already here; this one brings new ones
	/// into being, from the same prefab the host instantiated, because both
	/// copies run the same build and hold the same unit class tables.
	///
	/// Idempotent by construction: a ship this copy already has is left alone,
	/// so a message that arrives twice - which it will, for any ship that was
	/// also in the world snapshot - costs nothing and does nothing. That is
	/// why there is no need to decide whether the snapshot or the broadcast
	/// arrived first.
	///
	/// What this cannot reproduce is anything done to a ship after it was
	/// created: damage, refits, cargo in its hold. None of that is replicated
	/// yet, and none of it is in the message, so the two copies agree that a
	/// ship is a ship and disagree about its condition - which is the next
	/// thing rather than this one.
	/// </remarks>
	public sealed class NewUnitApplier : MonoBehaviour
	{
		public const string ObjectName = "Multiplayer New Unit Applier";

		private static NewUnitApplier instance;

		public static NewUnitApplier Instance => instance;

		private MultiplayerClientService service;
		private MultiplayerClientService connectedTo;

		private int createdTotal;
		private int alreadyHad;

		private void Awake()
		{
			instance = this;
		}

		private void OnEnable()
		{
			Attach();
			Connect();
		}

		private void OnDisable()
		{
			if (service != null)
			{
				service.NewUnitReceived -= Apply;
			}
		}

		private void Connect()
		{
			MultiplayerClientService current = MultiplayerClientService.Instance;

			if (current == null || current == connectedTo)
			{
				return;
			}

			if (connectedTo != null)
			{
				connectedTo.Connected -= Attach;
			}

			connectedTo = current;
			connectedTo.Connected += Attach;
		}

		private void Attach()
		{
			MultiplayerClientService current = MultiplayerClientService.Instance;

			if (current == null || current == service)
			{
				return;
			}

			if (service != null)
			{
				service.NewUnitReceived -= Apply;
			}

			service = current;
			service.NewUnitReceived += Apply;

			Debug.Log("Multiplayer: new-ship applier is listening to the host");
		}

		private void Apply(NewUnitMessage message)
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return;
			}

			// From the snapshot and from the broadcast are the same event as
			// far as this copy is concerned, so one of them is a no-op.
			if (engine.GetUnitByid(message.UnitId) != null)
			{
				alreadyHad++;

				return;
			}

			UnitClass unitClass = engine.GetUnitClassById(message.UnitClassId);

			if (unitClass == null || unitClass.UnitPrefab == null)
			{
				Debug.LogWarning("Multiplayer: the host says unit " + message.UnitId
					+ " is a unit class " + message.UnitClassId
					+ ", which this build does not have. It will not exist here, and"
					+ " the two copies now disagree about the world.");

				return;
			}

			Sector sector = engine.GetSectorById(message.SectorId);

			if (sector == null)
			{
				Debug.LogWarning("Multiplayer: the host says unit " + message.UnitId
					+ " is in sector " + message.SectorId
					+ ", which this build does not have.");

				return;
			}

			Faction faction = engine.GetFactionByid(message.FactionId);

			if (faction == null)
			{
				Debug.LogWarning("Multiplayer: the host says unit " + message.UnitId
					+ " belongs to faction " + message.FactionId
					+ ", which this build does not have.");

				return;
			}

			Unit ship = Instantiate(unitClass.UnitPrefab).GetComponent<Unit>();

			if (ship == null)
			{
				Debug.LogWarning("Multiplayer: unit class " + message.UnitClassId
					+ " has no Unit on its prefab. Unit " + message.UnitId
					+ " could not be built here.");

				return;
			}

			ship.Init();
			ship.Components.AutoAssignShipName();
			ship.Components.InstallDefaultComponents();
			ship.Components.AddDefaultCargoLoadout();
			ship.Faction = faction;
			ship.Sector = sector;

			ship.transform.localPosition = new Vector3(
				message.PositionX, message.PositionY, message.PositionZ);

			ship.transform.localRotation = new Quaternion(
				message.RotationX, message.RotationY, message.RotationZ, message.RotationW);

			createdTotal++;

			Debug.Log("Multiplayer: ship " + message.UnitId + " now exists here too - a "
				+ unitClass.GetSeriesName() + " of faction " + message.FactionId
				+ " in sector " + message.SectorId + ". Built " + createdTotal
				+ ", already had " + alreadyHad + ".");
		}
	}
}