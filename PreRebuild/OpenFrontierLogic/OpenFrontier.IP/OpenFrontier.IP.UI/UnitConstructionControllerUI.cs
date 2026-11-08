using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class UnitConstructionControllerUI : MonoBehaviour
	{
		public TextMeshProUGUI Text;

		public bool IgnoreDistance;

		public Unit Unit;

		public Image Image;

		private void Awake()
		{
			Image.enabled = false;
			Text.enabled = false;
		}

		public void Refresh()
		{
			ShowOrHideUnderConstruction();
		}

		private void ShowOrHideUnderConstruction()
		{
			bool flag = ShouldShowUnderConstructionInfo();
			Image image = Image;
			bool flag2 = (Text.enabled = flag);
			image.enabled = flag2;
			if (flag)
			{
				RefreshUnderConstructionText();
			}
		}

		public bool ShouldShowUnderConstructionInfo()
		{
			if (Unit != null && Unit.IsValidAndNotDestroyed && Unit.IsUnderConstructionOrDismantling)
			{
				if (!IgnoreDistance)
				{
					if (Unit.ActiveUnit != null)
					{
						return Unit.ActiveUnit.LastDistanceFromCamera < Unit.Engine.GameSettings.UIUnderConstructionDrawDistance;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		private void RefreshUnderConstructionText()
		{
			if (Text != null)
			{
				Text.SetText("{0}%", (int)(Unit.Components.ConstructionProgress * 100f));
			}
		}
	}
}
