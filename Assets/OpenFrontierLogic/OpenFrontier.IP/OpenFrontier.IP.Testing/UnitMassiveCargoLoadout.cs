using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Testing
{
	[RequireComponent(typeof(Unit))]
	public class UnitMassiveCargoLoadout : MonoBehaviour
	{
		[ContextMenu("Apply")]
		public void Apply()
		{
			Unit component = GetComponent<Unit>();
			foreach (CargoClass cargoClass in EngineASX.Instance.CargoClasses)
			{
				component.Components.CargoBayComponent.AddToCargo(cargoClass, 999, ignoreCapacity: true);
			}
		}
	}
}
