using UnityEngine;

public class AutoDestroyPS : MonoBehaviour
{
	private float timeLeft;

	private void Awake()
	{
		ParticleSystem.MainModule main = GetComponent<ParticleSystem>().main;
		timeLeft = main.startLifetimeMultiplier + main.duration;
		Object.Destroy(gameObject, timeLeft);
	}
}
