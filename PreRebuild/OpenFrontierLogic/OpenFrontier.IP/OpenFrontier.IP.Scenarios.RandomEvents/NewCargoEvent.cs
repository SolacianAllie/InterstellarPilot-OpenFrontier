using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios.RandomEvents
{
	public class NewCargoEvent : RandomUniverseEvent
	{
		public int MinCreditsValue = 500;

		public int MaxCreditsValue = 5000;

		public List<CargoClass> PossibleCargoClasses = new List<CargoClass>();

		public string MessageText = "Jettisoned Cargo";

		public string SubjectText = "";

		public bool UseEngineClasses = true;

		public bool IncludeEquipment = true;

		public bool IncludeTraded = true;

		public override void Generate(bool silent = false)
		{
			base.Generate();
			Cargo cargoContainerPrefab = World.Engine.CargoContainerPrefab;
			if (!(cargoContainerPrefab != null))
			{
				return;
			}
			CargoClass randomCargoClass = GetRandomCargoClass(World);
			if (!(randomCargoClass != null))
			{
				return;
			}
			int num = Random.Range(MinCreditsValue, MaxCreditsValue + 1);
			int quantity = 1;
			if (randomCargoClass.BasePrice > 0)
			{
				quantity = Mathf.CeilToInt((float)num / (float)randomCargoClass.BasePrice);
			}
			Sector random = World.Engine.Sectors.Where((Sector e) => !e.IsActive).GetRandom();
			if (random != null)
			{
				Vector3 randomSafeDeploymentSectorPosition = random.GetRandomSafeDeploymentSectorPosition(1.2f, 1.5f, 20f, GameController.Instance.NonOVerlappingUnitsMask);
				Cargo cargo = Unit.CreateCargoItem(randomCargoClass, cargoContainerPrefab, quantity, random, randomSafeDeploymentSectorPosition);
				cargo.Expires = true;
				cargo.SetSpawnTime();
				cargo.SetHealthBasedOnVolume();
				if (!silent)
				{
					GenerateMessage(World, cargo.Unit);
				}
			}
		}

		private CargoClass GetRandomCargoClass(WorldBase world)
		{
			List<CargoClass> list = PossibleCargoClasses;
			if (UseEngineClasses)
			{
				list = world.Engine.CargoClasses.Where((CargoClass e) => !e.IsReserved && !e.IsDeployable && ((IncludeTraded && e.IsTraded) || (IncludeEquipment && e.IsEquipment))).ToList();
			}
			return list?.GetRandom();
		}

		private void GenerateMessage(WorldBase world, Unit unit)
		{
			GamePlayer localPlayer = world.Engine.LocalPlayer;
			if (localPlayer != null)
			{
				PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
				playerActiveMessage.MessageText = MessageText;
				string subjectText = $"{SubjectText} in the {unit.Sector.Name} sector";
				playerActiveMessage.SubjectText = subjectText;
				playerActiveMessage.FromText = "# Unknown #";
				playerActiveMessage.SetSubjectUnitAndPosition(unit);
				localPlayer.AddMessage(playerActiveMessage);
			}
		}
	}
}
