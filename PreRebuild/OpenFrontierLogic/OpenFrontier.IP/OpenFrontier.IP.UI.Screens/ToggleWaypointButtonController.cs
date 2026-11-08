using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens
{
	public class ToggleWaypointButtonController : MonoBehaviour
	{
		public Button Button;

		public Unit CurrentTarget;

		public GameObject ToggleOnObject;

		private void Awake()
		{
			Button.onClick.AddListener(ButtonClick);
		}

		private void ButtonClick()
		{
			if (HasValidTarget())
			{
				EngineASX.Instance.LocalPlayer.ToggleCustomWaypoint(CurrentTarget);
			}
		}

		private void Update()
		{
			Button.interactable = WorldHelper.AllowToggleWaypoint(CurrentTarget);
			ToggleOnObject.SetActive(HasValidTarget() && CurrentTarget.IsPlayerCustomPathTarget());
		}

		private bool HasValidTarget()
		{
			if (CurrentTarget != null)
			{
				return CurrentTarget.IsValidAndNotDestroyed;
			}
			return false;
		}
	}
}
