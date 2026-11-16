using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// The per-system hook (spec §8), in its rendering-only shape for the
	/// explorable miniature: it holds WHERE this system sits in the galaxy.
	///
	/// The explorable scene renders the sky from wherever the CAMERA is -
	/// the band projects through the galactic plane from the camera
	/// position, the dust and glow tap the same rays, and the star catalog
	/// is real 3D content - so there are no uniforms to push per system and
	/// no re-bake on a jump. ED re-bakes because its band texture is built
	/// from the observer's point of view; our ray-plane projection does
	/// that work at draw time instead, so the bake is position-independent.
	///
	/// What this component is FOR is the capture that follows: the gameplay
	/// skybox will be rendered FROM this scene - a capture camera docked at
	/// the system's galactic position inside the miniature. The "Sky
	/// Observer" child is that docking point. This component's own space is
	/// the cloud's local space - the galaxy's own frame - so
	/// galacticPosition is expressed on the galaxy's axes, tilt included.
	/// </summary>
	[DisallowMultipleComponent]
	public class GalaxySky : MonoBehaviour
	{
		[Tooltip("Galaxy-local position of this system (the cloud's local axes - the galaxy's own frame).")]
		public Vector3 galacticPosition;

		[Tooltip("The capture's docking point, kept at galacticPosition. Created on demand as a child 'Sky Observer'.")]
		public Transform observerAnchor;

		/// <summary>Moves (or creates) the Sky Observer to galacticPosition.</summary>
		[ContextMenu("Place Sky Observer Anchor")]
		public void ApplyPosition()
		{
			if (observerAnchor == null)
			{
				Transform existing = transform.Find("Sky Observer");
				observerAnchor = existing != null ? existing : new GameObject("Sky Observer").transform;
				observerAnchor.SetParent(transform, false);
			}
			observerAnchor.localPosition = galacticPosition;
			observerAnchor.localRotation = Quaternion.identity;
		}

		/// <summary>Authoring convenience: a typed-in position moves an existing anchor (never creates one).</summary>
		void OnValidate()
		{
			if (observerAnchor != null)
			{
				observerAnchor.localPosition = galacticPosition;
			}
		}

		void OnDrawGizmosSelected()
		{
			Transform anchor = observerAnchor != null ? observerAnchor : transform.Find("Sky Observer");
			if (anchor == null) return;
			Gizmos.color = new Color(0.4f, 0.8f, 1f);
			Gizmos.DrawSphere(anchor.position, 12f);
			Gizmos.DrawLine(anchor.position, anchor.position + anchor.up * 60f);
		}
	}
}
