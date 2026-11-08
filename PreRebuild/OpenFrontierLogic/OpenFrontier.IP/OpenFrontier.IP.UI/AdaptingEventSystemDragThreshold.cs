using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenFrontier.IP.UI
{
	public class AdaptingEventSystemDragThreshold : MonoBehaviour
	{
		[SerializeField]
		private EventSystem eventSystem;

		[SerializeField]
		private int referenceDPI = 100;

		[SerializeField]
		private float referencePixelDrag = 8f;

		[SerializeField]
		private bool runOnAwake = true;

		private int oldScreenX = Screen.width;

		private int oldScreenY = Screen.height;

		private void Awake()
		{
			if (runOnAwake)
			{
				UpdatePixelDrag(ScreenDpi.Value);
			}
		}

		private void Update()
		{
			// Open Frontier: recompute on display change (fold swap changes
			// DPI) - and never feed Screen.dpi straight through: it can be 0,
			// which would set a zero drag threshold and eat every tap.
			if (Screen.width != oldScreenX || Screen.height != oldScreenY)
			{
				oldScreenX = Screen.width;
				oldScreenY = Screen.height;
				ScreenDpi.Invalidate();
				UpdatePixelDrag(ScreenDpi.Value);
			}
		}

		public void UpdatePixelDrag(float screenDpi)
		{
			if (eventSystem == null)
			{
				Debug.LogWarning("Trying to set pixel drag for adapting to screen dpi, but there is no event system assigned to the script", this);
			}
			if (screenDpi <= 0f)
			{
				screenDpi = ScreenDpi.FallbackDpi;
			}
			eventSystem.pixelDragThreshold = Mathf.RoundToInt(screenDpi / (float)referenceDPI * referencePixelDrag);
		}

		private void Reset()
		{
			if (eventSystem == null)
			{
				eventSystem = GetComponent<EventSystem>();
			}
		}
	}
}
