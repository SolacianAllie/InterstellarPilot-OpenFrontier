using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class AutoRecycleParticleSystem : MonoBehaviour
	{
		private void Update()
		{
			if (!GetComponent<ParticleSystem>().IsAlive())
			{
				EngineASX instance = EngineASX.Instance;
				if (instance != null && instance.Pooler != null)
				{
					instance.Pooler.RecyclePoolObject(gameObject);
				}
			}
		}
	}
}
