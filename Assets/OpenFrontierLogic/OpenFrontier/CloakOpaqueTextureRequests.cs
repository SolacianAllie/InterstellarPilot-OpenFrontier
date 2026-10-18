namespace OpenFrontier
{
	/// <summary>
	/// Open Frontier: process-wide counter of units whose cloak runtime
	/// needs _CameraOpaqueTexture. ActiveUnit flips a unit in when its
	/// cloak ramp first engages and out when it releases (or the renderer
	/// is disabled/destroyed); CloakOpaqueTextureSync reads <see cref="Any"/>
	/// at a poll interval and toggles the main camera's opaque-texture
	/// override once per change. Lives here (the OpenFrontier assembly)
	/// because the Engine assembly references this one, never the reverse.
	///
	/// The count's balance is held by the callers: each unit guards on its
	/// own wants-state, so a unit can only contribute once until it
	/// releases again. OnDisable/destroy paths release explicitly.
	/// </summary>
	public static class CloakOpaqueTextureRequests
	{
		private static int requestCount;

		/// <summary>
		/// One unit asked to start (wants = true) or stopped asking
		/// (wants = false). Call only on state transitions.
		/// </summary>
		public static void Request(bool wants)
		{
			requestCount += (wants ? 1 : -1);
		}

		/// <summary>True while at least one cloaking hull is on screen duty.</summary>
		public static bool Any => requestCount > 0;
	}
}
