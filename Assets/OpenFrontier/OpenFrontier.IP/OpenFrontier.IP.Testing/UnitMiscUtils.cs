using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.Testing
{
	public static class UnitMiscUtils
	{
		public static void ParalyseUnit(Unit unit)
		{
			if (unit.Components != null)
			{
				UnitEngineComponent engineComponent = unit.Components.EngineComponent;
				if (engineComponent != null)
				{
					engineComponent.HealthPoints = 0.01f;
				}
				PwrGeneratorComponent powerGenerator = unit.Components.PowerGenerator;
				if (powerGenerator != null)
				{
					powerGenerator.HealthPoints = 0.01f;
				}
				CapacitorComponent capacitor = unit.Components.Capacitor;
				if (capacitor != null)
				{
					capacitor.Charge = 0f;
				}
			}
		}
	}
}
