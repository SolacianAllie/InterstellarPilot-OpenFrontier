using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineMusic
{
	/// <summary>
	/// Shows what the music system is doing and why, on screen, in dev builds.
	/// </summary>
	/// <remarks>
	/// Lives here rather than in DevHud because of a reference cycle, not
	/// because of taste: DevHud is in the OpenFrontier assembly, OpenFrontier.IP
	/// already references OpenFrontier, and so OpenFrontier can never reference
	/// OpenFrontier.IP back. The music is inside OpenFrontier.IP, so anything
	/// that can see the music cannot see DevHud's assembly. Adding the
	/// reference the obvious way is rejected by Unity.
	///
	/// So this draws itself, immediately below DevHud's box and with the same
	/// DPI scaling and the same left edge, so the two read as one panel rather
	/// than two overlays that happen to be near each other.
	///
	/// The reason this is worth having at all: every other music decision here
	/// is silent. A wrong family, a handover that fires at the wrong moment, a
	/// hostile count that never reaches its threshold - none of those produce
	/// an error. They produce music that is subtly wrong, which is the hardest
	/// kind of bug to notice and the easiest to argue about. This is the
	/// answer to "why is it playing that".
	///
	/// Editor and development builds only, under the same guard DevHud uses.
	/// </remarks>
	public sealed class MusicDebugOverlay : MonoBehaviour
	{
		private const float Left = 8f;
		private const float Top = 78f;      // directly under DevHud's 66px box
		private const float Width = 340f;

		private static MusicDebugOverlay instance;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Create()
		{
#if DEVELOPMENT_BUILD || UNITY_EDITOR
			if (instance != null)
			{
				return;
			}

			GameObject go = new GameObject("Music Debug Overlay");
			instance = go.AddComponent<MusicDebugOverlay>();
			Object.DontDestroyOnLoad(go);
#endif
		}

		private void OnGUI()
		{
			MusicDirector director = MusicDirector.Instance;

			if (director == null)
			{
				return;
			}

			string body = Body(director);

			// One line per line of body, plus a little padding. Counted rather
			// than hardcoded so adding a row cannot silently overflow the box
			// and clip its own last line.
			int lines = 1;

			for (int i = 0; i < body.Length; i++)
			{
				if (body[i] == '\n')
				{
					lines++;
				}
			}

			// Same scale as the DevHud box directly above this one, so the two
			// stay one panel. DevOverlayScale is in the OpenFrontier
			// assembly, which this one can see because OpenFrontier.IP
			// references it.
			float scale = DevOverlayScale.Value;
			Matrix4x4 old = GUI.matrix;
			GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

			GUI.Box(new Rect(Left, Top, Width, lines * 18f + 10f), "MUSIC\n" + body);

			GUI.matrix = old;
		}

		/// <summary>
		/// The panel text: state, why, what is playing, and what changes next.
		/// </summary>
		private static string Body(MusicDirector director)
		{
			MusicCrossfadePlayer player = director.Player;
			MusicTimingTable.Entry track = player.Current;
			MusicCombatState combat = director.CombatState;

			string why = Why(director, combat);

			if (track == null)
			{
				return "state " + director.CurrentKey
					+ "\nwhy " + why
					+ "\nplaying nothing"
					+ (player.IsFadingOut ? " (fading out)" : "");
			}

			string position = "playing " + track.Name
				+ "\n" + player.CurrentElapsed.ToString("0") + "s of "
				+ track.Duration.ToString("0") + "s";

			string next;

			if (player.IsFadingOut)
			{
				next = "next " + (player.Incoming == null ? "?" : player.Incoming.Name)
					+ " after the fade";
			}
			else if (player.Incoming != null)
			{
				next = "next " + player.Incoming.Name + ", now";
			}
			else if (player.SecondsToEndingOnset > 0f)
			{
				next = "ends in " + player.SecondsToEndingOnset.ToString("0") + "s";
			}
			else
			{
				next = "ending, handing over";
			}

			return "state " + director.CurrentKey
				+ "\nwhy " + why
				+ "\n" + position
				+ "\n" + next
				+ "\nsrc " + player.AudioState()
				+ "\ntimeScale " + Time.timeScale.ToString("0.00")
				+ ", listeners " + EnabledListenerCount()
				+ ", " + MusicLibrary.LoadedClipCount + " clips loaded";
		}

		/// <summary>
		/// How many AudioListeners are actually able to hear anything.
		/// </summary>
		/// <remarks>
		/// The last thing left to rule out when a track is playing, the source
		/// says it is playing, the volume is right and the clip is loaded, and
		/// there is still no sound: nothing in the scene is listening. This
		/// project has exactly two AudioListeners - on MenuCamera and
		/// GameCamera - so it is a short list to be wrong about, and the game
		/// has no AudioMixer and no code that pauses or mutes one. If this
		/// reads 0, that is the answer, and it is nothing to do with music.
		/// </remarks>
		private static int EnabledListenerCount()
		{
			AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>();

			if (listeners == null)
			{
				return 0;
			}

			int count = 0;

			for (int i = 0; i < listeners.Length; i++)
			{
				if (listeners[i] != null && listeners[i].enabled
					&& listeners[i].gameObject.activeInHierarchy)
				{
					count++;
				}
			}

			return count;
		}

		/// <summary>
		/// The reason, in the same priority order the state machine uses.
		/// </summary>
		/// <remarks>
		/// Worth spelling out rather than showing the key alone. "State
		/// Space/Federal" tells you what is playing; "sector controlled by the
		/// Terran Republic" tells you whether that is right, and those two
		/// disagreeing is the bug you are actually looking for.
		/// </remarks>
		private static string Why(MusicDirector director, MusicCombatState combat)
		{
			if (director.CurrentKey == "Menu")
			{
				return "this is the main menu";
			}

			if (combat.InCombat)
			{
				return combat.HostileCount + " hostile(s), " + combat.Intensity + " "
					+ combat.Category + (combat.AnyTargetingPlayer ? ", targeted" : "")
					+ " - " + combat.HostileCount + " detected in this sector";
			}

			EngineASX engine = EngineASX.Instance;

			if (engine != null && engine.ActiveSector != null)
			{
				Faction owner = engine.ActiveSector.ControllingFaction;

				if (owner == null)
				{
					return "no one controls this sector";
				}

				// Say "yours" explicitly. Your own space is always Allied
				// whatever your faction type is, so the family and the
				// controller's name will not match here - Bandit, playing
				// Allied - and without this the overlay looks broken at the
				// exact moment it is working.
				string yours = owner == engine.LocalFaction ? " (yours - always Allied)" : "";

				return "sector controlled by " + owner.Name + yours
					+ ", type " + owner.FactionType;
			}

			return "no sector";
		}
	}
}
