using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	[RequireComponent(typeof(Selectable))]
	public class UseButtonColorSettings : MonoBehaviour
	{
		private void Awake()
		{
			if (GameController.Instance != null)
			{
				GetComponent<Selectable>().colors = GameController.Instance.GameSettings.UIButtonColors.ButtonColors;
				Toggle component = GetComponent<Toggle>();
				if (component != null && component.graphic != null)
				{
					component.graphic.color = GameController.Instance.GameSettings.UIButtonColors.ToggleGraphicColor;
				}
			}
		}
	}
}
