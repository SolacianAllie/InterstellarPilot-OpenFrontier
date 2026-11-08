using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud.Components
{
	public class HudShieldAndHullUI : UnitConditionControllerUI
	{
		public Transform ShieldsRoot;

		public Transform HullRoot;

		public override void Tick()
		{
			base.Tick();
			if (EngineASX.Instance.World.Permissions.AutoHidePlayerShieldAndHull)
			{
				SetEnabled(ShouldShowShieldAndHull());
			}
		}

		protected virtual void SetEnabled(bool enabled)
		{
			HullRoot.gameObject.SetActive(enabled);
			ShieldsRoot.gameObject.SetActive(enabled);
		}

		public bool ShouldShowShieldAndHull()
		{
			if (!EngineASX.Instance.Hud.AlwaysShowShieldAndHull && !localUnit.IsShieldDamaged() && !localUnit.IsHullDamaged())
			{
				return localUnit.IsShieldDisabled();
			}
			return true;
		}
	}
}
