using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud.Components
{
	public class WorldSpaceShieldAndHullUI : HudShieldAndHullUI
	{
		public float ManualScale;

		public float UnitShieldRingRadiusMultiplier = 1f;

		public Transform WorldRoot;

		public Canvas Canvas;

		public override void Tick()
		{
			if (localUnit != null && localUnit.IsValid)
			{
				WorldRoot.transform.position = localUnit.transform.position + localUnit.UnitClass.DisplayData.HudCompassManualFudgeOffset;
				WorldRoot.transform.localRotation = Quaternion.Euler(0f, localUnit.transform.eulerAngles.y, 0f);
				float scale = GetScale(localUnit.UnitClass);
				WorldRoot.transform.localScale = new Vector3(scale, scale, scale);
			}
			base.Tick();
		}

		protected override void SetEnabled(bool enabled)
		{
			Canvas.enabled = enabled;
		}

		public float GetScale(UnitClass unitClass)
		{
			return ManualScale + unitClass.ShieldRingRadius * UnitShieldRingRadiusMultiplier * unitClass.DisplayData.HudCompassRadiusFudgeMultiplier;
		}

		protected override void RepositionTargetShieldBearing()
		{
			if (ShieldBarBearingTransform != null && TargetUnit != null)
			{
				float num = (float)(-localUnit.GetShieldIndex(TargetUnit.transform.position)) * 60f;
				ShieldBarBearingTransform.transform.localEulerAngles = new Vector3(90f, 0f - num, 0f);
			}
		}
	}
}
