using UnityEngine;

namespace OpenFrontier
{
	/// <summary>
	/// Which input family the player's last observed input came from.
	/// Unknown is the boot state: devices may be CONNECTED, but until one
	/// actually produces input nothing has been "used", so nothing may
	/// behave as if it were (no gamepad default highlight, no assumed
	/// scheme). The tracker (InputSchemeTracker) writes; everything else
	/// reads.
	/// </summary>
	public enum InputScheme
	{
		Unknown = 0,
		Gamepad,
		KeyboardMouse,
		Touch,
	}

	/// <summary>
	/// The input family actually in use, not the devices merely connected -
	/// "the game uses whatever is actively in use, not prioritising anything".
	/// This class holds no behaviour: consumers read <see cref="Scheme"/>
	/// whenever they need it (menu default selection gates on Gamepad), and
	/// react to flips through <see cref="SchemeChanged"/>. The per-frame
	/// detection lives in InputSchemeTracker, which self-spawns here so no
	/// scene or prefab has to carry it (same shape as GestureScript).
	/// </summary>
	public static class ActiveInput
	{
		public static InputScheme Scheme { get; private set; }

		public static event System.Action<InputScheme> SchemeChanged;

		internal static void Set(InputScheme scheme)
		{
			if (Scheme == scheme)
			{
				return;
			}

			Scheme = scheme;
			SchemeChanged?.Invoke(scheme);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void SpawnTracker()
		{
			if (Object.FindFirstObjectByType<InputSchemeTracker>() != null)
			{
				return;
			}

			GameObject tracker = new GameObject("ActiveInputTracker");
			Object.DontDestroyOnLoad(tracker);
			tracker.AddComponent<InputSchemeTracker>();
		}
	}
}
