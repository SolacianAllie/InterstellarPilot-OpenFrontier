using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// Runtime half of a new-system core internal that has no gameplay hook yet.
	/// </summary>
	/// <remarks>
	/// ComponentBase declares one abstract member, ComponentClass, so a single
	/// component can back every data-only class. It cannot simply be left null:
	/// ComponentBay.InstallComponent calls CreateComponent and then
	/// dereferences the result with no null check, so a class with no runtime
	/// component would throw the moment it was fitted.
	/// </remarks>
	public class CoreInternalComponent : ComponentBase
	{
		public ComponentClass ModuleClass;

		public override ComponentClass ComponentClass => ModuleClass;
	}
}
