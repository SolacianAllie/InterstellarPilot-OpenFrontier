using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// The module half of a utility mount: what one can do, and what it costs
	/// to run.
	/// </summary>
	/// <remarks>
	/// Utility mounts are the family <see cref="ComponentCategory.UtilityMount"/>
	/// describes - no class at all, only a quality rating - so two mounts
	/// differ purely by how fast they swing and how tightly they hold. There
	/// is nothing to fit a size against, which is why this extends
	/// PowerConsumerClass rather than anything that wants a class: the only
	/// numbers that matter are the ones below.
	/// </remarks>
	public class UtilityMountClass : PowerConsumerClass
	{
		[Tooltip("How fast the mount can swing its beam onto a new bearing, in degrees per second. This is the rate a fast target has to beat.")]
		public float SlewRate = 40f;

		[Tooltip("How close the mount's beam has to be to the target's bearing, in degrees, before it counts as holding it. The beam drifting outside this is what blinds every laser missile riding it.")]
		public float LasingTolerance = 6f;

		[Tooltip("Seconds a beam survives without anything asking for it before the mount drops it. Stops a ship lasing a target forever after its last shot.")]
		public float DesignationTimeout = 3f;

		protected override ComponentBase createComponent(GameObject target)
		{
			UtilityMountComponent utilityMount = target.AddComponent<UtilityMountComponent>();
			utilityMount.ModuleClass = this;
			return utilityMount;
		}
	}
}
