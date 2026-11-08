using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapSelectableUnit : MonoBehaviour
	{
		private Button button;

		private SectorMapUnitItem unitItem;

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(OnClick);
			unitItem = GetComponent<SectorMapUnitItem>();
		}

		private void OnClick()
		{
			if (unitItem != null)
			{
				Unit unit = unitItem.Unit;
				if (unit != null && !unit.IsPlayerCurrentUnit)
				{
					EngineASX.Instance.Hud.CurrentTarget = unit;
				}
			}
		}
	}
}
