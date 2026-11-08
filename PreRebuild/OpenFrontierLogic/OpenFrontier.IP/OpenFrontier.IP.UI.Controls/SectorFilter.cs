using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens.UniverseMap;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	public class SectorFilter : MonoBehaviour
	{
		public delegate void SectorFilterChangedHandler(SectorFilter sender);

		public Sector Sector;

		public Button SectorFilterButton;

		public Button ClearSectorFilterButton;

		public event SectorFilterChangedHandler Changed;

		private void Awake()
		{
			SectorFilterButton.onClick.AddListener(SectorFilterButtonClick);
			ClearSectorFilterButton.onClick.AddListener(ClearSectorFilterButtonClick);
		}

		public void SetSector(Sector sector)
		{
			Sector = sector;
			Refresh();
		}

		public void Refresh()
		{
			if (Sector != null)
			{
				SectorFilterButton.SetText(Sector.Name);
			}
			else
			{
				SectorFilterButton.SetText("[All]");
			}
			ClearSectorFilterButton.gameObject.SetActive(Sector != null);
		}

		private void SectorFilterButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowCustomUniverseMapScreen((UniverseMapScreen screen) =>
			{
				screen.Title = "Select sector to filter on...";
				screen.RestrictNavigationAway();
				screen.AllowSectorSelection = true;
				screen.AllowSectorSelectionPick = true;
				screen.SelectSector(Sector);
				screen.ShowSelectedSectorInfo = false;
				screen.SectorSelectedCallback = SectorFilterSectorSelected;
			}, (UniverseMapScreen screen) =>
			{
				if (Sector != null)
				{
					screen.CenterOnSector(Sector);
				}
				else
				{
					screen.CenterOnSector(EngineASX.Instance.ActiveSector);
				}
			});
		}

		private void SectorFilterSectorSelected(UniverseMapScreen universeMapScreen, Sector sector)
		{
			universeMapScreen.NavigateBack();
			SetSector(sector);
			RaiseSectorChanged();
		}

		private void ClearSectorFilterButtonClick()
		{
			if (Sector != null)
			{
				SetSector(null);
				RaiseSectorChanged();
			}
		}

		private void RaiseSectorChanged()
		{
			if (Changed != null)
			{
				Changed(this);
			}
		}
	}
}
