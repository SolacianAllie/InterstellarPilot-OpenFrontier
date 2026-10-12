using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud.QuickTractor
{
	public class QuickTractorController : MonoBehaviour
	{
		public HudScreen HudScreen;

		public QuickTractorButton TractorButton;

		private HashSet<int> shipCompatibleCargoClasses = new HashSet<int>(8);

		private float lastTimeCachedCompatibleCargoClasses = float.MinValue;

		private void Awake()
		{
			TractorButton.Button.onClick.AddListener(TractorLoot);
			TractorButton.gameObject.SetActive(value: false);
			HudScreen.AutoScanner.ScanComplete += HudScreen_ScanComplete;
		}

		private void HudScreen_ScanComplete(HudAutoScanner sender)
		{
			if (HudScreen.PlayerTractorTurret != null && EngineASX.Instance.IsPlayerPilot)
			{
				CargoBayComponent cargoBayComponent = EngineASX.Instance.LocalUnit.CargoBayComponent;
				if (cargoBayComponent != null)
				{
					TractorButton.CargoComponent = GetBestTractorableCargo(HudScreen.PlayerTractorTurret, cargoBayComponent);
				}
				else
				{
					TractorButton.CargoComponent = null;
				}
			}
			else
			{
				TractorButton.CargoComponent = null;
			}
		}

		public void TractorLoot()
		{
			if (HudScreen.PlayerTractorTurret != null && TractorButton.CargoComponent != null && TractorButton.CargoComponent.Unit.IsValidAndNotDestroyed)
			{
				HudScreen.PlayerTractorTurret.Fire(TractorButton.CargoComponent.Unit);
			}
		}

		private void Update()
		{
			if (EngineASX.LoadedAndReady)
			{
				bool active = HudScreen.PlayerTractorTurret != null && TractorButton.CargoComponent != null && TractorButton.CargoComponent.Unit.IsValidAndNotDestroyed && TractorButton.CargoComponent.Unit.Tractorer == null && HudScreen.PlayerTractorTurret.IsReadyToFire(TractorButton.CargoComponent.Unit);
				TractorButton.gameObject.SetActive(active);
				if (Time.time > lastTimeCachedCompatibleCargoClasses + 120f)
				{
					NpcPilot.CacheCompatibleAmmoTypes(EngineASX.Instance.LocalUnit, shipCompatibleCargoClasses);
					lastTimeCachedCompatibleCargoClasses = Time.time;
				}
			}
		}

		public Cargo GetBestTractorableCargo(TractorTurretComponent tractorComponent, CargoBayComponent cargoBayComponent)
		{
			if (cargoBayComponent.FreeSpace <= 0f)
			{
				return null;
			}
			if (tractorComponent.IsReadyToFireIgnoringTarget())
			{
				int a = Physics.OverlapSphereNonAlloc(tractorComponent.transform.position, tractorComponent.TurretClass.MaxFiringRange, EngineASX.ColliderCache, GameController.Instance.CargoLayer, QueryTriggerInteraction.Collide);
				Unit unit = null;
				float num = 0f;
				int num2 = Mathf.Min(a, 10);
				for (int i = 0; i < num2; i++)
				{
					Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
					if (component != null && component.IsValidAndNotDestroyed && component.CargoComponent != null && component.Tractorer == null && cargoBayComponent.GetFreeSpaceFor(component.CargoComponent.CargoClass) > 0 && (component.Faction == null || component.Faction == EngineASX.Instance.LocalFaction || (EngineASX.Instance.GameSettings.ShowQuickTractorButtonForHostileCargo && EngineASX.Instance.LocalFaction.IsHostileTo(component.Faction))) && tractorComponent.IsReadyToFire(component))
					{
						float cargoScore = GetCargoScore(component.CargoComponent);
						if (cargoScore >= 0f && (unit == null || cargoScore > num))
						{
							unit = component;
							num = cargoScore;
						}
					}
				}
				if (unit != null)
				{
					return unit.CargoComponent;
				}
			}
			return null;
		}

		private float GetCargoScore(Cargo cargo)
		{
			if (cargo.CargoClass.IsEquipment)
			{
				if (shipCompatibleCargoClasses.Contains(cargo.CargoClass.UniqueId))
				{
					return 2f;
				}
				if (!EngineASX.Instance.World.ShowQuickTractorButtonForIncompatible)
				{
					return -1f;
				}
			}
			return 1f;
		}

		public void CleanupOnDisable()
		{
			TractorButton.gameObject.SetActive(value: false);
		}
	}
}
