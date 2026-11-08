using UnityEngine;

namespace OpenFrontier.IP
{
	public class AutoDestroyParticles : MonoBehaviour
	{
		private void Update()
		{
			if (!GetComponent<ParticleSystem>().IsAlive())
			{
				Object.Destroy(gameObject);
			}
		}
	}
}
