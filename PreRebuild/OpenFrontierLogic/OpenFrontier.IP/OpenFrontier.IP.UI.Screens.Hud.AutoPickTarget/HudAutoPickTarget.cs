using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.Hud.TargetScorer;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud.AutoPickTarget
{
	public class HudAutoPickTarget : MonoBehaviour
	{
		public float CooldownTime = 1f;

		private float lastAutoPickTargetTime;

		public HudScreen HudScreen;

		public HudUnitTargetScorer TargetScorer;

		public EngineASX Eng => EngineASX.Instance;

		private void Awake()
		{
			HudScreen.AutoScanner.ScanComplete += HudScreen_ScanComplete;
		}

		private void HudScreen_ScanComplete(HudAutoScanner sender)
		{
			if (!GameController.Instance.AutoTargetHostiles || !HudScreen.AllowPlayerTargetting || !(Time.time > lastAutoPickTargetTime + CooldownTime))
			{
				return;
			}
			HudCamera hudCamera = EngineASX.Instance.HudCamera;
			if (((object)hudCamera == null || hudCamera.HasChangeTargetCooldownExpired) && HudScreen.CurrentTarget == null)
			{
				Unit unit = AutopickTarget(sender.ScannedUnitCache);
				if (unit != null)
				{
					HudScreen.CurrentTarget = unit;
					lastAutoPickTargetTime = Time.time;
				}
			}
		}

		public Unit AutopickTarget(List<HudScannerUnit> cache)
		{
			Unit unit = null;
			float num = 0f;
			foreach (HudScannerUnit item in cache)
			{
				Unit unit2 = item.Unit;
				float num2 = 0f;
				if (IsValidAutoPickTarget(unit2))
				{
					if (unit2.UnitType == UnitType.Projectile)
					{
						num2 += EngineASX.Instance.GameSettings.PlayerAutoTargetLockedMissileScore;
					}
					num2 += TargetScorer.GetSelectTargetScore(unit2);
					if (unit == null || num2 > num)
					{
						unit = unit2;
					}
				}
			}
			return unit;
		}

		private bool IsValidAutoPickTarget(Unit unit)
		{
			if (unit.IsValidAndNotDestroyed && unit.IsHostileTo(Eng.LocalFaction) && (unit.IsStationOrShip() || unit.UnitType == UnitType.Projectile) && (unit.UnitType != UnitType.Station || unit.IsOwnedByPlayer || Vector3.Distance(unit.transform.position, Eng.PlayerUnit.transform.position) <= GameController.Instance.GameSettings.HudSettings.AutoPickTargetMaxRange))
			{
				if (unit.UnitType == UnitType.Projectile)
				{
					Projectile component = unit.GetComponent<Projectile>();
					if (component != null && IsProjectileValidAutoTarget(component))
					{
						return true;
					}
				}
				else if (unit.IsArmed)
				{
					return true;
				}
			}
			return false;
		}

		private bool IsProjectileValidAutoTarget(Projectile projectile)
		{
			if (projectile.IsMissile)
			{
				Missile component = projectile.GetComponent<Missile>();
				if (component.Target == HudScreen.PlayerUnit && !component.IsDisrupted && component.HasAquiredTarget)
				{
					return true;
				}
			}
			return false;
		}
	}
}
