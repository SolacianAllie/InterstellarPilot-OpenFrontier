using UnityEngine;

namespace OpenFrontier.IP.Engine.ActiveUnitFx
{
	public class ActiveUnitParticleController : MonoBehaviour
	{
		private ActiveUnit activeUnit;

		public float StopEmittingDistance = 280f;

		public float StartEmittingDistance = 300f;

		private ParticleSystem[] particleSystems;

		private void Awake()
		{
			activeUnit = GetComponentInParent<ActiveUnit>();
			if (activeUnit == null)
			{
				Debug.LogError("Require active unit", this);
			}
			particleSystems = GetComponentsInChildren<ParticleSystem>();
			if (particleSystems.Length == 0)
			{
				Debug.LogWarning("No particle systems", this);
			}
		}

		private void Update()
		{
			if (!(activeUnit != null) || !(activeUnit.LastDistanceFromCamera < float.MaxValue))
			{
				return;
			}
			ParticleSystem[] array = particleSystems;
			foreach (ParticleSystem particleSystem in array)
			{
				if (!particleSystem.emission.enabled)
				{
					if (activeUnit.LastDistanceFromCamera < StartEmittingDistance)
					{
						ParticleSystem.EmissionModule emission = particleSystem.emission;
						emission.enabled = true;
					}
				}
				else if (activeUnit.LastDistanceFromCamera > StopEmittingDistance)
				{
					ParticleSystem.EmissionModule emission2 = particleSystem.emission;
					emission2.enabled = false;
				}
			}
		}
	}
}
