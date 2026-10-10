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

		private void Update()
		{
			MultiplayerClientService service = MultiplayerClientService.Instance;

			// Connected, not merely present: a host that has also joined would
			// otherwise be uploading its own world to itself.
			if (service == null || !service.IsConnected || Time.unscaledTime < nextUploadTime)
			{
				return;
			}

			nextUploadTime = Time.unscaledTime + (1f / UploadsPerSecond);

			Upload(service);
		}

		private void Upload(MultiplayerClientService service)
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return;
			}

			Unit local = engine.LocalUnit;

			// Only the ship being flown. A client that uploaded every unit in
			// its copy would be asking the host to believe a whole second
			// world, and the authority it would be asserting over ships
			// nobody here is flying is exactly what must not exist.
			if (local == null || local.IsDestroyed || !local.IsActiveInEngine)
			{
				return;
			}

			batch.Clear();
			batch.Add(Sample(local));

			service.SendUnitStates(UnitStateBatch.Pack(batch));
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