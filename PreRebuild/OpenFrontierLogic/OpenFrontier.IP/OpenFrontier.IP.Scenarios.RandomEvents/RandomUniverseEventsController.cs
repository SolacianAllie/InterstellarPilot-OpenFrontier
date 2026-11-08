using OpenFrontier.IP.Engine;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios.RandomEvents
{
	public class RandomUniverseEventsController : MonoBehaviour
	{
		public float MaxTimeBetweenEvents = 720f;

		public float MinTimeBetweenEvents = 360f;

		public double NextEventTime;

		[ContextMenu("Generate Event")]
		public void GenerateEvent()
		{
			RandomUniverseEvent random = GetComponentsInChildren<RandomUniverseEvent>().GetRandom();
			if (random != null)
			{
				random.Generate();
			}
		}

		public void Init()
		{
			NextEventTime = EngineASX.Instance.ScenarioElapsedTime + (double)(Random.value * MaxTimeBetweenEvents);
		}

		private void UpdateNextEventTime()
		{
			NextEventTime = GetNextEventTime();
		}

		private void Update()
		{
			if (EngineASX.LoadedAndReady)
			{
				WorldBase world = EngineASX.Instance.World;
				if (world.ObjectiveState == WorldBase.ScenarioState.Playing && world.Engine.ScenarioElapsedTime > NextEventTime)
				{
					GenerateEvent();
					UpdateNextEventTime();
				}
			}
		}

		private double GetNextEventTime()
		{
			return EngineASX.Instance.ScenarioElapsedTime + (double)Mathf.Lerp(MinTimeBetweenEvents, MaxTimeBetweenEvents, Random.value);
		}
	}
}
