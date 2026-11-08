using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class UnitPathIconsDisplay : MonoBehaviour
	{
		public Graphic MissionPathIcon;

		public Graphic CustomPathIcon;

		public GameObject BountyIcon;

		[SerializeField]
		private Unit unit;

		public Unit Unit
		{
			get
			{
				return unit;
			}
			set
			{
				unit = value;
				if (unit != null && unit.Engine != null)
				{
					UpdatePathIconColors();
				}
			}
		}

		private void OnEnable()
		{
			if (MissionPathIcon != null)
			{
				MissionPathIcon.gameObject.SetActive(value: false);
			}
			if (CustomPathIcon != null)
			{
				CustomPathIcon.gameObject.SetActive(value: false);
			}
			if (BountyIcon != null)
			{
				BountyIcon.gameObject.SetActive(value: false);
			}
		}

		private void Update()
		{
			if (unit != null && unit.IsValid)
			{
				UpdateMissionPathIcon();
				UpdateCustomPathIcon();
				if (BountyIcon != null)
				{
					BountyIcon.SetActive(unit != null && unit.UnitPilotHasBounty());
				}
			}
		}

		private void UpdateCustomPathIcon()
		{
			if (CustomPathIcon != null)
			{
				CustomPathIcon.gameObject.SetActive(unit != null && unit.UnitType != UnitType.Waypoint && unit.IsPlayerCustomPathTargetOrFirstWaypointOrPathMarker());
			}
		}

		private void UpdateMissionPathIcon()
		{
			if (MissionPathIcon != null)
			{
				MissionPathIcon.gameObject.SetActive(unit != null && unit.UnitType != UnitType.Waypoint && unit.IsPlayerMissionPathTargetOrFirstWaypoint());
			}
		}

		private void UpdatePathIconColors()
		{
			if (CustomPathIcon != null)
			{
				CustomPathIcon.color = unit.Engine.CustomPathColor;
			}
			if (MissionPathIcon != null)
			{
				MissionPathIcon.color = unit.Engine.MissionPathColor;
			}
		}
	}
}
