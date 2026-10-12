using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class CustomSafeArea : MonoBehaviour
	{
		public enum SimDevice
		{
			None,
			iPhoneX,
			iPhoneXsMax,
			Pixel3XL_LSL,
			Pixel3XL_LSR
		}

		public static SimDevice Sim;

		private Rect[] NSA_iPhoneX = new Rect[2]
		{
			new Rect(0f, 0.04187192f, 1f, 0.9039409f),
			new Rect(0.054187194f, 0.056f, 0.89162564f, 0.944f)
		};

		private Rect[] NSA_iPhoneXsMax = new Rect[2]
		{
			new Rect(0f, 0.03794643f, 1f, 0.9129464f),
			new Rect(0.04910714f, 7f / 138f, 101f / 112f, 131f / 138f)
		};

		private Rect[] NSA_Pixel3XL_LSL = new Rect[2]
		{
			new Rect(0f, 0f, 1f, 0.94222975f),
			new Rect(0f, 0f, 0.94222975f, 1f)
		};

		private Rect[] NSA_Pixel3XL_LSR = new Rect[2]
		{
			new Rect(0f, 0f, 1f, 0.94222975f),
			new Rect(0.05777027f, 0f, 0.94222975f, 1f)
		};

		private RectTransform Panel;

		private Rect LastSafeArea = new Rect(0f, 0f, 0f, 0f);

		private Vector2Int LastScreenSize = new Vector2Int(0, 0);

		private ScreenOrientation LastOrientation = ScreenOrientation.AutoRotation;

		[SerializeField]
		private bool ConformX = true;

		[SerializeField]
		private bool ConformY = true;

		[SerializeField]
		private bool Logging;

		private void Start()
		{
			Panel = GetComponent<RectTransform>();
			if (Panel == null)
			{
				Debug.LogError("Cannot apply safe area - no RectTransform found on " + name);
				Object.Destroy(gameObject);
			}
			RefreshIfRequired();
		}

		private void Update()
		{
			RefreshIfRequired();
		}

		private void RefreshIfRequired()
		{
			Rect safeArea = GetSafeArea();
			if (safeArea != LastSafeArea || Screen.width != LastScreenSize.x || Screen.height != LastScreenSize.y || Screen.orientation != LastOrientation)
			{
				LastScreenSize.x = Screen.width;
				LastScreenSize.y = Screen.height;
				LastOrientation = Screen.orientation;
				ApplySafeArea(safeArea);
			}
		}

		public void Refresh()
		{
			Rect safeArea = GetSafeArea();
			LastScreenSize.x = Screen.width;
			LastScreenSize.y = Screen.height;
			LastOrientation = Screen.orientation;
			ApplySafeArea(safeArea);
		}

		private Rect GetSafeArea()
		{
			Rect safeArea = Screen.safeArea;
			float num = GameController.Instance.UISafeAreaLeft * (float)Screen.width;
			float num2 = GameController.Instance.UISafeAreaRight * (float)Screen.width;
			float num3 = GameController.Instance.UISafeAreaTop * (float)Screen.height;
			float num4 = GameController.Instance.UISafeAreaBottom * (float)Screen.height;
			safeArea.x += num;
			safeArea.y += num4;
			safeArea.width -= num;
			safeArea.width -= num2;
			safeArea.height -= num3;
			safeArea.height -= num4;
			return safeArea;
		}

		private void ApplySafeArea(Rect r)
		{
			LastSafeArea = r;
			if (!ConformX)
			{
				r.x = 0f;
				r.width = Screen.width;
			}
			if (!ConformY)
			{
				r.y = 0f;
				r.height = Screen.height;
			}
			if (Screen.width > 0 && Screen.height > 0)
			{
				Vector2 position = r.position;
				Vector2 anchorMax = r.position + r.size;
				position.x /= Screen.width;
				position.y /= Screen.height;
				anchorMax.x /= Screen.width;
				anchorMax.y /= Screen.height;
				if (position.x >= 0f && position.y >= 0f && anchorMax.x >= 0f && anchorMax.y >= 0f)
				{
					Panel.anchorMin = position;
					Panel.anchorMax = anchorMax;
				}
			}
			if (Logging)
			{
				Debug.LogFormat("New safe area applied to {0}: x={1}, y={2}, w={3}, h={4} on full extents w={5}, h={6}", name, r.x, r.y, r.width, r.height, Screen.width, Screen.height);
			}
		}
	}
}
