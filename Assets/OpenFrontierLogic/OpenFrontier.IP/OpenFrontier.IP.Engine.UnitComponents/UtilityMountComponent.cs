using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// The designator a laser-guided missile rides on, and the thing that makes
	/// the laser seeker the most fragile of the four.
	/// </summary>
	/// <remarks>
	/// A laser seeker cannot see anything by itself. It is riding a beam the
	/// launching ship is pointing at the target, so the beam has to still be
	/// there - which means the launcher has to still be designating that
	/// target, and this mount has to have swung round onto it.
	///
	/// That is why it covers every bearing rather than a wedge: nothing ever
	/// rotates an ActiveTurret, and a launcher sits at the hull's rotation, so
	/// a designator that inherited a turret's arc would be blind most of the
	/// time. It does not look down either, because in a world where units are
	/// Y-frozen there is no down to look towards - <c>RigidBodyFreezeY</c> is
	/// true by default and only three prefabs in the game unfreeze it.
	///
	/// What limits it instead is how fast it can swing. A target that
	/// outmanoeuvres the mount falls out of the designator, the beam drops, and
	/// every laser missile riding it goes blind at once. That is the whole
	/// reason this component exists: it turns "laser guided" from a word on a
	/// warning tone into something the target can do something about.
	///
	/// Nothing here is drawn. The mount has no visible part, and the only
	/// thing a player can observe is whether a laser missile stays locked.
	/// </remarks>
	public class UtilityMountComponent : ComponentBase
	{
		public ComponentClass ModuleClass;

		public override ComponentClass ComponentClass => ModuleClass;

		private UtilityMountClass Mount => ModuleClass as UtilityMountClass;

		/// <summary>What the mount is currently holding its beam on.</summary>
		private Unit designated;

		/// <summary>When the beam was last asked for. Idle beams are dropped.</summary>
		private float lastRequestedAt;

		/// <summary>
		/// Where the mount is actually pointing, as an absolute bearing in
		/// degrees. Separate from the target's bearing, and the gap between the
		/// two is the mount slewing.
		/// </summary>
		private float mountBearing;

		/// <summary>Whether the mount has ever pointed anywhere yet.</summary>
		private bool hasBearing;

		/// <summary>What the beam is on, or null when it is on nothing.</summary>
		public Unit Designated => designated;

		/// <summary>
		/// Whether the beam is on <paramref name="target"/> well enough for a
		/// missile to ride it.
		/// </summary>
		public bool IsLasing(Unit target)
		{
			if (designated == null || target == null || designated != target)
			{
				return false;
			}
			UtilityMountClass mount = Mount;
			if (mount == null)
			{
				return false;
			}
			// A ship that is not flying cannot keep a beam on anything, and a
			// docked one certainly should not be lasing from a station.
			if (Unit == null || !Unit.IsActiveInEngine || Unit.IsDocked)
			{
				return false;
			}
			return Mathf.Abs(BearingErrorTo(target)) <= mount.LasingTolerance;
		}

		/// <summary>
		/// Puts the beam on a target. Called when this ship launches something
		/// that needs one.
		/// </summary>
		/// <remarks>
		/// This is deliberately the only way to get a beam, rather than
		/// following whatever the ship happens to have selected. That keeps the
		/// mount independent of the HUD, which only the player's ship has, and
		/// means an AI ship designates exactly the same way a player's does.
		///
		/// It also gives deselection for free. Stop shooting at a ship, or
		/// start shooting at a different one, and the beam moves - which is
		/// what losing the laser by deselection means.
		/// </remarks>
		public void Designate(Unit target)
		{
			if (target == null)
			{
				return;
			}
			designated = target;
			lastRequestedAt = Time.time;
		}

		/// <summary>
		/// Drops the beam, for when the ship stops needing it entirely.
		/// </summary>
		public void ClearDesignation()
		{
			designated = null;
		}

		private void Update()
		{
			if (designated == null)
			{
				return;
			}
			UtilityMountClass mount = Mount;
			if (mount == null)
			{
				return;
			}
			// A beam nobody is riding is a beam that should have been dropped.
			// Without this a ship would keep one forever after its last shot.
			if (Time.time - lastRequestedAt > mount.DesignationTimeout)
			{
				designated = null;
				return;
			}
			if (designated.transform == null)
			{
				designated = null;
				return;
			}
			float wanted = BearingTo(designated);
			if (!hasBearing)
			{
				// First acquisition starts the mount on the target rather than
				// on whatever bearing it happened to be left at.
				mountBearing = wanted;
				hasBearing = true;
				return;
			}
			mountBearing = Mathf.MoveTowards(mountBearing, wanted, mount.SlewRate * Time.deltaTime);
		}

		/// <summary>
		/// How far the mount's bearing is from where the target is, wrapped to
		/// the shortest way round so crossing due north is not a 350 degree
		/// swing.
		/// </summary>
		private float BearingErrorTo(Unit target)
		{
			return Geometry.WrapDegreesForBearing(BearingTo(target) - mountBearing);
		}

		private float BearingTo(Unit target)
		{
			Vector3 offset = target.transform.position - transform.position;
			Vector3 flat = new Vector3(offset.x, 0f, offset.z);
			if (flat.sqrMagnitude <= 0.000001f)
			{
				return mountBearing;
			}
			return Quaternion.LookRotation(flat, Vector3.up).eulerAngles.y;
		}
	}
}
