using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class LoadingScreenUI : MonoBehaviour
	{
		private bool isDestroying;

		public LoadingWidgetUI LoadingWidget;

		private static LoadingScreenUI instance;

		public static LoadingScreenUI Instance => instance;

		private void Awake()
		{
			instance = this;
		}

		private void OnDestroy()
		{
			if (instance == this)
			{
				instance = null;
			}
		}

		private void Start()
		{
			Time.timeScale = 0f;
			if (GameController.Instance != null)
			{
				GameController.Instance.ScreenScaler.ScaleScreen(gameObject);
			}
		}

		private void Update()
		{
			if (!isDestroying && (GameController.Instance.ScenarioLoader == null || !GameController.Instance.ScenarioLoader.IsLoading) && EngineASX.LoadedAndReady)
			{
				isDestroying = true;
				Object.Destroy(gameObject);
			}
			LoadingWidget.gameObject.SetActive(EngineASX.IsLoading);
		}
	}
}
