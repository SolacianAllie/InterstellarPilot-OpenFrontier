using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud.ShipComponents
{
	public class ShipComponentsController : MonoBehaviour
	{
		public TurretGridUI TurretGrid;

		public ShipComponentUI PowerGeneratorComponentUI;

		public ShipComponentUI CapacitorComponentUI;

		public ShipComponentUI EngineComponentUI;

		public ShipComponentUI ShieldComponentUI;

		public Unit PlayerUnit;

		public UnitComponentHolder PlayerUnitComponents
		{
			get
			{
				if (PlayerUnit != null)
				{
					return PlayerUnit.Components;
				}
				return null;
			}
		}

		public void OnPlayerUnitChanged()
		{
			TurretGrid.OnPlayerUnitChanged();
			PowerGeneratorComponentUI.ClearFlashState();
			CapacitorComponentUI.ClearFlashState();
			EngineComponentUI.ClearFlashState();
			ShieldComponentUI.ClearFlashState();
		}

		public void Refresh()
		{
			TurretGrid.Refresh();
			UnitComponentHolder playerUnitComponents = PlayerUnitComponents;
			if (playerUnitComponents != null)
			{
				PowerGeneratorComponentUI.Component = playerUnitComponents.PowerGenerator;
				ShieldComponentUI.Component = playerUnitComponents.ShieldComponent;
				EngineComponentUI.Component = playerUnitComponents.EngineComponent;
				CapacitorComponentUI.Component = playerUnitComponents.Capacitor;
			}
		}
	}
}
