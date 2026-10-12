using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// Editor-time drawings for the parts bolted onto a ship.
	/// </summary>
	/// <remarks>
	/// A component's numbers live in an inspector and its behaviour happens
	/// somewhere you cannot see. A turret's firing arc is the sharpest
	/// example: it is a field on a bay, it decides whether the turret can ever
	/// hit anything, and there is nothing on screen that shows it - so
	/// changing it is guesswork, and a wrong value is invisible until the
	/// turret sits there failing to fire.
	///
	/// Deliberately not wrapped in UNITY_EDITOR. Unity never calls
	/// OnDrawGizmos outside the editor, and Gizmos itself is a stub there, so
	/// the code compiles to nothing useful and costs nothing at runtime - but
	/// it stays inside the conditional-free source, which means the offline
	/// build actually compiles it and a typo here is caught before anyone
	/// opens Unity rather than after.
	///
	/// Every drawing here mirrors a real test in the engine. If one of those
	/// tests changes, this has to change with it, or it starts lying - and a
	/// gizmo that lies is worse than no gizmo, because it is trusted.
	/// </remarks>
	public static class ComponentGizmos
	{
		/// <summary>
		/// Draws the wedge a turret can actually shoot into.
		/// </summary>
		/// <param name="centre">Where the turret is.</param>
		/// <param name="forward">Which way it points, in world space.</param>
		/// <param name="arcDegrees">
		/// The bay's WeaponArc.y - a horizontal arc, in degrees, centred on
		/// the bay's own forward.
		/// </param>
		/// <param name="range">
		/// How far it reaches, so the wedge has a radius rather than being an
		/// infinitely thin slice.
		/// </param>
		/// <remarks>
		/// Horizontal, because the test is horizontal:
		/// TurretComponent.IsLocalDirectionInFiringArcInternal wraps the
		/// direction's Y bearing and compares it against WeaponArc.y / 2. There
		/// is no pitch component in that test at all, so a gizmo drawn as a
		/// vertical cone would be showing a constraint the game does not have.
		///
		/// A WeaponArc.y of 360 or more means unrestricted, which the engine
		/// short-circuits rather than measuring, so it is drawn as a full ring
		/// to say "everywhere" instead of as a wedge with a seam down the back.
		/// </remarks>
		public static void DrawFiringArc(
			Vector3 centre, Vector3 forward, float arcDegrees, float range)
		{
			if (range <= 0f)
			{
				return;
			}

			forward = forward.normalized;

			if (forward.sqrMagnitude < 0.5f)
			{
				return;
			}

			if (arcDegrees >= 360f)
			{
				// Unrestricted. Drawn as a ring so it reads as "anywhere"
				// rather than as an arc with a seam where the wrap happens.
				Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.9f);
				DrawRing(centre, range, 48);

				return;
			}

			if (arcDegrees <= 0f)
			{
				return;
			}

			float half = arcDegrees / 2f;
			Vector3 left = Quaternion.AngleAxis(-half, Vector3.up) * forward * range;
			Vector3 right = Quaternion.AngleAxis(half, Vector3.up) * forward * range;

			Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.18f);
			Gizmos.DrawTriangle(centre, centre + left, centre + right);

			Gizmos.color = new Color(1f, 0.6f, 0.15f, 1f);
			Gizmos.DrawLine(centre, centre + left);
			Gizmos.DrawLine(centre, centre + right);

			// The far edge, so the arc's width is readable at a glance and not
			// only where it meets the turret.
			Gizmos.DrawLine(centre + left, centre + right);

			// The centre line, which is the axis the arc is measured from.
			Gizmos.color = new Color(1f, 0.6f, 0.15f, 0.45f);
			Gizmos.DrawLine(centre, centre + forward * range);
		}

		/// <summary>
		/// Draws a flat ring, used for firing range.
		/// </summary>
		/// <remarks>
		/// Flat in X and Z because that is how the engine measures distance:
		/// TurretComponent.GetDistance2D ignores height entirely, so a sphere
		/// would show a reach the turret does not have.
		/// </remarks>
		public static void DrawRangeRing(Vector3 centre, float radius, Color color, int segments = 48)
		{
			if (radius <= 0f)
			{
				return;
			}

			Gizmos.color = color;

			Vector3 previous = centre + new Vector3(radius, 0f, 0f);

			for (int i = 1; i <= segments; i++)
			{
				float angle = (i / (float)segments) * Mathf.PI * 2f;
				Vector3 point = centre + new Vector3(
					Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

				Gizmos.DrawLine(previous, point);
				previous = point;
			}
		}

		/// <summary>A private ring helper, so the arc drawing can reuse it.</summary>
		private static void DrawRing(Vector3 centre, float radius, int segments)
		{
			DrawRangeRing(centre, radius, Gizmos.color, segments);
		}
	}
}