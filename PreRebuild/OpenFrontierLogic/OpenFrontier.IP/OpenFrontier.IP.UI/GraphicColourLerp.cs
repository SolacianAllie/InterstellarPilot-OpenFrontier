using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class GraphicColourLerp : MonoBehaviour
	{
		public float Duration = 3f;

		private float elapsedTime;

		public Color EndColor = Color.white;

		public Graphic Graphic;

		public bool IgnoreTimeScale = true;

		public Color StartColor = new Color(1f, 1f, 1f, 0f);

		private Color initialColor = Color.white;

		public bool MultiplyInitialColor;

		public bool IsComplete => Progress >= 1f;

		public float Progress
		{
			get
			{
				return elapsedTime / Duration;
			}
			set
			{
				elapsedTime = Mathf.Clamp01(value) * Duration;
			}
		}

		public float ElapsedTime
		{
			get
			{
				return elapsedTime;
			}
			set
			{
				elapsedTime = Mathf.Clamp(value, 0f, Duration);
			}
		}

		private void Awake()
		{
			initialColor = Graphic.color;
		}

		public void Complete()
		{
			Progress = 1f;
			UpdateColor();
		}

		[ContextMenu("Reset")]
		public void Reset()
		{
			ElapsedTime = 0f;
			if (Graphic != null)
			{
				UpdateColor();
			}
		}

		private void Update()
		{
			ElapsedTime += (IgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime);
			if (Graphic != null)
			{
				UpdateColor();
			}
		}

		private void UpdateColor()
		{
			SetColor(Graphic);
		}

		private void SetColor(Graphic graphic)
		{
			Color color = Color.Lerp(StartColor, EndColor, Progress);
			if (MultiplyInitialColor)
			{
				graphic.color = color * initialColor;
			}
			else
			{
				graphic.color = color;
			}
		}
	}
}
