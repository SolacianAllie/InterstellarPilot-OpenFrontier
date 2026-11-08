using UnityEngine;

namespace OpenFrontier.IP.UI
{
	[RequireComponent(typeof(Canvas))]
	public class PhysicalCanvasScaler : MonoBehaviour
	{
		private Canvas canvas;

		private int oldScreenX = Screen.width;

		private int oldScreenY = Screen.height;

		public float ReferenceScreenHeight = 720f;

		public float TargetScreenHeightInches = 2.347441f;

		[ContextMenu("Update Canvas Scale Factor")]
		public void UpdateCanvasScaleFactor()
		{
			float desiredCanvasScaleFactor = GetDesiredCanvasScaleFactor();
			canvas.scaleFactor = desiredCanvasScaleFactor;
		}

		private void Start()
		{
			canvas = GetComponent<Canvas>();
			UpdateCanvasScaleFactor();
		}

		private float GetDesiredCanvasScaleFactor()
		{
			float physicalHeight = GetPhysicalHeight();
			return (float)Screen.height / ReferenceScreenHeight * TargetScreenHeightInches / physicalHeight;
		}

		private void Update()
		{
			if (Screen.width != oldScreenX || Screen.height != oldScreenY)
			{
				UpdateCanvasScaleFactor();
				oldScreenY = Screen.height;
				oldScreenX = Screen.width;
			}
		}

		private float GetPhysicalHeight()
		{
			if (Screen.dpi > 0f)
			{
				return (float)Screen.height / Screen.dpi;
			}
			return TargetScreenHeightInches;
		}
	}
}
