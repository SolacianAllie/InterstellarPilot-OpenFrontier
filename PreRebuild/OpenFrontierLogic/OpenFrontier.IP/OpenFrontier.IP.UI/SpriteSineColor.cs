using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class SpriteSineColor : MonoBehaviour
	{
		public Color from = Color.white;

		public float Speed = 1f;

		public Color to = Color.white;

		public bool UseRealTime;

		private Graphic widget;

		private void Awake()
		{
			widget = GetComponent<Graphic>();
		}

		private void Update()
		{
			widget.color = GetColor();
		}

		public Color GetColor()
		{
			if (UseRealTime)
			{
				return GetColor(Time.realtimeSinceStartup);
			}
			return GetColor(Time.time);
		}

		public void Reset()
		{
			widget.color = from;
		}

		private Color GetColor(float elapsedTime)
		{
			return GetColor(from, to, elapsedTime * Speed);
		}

		public static Color GetColor(Color from, Color to, float elapsedTime)
		{
			return Color.Lerp(from, to, Maths.Sine01(elapsedTime));
		}
	}
}
