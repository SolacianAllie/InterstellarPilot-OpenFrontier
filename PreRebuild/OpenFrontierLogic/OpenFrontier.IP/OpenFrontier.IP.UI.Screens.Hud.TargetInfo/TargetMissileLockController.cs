using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud.TargetInfo
{
	public class TargetMissileLockController : MonoBehaviour
	{
		public GameObject ToggleObject;

		public Unit TargetUnit;

		private void Update()
		{
			if (EngineASX.LoadedAndReady)
			{
				GameObject gameObject = ToggleObject.gameObject;
				Unit targetUnit = TargetUnit;
				gameObject.SetActive((object)targetUnit != null && targetUnit.IsValidAndNotDestroyed && TargetUnit.IsInActiveSector && TargetUnit.IsStationOrShip() && EngineASX.Instance.MissileLockController.HasMissileLock(TargetUnit));
			}
		}
	}
}
