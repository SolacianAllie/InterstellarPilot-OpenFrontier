using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud.Targeting
{
	public class TargetInScreenSelector : MonoBehaviour
	{
		public HudScanner HudScanner;

		public float SelectedPixelThreshold = 90f;

		public Unit GetTargetAtScreenPos(Vector3 mousePosition, Unit curTarget)
		{
			Unit result = null;
			float num = float.MaxValue;
			HudTargetController hudTargetController = EngineASX.Instance.Hud.HudTargetController;
			if (hudTargetController != null)
			{
				for (int i = 0; i < hudTargetController.ActiveTargetsCount; i++)
				{
					HudTargetUI activeTarget = hudTargetController.GetActiveTarget(i);
					if (activeTarget.Unit != curTarget)
					{
						Vector2 vector = RectTransformUtility.WorldToScreenPoint(null, activeTarget.transform.position);
						float num2 = Vector2.Distance(new Vector2(mousePosition.x, mousePosition.y), new Vector2(vector.x, vector.y));
						if (num2 < SelectedPixelThreshold && num2 < num)
						{
							num = num2;
							result = activeTarget.Unit;
						}
					}
				}
			}
			return result;
		}

		public static bool IsPositionInScreen(ref Vector3 screenPos)
		{
			if (screenPos.x >= 0f && screenPos.x < (float)Screen.width && screenPos.y >= 0f && screenPos.y < (float)Screen.height)
			{
				return screenPos.z > 0f;
			}
			return false;
		}
	}
}
