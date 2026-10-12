using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class TurretUI : MonoBehaviour
	{
		private const float timeBetweenAutoLoadTurret = 1.2f;

		public const float BayNameDisplayDuration = 3f;

		private static List<ActiveTurret> cache = new List<ActiveTurret>();

		private float bayNameExpiryTime;

		public Text BayNameLabel;

		private ActiveProjectileTurret currentProjectileTurret;

		private ActiveTurret currentActiveTurret;

		public HudScreen Hud;

		private float lastTurretConditionValue = -1f;

		private float nextAutoLoadTurret;

		public Color NotReadyColor = Color.red;

		public Color ReadyColor = Color.green;

		public Button SwitchAmmoButton;

		public Slider TurretEnergySlider;

		public Slider TurretHealthSlider;

		public Text TurretLabel;

		public ActiveTurret CurrentTurret
		{
			get
			{
				return currentActiveTurret;
			}
			set
			{
				if (currentActiveTurret != value)
				{
					ActiveTurret activeTurret = currentActiveTurret;
					currentActiveTurret = value;
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"{this}: Changed turret to: {currentActiveTurret}", this, 2);
					}
					if (activeTurret != null)
					{
						activeTurret.Fired -= currentTurret_Fired;
					}
					if (currentActiveTurret != null)
					{
						currentActiveTurret.Fired += currentTurret_Fired;
					}
					OnCurrentTurretChanged();
				}
			}
		}

		public void ToggleTurret()
		{
			if (Hud != null && Hud.PlayerUnit != null && Hud.PlayerUnit.Components.ActiveUnitComponents.ActiveTurrets.Count > 0)
			{
				cache.Clear();
				cache.AddRange(from e in Hud.PlayerUnit.Components.ActiveUnitComponents.ActiveTurrets
					orderby e.TurretClass.DefaultTurretGroup, e.TurretClass.GetFriendlyName()
					select e);
				int num = -1;
				if (currentActiveTurret != null)
				{
					num = cache.IndexOf(currentActiveTurret);
				}
				int num2 = num + 1;
				if (num2 >= cache.Count)
				{
					num2 = 0;
				}
				CurrentTurret = cache[num2];
				BayNameLabel.gameObject.SetActive(value: true);
				BayNameLabel.text = currentActiveTurret.TurretComponent.Bay.name;
				bayNameExpiryTime = Time.time + 3f;
			}
			else
			{
				CurrentTurret = null;
			}
		}

		public void UpdateLabels()
		{
			bool flag = currentActiveTurret != null;
			TurretLabel.gameObject.SetActive(flag);
			TurretEnergySlider.gameObject.SetActive(flag);
			if (!flag)
			{
				return;
			}
			if (currentProjectileTurret != null)
			{
				if (currentProjectileTurret.CurProjectileClass != null)
				{
					if (currentProjectileTurret.CurProjectileClass.AmmoClass != null)
					{
						TurretLabel.text = $"{currentActiveTurret.Unit.GetCargoCountOf(currentProjectileTurret.CurProjectileClass.AmmoClass)}x {currentProjectileTurret.CurProjectileClass.name}";
					}
					else
					{
						TurretLabel.text = currentProjectileTurret.CurProjectileClass.name;
					}
				}
				else
				{
					TurretLabel.text = $"{currentActiveTurret.TurretClass.GetFriendlyName()} - Empty";
				}
			}
			else
			{
				TurretLabel.text = currentActiveTurret.TurretClass.GetFriendlyName();
			}
		}

		public void SwitchAmmo()
		{
			if (currentProjectileTurret != null && !currentProjectileTurret.IsFiring)
			{
				currentProjectileTurret.ProjectileTurret.TrySelectNextProjectileClass(ignoreAmmo: false, conventionalWeaponsOnly: false);
				UpdateLabels();
			}
		}

		public void FireTurret()
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Player firing selected weapon", this, 3);
			}
			if (currentActiveTurret != null && !currentActiveTurret.AutoFire)
			{
				if (currentActiveTurret.IsFiring && currentActiveTurret is ActiveTractorTurret)
				{
					currentActiveTurret.CancelFiring();
				}
				else
				{
					Unit currentTarget = Hud.CurrentTarget;
					if (currentActiveTurret.TurretComponent.IsReadyToFire(currentTarget))
					{
						if (LogWrapper.LogMsgs)
						{
							LogWrapper.Log($"Player firing turret: {currentActiveTurret.TurretComponent}", this, 3);
						}
						currentActiveTurret.TurretComponent.Fire(currentTarget);
						if (currentActiveTurret.TurretComponent.IsMineDropper)
						{
							currentActiveTurret.Sector.RegisterMineDeployment();
						}
					}
				}
			}
			UpdateLabels();
		}

		public void TryFireSelectedTurret()
		{
			if (currentActiveTurret != null && (!currentActiveTurret.TurretClass.RequiresTarget || (Hud.CurrentTarget != null && currentActiveTurret.CanFireAt(Hud.CurrentTarget))))
			{
				FireTurret();
			}
		}

		public void Refresh()
		{
			UpdateLabels();
			RefreshButtonStates();
		}

		private void Start()
		{
			OnCurrentTurretChanged();
			ToggleTurret();
		}

		private void Update()
		{
			if (currentActiveTurret != null)
			{
				if (BayNameLabel.gameObject.activeSelf && Time.time > bayNameExpiryTime)
				{
					BayNameLabel.gameObject.SetActive(value: false);
				}
				if (currentProjectileTurret != null)
				{
					UpdateAutoAmmoLoad();
				}
				RefreshButtonStates();
				float healthNormalized = currentActiveTurret.TurretComponent.HealthNormalized;
				if (healthNormalized != lastTurretConditionValue)
				{
					lastTurretConditionValue = healthNormalized;
					TurretHealthSlider.value = healthNormalized;
					TurretHealthSlider.gameObject.SetActive(healthNormalized < 1f);
				}
				TurretEnergySlider.value = currentActiveTurret.ChargedEnergyNormalized;
			}
			else
			{
				ToggleTurret();
			}
		}

		private void UpdateAutoAmmoLoad()
		{
			if (Time.time > nextAutoLoadTurret)
			{
				AutoLoadTurrets();
				nextAutoLoadTurret = Time.time + 1.2f;
			}
		}

		private void AutoLoadTurrets()
		{
			Unit playerUnit = Hud.PlayerUnit;
			if (!(playerUnit != null))
			{
				return;
			}
			for (int i = 0; i < playerUnit.Components.Turrets.Count; i++)
			{
				ProjectileTurretComponent projectileTurretComponent = playerUnit.Components.Turrets[i] as ProjectileTurretComponent;
				if (projectileTurretComponent != null)
				{
					AutoLoadTurret(projectileTurretComponent);
				}
			}
		}

		private void AutoLoadTurret(ProjectileTurretComponent p)
		{
			if (p.CurProjectileClass == null && p.ProjectileTurretClass.CompatibleProjectiles.Count > 0)
			{
				p.TrySelectNextProjectileClass(ignoreAmmo: false, conventionalWeaponsOnly: false);
				if (p.CurProjectileClass != null && p == currentActiveTurret)
				{
					UpdateLabels();
				}
			}
		}

		private void currentTurret_Fired(ActiveTurret sender)
		{
			UpdateLabels();
		}

		private void OnCurrentTurretChanged()
		{
			currentProjectileTurret = currentActiveTurret as ActiveProjectileTurret;
			UpdateLabels();
			RefreshButtonStates();
			lastTurretConditionValue = -1f;
		}

		private void RefreshButtonStates()
		{
			RefreshAmmoButton();
		}

		private void RefreshAmmoButton()
		{
			if (SwitchAmmoButton != null)
			{
				bool flag = currentProjectileTurret != null && currentProjectileTurret.ProjectileTurretClass.CompatibleProjectiles.Count > 1;
				bool interactable = flag && !currentProjectileTurret.IsFiring;
				SwitchAmmoButton.gameObject.SetActive(flag);
				if (flag)
				{
					SwitchAmmoButton.interactable = interactable;
				}
			}
		}

		private bool CanFireCurrentTurret()
		{
			if (currentActiveTurret.TurretClass.RequiresTarget)
			{
				if (Hud.CurrentTarget != null)
				{
					return currentActiveTurret.CanFireAt(Hud.CurrentTarget);
				}
				return false;
			}
			return true;
		}
	}
}
