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

		private string cachedBody = "";
		private string cachedAgainstName;
		private string cachedAgainstKey;
		private float nextBodyAt;

		/// <summary>Box style with word wrap OFF, so IMGUI never re-wraps.</summary>
		private static GUIStyle panelStyle;

		private void OnGUI()
		{
			// ONLY ON REPAINT. IMGUI issues a Layout event and a Repaint event
			// every frame, so drawing in OnGUI without this does the whole
			// panel twice per frame - and the second time is pure waste,
			// because the text and its rectangle are both fixed.
			//
			// Safe here precisely because nothing in this panel participates
			// in GUILayout. It is an explicit Rect, so there is no layout to
			// keep in step; skipping the Layout event skips only work whose
			// result is thrown away.
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			MusicDirector director = MusicDirector.Instance;

			if (director == null)
			{
				return;
			}

			// Rebuilt four times a second, not sixty, and only during a
			// Repaint now. The positions it shows are seconds out of a
			// hundred, and a diagnostic that lags a quarter of a second is
			// still a diagnostic.
			//
			// Compared as two values rather than pasted into one key string,
			// because that concat allocated once per frame for the whole
			// session. Both are plain field reads, so the check is free.
			string currentName = director.Player.Current != null
				? director.Player.Current.Name
				: null;

			if (currentName != cachedAgainstName
				|| director.CurrentKey != cachedAgainstKey
				|| Time.realtimeSinceStartup >= nextBodyAt)
			{
				cachedAgainstName = currentName;
				cachedAgainstKey = director.CurrentKey;
				nextBodyAt = Time.realtimeSinceStartup + 0.25f;
				cachedBody = Body(director);
				cachedLines = CountLines(cachedBody);
			}

			string body = cachedBody;

			// Same scale as the DevHud box directly above this one, so the two
			// stay one panel. DevOverlayScale is in the OpenFrontier
			// assembly, which this one can see because OpenFrontier.IP
			// references it.
			float scale = DevOverlayScale.Value;
			Matrix4x4 old = GUI.matrix;
			GUI.matrix = scaledMatrix;

			GUI.Box(new Rect(Left, Top, Width, cachedLines * 18f + 10f), "MUSIC\n" + body, PanelStyle);

			GUI.matrix = old;
		}

		private int cachedLines = 1;

		private static int CountLines(string text)
		{
			int lines = 1;

			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '\n')
				{
					lines++;
				}
			}

			return lines;
		}

		/// <summary>
		/// The box style, with word wrap switched off.
		/// </summary>
		/// <remarks>
		/// Unity's default box style has wordWrap ON, which means IMGUI
		/// measures the text and breaks it into lines on EVERY event of EVERY
		/// frame - for a panel of several hundred characters, four times a
		/// frame across two overlays, whether or not one character of it had
		/// changed. Measuring wrapped text is the expensive part of drawing
		/// it, and it was happening to text that was already final.
		///
		/// With wordWrap off, IMGUI draws the lines it is given. The panel's
		/// own newlines still break as normal; a line too long for the box
		/// runs past its edge rather than wrapping, which is an acceptable
		/// trade for a diagnostic and the reason this is cheap at all.
		/// </remarks>
		private static GUIStyle PanelStyle
		{
			get
			{
				if (panelStyle == null)
				{
					panelStyle = new GUIStyle(GUI.skin.box)
					{
						wordWrap = false,
						richText = false,
						alignment = TextAnchor.UpperLeft,
						padding = new RectOffset(6, 6, 4, 4)
					};
				}

				return panelStyle;
			}
		}

		/// <summary>Cached, since it only changes when the DPI does.</summary>
		private static Matrix4x4 scaledMatrix =>
			Matrix4x4.Scale(new Vector3(DevOverlayScale.Value, DevOverlayScale.Value, 1f));

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

			if (player.IsCrossfading)
			{
				next = "crossfading to " + (player.Incoming == null ? "?" : player.Incoming.Name)
					+ ", equal power";
			}
			else if (player.IsFadingOut)
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
				+ "\nvoices " + director.VoiceReport()
				+ "\n" + player.AudioState();
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

			if (combat.IsThreat)
			{
				string linger = combat.ThreatState == MusicThreatState.AllClear
					? " (all clear - threat theme playing out)"
					: "";

				return combat.HostileCount + " seen, " + combat.Intensity + " "
					+ combat.Category + (combat.Engaged ? " (engaged)" : "") + linger
					+ " of " + MusicCombatState.ThreatThreshold + " to react at all";
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
