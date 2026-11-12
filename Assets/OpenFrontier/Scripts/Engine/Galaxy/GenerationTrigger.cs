using UnityEngine;

namespace OpenFrontier.Engine
{
	/// <summary>
	/// The galaxy stage's legal-placement volume — the user's words: systems
	/// are FORCED to generate inside this. The colliders authored on this
	/// object ARE the legal region: content the user shapes in the editor,
	/// never hard-coded bounds. The engine finds this component (wired on
	/// GalaxyStageBoot), validates it, and — when generation arrives (M7)
	/// — samples deterministic system positions strictly inside it.
	///
	/// The authored geometry is floats (editor content); the positions
	/// derived FROM it convert to doubles at the API boundary — galaxy-stage
	/// data are doubles in content data, floats decide only how things are
	/// drawn (pinned origin).
	/// </summary>
	public class GenerationTrigger : MonoBehaviour
	{
		private Collider[] legalShapes;

		/// <summary>True when at least one authored collider exists — the
		/// guard every consumer checks before sampling.</summary>
		public bool HasLegalRegion => legalShapes != null && legalShapes.Length > 0;

		private void Awake()
		{
			RefreshLegalShapes();
		}

		/// <summary>Re-read the authored colliders (call after editing the
		/// shape set at runtime; harmless at authoring time).</summary>
		public void RefreshLegalShapes()
		{
			legalShapes = GetComponents<Collider>();
			if (!HasLegalRegion)
			{
				// Loud: an empty Generation Trigger silently breaks every
				// generation that trusts it, in a way nothing downstream
				// would explain.
				Debug.LogError($"[GenerationTrigger] '{name}' has no colliders - the legal-placement region is EMPTY. Author at least one collider.", this);
			}
		}
	}
}
