using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// A heat vent: sheds heat from the ship it is fitted to.
	/// </summary>
	/// <remarks>
	/// Vents are hull equipment. They are hidden from the trade screen so a
	/// player cannot buy or sell one, and a ship fits as many as its hull
	/// calls for. Their rates sum - two class 3 vents shed twice what one does
	/// - and the total is scaled by the Systems pips, so a ship that has
	/// committed its power to weapons vents more slowly.
	///
	/// What they shed is not modelled yet. There is no heat system, so
	/// VentRate is currently a number nothing reads. The Elite Heat Vents page
	/// carries no specification table at all - it describes fins that radiate
	/// heat into space and notes that a better power plant lowers the baseline
	/// so the vents are used less - so these rates are a provisional ladder,
	/// not transcribed figures.
	/// </remarks>
	public class ThermalVentClass : ComponentClass
	{
		[Tooltip("Heat per second this vent sheds from its ship. Vents sum, and the total is scaled by the Systems pips. Zero on every other component, which is why fitting one changes nothing until a heat system exists.")]
		public float VentRate;

		protected override ComponentBase createComponent(GameObject target)
		{
			CoreInternalComponent coreInternalComponent = target.AddComponent<CoreInternalComponent>();
			coreInternalComponent.ModuleClass = this;
			return coreInternalComponent;
		}
	}
}