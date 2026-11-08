using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class TestingChangePlayerShipcs : MonoBehaviour
	{
		public int ButtonHeight = 50;

		public int ButtonWidth = 100;

		private EngineASX engine;

		public Vector3 PositionOffset = Vector3.zero;

		public void Awake()
		{
			engine = EngineASX.Instance;
		}

		private void OnGUI()
		{
			if (engine == null)
			{
				engine = EngineASX.Instance;
			}
			if (engine != null)
			{
				int num = (int)PositionOffset.y;
				int num2 = (int)PositionOffset.x;
				{
					foreach (UnitClass unitClass in engine.UnitClasses)
					{
						if (unitClass.IsPilottable)
						{
							if (num + ButtonHeight >= Screen.height)
							{
								num2 += ButtonWidth;
								num = (int)PositionOffset.y;
							}
							if (GUI.Button(new Rect(num2, num, ButtonWidth, ButtonHeight), unitClass.GetClassAndSeriesName()))
							{
								TryChangePlayerShip(unitClass);
							}
							num += ButtonHeight + 10;
						}
					}
					return;
				}
			}
			engine = EngineASX.Instance;
		}

		private void TryChangePlayerShip(UnitClass unitClass)
		{
			Unit playerUnit = engine.PlayerUnit;
			if (playerUnit != null)
			{
				Unit component = Object.Instantiate(unitClass.UnitPrefab.gameObject, playerUnit.transform.position, playerUnit.transform.rotation).GetComponent<Unit>();
				component.Init();
				component.Components.AutoAssignShipName();
				component.Components.InstallDefaultComponents();
				component.Sector = playerUnit.Sector;
				component.Components.PilotPerson = engine.LocalPlayer.Person;
			}
			playerUnit.SafeDestroy();
			if (engine.Hud != null)
			{
				engine.Hud.ShipComponentsController.TurretGrid.Refresh();
			}
		}
	}
}
