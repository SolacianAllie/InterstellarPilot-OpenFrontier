using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.UniverseMap
{
	public class UniverseMapSectorDisplayIcons : MonoBehaviour
	{
		public Image PropertyImage;

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
			}
		}

		public void Refresh()
		{
			RefreshPropertyImage();
		}

		private void RefreshPropertyImage()
		{
			PropertyImage.gameObject.SetActive(sector != null && Sector.ContainsPlayerProperty());
		}
	}
}
