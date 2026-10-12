using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CreatePassengersSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding passsengers...", this, 1);
			}
			Seed(world);
		}

		public static void Seed(WorldBase world)
		{
			EngineASX.Instance.EnumerateUnitsWithPredicate((Unit unit) =>
			{
				SeedAtUnit(unit);
			}, (Unit unit) => PassengerManager.ShouldCreatePassengersAtUnit(unit));
		}

		public static void SeedAtUnit(Unit unit)
		{
			float num = Random.Range(0.5f, 0.75f);
			int num2 = Mathf.CeilToInt((float)PasssengerGroupHelper.GetMaxPassengerGroupCountAtUnit(unit) * num);
			for (int i = 0; i < num2; i++)
			{
				Unit instantDestination = PassengerManager.GetInstantDestination(unit);
				if (instantDestination != null)
				{
					int num3 = PassengerManager.CreatePassengerGroup(unit, instantDestination, initialSeed: true);
					if (num3 > 0)
					{
						EngineASX.Instance.DebugInfo.PassengerGroups_NumGroupsSeeded++;
						EngineASX.Instance.DebugInfo.PassengerGroups_NumPassengersSeeded += num3;
					}
				}
			}
		}
	}
}
