using UnityEngine;

namespace OpenFrontier
{
	/// <summary>
	/// Open Frontier: process-wide counter of units whose cloak runtime
	/// needs the backdrop snapshot (_CloakBackdropRT). ActiveUnit flips a
	/// unit in when its cloak ramp first engages and out when it releases
	/// (or the renderer is disabled/destroyed); CloakBackdropFeature reads
	/// <see cref="Any"/> when it records and skips its blit entirely when
	/// nobody wants one. Lives here (the OpenFrontier assembly) because
	/// the Engine assembly references this one, never the reverse.
	///
	/// The count's balance is held by the callers: each unit guards on its
	/// own wants-state, so a unit can only contribute once until it
	/// releases again. OnDisable/destroy paths release explicitly.
	///
	/// The camera is PROVIDED by the engine side (GameController hands its
	/// instantiated mainCamera over at CreateCamera), because the camera
	/// that draws ships is a runtime instance, not a Camera.main guess.
	/// </summary>
	public static class CloakBackdropRequests
	{
		private static int requestCount;

		/// <summary>The camera the engine says renders ships; Unity-null when unprovided or destroyed.</summary>
		public static Camera ProvidedCamera { get; private set; }

		/// <summary>
		/// One unit asked to start (wants = true) or stopped asking
		/// (wants = false). Call only on state transitions.
		/// </summary>
		public static void Request(bool wants)
		{
			requestCount += (wants ? 1 : -1);
		}

		/// <summary>Engine-side camera handover, called wherever the render camera is actually created.</summary>
		public static void ProvideCamera(Camera camera)
		{
			ProvidedCamera = camera;
		}

		/// <summary>True while at least one cloaking hull is asking for the snapshot.</summary>
		public static bool Any => requestCount > 0;
	}
}
