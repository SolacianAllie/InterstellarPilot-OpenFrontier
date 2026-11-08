using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class TurretGridUI : ScrollList<TurretComponent>
	{
		public bool EquipmentOnly;

		[SerializeField]
		private HudScreen hud;

		public HudScreen Hud
		{
			get
			{
				return hud;
			}
			private set
			{
				hud = value;
			}
		}

		public void ToggleTurretActivated(TurretComponent turret)
		{
			if (turret.IsFiring)
			{
				turret.CancelFiring();
			}
			else if (turret.IsReadyToFire(hud.CurrentTarget))
			{
				turret.Fire(hud.CurrentTarget);
				if (turret.IsMineDropper)
				{
					turret.Unit.Sector.RegisterMineDeployment();
				}
			}
		}

		public static bool CanCancelWeaponFire(TurretComponent turret)
		{
			return turret.CanCancelFire();
		}

		public static bool CanToggleTurretActivated(TurretComponent turret, Unit currentTarget)
		{
			if (turret.AutoFireActive)
			{
				return false;
			}
			if (turret.IsFiring)
			{
				return CanCancelWeaponFire(turret);
			}
			return CanFireTurret(turret, currentTarget);
		}

		public static bool CanFireTurret(TurretComponent turret, Unit currentTarget)
		{
			if (turret.IsWeapon && currentTarget != null && turret.TurretClass.RequiresTarget)
			{
				if (currentTarget.IsPlayerOrAllied())
				{
					if (!GameController.Instance.PlayerOptions.General_AllowFireAtAllied)
					{
						return false;
					}
				}
				else if (!EngineASX.Instance.LocalFaction.IsHostileTo(currentTarget) && !GameController.Instance.PlayerOptions.General_AllowFireAtNeutral)
				{
					return false;
				}
			}
			return turret.IsReadyToFire(currentTarget);
		}

		protected override bool DetermineIsStale()
		{
			return false;
		}

		protected override void OnRefreshing()
		{
			base.OnRefreshing();
			Unit playerUnit = Engine.PlayerUnit;
			if (playerUnit != null)
			{
				SetItems(from e in playerUnit.Components.Turrets
					where ShouldShowComponent(e)
					orderby (int)e.TurretClass.DefaultTurretGroup
					select e);
			}
		}

		public bool ShouldShowComponent(ComponentBase component)
		{
			BayType bayType = component.ComponentClass.ComponentBayType.BayType;
			if (bayType == BayType.Electronics || bayType == BayType.TractorBeam)
			{
				return EquipmentOnly;
			}
			if (component.ComponentClass is TurretClass { IsPointDefence: not false } && !GameController.Instance.PlayerOptions.UI_ShowPointDefenceTurrets)
			{
				return false;
			}
			return !EquipmentOnly;
		}

		public void OnPlayerUnitChanged()
		{
			foreach (TurretGridItemUI item in UIItemPool)
			{
				item.OnPlayerUnitChanged();
			}
		}
	}
}
