using System;
using UnityEngine;

namespace OpenFrontier.Engine
{
	/// <summary>
	/// One system's anchor in the galaxy stage — the named object under
	/// "systems/" that a gamemode's data overlay references. The pinned
	/// contract carries BOTH kinds here: hand-authored anchors (a
	/// gamemode-placed system occupies one) and runtime-created anchors
	/// (generated systems get one at their generated position). The anchor
	/// is the system's PRESENCE in the galaxy — its galaxy position is
	/// this transform's position, and the capture rig reads it as the
	/// destination for the cubemap grab and the arrival-vector math.
	///
	/// Ids are the namespaced-string discipline from the first day
	/// ("core.*" built-ins, "mod.<author>.*" mods) — the same rule that
	/// made GamemodeDefinition.Id ship-once: anchors are cross-referenced
	/// by saves, gamemode overlays and the galaxy map later; renaming
	/// after saves exist is the reserved-id-band problem all over again.
	/// </summary>
	public class SystemAnchor : MonoBehaviour
	{
		[Tooltip("Stable unique id, never retyped after first ship: 'core.devtest.alpha'. Built-ins are 'core.*'; mods are 'mod.<author>.*'.")]
		public string AnchorId;

		[Tooltip("Gamemode Ids that place a system at this anchor. Empty = every gamemode may use it (universal anchor).")]
		public string[] GamemodeIds;

		/// <summary>True when this anchor may serve the given gamemode —
		/// the data-overlay test. Empty ownership = universal.</summary>
		public bool IsAvailableTo(string gamemodeId)
		{
			if (GamemodeIds == null || GamemodeIds.Length == 0)
			{
				return true;
			}

			return Array.IndexOf(GamemodeIds, gamemodeId) >= 0;
		}
	}
}
