using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class SectorPathIconsDisplay : MonoBehaviour
	{
		public Graphic MissionPathIcon;

		public Graphic CustomPathIcon;

		[SerializeField]
		private Sector sector;

		public Sector Sector
		{
			get
			{
				return sector;
			}
			set
			{
				sector = value;
				if (sector != null && sector.Engine != null)
				{
					UpdatePathIconColors();
				}
			}
		}

		private void OnEnable()
		{
			MissionPathIcon.gameObject.SetActive(value: false);
			CustomPathIcon.gameObject.SetActive(value: false);
		}

		public void Refresh()
		{
			UpdateMissionPathIcon();
			UpdateCustomPathIcon();
		}

		private void UpdateCustomPathIcon()
		{
			CustomPathIcon.gameObject.SetActive(sector != null && sector.ContainsPlayerCustomWaypointTarget());
		}

		private void UpdateMissionPathIcon()
		{
			MissionPathIcon.gameObject.SetActive(sector != null && sector.ContainsPlayerMissionWaypointTarget());
		}

		private void UpdatePathIconColors()
		{
			CustomPathIcon.color = sector.Engine.CustomPathColor;
			MissionPathIcon.color = sector.Engine.MissionPathColor;
		}
	}
}
