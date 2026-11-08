using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	// Tumbles one decorative asteroid-field rock (the visual ones, not
	// the mineable units). AsteroidFieldInstantiator gives each rock a
	// random axis and a size-scaled drift speed at spawn.
	//
	// Rotation is composed from a single accumulated angle that wraps
	// back into (-360, 360) every frame, so the value can never grow
	// into floating-point-noise territory over long sessions. The wrap
	// subtracts exactly one turn instead of snapping to zero:
	// AngleAxis(370) == AngleAxis(10), so the wrap is seamless, while a
	// hard reset to 0 would visibly twitch the rock.
	public class AsteroidFieldRotator : MonoBehaviour
	{
		// Degrees/sec for the SMALLEST rocks (nimble)...
		public const float MaxDegreesPerSecond = 10f;

		// ...and for the LARGEST (they lumber). Sizes in between lerp.
		public const float MinDegreesPerSecond = 2f;

		private Quaternion baseRotation;
		private Vector3 axis;
		private float degreesPerSecond;
		private float angle;

		public void Init(Vector3 rotationAxis, float speed)
		{
			baseRotation = transform.localRotation;
			axis = rotationAxis.normalized;
			degreesPerSecond = speed;
		}

		private void Update()
		{
			// Never initialized (no Init call) - stay frozen rather
			// than feed AngleAxis a zero axis (invalid quaternion).
			if (axis == Vector3.zero)
			{
				return;
			}
			angle += degreesPerSecond * Time.deltaTime;
			if (angle > 360f)
			{
				angle -= 360f;
			}
			else if (angle < -360f)
			{
				angle += 360f;
			}
			transform.localRotation = baseRotation * Quaternion.AngleAxis(angle, axis);
		}
	}
}
