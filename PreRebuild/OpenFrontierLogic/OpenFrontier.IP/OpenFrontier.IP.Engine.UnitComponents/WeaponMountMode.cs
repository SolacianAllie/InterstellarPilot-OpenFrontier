namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// How a weapon is mounted, which decides how far it can aim.
	/// </summary>
	/// <remarks>
	/// From the Elite weapon tables, which give every laser a Fixed, Gimbal or
	/// Turret variant. Fixed fires straight ahead and cannot aim, Gimbal tracks
	/// within a limited arc, and Turret tracks freely - which is why the same
	/// weapon number appears three times per class in those tables.
	///
	/// Stored but not yet acted on. A fixed mount drawing a full firing arc
	/// would be a lie in the scene view, and this is worth fixing when the
	/// per-weapon aiming rule is built rather than faked now.
	/// </remarks>
	public enum WeaponMountMode
	{
		/// <summary>Fires straight ahead. Cannot track a target.</summary>
		Fixed = 0,

		/// <summary>Tracks within a limited arc.</summary>
		Gimbal = 1,

		/// <summary>Tracks freely.</summary>
		Turret = 2
	}
}