using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud.CurrentTargetUI.WeaponsReadyIndicator
{
	public class WeaponsReadyIndicatorController : MonoBehaviour
	{
		private WeaponsReadyIndicatorItem[] pool;

		private UnitComponentHolder oldShip;

		private List<TurretComponent> turrets = new List<TurretComponent>(16);

		private float lastTimeRefreshedTurrets;

		private void Awake()
		{
			pool = GetComponentsInChildren<WeaponsReadyIndicatorItem>();
			WeaponsReadyIndicatorItem[] array = pool;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(value: false);
			}
		}

		private bool ShouldShowIndicator(TurretComponent turretComponent)
		{
			if (turretComponent is LaserTurretComponent)
			{
				return true;
			}
			if (turretComponent is ProjectileTurretComponent projectileTurretComponent)
			{
				if (projectileTurretComponent.ProjectileTurretClass.ProjectileTurretType != TurretType.Missiles)
				{
					return projectileTurretComponent.ProjectileTurretClass.ProjectileTurretType == TurretType.Default;
				}
				return true;
			}
			return false;
		}

		public void Refresh(UnitComponentHolder ship)
		{
			if (ship != oldShip || Time.time > lastTimeRefreshedTurrets + 4f)
			{
				lastTimeRefreshedTurrets = Time.time;
				RecacheShipTurrets(ship);
			}
			for (int i = 0; i < turrets.Count; i++)
			{
				TurretComponent turretComponent = turrets[i];
				if (turretComponent != null)
				{
					pool[i].Refresh(turretComponent);
				}
			}
		}

		private void RecacheShipTurrets(UnitComponentHolder ship)
		{
			turrets.Clear();
			oldShip = ship;
			int num = 0;
			if (ship != null)
			{
				foreach (TurretComponent item in ship.Turrets.OrderBy((TurretComponent e) => (int)e.TurretClass.DefaultTurretGroup))
				{
					if (ShouldShowIndicator(item) && num < pool.Length)
					{
						turrets.Add(item);
						pool[num].gameObject.SetActive(value: true);
						num++;
					}
				}
			}
			for (int num2 = num; num2 < pool.Length; num2++)
			{
				pool[num2].gameObject.SetActive(value: false);
			}
		}
	}
}
