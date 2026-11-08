using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Sends this copy's own ships to the host, and nothing else.
	/// </summary>
	/// <remarks>
	/// The joining client simulates nothing that anybody else can see. The
	/// host runs the whole world - including this player's ship - and this
	/// copy draws what it is told.
	///
	/// That is the opposite of the client-authoritative model it replaces,
	/// and the reason is worth stating because the difference is not a detail.
	/// With the client simulating its own ship, two copies were both
	/// integrating the same rigidbody: the client moved it locally and the
	/// host moved it from its own inputs, and the two disagreed. Every
	/// correction of that is a visible snap.
	///
	/// Uploading the finished state instead means the host is the only thing
	/// that ever integrates a ship, and the client is a renderer of somebody
	/// else's arithmetic. The cost is a round trip of lag between moving the
	/// stick and seeing the ship move, which is the real trade - and it is
	/// the right way round for this game, where watching a battle is most of
	/// what you do and being right matters more than being immediate.
	///
	/// Sent faster than the host broadcasts, because this is the one thing
	/// the client is the authority on and the input to a simulation rather
	/// than an observation of one.
	/// </remarks>
	public sealed class ClientShipUploader : MonoBehaviour
	{
		/// <summary>
		/// Uploads per second.
		/// </summary>
		/// <remarks>
		/// Twice the host's broadcast rate, so a ship that stops moving - which
		/// is most of the time - still arrives promptly, and because the host
		/// re-broadcasts on its own schedule a dropped upload is corrected
		/// within a frame or two rather than being a hole in the world.
		/// </remarks>
		public const float UploadsPerSecond = 20f;

		private const float PositionSnapDistance = 400f;

		private float nextUploadTime;

		private readonly List<UnitState> batch = new List<UnitState>(4);

		/// <summary>How many uploads have been sent, for the log.</summary>
		public int UploadsSent { get; private set; }

		private int reportedUploads;

		private float nextReportTime;

		private void Update()
		{
			MultiplayerClientService service = MultiplayerClientService.Instance;

			// Connected, not merely present: a host that has also joined would
			// otherwise be uploading its own world to itself.
			if (service == null || !service.IsConnected)
			{
				return;
			}

			Report(service);

			if (Time.unscaledTime < nextUploadTime)
			{
				return;
			}

			nextUploadTime = Time.unscaledTime + (1f / UploadsPerSecond);

			Upload(service);
		}

		/// <summary>
		/// Says whether this copy is sending its ship, and if not, why not.
		/// </summary>
		/// <remarks>
		/// There was no way to tell. Uploads were counted and never printed,
		/// and nothing anywhere said what stopped them either - so a client
		/// whose ship was silently not being sent looked exactly like a host
		/// that was ignoring it, and neither could tell the other.
		///
		/// A reporter that can only say "I did nothing" is still a reporter:
		/// the name of the reason is the whole content of it.
		/// </remarks>
		private void Report(MultiplayerClientService service)
		{
			if (Time.unscaledTime < nextReportTime)
			{
				return;
			}

			nextReportTime = Time.unscaledTime + 2f;

			int sent = UploadsSent - reportedUploads;
			reportedUploads = UploadsSent;

			Unit local = EngineASX.Instance == null ? null : EngineASX.Instance.LocalUnit;

			string why = "no reason - it is sending";

			if (!service.IsConnected)
			{
				why = "not connected";
			}
			else if (local == null)
			{
				why = "this copy has no ship being flown";
			}
			else if (local.IsDestroyed)
			{
				why = "the ship being flown has been destroyed";
			}

			int total = UploadsSent;

			NetLog.Client("uploading - " + sent + " ship state(s) in 2s; "
				+ local2(local) + "; " + why);

			Debug.Log("Multiplayer: uploading - " + sent + " ship state(s) in 2s ("
				+ total + " so far); " + why);
		}

		private string local2(Unit local)
		{
			return local == null
				? "no current unit"
				: "ship " + local.UniqueId + (local.IsActiveInEngine ? string.Empty : ", NOT active in engine");
		}

		private void Upload(MultiplayerClientService service)
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return;
			}

			Unit local = engine.LocalUnit;

			// Only the ship being flown, and only if this copy is the one that
			// simulates it.
			//
			// Asked of Authority rather than worked out here. This used to test
			// IsActiveInEngine, which is true only for units in the active
			// sector - so a player whose ship was anywhere else stopped
			// uploading, silently, with nothing in either log to say so. The
			// question "may this copy simulate this ship" is now answered in
			// one place, and this is that place asking.
			if (local == null || local.IsDestroyed || !Authority.OwnsUnit(local))
			{
				return;
			}

			batch.Clear();
			batch.Add(Sample(local));

			// The sector goes up with the ship.
			//
			// This copy simulates this ship, so it is the only machine that
			// knows where it is - including when it has crossed into another
			// sector, which is the one change the host cannot see any other
			// way. Its copy of this ship stays in the old sector forever,
			// which means the host keeps working out which ships to send this
			// player from a place the player left some minutes ago.
			//
			// Sent as part of the state rather than as its own message
			// because it changes rarely and this already goes out on a timer:
			// a message for it would be almost entirely silence.
			service.SendUnitStates(UnitStateBatch.Pack(
				batch, Time.unscaledTime,
				local.Sector == null ? 0 : local.Sector.UniqueId));
			UploadsSent++;
		}

		private static UnitState Sample(Unit unit)
		{
			Transform transform = unit.transform;
			Quaternion rotation = transform.localRotation;
			Vector3 velocity = unit.RBody != null
				? unit.RBody.linearVelocity
				: Vector3.zero;

			return new UnitState
			{
				Id = unit.UniqueId,
				PositionX = transform.localPosition.x,
				PositionY = transform.localPosition.y,
				PositionZ = transform.localPosition.z,
				RotationX = rotation.x,
				RotationY = rotation.y,
				RotationZ = rotation.z,
				RotationW = rotation.w,
				VelocityX = velocity.x,
				VelocityY = velocity.y,
				VelocityZ = velocity.z,
			};
		}
	}
}