using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP
{
	public class DestroyOnEngineLoad : MonoBehaviour
	{
		private EngineASX engine;

		private void Update()
		{
			if (engine == null)
			{
				engine = EngineASX.Instance;
				if (engine != null)
				{
					engine.WorldLoaded += engine_WorldLoaded;
				}
			}
		}

		private void engine_WorldLoaded(EngineASX sender)
		{
			sender.WorldLoaded -= engine_WorldLoaded;
			Object.Destroy(gameObject);
		}
	}
}
