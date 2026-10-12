using System.Collections.Generic;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Fleets.ActiveObjectives;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Npcs
{
	public class NpcCargoCollector
	{
		private double nextCheckTime;

		private TractorTurretComponent tractorTurretComponent;

		public NpcPilot Npc;

		private Queue<Cargo> cargoTargetQueue = new Queue<Cargo>(8);

		private Cargo bestCargoToCollect;

		private float? bestCargoToCollectScore;

		public const float RequiredFreeSpaceUnitsToSearch = 1f;

		public void Update()
		{
			if (bestCargoToCollect != null && Time.time > Npc.ComponentUseCooldownTime)
			{
				FireTurretIfAble();
			}
			if (cargoTargetQueue.Count > 0)
			{
				ContinueScoringCache();
			}
			else if ((double)Time.time > nextCheckTime)
			{
				if (tractorTurretComponent == null)
				{
					tractorTurretComponent = Npc.CurrentUnitComponents.TractorTurret;
				}
				else if (IsReadyToStartSearching())
				{
					PerformScan();
				}
				PerformanceSettings performanceSettings = GameController.Instance.GameSettings.PerformanceSettings;
				if (Npc.IsInCombat && Random.value < 0.25f)
				{
					nextCheckTime = Time.time + Random.Range(2f, 8f);
				}
				else
				{
					nextCheckTime = Time.time + Mathf.Lerp(performanceSettings.MinNpcCargoCollectorCheckInterval, performanceSettings.MaxNpcCargoCollectorCheckInterval, Npc.Settings.CombatEfficiency);
				}
			}
		}

		private void ContinueScoringCache()
		{
			Cargo cargo = cargoTargetQueue.Dequeue();
			float? num = ScorePotentialTarget(cargo);
			if (num.HasValue && (!bestCargoToCollectScore.HasValue || num > bestCargoToCollectScore))
			{
				bestCargoToCollect = cargo;
				bestCargoToCollectScore = num;
			}
			if (cargoTargetQueue.Count == 0)
			{
				OnFinishedSearching();
			}
		}

		private bool IsReadyToStartSearching()
		{
			return Npc.CurrentUnitComponents.CargoBayComponent.FreeSpace >= 1f;
		}

		private float? ScorePotentialTarget(Cargo cargo)
		{
			if (cargo.Expires)
			{
				double spawnTime = cargo.SpawnTime;
				if (EngineASX.Instance.ScenarioElapsedTime - spawnTime < (double)Mathf.Lerp(2f, 1f, Npc.Settings.CombatEfficiency))
				{
					return null;
				}
			}
			if (cargo != null && cargo.Unit != null && cargo.Unit.IsValidAndNotDestroyed && cargo.Quantity > 0)
			{
				int num = Mathf.Min(cargo.Quantity, Npc.CurrentUnitComponents.CargoBayComponent.GetFreeSpaceFor(cargo.CargoClass));
				if (num > 0)
				{
					CargoOwnership cargoOwnership = CollectCargoHelper.CalculateCargoOwnership(cargo, Npc.Faction);
					float num2 = Npc.ScoreCargoToCollect(cargo.Unit, cargo.CargoClass, num, cargoOwnership);
					if (num2 > 0f)
					{
						return num2;
					}
				}
			}
			return null;
		}

		private void OnFinishedSearching()
		{
			FireTurretIfAble();
		}

		private void FireTurretIfAble()
		{
			bool isInActiveSector = Npc.CurrentUnit.IsInActiveSector;
			if (bestCargoToCollect != null && bestCargoToCollect.Unit.IsValidAndNotDestroyed && Npc.CurrentUnitComponents.CargoBayComponent.GetFreeSpaceFor(bestCargoToCollect.CargoClass) > 0 && tractorTurretComponent != null && ((isInActiveSector && tractorTurretComponent.IsReadyToFireIgnoringTarget()) || (!isInActiveSector && tractorTurretComponent.IsReadyToFire(bestCargoToCollect.Unit))))
			{
				tractorTurretComponent.Fire(bestCargoToCollect.Unit);
			}
		}

		private void PerformScan()
		{
			bool flag = tractorTurretComponent.IsReadyToFireIgnoringTarget();
			bestCargoToCollect = null;
			bestCargoToCollectScore = null;
			cargoTargetQueue.Clear();
			if (!flag)
			{
				return;
			}
			Vector3 vector = Vector3.zero;
			if (Npc.CurrentUnit.IsInActiveSector && Npc.CurrentUnit.RBody != null)
			{
				vector = Npc.CurrentUnit.RBody.linearVelocity;
			}
			int num = Physics.OverlapSphereNonAlloc(tractorTurretComponent.transform.position + vector * 1.5f, tractorTurretComponent.TurretClass.MaxFiringRange, EngineASX.ColliderCache, GameController.Instance.CargoLayer, QueryTriggerInteraction.Collide);
			for (int i = 0; i < num; i++)
			{
				Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
				if (component != null && component.IsValidAndNotDestroyed && component.CargoComponent != null && component.Tractorer == null)
				{
					cargoTargetQueue.Enqueue(component.CargoComponent);
				}
			}
		}
	}
}
