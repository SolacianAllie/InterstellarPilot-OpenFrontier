using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.Hud.TargetScorer;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud.Targeting
{
	public class TargetInViewSelector : MonoBehaviour
	{
		public HudUnitTargetScorer TargetScorer;

		public HudScreen HudScreen;

		public HudScanner Scanner;

		public Unit GetTargetInView()
		{
			float num = 0f;
			Unit unit = null;
			Scanner.PopulateSceneUnitCache(GameController.Instance.SelectableMask, HudScreen.nextUnitCache);
			foreach (HudScannerUnit item in HudScreen.nextUnitCache)
			{
				Unit unit2 = item.Unit;
				float selectTargetScore = TargetScorer.GetSelectTargetScore(unit2.ActiveUnit, 0.2f, 10f);
				if (unit == null || selectTargetScore > num)
				{
					unit = unit2;
					num = selectTargetScore;
				}
			}
			return unit;
		}

		public void SelectTargetInView()
		{
			if (HudScreen.PlayerUnit != null)
			{
				Unit targetInView = GetTargetInView();
				if (targetInView != null)
				{
					HudScreen.CurrentTarget = targetInView;
				}
			}
		}
	}
}
