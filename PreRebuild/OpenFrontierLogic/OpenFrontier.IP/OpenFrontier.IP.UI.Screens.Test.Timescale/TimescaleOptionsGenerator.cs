using OpenFrontier.IP.UI.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test.Timescale
{
	public class TimescaleOptionsGenerator : MonoBehaviour
	{
		public Button ButtonPrefab;

		public Transform TargetTransform;

		private float[] timescales = new float[9] { 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f, 4f, 8f };

		public void Generate()
		{
			float[] array = timescales;
			foreach (float timescale in array)
			{
				Button button = Object.Instantiate(ButtonPrefab, TargetTransform);
				button.gameObject.name = $"ButtonTimescale_{timescale:N2}";
				button.onClick.AddListener(() =>
				{
					OnButtonClick(timescale);
				});
				button.SetText($"Time x{timescale:N2}");
			}
		}

		private void OnButtonClick(float timescale)
		{
			Time.timeScale = timescale;
		}
	}
}
