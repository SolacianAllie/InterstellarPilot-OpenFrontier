using UnityEngine;

namespace OpenFrontier.IP
{
	public class TweenLightColor : MonoBehaviour
	{
		public Color Color1 = Color.white;

		public Color Color2 = Color.black;

		public float Duration = 2f;

		private Light tweenLight;

		private void Awake()
		{
			tweenLight = GetComponent<Light>();
		}

		private void Update()
		{
			tweenLight.color = Color.Lerp(Color1, Color2, Maths.Sine01(Time.time));
		}
	}
}
