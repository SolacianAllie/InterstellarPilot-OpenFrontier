using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class DebrisMaker : MonoBehaviour
	{
		public DebrisSimple[] DebrisPrefabs;

		public int MaxCount = 8;

		public int MinCount = 5;

		public void CreateDebris(EngineASX engine, Vector3 position)
		{
			int num = Random.Range(MinCount, MaxCount);
			for (int i = 0; i < num; i++)
			{
				DebrisSimple random = DebrisPrefabs.GetRandom();
				if (random != null)
				{
					engine.PlayPooledDebris(random.gameObject, position);
				}
			}
		}
	}
}
