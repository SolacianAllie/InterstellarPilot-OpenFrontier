using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios.RandomEvents
{
	public class RandomUniverseEventsSeeder : MonoBehaviour
	{
		private WorldBase world;

		private void Awake()
		{
			world = GetComponentInParent<WorldBase>();
			world.NewGame += World_NewGame;
		}

		private void World_NewGame(WorldBase sender)
		{
			Seed();
		}

		private void Seed()
		{
			RandomUniverseEvent[] componentsInChildren = GetComponentsInChildren<RandomUniverseEvent>();
			foreach (RandomUniverseEvent randomUniverseEvent in componentsInChildren)
			{
				int num = Random.Range(randomUniverseEvent.MinSeedCount, randomUniverseEvent.MaxSeedCount + 1);
				for (int j = 0; j < num; j++)
				{
					randomUniverseEvent.Generate(silent: true);
				}
			}
		}
	}
}
