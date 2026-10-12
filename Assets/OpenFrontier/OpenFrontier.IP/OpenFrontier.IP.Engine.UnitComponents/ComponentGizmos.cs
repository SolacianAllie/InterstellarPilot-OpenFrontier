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
		/// Draws everything worth seeing about a slot: its size, and the arc
		/// and reach of whatever is in it.
		/// </summary>
		/// <remarks>
		/// On the bay rather than the turret, because the bay is where the
		/// editable numbers live - WeaponArc and Size are both ComponentBay
		/// fields - and because a bay is what you select when you want to
		/// change them. Gizmos are per selected object, so a drawing hung off
		/// the turret component only appears when the turret is selected, and
		/// nobody selects the turret when the thing they are changing is the
		/// slot it sits in.
		///
		/// Works on a prefab as well as a live ship: a ship being edited has
		/// no installed component yet, only the one it would start with, so
		/// both are looked at.
		/// </remarks>
		public static void DrawBay(ComponentBay bay)
		{
			if (bay == null)
			{
				return;
			}

			DrawBaySize(bay);

			// What the slot would hold, or does hold. Same thing on a prefab.
			ComponentClass componentClass = bay.InstalledComponentClass != null
				? bay.InstalledComponentClass
				: bay.InitialComponentClass;

			if (componentClass == null)
			{
				return;
			}

			// Only weapons have an arc and a range; a capacitor bay has
			// neither, and drawing a wedge for one would claim a limit it
			// does not have.
			TurretClass turretClass = componentClass as TurretClass;

			if (turretClass == null)
			{
				// Nothing in it. Only worth saying for a slot that could
				// hold a weapon - an empty capacitor bay is a perfectly
				// ordinary thing, and marking every one of them would make
				// the one that matters invisible.
				if (HoldsTurrets(bay))
				{
					DrawEmptyHardpoint(bay);
				}

				return;
			}

			if (!componentClass.IgnoreFiringArc)
			{
				DrawFiringArc(
					bay.transform.position,
					bay.transform.forward,
					bay.WeaponArc.y,
					turretClass.MaxFiringRange);

				DrawRangeRing(
					bay.transform.position, turretClass.MaxFiringRange,
					new Color(1f, 0.6f, 0.15f, 0.5f));
			}
			else
			{
				// Can shoot in any direction at all, so there is no wedge to
				// draw. Green, because that is the colour DrawFiringArc uses
				// for an unrestricted arc, and a weapon that ignores its arc
				// should not look like a broken one.
				DrawRangeRing(
					bay.transform.position, turretClass.MaxFiringRange,
					new Color(0.4f, 1f, 0.4f, 0.7f));
			}

			if (turretClass.MinFiringRange > 0f)
			{
				DrawRangeRing(
					bay.transform.position, turretClass.MinFiringRange,
					new Color(1f, 0.25f, 0.25f, 0.7f));
			}
		}

		/// <summary>
		/// Whether this slot could hold a weapon, empty or not.
		/// </summary>
		/// <remarks>
		/// Read off the data rather than guessed from the BayType enum, which
		/// has five values that take a TurretClass and not the two that seemed
		/// obvious: Turret and Mining are most of them, but Countermeasure,
		/// Tractor and Mine each hold one or two as well. Guessing the two
		/// would have left every countermeasure and tractor hardpoint on a
		/// ship unmarked while it was being edited - which is most of the point
		/// of drawing this at all.
		///
		/// Counted from all 25 TurretClass prefabs and their ComponentBayType
		/// references. If a new weapon type appears on a new kind of slot, this
		/// is the list that has to learn about it.
		/// </remarks>
		private static bool HoldsTurrets(ComponentBay bay)
		{
			if (bay == null || bay.BayType == null)
			{
				return false;
			}

			switch (bay.BayType.BayType)
			{
				case BayType.Turret:
				case BayType.Mining:
				case BayType.Countermeasure:
				case BayType.TractorBeam:
				case BayType.Mine:
					return true;

				default:
					return false;
			}
		}

		/// <summary>
		/// Marks a weapon slot with nothing in it.
		/// </summary>
		/// <remarks>
		/// A sphere, and the only sphere this file draws. The size is a cube,
		/// the arc is orange, the ranges are flat rings - so a small red ball
		/// is unmistakably the fourth thing, which is what it means.
		///
		/// Red because it is the one thing here that is wrong. An empty
		/// capacitor bay is normal and gets no marker at all; an empty
		/// hardpoint is not, and drawing it is the whole reason for this.
		/// </remarks>
		private static void DrawEmptyHardpoint(ComponentBay bay)
		{
			int size = (int)bay.Size + 1;

			Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.95f);
			Gizmos.DrawWireSphere(bay.transform.position, size * 0.35f);
		}

		/// <summary>
		/// Draws a slot's size, so slots can be told apart at a glance.
		/// </summary>
		/// <remarks>
		/// Gizmos carry no text, so the only way to say which size a slot is
		/// is to make bigger slots visibly bigger. One unit per size, so the
		/// four are told apart from each other rather than from the arc, which
		/// is drawn much larger and is about a different thing.
		///
		/// Deliberately small and drawn in cool colours: this is the size, and
		/// the arc is orange. Two things about a slot that are easy to confuse
		/// is one too many.
		/// </remarks>
		private static void DrawBaySize(ComponentBay bay)
		{
			int size = (int)bay.Size + 1;

			Gizmos.color = ColourForSize(bay.Size);
			Gizmos.DrawWireCube(bay.transform.position, Vector3.one * size * 0.4f);
		}

		/// <summary>A colour per size, so they are told apart without a label.</summary>
		private static Color ColourForSize(HardpointSize size)
		{
			switch (size)
			{
				case HardpointSize.Medium:
					return new Color(0.4f, 0.85f, 1f, 0.9f);

				case HardpointSize.Large:
					return new Color(0.6f, 0.6f, 1f, 0.9f);

				case HardpointSize.Huge:
					return new Color(0.85f, 0.5f, 1f, 0.95f);

				default:
					return new Color(0.5f, 1f, 0.75f, 0.9f);
			}
		}

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

			// The fill, as a fan of lines rather than a solid triangle.
			//
			// Gizmos has no DrawTriangle. It has DrawLine, DrawLineStrip,
			// DrawMesh, DrawCube and a handful of others, and a filled wedge
			// wants a polygon - which is why this was written against a method
			// that does not exist.
			//
			// A mesh would fill it properly, but Gizmos.DrawMesh wants one per
			// call and a gizmo runs every frame while the object stays
			// selected, so that means a mesh built every frame or a cache with
			// a leak in it. Lines cost nothing, allocate nothing, and one
			// every few degrees reads as solid from any angle you would edit
			// a ship from.
			int slices = Mathf.Clamp(Mathf.CeilToInt(arcDegrees / 4f), 3, 180);

			Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.09f);

			for (int i = 0; i <= slices; i++)
			{
				float angle = -half + (arcDegrees * i / slices);
				Vector3 edge = Quaternion.AngleAxis(angle, Vector3.up) * forward * range;

				Gizmos.DrawLine(centre, centre + edge);
			}

			// The far edge, so the arc's width is readable at a glance and not
			// only where it meets the turret.
			int arcSlices = Mathf.Clamp(Mathf.CeilToInt(arcDegrees / 4f), 3, 180);
			Vector3 previous = centre + left;

			for (int i = 1; i <= arcSlices; i++)
			{
				float angle = -half + (arcDegrees * i / arcSlices);
				Vector3 point = centre
					+ Quaternion.AngleAxis(angle, Vector3.up) * forward * range;

				Gizmos.DrawLine(previous, point);
				previous = point;
			}

			Gizmos.color = new Color(1f, 0.6f, 0.15f, 1f);
			Gizmos.DrawLine(centre, centre + left);
			Gizmos.DrawLine(centre, centre + right);

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