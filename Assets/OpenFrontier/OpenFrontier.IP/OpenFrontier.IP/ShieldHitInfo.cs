using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP
{
	public class ShieldHitInfo : MonoBehaviour
	{
		public Quaternion LocalRotation = Quaternion.identity;

		public Vector3 LocalTranslation = Vector3.zero;

		public float StartExpiryTime;

		public float StartTime;

		public Unit TargetUnit;

		public MeshRenderer Renderer;

		public float MaxScale;

		// Open Frontier: which of the 6 shield sections was hit - the
		// effect reads that section's LIVE UI health color every frame.
		public int ShieldIndex;

		// True when the hit emptied the section: the effect pops bright
		// red and dies fast instead of playing the full fade.
		public bool DepletedFlash;

		// When the visual was SPAWNED (not envelope-reset): the scale-in
		// curve (0 -> full over 1s, ease-out) runs off this clock, so
		// repeat hits that reset the flash envelope can't snap or
		// restart the size. Once the visual has faded, the next spawn
		// gets a fresh timestamp and the scale-in plays again.
		public float ScaleInStartTime;

		// Effect lifetime in seconds (short for DepletedFlash, else the
		// renderer's ShieldHitDuration).
		public float Duration;

		// Per-pool-object instanced material (created once from
		// Renderer.material) - the tint goes here.
		public Material CachedMaterial;

		public void SetShieldHitOrientation(Vector3 damageSourceWorldPosition)
		{
			if (TargetUnit != null)
			{
				LocalTranslation = TargetUnit.transform.InverseTransformPoint(damageSourceWorldPosition);
				LocalRotation = Quaternion.LookRotation(LocalTranslation);
			}
		}
	}
}
