using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud.TargetScorer
{
	public class HudUnitTargetScorer : MonoBehaviour
	{
		public float GetSelectTargetScore(Unit unit)
		{
			GameSettings gameSettings = GameController.Instance.GameSettings;
			return GetSelectTargetScore(unit.ActiveUnit, gameSettings.PlayerAutoTargetDistanceScore, gameSettings.PlayerAutoTargetBearingScore);
		}

		public float GetSelectTargetScore(ActiveUnit activeUnit, float distanceScore, float bearingScore)
		{
			float lastDistanceFromCamera = activeUnit.LastDistanceFromCamera;
			GameSettings gameSettings = GameController.Instance.GameSettings;
			float num = 0f;
			if (lastDistanceFromCamera < gameSettings.PlayerAutoTargetMaxDistance)
			{
				num += (1f - lastDistanceFromCamera / gameSettings.PlayerAutoTargetMaxDistance) * distanceScore;
			}
			float degrees = Unit.GetYBearing(GameController.Instance.MainCamera.transform.position, activeUnit.transform.position) - GameController.Instance.MainCamera.transform.eulerAngles.y;
			degrees = Geometry.WrapDegreesForBearing(degrees);
			return num + (180f - Mathf.Abs(degrees)) / 180f * bearingScore;
		}
	}
}
